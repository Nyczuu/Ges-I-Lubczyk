using FluentValidation;
using Nop.Plugin.Misc.ProductionLabels.Admin.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Validators;

/// <summary>
/// Represents a production labels product model validator - covers only <see cref="ProductionLabelsProductModel.DefaultShelfLifeDays"/>,
/// the one field on this tab with a validation rule. Required (post-review amendment, spec §5): the admin
/// cannot save this tab without providing a positive integer - a product that has never had this tab saved
/// still has no value (GenericAttribute row simply absent), which the batch-popup prefill already treats as
/// "no default configured, enter dates manually" - this validator only stops a *new* blank save, it does not
/// retroactively guarantee every product has a value.
/// </summary>
public class ProductionLabelsProductValidator : BaseNopValidator<ProductionLabelsProductModel>
{
    public ProductionLabelsProductValidator(ILocalizationService localizationService)
    {
        RuleFor(model => model.DefaultShelfLifeDays)
            .NotNull()
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays.Required"));

        RuleFor(model => model.DefaultShelfLifeDays)
            .GreaterThan(0)
            .When(model => model.DefaultShelfLifeDays.HasValue)
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays.GreaterThanZero"));
    }
}
