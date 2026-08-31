using System.Data;
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Catalog;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.Ingredients.Domain;

namespace Nop.Plugin.Misc.Ingredients.Data.Mapping.Builders;

/// <summary>
/// Represents a product ingredient mapping entity builder
/// </summary>
public class ProductIngredientMappingBuilder : NopEntityBuilder<ProductIngredientMapping>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ProductIngredientMapping.ProductId)).AsInt32().ForeignKey<Product>()
            .WithColumn(nameof(ProductIngredientMapping.IngredientId)).AsInt32().ForeignKey<Ingredient>(onDelete: Rule.None);
    }
}
