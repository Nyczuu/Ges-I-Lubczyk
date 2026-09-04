using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.ProductionLabels.Domain;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.ProductionLabels.Services;

/// <summary>
/// Represents a production batch service
/// </summary>
public class ProductionBatchService : IProductionBatchService
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly IRepository<ProductionBatch> _productionBatchRepository;

    #endregion

    #region Ctor

    public ProductionBatchService(ILocalizationService localizationService,
        IRepository<ProductionBatch> productionBatchRepository)
    {
        _localizationService = localizationService;
        _productionBatchRepository = productionBatchRepository;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Generates the system batch code: {ProductionDateUtc:yyyyMMdd}-{counter:D3}, where counter is
    /// 1 + MAX of the existing numeric suffix for (ProductId, ProductionDateUtc.Date) - deliberately not
    /// COUNT, which would collide after a mid-day batch delete (unlabeled batches are deletable, so COUNT
    /// can under-count and reissue an already-used code)
    /// </summary>
    protected virtual async Task<string> GenerateBatchCodeAsync(int productId, DateTime productionDateUtc)
    {
        var prefix = $"{productionDateUtc:yyyyMMdd}-";

        var existingCodes = await _productionBatchRepository.Table
            .Where(batch => batch.ProductId == productId && batch.BatchCode.StartsWith(prefix))
            .Select(batch => batch.BatchCode)
            .ToListAsync();

        var maxCounter = 0;
        foreach (var code in existingCodes)
        {
            var suffix = code.Length > prefix.Length ? code[prefix.Length..] : string.Empty;
            if (int.TryParse(suffix, out var counter) && counter > maxCounter)
                maxCounter = counter;
        }

        return $"{prefix}{maxCounter + 1:D3}";
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets production batches, newest-first
    /// </summary>
    public virtual async Task<IPagedList<ProductionBatch>> GetAllProductionBatchesAsync(int? productId = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _productionBatchRepository.Table;

        if (productId.HasValue && productId.Value > 0)
            query = query.Where(batch => batch.ProductId == productId.Value);

        query = query.OrderByDescending(batch => batch.CreatedOnUtc).ThenByDescending(batch => batch.Id);

        return await query.ToPagedListAsync(pageIndex, pageSize);
    }

    /// <summary>
    /// Gets a production batch by identifier. A plain repository query, not the
    /// <c>GetByIdAsync(id, cache => default)</c> shortcut - that overload still routes through
    /// <see cref="Nop.Core.Caching.IStaticCacheManager"/> when its cache-key function returns null (it
    /// falls back to the default by-id cache key), which would need its own cache-invalidation consumer
    /// on every insert/update/delete; the design deliberately keeps this entity uncached instead.
    /// </summary>
    public virtual async Task<ProductionBatch> GetProductionBatchByIdAsync(int productionBatchId)
    {
        return await _productionBatchRepository.Table.FirstOrDefaultAsync(batch => batch.Id == productionBatchId);
    }

    /// <summary>
    /// Inserts a production batch
    /// </summary>
    public virtual async Task InsertProductionBatchAsync(ProductionBatch productionBatch)
    {
        ArgumentNullException.ThrowIfNull(productionBatch);

        if (productionBatch.BestBeforeDateUtc <= productionBatch.ProductionDateUtc)
        {
            throw new NopException(await _localizationService.GetResourceAsync(
                "Plugins.Misc.ProductionLabels.Errors.BestBeforeDateNotAfterProductionDate"));
        }

        if (productionBatch.Quantity <= 0)
        {
            throw new NopException(await _localizationService.GetResourceAsync(
                "Plugins.Misc.ProductionLabels.Errors.QuantityNotGreaterThanZero"));
        }

        productionBatch.BatchCode = await GenerateBatchCodeAsync(productionBatch.ProductId, productionBatch.ProductionDateUtc);
        productionBatch.CreatedOnUtc = DateTime.UtcNow;

        await _productionBatchRepository.InsertAsync(productionBatch);
    }

    /// <summary>
    /// Deletes a production batch. Throws if a label has already been generated from it
    /// </summary>
    public virtual async Task DeleteProductionBatchAsync(ProductionBatch productionBatch)
    {
        ArgumentNullException.ThrowIfNull(productionBatch);

        if (productionBatch.LabelGeneratedOnUtc.HasValue)
        {
            throw new NopException(await _localizationService.GetResourceAsync(
                "Plugins.Misc.ProductionLabels.Errors.CannotDeleteLabeledBatch"));
        }

        await _productionBatchRepository.DeleteAsync(productionBatch);
    }

    /// <summary>
    /// Marks a production batch as having had a label generated
    /// </summary>
    public virtual async Task MarkLabelGeneratedAsync(ProductionBatch productionBatch)
    {
        ArgumentNullException.ThrowIfNull(productionBatch);

        productionBatch.LabelGeneratedOnUtc = DateTime.UtcNow;

        await _productionBatchRepository.UpdateAsync(productionBatch);
    }

    #endregion
}
