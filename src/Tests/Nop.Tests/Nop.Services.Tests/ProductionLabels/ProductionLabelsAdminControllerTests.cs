using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.ProductionLabels;
using Nop.Plugin.Misc.ProductionLabels.Admin.Controllers;
using Nop.Plugin.Misc.ProductionLabels.Admin.Factories;
using Nop.Plugin.Misc.ProductionLabels.Admin.Models;
using Nop.Plugin.Misc.ProductionLabels.Domain;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Plugin.Misc.ProductionLabels.Services.Pdf;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework.Factories;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ProductionLabels;

/// <summary>
/// Exercises ProductionLabelsAdminController's two behaviourally-interesting actions directly:
/// ProductionBatchDelete's catch-NopException-and-notify path (mirrors IngredientsAdminController's
/// composition-delete pattern), and GenerateLabel's stamp-only-after-a-successful-render ordering.
/// IProductionLabelModelFactory and IHtmlToPdfConverter are mocked - the former because label-content
/// correctness is already exhaustively covered by ProductionLabelModelFactoryTests and this test's own job
/// is only to prove the controller's sequencing; the latter because no concrete implementation exists yet
/// (out of scope for this unit - see spec Section 13). IProductionBatchService is real (from the DI
/// container/SQLite), so the stamp assertion re-queries genuine persisted state, not a mock's own memory.
/// </summary>
[TestFixture]
public class ProductionLabelsAdminControllerTests : ServiceTest
{
    private IGenericAttributeService _genericAttributeService;
    private ILocalizationService _localizationService;
    private IProductionBatchService _productionBatchService;
    private IProductService _productService;

