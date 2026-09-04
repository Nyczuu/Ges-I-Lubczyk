namespace Nop.Plugin.Misc.ProductionLabels.Domain;

/// <summary>
/// Represents a preset label size layout. Content and layout are identical between variants; only the
/// geometry differs, driven by CSS in the label template. A per-request rendering choice, never persisted
/// (unlike e.g. <see cref="Nop.Plugin.Misc.Ingredients.Domain.AllergenType"/>).
/// </summary>
public enum ProductionLabelSizeVariant
{
    SmallJar = 0,
    LargeJar = 1
}
