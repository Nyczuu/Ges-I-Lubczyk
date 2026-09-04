using AwesomeAssertions;
using Nop.Core.Domain.Localization;
using Nop.Web.Framework.Migrations.UpgradeTo500;
using NUnit.Framework;

namespace Nop.Tests.Nop.Web.Framework.Tests.Migrations;

[TestFixture]
public class GilThemeAnnouncementBarLocalizationMigrationTests
{
    [Test]
    public void LanguageWithMatchingUniqueSeoCodeIsIncludedWithConfiguredValue()
    {
        var languages = new List<Language>
        {
            new() { Id = 1, UniqueSeoCode = "pl" }
        };
        var valuesByTwoLetterCode = new Dictionary<string, string>
        {
            ["pl"] = "Polish copy"
        };

        var result = GilThemeAnnouncementBarLocalizationMigration.ResolveResourcesToSeed(languages, valuesByTwoLetterCode);

        result.Should().ContainSingle().Which.Should().Be(new KeyValuePair<int, string>(1, "Polish copy"));
    }

    [Test]
    public void LanguageWithNoMatchingUniqueSeoCodeIsExcludedRatherThanDefaulted()
    {
        var languages = new List<Language>
        {
            new() { Id = 1, UniqueSeoCode = "pl" },
            new() { Id = 2, UniqueSeoCode = "de" }
        };
        var valuesByTwoLetterCode = new Dictionary<string, string>
        {
            ["pl"] = "Polish copy"
        };

        var result = GilThemeAnnouncementBarLocalizationMigration.ResolveResourcesToSeed(languages, valuesByTwoLetterCode);

        result.Should().HaveCount(1);
        result.Should().NotContainKey(2);
        result[1].Should().Be("Polish copy");
    }

    [Test]
    public void EmptyLanguageListReturnsEmptyResult()
    {
        var languages = new List<Language>();
        var valuesByTwoLetterCode = new Dictionary<string, string>
        {
            ["pl"] = "Polish copy"
        };

        var result = GilThemeAnnouncementBarLocalizationMigration.ResolveResourcesToSeed(languages, valuesByTwoLetterCode);

        result.Should().BeEmpty();
    }
}
