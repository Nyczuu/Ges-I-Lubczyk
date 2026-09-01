using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.ServingSuggestions.Admin.Models;

/// <summary>
/// Represents a search model for the serving suggestion steps grid on the product-edit page tab
/// </summary>
public partial record ServingSuggestionStepSearchModel : BaseSearchModel
{
    #region Properties

    public int ServingSuggestionId { get; set; }

    #endregion
}