    private Mock<IHtmlToPdfConverter> _htmlToPdfConverterMock;
    private Mock<IProductionLabelModelFactory> _productionLabelModelFactoryMock;
    private Mock<INotificationService> _notificationServiceMock;
    private ProductionLabelsAdminModelFactory _productionLabelsAdminModelFactory;
    private TestableProductionLabelsAdminController _controller;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _genericAttributeService = GetService<IGenericAttributeService>();
        _localizationService = GetService<ILocalizationService>();
        _productionBatchService = GetService<IProductionBatchService>();
        _productService = GetService<IProductService>();
    }

    [SetUp]
    public void SetUp()
    {
        _htmlToPdfConverterMock = new Mock<IHtmlToPdfConverter>();
        _productionLabelModelFactoryMock = new Mock<IProductionLabelModelFactory>();
        _notificationServiceMock = new Mock<INotificationService>();

        _productionLabelsAdminModelFactory = new ProductionLabelsAdminModelFactory(
            _genericAttributeService,
            GetService<ILanguageService>(),
            _localizationService,
            GetService<ILocalizedModelFactory>(),
            _productionBatchService,
            _productService,
            GetService<IStoreContext>());

        _controller = new TestableProductionLabelsAdminController(
            _genericAttributeService,
            _htmlToPdfConverterMock.Object,
            _localizationService,
            _notificationServiceMock.Object,
            _productionBatchService,
            _productionLabelModelFactoryMock.Object,
            _productService,
            _productionLabelsAdminModelFactory)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = ServiceProvider }
            }
        };
    }

    private async Task<Product> CreateProductAsync(string name)
    {
        var product = new Product { Name = name, Published = true };
        await _productService.InsertProductAsync(product);

        return product;
    }

    private async Task<ProductionBatch> CreateBatchAsync(int productId)
    {
        var batch = new ProductionBatch
        {
            ProductId = productId,
            ProductionDateUtc = DateTime.UtcNow,
            BestBeforeDateUtc = DateTime.UtcNow.AddDays(30),
            Quantity = 10
        };
        await _productionBatchService.InsertProductionBatchAsync(batch);

        return batch;
    }

    [Test]
    public async Task ProductionBatchDelete_WhenAlreadyLabeled_ReturnsFailureJsonAndShowsErrorNotification()
    {
        var product = await CreateProductAsync("Controller test - delete labeled product");
        var batch = await CreateBatchAsync(product.Id);
        await _productionBatchService.MarkLabelGeneratedAsync(batch);

        var result = await _controller.ProductionBatchDelete(batch.Id);

        batch.LabelGeneratedOnUtc = null;
        await _productionBatchService.DeleteProductionBatchAsync(batch);
        await _productService.DeleteProductAsync(product);

        result.Should().BeOfType<JsonResult>();
        _notificationServiceMock.Verify(x => x.ErrorNotification(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Once);
    }

    [Test]
    public async Task ProductionBatchDelete_WhenNotLabeled_Succeeds_WithoutErrorNotification()
    {
        var product = await CreateProductAsync("Controller test - delete unlabeled product");
        var batch = await CreateBatchAsync(product.Id);

        await _controller.ProductionBatchDelete(batch.Id);

        var reloaded = await _productionBatchService.GetProductionBatchByIdAsync(batch.Id);

        await _productService.DeleteProductAsync(product);

        reloaded.Should().BeNull();
        _notificationServiceMock.Verify(x => x.ErrorNotification(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GenerateLabel_OnSuccessfulConversion_StampsLabelGeneratedOnUtcAndReturnsTheFile()
    {
        var product = await CreateProductAsync("Controller test - generate label success product");
        var batch = await CreateBatchAsync(product.Id);

        _productionLabelModelFactoryMock
            .Setup(x => x.PrepareProductionLabelModelAsync(It.IsAny<ProductionBatch>(), It.IsAny<int>(), It.IsAny<ProductionLabelSizeVariant>()))
            .ReturnsAsync(new ProductionLabelModel { BatchCode = batch.BatchCode });
        _htmlToPdfConverterMock.Setup(x => x.ConvertAsync(It.IsAny<string>())).ReturnsAsync(new byte[] { 1, 2, 3 });

        var model = new GenerateProductionLabelModel { ProductionBatchId = batch.Id, SizeVariant = ProductionLabelSizeVariant.SmallJar, LanguageId = 1 };

        var result = await _controller.GenerateLabel(model);

        var reloaded = await _productionBatchService.GetProductionBatchByIdAsync(batch.Id);
        var labelGeneratedOnUtc = reloaded.LabelGeneratedOnUtc;

        reloaded.LabelGeneratedOnUtc = null;
        await _productionBatchService.DeleteProductionBatchAsync(reloaded);
        await _productService.DeleteProductAsync(product);

        result.Should().BeOfType<FileContentResult>();
        labelGeneratedOnUtc.Should().NotBeNull();
    }

    /// <summary>
    /// The stamp-only-after-success regression case: a conversion failure must leave the batch unlocked
    /// and deletable, since no real label was produced.
    /// </summary>
    [Test]
    public async Task GenerateLabel_WhenConversionFails_DoesNotStampLabelGeneratedOnUtc()
    {
        var product = await CreateProductAsync("Controller test - generate label failure product");
        var batch = await CreateBatchAsync(product.Id);

        _productionLabelModelFactoryMock
            .Setup(x => x.PrepareProductionLabelModelAsync(It.IsAny<ProductionBatch>(), It.IsAny<int>(), It.IsAny<ProductionLabelSizeVariant>()))
            .ReturnsAsync(new ProductionLabelModel { BatchCode = batch.BatchCode });
        _htmlToPdfConverterMock.Setup(x => x.ConvertAsync(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("renderer failed"));

        var model = new GenerateProductionLabelModel { ProductionBatchId = batch.Id, SizeVariant = ProductionLabelSizeVariant.SmallJar, LanguageId = 1 };

        DateTime? labelGeneratedOnUtc;
        try
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _controller.GenerateLabel(model));
        }
        finally
        {
            var reloaded = await _productionBatchService.GetProductionBatchByIdAsync(batch.Id);
            labelGeneratedOnUtc = reloaded.LabelGeneratedOnUtc;

            reloaded.LabelGeneratedOnUtc = null;
            await _productionBatchService.DeleteProductionBatchAsync(reloaded);
            await _productService.DeleteProductAsync(product);
        }

        labelGeneratedOnUtc.Should().BeNull();
    }

    [Test]
    public async Task SaveProductInfo_WhenLocalesArePosted_WritesPerLanguageGenericAttributes()
    {
        var product = await CreateProductAsync("Controller test - save product info multi-language product");

        var model = new ProductionLabelsProductModel
        {
            ProductId = product.Id,
            Locales = new List<ProductionLabelsProductLocalizedModel>
            {
                new() { LanguageId = 1, StorageConditions = "Keep refrigerated (lang 1)", CountryOfOrigin = "Poland (lang 1)" },
                new() { LanguageId = 2, StorageConditions = "Keep refrigerated (lang 2)", CountryOfOrigin = "Poland (lang 2)" }
            }
        };

        await _controller.SaveProductInfo(model);

        var storageLang1 = await _genericAttributeService.GetAttributeAsync<string>(product, ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + 1);
        var originLang1 = await _genericAttributeService.GetAttributeAsync<string>(product, ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + 1);
        var storageLang2 = await _genericAttributeService.GetAttributeAsync<string>(product, ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + 2);
        var originLang2 = await _genericAttributeService.GetAttributeAsync<string>(product, ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + 2);

        await _productService.DeleteProductAsync(product);

        storageLang1.Should().Be("Keep refrigerated (lang 1)");
        originLang1.Should().Be("Poland (lang 1)");
        storageLang2.Should().Be("Keep refrigerated (lang 2)");
        originLang2.Should().Be("Poland (lang 2)");
    }

    /// <summary>
    /// The regression case: Html.LocalizedEditorAsync renders the standard (non-tabbed) template - posting
    /// the flat StorageConditions/CountryOfOrigin fields directly, not Locales[i].* - whenever at most one
    /// language is configured (Nop.Web.Framework/Extensions/HtmlExtensions.cs:46), so the model binder
    /// leaves Locales empty on this path. Before this fix, that silently dropped the save entirely.
    /// </summary>
    [Test]
    public async Task SaveProductInfo_WhenLocalesAreEmpty_WritesFlatPropertiesAgainstTheResolvedDefaultLanguage()
    {
        var product = await CreateProductAsync("Controller test - save product info single-language product");
        var defaultLanguageId = await _productionLabelsAdminModelFactory.ResolveCurrentStoreDefaultLanguageIdAsync();

        var model = new ProductionLabelsProductModel
        {
            ProductId = product.Id,
            StorageConditions = "Keep cool and dry",
            CountryOfOrigin = "Poland",
            Locales = new List<ProductionLabelsProductLocalizedModel>()
        };

        await _controller.SaveProductInfo(model);

        var storage = await _genericAttributeService.GetAttributeAsync<string>(product, ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + defaultLanguageId);
        var origin = await _genericAttributeService.GetAttributeAsync<string>(product, ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + defaultLanguageId);

        await _productService.DeleteProductAsync(product);

        storage.Should().Be("Keep cool and dry");
        origin.Should().Be("Poland");
    }

    /// <summary>
    /// Test-only subclass overriding the HTML-render seam so this suite does not need a real Razor view
    /// engine (not registered in this test harness - see BaseNopTest's own hand-registered service list).
    /// </summary>
    private class TestableProductionLabelsAdminController : ProductionLabelsAdminController
    {
        public TestableProductionLabelsAdminController(IGenericAttributeService genericAttributeService,
            IHtmlToPdfConverter htmlToPdfConverter,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IProductionBatchService productionBatchService,
            IProductionLabelModelFactory productionLabelModelFactory,
            IProductService productService,
            ProductionLabelsAdminModelFactory productionLabelsAdminModelFactory)
            : base(genericAttributeService, htmlToPdfConverter, localizationService, notificationService,
                productionBatchService, productionLabelModelFactory, productService, productionLabelsAdminModelFactory)
        {
        }

        protected override Task<string> RenderProductionLabelHtmlAsync(ProductionLabelModel model)
        {
            return Task.FromResult("<html><body>test label</body></html>");
        }
    }
}
