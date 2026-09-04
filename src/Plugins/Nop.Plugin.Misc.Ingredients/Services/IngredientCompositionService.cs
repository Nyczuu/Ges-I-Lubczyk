using Npgsql;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.Ingredients.Services;

/// <summary>
/// Represents an ingredient composition service
/// </summary>
public class IngredientCompositionService : IIngredientCompositionService
{
    #region Fields

    protected readonly INopDataProvider _dataProvider;
    protected readonly ILocalizationService _localizationService;
    protected readonly IRepository<Ingredient> _ingredientRepository;
    protected readonly IRepository<IngredientClosure> _ingredientClosureRepository;
    protected readonly IRepository<IngredientComposition> _ingredientCompositionRepository;

    #endregion

    #region Ctor

    public IngredientCompositionService(INopDataProvider dataProvider,
        ILocalizationService localizationService,
        IRepository<Ingredient> ingredientRepository,
        IRepository<IngredientClosure> ingredientClosureRepository,
        IRepository<IngredientComposition> ingredientCompositionRepository)
    {
        _dataProvider = dataProvider;
        _localizationService = localizationService;
        _ingredientRepository = ingredientRepository;
        _ingredientClosureRepository = ingredientClosureRepository;
        _ingredientCompositionRepository = ingredientCompositionRepository;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Validates a candidate composition edge against the current closure: rejects a self-loop, a cycle,
    /// or an edge whose realized composition path would exceed the maximum allowed depth
    /// </summary>
    protected virtual async Task ValidateNewEdgeAsync(int parentIngredientId, int childIngredientId)
    {
        if (parentIngredientId == childIngredientId)
            throw new NopException(await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Errors.SelfLoop"));

        var closureTable = _ingredientClosureRepository.Table;

        //cycle: child is already an ancestor of parent (a row (child, parent, *) exists)
        var wouldCycle = await closureTable
            .AnyAsync(c => c.AncestorIngredientId == childIngredientId && c.DescendantIngredientId == parentIngredientId);

        if (wouldCycle)
            throw new NopException(await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Errors.Cycle"));

        //depth: longest path through every ancestor of parent (including parent itself) and every
        //descendant of child (including child itself) must not exceed the maximum allowed depth
        var maxAncestorDepth = await closureTable
            .Where(c => c.DescendantIngredientId == parentIngredientId)
            .Select(c => (int?)c.Depth)
            .MaxAsync() ?? 0;

        var maxDescendantDepth = await closureTable
            .Where(c => c.AncestorIngredientId == childIngredientId)
            .Select(c => (int?)c.Depth)
            .MaxAsync() ?? 0;

        var realizedDepth = maxAncestorDepth + 1 + maxDescendantDepth;

        if (realizedDepth > IngredientsDefaults.MaxCompositionDepth)
            throw new NopException(await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Errors.MaxDepthExceeded"));
    }

    /// <summary>
    /// Determines whether the given exception (or an exception it wraps) is a PostgreSQL serialization
    /// failure - the SQLSTATE 40001 error a losing transaction gets when Serializable isolation detects a
    /// conflict with another concurrent transaction (e.g. two admins each adding one edge of the same
    /// would-be cycle; see spec Q11/Q4 and "Cycle prevention" in Docs/BusinessLogic/product-ingredients.md).
    /// Walks a single InnerException hop because a driver/ORM layer may wrap the original exception.
    /// </summary>
    public static bool IsSerializationFailure(Exception exception)
    {
        return exception switch
        {
            null => false,
            PostgresException postgresException => postgresException.SqlState == PostgresErrorCodes.SerializationFailure,
            _ => IsSerializationFailure(exception.InnerException)
        };
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the direct child compositions of a (composite) ingredient
    /// </summary>
    public virtual async Task<IList<IngredientComposition>> GetChildCompositionsAsync(int parentIngredientId)
    {
        var query = _ingredientCompositionRepository.Table
            .Where(c => c.ParentIngredientId == parentIngredientId)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Id);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Gets which of the given ingredients are themselves composite (have at least one direct child
    /// composition), for marking a "multi-ingredient composition" indicator in a grid without an
    /// N+1 lookup per row
    /// </summary>
    public virtual async Task<IList<int>> GetCompositeIngredientIdsAsync(IEnumerable<int> ingredientIds)
    {
        var ids = ingredientIds?.ToArray() ?? [];

        if (!ids.Any())
            return [];

        return await _ingredientCompositionRepository.Table
            .Where(composition => ids.Contains(composition.ParentIngredientId))
            .Select(composition => composition.ParentIngredientId)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Gets an ingredient composition by identifier
    /// </summary>
    public virtual async Task<IngredientComposition> GetIngredientCompositionByIdAsync(int ingredientCompositionId)
    {
        return await _ingredientCompositionRepository.GetByIdAsync(ingredientCompositionId, cache => default);
    }

    /// <summary>
    /// Adds a child (component) ingredient to a composite ingredient. Uniqueness of
    /// (ParentIngredientId, ChildIngredientId) is enforced here with a check-then-insert, matching the
    /// precedent of ProductController's RelatedProductAddPopup/FilterLevelValuesAddPopup: if the edge
    /// already exists, this is a silent no-op rather than a duplicate row or a thrown error.
    /// </summary>
    public virtual async Task AddChildIngredientAsync(int parentIngredientId, int childIngredientId, int displayOrder = 0)
    {
        using var transaction = _dataProvider.CreateTransactionScope();

        try
        {
            var alreadyExists = await _ingredientCompositionRepository.Table
                .AnyAsync(composition => composition.ParentIngredientId == parentIngredientId
                    && composition.ChildIngredientId == childIngredientId);

            if (!alreadyExists)
            {
                await ValidateNewEdgeAsync(parentIngredientId, childIngredientId);

                await _ingredientCompositionRepository.InsertAsync(new IngredientComposition
                {
                    ParentIngredientId = parentIngredientId,
                    ChildIngredientId = childIngredientId,
                    DisplayOrder = displayOrder
                });

                await RecomputeClosureAsync();
            }

            transaction.Complete();
        }
        catch (Exception exception) when (IsSerializationFailure(exception))
        {
            //the losing side of a concurrent conflict (e.g. two admins each adding one edge of the same
            //would-be cycle) - surface the generic retry message resolved for open question 4, rather than
            //the raw provider exception
            throw new NopException(await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Errors.ConcurrentConflict"), exception);
        }
    }

    /// <summary>
    /// Updates the display order of an ingredient composition edge
    /// </summary>
    public virtual async Task UpdateDisplayOrderAsync(int ingredientCompositionId, int displayOrder)
    {
        var ingredientComposition = await GetIngredientCompositionByIdAsync(ingredientCompositionId)
            ?? throw new NopException("No ingredient composition found with the specified id");

        ingredientComposition.DisplayOrder = displayOrder;

        await _ingredientCompositionRepository.UpdateAsync(ingredientComposition);
    }

    /// <summary>
    /// Removes a child ingredient from a composite ingredient, and recomputes the closure
    /// </summary>
    public virtual async Task RemoveChildIngredientAsync(IngredientComposition ingredientComposition)
    {
        ArgumentNullException.ThrowIfNull(ingredientComposition);

        using var transaction = _dataProvider.CreateTransactionScope();

        try
        {
            await _ingredientCompositionRepository.DeleteAsync(ingredientComposition);

            await RecomputeClosureAsync();

            transaction.Complete();
        }
        catch (Exception exception) when (IsSerializationFailure(exception))
        {
            //the full-closure-rewrite-on-every-write design means any two concurrent composition writes
            //can conflict, not only concurrent adds - see spec Q11/Q4
            throw new NopException(await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Errors.ConcurrentConflict"), exception);
        }
    }

    /// <summary>
    /// Recomputes the entire ingredient closure from scratch, from the current set of composition edges
    /// </summary>
    public virtual async Task RecomputeClosureAsync()
    {
        var ingredientIds = await _ingredientRepository.Table.Select(i => i.Id).ToListAsync();
        var edges = await _ingredientCompositionRepository.Table.ToListAsync();

        //reflexive rows: every ingredient is its own ancestor/descendant at depth 0
        var closure = new Dictionary<(int Ancestor, int Descendant), int>();
        foreach (var id in ingredientIds)
            closure[(id, id)] = 0;

        //fixed-point join against the composition edges, bounded to the depth cap itself
        //(a realizable path longer than that is rejected at write time, so it can never occur here)
        for (var round = 0; round < IngredientsDefaults.MaxCompositionDepth; round++)
        {
            var changed = false;

            foreach (var edge in edges)
            {
                foreach (var pair in closure.Keys.Where(k => k.Descendant == edge.ParentIngredientId).ToList())
                {
                    var candidateDepth = closure[pair] + 1;
                    var key = (pair.Ancestor, edge.ChildIngredientId);

                    if (!closure.TryGetValue(key, out var existingDepth) || candidateDepth > existingDepth)
                    {
                        closure[key] = candidateDepth;
                        changed = true;
                    }
                }
            }

            if (!changed)
                break;
        }

        //IngredientClosure is an internal, not-admin-editable derived table with no ILocalizedEntity/cache
        //consumer of its own (per design, the closure is never cached - it's recomputed transactionally at
        //write time), so its bulk delete/reinsert deliberately stays silent rather than raising an
        //EntityInserted/DeletedEvent<IngredientClosure> for every row on every composition write
        var existingClosureRows = await _ingredientClosureRepository.Table.ToListAsync();
        if (existingClosureRows.Any())
            await _ingredientClosureRepository.DeleteAsync(existingClosureRows, publishEvent: false);

        var newClosureRows = closure
            .Select(pair => new IngredientClosure
            {
                AncestorIngredientId = pair.Key.Ancestor,
                DescendantIngredientId = pair.Key.Descendant,
                Depth = pair.Value
            })
            .ToList();

        if (newClosureRows.Any())
            await _ingredientClosureRepository.InsertAsync(newClosureRows, publishEvent: false);
    }

    #endregion
}
