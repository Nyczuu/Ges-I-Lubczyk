using AwesomeAssertions;
using Nop.Plugin.Misc.Ingredients.Admin.Models;
using Nop.Plugin.Misc.Ingredients.Admin.Validators;
using Nop.Services.Localization;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Ingredients;

/// <summary>
/// Nutritional values are required (non-nullable decimal properties on IngredientModel), so a blank
/// submission is rejected by model binding before FluentValidation ever runs (see IngredientValidator's
/// own comments) - these tests cover what the validator itself is responsible for: rejecting a negative
/// value per field, and accepting a genuine all-zero submission (e.g. water), per spec section 11.
/// </summary>
[TestFixture]
public class IngredientValidatorTests : ServiceTest
{
    private IngredientValidator _validator;

    [OneTimeSetUp]
    public void SetUp()
    {
        _validator = new IngredientValidator(GetService<ILocalizationService>());
    }

    private static IngredientModel CreateValidModel()
    {
        return new IngredientModel
        {
            Name = "Water",
            CaloriesPer100g = 0,
            ProteinPer100g = 0,
            FatPer100g = 0,
            CarbohydratePer100g = 0
        };
    }

    [Test]
    public void Validate_Fails_WhenCaloriesPer100gIsNegative()
    {
        var model = CreateValidModel();
        model.CaloriesPer100g = -1;

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(IngredientModel.CaloriesPer100g));
    }

    [Test]
    public void Validate_Fails_WhenProteinPer100gIsNegative()
    {
        var model = CreateValidModel();
        model.ProteinPer100g = -1;

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(IngredientModel.ProteinPer100g));
    }

    [Test]
    public void Validate_Fails_WhenFatPer100gIsNegative()
    {
        var model = CreateValidModel();
        model.FatPer100g = -1;

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(IngredientModel.FatPer100g));
    }

    [Test]
    public void Validate_Fails_WhenCarbohydratePer100gIsNegative()
    {
        var model = CreateValidModel();
        model.CarbohydratePer100g = -1;

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(IngredientModel.CarbohydratePer100g));
    }

    [Test]
    public void Validate_Succeeds_WhenAllNutritionalValuesAreZero()
    {
        var model = CreateValidModel();

        var result = _validator.Validate(model);

        result.IsValid.Should().BeTrue();
    }
}
