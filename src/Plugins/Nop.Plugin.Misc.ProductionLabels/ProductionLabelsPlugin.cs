using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Cms;
using Nop.Plugin.Misc.ProductionLabels.Admin.Components;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.ProductionLabels;

/// <summary>
/// Represents the Production labels plugin
/// </summary>
public class ProductionLabelsPlugin : BasePlugin, IMiscPlugin, IWidgetPlugin
{
    #region Fields

    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly IPermissionService _permissionService;
    protected readonly ISettingService _settingService;
    protected readonly WidgetSettings _widgetSettings;

    #endregion

    #region Ctor

    public ProductionLabelsPlugin(IGenericAttributeService genericAttributeService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        IPermissionService permissionService,
        ISettingService settingService,
        WidgetSettings widgetSettings)
    {
        _genericAttributeService = genericAttributeService;
        _languageService = languageService;
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _permissionService = permissionService;
        _settingService = settingService;
        _widgetSettings = widgetSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return _nopUrlHelper.RouteUrl(ProductionLabelsDefaults.Routes.Admin.ListRouteName);
    }

    /// <summary>
    /// Gets a type of a view component for displaying widget
    /// </summary>
    /// <param name="widgetZone">Name of the widget zone</param>
    /// <returns>View component type</returns>
    public Type GetWidgetViewComponent(string widgetZone)
    {
        return typeof(ProductionLabelsAdminViewComponent);
    }

