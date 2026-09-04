using FluentMigrator;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Web.Framework.Extensions;

namespace Nop.Plugin.Misc.Ingredients.Data.Migrations;

[NopMigration("2026-09-04 00:00:00", "Nop.Plugin.Misc.Ingredients grid columns", MigrationProcessType.Update)]
public class GridColumnsMigration : MigrationBase
{
    #region Methods

    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        this.AddOrUpdateLocaleResource(new Dictionary<string, string>
        {
            ["Plugins.Misc.Ingredients.Fields.IsComposition"] = "Composition",
            ["Plugins.Misc.Ingredients.Fields.IsComposition.Hint"] = "Whether this ingredient is itself a composition of other ingredients."
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
