using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Ingredients.Admin.Models;

/// <summary>
/// Represents a search model for the composition grid on the Ingredient edit page
/// </summary>
public partial record IngredientCompositionSearchModel : BaseSearchModel
{
    #region Properties

    public int IngredientId { get; set; }

    #endregion
}
