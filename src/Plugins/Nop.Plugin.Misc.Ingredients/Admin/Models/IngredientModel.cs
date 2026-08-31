using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Ingredients.Admin.Models;

/// <summary>
/// Represents an ingredient model
/// </summary>
public partial record IngredientModel : BaseNopEntityModel, ILocalizedModel<IngredientLocalizedModel>
{
    #region Ctor

    public IngredientModel()
    {
        Locales = new List<IngredientLocalizedModel>();
        AvailableAllergenTypes = new List<SelectListItem>();
        IngredientCompositionSearchModel = new IngredientCompositionSearchModel();
    }

    #endregion

    #region Properties

    [NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.Name")]
    public string Name { get; set; }

    [NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.Description")]
    public string Description { get; set; }

    [NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.Allergen")]
    public int AllergenId { get; set; }

    public IList<SelectListItem> AvailableAllergenTypes { get; set; }

    public IList<IngredientLocalizedModel> Locales { get; set; }

    public IngredientCompositionSearchModel IngredientCompositionSearchModel { get; set; }

    #endregion
}

/// <summary>
/// Represents an ingredient locale model
/// </summary>
public partial record IngredientLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.Name")]
    public string Name { get; set; }

    [NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.Description")]
    public string Description { get; set; }
}
