namespace Nop.Plugin.Misc.Ingredients.Domain;

/// <summary>
/// Represents the 14 EU Regulation 1169/2011 Annex II allergens, plus a "none" value
/// </summary>
public enum AllergenType
{
    None = 0,
    CerealsContainingGluten = 1,
    Crustaceans = 2,
    Eggs = 3,
    Fish = 4,
    Peanuts = 5,
    Soybeans = 6,
    Milk = 7,
    Nuts = 8,
    Celery = 9,
    Mustard = 10,
    SesameSeeds = 11,
    SulphurDioxideAndSulphites = 12,
    Lupin = 13,
    Molluscs = 14
}
