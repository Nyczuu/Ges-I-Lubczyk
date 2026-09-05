using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.ProductionLabels.Admin.Factories;
using Nop.Plugin.Misc.ProductionLabels.Admin.Models;
using Nop.Plugin.Misc.ProductionLabels.Domain;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Plugin.Misc.ProductionLabels.Services.Pdf;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Controllers;

[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
[ValidateIpAddress]
[AuthorizeAdmin]
[SaveSelectedTab]
public class ProductionLabelsAdminController : BasePluginController
{
    #region Fields

    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly IHtmlToPdfConverter _htmlToPdfConverter;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IProductionBatchService _productionBatchService;
    protected readonly IProductionLabelModelFactory _productionLabelModelFactory;
    protected readonly IProductService _productService;
    protected readonly ProductionLabelsAdminModelFactory _productionLabelsAdminModelFactory;

    #endregion

    #region Ctor

    public ProductionLabelsAdminController(IGenericAttributeService genericAttributeService,
        IHtmlToPdfConverter htmlToPdfConverter,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IProductionBatchService productionBatchService,
        IProductionLabelModelFactory productionLabelModelFactory,
        IProductService productService,
        ProductionLabelsAdminModelFactory productionLabelsAdminModelFactory)
    {
        _genericAttributeService = genericAttributeService;
        _htmlToPdfConverter = htmlToPdfConverter;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _productionBatchService = productionBatchService;
        _productionLabelModelFactory = productionLabelModelFactory;
        _productService = productService;
        _productionLabelsAdminModelFactory = productionLabelsAdminModelFactory;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Renders the label content to an HTML string. Extracted into its own overridable seam (mirroring
    /// IngredientsViewComponent.PrepareIngredientsModelAsync's own extraction, "testable without a real
    /// ViewComponentContext") so the stamp-only-after-success ordering can be exercised in tests without a
    /// real Razor view engine, which this test harness does not register.
    /// </summary>
    protected virtual async Task<string> RenderProductionLabelHtmlAsync(ProductionLabelModel model)
    {
        return await RenderPartialViewToStringAsync("~/Plugins/Misc.ProductionLabels/Admin/Views/ProductionLabelTemplate.cshtml", model);
    }

    #endregion

    #region Methods

    public virtual IActionResult Index()
    {
        return RedirectToAction(nameof(List));
    }

    [CheckPermission(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW)]
    public virtual async Task<IActionResult> List()
    {
        var model = await _productionLabelsAdminModelFactory.PrepareProductionBatchSearchModelAsync(new ProductionBatchSearchModel());

        return View("~/Plugins/Misc.ProductionLabels/Admin/Views/List.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW)]
    public virtual async Task<IActionResult> List(ProductionBatchSearchModel searchModel)
    {
        var model = await _productionLabelsAdminModelFactory.PrepareProductionBatchListModelAsync(searchModel);

        return Json(model);
    }

    [CheckPermission(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_CREATE)]
    public virtual async Task<IActionResult> ProductionBatchCreatePopup(int productId)
    {
        var model = await _productionLabelsAdminModelFactory.PrepareProductionBatchModelAsync(productId);

        return View("~/Plugins/Misc.ProductionLabels/Admin/Views/ProductionBatchCreatePopup.cshtml", model);
    }

    /// <summary>
    /// Reads the configured product's default shelf-life, in days - serves the standalone "Production"
    /// section's create-batch popup, whose product picker means the product isn't known until the admin
    /// makes a client-side selection (spec §6). Gated by the same permission as ProductionBatchCreatePopup,
    /// the action whose flow it serves (spec §7 correction). The explicit Json result type override is
    /// necessary: CheckPermissionAttribute's default resolution maps every GET request to Html (a redirect
    /// to AccessDenied) regardless of whether it's an AJAX call.
    /// </summary>
    [CheckPermission(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_CREATE,
        CheckPermissionAttribute.CheckPermissionResultType.Json)]
    public virtual async Task<IActionResult> GetDefaultShelfLifeDays(int productId)
    {
        var defaultShelfLifeDays = await _productionLabelsAdminModelFactory.GetDefaultShelfLifeDaysAsync(productId);

        return Json(new { DefaultShelfLifeDays = defaultShelfLifeDays });
    }

    [HttpPost]
    [FormValueRequired("save")]
    [CheckPermission(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_CREATE)]
    public virtual async Task<IActionResult> ProductionBatchCreatePopup(ProductionBatchModel model)
    {
        if (ModelState.IsValid)
        {
            var productionBatch = model.ToEntity<ProductionBatch>();

            await _productionBatchService.InsertProductionBatchAsync(productionBatch);

            ViewBag.RefreshPage = true;

            return View("~/Plugins/Misc.ProductionLabels/Admin/Views/ProductionBatchCreatePopup.cshtml", model);
        }

        if (model.ProductId == 0)
            model = await _productionLabelsAdminModelFactory.PrepareProductionBatchModelAsync(0);

        return View("~/Plugins/Misc.ProductionLabels/Admin/Views/ProductionBatchCreatePopup.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_DELETE)]
    public virtual async Task<IActionResult> ProductionBatchDelete(int id)
    {
        var productionBatch = await _productionBatchService.GetProductionBatchByIdAsync(id)
            ?? throw new ArgumentException("No production batch found with the specified id", nameof(id));

        try
        {
            await _productionBatchService.DeleteProductionBatchAsync(productionBatch);
        }
        catch (NopException exception)
        {
            //the batch already has a label generated - surface it rather than letting it propagate as an
            //unhandled error, mirroring IngredientsAdminController's composition-delete pattern
            _notificationService.ErrorNotification(exception.Message);

            return Json(new { Result = false });
        }

        return new NullJsonResult();
    }

    [CheckPermission(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW)]
    public virtual async Task<IActionResult> GenerateLabelPopup(int productionBatchId)
    {
        var model = await _productionLabelsAdminModelFactory.PrepareGenerateProductionLabelModelAsync(productionBatchId);

        return View("~/Plugins/Misc.ProductionLabels/Admin/Views/GenerateLabelPopup.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW)]
    public virtual async Task<IActionResult> GenerateLabel(GenerateProductionLabelModel model)
    {
        var productionBatch = await _productionBatchService.GetProductionBatchByIdAsync(model.ProductionBatchId)
            ?? throw new ArgumentException("No production batch found with the specified id", nameof(model));

        //GenerateLabelPopup's GET already pre-fills LanguageId with the resolved default (store's own
        //default language, or the round-10 first-by-DisplayOrder fallback), via the same helper used here -
        //this only re-resolves it if a submission somehow arrives without one
        var languageId = model.LanguageId ?? await _productionLabelsAdminModelFactory.ResolveCurrentStoreDefaultLanguageIdAsync();

        var labelModel = await _productionLabelModelFactory.PrepareProductionLabelModelAsync(productionBatch, languageId, model.SizeVariant);

        var html = await RenderProductionLabelHtmlAsync(labelModel);

        //the stamp only happens after ConvertAsync succeeds - a conversion failure (external-library
        //failure, no special swallowing) propagates normally and leaves the batch unlocked and deletable,
        //since no real label was produced (spec Section 6/Section 10)
        var pdfBytes = await _htmlToPdfConverter.ConvertAsync(html);
        await _productionBatchService.MarkLabelGeneratedAsync(productionBatch);

        var fileName = $"{labelModel.BatchCode}.pdf";

        return File(pdfBytes, MimeTypes.ApplicationPdf, fileName);
    }

    [HttpPost]
    [CheckPermission(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_CREATE)]
    public virtual async Task<IActionResult> SaveProductInfo(ProductionLabelsProductModel model)
    {
        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState.Values.SelectMany(state => state.Errors))
                _notificationService.ErrorNotification(error.ErrorMessage);

            return RedirectToAction("Edit", "Product", new { id = model.ProductId, area = AreaNames.ADMIN });
        }

        var product = await _productService.GetProductByIdAsync(model.ProductId)
            ?? throw new ArgumentException("No product found with the specified id", nameof(model));

        if (model.Locales.Any())
        {
            foreach (var locale in model.Locales)
            {
                await _genericAttributeService.SaveAttributeAsync(product,
                    ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + locale.LanguageId, locale.StorageConditions);
                await _genericAttributeService.SaveAttributeAsync(product,
                    ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + locale.LanguageId, locale.CountryOfOrigin);
            }
        }
        else
        {
            //Html.LocalizedEditorAsync renders the standard (non-tabbed) template whenever at most one
            //language is configured (HtmlExtensions.cs:46); that template posts the flat
            //StorageConditions/CountryOfOrigin fields directly against the model, not Locales[i].*, so the
            //model binder leaves Locales empty on this path - write the flat values against the resolved
            //default language instead of silently dropping the save
            var languageId = await _productionLabelsAdminModelFactory.ResolveCurrentStoreDefaultLanguageIdAsync();

            await _genericAttributeService.SaveAttributeAsync(product,
                ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + languageId, model.StorageConditions);
            await _genericAttributeService.SaveAttributeAsync(product,
                ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + languageId, model.CountryOfOrigin);
        }

        //not per-language (spec §5/§6) - saved once, regardless of which branch above ran
        await _genericAttributeService.SaveAttributeAsync(product,
            ProductionLabelsDefaults.DefaultShelfLifeDaysAttributeKey, model.DefaultShelfLifeDays);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.ProductionLabels.ProductInfo.Saved"));

        return RedirectToAction("Edit", "Product", new { id = model.ProductId, area = AreaNames.ADMIN });
    }

    #endregion
}
