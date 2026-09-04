using AwesomeAssertions;
using Nop.Plugin.Misc.ProductionLabels.Admin.Models;
using Nop.Plugin.Misc.ProductionLabels.Admin.Validators;
using Nop.Services.Localization;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ProductionLabels;

[TestFixture]
public class ProductionLabelsProductValidatorTests : ServiceTest
{
    private ProductionLabelsProductValidator _validator;

    [OneTimeSetUp]
    public void SetUp()
    {
        _validator = new ProductionLabelsProductValidator(GetService<ILocalizationService>());
    }

    [Test]
    public void Validate_Fails_WhenDefaultShelfLifeDaysIsZero()
    {
        var model = new ProductionLabelsProductModel { DefaultShelfLifeDays = 0 };

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ProductionLabelsProductModel.DefaultShelfLifeDays));
    }

    [Test]
    public void Validate_Fails_WhenDefaultShelfLifeDaysIsNegative()
    {
        var model = new ProductionLabelsProductModel { DefaultShelfLifeDays = -1 };

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ProductionLabelsProductModel.DefaultShelfLifeDays));
    }

    [Test]
    public void Validate_Succeeds_WhenDefaultShelfLifeDaysIsNull()
    {
        var model = new ProductionLabelsProductModel { DefaultShelfLifeDays = null };

        var result = _validator.Validate(model);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_Succeeds_WhenDefaultShelfLifeDaysIsPositive()
    {
        var model = new ProductionLabelsProductModel { DefaultShelfLifeDays = 14 };

        var result = _validator.Validate(model);

        result.IsValid.Should().BeTrue();
    }
}
