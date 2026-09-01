using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Plugin.Misc.ServingSuggestions.Services.Events;
using Nop.Services.Catalog;
using Nop.Services.Media;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ServingSuggestions;

/// <summary>
/// Exercises the consumer directly (no Ingredients precedent - GIL-001 has zero product-deletion
/// consumers, see the ddd-modeler design's Pass 2 point 3). Product is ISoftDeletedEntity, so its own
/// DeleteProductAsync never issues a physical DELETE, but EntityRepository.DeleteAsync always publishes
/// EntityDeletedEvent&lt;Product&gt; regardless - the consumer relies on that event firing, not on the row
/// actually being removed.
/// </summary>
[TestFixture]
public class ServingSuggestionProductDeletedEventConsumerTests : ServiceTest
{
    private IServingSuggestionService _servingSuggestionService;
    private IPictureService _pictureService;
    private IProductService _productService;
    private IRepository<Picture> _pictureRepository;
    private ServingSuggestionProductDeletedEventConsumer _consumer;

    [OneTimeSetUp]
    public void SetUp()
    {
        _servingSuggestionService = GetService<IServingSuggestionService>();
        _pictureService = GetService<IPictureService>();
        _productService = GetService<IProductService>();
        _pictureRepository = GetService<IRepository<Picture>>();

        _consumer = new ServingSuggestionProductDeletedEventConsumer(_servingSuggestionService);
    }

    [Test]
    public async Task HandleEventAsync_RemovesTheServingSuggestionAndItsPicture_WhenTheProductHadOne()
    {
        var product = new Product { Name = "Consumer test product with serving suggestion", Published = true };
        await _productService.InsertProductAsync(product);

        var picture = await _pictureService.InsertPictureAsync([1, 2, 3], "image/png", "consumer-test", validateBinary: false);
        var servingSuggestion = new ServingSuggestion { ProductId = product.Id, Title = "Title", Description = "Description", PictureId = picture.Id };
        await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestion);

        await _consumer.HandleEventAsync(new EntityDeletedEvent<Product>(product));

        var reloadedServingSuggestion = await _servingSuggestionService.GetServingSuggestionByProductIdAsync(product.Id);
        //queried through IRepository<Picture> directly, not IPictureService.GetPictureByIdAsync - see
        //ServingSuggestionServiceTests for why (this isolated test project doesn't reference the one
        //plugin, Nop.Plugin.Misc.AzureBlob, that happens to invalidate Picture's by-id cache, so that
        //cached read would return the pre-delete object once warmed by the consumer's own lookup)
        var reloadedPicture = await _pictureRepository.GetByIdAsync(picture.Id);

        await _productService.DeleteProductAsync(product);

        reloadedServingSuggestion.Should().BeNull();
        reloadedPicture.Should().BeNull();
    }

    [Test]
    public async Task HandleEventAsync_DoesNothing_WhenTheProductHadNoServingSuggestion()
    {
        var product = new Product { Name = "Consumer test product without serving suggestion", Published = true };
        await _productService.InsertProductAsync(product);

        //no serving suggestion inserted - this must not throw
        await _consumer.HandleEventAsync(new EntityDeletedEvent<Product>(product));

        await _productService.DeleteProductAsync(product);

        Assert.Pass();
    }

    [Test]
    public async Task HandleEventAsync_DoesNothing_WhenTheEventPayloadIsNull()
    {
        //must not throw - defensive null-guard on the event payload
        await _consumer.HandleEventAsync(null);

        Assert.Pass();
    }
}
