using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using Paris2025.Enumerations;

namespace Paris2025.Entities;
//order line item
[CannotCreate, CannotDelete, CannotUpdate]
public class OrderProduct() : Entity
{
    [OneToMany<Product>(nameof(Product.OrderProducts))]
    public Product? Product { get; set; }
    
    [OneToMany<Order>(nameof(Order.OrderProducts))]
    public Order Order { get; set; }

    [OneToMany<Variant>(nameof(Variant.OrderProducts))]
    public Variant? Variant { get; set; }

    public int Quantity {get;set;}
    public decimal OrginalUnitPrice {get;set;}
    public decimal DiscountedUnitPrice {get;set;}
    public decimal DiscountedPrice {get;set;}
    public Currency Currency {get;set;}

    public bool RequiresShipping {get;set;}
    public bool Taxable {get;set;}
}