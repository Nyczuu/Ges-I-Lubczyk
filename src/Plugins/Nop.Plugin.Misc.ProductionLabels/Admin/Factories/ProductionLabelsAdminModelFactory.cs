using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.ProductionLabels.Admin.Models;
using Nop.Plugin.Misc.ProductionLabels.Domain;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework.Extensions;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Factories;

/// <summary>
/// Represents the production labels admin model factory: search/list/model preparation for both admin
/// surfaces (the product-edit tab and the standalone "Production" section), sharing one factory
/// </summary>
public class ProductionLabelsAdminModelFactory
{
    #region Fields

    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILocalizedModelFactory _localizedModelFactory;
    protected readonly IProductionBatchService _productionBatchService;
    protected readonly IProductService _productService;
    protected readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public ProductionLabelsAdminModelFactory(IGenericAttributeService genericAttributeService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        ILocalizedModelFactory localizedModelFactory,
        IProductionBatchService productionBatchService,
        IProductService productService,
        IStoreContext storeContext)
    {
        _genericAttributeService = genericAttributeService;
        _languageService = languageService;
        _localizationService = localizationService;
        _localizedModelFactory = localizedModelFactory;
        _productionBatchService = productionBatchService;
        _productService = productService;
        _storeContext = storeContext;
    }

    #endregion

    #region Utilities

    protected virtual async Task PrepareAvailableProductsAsync(IList<SelectListItem> items)
    {
        var products = await _productService.SearchProductsAsync(showHidden: true);

        foreach (var product in products)
        {
            items.Add(new SelectListItem { Text = product.Name, Value = product.Id.ToString() });
        }
    }

