using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Plugin.Misc.ServingSuggestions.Public.Components;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Media;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ServingSuggestions;

/// <summary>
/// Exercises the storefront rendering path itself - spec section 11's first two required scenarios
/// ("a product with a serving suggestion renders it in step order" and "a product with no serving
/// suggestion renders nothing extra"). ServingSuggestionViewComponent is not registered in the DI
/// container the test harness builds (view components aren't a service the rest of the app resolves by
/// interface), so it is constructed directly here; its constructor takes only already-registered service
/// interfaces.
/// </summary>
[TestFixture]
public class ServingSuggestionViewComponentTests : ServiceTest
{
    private IServingSuggestionService _servingSuggestionService;
    private ILocalizationService _localizationService;
    private IPictureService _pictureService;
    private IProductService _productService;
    private ServingSuggestionViewComponent _servingSuggestionViewComponent;

    [OneTimeSetUp]
    public void SetUp()
    {
        _servingSuggestionService = GetService<IServingSuggestionService>();
        _localizationService = GetService<ILocalizationService>();
        _pictureService = GetService<IPictureService>();
        _productService = GetService<IProductService>();

        _servingSuggestionViewComponent = new ServingSuggestionViewComponent(_localizationService, _pictureService, _servingSuggestionService);
    }

    private async Task<Product> CreateProductAsync(string name)
    {
        var product = new Product { Name = name, Published = true };
        await _productService.InsertProductAsync(product);

        return product;
    }

    [Test]
    public async Task PrepareServingSuggestionModelAsync_ReturnsNull_WhenTheProductHasNoServingSuggestion()
    {
        var product = await CreateProductAsync("View component test - no serving suggestion");

        var model = await _servingSuggestionViewComponent.PrepareServingSuggestionModelAsync(product.Id);

        await _productService.DeleteProductAsync(product);

        model.Should().BeNull();
    }

    [Test]
    public async Task PrepareServingSuggestionModelAsync_RendersTitleDescriptionPictureAndStepsInOrder()
    {
        var product = await CreateProductAsync("View component test - with serving suggestion");
        var picture = await _pictureService.InsertPictureAsync([1, 2, 3], "image/png", "view-component-test", validateBinary: false);

        var servingSuggestion = new ServingSuggestion
        {
            ProductId = product.Id,
            Title = "Serve warm",
            Description = "Best enjoyed fresh",
            PictureId = picture.Id
        };
        await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestion);

        var stepB = new ServingSuggestionStep { ServingSuggestionId = servingSuggestion.Id, Text = "Add a slice of lemon", DisplayOrder = 2 };
        var stepA = new ServingSuggestionStep { ServingSuggestionId = servingSuggestion.Id, Text = "Warm the jar gently", DisplayOrder = 1 };
        await _servingSuggestionService.InsertServingSuggestionStepAsync(stepB);
        await _servingSuggestionService.InsertServingSuggestionStepAsync(stepA);

        var model = await _servingSuggestionViewComponent.PrepareServingSuggestionModelAsync(product.Id);

        await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);
        await _productService.DeleteProductAsync(product);

        model.Should().NotBeNull();
        model.Title.Should().Be("Serve warm");
        model.Description.Should().Be("Best enjoyed fresh");
        model.PictureUrl.Should().NotBeNullOrEmpty();
        model.Steps.Select(step => step.Text).Should().ContainInOrder("Warm the jar gently", "Add a slice of lemon");
    }
}
