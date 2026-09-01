using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.ServingSuggestions.Domain;

namespace Nop.Plugin.Misc.ServingSuggestions.Data.Migrations;

[NopMigration("2026-08-31 00:00:00", "Misc.ServingSuggestions schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        this.CreateTableIfNotExists<ServingSuggestion>();
        this.CreateTableIfNotExists<ServingSuggestionStep>();
    }

    /// <summary>
    /// Collects the DOWN migration expressions
    /// </summary>
    public override void Down()
    {
        this.DeleteTableIfExists<ServingSuggestionStep>();
        this.DeleteTableIfExists<ServingSuggestion>();
    }
}
