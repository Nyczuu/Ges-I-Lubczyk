using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Ingredients.Admin.Models;

/// <summary>
/// Represents the model submitted from the "add ingredient" popup on the product-edit page tab
/// </summary>
public partial record AddProductIngredientModel : BaseNopModel
{
    #region Ctor

    public AddProductIngredientModel()
    {
        SelectedIngredientIds = new List<int>();
    }

    #endregion

    #region Properties

    public int ProductId { get; set; }

    public IList<int> SelectedIngredientIds { get; set; }

    #endregion
}
