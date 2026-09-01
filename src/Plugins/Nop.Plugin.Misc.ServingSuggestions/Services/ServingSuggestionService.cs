using Nop.Core.Domain.Localization;
using Nop.Data;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Services.Localization;
using Nop.Services.Media;

namespace Nop.Plugin.Misc.ServingSuggestions.Services;

/// <summary>
/// Represents a serving suggestion service
/// </summary>
public class ServingSuggestionService : IServingSuggestionService
{
    #region Fields

    protected readonly ILocalizedEntityService _localizedEntityService;
    protected readonly INopDataProvider _dataProvider;
    protected readonly IPictureService _pictureService;
    protected readonly IRepository<ServingSuggestion> _servingSuggestionRepository;
    protected readonly IRepository<ServingSuggestionStep> _servingSuggestionStepRepository;
    protected readonly IRepository<LocalizedProperty> _localizedPropertyRepository;

    #endregion

    #region Ctor

    public ServingSuggestionService(ILocalizedEntityService localizedEntityService,
        INopDataProvider dataProvider,
        IPictureService pictureService,
        IRepository<ServingSuggestion> servingSuggestionRepository,
        IRepository<ServingSuggestionStep> servingSuggestionStepRepository,
        IRepository<LocalizedProperty> localizedPropertyRepository)
    {
        _localizedEntityService = localizedEntityService;
        _dataProvider = dataProvider;
        _pictureService = pictureService;
        _servingSuggestionRepository = servingSuggestionRepository;
        _servingSuggestionStepRepository = servingSuggestionStepRepository;
        _localizedPropertyRepository = localizedPropertyRepository;
    }

    #endregion

    #region Utilities

    protected virtual async Task SaveLocalizedValuesAsync(ServingSuggestion servingSuggestion, IList<ServingSuggestionLocalizedValue> localizedValues)
    {
        if (localizedValues == null)
            return;

        foreach (var localized in localizedValues)
        {
            await _localizedEntityService.SaveLocalizedValueAsync(servingSuggestion, x => x.Title, localized.Title, localized.LanguageId);
            await _localizedEntityService.SaveLocalizedValueAsync(servingSuggestion, x => x.Description, localized.Description, localized.LanguageId);
        }
    }

