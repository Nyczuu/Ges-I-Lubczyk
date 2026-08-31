using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.Ingredients.Domain;

namespace Nop.Plugin.Misc.Ingredients.Data.Migrations;

[NopMigration("2026-08-30 00:00:00", "Nop.Plugin.Misc.Ingredients schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        this.CreateTableIfNotExists<Ingredient>();
        this.CreateTableIfNotExists<IngredientComposition>();
        this.CreateTableIfNotExists<IngredientClosure>();
        this.CreateTableIfNotExists<ProductIngredientMapping>();
    }

    /// <summary>
    /// Collects the DOWN migration expressions
    /// </summary>
    public override void Down()
    {
        this.DeleteTableIfExists<ProductIngredientMapping>();
        this.DeleteTableIfExists<IngredientClosure>();
        this.DeleteTableIfExists<IngredientComposition>();
        this.DeleteTableIfExists<Ingredient>();
    }
}
