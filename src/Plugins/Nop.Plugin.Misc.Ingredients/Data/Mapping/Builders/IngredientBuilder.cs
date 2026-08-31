using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.Ingredients.Domain;

namespace Nop.Plugin.Misc.Ingredients.Data.Mapping.Builders;

/// <summary>
/// Represents an ingredient entity builder
/// </summary>
public class IngredientBuilder : NopEntityBuilder<Ingredient>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table.WithColumn(nameof(Ingredient.Name)).AsString(400).NotNullable();
    }
}
