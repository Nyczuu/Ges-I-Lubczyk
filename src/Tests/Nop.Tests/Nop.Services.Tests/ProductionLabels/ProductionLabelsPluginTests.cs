using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Localization;
using Nop.Plugin.Misc.ProductionLabels;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Mvc.Routing;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ProductionLabels;

/// <summary>
/// Exercises the real ProductionLabelsPlugin.UninstallAsync directly - ServiceTest.InitPlugins() bypasses
/// the real InstallAsync/UninstallAsync, so nothing else in this suite proves the per-language
/// GenericAttribute purge (round 7's requirement, no sibling precedent) actually happens. This mutates
/// the shared, process-wide permission/setting state the same way the real uninstall pipeline would -
/// accepted deliberately here, mirroring ServingSuggestionsPluginTests's own identical posture.
/// </summary>
[TestFixture]
public class ProductionLabelsPluginTests : ServiceTest
{
    private IGenericAttributeService _genericAttributeService;
    private ILanguageService _languageService;
    private IPermissionService _permissionService;
    private IProductService _productService;
    private ProductionLabelsPlugin _productionLabelsPlugin;

    [OneTimeSetUp]
    public void SetUp()
    {
        _genericAttributeService = GetService<IGenericAttributeService>();
        _languageService = GetService<ILanguageService>();
        _permissionService = GetService<IPermissionService>();
        _productService = GetService<IProductService>();

        _productionLabelsPlugin = new ProductionLabelsPlugin(
            _genericAttributeService,
            _languageService,
            GetService<ILocalizationService>(),
            GetService<INopUrlHelper>(),
            _permissionService,
            GetService<ISettingService>(),
            GetService<WidgetSettings>());
    }

    /// <summary>
    /// Both concerns (permission removal, per-language GenericAttribute purge) are asserted from one real
    /// UninstallAsync call, deliberately, rather than split into two tests that would each call the same
    /// real UninstallAsync a second time - NUnit does not guarantee declaration order between two
    /// [Test] methods in the same fixture, and since UninstallAsync mutates the shared, process-wide
    /// permission state, whichever of two separate tests happened to run second would find the permission
    /// records already removed by the first.
    /// </summary>
    [Test]
    public async Task UninstallAsync_RemovesPermissionRecordsAndPurgesEveryLanguagesGenericAttributeRows()
    {
        var beforePermissionRecords = await _permissionService.GetAllPermissionRecordsAsync();
        beforePermissionRecords.Should().Contain(record => record.SystemName == ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW);
        beforePermissionRecords.Should().Contain(record => record.SystemName == ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_CREATE);
        beforePermissionRecords.Should().Contain(record => record.SystemName == ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_DELETE);

        //two languages, so this test can distinguish "purges for every configured language" from "purges
        //whatever the first language happens to be" - a single-language fixture could pass even if the
        //real production loop only ever touched one row
        var languageA = new Language { Name = "Production labels uninstall test language A", LanguageCulture = "xx-PA", UniqueSeoCode = "pa", Published = true };
        await _languageService.InsertLanguageAsync(languageA);
        var languageB = new Language { Name = "Production labels uninstall test language B", LanguageCulture = "xx-PB", UniqueSeoCode = "pb", Published = true };
        await _languageService.InsertLanguageAsync(languageB);

        var product = new Product { Name = "Production labels uninstall test product", Published = true };
        await _productService.InsertProductAsync(product);

        await _genericAttributeService.SaveAttributeAsync(product, ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + languageA.Id, "Keep cool A");
        await _genericAttributeService.SaveAttributeAsync(product, ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + languageA.Id, "Poland A");
        await _genericAttributeService.SaveAttributeAsync(product, ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + languageB.Id, "Keep cool B");
        await _genericAttributeService.SaveAttributeAsync(product, ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + languageB.Id, "Poland B");

        await _productionLabelsPlugin.UninstallAsync();

        var afterPermissionRecords = await _permissionService.GetAllPermissionRecordsAsync();
        var storageA = await _genericAttributeService.GetAttributeAsync<string>(product, ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + languageA.Id);
        var originA = await _genericAttributeService.GetAttributeAsync<string>(product, ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + languageA.Id);
        var storageB = await _genericAttributeService.GetAttributeAsync<string>(product, ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + languageB.Id);
        var originB = await _genericAttributeService.GetAttributeAsync<string>(product, ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + languageB.Id);

        await _productService.DeleteProductAsync(product);
        await _languageService.DeleteLanguageAsync(languageA);
        await _languageService.DeleteLanguageAsync(languageB);

        afterPermissionRecords.Should().NotContain(record => record.SystemName == ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW);
        afterPermissionRecords.Should().NotContain(record => record.SystemName == ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_CREATE);
        afterPermissionRecords.Should().NotContain(record => record.SystemName == ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_DELETE);
        storageA.Should().BeNullOrEmpty();
        originA.Should().BeNullOrEmpty();
        storageB.Should().BeNullOrEmpty();
        originB.Should().BeNullOrEmpty();
    }
}
