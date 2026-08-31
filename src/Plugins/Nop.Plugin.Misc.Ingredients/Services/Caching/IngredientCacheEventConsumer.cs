using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Services.Caching;

namespace Nop.Plugin.Misc.Ingredients.Services.Caching;

/// <summary>
/// Represents an ingredient cache event consumer
/// </summary>
public class IngredientCacheEventConsumer : CacheEventConsumer<Ingredient>;
