using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.ServingSuggestions.Admin.Models;

/// <summary>
/// Represents a serving suggestion model - used both for the product-edit card's own summary
/// and for the create/edit popup
/// </summary>
public partial record ServingSuggestionModel : BaseNopEntityModel, ILocalizedModel<ServingSuggestionLocalizedModel>
{
    #region Ctor

    public ServingSuggestionModel()
    {
        Locales = new List<ServingSuggestionLocalizedModel>();
        ServingSuggestionStepSearchModel = new ServingSuggestionStepSearchModel();
    }

    #endregion

    #region Properties

    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the product already has a serving suggestion
    /// </summary>
    public bool HasServingSuggestion { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ServingSuggestions.Fields.Title")]
    public string Title { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ServingSuggestions.Fields.Description")]
    public string Description { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ServingSuggestions.Fields.Picture")]
    public int PictureId { get; set; }

    public string PictureUrl { get; set; }

    public IList<ServingSuggestionLocalizedModel> Locales { get; set; }

    public ServingSuggestionStepSearchModel ServingSuggestionStepSearchModel { get; set; }

    #endregion
}

/// <summary>
/// Represents a serving suggestion locale model
/// </summary>
public partial record ServingSuggestionLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ServingSuggestions.Fields.Title")]
    public string Title { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ServingSuggestions.Fields.Description")]
    public string Description { get; set; }
}
