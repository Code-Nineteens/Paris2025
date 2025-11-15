using System.Linq;
using System.Reflection;
using Npgsql;
using Paris2025.Entities;
using Paris2025.Enumerations;

namespace Paris2025.Services;

public class ProductMapper
{
    /// <summary>
    /// Maps database row (columns c1-c34) to Product entity
    /// Column order: id,legacyResourceId,handle,title,vendor,productType,status,createdAt,updatedAt,publishedAt,templateSuffix,totalInventory,tracksInventory,onlineStoreUrl,onlineStorePreviewUrl,price_min,price_max,currency,featured_image_id,featured_image_url,featured_image_alt,featured_image_width,featured_image_height,description,descriptionHtml,seo_title,seo_description,hasOnlyDefaultVariant,hasOutOfStockVariants,isGiftCard,requiresSellingPlan,category_id,category_name,category_full_name
    /// </summary>
    public static Product MapFromReader(NpgsqlDataReader reader)
    {
        var product = new Product();

        // Map Entity Id (Guid) - check if 'id' column exists (new table) or use c1 (old table)
        var idIndex = GetColumnIndex(reader, "id");
        if (idIndex >= 0 && !reader.IsDBNull(idIndex))
        {
            // New table: id is Guid
            var idProperty = typeof(Product).BaseType?.GetProperty("Id") 
                            ?? typeof(Product).GetProperty("Id");
            if (idProperty != null && idProperty.CanWrite)
            {
                var idValue = reader.GetValue(idIndex);
                if (idValue is Guid guidValue)
                {
                    idProperty.SetValue(product, guidValue);
                }
                else if (idValue is string guidString && Guid.TryParse(guidString, out var parsedGuid))
                {
                    idProperty.SetValue(product, parsedGuid);
                }
            }
        }

        // c1 = id (old table) - map to SrcProductId (long)
        if (!reader.IsDBNull(0))
        {
            var columnName = reader.GetName(0);
            // If it's the old table with c1, parse as long for SrcProductId
            if (columnName == "c1")
            {
                var c1Value = reader.GetString(0);
                if (long.TryParse(c1Value, out var srcId))
                {
                    product.SrcProductId = srcId;
                }
            }
        }

        // c4 = title
        if (!reader.IsDBNull(3))
        {
            product.Title = reader.GetString(3);
        }

        // c5 = vendor
        if (!reader.IsDBNull(4))
        {
            product.Vendor = reader.GetString(4);
        }

        // c6 = productType
        if (!reader.IsDBNull(5))
        {
            product.ProductType = reader.GetString(5);
        }

        // c7 = status (enum conversion from uppercase string)
        if (!reader.IsDBNull(6))
        {
            var statusStr = reader.GetString(6);
            product.Status = ParseProductStatus(statusStr);
        }

        // c12 = totalInventory
        if (!reader.IsDBNull(11))
        {
            if (int.TryParse(reader.GetString(11), out var totalInventory))
            {
                product.TotalInventory = totalInventory;
            }
        }

        // c16 = price_min
        if (!reader.IsDBNull(15))
        {
            if (decimal.TryParse(reader.GetString(15), out var priceMin))
            {
                product.PriceMin = priceMin;
            }
        }

        // c17 = price_max
        if (!reader.IsDBNull(16))
        {
            if (decimal.TryParse(reader.GetString(16), out var priceMax))
            {
                product.PriceMax = priceMax;
            }
        }

        // c24 = description
        if (!reader.IsDBNull(23))
        {
            product.Description = reader.GetString(23);
        }

        // c25 = descriptionHtml
        if (!reader.IsDBNull(24))
        {
            product.DescriptionHtml = reader.GetString(24);
        }

        // c26 = seo_title
        if (!reader.IsDBNull(25))
        {
            product.SeoTitle = reader.GetString(25);
        }

        // c27 = seo_description
        if (!reader.IsDBNull(26))
        {
            product.SeoDescription = reader.GetString(26);
        }

        // c28 = hasOnlyDefaultVariant
        if (!reader.IsDBNull(27))
        {
            var value = reader.GetString(27);
            product.HasOnlyDefaultVariant = ParseBoolean(value);
        }

        // c29 = hasOutOfStockVariants
        if (!reader.IsDBNull(28))
        {
            var value = reader.GetString(28);
            product.HasOutOfStockVariants = ParseBoolean(value);
        }

        // c30 = isGiftCard
        if (!reader.IsDBNull(29))
        {
            var value = reader.GetString(29);
            product.IsGiftCard = ParseBoolean(value);
        }

        // c31 = requiresSellingPlan
        if (!reader.IsDBNull(30))
        {
            var value = reader.GetString(30);
            product.RequiresSellingPlan = ParseBoolean(value);
        }

        // c32 = category_id
        if (!reader.IsDBNull(31))
        {
            product.CategoryId = reader.GetString(31);
        }

        // c33 = category_name
        if (!reader.IsDBNull(32))
        {
            product.CategoryName = reader.GetString(32);
        }

        // c34 = category_full_name
        if (!reader.IsDBNull(33))
        {
            product.CategoryFullName = reader.GetString(33);
        }

        return product;
    }

