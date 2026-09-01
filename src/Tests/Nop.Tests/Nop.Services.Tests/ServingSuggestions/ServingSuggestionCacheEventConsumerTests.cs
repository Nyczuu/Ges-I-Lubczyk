using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Catalog;
using Nop.Services.Media;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ServingSuggestions;

[TestFixture]
public class ServingSuggestionCacheEventConsumerTests : ServiceTest
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
    public async Task UpdatingAServingSuggestion_InvalidatesItsByIdCacheEntry()
    {
        var product = new Product { Name = "Cache test product", Published = true };
        await _productService.InsertProductAsync(product);

        var picture = await _pictureService.InsertPictureAsync([1, 2, 3], "image/png", "cache-test", validateBinary: false);

        var servingSuggestion = new ServingSuggestion
        {
            ProductId = product.Id,
            Title = "Cache test - original title",
            Description = "Description",
            PictureId = picture.Id
        };
        await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestion);

        //warm the by-id cache
        await _servingSuggestionService.GetServingSuggestionByIdAsync(servingSuggestion.Id);

        //UpdateServingSuggestionAsync publishes EntityUpdatedEvent<ServingSuggestion> by default, which
        //ServingSuggestionCacheEventConsumer (CacheEventConsumer<ServingSuggestion>) should react to by
        //invalidating the cached entry
        servingSuggestion.Title = "Cache test - updated title";
        await _servingSuggestionService.UpdateServingSuggestionAsync(servingSuggestion);

        var reloaded = await _servingSuggestionService.GetServingSuggestionByIdAsync(servingSuggestion.Id);

        await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);
        await _productService.DeleteProductAsync(product);

        reloaded.Title.Should().Be("Cache test - updated title");
    }
}
