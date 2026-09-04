using Nop.Plugin.Misc.ProductionLabels.Domain;

namespace Nop.Plugin.Misc.ProductionLabels.Services;

/// <summary>
/// Represents a production label model factory: pure content assembly, no PDF dependency
/// </summary>
public interface IProductionLabelModelFactory
{
    /// <summary>
    /// Prepares the label content for one product+batch, in the given language. Throws
    /// <see cref="Nop.Core.NopException"/> only when real ingredient-composition truncation would occur
    /// (a node at the maximum nesting depth still has recorded, un-rendered children) - a legitimate,
    /// complete composition at the depth cap renders fully and does not throw.
    /// </summary>
    /// <param name="productionBatch">Production batch</param>
    /// <param name="languageId">The label's chosen language - passed explicitly end to end, never the ambient working language</param>
    /// <param name="sizeVariant">The chosen preset size layout</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the assembled label content
    /// </returns>
    Task<ProductionLabelModel> PrepareProductionLabelModelAsync(ProductionBatch productionBatch, int languageId, ProductionLabelSizeVariant sizeVariant);
}
