using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Ingredients.Public.Models;

/// <summary>
/// Represents the storefront model for a product's rendered ingredient list
/// </summary>
public partial record IngredientsModel : BaseNopModel
{
    public IngredientsModel()
    {
        Ingredients = new List<PublicIngredientModel>();
    }

    /// <summary>
    /// Gets or sets the directly-attached ingredients, each with its own nested composition
    /// </summary>
    public IList<PublicIngredientModel> Ingredients { get; set; }
}

/// <summary>
/// Represents a single ingredient node, with its own (possibly nested) composition
/// </summary>
public partial record PublicIngredientModel : BaseNopModel
{
    public PublicIngredientModel()
    {
        Children = new List<PublicIngredientModel>();
    }

    /// <summary>
    /// Gets or sets the localized name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the ingredients this one is made of, if any
    /// </summary>
    public IList<PublicIngredientModel> Children { get; set; }
}
