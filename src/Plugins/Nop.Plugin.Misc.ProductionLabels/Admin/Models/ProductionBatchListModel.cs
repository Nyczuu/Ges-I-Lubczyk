using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Models;

/// <summary>
/// Represents a production batch list model
/// </summary>
public partial record ProductionBatchListModel : BasePagedListModel<ProductionBatchModel>;
