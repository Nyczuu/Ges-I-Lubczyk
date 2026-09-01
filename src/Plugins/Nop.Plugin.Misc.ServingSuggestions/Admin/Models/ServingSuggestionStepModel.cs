using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.ServingSuggestions.Admin.Models;

/// <summary>
/// Represents a serving suggestion step model
/// </summary>
public partial record ServingSuggestionStepModel : BaseNopEntityModel, ILocalizedModel<ServingSuggestionStepLocalizedModel>
{
    #region Ctor

    public ServingSuggestionStepModel()
    {
        Locales = new List<ServingSuggestionStepLocalizedModel>();
    }

    #endregion

    #region Properties

    public int ServingSuggestionId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ServingSuggestions.Fields.Text")]
    public string Text { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ServingSuggestions.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<ServingSuggestionStepLocalizedModel> Locales { get; set; }

    #endregion
}

/// <summary>
/// Represents a serving suggestion step locale model
/// </summary>
public partial record ServingSuggestionStepLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.ServingSuggestions.Fields.Text")]
    public string Text { get; set; }
}
