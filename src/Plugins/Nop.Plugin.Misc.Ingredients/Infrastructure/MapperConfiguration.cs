using Nop.Core.Infrastructure.Mapper;
using Nop.Plugin.Misc.Ingredients.Admin.Models;
using Nop.Plugin.Misc.Ingredients.Domain;

namespace Nop.Plugin.Misc.Ingredients.Infrastructure;

/// <summary>
/// Represents mapping configuration for plugin models
/// </summary>
public class MapperConfiguration : BaseMapperProfile
{
    #region Ctor

    public MapperConfiguration()
    {
        CreateMap<Ingredient, IngredientModel>()
            .ForMember(model => model.AvailableAllergenTypes, options => options.Ignore())
            .ForMember(model => model.Locales, options => options.Ignore())
            .ForMember(model => model.IngredientCompositionSearchModel, options => options.Ignore());
        CreateMap<IngredientModel, Ingredient>()
            .ForMember(entity => entity.CreatedOnUtc, options => options.Ignore())
            .ForMember(entity => entity.UpdatedOnUtc, options => options.Ignore());

        CreateMap<IngredientComposition, IngredientCompositionModel>()
            .ForMember(model => model.ChildIngredientName, options => options.Ignore());
        CreateMap<IngredientCompositionModel, IngredientComposition>();

        CreateMap<ProductIngredientMapping, ProductIngredientModel>()
            .ForMember(model => model.IngredientName, options => options.Ignore());
        CreateMap<ProductIngredientModel, ProductIngredientMapping>();
    }

    #endregion
}
