using Nop.Core;
using Nop.Plugin.Misc.ProductionLabels.Domain;

namespace Nop.Plugin.Misc.ProductionLabels.Services;

/// <summary>
/// Represents a production batch service: CRUD/listing only, owns no Ingredients/Store/GenericAttribute
/// reads. Rows are immutable once created - there is deliberately no update method.
/// </summary>
public interface IProductionBatchService
{
    /// <summary>
    /// Gets production batches, newest-first. Backs both admin surfaces (the product-edit tab and the
    /// standalone "Production" section) via one shared search model.
    /// </summary>
    /// <param name="productId">Product identifier to filter by; pass null (or 0) to load batches for every product</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the paged list of production batches
    /// </returns>
    Task<IPagedList<ProductionBatch>> GetAllProductionBatchesAsync(int? productId = null, int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    /// Gets a production batch by identifier
    /// </summary>
    /// <param name="productionBatchId">Production batch identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the production batch
    /// </returns>
    Task<ProductionBatch> GetProductionBatchByIdAsync(int productionBatchId);

    /// <summary>
    /// Inserts a production batch: validates (<see cref="ProductionBatch.BestBeforeDateUtc"/> must be
    /// after <see cref="ProductionBatch.ProductionDateUtc"/>; <see cref="ProductionBatch.Quantity"/> must
    /// be greater than zero, both rejected with <see cref="Nop.Core.NopException"/>), generates the
    /// system <see cref="ProductionBatch.BatchCode"/>, and stamps <see cref="ProductionBatch.CreatedOnUtc"/>
    /// </summary>
    /// <param name="productionBatch">Production batch (BatchCode/CreatedOnUtc are set by this method, not the caller)</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertProductionBatchAsync(ProductionBatch productionBatch);

    /// <summary>
    /// Deletes a production batch. Throws <see cref="Nop.Core.NopException"/> if a label has already been
    /// generated from it - deleting a row a real label was printed from would break the paper trail the
    /// whole feature exists for.
    /// </summary>
    /// <param name="productionBatch">Production batch</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteProductionBatchAsync(ProductionBatch productionBatch);

    /// <summary>
    /// Marks a production batch as having had a label generated, stamping
    /// <see cref="ProductionBatch.LabelGeneratedOnUtc"/> with the current UTC time. Called from exactly
    /// one place: after a label has rendered successfully and is being returned to the admin - never
    /// before render, and never on a failed render.
    /// </summary>
    /// <param name="productionBatch">Production batch</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task MarkLabelGeneratedAsync(ProductionBatch productionBatch);
}
