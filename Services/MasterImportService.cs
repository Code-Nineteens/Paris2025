using Npgsql;
using Paris2025.Entities;
using Paris2025.Enumerations;

namespace Paris2025.Services;

/// <summary>
/// Master import service that handles all data migration from old tables to new tables
/// Filters by Shopify GID type (Product vs ProductVariant) and routes to appropriate tables
/// </summary>
public class MasterImportService
{
    private readonly string _connectionString;
    private readonly ILogger<MasterImportService> _logger;

    public MasterImportService(IConfiguration configuration, ILogger<MasterImportService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    /// <summary>
    /// Import all records from old tables to new tables
    /// Products (gid://shopify/Product/*) -> product table
    /// ProductVariants (gid://shopify/ProductVariant/*) -> variant table
    /// </summary>
    public async Task<ImportSummary> ImportAllDataAsync(int batchSize = 100)
    {
        var startTime = DateTime.UtcNow;
        var summary = new ImportSummary();

        try
        {
            _logger.LogInformation("=== Starting Master Data Import ===");
            _logger.LogInformation("Batch size: {BatchSize}", batchSize);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Step 1: Read all records from old_products
            _logger.LogInformation("Step 1: Reading all records from old_products table...");
            var allRecords = await ReadOldProductsAsync(connection);
            summary.TotalRead = allRecords.Count;
            _logger.LogInformation("Read {Count} total records from old_products", allRecords.Count);

            // Step 2: Parse and categorize by GID type
            _logger.LogInformation("Step 2: Categorizing records by Shopify GID type...");
            var productRecords = new List<(string id, Dictionary<string, object?> data)>();
            var variantRecords = new List<(string id, Dictionary<string, object?> data)>();

            foreach (var record in allRecords)
            {
                var gidType = ParseShopifyGidType(record.id);

                if (gidType == "Product")
                {
                    productRecords.Add(record);
                }
                else if (gidType == "ProductVariant")
                {
                    variantRecords.Add(record);
                }
                else if (gidType == "None")
                {
                    // Skip records with "None" as ID - these are intentionally empty
                    summary.Skipped++;
                }
                else
                {
                    // Unknown or invalid type - add to problematic records
                    summary.Skipped++;
                    summary.ProblematicRecords.Add(new ProblemRecord
                    {
                        Id = record.id,
                        Reason = $"Unknown or invalid GID type: {gidType}",
                        Title = GetString(record.data, "c4"),
                        Vendor = GetString(record.data, "c5"),
                        SrcProductId = GetLong(record.data, "c2")
                    });
                }
            }

            _logger.LogInformation("Categorization complete:");
            _logger.LogInformation("  - Products (gid://shopify/Product/*): {Count}", productRecords.Count);
            _logger.LogInformation("  - Variants (gid://shopify/ProductVariant/*): {Count}", variantRecords.Count);
            _logger.LogInformation("  - Skipped (invalid GID): {Count}", summary.Skipped);

            // Step 3: Import Products to product table
            if (productRecords.Count > 0)
            {
                _logger.LogInformation("Step 3: Importing {Count} Product records to product table...", productRecords.Count);
                var (savedProducts, productIdMap) = await ImportToProductTableAsync(connection, productRecords, batchSize);
                summary.ProductsSaved = savedProducts;
                summary.ProductIdMap = productIdMap;
                _logger.LogInformation("  - Products Saved: {Count} (with {Mappings} GUID mappings)", savedProducts, productIdMap.Count);
            }

            // Step 4: Import ProductVariants to variant table
            if (variantRecords.Count > 0)
            {
                _logger.LogInformation("Step 4: Importing {Count} ProductVariant records to variant table...", variantRecords.Count);
                var (savedVariants, variantIdMap) = await ImportToVariantTableAsync(connection, variantRecords, batchSize);
                summary.VariantsSaved = savedVariants;
                summary.VariantIdMap = variantIdMap;
                _logger.LogInformation("  - Variants Saved: {Count} (with {Mappings} GUID mappings)", savedVariants, variantIdMap.Count);
            }

            summary.Success = true;
            summary.Duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("=== Import Complete ===");
            _logger.LogInformation("  - Total Read: {Total}", summary.TotalRead);
            _logger.LogInformation("  - Products Saved: {Products}", summary.ProductsSaved);
            _logger.LogInformation("  - Variants Saved: {Variants}", summary.VariantsSaved);
            _logger.LogInformation("  - Skipped: {Skipped}", summary.Skipped);
            _logger.LogInformation("  - Duration: {Duration}ms", summary.Duration.TotalMilliseconds);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed: {Error}", ex.Message);
            summary.Success = false;
            summary.ErrorMessage = ex.Message;
            summary.Duration = DateTime.UtcNow - startTime;
            return summary;
        }
    }

    /// <summary>
    /// Import orders from old_orders to order table
    /// </summary>
    public async Task<(bool success, int savedCount, Dictionary<long, Guid> idMap, string? error)> ImportOrdersAsync(int batchSize = 100)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("=== Starting Order Import ===");
            _logger.LogInformation("Batch size: {BatchSize}", batchSize);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Step 1: Read all records from old_orders
            _logger.LogInformation("Step 1: Reading all records from old_orders table...");
            var orders = await ReadOldOrdersAsync(connection);
            _logger.LogInformation("Read {Count} orders from old_orders", orders.Count);

            if (orders.Count == 0)
            {
                _logger.LogWarning("No orders found in old_orders table");
                return (true, 0, new Dictionary<long, Guid>(), null);
            }

            // Import orders
            _logger.LogInformation("Step 2: Importing {Count} orders to order table...", orders.Count);
            var (saved, idMap) = await ImportToOrderTableAsync(connection, orders, batchSize);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("=== Order Import Complete ===");
            _logger.LogInformation("  - Orders Saved: {Count} (with {Mappings} GUID mappings)", saved, idMap.Count);
            _logger.LogInformation("  - Duration: {Duration}ms", duration.TotalMilliseconds);

            return (true, saved, idMap, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order import failed: {Error}", ex.Message);
            return (false, 0, new Dictionary<long, Guid>(), ex.Message);
        }
    }