    /// <summary>
    /// Maps Product entity to database row using SQL INSERT
    /// New entities use snake_case table name without 's': Product -> product
    /// Note: id is auto-generated by EntityArchitect, so it's not included
    /// </summary>
    public static string GenerateInsertSql(Product product)
    {
        return @"INSERT INTO product (src_product_id, title, vendor, product_type, status, total_inventory, price_min, price_max, price_current, description, description_html, seo_title, seo_description, has_only_default_variant, has_out_of_stock_variants, is_gift_card, requires_selling_plan, category_id, category_name, category_full_name, created_at, updated_at)
                 VALUES (@src_product_id, @title, @vendor, @product_type, @status, @total_inventory, @price_min, @price_max, @price_current, @description, @description_html, @seo_title, @seo_description, @has_only_default_variant, @has_out_of_stock_variants, @is_gift_card, @requires_selling_plan, @category_id, @category_name, @category_full_name, @created_at, @updated_at)";
    }

    public static void AddInsertParameters(NpgsqlCommand command, Product product)
    {
        var now = DateTime.UtcNow;
        
        // id is auto-generated, so we don't include it
        command.Parameters.AddWithValue("src_product_id", product.SrcProductId);
        
        // Required fields - use empty string if null to avoid NOT NULL constraint violations
        command.Parameters.AddWithValue("title", (object?)product.Title ?? string.Empty);
        command.Parameters.AddWithValue("vendor", (object?)product.Vendor ?? string.Empty);
        command.Parameters.AddWithValue("product_type", (object?)product.ProductType ?? string.Empty);
        
        // Status is stored as integer (enum ID), not string
        var statusId = product.Status?.Id ?? ProductStatus.Draft.Id;
        command.Parameters.AddWithValue("status", statusId);
        
        command.Parameters.AddWithValue("total_inventory", product.TotalInventory);
        command.Parameters.AddWithValue("price_min", product.PriceMin);
        command.Parameters.AddWithValue("price_max", product.PriceMax);
        command.Parameters.AddWithValue("price_current", product.PriceCurrent);
        
        // Required text fields - use empty string if null
        command.Parameters.AddWithValue("description", (object?)product.Description ?? string.Empty);
        command.Parameters.AddWithValue("description_html", (object?)product.DescriptionHtml ?? string.Empty);
        command.Parameters.AddWithValue("seo_title", (object?)product.SeoTitle ?? string.Empty);
        command.Parameters.AddWithValue("seo_description", (object?)product.SeoDescription ?? string.Empty);
        
        // Boolean values as actual booleans
        command.Parameters.AddWithValue("has_only_default_variant", product.HasOnlyDefaultVariant);
        command.Parameters.AddWithValue("has_out_of_stock_variants", product.HasOutOfStockVariants);
        command.Parameters.AddWithValue("is_gift_card", product.IsGiftCard);
        command.Parameters.AddWithValue("requires_selling_plan", product.RequiresSellingPlan);
        
        // Required category fields - use empty string if null
        command.Parameters.AddWithValue("category_id", (object?)product.CategoryId ?? string.Empty);
        command.Parameters.AddWithValue("category_name", (object?)product.CategoryName ?? string.Empty);
        command.Parameters.AddWithValue("category_full_name", (object?)product.CategoryFullName ?? string.Empty);
        
        // Timestamps
        command.Parameters.AddWithValue("created_at", now);
        command.Parameters.AddWithValue("updated_at", now);
    }
    
    /// <summary>
    /// Validates product before saving to check for potential issues
    /// </summary>
    public static void ValidateProduct(Product product, ILogger? logger = null)
    {
        var issues = new List<string>();
        
        if (string.IsNullOrWhiteSpace(product.Title))
            issues.Add("Title is null or empty");
        if (string.IsNullOrWhiteSpace(product.Vendor))
            issues.Add("Vendor is null or empty");
        if (string.IsNullOrWhiteSpace(product.ProductType))
            issues.Add("ProductType is null or empty");
        if (product.Status == null)
            issues.Add("Status is null");
        
        if (issues.Any() && logger != null)
        {
            logger.LogWarning("Product validation issues for SrcId {SrcProductId}: {Issues}", 
                product.SrcProductId, string.Join(", ", issues));
        }
    }

    private static ProductStatus ParseProductStatus(string statusStr)
    {
        if (string.IsNullOrWhiteSpace(statusStr))
            return ProductStatus.Draft;

        var upperStatus = statusStr.ToUpper().Trim();
        
        return upperStatus switch
        {
            "ACTIVE" => ProductStatus.Active,
            "DRAFT" => ProductStatus.Draft,
            "ARCHIVED" => ProductStatus.Archived,
            _ => ProductStatus.Draft
        };
    }

    private static bool ParseBoolean(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var upperValue = value.ToUpper().Trim();
        return upperValue == "TRUE" || upperValue == "1" || upperValue == "YES";
    }

    private static string? GetEnumerationName(ProductStatus status)
    {
        // Try to get the name using reflection or direct comparison
        if (status == ProductStatus.Active) return "ACTIVE";
        if (status == ProductStatus.Draft) return "DRAFT";
        if (status == ProductStatus.Archived) return "ARCHIVED";
        
        // Fallback: try to use reflection to get Name property if it exists
        var nameProperty = status.GetType().GetProperty("Name");
        if (nameProperty != null)
        {
            return nameProperty.GetValue(status)?.ToString();
        }
        
        return null;
    }

    private static int GetColumnIndex(NpgsqlDataReader reader, string columnName)
    {
        try
        {
            return reader.GetOrdinal(columnName);
        }
        catch
        {
            return -1;
        }
    }
}

