using AwesomeAssertions;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.ProductionLabels;
using Nop.Plugin.Misc.ProductionLabels.Admin.Factories;
using Nop.Plugin.Misc.ProductionLabels.Admin.Models;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Web.Framework.Factories;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ProductionLabels;

/// <summary>
/// ProductionLabelsAdminModelFactory previously had zero test coverage. Covers the round-10 Gate-2
/// default-language resolution (both branches of ResolveDefaultLanguageIdAsync), the standard-template
/// regression fix (flat StorageConditions/CountryOfOrigin populated from the default language's own
/// locale - needed because Html.LocalizedEditorAsync renders the standard template, bound directly to
/// those flat properties rather than Locales[i].*, whenever at most one language is configured -
/// Nop.Web.Framework/Extensions/HtmlExtensions.cs:46), and basic coverage of the remaining Prepare*
/// methods.
/// </summary>
[TestFixture]
public class ProductionLabelsAdminModelFactoryTests : ServiceTest
{
    private IGenericAttributeService _genericAttributeService;
    private ILanguageService _languageService;
    private IProductionBatchService _productionBatchService;
    private IProductService _productService;
    private IStoreContext _storeContext;
    private ProductionLabelsAdminModelFactory _factory;

    [OneTimeSetUp]
    public void SetUp()
    {
        _genericAttributeService = GetService<IGenericAttributeService>();
        _languageService = GetService<ILanguageService>();
        _productionBatchService = GetService<IProductionBatchService>();
        _productService = GetService<IProductService>();
        _storeContext = GetService<IStoreContext>();

        _factory = new ProductionLabelsAdminModelFactory(
            _genericAttributeService,
            _languageService,
            GetService<ILocalizationService>(),
            GetService<ILocalizedModelFactory>(),
            _productionBatchService,
            _productService,
            _storeContext);
    }

    private async Task<Product> CreateProductAsync(string name)
    {
        var product = new Product { Name = name, Published = true };
        await _productService.InsertProductAsync(product);

        return product;
    }

    [Test]
    public async Task ResolveDefaultLanguageIdAsync_WhenStoreHasAnExplicitDefaultLanguage_ReturnsIt()
    {
        var store = new Store { DefaultLanguageId = 42 };
        var storeLanguages = new List<Language> { new() { Id = 1, DisplayOrder = 0 } };

        var result = await _factory.ResolveDefaultLanguageIdAsync(store, storeLanguages);

        result.Should().Be(42);
    }

    [Test]
    public async Task ResolveDefaultLanguageIdAsync_WhenStoreDefaultLanguageIdIsZero_FallsBackToTheFirstLanguageInTheList()
    {
        var store = new Store { DefaultLanguageId = 0 };
        var firstByDisplayOrder = new Language { Id = 7, DisplayOrder = 0 };
        var second = new Language { Id = 9, DisplayOrder = 1 };
        var storeLanguages = new List<Language> { firstByDisplayOrder, second };

        var result = await _factory.ResolveDefaultLanguageIdAsync(store, storeLanguages);

        result.Should().Be(firstByDisplayOrder.Id);
    }

    [Test]
    public async Task ResolveDefaultLanguageIdAsync_WhenStoreDefaultLanguageIdIsZeroAndNoLanguagesGiven_ReturnsZero()
    {
        var store = new Store { DefaultLanguageId = 0 };

        var result = await _factory.ResolveDefaultLanguageIdAsync(store, new List<Language>());

        result.Should().Be(0);
    }

    [Test]
    public async Task PrepareProductionLabelsProductModelAsync_WhenProductIdIsZero_ReturnsModelWithoutLocales()
    {
        var model = await _factory.PrepareProductionLabelsProductModelAsync(0);

        model.Locales.Should().BeEmpty();
    }

    [Test]
    public async Task PrepareProductionLabelsProductModelAsync_PopulatesLocalesFromGenericAttributes()
    {
        var product = await CreateProductAsync("Factory test - locales product");
        var defaultLanguageId = await _factory.ResolveCurrentStoreDefaultLanguageIdAsync();

        await _genericAttributeService.SaveAttributeAsync(product,
            ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + defaultLanguageId, "Keep refrigerated");
        await _genericAttributeService.SaveAttributeAsync(product,
            ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + defaultLanguageId, "Poland");

        var model = await _factory.PrepareProductionLabelsProductModelAsync(product.Id);

        await _productService.DeleteProductAsync(product);

        var locale = model.Locales.Should().ContainSingle(l => l.LanguageId == defaultLanguageId).Subject;
        locale.StorageConditions.Should().Be("Keep refrigerated");
        locale.CountryOfOrigin.Should().Be("Poland");
    }