    /// <summary>
    /// Import variant-product relations from inventory_variants table
    /// </summary>
    public async Task<(bool success, int savedCount, int skipped, string? error)> ImportVariantProductRelationsAsync(
        Dictionary<long, Guid> productIdMap,
        Dictionary<long, Guid> variantIdMap,
        int batchSize = 10000)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("=== Starting Variant-Product Relations Import ===");
            _logger.LogInformation("Product ID Map size: {Size}", productIdMap.Count);
            _logger.LogInformation("Variant ID Map size: {Size}", variantIdMap.Count);
            _logger.LogInformation("Batch size: {BatchSize}", batchSize);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Read relations from inventory_variants
            _logger.LogInformation("Step 1: Reading relations from inventory_variants table...");
            var relations = await ReadInventoryVariantsAsync(connection);
            _logger.LogInformation("Read {Count} relations from inventory_variants", relations.Count);

            if (relations.Count == 0)
            {
                _logger.LogWarning("No relations found in inventory_variants table");
                return (true, 0, 0, null);
            }

            // Import relations
            _logger.LogInformation("Step 2: Importing {Count} relations to variant_product table...", relations.Count);
            var (saved, skipped) = await ImportToVariantProductTableAsync(connection, relations, productIdMap, variantIdMap, batchSize);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("=== Variant-Product Relations Import Complete ===");
            _logger.LogInformation("  - Relations Saved: {Count}", saved);
            _logger.LogInformation("  - Skipped: {Count}", skipped);
            _logger.LogInformation("  - Duration: {Duration}ms", duration.TotalMilliseconds);

