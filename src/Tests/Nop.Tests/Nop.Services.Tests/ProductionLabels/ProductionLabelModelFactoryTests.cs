using System.Linq.Expressions;
using AwesomeAssertions;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Plugin.Misc.ProductionLabels;
using Nop.Plugin.Misc.ProductionLabels.Domain;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using NUnit.Framework;
using Npgsql;

namespace Nop.Tests.Nop.Services.Tests.ProductionLabels;

/// <summary>
/// NUnit + Moq, deliberately not a ServiceTest/SQLite fixture - the graceful-degrade scenario needs to
/// simulate a PostgresException a real SQLite fixture can't produce.
/// </summary>
[TestFixture]
public class ProductionLabelModelFactoryTests
{
    private Mock<IGenericAttributeService> _genericAttributeServiceMock;
    private Mock<IIngredientService> _ingredientServiceMock;
    private Mock<ILocalizationService> _localizationServiceMock;
    private Mock<ILogger> _loggerMock;
    private Mock<IMeasureService> _measureServiceMock;
    private Mock<IProductIngredientService> _productIngredientServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IStoreContext> _storeContextMock;
    private MeasureSettings _measureSettings;

    private Product _product;
    private Store _store;
    private ProductionBatch _batch;

    private const int DefaultLanguageId = 1;

    [SetUp]
    public void SetUp()
    {
        _genericAttributeServiceMock = new Mock<IGenericAttributeService>();
        _ingredientServiceMock = new Mock<IIngredientService>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _loggerMock = new Mock<ILogger>();
        _measureServiceMock = new Mock<IMeasureService>();
        _productIngredientServiceMock = new Mock<IProductIngredientService>();
        _productServiceMock = new Mock<IProductService>();
        _storeContextMock = new Mock<IStoreContext>();
        _measureSettings = new MeasureSettings { BaseWeightId = 7 };

        _product = new Product { Id = 42, Name = "Default product name", Weight = 0.25m };
        _store = new Store { Id = 1, CompanyName = "Ges I Lubczyk", CompanyAddress = "Warsaw, Poland", CompanyPhoneNumber = "+48 000 000 000" };
        _batch = new ProductionBatch { Id = 100, ProductId = _product.Id, BatchCode = "20260904-001", BestBeforeDateUtc = new DateTime(2027, 1, 1) };

        _productServiceMock.Setup(x => x.GetProductByIdAsync(_product.Id)).ReturnsAsync(_product);
        _storeContextMock.Setup(x => x.GetCurrentStoreAsync()).ReturnsAsync(_store);
        _measureServiceMock.Setup(x => x.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId)).ReturnsAsync(new MeasureWeight { Id = 7, Name = "g" });

        //by default: no ingredients, no storage/origin text - individual tests override as needed
        _productIngredientServiceMock.Setup(x => x.GetDirectIngredientsByProductIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Ingredient>());
        _productIngredientServiceMock.Setup(x => x.GetCompositionsReachableFromAsync(It.IsAny<IList<int>>())).ReturnsAsync(new List<IngredientComposition>());
        _ingredientServiceMock.Setup(x => x.GetIngredientsByIdsAsync(It.IsAny<int[]>())).ReturnsAsync(new List<Ingredient>());

