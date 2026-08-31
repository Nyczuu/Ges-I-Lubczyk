using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Ingredients.Admin.Models;

/// <summary>
/// Represents an ingredient list model
/// </summary>
public partial record IngredientListModel : BasePagedListModel<IngredientModel>;
