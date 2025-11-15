using EntityArchitect.CRUD;
using EntityArchitect.CRUD.Actions;
using EntityArchitect.CRUD.Entities;
using Paris2025.Repositories;
using Paris2025.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddEntityArchitect(typeof(Program).Assembly, connectionString ?? "");
builder.Services.UseActions(typeof(Program).Assembly);

// Register import service
builder.Services.AddScoped<MasterImportService>();

var app = builder.Build();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// SINGLE MASTER IMPORT ENDPOINT
// Imports all data from old tables to new tables
// - Reads from old_products
// - Filters by Shopify GID type (Product vs ProductVariant)
// - Products (gid://shopify/Product/*) -> variant table
// - ProductVariants (gid://shopify/ProductVariant/*) -> product table
// app.MapPost("/api/import", async (MasterImportService importService, ILogger<Program> logger, HttpContext context) =>
// {
//     var batchSize = 100;
//     if (context.Request.Query.TryGetValue("batchSize", out var batchSizeValue) && 
//         int.TryParse(batchSizeValue, out var parsedBatchSize) && parsedBatchSize > 0)
//     {
//         batchSize = parsedBatchSize;
//     }
//     
//     logger.LogInformation("=== IMPORT STARTED ===");
//     logger.LogInformation("Batch size: {BatchSize}", batchSize);
//     
//     var result = await importService.ImportAllDataAsync(batchSize);
//     
//     if (result.Success)
//     {
//         logger.LogInformation("=== IMPORT SUCCESSFUL ===");
//         return Results.Ok(new { 
//             success = true,
//             totalRead = result.TotalRead,
//             productsSaved = result.ProductsSaved,
//             variantsSaved = result.VariantsSaved,
//             skipped = result.Skipped,
//             productIdMappings = result.ProductIdMap.Count,
//             variantIdMappings = result.VariantIdMap.Count,
//             problematicRecords = result.ProblematicRecords.Select(p => new {
//                 id = p.Id,
//                 reason = p.Reason,
//                 title = p.Title,
//                 vendor = p.Vendor,
//                 srcProductId = p.SrcProductId
//             }).ToList(),
//             durationMs = result.Duration.TotalMilliseconds,
//             message = result.ProblematicRecords.Any() 
//                 ? $"Import completed with {result.ProblematicRecords.Count} problematic records"
//                 : "Import completed successfully. Generated new GUIDs for all records."
//         });
//     }
//     else
//     {
//         logger.LogError("=== IMPORT FAILED ===");
//         logger.LogError("Error: {Error}", result.ErrorMessage);
//         return Results.BadRequest(new {
//             success = false,
//             error = result.ErrorMessage,
//             totalRead = result.TotalRead,
//             productsSaved = result.ProductsSaved,
//             variantsSaved = result.VariantsSaved,
//             skipped = result.Skipped,
//             problematicRecords = result.ProblematicRecords.Select(p => new {
//                 id = p.Id,
//                 reason = p.Reason,
//                 title = p.Title,
//                 vendor = p.Vendor,
//                 srcProductId = p.SrcProductId
//             }).ToList(),
//             durationMs = result.Duration.TotalMilliseconds
//         });
//     }
// });
//
// // Endpoint to import orders from old_orders to order table
// app.MapPost("/api/import-orders", async (MasterImportService importService, ILogger<Program> logger, HttpContext context) =>
// {
//     var batchSize = 100;
//     if (context.Request.Query.TryGetValue("batchSize", out var batchSizeValue) && 
//         int.TryParse(batchSizeValue, out var parsedBatchSize) && parsedBatchSize > 0)
//     {
//         batchSize = parsedBatchSize;
//     }
//     
//     logger.LogInformation("=== ORDER IMPORT STARTED ===");
//     logger.LogInformation("Batch size: {BatchSize}", batchSize);
//     
//     var (success, savedCount, idMap, error) = await importService.ImportOrdersAsync(batchSize);
//     
//     if (success)
//     {
//         logger.LogInformation("=== ORDER IMPORT SUCCESSFUL ===");
//         return Results.Ok(new { 
//             success = true,
//             ordersSaved = savedCount,
//             orderIdMappings = idMap.Count,
//             durationMs = 0, // Will be in logs
//             message = $"Successfully imported {savedCount} orders with {idMap.Count} GUID mappings"
//         });
//     }
//     else
//     {
//         logger.LogError("=== ORDER IMPORT FAILED ===");
//         logger.LogError("Error: {Error}", error);
//         return Results.BadRequest(new {
//             success = false,
//             error = error,
//             ordersSaved = savedCount,
//             orderIdMappings = idMap.Count
//         });
//     }
// });
//
// // Endpoint to import variant-product relationships from inventory_variants
// // Reads existing products and variants from database and creates relationships
// app.MapPost("/api/import-variant-product-relations", async (MasterImportService importService, ILogger<Program> logger, HttpContext context) =>
// {
//     var batchSize = 10000;
//     if (context.Request.Query.TryGetValue("batchSize", out var batchSizeValue) && 
//         int.TryParse(batchSizeValue, out var parsedBatchSize) && parsedBatchSize > 0)
//     {
//         batchSize = parsedBatchSize;
//     }
//     
//     logger.LogInformation("=== VARIANT-PRODUCT RELATIONS IMPORT STARTED ===");
//     logger.LogInformation("Batch size: {BatchSize}", batchSize);
//     
//     // Step 1: Load ID maps from database
//     logger.LogInformation("Loading product and variant ID maps from database...");
//     var productIdMap = await importService.GetProductIdMapAsync();
//     var variantIdMap = await importService.GetVariantIdMapAsync();
//     
//     if (productIdMap.Count == 0 || variantIdMap.Count == 0)
//     {
//         logger.LogError("No products or variants found in database. Run /api/import first.");
//         return Results.BadRequest(new
//         {
//             success = false,
//             error = "No products or variants found. Please run /api/import first to import products and variants."
//         });
//     }
//     
//     // Step 2: Import relations
//     var (success, savedCount, skipped, error) = await importService.ImportVariantProductRelationsAsync(productIdMap, variantIdMap, batchSize);
//     
//     if (success)
//     {
//         logger.LogInformation("=== VARIANT-PRODUCT RELATIONS IMPORT SUCCESSFUL ===");
//         return Results.Ok(new
//         {
//             success = true,
//             relationsSaved = savedCount,
//             skipped = skipped,
//             message = $"Successfully imported {savedCount} variant-product relations (skipped {skipped})"
//         });
//     }
//     else
//     {
//         logger.LogError("=== VARIANT-PRODUCT RELATIONS IMPORT FAILED ===");
//         logger.LogError("Error: {Error}", error);
//         return Results.BadRequest(new
//         {
//             success = false,
//             error = error,
//             relationsSaved = savedCount,
//             skipped = skipped
//         });
//     }
// });
//
// // Endpoint to import order-product relationships from order_line_items
// // Reads existing orders and products from database and creates relationships
// app.MapPost("/api/import-order-product-relations", async (MasterImportService importService, ILogger<Program> logger, HttpContext context) =>
// {
//     var batchSize = 10000;
//     if (context.Request.Query.TryGetValue("batchSize", out var batchSizeValue) && 
//         int.TryParse(batchSizeValue, out var parsedBatchSize) && parsedBatchSize > 0)
//     {
//         batchSize = parsedBatchSize;
//     }
//     
//     logger.LogInformation("=== ORDER-PRODUCT RELATIONS IMPORT STARTED ===");
//     logger.LogInformation("Batch size: {BatchSize}", batchSize);
//     
//     // Step 1: Load ID maps from database
//     logger.LogInformation("Loading order, product, and variant ID maps from database...");
//     var orderIdMap = await importService.GetOrderIdMapAsync();
//     var productIdMap = await importService.GetProductIdMapAsync();
//     var variantIdMap = await importService.GetVariantIdMapAsync();
//     
//     if (orderIdMap.Count == 0)
//     {
//         logger.LogError("No orders found in database.");
//         return Results.BadRequest(new
//         {
//             success = false,
//             error = "No orders found. Please run /api/import-orders first."
//         });
//     }
//     
//     if (productIdMap.Count == 0 && variantIdMap.Count == 0)
//     {
//         logger.LogError("No products or variants found in database.");
//         return Results.BadRequest(new
//         {
//             success = false,
//             error = "No products or variants found. Please run /api/import first."
//         });
//     }
//     
//     // Step 2: Import relations
//     var (success, savedCount, skipped, error) = await importService.ImportOrderProductRelationsAsync(orderIdMap, productIdMap, variantIdMap, batchSize);
//     
//     if (success)
//     {
//         logger.LogInformation("=== ORDER-PRODUCT RELATIONS IMPORT SUCCESSFUL ===");
//         return Results.Ok(new
//         {
//             success = true,
//             relationsSaved = savedCount,
//             skipped = skipped,
//             message = $"Successfully imported {savedCount} order-product relations (skipped {skipped})"
//         });
//     }
//     else
//     {
//         logger.LogError("=== ORDER-PRODUCT RELATIONS IMPORT FAILED ===");
//         logger.LogError("Error: {Error}", error);
//         return Results.BadRequest(new
//         {
//             success = false,
//             error = error,
//             relationsSaved = savedCount,
//             skipped = skipped
//         });
//     }
// });
//
// // Endpoint to update product inventory fields from inventory_products joined with inventory_variants
// app.MapPost("/api/update-product-inventory-fields", async (MasterImportService importService, ILogger<Program> logger) =>
// {
//     logger.LogInformation("=== PRODUCT INVENTORY FIELDS UPDATE STARTED ===");
//     
//     var (success, updatedCount, error) = await importService.UpdateProductInventoryFieldsAsync();
//     
//     if (success)
//     {
//         logger.LogInformation("=== PRODUCT INVENTORY FIELDS UPDATE SUCCESSFUL ===");
//         return Results.Ok(new
//         {
//             success = true,
//             recordsUpdated = updatedCount,
//             message = $"Successfully updated {updatedCount} product inventory fields"
//         });
//     }
//     else
//     {
//         logger.LogError("=== PRODUCT INVENTORY FIELDS UPDATE FAILED ===");
//         logger.LogError("Error: {Error}", error);
//         return Results.BadRequest(new
//         {
//             success = false,
//             error = error,
//             recordsUpdated = updatedCount
//         });
//     }
// });
//
// // Endpoint to update variant inventory fields from inventory_products joined with inventory_variants
// app.MapPost("/api/update-variant-inventory-fields", async (MasterImportService importService, ILogger<Program> logger) =>
// {
//     logger.LogInformation("=== VARIANT INVENTORY FIELDS UPDATE STARTED ===");
//     
//     var (success, updatedCount, error) = await importService.UpdateVariantInventoryFieldsAsync();
//     
//     if (success)
//     {
//         logger.LogInformation("=== VARIANT INVENTORY FIELDS UPDATE SUCCESSFUL ===");
//         return Results.Ok(new
//         {
//             success = true,
//             recordsUpdated = updatedCount,
//             message = $"Successfully updated {updatedCount} variant inventory fields"
//         });
//     }
//     else
//     {
//         logger.LogError("=== VARIANT INVENTORY FIELDS UPDATE FAILED ===");
//         logger.LogError("Error: {Error}", error);
//         return Results.BadRequest(new
//         {
//             success = false,
//             error = error,
//             recordsUpdated = updatedCount
//         });
//     }
// });

app.Run();
