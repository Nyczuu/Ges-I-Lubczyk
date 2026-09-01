using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Catalog;
using Nop.Services.Media;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ServingSuggestions;

[TestFixture]
public class ServingSuggestionStepCacheEventConsumerTests : ServiceTest
{
    private IServingSuggestionService _servingSuggestionService;
    private IPictureService _pictureService;
    private IProductService _productService;

    [OneTimeSetUp]
    public void SetUp()
    {
        _servingSuggestionService = GetService<IServingSuggestionService>();
        _pictureService = GetService<IPictureService>();
        _productService = GetService<IProductService>();
    }

    [Test]
    public async Task UpdatingAServingSuggestionStep_InvalidatesItsByIdCacheEntry()
    {
        var product = new Product { Name = "Step cache test product", Published = true };
        await _productService.InsertProductAsync(product);

        var picture = await _pictureService.InsertPictureAsync([1, 2, 3], "image/png", "step-cache-test", validateBinary: false);

        var servingSuggestion = new ServingSuggestion
        {
            ProductId = product.Id,
            Title = "Step cache test title",
            Description = "Description",
            PictureId = picture.Id
        };
        await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestion);

        var step = new ServingSuggestionStep { ServingSuggestionId = servingSuggestion.Id, Text = "Step cache test - original text" };
        await _servingSuggestionService.InsertServingSuggestionStepAsync(step);

        //warm the by-id cache
        await _servingSuggestionService.GetServingSuggestionStepByIdAsync(step.Id);

        //UpdateServingSuggestionStepAsync publishes EntityUpdatedEvent<ServingSuggestionStep> by default,
        //which ServingSuggestionStepCacheEventConsumer (CacheEventConsumer<ServingSuggestionStep>) should
        //react to by invalidating the cached entry
        step.Text = "Step cache test - updated text";
        await _servingSuggestionService.UpdateServingSuggestionStepAsync(step);

        var reloaded = await _servingSuggestionService.GetServingSuggestionStepByIdAsync(step.Id);

        await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);
        await _productService.DeleteProductAsync(product);

        reloaded.Text.Should().Be("Step cache test - updated text");
    }
}
