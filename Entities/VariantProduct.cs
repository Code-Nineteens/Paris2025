using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace Paris2025.Entities;
//variant line item
[CannotCreate, CannotDelete, CannotUpdate]
public class VariantProduct() : Entity
{
    [OneToMany<Product>(nameof(Product.Variants))]
    public Product Product { get; set; }
    
    [OneToOne<Variant>(nameof(Variant.Product))]
    public Variant Variant { get; set; }
}