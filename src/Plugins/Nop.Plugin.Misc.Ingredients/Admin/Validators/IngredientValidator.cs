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

        //no NotEmpty/NotNull rule here for the four nutritional fields: they are plain (non-nullable)
        //decimal properties, so an empty form submission already fails ASP.NET Core model binding before
        //this validator ever runs (same mechanism as ProductModel.Price) - "required" is enforced
        //structurally, not by a rule that would also reject a genuine 0 (water, salt)
        RuleFor(model => model.CaloriesPer100g)
            .GreaterThanOrEqualTo(0)
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Fields.CaloriesPer100g.GreaterThanOrEqualZero"));

        RuleFor(model => model.ProteinPer100g)
            .GreaterThanOrEqualTo(0)
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Fields.ProteinPer100g.GreaterThanOrEqualZero"));

        RuleFor(model => model.FatPer100g)
            .GreaterThanOrEqualTo(0)
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Fields.FatPer100g.GreaterThanOrEqualZero"));

        RuleFor(model => model.CarbohydratePer100g)
            .GreaterThanOrEqualTo(0)
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Fields.CarbohydratePer100g.GreaterThanOrEqualZero"));

        //max-length rule for Name (400, per IngredientBuilder) and upper-bound rules for the four
        //nutritional fields (from their AsDecimal(18, 4) column metadata) are added automatically below
        SetDatabaseValidationRules<Ingredient>();
    }
}
