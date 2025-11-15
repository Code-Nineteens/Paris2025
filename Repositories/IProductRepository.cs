using Paris2025.Entities;

namespace Paris2025.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product> SaveAsync(Product product);
    Task<List<Product>> SaveRangeAsync(List<Product> products, int batchSize = 100);
    Task<int> DeleteAllAsync();
}

