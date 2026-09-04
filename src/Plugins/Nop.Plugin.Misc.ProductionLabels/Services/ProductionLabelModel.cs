using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.ProductionLabels.Domain;

namespace Nop.Plugin.Misc.ProductionLabels.Services;

/// <summary>
/// Represents the assembled content of one printable product label (EU 1169/2011-scoped, see spec §6):
/// product name, fully-expanded ingredient tree, net quantity, batch/best-before data, food business
/// operator (store) details, and the two per-product per-language admin inputs. Pure content - no PDF
/// dependency, rendered by <c>Admin/Views/ProductionLabelTemplate.cshtml</c>.
/// </summary>
public class ProductionLabelModel
{
    public ProductionLabelModel()
    {
        Ingredients = new List<ProductionLabelIngredientModel>();
    }

    /// <summary>
    /// Gets or sets the product name, resolved for the label's chosen language
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// Gets or sets the directly-attached ingredients, descending weight order, each with its own
    /// (possibly nested) composite children fully expanded inline
    /// </summary>
    public IList<ProductionLabelIngredientModel> Ingredients { get; set; }

    /// <summary>
    /// Gets or sets the net quantity, formatted with its unit (e.g. "250 g") - <see cref="Nop.Core.Domain.Catalog.Product.Weight"/>
    /// combined with the store's base weight measure via <c>IMeasureService</c>
    /// </summary>
    public string NetQuantity { get; set; }

    /// <summary>
    /// Gets or sets the system-generated batch code
    /// </summary>
    public string BatchCode { get; set; }

    /// <summary>
    /// Gets or sets the best-before date
    /// </summary>
    public DateTime BestBeforeDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the food business operator (store) name
    /// </summary>
    public string CompanyName { get; set; }

    /// <summary>
    /// Gets or sets the food business operator (store) address
    /// </summary>
    public string CompanyAddress { get; set; }

    /// <summary>
    /// Gets or sets the food business operator (store) phone number
    /// </summary>
    public string CompanyPhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the storage conditions text for the label's chosen language - null/empty renders
    /// without that line rather than blocking generation (spec §10)
    /// </summary>
    public string StorageConditions { get; set; }

    /// <summary>
    /// Gets or sets the country of origin text for the label's chosen language - null/empty renders
    /// without that line rather than blocking generation (spec §10)
    /// </summary>
    public string CountryOfOrigin { get; set; }

    /// <summary>
    /// Gets or sets the preset size variant, driving the template's CSS geometry
    /// </summary>
    public ProductionLabelSizeVariant SizeVariant { get; set; }
}

/// <summary>
/// Represents one node (root or nested composite child) of the label's fully-expanded ingredient tree
/// </summary>
public class ProductionLabelIngredientModel
{
    public ProductionLabelIngredientModel()
    {
        Children = new List<ProductionLabelIngredientModel>();
    }

    /// <summary>
    /// Gets or sets the ingredient name, resolved for the label's chosen language
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the EU-regulated allergen classification of this ingredient, if any
    /// </summary>
    public AllergenType AllergenType { get; set; }

    /// <summary>
    /// Gets or sets the nested composite children, expanded inline (e.g. "beef broth (bones, water,
    /// carrot, celery, salt)"), empty for a non-composite ingredient
    /// </summary>
    public IList<ProductionLabelIngredientModel> Children { get; set; }
}
