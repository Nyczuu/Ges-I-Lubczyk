using AwesomeAssertions;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Models;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Validators;
using Nop.Services.Localization;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ServingSuggestions;

/// <summary>
/// This is where spec section 11's "creating a serving suggestion without an image is rejected by
/// admin-form validation" is actually enforced: PictureId is only ever set by the admin controller once a
/// picture has been uploaded (new) or carried over (existing) - see ServingSuggestionController -, so a
/// submission with no picture stays at 0 and this rule catches it before the write happens.
/// </summary>
[TestFixture]
public class ServingSuggestionValidatorTests : ServiceTest
{
    private ServingSuggestionValidator _validator;

    [OneTimeSetUp]
    public void SetUp()
    {
        _validator = new ServingSuggestionValidator(GetService<ILocalizationService>());
    }

    [Test]
    public void Validate_Fails_WhenNoPictureHasBeenProvided()
    {
        var model = new ServingSuggestionModel { Title = "Serve chilled", Description = "Best served cold", PictureId = 0 };

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ServingSuggestionModel.PictureId));
    }

    [Test]
    public void Validate_Fails_WhenTitleIsEmpty()
    {
        var model = new ServingSuggestionModel { Title = string.Empty, Description = "Best served cold", PictureId = 1 };

        var result = _validator.Validate(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ServingSuggestionModel.Title));
    }

    [Test]
    public void Validate_Succeeds_WhenTitleAndPictureAreProvided()
    {
        var model = new ServingSuggestionModel { Title = "Serve chilled", Description = "Best served cold", PictureId = 1 };

        var result = _validator.Validate(model);

        result.IsValid.Should().BeTrue();
    }
}