    /// <summary>
    /// Regression test for the standard-template gap: before this fix, ProductionLabelsProductModel had no
    /// flat StorageConditions/CountryOfOrigin properties at all, so the standard template's
    /// asp-for="StorageConditions" could not even resolve. Adding the properties alone is not enough
    /// either - they must actually be populated from real data, or the standard template renders a blank
    /// form despite saved data existing for the product's one configured language.
    /// </summary>
    [Test]
    public async Task PrepareProductionLabelsProductModelAsync_PopulatesFlatPropertiesFromTheDefaultLanguagesLocale()
    {
        var product = await CreateProductAsync("Factory test - flat properties product");
        var defaultLanguageId = await _factory.ResolveCurrentStoreDefaultLanguageIdAsync();

        await _genericAttributeService.SaveAttributeAsync(product,
            ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + defaultLanguageId, "Keep cool and dry");
        await _genericAttributeService.SaveAttributeAsync(product,
            ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + defaultLanguageId, "Poland");

        var model = await _factory.PrepareProductionLabelsProductModelAsync(product.Id);

        await _productService.DeleteProductAsync(product);

        model.StorageConditions.Should().Be("Keep cool and dry");
        model.CountryOfOrigin.Should().Be("Poland");
    }

    [Test]
    public async Task PrepareProductionBatchSearchModelAsync_PopulatesAllProductsOptionAndPageSize()
    {
        var searchModel = await _factory.PrepareProductionBatchSearchModelAsync(new ProductionBatchSearchModel());

        searchModel.AvailableProducts.Should().Contain(item => item.Value == "0");
        searchModel.PageSize.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Deliberately exercises PrepareProductionBatchListModelAsync only with a filter that matches zero
    /// batches, rather than inserting a real batch and asserting on its mapped ProductName. With at least
    /// one matching row, ModelExtensions.PrepareToGridAsync's dataFillFunction would actually invoke
    /// batch.ToModel&lt;ProductionBatchModel&gt;() - the real Mapster/AutoMapper-style pipeline behind
    /// Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions.MappingExtensions.ToModel. That pipeline sits
    /// behind MapperConfiguration.TypeAdapterConfig, process-wide static state this test harness never
    /// initializes centrally (BaseNopTest hand-registers services rather than going through the real app's
    /// NopEngine.AddMapper()); a real call was confirmed (empirically, by running the full suite with and
    /// without it) to collide with AdminMapperConfigurationTest.ConfigurationIsValid, a core, pre-existing
    /// test that unconditionally reconstructs and reconfigures AdminMapperConfiguration against that same
    /// shared TypeAdapterConfig with no guard against it having already been compiled by an earlier real
    /// .Adapt() call elsewhere in the process - whichever test happens to trigger that first. Since that
    /// core test is outside this unit's scope to change, this test stays on the safe side of that gap: a
    /// zero-row result never reaches the dataFillFunction projection at all (LINQ's SelectAwait never
    /// invokes its selector over an empty source), so PrepareToGridAsync's paging/count wiring can still be
    /// verified here without ever touching the shared mapper.
    ///
    /// A later attempt tried giving this plugin's own Infrastructure.MapperConfiguration a scoped,
    /// one-time Nop.Core.Infrastructure.Mapper.MapperConfiguration.Init() call in this fixture's own
    /// OneTimeSetUp (independent of AdminMapperConfiguration), then adding a companion test with one real
    /// inserted batch to cover the hand-written ProductName backfill join. Confirmed (empirically, by
    /// running the full suite) to collide identically: Mapster's TypeAdapterConfig tracks "Adapt already
    /// called" as one flag for the whole shared config instance, not per type pair, so the real
    /// .ToModel&lt;ProductionBatchModel&gt;() call this companion test made was enough to later break
    /// AdminMapperConfigurationTest.ConfigurationIsValid's own unrelated CreateMap(...).ForMember(...)
    /// calls with "TypeAdapter.Adapt was already called, please clone or create new TypeAdapterConfig."
    /// Scoping the registration to just this plugin's own pair does not help - the lock is global to the
    /// TypeAdapterConfig instance the moment any real Adapt happens anywhere against it, regardless of
    /// which pair triggered it. That attempt was reverted; the ProductName backfill join therefore still
    /// has no test exercising the real Mapster path as of this writing.
    /// </summary>
    [Test]
    public async Task PrepareProductionBatchListModelAsync_WhenNoBatchesMatchTheFilter_ReturnsEmptyDataWithZeroRecordsTotal()
    {
        var product = await CreateProductAsync("Factory test - empty batch list product");

        var searchModel = new ProductionBatchSearchModel { SearchProductId = product.Id };
        searchModel.SetGridPageSize();

        var listModel = await _factory.PrepareProductionBatchListModelAsync(searchModel);

        await _productService.DeleteProductAsync(product);

        listModel.Data.Should().BeEmpty();
        listModel.RecordsTotal.Should().Be(0);
    }

    [Test]
    public async Task PrepareProductionBatchModelAsync_WhenProductIdIsZero_PopulatesAvailableProductsFromTheCatalog()
    {
        var product = await CreateProductAsync("Factory test - batch model available products");

        var model = await _factory.PrepareProductionBatchModelAsync(0);

        await _productService.DeleteProductAsync(product);

        model.ProductId.Should().Be(0);
        model.AvailableProducts.Should().Contain(item => item.Value == product.Id.ToString() && item.Text == product.Name);
    }

    /// <summary>
    /// The product-edit page tab reaches PrepareProductionBatchModelAsync with a real, fixed product id;
    /// the method doesn't look products up (or need one to exist) on this path - only the standalone
    /// section's own product picker, reached via productId == 0, does.
    /// </summary>
    [Test]
    public async Task PrepareProductionBatchModelAsync_WhenProductIdIsNotZero_LeavesAvailableProductsEmpty()
    {
        var model = await _factory.PrepareProductionBatchModelAsync(42);

        model.ProductId.Should().Be(42);
        model.AvailableProducts.Should().BeEmpty();
    }

    [Test]
    public async Task PrepareGenerateProductionLabelModelAsync_WhenStoreHasOnlyOneConfiguredLanguage_LeavesAvailableLanguagesEmpty()
    {
        var model = await _factory.PrepareGenerateProductionLabelModelAsync(0);

        model.AvailableLanguages.Should().BeEmpty();
    }

    /// <summary>
    /// Temporarily inserts a second active language (deleted again immediately after use, before the
    /// assertions) to push the current store's configured-language count above one and exercise the
    /// AvailableLanguages population branch - the seeded test store otherwise has exactly one language
    /// (Nop.Services.Installation.InstallRequiredData.InstallLanguagesAsync installs only the default
    /// culture's language when InstallationSettings.CultureInfo matches NopCommonDefaults.DefaultLanguageCulture,
    /// which is how BaseNopTest.Init installs it).
    /// </summary>
    [Test]
    public async Task PrepareGenerateProductionLabelModelAsync_WhenStoreHasMoreThanOneConfiguredLanguage_PopulatesAvailableLanguages()
    {
        var store = await _storeContext.GetCurrentStoreAsync();

        var extraLanguage = new Language
        {
            Name = "Factory test - extra language",
            LanguageCulture = "pl-PL",
            UniqueSeoCode = "pl",
            FlagImageFileName = "pl.png",
            Published = true,
            DisplayOrder = 99
        };
        await _languageService.InsertLanguageAsync(extraLanguage);

        var expectedLanguages = await _languageService.GetAllLanguagesAsync(storeId: store.Id);

        var model = await _factory.PrepareGenerateProductionLabelModelAsync(0);

        await _languageService.DeleteLanguageAsync(extraLanguage);

        model.AvailableLanguages.Should().HaveCount(expectedLanguages.Count);
        model.AvailableLanguages.Should().Contain(item => item.Value == extraLanguage.Id.ToString() && item.Text == extraLanguage.Name);
    }
}
