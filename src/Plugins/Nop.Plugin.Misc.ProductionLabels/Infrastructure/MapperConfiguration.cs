using Nop.Core.Infrastructure.Mapper;
using Nop.Plugin.Misc.ProductionLabels.Admin.Models;
using Nop.Plugin.Misc.ProductionLabels.Domain;

namespace Nop.Plugin.Misc.ProductionLabels.Infrastructure;

/// <summary>
/// Represents mapping configuration for plugin models
/// </summary>
public class MapperConfiguration : BaseMapperProfile
{
    #region Ctor

    public MapperConfiguration()
    {
        CreateMap<ProductionBatch, ProductionBatchModel>()
            .ForMember(model => model.ProductName, options => options.Ignore())
            .ForMember(model => model.AvailableProducts, options => options.Ignore());
        CreateMap<ProductionBatchModel, ProductionBatch>()
            .ForMember(entity => entity.BatchCode, options => options.Ignore())
            .ForMember(entity => entity.CreatedOnUtc, options => options.Ignore())
            .ForMember(entity => entity.LabelGeneratedOnUtc, options => options.Ignore());
    }

    #endregion
}
