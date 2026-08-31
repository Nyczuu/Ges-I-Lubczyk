using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Ingredients.Admin.Models;

/// <summary>
/// Represents the model submitted from the "add ingredient" popup on the Ingredient edit page
/// </summary>
public partial record AddIngredientCompositionModel : BaseNopModel
{
    #region Ctor

    public AddIngredientCompositionModel()
    {
        SelectedIngredientIds = new List<int>();
    }

    #endregion

    #region Properties

    public int ParentIngredientId { get; set; }

    public IList<int> SelectedIngredientIds { get; set; }

    #endregion
}
