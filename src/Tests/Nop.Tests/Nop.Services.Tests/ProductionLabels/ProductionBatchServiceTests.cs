using AwesomeAssertions;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.ProductionLabels.Domain;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Services.Catalog;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ProductionLabels;

[TestFixture]
public class ProductionBatchServiceTests : ServiceTest
{
    private IProductionBatchService _productionBatchService;
    private IProductService _productService;

    [OneTimeSetUp]
    public void SetUp()
    {
        _productionBatchService = GetService<IProductionBatchService>();
        _productService = GetService<IProductService>();
    }

    private async Task<Product> CreateProductAsync(string name)
    {
        var product = new Product { Name = name, Published = true };
        await _productService.InsertProductAsync(product);

        return product;
    }

    private static ProductionBatch NewBatch(int productId, DateTime productionDateUtc, int quantity = 10) => new()
    {
        ProductId = productId,
        ProductionDateUtc = productionDateUtc,
        BestBeforeDateUtc = productionDateUtc.AddDays(30),
        Quantity = quantity
    };

    [Test]
    public async Task InsertProductionBatchAsync_GeneratesBatchCodeAndStampsCreatedOnUtc()
    {
        var product = await CreateProductAsync("Batch code format test product");
        var productionDate = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);

        var batch = NewBatch(product.Id, productionDate);
        await _productionBatchService.InsertProductionBatchAsync(batch);

        await _productionBatchService.DeleteProductionBatchAsync(batch);
        await _productService.DeleteProductAsync(product);

