using System.Linq;
using Npgsql;
using Paris2025.Entities;
using Paris2025.Services;

namespace Paris2025.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(IConfiguration configuration, ILogger<ProductRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        _logger.LogInformation("Starting to fetch all products from old_products table");
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var products = new List<Product>();
        // Read from old_products table (source data)
        await using var command = new NpgsqlCommand("SELECT * FROM old_products", connection);
        await using var reader = await command.ExecuteReaderAsync();

        var count = 0;
        while (await reader.ReadAsync())
        {
            var product = ProductMapper.MapFromReader(reader);
            products.Add(product);
            count++;
            
            if (count % 1000 == 0)
            {
                _logger.LogInformation("Loaded {Count} products so far...", count);
            }
        }

        _logger.LogInformation("Successfully loaded {TotalCount} products from old_products table", products.Count);
        return products;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching product by ID: {ProductId}", id);
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Query new product table by Guid id
        await using var command = new NpgsqlCommand("SELECT * FROM product WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        
        await using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            var product = ProductMapper.MapFromReader(reader);
            _logger.LogInformation("Successfully found product with ID: {ProductId}", id);
            return product;
        }

        _logger.LogWarning("Product with ID {ProductId} not found", id);
        return null;
    }

    public async Task<Product> SaveAsync(Product product)
    {
        _logger.LogInformation("Saving single product: {ProductTitle} (SrcId: {SrcProductId})", 
            product.Title, product.SrcProductId);
        
        // Validate product before saving
        ProductMapper.ValidateProduct(product, _logger);
        
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = ProductMapper.GenerateInsertSql(product);
            _logger.LogDebug("SQL: {Sql}", sql);
            
            await using var command = new NpgsqlCommand(sql, connection);
            ProductMapper.AddInsertParameters(command, product);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation("Successfully saved product: {ProductTitle} (SrcId: {SrcProductId}). Rows affected: {RowsAffected}", 
                    product.Title, product.SrcProductId, rowsAffected);
            }
            else
            {
                _logger.LogWarning("No rows affected when saving product: {ProductTitle} (SrcId: {SrcProductId})", 
                    product.Title, product.SrcProductId);
            }
            
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving product: {ProductTitle} (SrcId: {SrcProductId}). Error: {ErrorMessage}", 
                product.Title, product.SrcProductId, ex.Message);
            throw;
        }
    }

    public async Task<List<Product>> SaveRangeAsync(List<Product> products, int batchSize = 100)
    {
        if (products == null || products.Count == 0)
        {
            _logger.LogWarning("SaveRangeAsync called with empty or null product list");
            return products ?? new List<Product>();
        }

        _logger.LogInformation("Starting to save {TotalCount} products in batches of {BatchSize}", 
            products.Count, batchSize);

        var totalSaved = 0;
        var batchNumber = 0;

        for (int i = 0; i < products.Count; i += batchSize)
        {
            batchNumber++;
            var batch = products.Skip(i).Take(batchSize).ToList();
            var batchStartIndex = i + 1;
            var batchEndIndex = Math.Min(i + batchSize, products.Count);

            _logger.LogInformation("Processing batch {BatchNumber}: products {StartIndex} to {EndIndex} of {TotalCount}", 
                batchNumber, batchStartIndex, batchEndIndex, products.Count);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var sql = ProductMapper.GenerateInsertSql(batch[0]);
                _logger.LogDebug("Batch {BatchNumber} SQL: {Sql}", batchNumber, sql);
                
                var batchRowsAffected = 0;
                foreach (var product in batch)
                {
                    try
                    {
                        await using var command = new NpgsqlCommand(sql, connection, transaction);
                        ProductMapper.AddInsertParameters(command, product);
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        batchRowsAffected += rowsAffected;
                        
                        if (rowsAffected == 0)
                        {
                            _logger.LogWarning("No rows affected for product: {ProductTitle} (SrcId: {SrcProductId}) in batch {BatchNumber}", 
                                product.Title, product.SrcProductId, batchNumber);
                        }
                    }
                    catch (Exception productEx)
                    {
                        _logger.LogError(productEx, "Error saving individual product in batch {BatchNumber}: {ProductTitle} (SrcId: {SrcProductId}). Error: {ErrorMessage}", 
                            batchNumber, product.Title, product.SrcProductId, productEx.Message);
                        throw;
                    }
                }

                await transaction.CommitAsync();
                totalSaved += batch.Count;
                
                _logger.LogInformation("Successfully saved batch {BatchNumber}: {BatchCount} products, {RowsAffected} rows affected (Total saved: {TotalSaved}/{TotalCount})", 
                    batchNumber, batch.Count, batchRowsAffected, totalSaved, products.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving batch {BatchNumber}: {ErrorMessage}. Stack trace: {StackTrace}. Rolling back transaction.", 
                    batchNumber, ex.Message, ex.StackTrace);
                throw;
            }
        }

        _logger.LogInformation("Completed saving all products. Total saved: {TotalSaved} of {TotalCount}", 
            totalSaved, products.Count);
        
        return products;
    }

    public async Task<int> DeleteAllAsync()
    {
        _logger.LogWarning("Deleting all products from product table");
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Delete from new product table (snake_case, no 's')
        await using var command = new NpgsqlCommand("DELETE FROM product", connection);
        var deletedCount = await command.ExecuteNonQueryAsync();
        
        _logger.LogInformation("Deleted {DeletedCount} products from product table", deletedCount);
        
        return deletedCount;
    }
}

