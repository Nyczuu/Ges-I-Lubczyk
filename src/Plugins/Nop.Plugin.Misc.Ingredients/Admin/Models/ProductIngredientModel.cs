using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Ingredients.Admin.Models;

/// <summary>
/// Represents a row of the ingredients grid on the product-edit page tab
/// </summary>
public partial record ProductIngredientModel : BaseNopEntityModel
{
    #region Properties

    public int ProductId { get; set; }

    public int IngredientId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.Ingredient")]
    public string IngredientName { get; set; }

    [NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    #endregion
}
