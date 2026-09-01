using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Plugin.Misc.ServingSuggestions;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Security;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ServingSuggestions;

/// <summary>
/// Exercises the real ServingSuggestionsPlugin.UninstallAsync directly. ServiceTest.InitPlugins() bypasses
/// the real InstallAsync/UninstallAsync (it registers the plugin descriptor and runs the schema migration
/// directly), so nothing else in this test suite proves the LocalizedProperty and Picture purges described
/// in the design (Pass 2, point 6) actually happen. This mutates the shared, process-wide permission/setting
/// state the same way the real uninstall pipeline would - accepted deliberately here so the two genuinely
/// unprecedented mechanisms in this plugin (the LocalizedProperty bulk delete, and - no Ingredients
/// precedent for this half - the Picture purge) are proven against the real method rather than skipped as
/// untestable.
/// </summary>
[TestFixture]
public class ServingSuggestionsPluginTests : ServiceTest
{
    private IServingSuggestionService _servingSuggestionService;
    private IPictureService _pictureService;
    private IProductService _productService;
    private IRepository<LocalizedProperty> _localizedPropertyRepository;
    private IRepository<ServingSuggestion> _servingSuggestionRepository;
    private IRepository<Picture> _pictureRepository;
    private ServingSuggestionsPlugin _servingSuggestionsPlugin;

    [OneTimeSetUp]
    public void SetUp()
    {
        _servingSuggestionService = GetService<IServingSuggestionService>();
        _pictureService = GetService<IPictureService>();
        _productService = GetService<IProductService>();
        _localizedPropertyRepository = GetService<IRepository<LocalizedProperty>>();
        _servingSuggestionRepository = GetService<IRepository<ServingSuggestion>>();
        _pictureRepository = GetService<IRepository<Picture>>();

        _servingSuggestionsPlugin = new ServingSuggestionsPlugin(
            GetService<ILocalizationService>(),
            GetService<IPermissionService>(),
            _pictureService,
            _localizedPropertyRepository,
            _servingSuggestionRepository,
            GetService<ISettingService>(),
            GetService<WidgetSettings>());
    }

    [Test]
    public async Task UninstallAsync_PurgesOrphanedLocalizedPropertyRowsAndPictures_ForEveryProduct()
    {
        //two products, each with their own ServingSuggestion and its own picture, so this test can
        //actually distinguish "purges for every product" (the real production loop in
        //ServingSuggestionsPlugin.UninstallAsync) from "purges whatever the first row happens to be" - a
        //single-product fixture could pass even if the loop only ever touched one row. Only the second
        //suggestion gets a step, so the ServingSuggestionStep LocaleKeyGroup purge is exercised too.
        var language = new Language { Name = "Serving suggestions uninstall test language", LanguageCulture = "xx-SU", UniqueSeoCode = "su", Published = true };
        await GetService<ILanguageService>().InsertLanguageAsync(language);

        var productA = new Product { Name = "Uninstall test product A", Published = true };
        await _productService.InsertProductAsync(productA);
        var pictureA = await _pictureService.InsertPictureAsync([1, 2, 3], "image/png", "uninstall-test-a", validateBinary: false);
        var servingSuggestionA = new ServingSuggestion { ProductId = productA.Id, Title = "Title A", Description = "Description A", PictureId = pictureA.Id };
        await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestionA, new List<ServingSuggestionLocalizedValue>
        {
            new(language.Id, "Localized uninstall test title A", null)
        });

        var productB = new Product { Name = "Uninstall test product B", Published = true };
        await _productService.InsertProductAsync(productB);
        var pictureB = await _pictureService.InsertPictureAsync([4, 5, 6], "image/png", "uninstall-test-b", validateBinary: false);
        var servingSuggestionB = new ServingSuggestion { ProductId = productB.Id, Title = "Title B", Description = "Description B", PictureId = pictureB.Id };
        await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestionB, new List<ServingSuggestionLocalizedValue>
        {
            new(language.Id, "Localized uninstall test title B", null)
        });

        var stepB = new ServingSuggestionStep { ServingSuggestionId = servingSuggestionB.Id, Text = "Step B" };
        await _servingSuggestionService.InsertServingSuggestionStepAsync(stepB, new List<ServingSuggestionStepLocalizedValue>
        {
            new(language.Id, "Localized uninstall test step B")
        });

        var beforeUninstallServingSuggestionProperties = await _localizedPropertyRepository.Table
            .Where(property => property.LocaleKeyGroup == nameof(ServingSuggestion))
            .ToListAsync();
        var beforeUninstallStepProperties = await _localizedPropertyRepository.Table
            .Where(property => property.LocaleKeyGroup == nameof(ServingSuggestionStep))
            .ToListAsync();

        //the real method under test - UninstallAsync does not drop the plugin's own tables itself (that's
        //the framework's ApplyDownMigrations, run separately after UninstallAsync in the real pipeline), so
        //the ServingSuggestion rows themselves survive this call; the Picture rows do not, because the
        //design requires them to be purged here before the table (and its PictureId column) is gone
        await _servingSuggestionsPlugin.UninstallAsync();

        var afterUninstallServingSuggestionProperties = await _localizedPropertyRepository.Table
            .Where(property => property.LocaleKeyGroup == nameof(ServingSuggestion))
            .ToListAsync();
        var afterUninstallStepProperties = await _localizedPropertyRepository.Table
            .Where(property => property.LocaleKeyGroup == nameof(ServingSuggestionStep))
            .ToListAsync();
        //queried through IRepository<Picture> directly - see ServingSuggestionServiceTests for why
        //IPictureService.GetPictureByIdAsync's own cached read is not used here (this isolated test
        //project doesn't reference Nop.Plugin.Misc.AzureBlob, the one plugin that invalidates Picture's
        //by-id cache, so UninstallAsync's own internal GetPictureByIdAsync call - needed to load the
        //Picture object DeletePictureAsync takes - would otherwise leave this check reading a stale,
        //pre-delete cached object)
        var reloadedPictureA = await _pictureRepository.GetByIdAsync(pictureA.Id);
        var reloadedPictureB = await _pictureRepository.GetByIdAsync(pictureB.Id);

        //cleanup - the entity rows themselves, since UninstallAsync deliberately leaves them for the table drop
        await GetService<IRepository<ServingSuggestionStep>>().DeleteAsync(stepB);
        await _servingSuggestionRepository.DeleteAsync(servingSuggestionA);
        await _servingSuggestionRepository.DeleteAsync(servingSuggestionB);
        await _productService.DeleteProductAsync(productA);
        await _productService.DeleteProductAsync(productB);
        await GetService<ILanguageService>().DeleteLanguageAsync(language);

        beforeUninstallServingSuggestionProperties.Should().HaveCount(2);
        beforeUninstallStepProperties.Should().ContainSingle();
        afterUninstallServingSuggestionProperties.Should().BeEmpty();
        afterUninstallStepProperties.Should().BeEmpty();
        reloadedPictureA.Should().BeNull();
        reloadedPictureB.Should().BeNull();
    }
}
