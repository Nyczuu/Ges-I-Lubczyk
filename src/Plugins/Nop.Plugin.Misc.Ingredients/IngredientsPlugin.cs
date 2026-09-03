using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Localization;
using Nop.Data;
using Nop.Plugin.Misc.Ingredients.Admin.Components;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Public.Components;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.Ingredients;

/// <summary>
/// Represents the Ingredients plugin
/// </summary>
public class IngredientsPlugin : BasePlugin, IMiscPlugin, IWidgetPlugin
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly IPermissionService _permissionService;
    protected readonly IRepository<LocalizedProperty> _localizedPropertyRepository;
    protected readonly ISettingService _settingService;
    protected readonly WidgetSettings _widgetSettings;

    #endregion

    #region Ctor

    public IngredientsPlugin(ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        IPermissionService permissionService,
        IRepository<LocalizedProperty> localizedPropertyRepository,
        ISettingService settingService,
        WidgetSettings widgetSettings)
    {
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _permissionService = permissionService;
        _localizedPropertyRepository = localizedPropertyRepository;
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
        return _nopUrlHelper.RouteUrl(IngredientsDefaults.Routes.Admin.ListRouteName);
    }

    /// <summary>
    /// Gets a type of a view component for displaying widget
    /// </summary>
    /// <param name="widgetZone">Name of the widget zone</param>
    /// <returns>View component type</returns>
    public Type GetWidgetViewComponent(string widgetZone)
    {
        if (widgetZone == AdminWidgetZones.ProductDetailsBlock)
            return typeof(ProductIngredientsAdminViewComponent);

        return typeof(IngredientsViewComponent);
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
            PublicWidgetZones.ProductDetailsBeforeCollateral,
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
        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(IngredientsDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(IngredientsDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.Ingredients.Ingredients"] = "Ingredients",
            ["Plugins.Misc.Ingredients.Ingredients.List"] = "Ingredients",
            ["Plugins.Misc.Ingredients.Ingredients.AddNew"] = "Add new ingredient",
            ["Plugins.Misc.Ingredients.Ingredients.EditIngredientDetails"] = "Edit ingredient details",
            ["Plugins.Misc.Ingredients.Ingredients.BackToList"] = "back to ingredient list",
            ["Plugins.Misc.Ingredients.Ingredients.Added"] = "The ingredient has been added successfully.",
            ["Plugins.Misc.Ingredients.Ingredients.Updated"] = "The ingredient has been updated successfully.",
            ["Plugins.Misc.Ingredients.Ingredients.Deleted"] = "The ingredient has been deleted successfully.",
            ["Plugins.Misc.Ingredients.Ingredients.Info"] = "Ingredient info",
            ["Plugins.Misc.Ingredients.Ingredients.Composition"] = "Composition",
            ["Plugins.Misc.Ingredients.Fields.Name"] = "Name",
            ["Plugins.Misc.Ingredients.Fields.Name.Hint"] = "The name of the ingredient.",
            ["Plugins.Misc.Ingredients.Fields.Name.Required"] = "Please provide a name.",
            ["Plugins.Misc.Ingredients.Fields.Description"] = "Description",
            ["Plugins.Misc.Ingredients.Fields.Description.Hint"] = "The description of the ingredient.",
            ["Plugins.Misc.Ingredients.Fields.Allergen"] = "Allergen",
            ["Plugins.Misc.Ingredients.Fields.Allergen.Hint"] = "The EU-regulated allergen this ingredient is classified as, if any.",
            ["Plugins.Misc.Ingredients.Fields.CaloriesPer100g"] = "Calories per 100g (kcal)",
            ["Plugins.Misc.Ingredients.Fields.CaloriesPer100g.Hint"] = "The energy value of this ingredient, in kilocalories per 100g.",
            ["Plugins.Misc.Ingredients.Fields.CaloriesPer100g.GreaterThanOrEqualZero"] = "Calories per 100g must be zero or greater.",
            ["Plugins.Misc.Ingredients.Fields.ProteinPer100g"] = "Protein per 100g (g)",
            ["Plugins.Misc.Ingredients.Fields.ProteinPer100g.Hint"] = "The protein content of this ingredient, in grams per 100g.",
            ["Plugins.Misc.Ingredients.Fields.ProteinPer100g.GreaterThanOrEqualZero"] = "Protein per 100g must be zero or greater.",
            ["Plugins.Misc.Ingredients.Fields.FatPer100g"] = "Fat per 100g (g)",
            ["Plugins.Misc.Ingredients.Fields.FatPer100g.Hint"] = "The fat content of this ingredient, in grams per 100g.",
            ["Plugins.Misc.Ingredients.Fields.FatPer100g.GreaterThanOrEqualZero"] = "Fat per 100g must be zero or greater.",
            ["Plugins.Misc.Ingredients.Fields.CarbohydratePer100g"] = "Carbohydrate per 100g (g)",
            ["Plugins.Misc.Ingredients.Fields.CarbohydratePer100g.Hint"] = "The carbohydrate content of this ingredient, in grams per 100g.",
            ["Plugins.Misc.Ingredients.Fields.CarbohydratePer100g.GreaterThanOrEqualZero"] = "Carbohydrate per 100g must be zero or greater.",
            ["Plugins.Misc.Ingredients.Fields.DisplayOrder"] = "Display order",
            ["Plugins.Misc.Ingredients.Fields.DisplayOrder.Hint"] = "The order the ingredient is displayed in.",
            ["Plugins.Misc.Ingredients.Fields.Ingredient"] = "Ingredient",
            ["Plugins.Misc.Ingredients.Composition.AddNew"] = "Add ingredient",
            ["Plugins.Misc.Ingredients.Composition.SearchIngredientName"] = "Ingredient name",
            ["Plugins.Misc.Ingredients.ProductIngredients"] = "Ingredients",
            ["Plugins.Misc.Ingredients.ProductIngredients.AddNew"] = "Add ingredient",
            ["Plugins.Misc.Ingredients.ProductIngredients.SaveBeforeEdit"] = "You need to save the product before you can add ingredients for this product page.",
            ["Plugins.Misc.Ingredients.Errors.SelfLoop"] = "An ingredient cannot be made of itself.",
            ["Plugins.Misc.Ingredients.Errors.Cycle"] = "This would create a cycle: the ingredient you are adding already contains the composite ingredient you are adding it to.",
            ["Plugins.Misc.Ingredients.Errors.MaxDepthExceeded"] = "This composition would exceed the maximum allowed nesting depth.",
            ["Plugins.Misc.Ingredients.Errors.ConcurrentConflict"] = "Someone else changed this at the same time. Please try again.",
            ["Plugins.Misc.Ingredients.Errors.InUseByIngredients"] = "This ingredient cannot be deleted because it is still used in the composition of: {0}.",
            ["Plugins.Misc.Ingredients.Errors.InUseByProducts"] = "This ingredient cannot be deleted because the following products still use it: {0}.",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.None"] = "None",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.CerealsContainingGluten"] = "Cereals containing gluten",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Crustaceans"] = "Crustaceans",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Eggs"] = "Eggs",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Fish"] = "Fish",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Peanuts"] = "Peanuts",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Soybeans"] = "Soybeans",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Milk"] = "Milk",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Nuts"] = "Nuts",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Celery"] = "Celery",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Mustard"] = "Mustard",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.SesameSeeds"] = "Sesame seeds",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.SulphurDioxideAndSulphites"] = "Sulphur dioxide and sulphites",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Lupin"] = "Lupin",
            ["Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Molluscs"] = "Molluscs"
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
        if (_widgetSettings.ActiveWidgetSystemNames.Contains(IngredientsDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Remove(IngredientsDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        //permissions (installed automatically; removal is not)
        var permissionRecords = await _permissionService.GetAllPermissionRecordsAsync();

        foreach (var systemName in new[]
                 {
                     IngredientsPermissionConfigManager.INGREDIENTS_VIEW,
                     IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE
                 })
        {
            var permissionRecord = permissionRecords.FirstOrDefault(record => record.SystemName == systemName);
            if (permissionRecord != null)
                await _permissionService.DeletePermissionRecordAsync(permissionRecord);
        }

        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.Ingredients");
        await _localizationService.DeleteLocaleResourcesAsync("Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType");

        //LocalizedProperty is a shared core table our migration must never touch structurally,
        //so rows with LocaleKeyGroup = "Ingredient" would survive the table drop as orphans unless
        //purged explicitly here - this is the one cleanup step the automatic table-drop doesn't cover
        await _localizedPropertyRepository.DeleteAsync(property => property.LocaleKeyGroup == nameof(Ingredient));

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
