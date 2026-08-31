using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Ingredients.Admin.Models;

/// <summary>
/// Represents a list model for the composition grid on the Ingredient edit page
/// </summary>
public partial record IngredientCompositionListModel : BasePagedListModel<IngredientCompositionModel>;
