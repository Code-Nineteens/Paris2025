using EntityArchitect.CRUD.Enumerations;

namespace Paris2025.Enumerations;

public class ProductStatus : Enumeration
{
    public ProductStatus(int id) : base(id)
    {
    }

    public ProductStatus(int id, string name) : base(id, name)
    {
    }
    
    public static ProductStatus Active = new ProductStatus(1, "ACTIVE");
    public static ProductStatus Draft = new ProductStatus(2, "DRAFT");
    public static ProductStatus Archived = new ProductStatus(3, "ARCHIVED");
}