using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Services.Caching;

namespace Nop.Plugin.Misc.ServingSuggestions.Services.Caching;

/// <summary>
/// Represents a serving suggestion step cache event consumer
/// </summary>
public class ServingSuggestionStepCacheEventConsumer : CacheEventConsumer<ServingSuggestionStep>;
