using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.Ingredients.Admin.Factories;
using Nop.Plugin.Misc.Ingredients.Admin.Models;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.Ingredients.Admin.Controllers;

[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
[ValidateIpAddress]
[AuthorizeAdmin]
[SaveSelectedTab]
public class IngredientsAdminController : BasePluginController
{
    #region Fields

    protected readonly IIngredientCompositionService _ingredientCompositionService;
    protected readonly IIngredientService _ingredientService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IngredientAdminModelFactory _ingredientAdminModelFactory;
    protected readonly IProductIngredientService _productIngredientService;

    #endregion

    #region Ctor

    public IngredientsAdminController(IIngredientCompositionService ingredientCompositionService,
        IIngredientService ingredientService,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IngredientAdminModelFactory ingredientAdminModelFactory,
        IProductIngredientService productIngredientService)
    {
        _ingredientCompositionService = ingredientCompositionService;
        _ingredientService = ingredientService;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _ingredientAdminModelFactory = ingredientAdminModelFactory;
        _productIngredientService = productIngredientService;
    }

    #endregion

    #region Utilities

    protected virtual List<IngredientLocalizedValue> ToLocalizedValues(IngredientModel model)
    {
        return model.Locales
            .Select(locale => new IngredientLocalizedValue(locale.LanguageId, locale.Name, locale.Description))
            .ToList();
    }

    #endregion

    #region Methods

    #region Ingredients

    public virtual IActionResult Index()
    {
        return RedirectToAction(nameof(List));
    }

    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_VIEW)]
    public virtual async Task<IActionResult> List()
    {
        var model = await _ingredientAdminModelFactory.PrepareIngredientSearchModelAsync(new IngredientSearchModel());

        return View(model);
    }

    [HttpPost]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_VIEW)]
    public virtual async Task<IActionResult> List(IngredientSearchModel searchModel)
    {
        var model = await _ingredientAdminModelFactory.PrepareIngredientListModelAsync(searchModel);

        return Json(model);
    }

    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Create()
    {
        var model = await _ingredientAdminModelFactory.PrepareIngredientModelAsync(new IngredientModel(), null);

        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Create(IngredientModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var ingredient = model.ToEntity<Ingredient>();

            await _ingredientService.InsertIngredientAsync(ingredient, ToLocalizedValues(model));

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Ingredients.Added"));

            if (!continueEditing)
                return RedirectToAction(nameof(List));

            return RedirectToAction(nameof(Edit), new { id = ingredient.Id });
        }

        model = await _ingredientAdminModelFactory.PrepareIngredientModelAsync(model, null, true);

        return View(model);
    }

    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_VIEW)]
    public virtual async Task<IActionResult> Edit(int id)
    {
        var ingredient = await _ingredientService.GetIngredientByIdAsync(id);
        if (ingredient == null)
            return RedirectToAction(nameof(List));

        var model = await _ingredientAdminModelFactory.PrepareIngredientModelAsync(null, ingredient);

        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Edit(IngredientModel model, bool continueEditing)
    {
        var ingredient = await _ingredientService.GetIngredientByIdAsync(model.Id);
        if (ingredient == null)
            return RedirectToAction(nameof(List));

        if (ModelState.IsValid)
        {
            ingredient = model.ToEntity(ingredient);

            await _ingredientService.UpdateIngredientAsync(ingredient, ToLocalizedValues(model));

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Ingredients.Updated"));

            if (!continueEditing)
                return RedirectToAction(nameof(List));

            return RedirectToAction(nameof(Edit), new { id = ingredient.Id });
        }

        model = await _ingredientAdminModelFactory.PrepareIngredientModelAsync(model, ingredient, true);

        return View(model);
    }

    [HttpPost]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Delete(int id)
    {
        var ingredient = await _ingredientService.GetIngredientByIdAsync(id);
        if (ingredient == null)
            return RedirectToAction(nameof(List));

        await _ingredientService.DeleteIngredientAsync(ingredient);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Ingredients.Deleted"));

        return RedirectToAction(nameof(List));
    }

    #endregion

    #region Ingredient composition (nested grid on the Ingredient edit page)

    [HttpPost]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_VIEW)]
    public virtual async Task<IActionResult> IngredientCompositionList(IngredientCompositionSearchModel searchModel)
    {
        var model = await _ingredientAdminModelFactory.PrepareIngredientCompositionListModelAsync(searchModel);

        return Json(model);
    }

    [HttpPost]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> IngredientCompositionUpdate(IngredientCompositionModel model)
    {
        await _ingredientCompositionService.UpdateDisplayOrderAsync(model.Id, model.DisplayOrder);

        return new NullJsonResult();
    }

    [HttpPost]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> IngredientCompositionDelete(int id)
    {
        var ingredientComposition = await _ingredientCompositionService.GetIngredientCompositionByIdAsync(id)
            ?? throw new ArgumentException("No ingredient composition found with the specified id", nameof(id));

        try
        {
            await _ingredientCompositionService.RemoveChildIngredientAsync(ingredientComposition);
        }
        catch (NopException exception)
        {
            //a losing concurrent-write conflict (Plugins.Misc.Ingredients.Errors.ConcurrentConflict) -
            //surface it rather than letting it propagate as an unhandled error
            _notificationService.ErrorNotification(exception.Message);

            return Json(new { Result = false });
        }

        return new NullJsonResult();
    }

    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> IngredientCompositionAddPopup(int parentIngredientId)
    {
        var searchModel = await _ingredientAdminModelFactory.PrepareAddIngredientSearchModelAsync(new IngredientSearchModel
        {
            ParentIngredientId = parentIngredientId
        });

        return View(searchModel);
    }

    [HttpPost]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> IngredientCompositionAddPopupList(IngredientSearchModel searchModel)
    {
        var ingredients = await _ingredientService.GetAllIngredientsAsync(searchModel.SearchName,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        //exclude the composite ingredient itself from its own candidate-children list
        var model = await new IngredientListModel().PrepareToGridAsync(searchModel, ingredients, () =>
        {
            return ingredients
                .Where(ingredient => ingredient.Id != searchModel.ParentIngredientId)
                .Select(ingredient => ingredient.ToModel<IngredientModel>())
                .ToAsyncEnumerable();
        });

        return Json(model);
    }

    [HttpPost]
    [FormValueRequired("save")]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> IngredientCompositionAddPopup(AddIngredientCompositionModel model)
    {
        if (model.SelectedIngredientIds.Any())
        {
            var existingChildren = await _ingredientCompositionService.GetChildCompositionsAsync(model.ParentIngredientId);

            foreach (var childIngredientId in model.SelectedIngredientIds)
            {
                //whether this composition edge already exists
                if (existingChildren.Any(composition => composition.ChildIngredientId == childIngredientId))
                    continue;

                try
                {
                    await _ingredientCompositionService.AddChildIngredientAsync(model.ParentIngredientId, childIngredientId);
                }
                catch (NopException exception)
                {
                    //self-loop/cycle/max-depth validation, or a losing concurrent-write conflict
                    //(Plugins.Misc.Ingredients.Errors.ConcurrentConflict) - surface it rather than letting
                    //it propagate as an unhandled error, and stop processing the rest of this batch
                    _notificationService.ErrorNotification(exception.Message);

                    var errorSearchModel = await _ingredientAdminModelFactory.PrepareAddIngredientSearchModelAsync(new IngredientSearchModel
                    {
                        ParentIngredientId = model.ParentIngredientId
                    });

                    return View(errorSearchModel);
                }
            }
        }

        ViewBag.RefreshPage = true;

        return View(new IngredientSearchModel());
    }

    #endregion

    #region Product ingredients (tab on the product-edit page)

    [HttpPost]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_VIEW)]
    public virtual async Task<IActionResult> ProductIngredientList(ProductIngredientSearchModel searchModel)
    {
        var model = await _ingredientAdminModelFactory.PrepareProductIngredientListModelAsync(searchModel);

        return Json(model);
    }

    [HttpPost]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ProductIngredientUpdate(ProductIngredientModel model)
    {
        var productIngredient = await _productIngredientService.GetProductIngredientByIdAsync(model.Id)
            ?? throw new ArgumentException("No product ingredient mapping found with the specified id");

        productIngredient.DisplayOrder = model.DisplayOrder;
        await _productIngredientService.UpdateProductIngredientAsync(productIngredient);

        return new NullJsonResult();
    }

    [HttpPost]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ProductIngredientDelete(int id)
    {
        var productIngredient = await _productIngredientService.GetProductIngredientByIdAsync(id)
            ?? throw new ArgumentException("No product ingredient mapping found with the specified id", nameof(id));

        await _productIngredientService.DeleteProductIngredientAsync(productIngredient);

        return new NullJsonResult();
    }

    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ProductIngredientAddPopup(int productId)
    {
        var searchModel = await _ingredientAdminModelFactory.PrepareAddIngredientSearchModelAsync(new IngredientSearchModel
        {
            ProductId = productId
        });

        return View(searchModel);
    }

    [HttpPost]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ProductIngredientAddPopupList(IngredientSearchModel searchModel)
    {
        var ingredients = await _ingredientService.GetAllIngredientsAsync(searchModel.SearchName,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        var model = await new IngredientListModel().PrepareToGridAsync(searchModel, ingredients, () =>
        {
            return ingredients.Select(ingredient => ingredient.ToModel<IngredientModel>()).ToAsyncEnumerable();
        });

        return Json(model);
    }

    [HttpPost]
    [FormValueRequired("save")]
    [CheckPermission(IngredientsPermissionConfigManager.INGREDIENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> ProductIngredientAddPopup(AddProductIngredientModel model)
    {
        if (model.SelectedIngredientIds.Any())
        {
            var existingMappings = await _productIngredientService.GetProductIngredientsByProductIdAsync(model.ProductId);

            foreach (var ingredientId in model.SelectedIngredientIds)
            {
                //whether this product/ingredient mapping already exists
                if (existingMappings.Any(mapping => mapping.IngredientId == ingredientId))
                    continue;

                await _productIngredientService.InsertProductIngredientAsync(new ProductIngredientMapping
                {
                    ProductId = model.ProductId,
                    IngredientId = ingredientId
                });
            }
        }

        ViewBag.RefreshPage = true;

        return View(new IngredientSearchModel());
    }

    #endregion

    #endregion
}
