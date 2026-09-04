using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Misc.ProductionLabels.Domain;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Models;

/// <summary>
/// Represents the "Generate label" popup's options: which batch, which of the two preset size variants,
/// and (only when the store has more than one configured language) which language
/// </summary>
public partial record GenerateProductionLabelModel : BaseNopModel
{
    #region Ctor

    public GenerateProductionLabelModel()
    {
        AvailableSizeVariants = new List<SelectListItem>();
        AvailableLanguages = new List<SelectListItem>();
    }

    #endregion

    #region Properties

    public int ProductionBatchId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.SizeVariant")]
    public ProductionLabelSizeVariant SizeVariant { get; set; }

    /// <summary>
    /// Gets or sets the label's chosen language; null defaults to the store's default language (or, when
    /// the store's own default language is unset, the first language by display order among the store's
    /// active languages)
    /// </summary>
    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.Language")]
    public int? LanguageId { get; set; }

    public IList<SelectListItem> AvailableSizeVariants { get; set; }

    /// <summary>
    /// Gets or sets the store's configured languages; populated only when the store has more than one -
    /// a deliberately different scope from <see cref="ProductionLabelsProductModel"/>'s per-language
    /// editor, which spans every system-configured language regardless of store
    /// </summary>
    public IList<SelectListItem> AvailableLanguages { get; set; }

    #endregion
}
