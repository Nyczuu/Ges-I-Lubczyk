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
/// Seeds the mini-cart free-shipping bar's locale resources (GIL-003-05). The bar's copy is brand-new,
/// theme-owned text with no existing resource key and no plugin InstallAsync to seed it - the same
/// mechanism GIL-003-01 uses for its announcement-bar text (see
/// GilThemeAnnouncementBarLocalizationMigration). Seeds every language it has copy for (Polish and
/// English) rather than a single hardcoded language, since English is also a live, Published,
/// customer-selectable language on this store. Uses ISyncCodeHelper throughout, matching every sibling
/// migration in this folder, instead of blocking on async ILanguageService/ILocalizationService calls via
/// .Result/.Wait() (this codebase's documented anti-pattern) - see Docs/knowledge-base/12-coding-standards.md.
/// </summary>
[NopUpdateMigration("2026-09-03 14:00:00", "5.00", UpdateMigrationType.Localization)]
public class MiniCartLocalizationMigration : MigrationBase
{
    private static readonly IDictionary<string, IDictionary<string, string>> ValuesByTwoLetterCode =
        new Dictionary<string, IDictionary<string, string>>
        {
            ["pl"] = new Dictionary<string, string>
            {
                ["ShoppingCart.Mini.FreeShipping.AmountToGo"] = "Do darmowej dostawy brakuje: {0}",
                ["ShoppingCart.Mini.FreeShipping.Reached"] = "Przysługuje Ci darmowa dostawa!"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["ShoppingCart.Mini.FreeShipping.AmountToGo"] = "Spend {0} more for free shipping",
                ["ShoppingCart.Mini.FreeShipping.Reached"] = "You've earned free shipping!"
            }
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
                $"{nameof(MiniCartLocalizationMigration)}: no matching language found, skipping resource seed.").Wait();
            return;
        }

        var languageIds = resourcesToSeed.Select(r => r.languageId).Distinct().ToList();
        var resourceNames = resourcesToSeed.Select(r => r.resourceName.ToLowerInvariant()).Distinct().ToList();

        var existingByLanguageAndName = syncCodeHelper.GetAllEntities<LocaleStringResource>(query =>
                query.Where(r => languageIds.Contains(r.LanguageId)
                    && resourceNames.Contains(r.ResourceName.ToLower())))
            .ToDictionary(r => (r.LanguageId, r.ResourceName.ToLowerInvariant()));

        var toInsert = new List<LocaleStringResource>();
        var toUpdate = new List<LocaleStringResource>();

        foreach (var (languageId, resourceName, value) in resourcesToSeed)
        {
            var key = (languageId, resourceName.ToLowerInvariant());

            if (existingByLanguageAndName.TryGetValue(key, out var existing))
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
                    ResourceName = resourceName.ToLowerInvariant(),
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
    /// Pure, no EngineContext/DB access - unit-testable directly. For each language whose two-letter code
    /// has configured copy, resolves every configured (resourceName, value) pair for that language;
    /// languages with no configured copy are skipped, not defaulted to another language's text.
    /// </summary>
    internal static IList<(int languageId, string resourceName, string value)> ResolveResourcesToSeed(
        IList<Language> languages, IDictionary<string, IDictionary<string, string>> valuesByTwoLetterCode)
    {
        return languages
            .Where(language => valuesByTwoLetterCode.ContainsKey(language.UniqueSeoCode))
            .SelectMany(language => valuesByTwoLetterCode[language.UniqueSeoCode]
                .Select(pair => (language.Id, pair.Key, pair.Value)))
            .ToList();
    }

    /// <summary>Collects the DOWN migration expressions</summary>
    public override void Down()
    {
        //do nothing in a fresh installation
    }
}
