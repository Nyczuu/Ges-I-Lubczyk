using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.Ingredients.Domain;

namespace Nop.Plugin.Misc.Ingredients.Data.Mapping.Builders;

/// <summary>
/// Represents an ingredient closure entity builder
/// </summary>
/// <remarks>
/// No FK constraint, same reasoning as <see cref="IngredientCompositionBuilder"/>: both
/// <see cref="IngredientClosure.AncestorIngredientId"/> and <see cref="IngredientClosure.DescendantIngredientId"/>
/// point at the same target table.
/// </remarks>
public class IngredientClosureBuilder : NopEntityBuilder<IngredientClosure>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
    }
}
