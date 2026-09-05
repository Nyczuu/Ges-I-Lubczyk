using FluentMigrator;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Web.Framework.Extensions;

namespace Nop.Plugin.Misc.ProductionLabels.Data.Migrations;

/// <summary>
/// Adds the locale resources for the new default shelf-life days field (spec §5/§7). Mirrors
/// Nop.Plugin.Misc.Ingredients's own NutritionalValuesMigration.cs precedent: an existing installation of
/// this already-shipped plugin only picks up this migration's Up() via a plugin.json version bump, which
/// PluginService.UpdatePluginsAsync() detects on next app start (MigrationProcessType.Update). A brand-new
/// install never runs this Up() at all - it is stamped as already-applied - so the same three keys are
/// duplicated in ProductionLabelsPlugin.InstallAsync() for that path.
/// </summary>
[NopMigration("2026-09-04 12:00:00", "Misc.ProductionLabels default shelf-life days", MigrationProcessType.Update)]
public class DefaultShelfLifeDaysMigration : MigrationBase
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
            ["Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays"] = "Default shelf-life (days)",
            ["Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays.Hint"] = "The number of days from production to best-before, used to prefill new batches.",
            ["Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays.Required"] = "Default shelf-life (days) is required.",
            ["Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays.GreaterThanZero"] = "Default shelf-life (days) must be greater than zero."
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
