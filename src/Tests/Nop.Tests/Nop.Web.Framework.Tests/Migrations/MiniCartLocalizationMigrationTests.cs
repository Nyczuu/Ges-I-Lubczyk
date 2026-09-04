using AwesomeAssertions;
using Nop.Core.Domain.Localization;
using Nop.Web.Framework.Migrations.UpgradeTo500;
using NUnit.Framework;

namespace Nop.Tests.Nop.Web.Framework.Tests.Migrations;

[TestFixture]
public class MiniCartLocalizationMigrationTests
{
    [Test]
    public void LanguageWithMatchingUniqueSeoCodeIsIncludedWithEveryConfiguredKey()
    {
        var languages = new List<Language>
        {
            new() { Id = 1, UniqueSeoCode = "pl" }
        };
        var valuesByTwoLetterCode = new Dictionary<string, IDictionary<string, string>>
        {
            ["pl"] = new Dictionary<string, string>
            {
                ["ShoppingCart.Mini.FreeShipping.AmountToGo"] = "Polish amount-to-go copy",
                ["ShoppingCart.Mini.FreeShipping.Reached"] = "Polish reached copy"
            }
        };

        var result = MiniCartLocalizationMigration.ResolveResourcesToSeed(languages, valuesByTwoLetterCode);

        result.Should().HaveCount(2);
        result.Should().Contain((1, "ShoppingCart.Mini.FreeShipping.AmountToGo", "Polish amount-to-go copy"));
        result.Should().Contain((1, "ShoppingCart.Mini.FreeShipping.Reached", "Polish reached copy"));
    }

    [Test]
    public void LanguageWithNoMatchingUniqueSeoCodeIsExcludedRatherThanDefaulted()
    {
        var languages = new List<Language>
        {
            new() { Id = 1, UniqueSeoCode = "pl" },
            new() { Id = 2, UniqueSeoCode = "de" }
        };
        var valuesByTwoLetterCode = new Dictionary<string, IDictionary<string, string>>
        {
            ["pl"] = new Dictionary<string, string>
            {
                ["ShoppingCart.Mini.FreeShipping.Reached"] = "Polish reached copy"
            }
        };

        var result = MiniCartLocalizationMigration.ResolveResourcesToSeed(languages, valuesByTwoLetterCode);

        result.Should().ContainSingle();
        result.Should().NotContain(entry => entry.languageId == 2);
    }

    [Test]
    public void MultipleLanguagesEachResolveTheirOwnCopy()
    {
        var languages = new List<Language>
        {
            new() { Id = 1, UniqueSeoCode = "pl" },
            new() { Id = 2, UniqueSeoCode = "en" }
        };
        var valuesByTwoLetterCode = new Dictionary<string, IDictionary<string, string>>
        {
            ["pl"] = new Dictionary<string, string>
            {
                ["ShoppingCart.Mini.FreeShipping.Reached"] = "Polish reached copy"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["ShoppingCart.Mini.FreeShipping.Reached"] = "English reached copy"
            }
        };

        var result = MiniCartLocalizationMigration.ResolveResourcesToSeed(languages, valuesByTwoLetterCode);

        result.Should().HaveCount(2);
        result.Should().Contain((1, "ShoppingCart.Mini.FreeShipping.Reached", "Polish reached copy"));
        result.Should().Contain((2, "ShoppingCart.Mini.FreeShipping.Reached", "English reached copy"));
    }

    [Test]
    public void EmptyLanguageListReturnsEmptyResult()
    {
        var languages = new List<Language>();
        var valuesByTwoLetterCode = new Dictionary<string, IDictionary<string, string>>
        {
            ["pl"] = new Dictionary<string, string>
            {
                ["ShoppingCart.Mini.FreeShipping.Reached"] = "Polish reached copy"
            }
        };

        var result = MiniCartLocalizationMigration.ResolveResourcesToSeed(languages, valuesByTwoLetterCode);

        result.Should().BeEmpty();
    }
}