        batch.BatchCode.Should().Be("20260904-001");
        batch.CreatedOnUtc.Should().NotBe(default);
    }

    [Test]
    public async Task InsertProductionBatchAsync_SecondBatchSameProductSameDay_IncrementsCounter()
    {
        var product = await CreateProductAsync("Batch code counter test product");
        var productionDate = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);

        var first = NewBatch(product.Id, productionDate);
        await _productionBatchService.InsertProductionBatchAsync(first);
        var second = NewBatch(product.Id, productionDate);
        await _productionBatchService.InsertProductionBatchAsync(second);

        await _productionBatchService.DeleteProductionBatchAsync(first);
        await _productionBatchService.DeleteProductionBatchAsync(second);
        await _productService.DeleteProductAsync(product);

        second.BatchCode.Should().Be("20260904-002");
    }

    /// <summary>
    /// Regression-shaped test for the design's explicit MAX-not-COUNT choice: deleting the middle batch of
    /// a day and then inserting a new one must not collide with the remaining batch's code. A COUNT-based
    /// counter would produce "-002" again here (a real duplicate), since only one row remains at insert
    /// time.
    /// </summary>
    [Test]
    public async Task InsertProductionBatchAsync_AfterDeletingAMiddleBatch_DoesNotReuseAnExistingCode()
    {
        var product = await CreateProductAsync("Batch code no-collision test product");
        var productionDate = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);

        var first = NewBatch(product.Id, productionDate);
        await _productionBatchService.InsertProductionBatchAsync(first);
        var second = NewBatch(product.Id, productionDate);
        await _productionBatchService.InsertProductionBatchAsync(second);

        //delete the first (unlabeled, so deletable), leaving only "-002" in place
        await _productionBatchService.DeleteProductionBatchAsync(first);

        var third = NewBatch(product.Id, productionDate);
        await _productionBatchService.InsertProductionBatchAsync(third);

        await _productionBatchService.DeleteProductionBatchAsync(second);
        await _productionBatchService.DeleteProductionBatchAsync(third);
        await _productService.DeleteProductAsync(product);

        third.BatchCode.Should().Be("20260904-003");
    }

    [Test]
    public void InsertProductionBatchAsync_WhenBestBeforeNotAfterProductionDate_ThrowsNopException()
    {
        var productionDate = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);
        var batch = new ProductionBatch
        {
            ProductId = 1,
            ProductionDateUtc = productionDate,
            BestBeforeDateUtc = productionDate,
            Quantity = 10
        };

        Assert.ThrowsAsync<NopException>(async () => await _productionBatchService.InsertProductionBatchAsync(batch));
    }

    [Test]
    public void InsertProductionBatchAsync_WhenQuantityIsZero_ThrowsNopException()
    {
        var batch = NewBatch(1, DateTime.UtcNow, quantity: 0);

        Assert.ThrowsAsync<NopException>(async () => await _productionBatchService.InsertProductionBatchAsync(batch));
    }

    [Test]
    public void InsertProductionBatchAsync_WhenQuantityIsNegative_ThrowsNopException()
    {
        var batch = NewBatch(1, DateTime.UtcNow, quantity: -5);

        Assert.ThrowsAsync<NopException>(async () => await _productionBatchService.InsertProductionBatchAsync(batch));
    }

    [Test]
    public async Task DeleteProductionBatchAsync_WhenNotLabeled_Succeeds()
    {
        var product = await CreateProductAsync("Delete unlabeled test product");
        var batch = NewBatch(product.Id, DateTime.UtcNow);
        await _productionBatchService.InsertProductionBatchAsync(batch);

        await _productionBatchService.DeleteProductionBatchAsync(batch);

        var reloaded = await _productionBatchService.GetProductionBatchByIdAsync(batch.Id);

        await _productService.DeleteProductAsync(product);

        reloaded.Should().BeNull();
    }

    [Test]
    public async Task DeleteProductionBatchAsync_WhenLabeled_ThrowsNopExceptionAndLeavesRowIntact()
    {
        var product = await CreateProductAsync("Delete labeled test product");
        var batch = NewBatch(product.Id, DateTime.UtcNow);
        await _productionBatchService.InsertProductionBatchAsync(batch);
        await _productionBatchService.MarkLabelGeneratedAsync(batch);

        ProductionBatch reloaded;
        try
        {
            Assert.ThrowsAsync<NopException>(async () => await _productionBatchService.DeleteProductionBatchAsync(batch));
        }
        finally
        {
            reloaded = await _productionBatchService.GetProductionBatchByIdAsync(batch.Id);

            batch.LabelGeneratedOnUtc = null;
            await _productionBatchService.DeleteProductionBatchAsync(batch);
            await _productService.DeleteProductAsync(product);
        }

        reloaded.Should().NotBeNull();
        reloaded.LabelGeneratedOnUtc.Should().NotBeNull();
    }

    [Test]
    public async Task MarkLabelGeneratedAsync_StampsLabelGeneratedOnUtc()
    {
        var product = await CreateProductAsync("Mark label generated test product");
        var batch = NewBatch(product.Id, DateTime.UtcNow);
        await _productionBatchService.InsertProductionBatchAsync(batch);

        await _productionBatchService.MarkLabelGeneratedAsync(batch);

        var reloaded = await _productionBatchService.GetProductionBatchByIdAsync(batch.Id);
        var labelGeneratedOnUtc = reloaded.LabelGeneratedOnUtc;

        reloaded.LabelGeneratedOnUtc = null;
        await _productionBatchService.DeleteProductionBatchAsync(reloaded);
        await _productService.DeleteProductAsync(product);

        labelGeneratedOnUtc.Should().NotBeNull();
    }

    [Test]
    public async Task GetAllProductionBatchesAsync_ReturnsNewestFirst()
    {
        var product = await CreateProductAsync("Newest first test product");
        var older = NewBatch(product.Id, DateTime.UtcNow.AddDays(-2));
        await _productionBatchService.InsertProductionBatchAsync(older);
        var newer = NewBatch(product.Id, DateTime.UtcNow.AddDays(-1));
        await _productionBatchService.InsertProductionBatchAsync(newer);

        var page = await _productionBatchService.GetAllProductionBatchesAsync(product.Id);

        await _productionBatchService.DeleteProductionBatchAsync(older);
        await _productionBatchService.DeleteProductionBatchAsync(newer);
        await _productService.DeleteProductAsync(product);

        page.Select(batch => batch.Id).Should().ContainInOrder(newer.Id, older.Id);
    }

    [Test]
    public async Task GetAllProductionBatchesAsync_FiltersByProductId_WhenProvided()
    {
        var productA = await CreateProductAsync("Filter by product test product A");
        var productB = await CreateProductAsync("Filter by product test product B");
        var batchA = NewBatch(productA.Id, DateTime.UtcNow);
        await _productionBatchService.InsertProductionBatchAsync(batchA);
        var batchB = NewBatch(productB.Id, DateTime.UtcNow);
        await _productionBatchService.InsertProductionBatchAsync(batchB);

        var page = await _productionBatchService.GetAllProductionBatchesAsync(productA.Id);

        await _productionBatchService.DeleteProductionBatchAsync(batchA);
        await _productionBatchService.DeleteProductionBatchAsync(batchB);
        await _productService.DeleteProductAsync(productA);
        await _productService.DeleteProductAsync(productB);

        page.Select(batch => batch.Id).Should().BeEquivalentTo([batchA.Id]);
    }

    [Test]
    public async Task GetAllProductionBatchesAsync_WithNoProductIdFilter_ReturnsBatchesAcrossProducts()
    {
        var productA = await CreateProductAsync("Cross-product list test product A");
        var productB = await CreateProductAsync("Cross-product list test product B");
        var batchA = NewBatch(productA.Id, DateTime.UtcNow);
        await _productionBatchService.InsertProductionBatchAsync(batchA);
        var batchB = NewBatch(productB.Id, DateTime.UtcNow);
        await _productionBatchService.InsertProductionBatchAsync(batchB);

        var page = await _productionBatchService.GetAllProductionBatchesAsync();

        await _productionBatchService.DeleteProductionBatchAsync(batchA);
        await _productionBatchService.DeleteProductionBatchAsync(batchB);
        await _productService.DeleteProductAsync(productA);
        await _productService.DeleteProductAsync(productB);

        page.Select(batch => batch.Id).Should().Contain([batchA.Id, batchB.Id]);
    }
}
