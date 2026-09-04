using FluentValidation;
using Nop.Plugin.Misc.ProductionLabels.Admin.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Validators;

/// <summary>
/// Represents a production labels product model validator - covers only <see cref="ProductionLabelsProductModel.DefaultShelfLifeDays"/>,
/// the one field on this tab with a validation rule (spec §5: when provided, must be a positive integer;
/// blank/null means "no default configured").
/// </summary>
public class ProductionLabelsProductValidator : BaseNopValidator<ProductionLabelsProductModel>
{
    public ProductionLabelsProductValidator(ILocalizationService localizationService)
    {
        RuleFor(model => model.DefaultShelfLifeDays)
            .GreaterThan(0)
            .When(model => model.DefaultShelfLifeDays.HasValue)
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays.GreaterThanZero"));
    }
}
