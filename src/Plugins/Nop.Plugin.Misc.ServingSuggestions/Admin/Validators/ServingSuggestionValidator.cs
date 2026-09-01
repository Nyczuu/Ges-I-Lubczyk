using FluentValidation;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Models;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Misc.ServingSuggestions.Admin.Validators;

/// <summary>
/// Represents a serving suggestion model validator
/// </summary>
public class ServingSuggestionValidator : BaseNopValidator<ServingSuggestionModel>
{
    public ServingSuggestionValidator(ILocalizationService localizationService)
    {
        RuleFor(model => model.Title)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.ServingSuggestions.Fields.Title.Required"));

        //image is required (spec §1) - PictureId is only ever set by the controller once a picture has
        //been uploaded (new) or carried over (existing), so a submission with no picture stays at 0
        RuleFor(model => model.PictureId)
            .GreaterThan(0)
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.ServingSuggestions.Fields.Picture.Required"));

        //max-length rule for Title (400, per ServingSuggestionBuilder) is added automatically below,
        //from the entity's own column metadata
        SetDatabaseValidationRules<ServingSuggestion>();
    }
}
