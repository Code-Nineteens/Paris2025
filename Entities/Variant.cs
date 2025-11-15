using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using Paris2025.Enumerations;

namespace Paris2025.Entities;
[CannotCreate, CannotDelete, CannotUpdate]
public class Variant() : Entity
{
    public long SrcProductId { get; set; }
    public string Title { get; set; }
    public string Vendor { get; set; }
    public string ProductType { get; set; }
    public ProductStatus Status { get; set; }
    public int TotalInventory { get; set; }
    public decimal PriceMin { get; set; }
    public decimal PriceMax { get; set; }
    public decimal PriceCurrent { get; set; }
    public string Description { get; set; }
    public string DescriptionHtml { get; set; }
    public string SeoTitle { get; set; }
    public string SeoDescription { get; set; }
    public bool HasOnlyDefaultVariant { get; set; }
    public bool HasOutOfStockVariants { get; set; }
    public bool IsGiftCard { get; set; }
    public bool RequiresSellingPlan {get; set;}
    public string CategoryId { get; set; }
    public string CategoryName { get; set; }
    public string CategoryFullName { get; set; }
    
    [OneToOne<VariantProduct>(nameof(VariantProduct.Variant))]
    public VariantProduct Product { get; set; }

    [ManyToOne<OrderProduct>(nameof(OrderProduct.Variant))]
    public List<OrderProduct> OrderProducts { get; set; }
    
}