using FluentValidation;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Models;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Misc.ServingSuggestions.Admin.Validators;

/// <summary>
/// Represents a serving suggestion step model validator
/// </summary>
public class ServingSuggestionStepValidator : BaseNopValidator<ServingSuggestionStepModel>
{
    public ServingSuggestionStepValidator(ILocalizationService localizationService)
    {
        RuleFor(model => model.Text)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.ServingSuggestions.Fields.Text.Required"));

        SetDatabaseValidationRules<ServingSuggestionStep>();
    }
}