        _genericAttributeServiceMock
            .Setup(x => x.GetAttributeAsync(It.IsAny<BaseEntity>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((string)null);

        //echoes back whatever the entity's own Name property already holds - test data sets Name directly
        //to the value that should be considered "the localized value for this call's languageId"
        SetUpLocalizedNameEcho<Product>();
        SetUpLocalizedNameEcho<Ingredient>();
    }

    private void SetUpLocalizedNameEcho<TEntity>() where TEntity : BaseEntity, ILocalizedEntity
    {
        _localizationServiceMock
            .Setup(x => x.GetLocalizedAsync(It.IsAny<TEntity>(), It.IsAny<Expression<Func<TEntity, string>>>(),
                It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns<TEntity, Expression<Func<TEntity, string>>, int?, bool, bool>(
                (entity, selector, languageId, returnDefault, ensureTwo) => Task.FromResult(selector.Compile().Invoke(entity)));
    }

    private ProductionLabelModelFactory CreateFactory() => new(
        _genericAttributeServiceMock.Object,
        _ingredientServiceMock.Object,
        _localizationServiceMock.Object,
        _loggerMock.Object,
        _measureServiceMock.Object,
        _productIngredientServiceMock.Object,
        _productServiceMock.Object,
        _storeContextMock.Object,
        _measureSettings);

    [Test]
    public async Task PrepareProductionLabelModelAsync_NormalCase_AssemblesExpectedFields()
    {
        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        model.ProductName.Should().Be("Default product name");
        model.NetQuantity.Should().Be("0.25 g");
        model.BatchCode.Should().Be("20260904-001");
        model.BestBeforeDateUtc.Should().Be(_batch.BestBeforeDateUtc);
        model.CompanyName.Should().Be("Ges I Lubczyk");
        model.CompanyAddress.Should().Be("Warsaw, Poland");
        model.CompanyPhoneNumber.Should().Be("+48 000 000 000");
        model.SizeVariant.Should().Be(ProductionLabelSizeVariant.SmallJar);
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_ZeroWeight_RendersEmptyNetQuantity()
    {
        _product.Weight = decimal.Zero;

        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        model.NetQuantity.Should().BeEmpty();
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_ZeroIngredients_RendersEmptyIngredientsList()
    {
        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        model.Ingredients.Should().BeEmpty();
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_CompositeIngredientExpandsInlineWithNestedChildren()
    {
        var beefBroth = new Ingredient { Id = 1, Name = "Beef broth" };
        var bones = new Ingredient { Id = 2, Name = "Bones" };
        var water = new Ingredient { Id = 3, Name = "Water" };

        _productIngredientServiceMock.Setup(x => x.GetDirectIngredientsByProductIdAsync(_product.Id))
            .ReturnsAsync(new List<Ingredient> { beefBroth });
        _productIngredientServiceMock.Setup(x => x.GetCompositionsReachableFromAsync(It.IsAny<IList<int>>()))
            .ReturnsAsync(new List<IngredientComposition>
            {
                new() { ParentIngredientId = beefBroth.Id, ChildIngredientId = bones.Id, DisplayOrder = 0 },
                new() { ParentIngredientId = beefBroth.Id, ChildIngredientId = water.Id, DisplayOrder = 1 }
            });
        _ingredientServiceMock.Setup(x => x.GetIngredientsByIdsAsync(It.IsAny<int[]>()))
            .ReturnsAsync(new List<Ingredient> { beefBroth, bones, water });

        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        model.Ingredients.Should().ContainSingle();
        var root = model.Ingredients[0];
        root.Name.Should().Be("Beef broth");
        root.Children.Should().HaveCount(2);
        root.Children.Select(c => c.Name).Should().ContainInOrder("Bones", "Water");
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_RootOrderingFollowsProductIngredientMappingDisplayOrder()
    {
        //GetDirectIngredientsByProductIdAsync already returns DisplayOrder-sorted results (per its own
        //contract) - the factory must preserve that order, not re-sort or use insertion order
        var second = new Ingredient { Id = 1, Name = "Second by insertion, first by display order" };
        var first = new Ingredient { Id = 2, Name = "First by insertion, second by display order" };

        _productIngredientServiceMock.Setup(x => x.GetDirectIngredientsByProductIdAsync(_product.Id))
            .ReturnsAsync(new List<Ingredient> { second, first });

        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        model.Ingredients.Select(i => i.Name).Should().ContainInOrder(second.Name, first.Name);
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_WithinCompositeOrderingFollowsIngredientCompositionDisplayOrder()
    {
        var parent = new Ingredient { Id = 1, Name = "Parent" };
        var childA = new Ingredient { Id = 2, Name = "Child A (display order 0)" };
        var childB = new Ingredient { Id = 3, Name = "Child B (display order 1)" };

        _productIngredientServiceMock.Setup(x => x.GetDirectIngredientsByProductIdAsync(_product.Id))
            .ReturnsAsync(new List<Ingredient> { parent });
        //the reachable-edges list is itself already DisplayOrder-sorted (its own real contract) - the
        //factory groups rather than re-sorts, distinct from the root-ordering scenario above
        _productIngredientServiceMock.Setup(x => x.GetCompositionsReachableFromAsync(It.IsAny<IList<int>>()))
            .ReturnsAsync(new List<IngredientComposition>
            {
                new() { ParentIngredientId = parent.Id, ChildIngredientId = childA.Id, DisplayOrder = 0 },
                new() { ParentIngredientId = parent.Id, ChildIngredientId = childB.Id, DisplayOrder = 1 }
            });
        _ingredientServiceMock.Setup(x => x.GetIngredientsByIdsAsync(It.IsAny<int[]>()))
            .ReturnsAsync(new List<Ingredient> { parent, childA, childB });

        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        model.Ingredients[0].Children.Select(c => c.Name).Should().ContainInOrder(childA.Name, childB.Name);
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_EachNodeCarriesCorrectAllergenType()
    {
        var parent = new Ingredient { Id = 1, Name = "Wheat flour blend", Allergen = AllergenType.CerealsContainingGluten };
        var child = new Ingredient { Id = 2, Name = "Salt", Allergen = AllergenType.None };
        var grandchild = new Ingredient { Id = 3, Name = "Milk powder", Allergen = AllergenType.Milk };

        _productIngredientServiceMock.Setup(x => x.GetDirectIngredientsByProductIdAsync(_product.Id))
            .ReturnsAsync(new List<Ingredient> { parent });
        _productIngredientServiceMock.Setup(x => x.GetCompositionsReachableFromAsync(It.IsAny<IList<int>>()))
            .ReturnsAsync(new List<IngredientComposition>
            {
                new() { ParentIngredientId = parent.Id, ChildIngredientId = child.Id, DisplayOrder = 0 },
                new() { ParentIngredientId = child.Id, ChildIngredientId = grandchild.Id, DisplayOrder = 0 }
            });
        _ingredientServiceMock.Setup(x => x.GetIngredientsByIdsAsync(It.IsAny<int[]>()))
            .ReturnsAsync(new List<Ingredient> { parent, child, grandchild });

        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        var rootNode = model.Ingredients[0];
        var childNode = rootNode.Children[0];
        var grandchildNode = childNode.Children[0];

        rootNode.AllergenType.Should().Be(AllergenType.CerealsContainingGluten);
        childNode.AllergenType.Should().Be(AllergenType.None);
        grandchildNode.AllergenType.Should().Be(AllergenType.Milk);
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_BlankStorageAndOrigin_RendersWithoutBlockingGeneration()
    {
        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        model.StorageConditions.Should().BeNullOrEmpty();
        model.CountryOfOrigin.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_StorageAndOriginContainingHtml_ArePassedThroughAsLiteralText()
    {
        const string maliciousStorage = "<script>alert('storage')</script>";
        const string maliciousOrigin = "<b>Poland</b>";

        _genericAttributeServiceMock
            .Setup(x => x.GetAttributeAsync(_product, ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + DefaultLanguageId, 0, (string)null))
            .ReturnsAsync(maliciousStorage);
        _genericAttributeServiceMock
            .Setup(x => x.GetAttributeAsync(_product, ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + DefaultLanguageId, 0, (string)null))
            .ReturnsAsync(maliciousOrigin);

        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        //the factory must not encode/strip this itself - it is carried through verbatim, and only the
        //view's own default (non-Html.Raw) @-encoding is responsible for rendering it as literal text
        model.StorageConditions.Should().Be(maliciousStorage);
        model.CountryOfOrigin.Should().Be(maliciousOrigin);
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_LegitimateDepth3Composite_RendersFullyWithoutThrowing()
    {
        var level0 = new Ingredient { Id = 1, Name = "Level 0" };
        var level1 = new Ingredient { Id = 2, Name = "Level 1" };
        var level2 = new Ingredient { Id = 3, Name = "Level 2" };
        var level3 = new Ingredient { Id = 4, Name = "Level 3" };

        _productIngredientServiceMock.Setup(x => x.GetDirectIngredientsByProductIdAsync(_product.Id))
            .ReturnsAsync(new List<Ingredient> { level0 });
        _productIngredientServiceMock.Setup(x => x.GetCompositionsReachableFromAsync(It.IsAny<IList<int>>()))
            .ReturnsAsync(new List<IngredientComposition>
            {
                new() { ParentIngredientId = level0.Id, ChildIngredientId = level1.Id },
                new() { ParentIngredientId = level1.Id, ChildIngredientId = level2.Id },
                new() { ParentIngredientId = level2.Id, ChildIngredientId = level3.Id }
                //level3 has no further recorded children - a legitimate, complete depth-3 composite
            });
        _ingredientServiceMock.Setup(x => x.GetIngredientsByIdsAsync(It.IsAny<int[]>()))
            .ReturnsAsync(new List<Ingredient> { level0, level1, level2, level3 });

        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        model.Ingredients[0].Children[0].Children[0].Children[0].Name.Should().Be("Level 3");
    }

    [Test]
    public void PrepareProductionLabelModelAsync_RealTruncationAtDepthBoundary_ThrowsNopException()
    {
        var level0 = new Ingredient { Id = 1, Name = "Level 0" };
        var level1 = new Ingredient { Id = 2, Name = "Level 1" };
        var level2 = new Ingredient { Id = 3, Name = "Level 2" };
        var level3 = new Ingredient { Id = 4, Name = "Level 3" };
        var level4 = new Ingredient { Id = 5, Name = "Level 4 - would be cut off" };

        _productIngredientServiceMock.Setup(x => x.GetDirectIngredientsByProductIdAsync(_product.Id))
            .ReturnsAsync(new List<Ingredient> { level0 });
        _productIngredientServiceMock.Setup(x => x.GetCompositionsReachableFromAsync(It.IsAny<IList<int>>()))
            .ReturnsAsync(new List<IngredientComposition>
            {
                new() { ParentIngredientId = level0.Id, ChildIngredientId = level1.Id },
                new() { ParentIngredientId = level1.Id, ChildIngredientId = level2.Id },
                new() { ParentIngredientId = level2.Id, ChildIngredientId = level3.Id },
                //level3 (depth boundary, MaxCompositionDepth = 3) still has a recorded child - real truncation
                new() { ParentIngredientId = level3.Id, ChildIngredientId = level4.Id }
            });
        _ingredientServiceMock.Setup(x => x.GetIngredientsByIdsAsync(It.IsAny<int[]>()))
            .ReturnsAsync(new List<Ingredient> { level0, level1, level2, level3, level4 });

        var factory = CreateFactory();

        Assert.ThrowsAsync<NopException>(async () =>
            await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar));
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_ExplicitNonDefaultLanguage_UsesThatLanguageForIngredientNamesAndProductName()
    {
        const int frenchLanguageId = 5;

        _localizationServiceMock
            .Setup(x => x.GetLocalizedAsync(It.IsAny<Product>(), It.IsAny<Expression<Func<Product, string>>>(),
                It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns<Product, Expression<Func<Product, string>>, int?, bool, bool>(
                (entity, selector, languageId, returnDefault, ensureTwo) =>
                    Task.FromResult(languageId == frenchLanguageId ? "Nom du produit en français" : selector.Compile().Invoke(entity)));

        var ingredient = new Ingredient { Id = 1, Name = "English ingredient name" };
        _productIngredientServiceMock.Setup(x => x.GetDirectIngredientsByProductIdAsync(_product.Id))
            .ReturnsAsync(new List<Ingredient> { ingredient });
        _localizationServiceMock
            .Setup(x => x.GetLocalizedAsync(It.IsAny<Ingredient>(), It.IsAny<Expression<Func<Ingredient, string>>>(),
                It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns<Ingredient, Expression<Func<Ingredient, string>>, int?, bool, bool>(
                (entity, selector, languageId, returnDefault, ensureTwo) =>
                    Task.FromResult(languageId == frenchLanguageId ? "Nom de l'ingrédient en français" : selector.Compile().Invoke(entity)));

        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, frenchLanguageId, ProductionLabelSizeVariant.SmallJar);

        model.ProductName.Should().Be("Nom du produit en français");
        model.Ingredients[0].Name.Should().Be("Nom de l'ingrédient en français");
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_PerLanguageStorageAndOrigin_UsesTheChosenLanguagesSavedValue()
    {
        const int englishLanguageId = 1;
        const int frenchLanguageId = 5;

        _genericAttributeServiceMock
            .Setup(x => x.GetAttributeAsync(_product, ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + englishLanguageId, 0, (string)null))
            .ReturnsAsync("Keep refrigerated below 4°C");
        _genericAttributeServiceMock
            .Setup(x => x.GetAttributeAsync(_product, ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + frenchLanguageId, 0, (string)null))
            .ReturnsAsync("Conserver au réfrigérateur en dessous de 4°C");
        _genericAttributeServiceMock
            .Setup(x => x.GetAttributeAsync(_product, ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + englishLanguageId, 0, (string)null))
            .ReturnsAsync("Poland");
        _genericAttributeServiceMock
            .Setup(x => x.GetAttributeAsync(_product, ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + frenchLanguageId, 0, (string)null))
            .ReturnsAsync("Pologne");

        var factory = CreateFactory();

        var englishModel = await factory.PrepareProductionLabelModelAsync(_batch, englishLanguageId, ProductionLabelSizeVariant.SmallJar);
        var frenchModel = await factory.PrepareProductionLabelModelAsync(_batch, frenchLanguageId, ProductionLabelSizeVariant.SmallJar);

        englishModel.StorageConditions.Should().Be("Keep refrigerated below 4°C");
        englishModel.CountryOfOrigin.Should().Be("Poland");
        frenchModel.StorageConditions.Should().Be("Conserver au réfrigérateur en dessous de 4°C");
        frenchModel.CountryOfOrigin.Should().Be("Pologne");
    }

    [Test]
    public async Task PrepareProductionLabelModelAsync_WhenIngredientsPluginTablesAreMissing_ReturnsEmptyIngredientsAndLogsWarning()
    {
        var missingTableException = new PostgresException("relation \"Ingredient\" does not exist", "ERROR", "ERROR", PostgresErrorCodes.UndefinedTable);
        _productIngredientServiceMock.Setup(x => x.GetDirectIngredientsByProductIdAsync(It.IsAny<int>())).ThrowsAsync(missingTableException);

        var factory = CreateFactory();

        var model = await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar);

        model.Ingredients.Should().BeEmpty();
        _loggerMock.Verify(x => x.WarningAsync(It.IsAny<string>(), missingTableException, null), Times.Once);
    }

    [Test]
    public void PrepareProductionLabelModelAsync_WhenIngredientReadFailsForAnUnrelatedReason_PropagatesTheException()
    {
        //scoped deliberately narrow: a genuine connection failure or an unrelated SQLSTATE must still
        //surface normally, not be swallowed into the same empty-ingredients path
        var unrelatedException = new PostgresException("unique constraint violated", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);
        _productIngredientServiceMock.Setup(x => x.GetDirectIngredientsByProductIdAsync(It.IsAny<int>())).ThrowsAsync(unrelatedException);

        var factory = CreateFactory();

        Assert.ThrowsAsync<PostgresException>(async () =>
            await factory.PrepareProductionLabelModelAsync(_batch, DefaultLanguageId, ProductionLabelSizeVariant.SmallJar));
    }
}
