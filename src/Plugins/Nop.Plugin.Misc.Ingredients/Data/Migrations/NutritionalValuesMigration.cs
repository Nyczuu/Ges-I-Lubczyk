using FluentMigrator;
using Nop.Data;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Web.Framework.Extensions;

namespace Nop.Plugin.Misc.Ingredients.Data.Migrations;

[NopMigration("2026-09-03 00:00:00", "Nop.Plugin.Misc.Ingredients nutritional values", MigrationProcessType.Update)]
public class NutritionalValuesMigration : MigrationBase
{
    #region Methods

    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        this.AddOrAlterColumnFor<Ingredient>(x => x.CaloriesPer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        this.AddOrAlterColumnFor<Ingredient>(x => x.ProteinPer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        this.AddOrAlterColumnFor<Ingredient>(x => x.FatPer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        this.AddOrAlterColumnFor<Ingredient>(x => x.CarbohydratePer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);

        this.AddOrUpdateLocaleResource(new Dictionary<string, string>
        {
            ["Plugins.Misc.Ingredients.Fields.CaloriesPer100g"] = "Calories per 100g (kcal)",
            ["Plugins.Misc.Ingredients.Fields.CaloriesPer100g.Hint"] = "The energy value of this ingredient, in kilocalories per 100g.",
            ["Plugins.Misc.Ingredients.Fields.CaloriesPer100g.GreaterThanOrEqualZero"] = "Calories per 100g must be zero or greater.",
            ["Plugins.Misc.Ingredients.Fields.ProteinPer100g"] = "Protein per 100g (g)",
            ["Plugins.Misc.Ingredients.Fields.ProteinPer100g.Hint"] = "The protein content of this ingredient, in grams per 100g.",
            ["Plugins.Misc.Ingredients.Fields.ProteinPer100g.GreaterThanOrEqualZero"] = "Protein per 100g must be zero or greater.",
            ["Plugins.Misc.Ingredients.Fields.FatPer100g"] = "Fat per 100g (g)",
            ["Plugins.Misc.Ingredients.Fields.FatPer100g.Hint"] = "The fat content of this ingredient, in grams per 100g.",
            ["Plugins.Misc.Ingredients.Fields.FatPer100g.GreaterThanOrEqualZero"] = "Fat per 100g must be zero or greater.",
            ["Plugins.Misc.Ingredients.Fields.CarbohydratePer100g"] = "Carbohydrate per 100g (g)",
            ["Plugins.Misc.Ingredients.Fields.CarbohydratePer100g.Hint"] = "The carbohydrate content of this ingredient, in grams per 100g.",
            ["Plugins.Misc.Ingredients.Fields.CarbohydratePer100g.GreaterThanOrEqualZero"] = "Carbohydrate per 100g must be zero or greater."
        });
    }

    /// <summary>
    /// Collects the DOWN migration expressions
    /// </summary>
    public override void Down()
    {
        //nothing - forward-only
    }

    #endregion
}