    /// <summary>
    /// Gets widget zones where this widget should be rendered - only the admin product-details zone;
    /// no PublicWidgetZones entry, since this plugin has no storefront surface at all
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the widget zones
    /// </returns>
    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string>
        {
            AdminWidgetZones.ProductDetailsBlock
        });
    }

    /// <summary>
    /// Install the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InstallAsync()
    {
        //widget
        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(ProductionLabelsDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(ProductionLabelsDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.ProductionLabels.Production"] = "Production",
            ["Plugins.Misc.ProductionLabels.List"] = "Production batches",
            ["Plugins.Misc.ProductionLabels.List.AddNew"] = "Log new batch",
            ["Plugins.Misc.ProductionLabels.List.SearchProduct"] = "Product",
            ["Plugins.Misc.ProductionLabels.List.SearchProduct.Hint"] = "Search by a specific product.",
            ["Plugins.Misc.ProductionLabels.ProductionBatches.AddNew"] = "Log new batch",
            ["Plugins.Misc.ProductionLabels.ProductionBatches.SaveBeforeEdit"] = "You need to save the product before you can log production batches for this product page.",
            ["Plugins.Misc.ProductionLabels.ProductInfo.Saved"] = "Storage conditions / country of origin have been saved successfully.",
            ["Plugins.Misc.ProductionLabels.GenerateLabel"] = "Generate label",
            ["Plugins.Misc.ProductionLabels.Fields.Product"] = "Product",
            ["Plugins.Misc.ProductionLabels.Fields.Product.Hint"] = "The product this batch belongs to.",
            ["Plugins.Misc.ProductionLabels.Fields.BatchCode"] = "Batch code",
            ["Plugins.Misc.ProductionLabels.Fields.BatchCode.Hint"] = "The system-generated batch code.",
            ["Plugins.Misc.ProductionLabels.Fields.ProductionDate"] = "Production date",
            ["Plugins.Misc.ProductionLabels.Fields.ProductionDate.Hint"] = "The date this batch was produced.",
            ["Plugins.Misc.ProductionLabels.Fields.BestBeforeDate"] = "Best before date",
            ["Plugins.Misc.ProductionLabels.Fields.BestBeforeDate.Hint"] = "The best-before date printed on the label.",
            ["Plugins.Misc.ProductionLabels.Fields.BestBeforeDate.MustBeAfterProductionDate"] = "The best-before date must be after the production date.",
            ["Plugins.Misc.ProductionLabels.Fields.Quantity"] = "Quantity",
            ["Plugins.Misc.ProductionLabels.Fields.Quantity.Hint"] = "The quantity produced.",
            ["Plugins.Misc.ProductionLabels.Fields.Quantity.GreaterThanZero"] = "Quantity must be greater than zero.",
            ["Plugins.Misc.ProductionLabels.Fields.LabelGeneratedOnUtc"] = "Label generated on",
            ["Plugins.Misc.ProductionLabels.Fields.CreatedOn"] = "Created on",
            ["Plugins.Misc.ProductionLabels.Fields.StorageConditions"] = "Storage conditions",
            ["Plugins.Misc.ProductionLabels.Fields.StorageConditions.Hint"] = "The storage conditions text printed on the label, for this language.",
            ["Plugins.Misc.ProductionLabels.Fields.CountryOfOrigin"] = "Country of origin",
            ["Plugins.Misc.ProductionLabels.Fields.CountryOfOrigin.Hint"] = "The country of origin text printed on the label, for this language.",
            ["Plugins.Misc.ProductionLabels.Fields.SizeVariant"] = "Label size",
            ["Plugins.Misc.ProductionLabels.Fields.SizeVariant.Hint"] = "The preset label size layout.",
            ["Plugins.Misc.ProductionLabels.Fields.Language"] = "Language",
            ["Plugins.Misc.ProductionLabels.Fields.Language.Hint"] = "The language to render the label in.",
            ["Plugins.Misc.ProductionLabels.Errors.BestBeforeDateNotAfterProductionDate"] = "The best-before date must be after the production date.",
            ["Plugins.Misc.ProductionLabels.Errors.QuantityNotGreaterThanZero"] = "Quantity must be greater than zero.",
            ["Plugins.Misc.ProductionLabels.Errors.CannotDeleteLabeledBatch"] = "This batch cannot be deleted because a label has already been generated from it.",
            ["Plugins.Misc.ProductionLabels.Errors.CompositionTruncated"] = "This product's ingredient composition is too deeply nested to render completely on the label.",
            ["Plugins.Misc.ProductionLabels.Label.Ingredients"] = "Ingredients",
            ["Plugins.Misc.ProductionLabels.Label.NetQuantity"] = "Net quantity",
            ["Plugins.Misc.ProductionLabels.Label.BestBefore"] = "Best before",
            ["Plugins.Misc.ProductionLabels.Label.BatchCode"] = "Batch",
            ["Plugins.Misc.ProductionLabels.Label.StorageConditions"] = "Storage conditions",
            ["Plugins.Misc.ProductionLabels.Label.CountryOfOrigin"] = "Country of origin",
            ["Enums.Nop.Plugin.Misc.ProductionLabels.Domain.ProductionLabelSizeVariant.SmallJar"] = "Small jar",
            ["Enums.Nop.Plugin.Misc.ProductionLabels.Domain.ProductionLabelSizeVariant.LargeJar"] = "Large jar"
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        //widget
        if (_widgetSettings.ActiveWidgetSystemNames.Contains(ProductionLabelsDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Remove(ProductionLabelsDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        //permissions (installed automatically; removal is not)
        var permissionRecords = await _permissionService.GetAllPermissionRecordsAsync();

        foreach (var systemName in new[]
                 {
                     ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW,
                     ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_CREATE,
                     ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_DELETE
                 })
        {
            var permissionRecord = permissionRecords.FirstOrDefault(record => record.SystemName == systemName);
            if (permissionRecord != null)
                await _permissionService.DeletePermissionRecordAsync(permissionRecord);
        }

        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.ProductionLabels");
        await _localizationService.DeleteLocaleResourcesAsync("Enums.Nop.Plugin.Misc.ProductionLabels.Domain.ProductionLabelSizeVariant");

        //GenericAttribute is a shared core table our migration never touches structurally, so the
        //per-(product, language) storage-conditions/country-of-origin rows would survive the table drop as
        //orphans unless purged explicitly here - one DeleteAttributesAsync<Product> call per language per
        //field, since round 7 made both keys per-language rather than one fixed key each
        var languages = await _languageService.GetAllLanguagesAsync(showHidden: true);

        foreach (var language in languages)
        {
            await _genericAttributeService.DeleteAttributesAsync<Product>(ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + language.Id);
            await _genericAttributeService.DeleteAttributesAsync<Product>(ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + language.Id);
        }

        await base.UninstallAsync();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
    /// </summary>
    public bool HideInWidgetList => true;

    #endregion
}
