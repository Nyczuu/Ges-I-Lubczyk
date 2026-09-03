using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Factories;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Models;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.ServingSuggestions.Admin.Controllers;

[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
[ValidateIpAddress]
[AuthorizeAdmin]
[SaveSelectedTab]
public class ServingSuggestionController : BasePluginController
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IPictureService _pictureService;
    protected readonly IServingSuggestionService _servingSuggestionService;
    protected readonly ServingSuggestionAdminModelFactory _servingSuggestionAdminModelFactory;

    #endregion

    #region Ctor

    public ServingSuggestionController(ILocalizationService localizationService,
        INotificationService notificationService,
        IPictureService pictureService,
        IServingSuggestionService servingSuggestionService,
        ServingSuggestionAdminModelFactory servingSuggestionAdminModelFactory)
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        _pictureService = pictureService;
        _servingSuggestionService = servingSuggestionService;
        _servingSuggestionAdminModelFactory = servingSuggestionAdminModelFactory;
    }

    #endregion

    #region Utilities

    protected virtual List<ServingSuggestionLocalizedValue> ToLocalizedValues(ServingSuggestionModel model)
    {
        return model.Locales
            .Select(locale => new ServingSuggestionLocalizedValue(locale.LanguageId, locale.Title, locale.Description))
            .ToList();
    }

    protected virtual List<ServingSuggestionStepLocalizedValue> ToLocalizedValues(ServingSuggestionStepModel model)
    {
        return model.Locales
            .Select(locale => new ServingSuggestionStepLocalizedValue(locale.LanguageId, locale.Text))
            .ToList();
    }

    #endregion

    #region Methods

    #region Serving suggestion (tab on the product-edit page)

    [CheckPermission(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_VIEW)]
    public virtual async Task<IActionResult> ServingSuggestionEditPopup(int productId)
    {
        var servingSuggestion = await _servingSuggestionService.GetServingSuggestionByProductIdAsync(productId);

        var model = await _servingSuggestionAdminModelFactory.PrepareServingSuggestionModelAsync(null, servingSuggestion, productId);

        return View("~/Plugins/Misc.ServingSuggestions/Admin/Views/ServingSuggestionEditPopup.cshtml", model);
    }

    [HttpPost]
    [FormValueRequired("save")]
    [CheckPermission(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ServingSuggestionEditPopup(ServingSuggestionModel model, IFormCollection form)
    {
        var servingSuggestion = await _servingSuggestionService.GetServingSuggestionByProductIdAsync(model.ProductId);

        //an old picture is only deleted after the write below succeeds, mirroring CategoryController.cs:294-299
        var prevPictureId = servingSuggestion?.PictureId ?? 0;

        //the model was bound (and validated) with whatever raw PictureId came from the form - that error
        //no longer reflects reality once the final PictureId (existing/uploaded) is resolved below, so it
        //is discarded here rather than trusted
        ModelState.Remove(nameof(model.PictureId));

        var files = form.Files.ToList();

        //only touch the picture upload once every other field is already known-valid. Inserting the
        //picture unconditionally (before this check) orphaned a Picture row on every failed resubmission:
        //the file input can't be re-populated on re-render, so a user fixing a Title error and resubmitting
        //would silently upload a second copy of the same file, with the first one never linked to anything
        //and never cleaned up
        if (ModelState.IsValid)
        {
            //picture upload, mirroring ProductController.ProductPictureAdd's IFormCollection-based upload
            if (files.Any())
            {
                var picture = await _pictureService.InsertPictureAsync(files[0]);
                model.PictureId = picture.Id;
            }
            else if (servingSuggestion != null)
            {
                //no new file this submission - keep the existing picture
                model.PictureId = servingSuggestion.PictureId;
            }

            if (model.PictureId <= 0)
            {
                ModelState.AddModelError(nameof(model.PictureId),
                    await _localizationService.GetResourceAsync("Plugins.Misc.ServingSuggestions.Fields.Picture.Required"));
            }
        }

        if (ModelState.IsValid)
        {
            if (servingSuggestion == null)
            {
                servingSuggestion = model.ToEntity<ServingSuggestion>();
                servingSuggestion.ProductId = model.ProductId;

                await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestion, ToLocalizedValues(model));

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.ServingSuggestions.Added"));
            }
            else
            {
                servingSuggestion = model.ToEntity(servingSuggestion);

                await _servingSuggestionService.UpdateServingSuggestionAsync(servingSuggestion, ToLocalizedValues(model));

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.ServingSuggestions.Updated"));
            }

            //delete the previous picture last (if it was replaced) - the write above must succeed first
            if (prevPictureId > 0 && prevPictureId != servingSuggestion.PictureId)
            {
                var prevPicture = await _pictureService.GetPictureByIdAsync(prevPictureId);
                if (prevPicture != null)
                    await _pictureService.DeletePictureAsync(prevPicture);
            }

            ViewBag.RefreshPage = true;

            return View("~/Plugins/Misc.ServingSuggestions/Admin/Views/ServingSuggestionEditPopup.cshtml", model);
        }

        model = await _servingSuggestionAdminModelFactory.PrepareServingSuggestionModelAsync(model, servingSuggestion, model.ProductId, true);

        return View("~/Plugins/Misc.ServingSuggestions/Admin/Views/ServingSuggestionEditPopup.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ServingSuggestionDelete(int productId)
    {
        var servingSuggestion = await _servingSuggestionService.GetServingSuggestionByProductIdAsync(productId)
            ?? throw new ArgumentException("No serving suggestion found for the specified product", nameof(productId));

        await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.ServingSuggestions.Deleted"));

        return new NullJsonResult();
    }

    #endregion

    #region Serving suggestion steps (nested grid on the product-edit page tab)

    [HttpPost]
    [CheckPermission(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_VIEW)]
    public virtual async Task<IActionResult> ServingSuggestionStepList(ServingSuggestionStepSearchModel searchModel)
    {
        var model = await _servingSuggestionAdminModelFactory.PrepareServingSuggestionStepListModelAsync(searchModel);

        return Json(model);
    }

    [CheckPermission(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ServingSuggestionStepCreatePopup(int servingSuggestionId)
    {
        var model = await _servingSuggestionAdminModelFactory.PrepareServingSuggestionStepModelAsync(new ServingSuggestionStepModel
        {
            ServingSuggestionId = servingSuggestionId
        }, null);

        return View("~/Plugins/Misc.ServingSuggestions/Admin/Views/ServingSuggestionStepCreatePopup.cshtml", model);
    }

    [HttpPost]
    [FormValueRequired("save")]
    [CheckPermission(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ServingSuggestionStepCreatePopup(ServingSuggestionStepModel model)
    {
        if (ModelState.IsValid)
        {
            var step = model.ToEntity<ServingSuggestionStep>();

            await _servingSuggestionService.InsertServingSuggestionStepAsync(step, ToLocalizedValues(model));

            ViewBag.RefreshPage = true;

            return View("~/Plugins/Misc.ServingSuggestions/Admin/Views/ServingSuggestionStepCreatePopup.cshtml", model);
        }

        model = await _servingSuggestionAdminModelFactory.PrepareServingSuggestionStepModelAsync(model, null, true);

        return View("~/Plugins/Misc.ServingSuggestions/Admin/Views/ServingSuggestionStepCreatePopup.cshtml", model);
    }

    [CheckPermission(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_VIEW)]
    public virtual async Task<IActionResult> ServingSuggestionStepEditPopup(int id)
    {
        var step = await _servingSuggestionService.GetServingSuggestionStepByIdAsync(id);
        if (step == null)
            return RedirectToAction(nameof(ServingSuggestionStepCreatePopup));

        var model = await _servingSuggestionAdminModelFactory.PrepareServingSuggestionStepModelAsync(null, step);

        return View("~/Plugins/Misc.ServingSuggestions/Admin/Views/ServingSuggestionStepEditPopup.cshtml", model);
    }

    [HttpPost]
    [FormValueRequired("save")]
    [CheckPermission(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ServingSuggestionStepEditPopup(ServingSuggestionStepModel model)
    {
        var step = await _servingSuggestionService.GetServingSuggestionStepByIdAsync(model.Id);
        if (step == null)
            return RedirectToAction(nameof(ServingSuggestionStepCreatePopup));

        if (ModelState.IsValid)
        {
            step = model.ToEntity(step);

            await _servingSuggestionService.UpdateServingSuggestionStepAsync(step, ToLocalizedValues(model));

            ViewBag.RefreshPage = true;

            return View("~/Plugins/Misc.ServingSuggestions/Admin/Views/ServingSuggestionStepEditPopup.cshtml", model);
        }

        model = await _servingSuggestionAdminModelFactory.PrepareServingSuggestionStepModelAsync(model, step, true);

        return View("~/Plugins/Misc.ServingSuggestions/Admin/Views/ServingSuggestionStepEditPopup.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ServingSuggestionStepUpdate(ServingSuggestionStepModel model)
    {
        var step = await _servingSuggestionService.GetServingSuggestionStepByIdAsync(model.Id)
            ?? throw new ArgumentException("No serving suggestion step found with the specified id");

        step.DisplayOrder = model.DisplayOrder;
        await _servingSuggestionService.UpdateServingSuggestionStepAsync(step);

        return new NullJsonResult();
    }

    [HttpPost]
    [CheckPermission(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ServingSuggestionStepDelete(int id)
    {
        var step = await _servingSuggestionService.GetServingSuggestionStepByIdAsync(id)
            ?? throw new ArgumentException("No serving suggestion step found with the specified id", nameof(id));

        await _servingSuggestionService.DeleteServingSuggestionStepAsync(step);

        return new NullJsonResult();
    }

    #endregion

    #endregion
}
