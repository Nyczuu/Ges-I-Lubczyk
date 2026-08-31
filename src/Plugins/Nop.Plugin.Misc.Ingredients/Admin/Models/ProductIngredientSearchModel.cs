using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Ingredients.Admin.Models;

/// <summary>
/// Represents a search model for the ingredients grid on the product-edit page tab
/// </summary>
public partial record ProductIngredientSearchModel : BaseSearchModel
{
    #region Properties

    public int ProductId { get; set; }

    #endregion
}
