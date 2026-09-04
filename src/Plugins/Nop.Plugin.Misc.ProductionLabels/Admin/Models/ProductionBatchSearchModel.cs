using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Models;

/// <summary>
/// Represents a production batch search model - one shared search model driving both admin surfaces
/// (mirrors the real core precedent <c>ProductReviewSearchModel.SearchProductId</c>)
/// </summary>
public partial record ProductionBatchSearchModel : BaseSearchModel
{
    #region Ctor

    public ProductionBatchSearchModel()
    {
        AvailableProducts = new List<SelectListItem>();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the product identifier to filter by; 0 = all products (the standalone section's scope)
    /// </summary>
    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.List.SearchProduct")]
    public int SearchProductId { get; set; }

    /// <summary>
    /// Gets or sets the candidate products for the standalone section's own filter dropdown; left empty
    /// on the product-edit page tab, where the product is already fixed
    /// </summary>
    public IList<SelectListItem> AvailableProducts { get; set; }

    #endregion
}
