using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using Paris2025.Enumerations;

namespace Paris2025.Entities;
[CannotCreate, CannotDelete, CannotUpdate]
public class Order() : Entity
{
    public long SrcOrderId { get; set; }
    public string OrderName { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public DateTime? CancelDate { get; set; }
    public Currency Currency { get; set; }
    public Currency PresentmentCurrency { get; set; }
    public FinancialStatus Status { get; set; }
    public FulfillmentStatus FulfillmentStatus { get; set; }
    
    public decimal TotalPrice { get; set; }
    public decimal SubtotalPrice { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalShipping { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal TotalTip { get; set; }
    
    public bool Confirmed { get; set; }
    public bool Test { get; set; }
    public bool Closed { get; set; }
    public bool Taxexempt { get; set; }
    public bool TaxesIncluded { get; set; }
    public bool DutiesIncluded { get; set; }

    public bool? Fulfillable { get; set; }
    public bool? RequiresShipping { get; set; }
    public bool? CustomerAcceptsMarketing { get; set; }
    public bool? BillingAddressMatchesShippingAddress { get; set; }
    public bool? CanMarkAsPaid { get; set; }
    public bool? CannotNotifyCustomer { get; set; }

    public string Note { get; set; }
    public string SourceName { get; set; }
    public string SourceIdentifier { get; set; }
    public string ConfirmationNumber { get; set; }
    public string PoNumber { get; set; }
    public string ClientIp { get; set; }
    public string CustomerLocale { get; set; }

    public string Customer_Id { get; set; }
    public string Customer_Email { get; set; }
    public string Customer_Name { get; set; }

    public string Billing_Address_1 { get; set; }
    public string Billing_Address_2 { get; set; }
    public string Billing_City { get; set; }
    public string Billing_Province { get; set; }
    public string Billing_Country { get; set; }
    public string Billing_Zip { get; set; }

    public string Shipping_Address_1 { get; set; }
    public string Shipping_Address_2 { get; set; }
    public string Shipping_City { get; set; }
    public string Shipping_Province { get; set; }
    public string Shipping_Country { get; set; }
    public string Shipping_Zip { get; set; }
    
    [ManyToOne<OrderProduct>(nameof(OrderProduct.Order))]
    public List<OrderProduct> OrderProducts { get; set; }
}