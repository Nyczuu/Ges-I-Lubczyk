using Nop.Plugin.Misc.ServingSuggestions.Admin.Models;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework.Extensions;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.ServingSuggestions.Admin.Factories;

/// <summary>
/// Represents the serving suggestion admin model factory
/// </summary>
public class ServingSuggestionAdminModelFactory
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly ILocalizedModelFactory _localizedModelFactory;
    protected readonly IPictureService _pictureService;
    protected readonly IServingSuggestionService _servingSuggestionService;

    #endregion

    #region Ctor

    public ServingSuggestionAdminModelFactory(ILocalizationService localizationService,
        ILocalizedModelFactory localizedModelFactory,
        IPictureService pictureService,
        IServingSuggestionService servingSuggestionService)
    {
        _localizationService = localizationService;
        _localizedModelFactory = localizedModelFactory;
        _pictureService = pictureService;
        _servingSuggestionService = servingSuggestionService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepare a serving suggestion model
    /// </summary>
    /// <param name="model">Serving suggestion model, or null to build a fresh one</param>
    /// <param name="servingSuggestion">Serving suggestion entity, or null if the product has none yet</param>
    /// <param name="productId">Product identifier</param>
    /// <param name="excludeProperties">Whether to exclude populating the model's properties from the entity</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the serving suggestion model
    /// </returns>
    public virtual async Task<ServingSuggestionModel> PrepareServingSuggestionModelAsync(ServingSuggestionModel model,
        ServingSuggestion servingSuggestion, int productId, bool excludeProperties = false)
    {
        Func<ServingSuggestionLocalizedModel, int, Task> localizedModelConfiguration = null;

        if (servingSuggestion != null)
        {
            model ??= servingSuggestion.ToModel<ServingSuggestionModel>();

            model.HasServingSuggestion = true;
            model.PictureUrl = await _pictureService.GetPictureUrlAsync(servingSuggestion.PictureId);

            await PrepareServingSuggestionStepSearchModelAsync(model.ServingSuggestionStepSearchModel, servingSuggestion.Id);

            localizedModelConfiguration = async (locale, languageId) =>
            {
                locale.Title = await _localizationService.GetLocalizedAsync(servingSuggestion, entity => entity.Title, languageId, false, false);
                locale.Description = await _localizationService.GetLocalizedAsync(servingSuggestion, entity => entity.Description, languageId, false, false);
            };
        }

        model ??= new ServingSuggestionModel();
        model.ProductId = productId;

        if (!excludeProperties)
            model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync(localizedModelConfiguration);

        return model;
    }

    /// <summary>
    /// Prepare serving suggestion step search model
    /// </summary>
    public virtual Task<ServingSuggestionStepSearchModel> PrepareServingSuggestionStepSearchModelAsync(ServingSuggestionStepSearchModel searchModel, int servingSuggestionId)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        searchModel.ServingSuggestionId = servingSuggestionId;
        searchModel.SetGridPageSize();

        return Task.FromResult(searchModel);
    }

    /// <summary>
    /// Prepare paged serving suggestion step list model
    /// </summary>
    public virtual async Task<ServingSuggestionStepListModel> PrepareServingSuggestionStepListModelAsync(ServingSuggestionStepSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var steps = (await _servingSuggestionService.GetServingSuggestionStepsAsync(searchModel.ServingSuggestionId))
            .ToPagedList(searchModel);

        var model = await new ServingSuggestionStepListModel().PrepareToGridAsync(searchModel, steps, () =>
        {
            return steps.SelectAwait(async step =>
            {
                var stepModel = step.ToModel<ServingSuggestionStepModel>();
                stepModel.Text = await _localizationService.GetLocalizedAsync(step, entity => entity.Text);

                return stepModel;
            });
        });

        return model;
    }

    /// <summary>
    /// Prepare a serving suggestion step model
    /// </summary>
    public virtual async Task<ServingSuggestionStepModel> PrepareServingSuggestionStepModelAsync(ServingSuggestionStepModel model,
        ServingSuggestionStep step, bool excludeProperties = false)
    {
        Func<ServingSuggestionStepLocalizedModel, int, Task> localizedModelConfiguration = null;

        if (step != null)
        {
            model ??= step.ToModel<ServingSuggestionStepModel>();

            localizedModelConfiguration = async (locale, languageId) =>
            {
                locale.Text = await _localizationService.GetLocalizedAsync(step, entity => entity.Text, languageId, false, false);
            };
        }

        model ??= new ServingSuggestionStepModel();

        if (!excludeProperties)
            model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync(localizedModelConfiguration);

        return model;
    }

    #endregion
}