    protected virtual async Task SaveLocalizedValuesAsync(ServingSuggestionStep step, IList<ServingSuggestionStepLocalizedValue> localizedValues)
    {
        if (localizedValues == null)
            return;

        foreach (var localized in localizedValues)
        {
            await _localizedEntityService.SaveLocalizedValueAsync(step, x => x.Text, localized.Text, localized.LanguageId);
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a serving suggestion by identifier
    /// </summary>
    public virtual async Task<ServingSuggestion> GetServingSuggestionByIdAsync(int servingSuggestionId)
    {
        return await _servingSuggestionRepository.GetByIdAsync(servingSuggestionId, cache => default);
    }

    /// <summary>
    /// Gets the serving suggestion of a product
    /// </summary>
    public virtual async Task<ServingSuggestion> GetServingSuggestionByProductIdAsync(int productId)
    {
        var query = _servingSuggestionRepository.Table.Where(servingSuggestion => servingSuggestion.ProductId == productId);

        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Inserts a serving suggestion
    /// </summary>
    public virtual async Task InsertServingSuggestionAsync(ServingSuggestion servingSuggestion, IList<ServingSuggestionLocalizedValue> localizedValues = null)
    {
        ArgumentNullException.ThrowIfNull(servingSuggestion);

        using var transaction = _dataProvider.CreateTransactionScope();

        await _servingSuggestionRepository.InsertAsync(servingSuggestion);

        await SaveLocalizedValuesAsync(servingSuggestion, localizedValues);

        transaction.Complete();
    }

    /// <summary>
    /// Updates a serving suggestion
    /// </summary>
    public virtual async Task UpdateServingSuggestionAsync(ServingSuggestion servingSuggestion, IList<ServingSuggestionLocalizedValue> localizedValues = null)
    {
        ArgumentNullException.ThrowIfNull(servingSuggestion);

        using var transaction = _dataProvider.CreateTransactionScope();

        await _servingSuggestionRepository.UpdateAsync(servingSuggestion);

        await SaveLocalizedValuesAsync(servingSuggestion, localizedValues);

        transaction.Complete();
    }

    /// <summary>
    /// Deletes a serving suggestion, its steps, its localized values, and its picture. Deletion ordering is
    /// load-bearing: the Picture row is deleted last, because ServingSuggestion.PictureId has a DB cascade FK
    /// to Picture, so deleting the Picture first would silently take the ServingSuggestion/ServingSuggestionStep
    /// rows with it, ahead of this method's own explicit cleanup.
    /// </summary>
    public virtual async Task DeleteServingSuggestionAsync(ServingSuggestion servingSuggestion)
    {
        ArgumentNullException.ThrowIfNull(servingSuggestion);

        using var transaction = _dataProvider.CreateTransactionScope();

        var steps = await GetServingSuggestionStepsAsync(servingSuggestion.Id);

        await _localizedPropertyRepository.DeleteAsync(property =>
            property.EntityId == servingSuggestion.Id && property.LocaleKeyGroup == nameof(ServingSuggestion));

        foreach (var step in steps)
        {
            await _localizedPropertyRepository.DeleteAsync(property =>
                property.EntityId == step.Id && property.LocaleKeyGroup == nameof(ServingSuggestionStep));
        }

        //the ServingSuggestion row cascades ServingSuggestionStep rows automatically (real DB FK)
        await _servingSuggestionRepository.DeleteAsync(servingSuggestion);

        var pictureId = servingSuggestion.PictureId;
        var picture = await _pictureService.GetPictureByIdAsync(pictureId);
        if (picture != null)
            await _pictureService.DeletePictureAsync(picture);

        transaction.Complete();
    }

    /// <summary>
    /// Gets the steps of a serving suggestion, ordered by display order
    /// </summary>
    public virtual async Task<IList<ServingSuggestionStep>> GetServingSuggestionStepsAsync(int servingSuggestionId)
    {
        var query = _servingSuggestionStepRepository.Table
            .Where(step => step.ServingSuggestionId == servingSuggestionId)
            .OrderBy(step => step.DisplayOrder)
            .ThenBy(step => step.Id);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Gets a serving suggestion step by identifier
    /// </summary>
    public virtual async Task<ServingSuggestionStep> GetServingSuggestionStepByIdAsync(int servingSuggestionStepId)
    {
        return await _servingSuggestionStepRepository.GetByIdAsync(servingSuggestionStepId, cache => default);
    }

    /// <summary>
    /// Inserts a serving suggestion step
    /// </summary>
    public virtual async Task InsertServingSuggestionStepAsync(ServingSuggestionStep step, IList<ServingSuggestionStepLocalizedValue> localizedValues = null)
    {
        ArgumentNullException.ThrowIfNull(step);

        await _servingSuggestionStepRepository.InsertAsync(step);

        await SaveLocalizedValuesAsync(step, localizedValues);
    }

    /// <summary>
    /// Updates a serving suggestion step
    /// </summary>
    public virtual async Task UpdateServingSuggestionStepAsync(ServingSuggestionStep step, IList<ServingSuggestionStepLocalizedValue> localizedValues = null)
    {
        ArgumentNullException.ThrowIfNull(step);

        await _servingSuggestionStepRepository.UpdateAsync(step);

        await SaveLocalizedValuesAsync(step, localizedValues);
    }

    /// <summary>
    /// Deletes a serving suggestion step
    /// </summary>
    public virtual async Task DeleteServingSuggestionStepAsync(ServingSuggestionStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        await _localizedPropertyRepository.DeleteAsync(property =>
            property.EntityId == step.Id && property.LocaleKeyGroup == nameof(ServingSuggestionStep));

        await _servingSuggestionStepRepository.DeleteAsync(step);
    }

    #endregion
}
