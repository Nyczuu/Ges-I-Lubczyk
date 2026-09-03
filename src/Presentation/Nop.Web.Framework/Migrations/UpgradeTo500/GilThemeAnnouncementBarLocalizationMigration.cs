using FluentMigrator;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Services.Localization;
using Nop.Services.Logging;

namespace Nop.Web.Framework.Migrations.UpgradeTo500;

/// <summary>
/// Seeds the Gęś i Lubczyk theme's announcement-bar locale resource (GIL-003-01).
/// A theme has no InstallAsync-style lifecycle to seed a brand-new resource key, so this additive,
/// theme-scoped migration is the sanctioned exception to this Epic otherwise being theme/plugin-only
/// (see GIL-003 Epic spec §4). Targets the Polish language row explicitly - this store's database also
/// has an English Language row, and the standard AddOrUpdateLocaleResource FluentMigrator helper
/// resolves its target language via the hardcoded "en-US" culture, which would silently seed this
/// Polish copy into the wrong row.
/// </summary>
[NopUpdateMigration("2026-09-03 09:01:00", "5.00", UpdateMigrationType.Localization)]
public class GilThemeAnnouncementBarLocalizationMigration : MigrationBase
{
    /// <summary>Collect the UP migration expressions</summary>
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        var languageService = EngineContext.Current.Resolve<ILanguageService>();
        var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

        var polishLanguageId = languageService.GetAllLanguagesAsync(true).Result
            .FirstOrDefault(language => language.UniqueSeoCode == "pl")?.Id;

        if (polishLanguageId is null)
        {
            //no 'pl' language row on this store - skip rather than fall back to seeding every language,
            //which would reintroduce the wrong-language failure mode this migration exists to prevent
            EngineContext.Current.Resolve<ILogger>().WarningAsync(
                "GilThemeAnnouncementBarLocalizationMigration: no 'pl' language found, skipping resource seed.").Wait();
            return;
        }

        localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Header.AnnouncementBar.Text"] = "Rzemieślnicza wekownia Gęś i Lubczyk – zamówienia realizujemy co tydzień, prosto ze spiżarni Joanny Nycz.",
        }, polishLanguageId).Wait();
    }

    /// <summary>Collects the DOWN migration expressions</summary>
    public override void Down()
    {
        //do nothing in a fresh installation
    }
}