            return (true, saved, skipped, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Variant-Product relations import failed: {Error}", ex.Message);
            return (false, 0, 0, ex.Message);
        }
    }

    /// <summary>
    /// Import order-product relations from order_line_items table
    /// </summary>
    public async Task<(bool success, int savedCount, int skipped, string? error)> ImportOrderProductRelationsAsync(
        Dictionary<long, Guid> orderIdMap,
        Dictionary<long, Guid> productIdMap,
        Dictionary<long, Guid> variantIdMap,
        int batchSize = 10000)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("=== Starting Order-Product Relations Import ===");
            _logger.LogInformation("Order ID Map size: {Size}", orderIdMap.Count);
            _logger.LogInformation("Product ID Map size: {Size}", productIdMap.Count);
            _logger.LogInformation("Batch size: {BatchSize}", batchSize);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Read relations from order_line_items
            _logger.LogInformation("Step 1: Reading relations from order_line_items table...");
            var lineItems = await ReadOrderLineItemsAsync(connection);
            _logger.LogInformation("Read {Count} line items from order_line_items", lineItems.Count);

            if (lineItems.Count == 0)
            {
                _logger.LogWarning("No line items found in order_line_items table");
                return (true, 0, 0, null);
            }

            // Import relations
            _logger.LogInformation("Step 2: Importing {Count} line items to order_product table...", lineItems.Count);
            var (saved, skipped) = await ImportToOrderProductTableAsync(connection, lineItems, orderIdMap, productIdMap, variantIdMap, batchSize);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("=== Order-Product Relations Import Complete ===");
            _logger.LogInformation("  - Relations Saved: {Count}", saved);
            _logger.LogInformation("  - Skipped: {Count}", skipped);
            _logger.LogInformation("  - Duration: {Duration}ms", duration.TotalMilliseconds);

            return (true, saved, skipped, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order-Product relations import failed: {Error}", ex.Message);
            return (false, 0, 0, ex.Message);
        }
    }

    /// <summary>
    /// Updates product inventory fields from inventory_products table
    /// Joins product -> inventory_variants -> inventory_products
    /// Only fills null/empty fields
    /// </summary>
    public async Task<(bool success, int updatedCount, string? error)> UpdateProductInventoryFieldsAsync()
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("=== Starting Product Inventory Fields Update ===");

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                UPDATE product p
                SET 
                    inventory_title = COALESCE(NULLIF(p.inventory_title, ''), ip.inventory_title),
                    inventory_vendor = COALESCE(NULLIF(p.inventory_vendor, ''), ip.inventory_vendor),
                    inventory_product_type = COALESCE(NULLIF(p.inventory_product_type, ''), ip.inventory_producttype),
                    inventory_status = COALESCE(p.inventory_status, 
                        CASE 
                            WHEN UPPER(ip.inventory_status) = 'ACTIVE' THEN 1
                            WHEN UPPER(ip.inventory_status) = 'DRAFT' THEN 2
                            WHEN UPPER(ip.inventory_status) = 'ARCHIVED' THEN 3
                            ELSE 2
                        END),
                    inventory_total_inventory = COALESCE(p.inventory_total_inventory, ip.inventory_totalinventory),
                    updated_at = @updatedAt
                FROM inventory_variants iv
                INNER JOIN inventory_products ip ON iv.inventory_product_id = ip.inventory_id
                WHERE p.src_product_id = CAST(SPLIT_PART(iv.inventory_product_id, '/', 5) AS BIGINT)
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("=== Product Inventory Fields Update Complete ===");
            _logger.LogInformation("  - Products Updated: {Count}", rowsAffected);
            _logger.LogInformation("  - Duration: {Duration}ms", duration.TotalMilliseconds);

            return (true, rowsAffected, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product inventory fields update failed: {Error}", ex.Message);
            return (false, 0, ex.Message);
        }
    }

    /// <summary>
    /// Updates variant inventory fields from inventory_products table
    /// Joins variant -> inventory_variants -> inventory_products
    /// Only fills null/empty fields
    /// </summary>
    public async Task<(bool success, int updatedCount, string? error)> UpdateVariantInventoryFieldsAsync()
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("=== Starting Variant Inventory Fields Update ===");

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                UPDATE variant v
                SET 
                    inventory_title = COALESCE(NULLIF(v.inventory_title, ''), ip.inventory_title),
                    inventory_vendor = COALESCE(NULLIF(v.inventory_vendor, ''), ip.inventory_vendor),
                    inventory_product_type = COALESCE(NULLIF(v.inventory_product_type, ''), ip.inventory_producttype),
                    inventory_status = COALESCE(v.inventory_status, 
                        CASE 
                            WHEN UPPER(ip.inventory_status) = 'ACTIVE' THEN 1
                            WHEN UPPER(ip.inventory_status) = 'DRAFT' THEN 2
                            WHEN UPPER(ip.inventory_status) = 'ARCHIVED' THEN 3
                            ELSE 2
                        END),
                    inventory_total_inventory = COALESCE(v.inventory_total_inventory, ip.inventory_totalinventory),
                    updated_at = @updatedAt
                FROM inventory_variants iv
                INNER JOIN inventory_products ip ON iv.inventory_product_id = ip.inventory_id
                WHERE v.src_product_id = CAST(SPLIT_PART(iv.inventory_variant_id, '/', 5) AS BIGINT)
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("=== Variant Inventory Fields Update Complete ===");
            _logger.LogInformation("  - Variants Updated: {Count}", rowsAffected);
            _logger.LogInformation("  - Duration: {Duration}ms", duration.TotalMilliseconds);

            return (true, rowsAffected, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Variant inventory fields update failed: {Error}", ex.Message);
            return (false, 0, ex.Message);
        }
    }

    // ========== PUBLIC HELPER METHODS FOR ID MAPS ==========

    /// <summary>
    /// Reads product ID mappings from database (src_product_id -> id)
    /// </summary>
    public async Task<Dictionary<long, Guid>> GetProductIdMapAsync()
    {
        var idMap = new Dictionary<long, Guid>();
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT src_product_id, id FROM product";
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var srcId = reader.GetInt64(0);
            var guid = reader.GetGuid(1);
            idMap[srcId] = guid;
        }
        
        _logger.LogInformation("Loaded {Count} product ID mappings from database", idMap.Count);
        return idMap;
    }

    /// <summary>
    /// Reads variant ID mappings from database (src_product_id -> id)
    /// </summary>
    public async Task<Dictionary<long, Guid>> GetVariantIdMapAsync()
    {
        var idMap = new Dictionary<long, Guid>();
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT src_product_id, id FROM variant";
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var srcId = reader.GetInt64(0);
            var guid = reader.GetGuid(1);
            idMap[srcId] = guid;
        }
        
        _logger.LogInformation("Loaded {Count} variant ID mappings from database", idMap.Count);
        return idMap;
    }

    /// <summary>
    /// Reads order ID mappings from database (src_order_id -> id)
    /// </summary>
    public async Task<Dictionary<long, Guid>> GetOrderIdMapAsync()
    {
        var idMap = new Dictionary<long, Guid>();
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT src_order_id, id FROM \"order\"";
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var srcId = reader.GetInt64(0);
            var guid = reader.GetGuid(1);
            idMap[srcId] = guid;
        }
        
        _logger.LogInformation("Loaded {Count} order ID mappings from database", idMap.Count);
        return idMap;
    }

    // ========== PRIVATE HELPER METHODS ==========

    private async Task<List<(string id, Dictionary<string, object?> data)>> ReadOldProductsAsync(NpgsqlConnection connection)
    {
        var records = new List<(string id, Dictionary<string, object?> data)>();

        await using var command = new NpgsqlCommand("SELECT * FROM old_products", connection);
        await using var reader = await command.ExecuteReaderAsync();

        var fieldCount = reader.FieldCount;
        var columnNames = new List<string>();
        for (int i = 0; i < fieldCount; i++)
        {
            columnNames.Add(reader.GetName(i));
        }

        int count = 0;
        while (await reader.ReadAsync())
        {
            var data = new Dictionary<string, object?>();
            for (int i = 0; i < fieldCount; i++)
            {
                data[columnNames[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            var id = reader.GetString(GetColumnIndex(reader, "c1"));
            records.Add((id, data));

            count++;
            if (count % 1000 == 0)
            {
                _logger.LogInformation("Read {Count} records so far...", count);
            }
        }

        return records;
    }

    private async Task<List<Dictionary<string, object?>>> ReadOldOrdersAsync(NpgsqlConnection connection)
    {
        var records = new List<Dictionary<string, object?>>();

        await using var command = new NpgsqlCommand("SELECT * FROM old_orders", connection);
        await using var reader = await command.ExecuteReaderAsync();

        var fieldCount = reader.FieldCount;
        var columnNames = new List<string>();
        for (int i = 0; i < fieldCount; i++)
        {
            columnNames.Add(reader.GetName(i));
        }
        
        int count = 0;
        while (await reader.ReadAsync())
        {
            var data = new Dictionary<string, object?>();
            for (int i = 0; i < fieldCount; i++)
            {
                data[columnNames[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            records.Add(data);
            count++;
            
            if (count % 1000 == 0)
            {
                _logger.LogInformation("Read {Count} orders so far...", count);
            }
        }

        return records;
    }

    private async Task<List<(string variantGid, string productGid)>> ReadInventoryVariantsAsync(NpgsqlConnection connection)
    {
        var relations = new List<(string variantGid, string productGid)>();

        await using var command = new NpgsqlCommand("SELECT inventory_variant_id, inventory_product_id FROM inventory_variants", connection);
        await using var reader = await command.ExecuteReaderAsync();

        int count = 0;
        while (await reader.ReadAsync())
        {
            var variantGid = reader.GetString(0);
            var productGid = reader.GetString(1);
            
            relations.Add((variantGid, productGid));
            count++;
            
            if (count % 10000 == 0)
            {
                _logger.LogInformation("Read {Count} relations so far...", count);
            }
        }

        _logger.LogInformation("Finished reading {Count} relations from inventory_variants", count);
        return relations;
    }

    private async Task<List<OrderLineItemData>> ReadOrderLineItemsAsync(NpgsqlConnection connection)
    {
        var lineItems = new List<OrderLineItemData>();

        var sql = @"
            SELECT 
                order_id,
                product_id,
                quantity,
                original_unit_price,
                discounted_unit_price,
                discounted_total,
                currency,
                requiresshipping,
                taxable
            FROM order_line_items
        ";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        int count = 0;
        while (await reader.ReadAsync())
        {
            lineItems.Add(new OrderLineItemData
            {
                OrderGid = GetStringFromReader(reader, 0),
                ProductGid = GetStringFromReader(reader, 1),
                Quantity = GetIntFromReader(reader, 2),
                OriginalUnitPrice = GetDecimalFromReader(reader, 3),
                DiscountedUnitPrice = GetDecimalFromReader(reader, 4),
                DiscountedPrice = GetDecimalFromReader(reader, 5),
                CurrencyCode = GetStringFromReader(reader, 6),
                RequiresShipping = GetBoolFromReader(reader, 7),
                Taxable = GetBoolFromReader(reader, 8)
            });
            
            count++;
            if (count % 10000 == 0)
            {
                _logger.LogInformation("Read {Count} line items so far...", count);
            }
        }

        _logger.LogInformation("Finished reading {Count} line items from order_line_items", count);
        return lineItems;
    }

    private string ParseShopifyGidType(string gid)
    {
        if (string.IsNullOrEmpty(gid))
        {
            return "None";
        }

        // Handle explicit "None" value (common in databases for null values)
        if (gid.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return "None";
        }

        var parts = gid.Split('/');
        if (parts.Length < 4)
        {
            _logger.LogWarning("Invalid GID format (not enough parts): {Gid}", gid);
            return "Unknown";
        }

        if (parts[0] != "gid:" || parts[2] != "shopify")
        {
            _logger.LogWarning("Invalid GID format (wrong protocol or domain): {Gid}", gid);
            return "Unknown";
        }

        var type = parts[3];
        
        if (type != "Product" && type != "ProductVariant")
        {
            _logger.LogWarning("Unknown Shopify type '{Type}' in GID: {Gid}", type, gid);
            return "Unknown";
        }

        return type;
    }

    private long ExtractIdFromGid(string gid)
    {
        try
        {
            var parts = gid.Split('/');
            if (parts.Length > 0)
            {
                var lastPart = parts[^1];
                if (long.TryParse(lastPart, out var id))
                    return id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to extract ID from GID '{Gid}': {Error}", gid, ex.Message);
        }
        return 0;
    }

    private async Task<(int savedCount, Dictionary<long, Guid> idMap)> ImportToProductTableAsync(
        NpgsqlConnection connection,
        List<(string id, Dictionary<string, object?> data)> records,
        int batchSize)
    {
        int totalSaved = 0;
        int batchNumber = 0;
        var idMap = new Dictionary<long, Guid>();

        for (int i = 0; i < records.Count; i += batchSize)
        {
            batchNumber++;
            var batch = records.Skip(i).Take(batchSize).ToList();

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Processing product batch {BatchNumber} ({Start}-{End} of {Total})",
                    batchNumber, i + 1, Math.Min(i + batchSize, records.Count), records.Count);

                foreach (var record in batch)
                {
                    try
                    {
                        var product = MapToProduct(record.data);
                        
                        if (string.IsNullOrEmpty(product.Title))
                        {
                            _logger.LogWarning("Skipping record {Id} - missing Title", record.id);
                            continue;
                        }
                        
                        var newGuid = Guid.NewGuid();
                        
                        var sql = GenerateProductInsertSqlWithId();

                        await using var command = new NpgsqlCommand(sql, connection, transaction);
                        command.Parameters.AddWithValue("id", newGuid);
                        AddProductParameters(command, product);

                        await command.ExecuteNonQueryAsync();
                        
                        idMap[product.SrcProductId] = newGuid;
                        
                        totalSaved++;
                        
                        if (totalSaved % 100 == 0)
                        {
                            _logger.LogInformation("Saved {Count} products...", totalSaved);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save record {Id} to product table: {Error}", record.id, ex.Message);
                        throw;
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Batch {BatchNumber} committed successfully ({Count} records)", batchNumber, batch.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in batch {BatchNumber}, rolling back: {Error}", batchNumber, ex.Message);
                throw;
            }
        }

        _logger.LogInformation("Created ID mapping for {Count} products", idMap.Count);
        return (totalSaved, idMap);
    }

    private async Task<(int savedCount, Dictionary<long, Guid> idMap)> ImportToVariantTableAsync(
        NpgsqlConnection connection,
        List<(string id, Dictionary<string, object?> data)> records,
        int batchSize)
    {
        int totalSaved = 0;
        int batchNumber = 0;
        var idMap = new Dictionary<long, Guid>();

        for (int i = 0; i < records.Count; i += batchSize)
        {
            batchNumber++;
            var batch = records.Skip(i).Take(batchSize).ToList();

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Processing variant batch {BatchNumber} ({Start}-{End} of {Total})",
                    batchNumber, i + 1, Math.Min(i + batchSize, records.Count), records.Count);

                foreach (var record in batch)
                {
                    try
                    {
                        var variant = MapToVariant(record.data);
                        
                        if (string.IsNullOrEmpty(variant.Title))
                        {
                            _logger.LogWarning("Skipping record {Id} - missing Title", record.id);
                            continue;
                        }
                        
                        var newGuid = Guid.NewGuid();
                        
                        var sql = GenerateVariantInsertSqlWithId();

                        await using var command = new NpgsqlCommand(sql, connection, transaction);
                        command.Parameters.AddWithValue("id", newGuid);
                        AddVariantParameters(command, variant);

                        await command.ExecuteNonQueryAsync();
                        
                        idMap[variant.SrcProductId] = newGuid;
                        
                        totalSaved++;
                        
                        if (totalSaved % 100 == 0)
                        {
                            _logger.LogInformation("Saved {Count} variants...", totalSaved);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save record {Id} to variant table: {Error}", record.id, ex.Message);
                        throw;
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Batch {BatchNumber} committed successfully ({Count} records)", batchNumber, batch.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in batch {BatchNumber}, rolling back: {Error}", batchNumber, ex.Message);
                throw;
            }
        }

        _logger.LogInformation("Created ID mapping for {Count} variants", idMap.Count);
        return (totalSaved, idMap);
    }

    private async Task<(int savedCount, Dictionary<long, Guid> idMap)> ImportToOrderTableAsync(
        NpgsqlConnection connection,
        List<Dictionary<string, object?>> records,
        int batchSize)
    {
        int totalSaved = 0;
        int batchNumber = 0;
        var idMap = new Dictionary<long, Guid>();

        for (int i = 0; i < records.Count; i += batchSize)
        {
            batchNumber++;
            var batch = records.Skip(i).Take(batchSize).ToList();

            _logger.LogInformation("Processing order batch {BatchNumber} ({Start}-{End} of {Total})",
                batchNumber, i + 1, Math.Min(i + batchSize, records.Count), records.Count);

            var copyCommand = @"COPY ""order"" (
                id, src_order_id, order_name, order_date, close_date, cancel_date,
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
            ) FROM STDIN (FORMAT BINARY)";

            await using var writer = await connection.BeginBinaryImportAsync(copyCommand);
            
            foreach (var record in batch)
            {
                var order = OrderMapper.MapFromDictionary(record, _logger);
                var newGuid = Guid.NewGuid();
                
                await writer.StartRowAsync();
                await writer.WriteAsync(newGuid, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(order.SrcOrderId, NpgsqlTypes.NpgsqlDbType.Bigint);
                await writer.WriteAsync(order.OrderName ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                await writer.WriteAsync(order.CloseDate.HasValue ? DateTime.SpecifyKind(order.CloseDate.Value, DateTimeKind.Utc) : (DateTime?)null, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                await writer.WriteAsync(order.CancelDate.HasValue ? DateTime.SpecifyKind(order.CancelDate.Value, DateTimeKind.Utc) : (DateTime?)null, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                await writer.WriteAsync(order.Currency.Id, NpgsqlTypes.NpgsqlDbType.Integer);
                await writer.WriteAsync(order.PresentmentCurrency.Id, NpgsqlTypes.NpgsqlDbType.Integer);
                await writer.WriteAsync(order.Status.Id, NpgsqlTypes.NpgsqlDbType.Integer);
                await writer.WriteAsync(order.FulfillmentStatus.Id, NpgsqlTypes.NpgsqlDbType.Integer);
                await writer.WriteAsync(order.TotalPrice, NpgsqlTypes.NpgsqlDbType.Numeric);
                await writer.WriteAsync(order.SubtotalPrice, NpgsqlTypes.NpgsqlDbType.Numeric);
                await writer.WriteAsync(order.TotalDiscounts, NpgsqlTypes.NpgsqlDbType.Numeric);
                await writer.WriteAsync(order.TotalShipping, NpgsqlTypes.NpgsqlDbType.Numeric);
                await writer.WriteAsync(order.TotalTax, NpgsqlTypes.NpgsqlDbType.Numeric);
                await writer.WriteAsync(order.TotalRefunded, NpgsqlTypes.NpgsqlDbType.Numeric);
                await writer.WriteAsync(order.TotalTip, NpgsqlTypes.NpgsqlDbType.Numeric);
                await writer.WriteAsync(order.Confirmed, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.Test, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.Closed, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.Taxexempt, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.TaxesIncluded, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.DutiesIncluded, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.Fulfillable, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.RequiresShipping, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.CustomerAcceptsMarketing, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.BillingAddressMatchesShippingAddress, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.CanMarkAsPaid, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.CannotNotifyCustomer, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(order.Note ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.SourceName ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.SourceIdentifier ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.ConfirmationNumber ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.PoNumber ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.ClientIp ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.CustomerLocale ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Customer_Id ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Customer_Email ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Customer_Name ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Billing_Address_1 ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Billing_Address_2 ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Billing_City ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Billing_Province ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Billing_Country ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Billing_Zip ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Shipping_Address_1 ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Shipping_Address_2 ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Shipping_City ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Shipping_Province ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Shipping_Country ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(order.Shipping_Zip ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(DateTime.UtcNow, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                await writer.WriteAsync(DateTime.UtcNow, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                
                idMap[order.SrcOrderId] = newGuid;
                totalSaved++;
            }
            
            await writer.CompleteAsync();
            _logger.LogInformation("Batch {BatchNumber} completed ({Count} records)", batchNumber, batch.Count);
        }

        _logger.LogInformation("Created ID mapping for {Count} orders", idMap.Count);
        return (totalSaved, idMap);
    }

    private async Task<(int savedCount, int skipped)> ImportToVariantProductTableAsync(
        NpgsqlConnection connection,
        List<(string variantGid, string productGid)> relations,
        Dictionary<long, Guid> productIdMap,
        Dictionary<long, Guid> variantIdMap,
        int batchSize)
    {
        int totalSaved = 0;
        int batchNumber = 0;
        int skipped = 0;

        for (int i = 0; i < relations.Count; i += batchSize)
        {
            batchNumber++;
            var batch = relations.Skip(i).Take(batchSize).ToList();

            _logger.LogInformation("Processing variant-product batch {BatchNumber} ({Start}-{End} of {Total})",
                batchNumber, i + 1, Math.Min(i + batchSize, relations.Count), relations.Count);

            var copyCommand = @"COPY variant_product (
                id, variant_id, product_id, created_at, updated_at
            ) FROM STDIN (FORMAT BINARY)";

            await using var writer = await connection.BeginBinaryImportAsync(copyCommand);
            
            foreach (var (variantGid, productGid) in batch)
            {
                var variantLegacyId = ExtractIdFromGid(variantGid);
                var productLegacyId = ExtractIdFromGid(productGid);
                
                if (variantLegacyId == 0 || productLegacyId == 0)
                {
                    skipped++;
                    continue;
                }
                
                if (!variantIdMap.TryGetValue(variantLegacyId, out var variantNewId))
                {
                    skipped++;
                    continue;
                }
                
                if (!productIdMap.TryGetValue(productLegacyId, out var productNewId))
                {
                    skipped++;
                    continue;
                }
                
                await writer.StartRowAsync();
                await writer.WriteAsync(Guid.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(variantNewId, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(productNewId, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(DateTime.UtcNow, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                await writer.WriteAsync(DateTime.UtcNow, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                
                totalSaved++;
            }
            
            await writer.CompleteAsync();
            _logger.LogInformation("Batch {BatchNumber} completed ({Count} records saved)", batchNumber, totalSaved);
        }

        return (totalSaved, skipped);
    }

    private async Task<(int savedCount, int skipped)> ImportToOrderProductTableAsync(
        NpgsqlConnection connection,
        List<OrderLineItemData> lineItems,
        Dictionary<long, Guid> orderIdMap,
        Dictionary<long, Guid> productIdMap,
        Dictionary<long, Guid> variantIdMap,
        int batchSize)
    {
        int totalSaved = 0;
        int batchNumber = 0;
        int skipped = 0;

        for (int i = 0; i < lineItems.Count; i += batchSize)
        {
            batchNumber++;
            var batch = lineItems.Skip(i).Take(batchSize).ToList();

            _logger.LogInformation("Processing order-product batch {BatchNumber} ({Start}-{End} of {Total})",
                batchNumber, i + 1, Math.Min(i + batchSize, lineItems.Count), lineItems.Count);

            var copyCommand = @"COPY order_product (
                id, order_id, product_id, variant_id, quantity, orginal_unit_price, discounted_unit_price, 
                discounted_price, currency, requires_shipping, taxable, created_at, updated_at
            ) FROM STDIN (FORMAT BINARY)";

            await using var writer = await connection.BeginBinaryImportAsync(copyCommand);
            
            foreach (var item in batch)
            {
                var orderLegacyId = ExtractIdFromGid(item.OrderGid);
                
                // Order is required
                if (orderLegacyId == 0)
                {
                    skipped++;
                    continue;
                }
                
                if (!orderIdMap.TryGetValue(orderLegacyId, out var orderNewId))
                {
                    skipped++;
                    continue;
                }
                
                // Product/Variant is optional - can be null
                Guid? productNewId = null;
                Guid? variantNewId = null;
                
                if (!string.IsNullOrEmpty(item.ProductGid) && !item.ProductGid.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if it's a Product or ProductVariant GID
                    var gidType = ParseShopifyGidType(item.ProductGid);
                    
                    // Skip if it's "None" or "Unknown"
                    if (gidType != "None" && gidType != "Unknown")
                    {
                        var legacyId = ExtractIdFromGid(item.ProductGid);
                        
                        if (legacyId > 0)
                        {
                            if (gidType == "Product" && productIdMap.TryGetValue(legacyId, out var foundProductId))
                            {
                                productNewId = foundProductId;
                            }
                            else if (gidType == "ProductVariant" && variantIdMap.TryGetValue(legacyId, out var foundVariantId))
                            {
                                variantNewId = foundVariantId;
                            }
                        }
                    }
                }
                
                var currency = ParseCurrencyFromCode(item.CurrencyCode);
                
                await writer.StartRowAsync();
                await writer.WriteAsync(Guid.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(orderNewId, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(productNewId, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(variantNewId, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(item.Quantity, NpgsqlTypes.NpgsqlDbType.Integer);
                await writer.WriteAsync(item.OriginalUnitPrice, NpgsqlTypes.NpgsqlDbType.Numeric);
                await writer.WriteAsync(item.DiscountedUnitPrice, NpgsqlTypes.NpgsqlDbType.Numeric);
                await writer.WriteAsync(item.DiscountedPrice, NpgsqlTypes.NpgsqlDbType.Numeric);
                await writer.WriteAsync(currency.Id, NpgsqlTypes.NpgsqlDbType.Integer);
                await writer.WriteAsync(item.RequiresShipping, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(item.Taxable, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(DateTime.UtcNow, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                await writer.WriteAsync(DateTime.UtcNow, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                
                totalSaved++;
            }
            
            await writer.CompleteAsync();
            _logger.LogInformation("Batch {BatchNumber} completed ({Count} records saved)", batchNumber, totalSaved);
        }

        return (totalSaved, skipped);
    }

    // ========== MAPPING METHODS ==========

    private Product MapToProduct(Dictionary<string, object?> data)
    {
        return new Product
        {
            SrcProductId = GetLong(data, "c2"),
            Title = GetString(data, "c4"),
            Vendor = GetString(data, "c5"),
            ProductType = GetString(data, "c6"),
            Status = ParseProductStatus(GetString(data, "c7")),
            TotalInventory = GetInt(data, "c12"),
            PriceMin = GetDecimal(data, "c16"),
            PriceMax = GetDecimal(data, "c17"),
            Description = GetString(data, "c24"),
            DescriptionHtml = GetString(data, "c25"),
            SeoTitle = GetString(data, "c26"),
            SeoDescription = GetString(data, "c27"),
            HasOnlyDefaultVariant = ParseBoolean(GetString(data, "c28")),
            HasOutOfStockVariants = ParseBoolean(GetString(data, "c29")),
            IsGiftCard = ParseBoolean(GetString(data, "c30")),
            RequiresSellingPlan = ParseBoolean(GetString(data, "c31")),
            CategoryId = GetString(data, "c32"),
            CategoryName = GetString(data, "c33"),
            CategoryFullName = GetString(data, "c34")
        };
    }

    private Variant MapToVariant(Dictionary<string, object?> data)
    {
        return new Variant
        {
            SrcProductId = GetLong(data, "c2"),
            Title = GetString(data, "c4"),
            Vendor = GetString(data, "c5"),
            ProductType = GetString(data, "c6"),
            Status = ParseProductStatus(GetString(data, "c7")),
            TotalInventory = GetInt(data, "c12"),
            PriceMin = GetDecimal(data, "c16"),
            PriceMax = GetDecimal(data, "c17"),
            Description = GetString(data, "c24"),
            DescriptionHtml = GetString(data, "c25"),
            SeoTitle = GetString(data, "c26"),
            SeoDescription = GetString(data, "c27"),
            HasOnlyDefaultVariant = ParseBoolean(GetString(data, "c28")),
            HasOutOfStockVariants = ParseBoolean(GetString(data, "c29")),
            IsGiftCard = ParseBoolean(GetString(data, "c30")),
            RequiresSellingPlan = ParseBoolean(GetString(data, "c31")),
            CategoryId = GetString(data, "c32"),
            CategoryName = GetString(data, "c33"),
            CategoryFullName = GetString(data, "c34")
        };
    }

    // ========== SQL GENERATION METHODS ==========

    private string GenerateProductInsertSqlWithId()
    {
        return @"
            INSERT INTO product (
                id,
                src_product_id, title, vendor, product_type, status,
                total_inventory, price_min, price_max, price_current,
                description, description_html, seo_title, seo_description,
                has_only_default_variant, has_out_of_stock_variants,
                is_gift_card, requires_selling_plan,
                category_id, category_name, category_full_name,
                created_at, updated_at
            ) VALUES (
                @id,
                @src_product_id, @title, @vendor, @product_type, @status,
                @total_inventory, @price_min, @price_max, @price_current,
                @description, @description_html, @seo_title, @seo_description,
                @has_only_default_variant, @has_out_of_stock_variants,
                @is_gift_card, @requires_selling_plan,
                @category_id, @category_name, @category_full_name,
                @created_at, @updated_at
            )";
    }

    private string GenerateVariantInsertSqlWithId()
    {
        return @"
            INSERT INTO variant (
                id,
                src_product_id, title, vendor, product_type, status,
                total_inventory, price_min, price_max, price_current,
                description, description_html, seo_title, seo_description,
                has_only_default_variant, has_out_of_stock_variants,
                is_gift_card, requires_selling_plan,
                category_id, category_name, category_full_name,
                created_at, updated_at
            ) VALUES (
                @id,
                @src_product_id, @title, @vendor, @product_type, @status,
                @total_inventory, @price_min, @price_max, @price_current,
                @description, @description_html, @seo_title, @seo_description,
                @has_only_default_variant, @has_out_of_stock_variants,
                @is_gift_card, @requires_selling_plan,
                @category_id, @category_name, @category_full_name,
                @created_at, @updated_at
            )";
    }

    private void AddProductParameters(NpgsqlCommand command, Product product)
    {
        command.Parameters.AddWithValue("src_product_id", product.SrcProductId);
        command.Parameters.AddWithValue("title", product.Title ?? "");
        command.Parameters.AddWithValue("vendor", product.Vendor ?? "");
        command.Parameters.AddWithValue("product_type", product.ProductType ?? "");
        command.Parameters.AddWithValue("status", product.Status?.Id ?? ProductStatus.Draft.Id);
        command.Parameters.AddWithValue("total_inventory", product.TotalInventory);
        command.Parameters.AddWithValue("price_min", product.PriceMin);
        command.Parameters.AddWithValue("price_max", product.PriceMax);
        command.Parameters.AddWithValue("price_current", product.PriceMax);
        command.Parameters.AddWithValue("description", product.Description ?? "");
        command.Parameters.AddWithValue("description_html", product.DescriptionHtml ?? "");
        command.Parameters.AddWithValue("seo_title", product.SeoTitle ?? "");
        command.Parameters.AddWithValue("seo_description", product.SeoDescription ?? "");
        command.Parameters.AddWithValue("has_only_default_variant", product.HasOnlyDefaultVariant);
        command.Parameters.AddWithValue("has_out_of_stock_variants", product.HasOutOfStockVariants);
        command.Parameters.AddWithValue("is_gift_card", product.IsGiftCard);
        command.Parameters.AddWithValue("requires_selling_plan", product.RequiresSellingPlan);
        command.Parameters.AddWithValue("category_id", product.CategoryId ?? "");
        command.Parameters.AddWithValue("category_name", product.CategoryName ?? "");
        command.Parameters.AddWithValue("category_full_name", product.CategoryFullName ?? "");
        command.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        command.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
    }

    private void AddVariantParameters(NpgsqlCommand command, Variant variant)
    {
        command.Parameters.AddWithValue("src_product_id", variant.SrcProductId);
        command.Parameters.AddWithValue("title", variant.Title ?? "");
        command.Parameters.AddWithValue("vendor", variant.Vendor ?? "");
        command.Parameters.AddWithValue("product_type", variant.ProductType ?? "");
        command.Parameters.AddWithValue("status", variant.Status?.Id ?? ProductStatus.Draft.Id);
        command.Parameters.AddWithValue("total_inventory", variant.TotalInventory);
        command.Parameters.AddWithValue("price_min", variant.PriceMin);
        command.Parameters.AddWithValue("price_max", variant.PriceMax);
        command.Parameters.AddWithValue("price_current", variant.PriceMax);
        command.Parameters.AddWithValue("description", variant.Description ?? "");
        command.Parameters.AddWithValue("description_html", variant.DescriptionHtml ?? "");
        command.Parameters.AddWithValue("seo_title", variant.SeoTitle ?? "");
        command.Parameters.AddWithValue("seo_description", variant.SeoDescription ?? "");
        command.Parameters.AddWithValue("has_only_default_variant", variant.HasOnlyDefaultVariant);
        command.Parameters.AddWithValue("has_out_of_stock_variants", variant.HasOutOfStockVariants);
        command.Parameters.AddWithValue("is_gift_card", variant.IsGiftCard);
        command.Parameters.AddWithValue("requires_selling_plan", variant.RequiresSellingPlan);
        command.Parameters.AddWithValue("category_id", variant.CategoryId ?? "");
        command.Parameters.AddWithValue("category_name", variant.CategoryName ?? "");
        command.Parameters.AddWithValue("category_full_name", variant.CategoryFullName ?? "");
        command.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        command.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
    }

    // ========== HELPER METHODS ==========

    private string GetString(Dictionary<string, object?> data, string key)
    {
        if (data.TryGetValue(key, out var value) && value != null)
        {
            return value.ToString() ?? "";
        }
        return "";
    }

    private long GetLong(Dictionary<string, object?> data, string key)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null)
            {
                if (value is long longValue) return longValue;
                if (value is int intValue) return intValue;
                
                var strValue = value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(strValue) && long.TryParse(strValue, out var parsed))
                    return parsed;
            }
        }
        catch { }
        return 0;
    }

    private int GetInt(Dictionary<string, object?> data, string key)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null)
            {
                if (value is int intValue) return intValue;
                if (value is long longValue) return (int)longValue;
                if (value is decimal decValue) return (int)decValue;
                if (value is double doubleValue) return (int)doubleValue;
                
                var strValue = value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(strValue))
                {
                    if (int.TryParse(strValue, out var parsedInt)) return parsedInt;
                    if (decimal.TryParse(strValue, out var parsedDecimal)) return (int)parsedDecimal;
                }
            }
        }
        catch { }
        return 0;
    }

    private decimal GetDecimal(Dictionary<string, object?> data, string key)
    {
        try
        {
            if (data.TryGetValue(key, out var value) && value != null)
            {
                if (value is decimal decValue) return decValue;
                if (value is double doubleValue) return (decimal)doubleValue;
                if (value is int intValue) return intValue;
                if (value is long longValue) return longValue;
                
                var strValue = value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(strValue) && decimal.TryParse(strValue, out var parsed))
                    return parsed;
            }
        }
        catch { }
        return 0m;
    }

    private ProductStatus ParseProductStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return ProductStatus.Draft;
        
        return status.ToUpper().Trim() switch
        {
            "ACTIVE" => ProductStatus.Active,
            "DRAFT" => ProductStatus.Draft,
            "ARCHIVED" => ProductStatus.Archived,
            _ => ProductStatus.Draft
        };
    }

    private bool ParseBoolean(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        
        return value.ToUpper().Trim() switch
        {
            "TRUE" or "T" or "1" or "YES" or "Y" => true,
            _ => false
        };
    }

    private Currency ParseCurrencyFromCode(string? code)
    {
        if (string.IsNullOrEmpty(code)) return Currency.USD;
        
        return code.ToUpper().Trim() switch
        {
            "USD" => Currency.USD,
            "EUR" => Currency.EUR,
            "GBP" => Currency.GBP,
            "PLN" => Currency.PLN,
            _ => Currency.USD
        };
    }

    private int GetColumnIndex(NpgsqlDataReader reader, string columnName)
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

    private string GetStringFromReader(NpgsqlDataReader reader, int index)
    {
        return reader.IsDBNull(index) ? "" : reader.GetString(index);
    }

    private int GetIntFromReader(NpgsqlDataReader reader, int index)
    {
        if (reader.IsDBNull(index)) return 0;
        
        try
        {
            // Try reading as int first
            if (reader.GetFieldType(index) == typeof(int) || reader.GetFieldType(index) == typeof(long))
            {
                return reader.GetInt32(index);
            }
            
            // If it's text, parse it
            var value = reader.GetValue(index);
            if (value is string strValue)
            {
                if (int.TryParse(strValue, out var parsed))
                    return parsed;
                if (decimal.TryParse(strValue, out var decParsed))
                    return (int)decParsed;
            }
            
            return Convert.ToInt32(value);
        }
        catch
        {
            return 0;
        }
    }

    private decimal GetDecimalFromReader(NpgsqlDataReader reader, int index)
    {
        if (reader.IsDBNull(index)) return 0m;
        
        try
        {
            // Try reading as decimal first
            if (reader.GetFieldType(index) == typeof(decimal) || reader.GetFieldType(index) == typeof(double) || reader.GetFieldType(index) == typeof(float))
            {
                return reader.GetDecimal(index);
            }
            
            // If it's text, parse it
            var value = reader.GetValue(index);
            if (value is string strValue)
            {
                if (decimal.TryParse(strValue, out var parsed))
                    return parsed;
            }
            
            return Convert.ToDecimal(value);
        }
        catch
        {
            return 0m;
        }
    }

    private bool GetBoolFromReader(NpgsqlDataReader reader, int index)
    {
        if (reader.IsDBNull(index)) return false;
        
        try
        {
            // Try reading as bool first
            if (reader.GetFieldType(index) == typeof(bool))
            {
                return reader.GetBoolean(index);
            }
            
            // If it's text, parse it
            var value = reader.GetValue(index);
            if (value is string strValue)
            {
                return strValue.ToUpper().Trim() switch
                {
                    "TRUE" or "T" or "1" or "YES" or "Y" => true,
                    _ => false
                };
            }
            
            return Convert.ToBoolean(value);
        }
        catch
        {
            return false;
        }
    }

    private long GetLongFromReader(NpgsqlDataReader reader, int index)
    {
        if (reader.IsDBNull(index)) return 0L;
        
        try
        {
            // Try reading as long first
            if (reader.GetFieldType(index) == typeof(long) || reader.GetFieldType(index) == typeof(int))
            {
                return reader.GetInt64(index);
            }
            
            // If it's text, parse it
            var value = reader.GetValue(index);
            if (value is string strValue)
            {
                if (long.TryParse(strValue, out var parsed))
                    return parsed;
            }
            
            return Convert.ToInt64(value);
        }
        catch
        {
            return 0L;
        }
    }
}

// ========== SUPPORTING CLASSES ==========

public class ImportSummary
{
    public bool Success { get; set; }
    public int TotalRead { get; set; }
    public int ProductsSaved { get; set; }
    public int VariantsSaved { get; set; }
    public int OrdersSaved { get; set; }
    public int Skipped { get; set; }
    public List<ProblemRecord> ProblematicRecords { get; set; } = new List<ProblemRecord>();
    public Dictionary<long, Guid> ProductIdMap { get; set; } = new Dictionary<long, Guid>();
    public Dictionary<long, Guid> VariantIdMap { get; set; } = new Dictionary<long, Guid>();
    public Dictionary<long, Guid> OrderIdMap { get; set; } = new Dictionary<long, Guid>();
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ProblemRecord
{
    public string Id { get; set; } = "";
    public string Reason { get; set; } = "";
    public string? Title { get; set; }
    public string? Vendor { get; set; }
    public long SrcProductId { get; set; }
}

public class OrderLineItemData
{
    public string OrderGid { get; set; } = "";
    public string ProductGid { get; set; } = "";
    public int Quantity { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public decimal DiscountedUnitPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool RequiresShipping { get; set; }
    public bool Taxable { get; set; }
}
