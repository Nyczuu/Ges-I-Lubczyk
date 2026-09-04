using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.ProductionLabels.Domain;

namespace Nop.Plugin.Misc.ProductionLabels.Data.Migrations;

[NopMigration("2026-09-04 00:00:00", "Misc.ProductionLabels schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        this.CreateTableIfNotExists<ProductionBatch>();
    }

    /// <summary>
    /// Collects the DOWN migration expressions
    /// </summary>
    public override void Down()
    {
        this.DeleteTableIfExists<ProductionBatch>();
    }
}
