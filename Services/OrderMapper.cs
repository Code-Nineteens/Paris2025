using Npgsql;
using Paris2025.Entities;
using Paris2025.Enumerations;

namespace Paris2025.Services;

public class OrderMapper
{
    /// <summary>
    /// Maps database row to Order entity from old_orders table
    /// Uses actual column names from old_orders table
    /// </summary>
    public static Order MapFromDictionary(Dictionary<string, object?> data, ILogger logger)
    {
        return new Order
        {
            SrcOrderId = GetLong(data, "legacyresourceid", logger),
            OrderName = GetString(data, "order_name"),
            OrderDate = GetDateTime(data, "createdat", logger) ?? DateTime.UtcNow,
            CloseDate = GetNullableDateTime(data, "closedat", logger),
            CancelDate = GetNullableDateTime(data, "cancelledat", logger),
            Currency = ParseCurrencyFromCode(GetString(data, "currencycode")),
            PresentmentCurrency = ParseCurrencyFromCode(GetString(data, "presentmentcurrencycode")),
            Status = ParseFinancialStatusFromText(GetString(data, "financial_status")),
            FulfillmentStatus = ParseFulfillmentStatusFromText(GetString(data, "fulfillment_status")),
            
            TotalPrice = GetDecimal(data, "total_price"),
            SubtotalPrice = GetDecimal(data, "subtotal_price"),
            TotalDiscounts = GetDecimal(data, "total_discounts"),
            TotalShipping = GetDecimal(data, "total_shipping"),
            TotalTax = GetDecimal(data, "total_tax"),
            TotalRefunded = GetDecimal(data, "total_refunded"),
            TotalTip = GetDecimal(data, "total_tip"),
            
            Confirmed = GetBoolean(data, "confirmed"),
            Test = GetBoolean(data, "test"),
            Closed = GetBoolean(data, "closed"),
            Taxexempt = GetBoolean(data, "taxexempt"),
            TaxesIncluded = GetBoolean(data, "taxesincluded"),
            DutiesIncluded = GetBoolean(data, "dutiesincluded"),
            
            Fulfillable = GetNullableBoolean(data, "fulfillable"),
            RequiresShipping = GetNullableBoolean(data, "requiresshipping"),
            CustomerAcceptsMarketing = GetNullableBoolean(data, "customeracceptsmarketing"),
            BillingAddressMatchesShippingAddress = GetNullableBoolean(data, "billingaddressmatchesshippingaddress"),
            CanMarkAsPaid = GetNullableBoolean(data, "canmarkaspaid"),
            CannotNotifyCustomer = GetNullableBoolean(data, "cannot_notify_customer"),
            
            Note = GetStringWithLog(data, "note", logger),
            SourceName = GetStringWithLog(data, "source_name", logger),
            SourceIdentifier = GetStringWithLog(data, "source_identifier", logger),
            ConfirmationNumber = GetStringWithLog(data, "confirmation_number", logger),
            PoNumber = GetStringWithLog(data, "po_number", logger),
            ClientIp = GetStringWithLog(data, "client_ip", logger),
            CustomerLocale = GetStringWithLog(data, "customer_locale", logger),
            
            Customer_Id = GetString(data, "customer_id"),
            Customer_Email = GetString(data, "customer_email"),
            Customer_Name = GetString(data, "customer_name"),
            
            Billing_Address_1 = GetString(data, "billing_address_1"),
            Billing_Address_2 = GetString(data, "billing_address_2"),
            Billing_City = GetString(data, "billing_city"),
            Billing_Province = GetString(data, "billing_province"),
            Billing_Country = GetString(data, "billing_country"),
            Billing_Zip = GetString(data, "billing_zip"),
            
            Shipping_Address_1 = GetString(data, "shipping_address_1"),
            Shipping_Address_2 = GetString(data, "shipping_address_2"),
            Shipping_City = GetString(data, "shipping_city"),
            Shipping_Province = GetString(data, "shipping_province"),
            Shipping_Country = GetString(data, "shipping_country"),
            Shipping_Zip = GetString(data, "shipping_zip")
        };
    }

