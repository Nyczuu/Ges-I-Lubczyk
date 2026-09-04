using AwesomeAssertions;
using Nop.Plugin.Misc.ProductionLabels.Admin.Models;
using Nop.Plugin.Misc.ProductionLabels.Admin.Validators;
using Nop.Services.Localization;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ProductionLabels;

[TestFixture]
public class ProductionBatchValidatorTests : ServiceTest
{
    private ProductionBatchValidator _validator;

    [OneTimeSetUp]
    public void SetUp()
    {
        _validator = new ProductionBatchValidator(GetService<ILocalizationService>());
    }

    private static ProductionBatchModel ValidModel() => new()
    {
        ProductId = 1,
        ProductionDateUtc = new DateTime(2026, 9, 4),
        BestBeforeDateUtc = new DateTime(2026, 12, 4),
        Quantity = 10
    };

    [Test]
    public void Validate_Fails_WhenBestBeforeDateIsNotAfterProductionDate()
    {
        var model = ValidModel();
        model.BestBeforeDateUtc = model.ProductionDateUtc;

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ProductionBatchModel.BestBeforeDateUtc));
    }

    [Test]
    public void Validate_Fails_WhenBestBeforeDateIsBeforeProductionDate()
    {
        var model = ValidModel();
        model.BestBeforeDateUtc = model.ProductionDateUtc.AddDays(-1);

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ProductionBatchModel.BestBeforeDateUtc));
    }

    [Test]
    public void Validate_Fails_WhenQuantityIsZero()
    {
        var model = ValidModel();
        model.Quantity = 0;

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ProductionBatchModel.Quantity));
    }

    [Test]
    public void Validate_Fails_WhenQuantityIsNegative()
    {
        var model = ValidModel();
        model.Quantity = -1;

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ProductionBatchModel.Quantity));
    }

    [Test]
    public void Validate_Succeeds_ForAValidModel()
    {
        var result = _validator.Validate(ValidModel());

        result.IsValid.Should().BeTrue();
    }
}
