using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Ingredients.Admin.Models;

/// <summary>
/// Represents a row of the composition grid on the Ingredient edit page
/// </summary>
public partial record IngredientCompositionModel : BaseNopEntityModel
{
    #region Properties

    public int ParentIngredientId { get; set; }

    public int ChildIngredientId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.Ingredient")]
    public string ChildIngredientName { get; set; }

    [NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    #endregion
}
