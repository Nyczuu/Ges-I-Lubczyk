using FluentMigrator;
using Nop.Core.Caching;
using Nop.Core.Domain.Localization;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Services.Helpers;
using Nop.Services.Logging;

namespace Nop.Web.Framework.Migrations.UpgradeTo500;

/// <summary>
/// Seeds the Gęś i Lubczyk theme's announcement-bar locale resource (GIL-003-01).
/// A theme has no InstallAsync-style lifecycle to seed a brand-new resource key, so this additive,
/// theme-scoped migration is the sanctioned exception to this Epic otherwise being theme/plugin-only
/// (see GIL-003 Epic spec §4). Seeds every language it has copy for (Polish and English) rather than a
/// single hardcoded language, since English is also a live, Published, customer-selectable language on
/// this store. Uses ISyncCodeHelper throughout, matching every sibling migration in this folder, instead
/// of blocking on async ILanguageService/ILocalizationService calls via .Result/.Wait() (this codebase's
/// documented anti-pattern) - see Docs/knowledge-base/12-coding-standards.md.
/// </summary>
[NopUpdateMigration("2026-09-03 09:01:00", "5.00", UpdateMigrationType.Localization)]
public class GilThemeAnnouncementBarLocalizationMigration : MigrationBase
{
    private const string ResourceName = "Header.AnnouncementBar.Text";

    private static readonly IDictionary<string, string> ValuesByTwoLetterCode = new Dictionary<string, string>
    {
        ["pl"] = "Rzemieślnicza wekownia Gęś i Lubczyk – zamówienia realizujemy co tydzień, prosto ze spiżarni Joanny Nycz.",
        ["en"] = "Gęś i Lubczyk artisan pantry – orders are fulfilled weekly, straight from Joanna Nycz's kitchen.",
    };

    /// <summary>Collect the UP migration expressions</summary>
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        var syncCodeHelper = EngineContext.Current.Resolve<ISyncCodeHelper>();
        var staticCacheManager = EngineContext.Current.Resolve<IStaticCacheManager>();

        var languages = syncCodeHelper.GetAllLanguages(true);
        var resourcesToSeed = ResolveResourcesToSeed(languages, ValuesByTwoLetterCode);

        if (!resourcesToSeed.Any())
        {
            EngineContext.Current.Resolve<ILogger>().WarningAsync(
                $"{nameof(GilThemeAnnouncementBarLocalizationMigration)}: no matching language found for " +
                $"{ResourceName}, skipping resource seed.").Wait();
            return;
        }

        var existingByLanguageId = syncCodeHelper.GetAllEntities<LocaleStringResource>(query =>
                query.Where(r => r.ResourceName.ToLower() == ResourceName.ToLowerInvariant()
                    && resourcesToSeed.Keys.Contains(r.LanguageId)))
            .ToDictionary(r => r.LanguageId);

        var toInsert = new List<LocaleStringResource>();
        var toUpdate = new List<LocaleStringResource>();

        foreach (var (languageId, value) in resourcesToSeed)
        {
            if (existingByLanguageId.TryGetValue(languageId, out var existing))
            {
                if (existing.ResourceValue == value)
                    continue;

                existing.ResourceValue = value;
                toUpdate.Add(existing);
            }
            else
            {
                toInsert.Add(new LocaleStringResource
                {
                    LanguageId = languageId,
                    ResourceName = ResourceName.ToLowerInvariant(),
                    ResourceValue = value
                });
            }
        }

        if (toInsert.Any())
            syncCodeHelper.InsertEntities(toInsert);

        if (toUpdate.Any())
            syncCodeHelper.UpdateEntities(toUpdate);

        staticCacheManager.RemoveByPrefixAsync(NopEntityCacheDefaults<LocaleStringResource>.Prefix).Wait();
    }

    /// <summary>
    /// Pure, no EngineContext/DB access - unit-testable directly. Maps each language whose two-letter
    /// code has a configured value to that value; languages with no configured copy are skipped, not
    /// defaulted to another language's text.
    /// </summary>
    internal static IDictionary<int, string> ResolveResourcesToSeed(
        IList<Language> languages, IDictionary<string, string> valuesByTwoLetterCode)
    {
        return languages
            .Where(language => valuesByTwoLetterCode.ContainsKey(language.UniqueSeoCode))
            .ToDictionary(language => language.Id, language => valuesByTwoLetterCode[language.UniqueSeoCode]);
    }

    /// <summary>Collects the DOWN migration expressions</summary>
    public override void Down()
    {
        //do nothing in a fresh installation
    }
}
