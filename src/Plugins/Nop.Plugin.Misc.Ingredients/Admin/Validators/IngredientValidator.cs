using FluentValidation;
using Nop.Plugin.Misc.Ingredients.Admin.Models;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Misc.Ingredients.Admin.Validators;

/// <summary>
/// Represents an ingredient model validator
/// </summary>
public class IngredientValidator : BaseNopValidator<IngredientModel>
{
    public IngredientValidator(ILocalizationService localizationService)
    {
        RuleFor(model => model.Name)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Fields.Name.Required"));

        //max-length rule for Name (400, per IngredientBuilder) is added automatically below,
        //from the entity's own column metadata
        SetDatabaseValidationRules<Ingredient>();
    }
}
