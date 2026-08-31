using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.Ingredients.Domain;

namespace Nop.Plugin.Misc.Ingredients.Data.Mapping.Builders;

/// <summary>
/// Represents an ingredient composition entity builder
/// </summary>
/// <remarks>
/// No FK constraint: both <see cref="IngredientComposition.ParentIngredientId"/> and
/// <see cref="IngredientComposition.ChildIngredientId"/> point at the same target table
/// (<see cref="Ingredient"/>), and <c>NopEntityBuilder</c>'s <c>ForeignKey&lt;TPrimary&gt;</c>
/// extension exposes no constraint-name parameter, so declaring it twice would collide on
/// FluentMigrator's auto-generated constraint name (same precedent as <c>RelatedProduct</c>).
/// </remarks>
public class IngredientCompositionBuilder : NopEntityBuilder<IngredientComposition>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
    }
}
