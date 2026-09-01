using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Services.Caching;

namespace Nop.Plugin.Misc.ServingSuggestions.Services.Caching;

/// <summary>
/// Represents a serving suggestion cache event consumer
/// </summary>
public class ServingSuggestionCacheEventConsumer : CacheEventConsumer<ServingSuggestion>;
