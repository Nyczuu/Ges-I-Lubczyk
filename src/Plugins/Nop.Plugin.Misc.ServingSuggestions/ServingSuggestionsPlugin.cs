using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Localization;
using Nop.Data;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Components;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Plugin.Misc.ServingSuggestions.Public.Components;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Misc.ServingSuggestions;

/// <summary>
/// Represents the Serving suggestions plugin
/// </summary>
public class ServingSuggestionsPlugin : BasePlugin, IMiscPlugin, IWidgetPlugin
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly IPermissionService _permissionService;
    protected readonly IPictureService _pictureService;
    protected readonly IRepository<LocalizedProperty> _localizedPropertyRepository;
    protected readonly IRepository<ServingSuggestion> _servingSuggestionRepository;
    protected readonly ISettingService _settingService;
    protected readonly WidgetSettings _widgetSettings;

    #endregion

    #region Ctor

    public ServingSuggestionsPlugin(ILocalizationService localizationService,
        IPermissionService permissionService,
        IPictureService pictureService,
        IRepository<LocalizedProperty> localizedPropertyRepository,
        IRepository<ServingSuggestion> servingSuggestionRepository,
        ISettingService settingService,
        WidgetSettings widgetSettings)
    {
        _localizationService = localizationService;
        _permissionService = permissionService;
        _pictureService = pictureService;
        _localizedPropertyRepository = localizedPropertyRepository;
        _servingSuggestionRepository = servingSuggestionRepository;
        _settingService = settingService;
        _widgetSettings = widgetSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a type of a view component for displaying widget
    /// </summary>
    /// <param name="widgetZone">Name of the widget zone</param>
    /// <returns>View component type</returns>
    public Type GetWidgetViewComponent(string widgetZone)
    {
        if (widgetZone == AdminWidgetZones.ProductDetailsBlock)
            return typeof(ServingSuggestionAdminViewComponent);

        return typeof(ServingSuggestionViewComponent);
    }

    /// <summary>
    /// Gets widget zones where this widget should be rendered
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the widget zones
    /// </returns>
    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string>
        {
            PublicWidgetZones.ProductDetailsBottom,
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
        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(ServingSuggestionsDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(ServingSuggestionsDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.ServingSuggestions.ServingSuggestion"] = "Serving suggestion",
            ["Plugins.Misc.ServingSuggestions.ServingSuggestion.AddNew"] = "Add serving suggestion",
            ["Plugins.Misc.ServingSuggestions.ServingSuggestion.SaveBeforeEdit"] = "You need to save the product before you can add a serving suggestion for this product page.",
            ["Plugins.Misc.ServingSuggestions.ServingSuggestion.Edit"] = "Edit serving suggestion",
            ["Plugins.Misc.ServingSuggestions.ServingSuggestion.DeleteConfirm"] = "Are you sure you want to delete this serving suggestion?",
            ["Plugins.Misc.ServingSuggestions.Added"] = "The serving suggestion has been added successfully.",
            ["Plugins.Misc.ServingSuggestions.Updated"] = "The serving suggestion has been updated successfully.",
            ["Plugins.Misc.ServingSuggestions.Deleted"] = "The serving suggestion has been deleted successfully.",
            ["Plugins.Misc.ServingSuggestions.Fields.Title"] = "Title",
            ["Plugins.Misc.ServingSuggestions.Fields.Title.Hint"] = "The title of the serving suggestion.",
            ["Plugins.Misc.ServingSuggestions.Fields.Title.Required"] = "Please provide a title.",
            ["Plugins.Misc.ServingSuggestions.Fields.Description"] = "Description",
            ["Plugins.Misc.ServingSuggestions.Fields.Description.Hint"] = "The description of the serving suggestion.",
            ["Plugins.Misc.ServingSuggestions.Fields.Picture"] = "Picture",
            ["Plugins.Misc.ServingSuggestions.Fields.Picture.Hint"] = "The image shown for this serving suggestion.",
            ["Plugins.Misc.ServingSuggestions.Fields.Picture.Required"] = "Please upload a picture.",
            ["Plugins.Misc.ServingSuggestions.Fields.Text"] = "Step",
            ["Plugins.Misc.ServingSuggestions.Fields.Text.Hint"] = "The text of this instruction step.",
            ["Plugins.Misc.ServingSuggestions.Fields.Text.Required"] = "Please provide the step text.",
            ["Plugins.Misc.ServingSuggestions.Fields.DisplayOrder"] = "Display order",
            ["Plugins.Misc.ServingSuggestions.Fields.DisplayOrder.Hint"] = "The order the step is displayed in.",
            ["Plugins.Misc.ServingSuggestions.Steps.AddNew"] = "Add step",
            ["Plugins.Misc.ServingSuggestions.Steps.EditStepDetails"] = "Edit step details"
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        //Picture is a shared core table, and SchemaMigration.Down() (which the framework runs right after
        //this method returns) drops the ServingSuggestion table - taking every PictureId value with it.
        //The picture rows themselves must therefore be purged here, before that happens; this cannot be
        //deferred to a step running after the table drop.
        var servingSuggestions = await _servingSuggestionRepository.GetAllAsync(query => query);
        foreach (var servingSuggestion in servingSuggestions)
        {
            var picture = await _pictureService.GetPictureByIdAsync(servingSuggestion.PictureId);
            if (picture != null)
                await _pictureService.DeletePictureAsync(picture);
        }

        //widget
        if (_widgetSettings.ActiveWidgetSystemNames.Contains(ServingSuggestionsDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Remove(ServingSuggestionsDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        //permissions (installed automatically; removal is not)
        var permissionRecords = await _permissionService.GetAllPermissionRecordsAsync();

        foreach (var systemName in new[]
                 {
                     ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_VIEW,
                     ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_CREATE_EDIT_DELETE
                 })
        {
            var permissionRecord = permissionRecords.FirstOrDefault(record => record.SystemName == systemName);
            if (permissionRecord != null)
                await _permissionService.DeletePermissionRecordAsync(permissionRecord);
        }

        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.ServingSuggestions");

        //LocalizedProperty is a shared core table our migration must never touch structurally, so rows with
        //LocaleKeyGroup = "ServingSuggestion"/"ServingSuggestionStep" would survive the table drop as orphans
        //unless purged explicitly here - this is the one cleanup step the automatic table-drop doesn't cover
        await _localizedPropertyRepository.DeleteAsync(property => property.LocaleKeyGroup == nameof(ServingSuggestion));
        await _localizedPropertyRepository.DeleteAsync(property => property.LocaleKeyGroup == nameof(ServingSuggestionStep));

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
