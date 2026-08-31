using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Ingredients.Admin.Models;

/// <summary>
/// Represents an ingredient search model. Reused, with <see cref="BaseSearchModel.SetPopupGridPageSize"/>,
/// as the search model for the "add ingredient" popups.
/// </summary>
public partial record IngredientSearchModel : BaseSearchModel
{
    #region Properties

    [NopResourceDisplayName("Plugins.Misc.Ingredients.Composition.SearchIngredientName")]
    public string SearchName { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the composite ingredient the popup grid is adding children to
    /// (0 when the popup is instead attaching ingredients to a product)
    /// </summary>
    public int ParentIngredientId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the product the popup grid is attaching ingredients to
    /// (0 when the popup is instead adding children to a composite ingredient)
    /// </summary>
    public int ProductId { get; set; }

    #endregion
}
