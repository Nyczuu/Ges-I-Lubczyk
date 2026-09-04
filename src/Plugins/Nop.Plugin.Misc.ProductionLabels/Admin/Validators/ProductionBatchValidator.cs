using FluentValidation;
using Nop.Plugin.Misc.ProductionLabels.Admin.Models;
using Nop.Plugin.Misc.ProductionLabels.Domain;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Validators;

/// <summary>
/// Represents a production batch model validator. Mirrored by the identical service-layer
/// <see cref="Nop.Core.NopException"/> checks in <c>ProductionBatchService</c> (validator for UX, service
/// for every caller - this repo's established double-enforcement pattern).
/// </summary>
public class ProductionBatchValidator : BaseNopValidator<ProductionBatchModel>
{
    public ProductionBatchValidator(ILocalizationService localizationService)
    {
        RuleFor(model => model.BestBeforeDateUtc)
            .GreaterThan(model => model.ProductionDateUtc)
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.ProductionLabels.Fields.BestBeforeDate.MustBeAfterProductionDate"));

        RuleFor(model => model.Quantity)
            .GreaterThan(0)
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.ProductionLabels.Fields.Quantity.GreaterThanZero"));

        SetDatabaseValidationRules<ProductionBatch>();
    }
}