    public static string GenerateOrderInsertSqlWithId()
    {
        return @"
            INSERT INTO ""order"" (
                id,
                src_order_id, order_name, order_date, close_date, cancel_date,
                currency, presentment_currency, status, fulfillment_status,
                total_price, subtotal_price, total_discounts, total_shipping, total_tax, total_refunded, total_tip,
                confirmed, test, closed, taxexempt, taxes_included, duties_included,
                fulfillable, requires_shipping, customer_accepts_marketing,
                billing_address_matches_shipping_address, can_mark_as_paid, cannot_notify_customer,
                note, source_name, source_identifier, confirmation_number, po_number, client_ip, customer_locale,
                customer_id, customer_email, customer_name,
                billing_address_1, billing_address_2, billing_city, billing_province, billing_country, billing_zip,
                shipping_address_1, shipping_address_2, shipping_city, shipping_province, shipping_country, shipping_zip,
                created_at, updated_at
            ) VALUES (
                @id,
                @src_order_id, @order_name, @order_date, @close_date, @cancel_date,
                @currency, @presentment_currency, @status, @fulfillment_status,
                @total_price, @subtotal_price, @total_discounts, @total_shipping, @total_tax, @total_refunded, @total_tip,
                @confirmed, @test, @closed, @taxexempt, @taxes_included, @duties_included,
                @fulfillable, @requires_shipping, @customer_accepts_marketing,
                @billing_address_matches_shipping_address, @can_mark_as_paid, @cannot_notify_customer,
                @note, @source_name, @source_identifier, @confirmation_number, @po_number, @client_ip, @customer_locale,
                @customer_id, @customer_email, @customer_name,
                @billing_address_1, @billing_address_2, @billing_city, @billing_province, @billing_country, @billing_zip,
                @shipping_address_1, @shipping_address_2, @shipping_city, @shipping_province, @shipping_country, @shipping_zip,
                @created_at, @updated_at
            )";
    }

    public static void AddOrderParameters(NpgsqlCommand command, Order order)
    {
        command.Parameters.AddWithValue("src_order_id", order.SrcOrderId);
        command.Parameters.AddWithValue("order_name", order.OrderName ?? "");
        command.Parameters.AddWithValue("order_date", order.OrderDate);
        command.Parameters.AddWithValue("close_date", (object?)order.CloseDate ?? DBNull.Value);
        command.Parameters.AddWithValue("cancel_date", (object?)order.CancelDate ?? DBNull.Value);
        command.Parameters.AddWithValue("currency", order.Currency?.Id ?? Currency.USD.Id);
        command.Parameters.AddWithValue("presentment_currency", order.PresentmentCurrency?.Id ?? Currency.USD.Id);
        command.Parameters.AddWithValue("status", order.Status?.Id ?? FinancialStatus.Pending.Id);
        command.Parameters.AddWithValue("fulfillment_status", order.FulfillmentStatus?.Id ?? FulfillmentStatus.Unfulfilled.Id);
        
        command.Parameters.AddWithValue("total_price", order.TotalPrice);
        command.Parameters.AddWithValue("subtotal_price", order.SubtotalPrice);
        command.Parameters.AddWithValue("total_discounts", order.TotalDiscounts);
        command.Parameters.AddWithValue("total_shipping", order.TotalShipping);
        command.Parameters.AddWithValue("total_tax", order.TotalTax);
        command.Parameters.AddWithValue("total_refunded", order.TotalRefunded);
        command.Parameters.AddWithValue("total_tip", order.TotalTip);
        
        command.Parameters.AddWithValue("confirmed", order.Confirmed);
        command.Parameters.AddWithValue("test", order.Test);
        command.Parameters.AddWithValue("closed", order.Closed);
        command.Parameters.AddWithValue("taxexempt", order.Taxexempt);
        command.Parameters.AddWithValue("taxes_included", order.TaxesIncluded);
        command.Parameters.AddWithValue("duties_included", order.DutiesIncluded);
        
        command.Parameters.AddWithValue("fulfillable", (object?)order.Fulfillable ?? DBNull.Value);
        command.Parameters.AddWithValue("requires_shipping", (object?)order.RequiresShipping ?? DBNull.Value);
        command.Parameters.AddWithValue("customer_accepts_marketing", (object?)order.CustomerAcceptsMarketing ?? DBNull.Value);
        command.Parameters.AddWithValue("billing_address_matches_shipping_address", (object?)order.BillingAddressMatchesShippingAddress ?? DBNull.Value);
        command.Parameters.AddWithValue("can_mark_as_paid", (object?)order.CanMarkAsPaid ?? DBNull.Value);
        command.Parameters.AddWithValue("cannot_notify_customer", (object?)order.CannotNotifyCustomer ?? DBNull.Value);
        
        command.Parameters.AddWithValue("note", order.Note ?? "");
        command.Parameters.AddWithValue("source_name", order.SourceName ?? "");
        command.Parameters.AddWithValue("source_identifier", order.SourceIdentifier ?? "");
        command.Parameters.AddWithValue("confirmation_number", order.ConfirmationNumber ?? "");
        command.Parameters.AddWithValue("po_number", order.PoNumber ?? "");
        command.Parameters.AddWithValue("client_ip", order.ClientIp ?? "");
        command.Parameters.AddWithValue("customer_locale", order.CustomerLocale ?? "");
        
        command.Parameters.AddWithValue("customer_id", order.Customer_Id ?? "");
        command.Parameters.AddWithValue("customer_email", order.Customer_Email ?? "");
        command.Parameters.AddWithValue("customer_name", order.Customer_Name ?? "");
        
        command.Parameters.AddWithValue("billing_address_1", order.Billing_Address_1 ?? "");
        command.Parameters.AddWithValue("billing_address_2", order.Billing_Address_2 ?? "");
        command.Parameters.AddWithValue("billing_city", order.Billing_City ?? "");
        command.Parameters.AddWithValue("billing_province", order.Billing_Province ?? "");
        command.Parameters.AddWithValue("billing_country", order.Billing_Country ?? "");
        command.Parameters.AddWithValue("billing_zip", order.Billing_Zip ?? "");
        
        command.Parameters.AddWithValue("shipping_address_1", order.Shipping_Address_1 ?? "");
        command.Parameters.AddWithValue("shipping_address_2", order.Shipping_Address_2 ?? "");
        command.Parameters.AddWithValue("shipping_city", order.Shipping_City ?? "");
        command.Parameters.AddWithValue("shipping_province", order.Shipping_Province ?? "");
        command.Parameters.AddWithValue("shipping_country", order.Shipping_Country ?? "");
        command.Parameters.AddWithValue("shipping_zip", order.Shipping_Zip ?? "");
        
        command.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        command.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
    }

