#!/usr/bin/env node
// PreToolUse hook (Write|Edit matcher) — blocks stack patterns that are wrong for this
// codebase, and warns on core modifications.
//
// Why this exists: every rule below is already written in
// Docs/ai-harness/00-system-instructions.md. A rule in a document only works if the
// document is in context, and the specific failure mode here is a model writing correct,
// idiomatic .NET that happens to be wrong for nopCommerce — EF Core instead of Linq2DB,
// a bare services.AddScoped instead of INopStartup, DataAnnotations instead of
// FluentValidation. Those compile. Several of them pass tests. This hook turns the
// document's rules into an actual denial at the moment of writing.
//
// Deny vs warn: a deny is for something that is unambiguously wrong here. Writing into
// Nop.Core/Nop.Data/Nop.Services/Nop.Web is *allowed* but requires human confirmation
// (rule 3), so it warns rather than blocking — the hook cannot tell an approved additive
// nullable property from an unapproved refactor.

const DENY_RULES = [
  {
    pattern: /Microsoft\.EntityFrameworkCore|\bDbContext\b|\bDbSet<|OnModelCreating|AddDbContext/,
    reason:
      "Entity Framework Core does not exist in this codebase. Data access is Linq2DB + " +
      "FluentMigrator — inject IRepository<T>. See Docs/knowledge-base/03-data-access-linq2db-fluentmigrator.md " +
      "and rule 1 of Docs/ai-harness/00-system-instructions.md.",
  },
  {
    pattern: /Create\.TableFor</,
    reason:
      "Create.TableFor<T>() does not exist in this codebase — it appears in older nopCommerce " +
      "documentation. Use this.CreateTableIfNotExists<T>() from Nop.Data.Extensions.FluentMigratorExtensions.",
  },
  {
    pattern: /\bFluentAssertions\b/,
    reason:
      "FluentAssertions is a paid commercial licence from v8. This repo uses AwesomeAssertions " +
      "(identical .Should() API) — 'using AwesomeAssertions;'. See Docs/knowledge-base/13-testing.md.",
  },
  {
    pattern: /\[Fact\]|\[Theory\]|\bXunit\b|\bTUnit\b|\bNSubstitute\b|Substitute\.For</,
    reason:
      "The test stack here is NUnit + Moq ([Test], [TestFixture], Mock<T>). " +
      "See Docs/knowledge-base/13-testing.md.",
  },
  {
    pattern: /\bWITH\s*\(NOLOCK\)|\bGETDATE\(\)|\bISNULL\(|\bGROUP_CONCAT\(|\bsp_[a-z]/i,
    reason:
      "Provider-specific SQL (SQL Server / MySQL) in a PostgreSQL project. Use Linq2DB and " +
      "FluentMigrator abstractions. See Docs/ai-harness/03-database-postgres.md.",
  },
];

// Registration outside INopStartup — only meaningful in the composition root files.
const STARTUP_FILE = /(Program|Startup)\.cs$/;
const BARE_REGISTRATION = /services\.Add(Scoped|Singleton|Transient)</;

// DataAnnotations on a view model — checked only under Models/ to avoid false positives
// on domain or configuration types.
const MODEL_FILE = /[\\/]Models[\\/].*\.cs$/;
const DATA_ANNOTATION = /\[(Required|StringLength|MaxLength|MinLength|RegularExpression|Range)\s*[\](]/;

const WARN_RULES = [
  {
    pathPattern: /src[\\/]Libraries[\\/]Nop\.Data[\\/]Migrations[\\/]UpgradeTo/i,
    reason:
      "This is nopCommerce's own version-upgrade migration folder, not a place for project schema " +
      "changes. Listed as a red flag in Docs/ai-harness/02-extensibility-and-plugins.md. Write a " +
      "plugin migration instead.",
  },
  {
    pathPattern: /src[\\/](Libraries[\\/]Nop\.(Core|Data|Services)|Presentation[\\/]Nop\.Web(\.Framework)?)[\\/]/i,
    reason:
      "This is a core modification. Rule 3 of Docs/ai-harness/00-system-instructions.md: default " +
      "location for new functionality is a plugin, and a core change needs explicit human " +
      "confirmation first. The one sanctioned exception is an additive, nullable property on a " +
      "domain class as part of the documented entity-extension pattern. If this change is that " +
      "exception, or has already been confirmed, proceed.",
  },
];

let raw = "";
process.stdin.on("data", (chunk) => {
  raw += chunk;
});

process.stdin.on("end", () => {
  let filePath = "";
  let content = "";
  try {
    const input = JSON.parse(raw);
    const toolInput = input.tool_input || {};
    filePath = toolInput.file_path || "";
    content = [toolInput.content, toolInput.new_string].filter(Boolean).join("\n");
  } catch {
    process.exit(0);
  }

  const deny = (reason) => {
    console.log(
      JSON.stringify({
        hookSpecificOutput: {
          hookEventName: "PreToolUse",
          permissionDecision: "deny",
          permissionDecisionReason: reason,
        },
      }),
    );
  };

  if (/\.cs$|\.csproj$|\.cshtml$/.test(filePath)) {
    for (const rule of DENY_RULES) {
      if (rule.pattern.test(content)) {
        deny(rule.reason);
        return;
      }
    }
    if (STARTUP_FILE.test(filePath) && BARE_REGISTRATION.test(content)) {
      deny(
        "Service registration in Program.cs/Startup.cs is not how this codebase wires DI. Every " +
          "registration goes through an INopStartup implementation, auto-discovered by ITypeFinder. " +
          "See rule 2 of Docs/ai-harness/00-system-instructions.md and " +
          "Docs/knowledge-base/02-dependency-injection-and-typefinder.md.",
      );
      return;
    }
    if (MODEL_FILE.test(filePath) && DATA_ANNOTATION.test(content)) {
      deny(
        "DataAnnotations validation attributes are not used on nopCommerce view models. Validation " +
          "is FluentValidation via BaseNopValidator<TModel>. See " +
          "Docs/knowledge-base/08-settings-permissions-validation.md.",
      );
      return;
    }
  }

  const warnings = WARN_RULES.filter((rule) => rule.pathPattern.test(filePath)).map((rule) => rule.reason);
  if (warnings.length > 0) {
    console.log(
      JSON.stringify({
        hookSpecificOutput: {
          hookEventName: "PreToolUse",
          additionalContext: warnings.join("\n\n"),
        },
      }),
    );
    return;
  }

  process.exit(0);
});
