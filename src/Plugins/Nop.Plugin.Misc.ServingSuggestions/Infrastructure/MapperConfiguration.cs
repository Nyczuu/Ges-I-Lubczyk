using Nop.Core.Infrastructure.Mapper;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Models;
using Nop.Plugin.Misc.ServingSuggestions.Domain;

namespace Nop.Plugin.Misc.ServingSuggestions.Infrastructure;

/// <summary>
/// Represents mapping configuration for plugin models
/// </summary>
public class MapperConfiguration : BaseMapperProfile
{
    #region Ctor

    public MapperConfiguration()
    {
        CreateMap<ServingSuggestion, ServingSuggestionModel>()
            .ForMember(model => model.HasServingSuggestion, options => options.Ignore())
            .ForMember(model => model.PictureUrl, options => options.Ignore())
            .ForMember(model => model.Locales, options => options.Ignore())
            .ForMember(model => model.ServingSuggestionStepSearchModel, options => options.Ignore());
        CreateMap<ServingSuggestionModel, ServingSuggestion>();

        CreateMap<ServingSuggestionStep, ServingSuggestionStepModel>()
            .ForMember(model => model.Locales, options => options.Ignore());
        CreateMap<ServingSuggestionStepModel, ServingSuggestionStep>();
    }

    #endregion
}