    // Helper methods with improved error handling
    private static string GetString(Dictionary<string, object?> data, string key)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null && value != DBNull.Value)
            {
                return value.ToString() ?? "";
            }
        }
        catch
        {
            // Silently handle errors
        }
        return "";
    }

    private static string GetStringWithLog(Dictionary<string, object?> data, string key, ILogger logger)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null && value != DBNull.Value)
            {
                return value.ToString() ?? "";
            }
        }
        catch
        {
            // Silently handle errors
        }
        return "";
    }

    private static long GetLong(Dictionary<string, object?> data, string key, ILogger logger)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null && value != DBNull.Value)
            {
                if (value is long longValue) return longValue;
                if (value is int intValue) return intValue;
                if (value is decimal decValue) return (long)decValue;
                if (value is double doubleValue) return (long)doubleValue;
                if (value is float floatValue) return (long)floatValue;
                
                var strValue = value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(strValue))
                {
                    if (long.TryParse(strValue, out var parsedLong))
                        return parsedLong;
                    if (decimal.TryParse(strValue, out var parsedDecimal))
                        return (long)parsedDecimal;
                    if (double.TryParse(strValue, out var parsedDouble))
                        return (long)parsedDouble;
                }
            }
        }
        catch
        {
            // Silently handle errors
        }
        return 0;
    }

    private static int GetInt(Dictionary<string, object?> data, string key, ILogger logger)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null && value != DBNull.Value)
            {
                if (value is int intValue) return intValue;
                if (value is long longValue) return (int)longValue;
                if (value is decimal decValue) return (int)decValue;
                if (value is double doubleValue) return (int)doubleValue;
                if (value is float floatValue) return (int)floatValue;
                
                var strValue = value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(strValue))
                {
                    if (int.TryParse(strValue, out var parsedInt))
                        return parsedInt;
                    if (decimal.TryParse(strValue, out var parsedDecimal))
                        return (int)parsedDecimal;
                    if (double.TryParse(strValue, out var parsedDouble))
                        return (int)parsedDouble;
                }
            }
        }
        catch
        {
            // Silently handle errors
        }
        return 0;
    }

    private static decimal GetDecimal(Dictionary<string, object?> data, string key)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null && value != DBNull.Value)
            {
                if (value is decimal decValue) return decValue;
                if (value is double doubleValue) return (decimal)doubleValue;
                if (value is float floatValue) return (decimal)floatValue;
                if (value is int intValue) return intValue;
                if (value is long longValue) return longValue;
                if (decimal.TryParse(value.ToString()?.Trim(), out var parsed))
                    return parsed;
            }
        }
        catch
        {
            // Silently handle errors
        }
        return 0m;
    }

    private static DateTime? GetDateTime(Dictionary<string, object?> data, string key, ILogger logger)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null && value != DBNull.Value)
            {
                if (value is DateTime dtValue) return dtValue;
                
                var strValue = value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(strValue) && DateTime.TryParse(strValue, out var parsed))
                    return parsed;
            }
        }
        catch
        {
            // Silently handle errors
        }
        return null;
    }

    private static DateTime? GetNullableDateTime(Dictionary<string, object?> data, string key, ILogger logger)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null && value != DBNull.Value)
            {
                if (value is DateTime dtValue) return dtValue;
                
                var strValue = value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(strValue) && DateTime.TryParse(strValue, out var parsed))
                    return parsed;
            }
        }
        catch
        {
            // Silently handle errors
        }
        return null;
    }

    private static bool GetBoolean(Dictionary<string, object?> data, string key)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null && value != DBNull.Value)
            {
                if (value is bool boolValue) return boolValue;
                
                var strValue = value.ToString()?.ToUpper().Trim();
                return strValue switch
                {
                    "TRUE" or "T" or "YES" or "Y" or "1" => true,
                    _ => false
                };
            }
        }
        catch
        {
            // Silently handle errors
        }
        return false;
    }

    private static bool? GetNullableBoolean(Dictionary<string, object?> data, string key)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null && value != DBNull.Value)
            {
                if (value is bool boolValue) return boolValue;
                
                var strValue = value.ToString()?.ToUpper().Trim();
                if (string.IsNullOrEmpty(strValue)) return null;
                
                return strValue switch
                {
                    "TRUE" or "T" or "YES" or "Y" or "1" => true,
                    "FALSE" or "F" or "NO" or "N" or "0" => false,
                    _ => null
                };
            }
        }
        catch
        {
            // Silently handle errors
        }
        return null;
    }

    private static Currency ParseCurrencyFromCode(string? currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode)) return Currency.USD;
        return currencyCode.ToUpper().Trim() switch
        {
            "USD" => Currency.USD,
            "EUR" => Currency.EUR,
            "GBP" => Currency.GBP,
            _ => Currency.USD
        };
    }

    private static FinancialStatus ParseFinancialStatusFromText(string? status)
    {
        if (string.IsNullOrEmpty(status)) return FinancialStatus.Pending;
        return status.ToUpper().Trim() switch
        {
            "PAID" => FinancialStatus.Paid,
            "PENDING" => FinancialStatus.Pending,
            "REFUNDED" => FinancialStatus.Refunded,
            "VOIDED" => FinancialStatus.Voided,
            _ => FinancialStatus.Pending
        };
    }

    private static FulfillmentStatus ParseFulfillmentStatusFromText(string? status)
    {
        if (string.IsNullOrEmpty(status)) return FulfillmentStatus.Unfulfilled;
        return status.ToUpper().Trim() switch
        {
            "FULFILLED" => FulfillmentStatus.Fulfilled,
            "UNFULFILLED" => FulfillmentStatus.Unfulfilled,
            "PARTIAL" or "PARTIALLY_FULFILLED" or "PARTIALLY FULFILLED" => FulfillmentStatus.PartiallyFulfilled,
            "IN_PROGRESS" or "IN PROGRESS" => FulfillmentStatus.InProgress,
            _ => FulfillmentStatus.Unfulfilled
        };
    }
}

