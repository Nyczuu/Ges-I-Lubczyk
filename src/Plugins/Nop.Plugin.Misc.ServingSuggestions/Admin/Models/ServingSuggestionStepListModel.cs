using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.ServingSuggestions.Admin.Models;

/// <summary>
/// Represents a list model for the serving suggestion steps grid on the product-edit page tab
/// </summary>
public partial record ServingSuggestionStepListModel : BasePagedListModel<ServingSuggestionStepModel>;
