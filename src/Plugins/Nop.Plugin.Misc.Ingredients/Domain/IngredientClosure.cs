using Nop.Core;

namespace Nop.Plugin.Misc.Ingredients.Domain;

/// <summary>
/// Represents a materialized transitive closure row over <see cref="IngredientComposition"/> edges.
/// Maintained at write time; every ingredient has a reflexive row against itself with depth 0.
/// </summary>
public class IngredientClosure : BaseEntity
{
    /// <summary>
    /// Gets or sets the ancestor ingredient identifier
    /// </summary>
    public int AncestorIngredientId { get; set; }

    /// <summary>
    /// Gets or sets the descendant ingredient identifier
    /// </summary>
    public int DescendantIngredientId { get; set; }

    /// <summary>
    /// Gets or sets the longest known number of <see cref="IngredientComposition"/> edges between the pair
    /// </summary>
    public int Depth { get; set; }
}
