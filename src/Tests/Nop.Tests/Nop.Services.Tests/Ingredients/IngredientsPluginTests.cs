using AwesomeAssertions;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Localization;
using Nop.Data;
using Nop.Plugin.Misc.Ingredients;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Mvc.Routing;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Ingredients;

/// <summary>
/// Exercises the real IngredientsPlugin.UninstallAsync directly. ServiceTest.InitPlugins() bypasses the
/// real InstallAsync/UninstallAsync (it registers the plugin descriptor and runs the schema migration
/// directly), so nothing else in this test suite proves the LocalizedProperty purge described in the class
/// design (spec section 7, Q7) actually happens. This mutates the shared, process-wide permission/setting
/// state the same way the real uninstall pipeline would - accepted deliberately here so the one genuinely
/// unprecedented mechanism in this plugin (the LocalizedProperty bulk delete) is proven against the real
/// method rather than skipped as untestable.
/// </summary>
[TestFixture]
public class IngredientsPluginTests : ServiceTest
{
    private IIngredientService _ingredientService;
    private ILanguageService _languageService;
    private IRepository<LocalizedProperty> _localizedPropertyRepository;
    private IngredientsPlugin _ingredientsPlugin;

    [OneTimeSetUp]
    public void SetUp()
    {
        _ingredientService = GetService<IIngredientService>();
        _languageService = GetService<ILanguageService>();
        _localizedPropertyRepository = GetService<IRepository<LocalizedProperty>>();

        _ingredientsPlugin = new IngredientsPlugin(
            GetService<ILocalizationService>(),
            GetService<INopUrlHelper>(),
            GetService<IPermissionService>(),
            _localizedPropertyRepository,
            GetService<ISettingService>(),
            GetService<WidgetSettings>());
    }

    [Test]
    public async Task UninstallAsync_PurgesOrphanedLocalizedPropertyRows_ForTheIngredientLocaleKeyGroup()
    {
        var language = new Language { Name = "Ingredients uninstall test language", LanguageCulture = "xx-DD", UniqueSeoCode = "xd", Published = true };
        await _languageService.InsertLanguageAsync(language);

        var ingredient = new Ingredient { Name = "Uninstall test ingredient" };
        await _ingredientService.InsertIngredientAsync(ingredient, new List<IngredientLocalizedValue>
        {
            new(language.Id, "Localized uninstall test name", null)
        });

        var beforeUninstall = await _localizedPropertyRepository.Table
            .Where(property => property.LocaleKeyGroup == nameof(Ingredient))
            .ToListAsync();

        //the real method under test - UninstallAsync does not drop the plugin's own tables itself (that's
        //the framework's ApplyDownMigrations, run separately after UninstallAsync in the real pipeline -
        //see spec's Migration section), so the Ingredient row itself survives this call and is cleaned up
        //explicitly below
        await _ingredientsPlugin.UninstallAsync();

        var afterUninstall = await _localizedPropertyRepository.Table
            .Where(property => property.LocaleKeyGroup == nameof(Ingredient))
            .ToListAsync();

        //cleanup
        await _ingredientService.DeleteIngredientAsync(ingredient);
        await _languageService.DeleteLanguageAsync(language);

        beforeUninstall.Should().NotBeEmpty();
        afterUninstall.Should().BeEmpty();
    }
}
