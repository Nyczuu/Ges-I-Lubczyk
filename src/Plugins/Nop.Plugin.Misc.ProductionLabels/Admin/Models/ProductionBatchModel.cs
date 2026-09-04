using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Models;

/// <summary>
/// Represents a production batch model - backs both admin surfaces (the product-edit tab and the
/// standalone "Production" section)
/// </summary>
public partial record ProductionBatchModel : BaseNopEntityModel
{
    #region Ctor

    public ProductionBatchModel()
    {
        AvailableProducts = new List<SelectListItem>();
    }

    #endregion

    #region Properties

    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.Product")]
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name - populated by the admin factory, not AutoMapper
    /// </summary>
    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.Product")]
    public string ProductName { get; set; }

    /// <summary>
    /// Gets or sets the batch code - shown, never editable (system-generated)
    /// </summary>
    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.BatchCode")]
    public string BatchCode { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.ProductionDate")]
    [UIHint("Date")]
    public DateTime ProductionDateUtc { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.BestBeforeDate")]
    [UIHint("Date")]
    public DateTime BestBeforeDateUtc { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.Quantity")]
    public int Quantity { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.LabelGeneratedOnUtc")]
    public DateTime? LabelGeneratedOnUtc { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.CreatedOn")]
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the candidate products for the standalone section's create-batch popup; left empty
    /// when the popup was reached from a specific product's own edit-page tab (where ProductId is fixed)
    /// </summary>
    public IList<SelectListItem> AvailableProducts { get; set; }

    #endregion
}
