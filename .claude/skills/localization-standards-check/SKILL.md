---
name: localization-standards-check
description: >-
  Load this whenever a change introduces or edits text a user can see — an admin field label, a
  validation message, a button, a storefront string, an email or notification body. Use it BEFORE
  writing the code: a hardcoded string compiles, renders, and passes every test, so nothing catches it
  until a second language exists or a store owner asks to reword a label they cannot reach.
---

# Localization Standards Check

Full docs: the Localization section of [`Docs/ai-harness/01-architecture-and-standards.md`](../../../Docs/ai-harness/01-architecture-and-standards.md)
and [`Docs/knowledge-base/08-settings-permissions-validation.md`](../../../Docs/knowledge-base/08-settings-permissions-validation.md).
This is the checklist form.

## The rule

**No user-facing string literal in C# or Razor.** Every one is a locale resource.

| Where | How |
|---|---|
| View model property label | `[NopResourceDisplayName("Plugins.{Group}.{Name}.Fields.X")]` |
| Validation message | `.WithMessageAwait(localizationService.GetResourceAsync("..."))` |
| Anywhere in code | `await _localizationService.GetResourceAsync("key")` |
| Razor | the localization tag helper / `T("key")`, matching surrounding views |

Not covered by this rule: log messages, exception messages meant for developers, and internal constants.
Those stay in English and stay in code.

## Key naming

- [ ] Plugin keys are prefixed `Plugins.{Group}.{Name}.` — this prefix is what makes bulk removal on
      uninstall possible.
- [ ] Field labels: `Plugins.{Group}.{Name}.Fields.{Property}`.
- [ ] **Every admin field label has a matching `.Hint` key** — nopCommerce renders it as the field's
      tooltip, and a missing hint shows as an empty tooltip rather than an error.
- [ ] Reuse an existing core key (`Admin.Common.Save`, `Admin.Catalog.Products.Fields.*`) rather than
      adding a near-duplicate under your own prefix.

## Install / uninstall

- [ ] `InstallAsync` calls `AddOrUpdateLocaleResourceAsync` with the full dictionary of new keys.
- [ ] `UninstallAsync` calls `DeleteLocaleResourcesAsync` for the same keys or the shared prefix.
      Resources left behind after uninstall are invisible orphans in every store that ever installed
      the plugin — see `plugin-standards-check` for the full symmetry table.
- [ ] Keys added in a later version are added in that version's install/update path too, not only to a
      fresh install — an existing store never re-runs `InstallAsync`.

## Multi-store, multi-language

- [ ] Nothing assumes a single language or store. If the value genuinely differs per store, that is a
      setting, not a resource.
- [ ] No string concatenation to build a sentence from fragments — word order differs between languages.
      One resource per complete message, with placeholders.

## Before calling this done

- [ ] Zero user-facing literals in the diff (controllers, models, validators, views).
- [ ] Every new key has a `.Hint` sibling where it labels an admin field.
- [ ] Install adds every key; uninstall removes every key.
- [ ] New keys in an update ship through the update path, not only through a fresh install.
