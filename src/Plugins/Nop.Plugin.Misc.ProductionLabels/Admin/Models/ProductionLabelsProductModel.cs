using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Models;

/// <summary>
/// Represents the per-product, per-(active-store-)language Storage conditions / Country of origin editor
/// shown on the product-edit page tab (spec §5, §6) - one input per configured language per field, reusing
/// <c>Html.LocalizedEditorAsync</c> and <c>ILocalizedModelFactory.PrepareLocalizedModelsAsync</c> across all
/// system-configured languages, the same admin pattern as any other localized field. A deliberately
/// different scope from the label-generation-time language picker (<see cref="GenerateProductionLabelModel"/>),
/// which is scoped to the store's configured languages only.
/// </summary>
public partial record ProductionLabelsProductModel : BaseNopModel, ILocalizedModel<ProductionLabelsProductLocalizedModel>
{
    #region Ctor

    public ProductionLabelsProductModel()
    {
        Locales = new List<ProductionLabelsProductLocalizedModel>();
        BatchSearchModel = new ProductionBatchSearchModel();
    }

    #endregion

    #region Properties

    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the storage conditions text for the store's resolved default language. Html.LocalizedEditorAsync
    /// (Nop.Web.Framework/Extensions/HtmlExtensions.cs:46) renders the standard, non-tabbed template - bound
    /// directly to this flat property via asp-for, not to Locales[i].* - whenever at most one language is
    /// configured, which is the out-of-the-box nopCommerce default. Populated by the admin factory from the
    /// default language's own locale entry; written by the controller when Locales arrives empty on save.
    /// </summary>
    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.StorageConditions")]
    public string StorageConditions { get; set; }

    /// <summary>
    /// Gets or sets the country of origin text for the store's resolved default language - see
    /// <see cref="StorageConditions"/> for why this flat property exists alongside <see cref="Locales"/>.
    /// </summary>
    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.CountryOfOrigin")]
    public string CountryOfOrigin { get; set; }

    public IList<ProductionLabelsProductLocalizedModel> Locales { get; set; }

    /// <summary>
    /// Gets or sets the search model backing the batch-history grid on this same product-edit page tab
    /// </summary>
    public ProductionBatchSearchModel BatchSearchModel { get; set; }

    #endregion
}

/// <summary>
/// Represents one language's Storage conditions / Country of origin values
/// </summary>
public partial record ProductionLabelsProductLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.StorageConditions")]
    public string StorageConditions { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.CountryOfOrigin")]
    public string CountryOfOrigin { get; set; }
}