    protected virtual async Task PrepareAvailableSizeVariantsAsync(IList<SelectListItem> items)
    {
        foreach (var sizeVariant in Enum.GetValues<ProductionLabelSizeVariant>())
        {
            items.Add(new SelectListItem
            {
                Text = await _localizationService.GetLocalizedEnumAsync(sizeVariant),
                Value = ((int)sizeVariant).ToString()
            });
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepare production batch search model for the standalone "Production" section, including its own
    /// "filter by product" dropdown (the product-edit page tab's own search model is scoped and populated
    /// separately via <see cref="PrepareProductionLabelsProductModelAsync"/> and never reaches this method)
    /// </summary>
    public virtual async Task<ProductionBatchSearchModel> PrepareProductionBatchSearchModelAsync(ProductionBatchSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        searchModel.AvailableProducts.Add(new SelectListItem
        {
            Text = await _localizationService.GetResourceAsync("Admin.Common.All"),
            Value = "0"
        });
        await PrepareAvailableProductsAsync(searchModel.AvailableProducts);

        searchModel.SetGridPageSize();

        return searchModel;
    }

    /// <summary>
    /// Prepare paged production batch list model
    /// </summary>
    public virtual async Task<ProductionBatchListModel> PrepareProductionBatchListModelAsync(ProductionBatchSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var productId = searchModel.SearchProductId > 0 ? searchModel.SearchProductId : (int?)null;

        var productionBatches = await _productionBatchService.GetAllProductionBatchesAsync(productId,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        var productIds = productionBatches.Select(batch => batch.ProductId).Distinct().ToArray();
        var products = await _productService.GetProductsByIdsAsync(productIds);

        var model = await new ProductionBatchListModel().PrepareToGridAsync(searchModel, productionBatches, () =>
        {
            return productionBatches.SelectAwait(async batch =>
            {
                var batchModel = batch.ToModel<ProductionBatchModel>();

                batchModel.ProductName = products
                    .FirstOrDefault(product => product.Id == batch.ProductId)?.Name;

                return batchModel;
            });
        });

        return model;
    }

    /// <summary>
    /// Prepare a production batch model for the create popup
    /// </summary>
    /// <param name="productId">Product identifier if reached from a specific product's own edit-page tab; 0 if reached from the standalone section (in which case a product picker is populated)</param>
    public virtual async Task<ProductionBatchModel> PrepareProductionBatchModelAsync(int productId)
    {
        var model = new ProductionBatchModel
        {
            ProductId = productId,
            ProductionDateUtc = DateTime.UtcNow.Date,
            BestBeforeDateUtc = DateTime.UtcNow.Date
        };

        if (productId == 0)
            await PrepareAvailableProductsAsync(model.AvailableProducts);

        return model;
    }

    /// <summary>
    /// Prepare the product-edit page tab model: the per-(system-configured-)language storage
    /// conditions/country of origin editor, plus the nested batch-history grid search model
    /// </summary>
    public virtual async Task<ProductionLabelsProductModel> PrepareProductionLabelsProductModelAsync(int productId)
    {
        var model = new ProductionLabelsProductModel
        {
            ProductId = productId
        };

        model.BatchSearchModel.SearchProductId = productId;
        model.BatchSearchModel.SetGridPageSize();

        if (productId > 0)
        {
            var product = await _productService.GetProductByIdAsync(productId);

            model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync<ProductionLabelsProductLocalizedModel>(async (locale, languageId) =>
            {
                locale.StorageConditions = await _genericAttributeService.GetAttributeAsync<string>(product,
                    ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + languageId);
                locale.CountryOfOrigin = await _genericAttributeService.GetAttributeAsync<string>(product,
                    ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + languageId);
            });

            //Html.LocalizedEditorAsync renders the standard (non-tabbed) template - bound directly to these
            //flat properties via asp-for, not to Locales[i].* - whenever Model.Locales.Count is 0 or 1
            //(HtmlExtensions.cs:46), which is the out-of-the-box nopCommerce default. Populate them from the
            //resolved default language's own locale entry so that path shows existing data instead of a
            //blank form; falls back to whichever locale happens to be first when the resolved default
            //language isn't among model.Locales at all (e.g. DefaultLanguageId not mapped to this store).
            var defaultLanguageId = await ResolveCurrentStoreDefaultLanguageIdAsync();
            var defaultLocale = model.Locales.FirstOrDefault(locale => locale.LanguageId == defaultLanguageId)
                ?? model.Locales.FirstOrDefault();

            if (defaultLocale != null)
            {
                model.StorageConditions = defaultLocale.StorageConditions;
                model.CountryOfOrigin = defaultLocale.CountryOfOrigin;
            }
        }

        return model;
    }

    /// <summary>
    /// Prepare the "Generate label" popup's options model
    /// </summary>
    public virtual async Task<GenerateProductionLabelModel> PrepareGenerateProductionLabelModelAsync(int productionBatchId)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var storeLanguages = await _languageService.GetAllLanguagesAsync(storeId: store.Id);

        var model = new GenerateProductionLabelModel
        {
            ProductionBatchId = productionBatchId,
            SizeVariant = ProductionLabelSizeVariant.SmallJar,
            LanguageId = await ResolveDefaultLanguageIdAsync(store, storeLanguages)
        };

        await PrepareAvailableSizeVariantsAsync(model.AvailableSizeVariants);

        if (storeLanguages.Count > 1)
        {
            foreach (var language in storeLanguages)
            {
                model.AvailableLanguages.Add(new SelectListItem { Text = language.Name, Value = language.Id.ToString() });
            }
        }

        return model;
    }

    /// <summary>
    /// Resolves the default label language: the store's own default language, or - when the store's
    /// DefaultLanguageId is unset (0) - the first language by display order among the store's active
    /// languages (mirroring the doc comment on Store.DefaultLanguageId itself: "0 is set when we use the
    /// default language display order")
    /// </summary>
    public virtual Task<int> ResolveDefaultLanguageIdAsync(Store store, IList<Language> storeLanguages)
    {
        ArgumentNullException.ThrowIfNull(store);

        if (store.DefaultLanguageId > 0)
            return Task.FromResult(store.DefaultLanguageId);

        return Task.FromResult(storeLanguages?.FirstOrDefault()?.Id ?? 0);
    }

    /// <summary>
    /// Resolves the default label language for the current store, fetching the store and its active
    /// languages itself. Used by <see cref="PrepareGenerateProductionLabelModelAsync"/> to pre-fill the
    /// popup, and by the controller as a defensive fallback if a "Generate label" submission somehow
    /// arrives with no language selected at all.
    /// </summary>
    public virtual async Task<int> ResolveCurrentStoreDefaultLanguageIdAsync()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var storeLanguages = await _languageService.GetAllLanguagesAsync(storeId: store.Id);

        return await ResolveDefaultLanguageIdAsync(store, storeLanguages);
    }

    #endregion
}
