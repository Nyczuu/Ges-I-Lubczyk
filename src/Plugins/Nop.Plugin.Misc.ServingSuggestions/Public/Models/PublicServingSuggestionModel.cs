using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.ServingSuggestions.Public.Models;

/// <summary>
/// Represents the storefront model for a product's rendered serving suggestion
/// </summary>
public partial record PublicServingSuggestionModel : BaseNopModel
{
    public PublicServingSuggestionModel()
    {
        Steps = new List<PublicServingSuggestionStepModel>();
    }

    /// <summary>
    /// Gets or sets the localized title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the localized description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the picture URL
    /// </summary>
    public string PictureUrl { get; set; }

    /// <summary>
    /// Gets or sets the ordered steps
    /// </summary>
    public IList<PublicServingSuggestionStepModel> Steps { get; set; }
}

/// <summary>
/// Represents a single serving suggestion step
/// </summary>
public partial record PublicServingSuggestionStepModel : BaseNopModel
{
    /// <summary>
    /// Gets or sets the localized step text
    /// </summary>
    public string Text { get; set; }
}
