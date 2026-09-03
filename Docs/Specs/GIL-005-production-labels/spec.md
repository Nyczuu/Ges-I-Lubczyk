---
id: GIL-005
kind: Task
title: Production batches and printable PDF labels per product
status: Ready
---

# Task — Production batches and printable PDF labels per product

A single implementable unit of work (feature). Mirrors the Task checklist in `.claude/agents/spec-intake.md`.

**Renumbered from GIL-004:** that ID was claimed concurrently by another draft
(`Docs/Specs/GIL-004-ingredient-nutritional-values/spec.md`, unrelated) while this spec was being
written. Resolved by the developer: this ticket takes GIL-005, the other keeps GIL-004.

## 1. Business goal & outcome

Production staff need to (a) record a history of production runs per product — batch code, production
date, best-before date, quantity — for traceability/recall, and (b) generate the actual printable PDF
product label per product+batch to send to the print shop. **Resolved (round 2):** this is a real
label that ends up on the jar sold to a customer, not an internal tag — so its content is scoped by EU
Regulation 1169/2011, the same regulation GIL-001's allergen classification already targets (without
claiming full compliance — see `Docs/BusinessLogic/product-ingredients.md`). This spec does not claim
full legal compliance either; it defines what data feeds the label and where that data comes from,
field by field (§6).

Today neither capability exists: the admin has no concept of a production batch, and no mechanism
produces a printable label (core `IPdfService` only builds order invoices, packing slips, and catalog
exports from a fixed document model, not an HTML-templated label).

Success: an admin/production user can log a new batch against a product (system assigns the batch
code), see batch history both from that product's edit page and from a central "Production" list, and
download a PDF label for a chosen product+batch using one of two preset size layouts.

## 2. Root cause / current behavior *(bug fixes only)*

N/A — new feature, not a bug fix.

## 3. Placement — plugin or core?

New plugin: `Nop.Plugin.Misc.ProductionLabels`. Per the "extending an existing plugin vs. adding a new
plugin that depends on it" rule in `Docs/ai-harness/02-extensibility-and-plugins.md`: label/batch
tracking has an independent install/uninstall lifecycle from `Nop.Plugin.Misc.Ingredients` (disabling
label printing must not affect storefront ingredient display, and vice versa), so it is a new plugin,
not an extension of Ingredients. References `Nop.Plugin.Misc.Ingredients`'s public `IIngredientService`
/ `IProductIngredientService` via `ProjectReference` to read ingredient/allergen data for the label —
never Ingredients' repository or internals directly. No `Nop.Core`/`Nop.Data`/`Nop.Services`/
`Nop.Web(.Framework)` changes.

## 4. Extension point

`IMiscPlugin` (admin-owned capability — no payment/shipping/tax/auth role fits) **and** `IWidgetPlugin`.
Verified against `Docs/knowledge-base/06-plugin-types-reference.md` and against
`Nop.Plugin.Misc.Ingredients`'s own plugin class (`src/Plugins/Nop.Plugin.Misc.Ingredients/IngredientsPlugin.cs`):
attaching a tab to the admin product-edit page is itself delivered through the widget mechanism
(`AdminWidgetZones.ProductDetailsBlock`) — there is no non-widget way to inject into that page, so
`IWidgetPlugin` is required even though nothing renders on the storefront. Unlike
Ingredients/ServingSuggestions, this plugin registers **only** the admin zone — no `PublicWidgetZones`
entry.

## 5. Data model & migration

New entity `ProductionBatch`: `ProductId` (int, not null, references `Product`), `BatchCode` (string,
not null, system-generated — see below), `ProductionDateUtc` (DateTime, not null), `BestBeforeDateUtc`
(DateTime, not null), `Quantity` (int, not null), `LabelGeneratedOnUtc` (DateTime, **nullable**), plus
the standard `CreatedOnUtc` (not null). Own
`NopEntityBuilder<ProductionBatch>` + `[NopMigration("<install date> 00:00:00", "Misc.ProductionLabels schema", MigrationProcessType.Installation)]`
via `CreateTableIfNotExists<ProductionBatch>()`, mirroring `Nop.Plugin.Misc.ServingSuggestions`'s actual
migration shape (`src/Plugins/Nop.Plugin.Misc.ServingSuggestions/Data/Migrations/SchemaMigration.cs:8`).
**Round 4 correction:** an earlier draft of this spec named the attribute `[NopSchemaMigration]` —
wrong; that attribute is for migrations that must run before the DI container is available, not
ordinary plugin table creation (every plugin schema migration in this repo, Ingredients included, uses
`[NopMigration(...)]`). Fresh table on a fresh install; no data-migration concern since nothing
pre-existing maps to it.

**Validation (round 2 gap — Task checklist requires invalid-input handling):** `BestBeforeDateUtc` must
be after `ProductionDateUtc`; `Quantity` must be greater than zero. Both rejected at the service layer
with a validation error, not silently clamped.

**Concurrency:** two admins racing to delete the same not-yet-labeled batch, or a delete racing a label
generation on it, is handled by an ordinary check-then-act read of `LabelGeneratedOnUtc` at delete time
— last-write-wins on the rare simultaneous case, same posture GIL-002 took for `ServingSuggestion`. Not
worth more than this given the request volume a manual admin action implies.

**Resolved (round 2):** rows are **immutable once created** — a mistake is corrected by creating a new
row, not editing the old one, so the history stays a true audit trail. `BatchCode` is **system-generated**
(not typed by staff), proposed format `<ProductId-scoped date + sequential counter>` (e.g.
`20260903-001`) — human-readable enough to write on a physical pallet/jar batch, exact format left to
`ddd-modeler`. Uniqueness is a consequence of generation, not a hand-enforced constraint.

**Proposed default (not explicitly asked — flag if wrong):** a batch row may still be **deleted** (not
edited) as long as no label has ever been generated from it — covers an obvious data-entry mistake
caught before anything reaches a printer. Once a label has been generated from a batch, that batch is
fully locked (no delete) — deleting a row a real label was printed from would break the paper trail the
whole feature exists for. See §7 (permissions) and §10 (failure scenario).

**Proposed default:** `ProductionBatch` rows for a product are **retained** when that product is
soft-deleted (`Product` is `ISoftDeletedEntity`; per
`Docs/BusinessLogic/product-serving-suggestions.md`'s "Product deletion" section, a DB-level FK cascade
never fires on that path — this would need an explicit no-op, i.e. deliberately *not* wiring an
`EntityDeletedEvent<Product>` consumer that deletes batches). Reasoning: a recall/traceability record
should outlive a discontinued product, unlike `ServingSuggestion` (presentation content, correctly
cascade-deleted). Flag if the discontinued-product case should behave differently.

No existing mechanism covers any of this: `GenericAttribute` is schema-free and cannot be
listed/sorted/filtered per product or by date, and none of `ProductTag`/`SpecificationAttribute`/
`ProductAttribute` model a repeating, immutable, per-product log entry.

**Multi-store scoping (round 2 gap):** **proposed default** — no store-mapping, same posture as
`Ingredient`/`ServingSuggestion` today (both single-store-only in practice, never addressed explicitly).
Flag if production batches must be scoped per store.

**Label language — resolved (round 5):** the store's default language, with an explicit override
available at generation time when the store has more than one language configured. Not the ambient
ASP.NET Core request culture and not the generating admin's own session language — both would risk a
label rendering in the wrong language for the product's actual market. `Ingredient.Name`/`Description`
are `ILocalizedEntity` (resolved via `ILocalizationService.GetLocalizedAsync(..., languageId)`); unlike
the storefront widget, which calls this with no `languageId` (letting it fall back to the ambient working
language), the label generation path must pass an explicit `languageId` end to end — the chosen language,
not whatever happens to be ambient for the admin's own request.

**Ingredient ordering — resolved (round 3):** the label's descending-by-weight ingredient order reuses
the existing per-product `DisplayOrder` (`ProductIngredientMapping`), now carrying **two purposes at
once** — storefront display and legally-required weight order — where
`Docs/BusinessLogic/product-ingredients.md` explicitly scoped weight-ordering as out of v1 for GIL-001.
**This is a prerequisite, not a detail:** before this ticket ships, every existing product's ingredient
`DisplayOrder` needs a data-quality pass to confirm it is actually in descending-weight order, since
nothing has enforced that meaning until now. `ddd-modeler`/`plan-and-implement` should treat this as a
blocking checklist item, not an afterthought — a wrong order on a printed retail label is a compliance
defect, not a cosmetic one.

**Composite ingredients — resolved (round 4, reversing round 3's initial answer):** the label expands
composite ingredients inline, the same "fully expanded" style
`Docs/BusinessLogic/product-ingredients.md:141-143` describes for the storefront widget (e.g. "beef
broth (bones, water, carrot, celery, salt)"), rather than showing only directly-attached ingredients.
This pulls in a **second** ordering field with the identical problem: `IngredientComposition.DisplayOrder`
governs the order of components *within* a composite ingredient (used by
`IProductIngredientService.GetCompositionsReachableFromAsync`), and
`Docs/BusinessLogic/product-ingredients.md`'s "out of scope for v1" statement covers weight-ordering for
the ingredient system as a whole — this field included, not just the top-level one. The same prerequisite
therefore applies twice: **both** `ProductIngredientMapping.DisplayOrder` and
`IngredientComposition.DisplayOrder` need a pre-ship data-quality pass for descending-weight accuracy,
for every product and every composite ingredient reachable from it (up to GIL-001's 3-level nesting
cap), not just the former.

**Nutrition declaration — explicitly out of scope (round 3):** `Docs/Specs/GIL-004-ingredient-nutritional-values/spec.md`
(status: In Progress as of round 6 — a separate ticket, not authored here) is bringing in per-ingredient
kcal/macro data. This ticket's label
does **not** include a nutrition-facts table — deferred to a follow-up once GIL-004's data exists, not
folded in here.

**Storage conditions / country of origin — resolved (round 3), reversing round 2's framing:** these are
**not** fixed template text the developer supplies once. They are **per-product admin input**, since
both can genuinely vary by product. New `GenericAttribute` values on `Product`:
`ProductionLabels.StorageConditions` and `ProductionLabels.CountryOfOrigin` (both `string`, optional at
the data layer — see §10 for what an empty value does on the rendered label). Per
`entity-extension-check`: `ProductTag`/`SpecificationAttribute`/`ProductAttribute`/`Category`/
`Manufacturer` all rejected (none fit free-form per-product label text, and `SpecificationAttribute`
would additionally leak this into storefront comparison/filter UI, which is out of scope). Between
`GenericAttribute` and a schema migration on `Product`: `GenericAttribute` — nothing filters, sorts, or
reports on either value, so the "does something need `WHERE`/`ORDER BY` on it" test says no. Zero core
touch, via `IGenericAttributeService.SaveAttributeAsync`/`GetAttributeAsync<string>`.

**Round 7 correction — these values are per-language, not a single shared string.** §5's own label-language
decision (above) applies here too: a label generated in a different language must not print
untranslated storage-conditions/country-of-origin text while the ingredients section correctly switches
language — the developer confirmed both fields need a distinct value per label language, not one shared
value regardless of which language was chosen. `IGenericAttributeService.SaveAttributeAsync`/
`GetAttributeAsync<TPropType>` take a `storeId` but no `languageId`
(`src/Libraries/Nop.Services/Common/IGenericAttributeService.cs:66,80`), so a single key per field is no
longer sufficient. **Mechanism, considered and decided:** `LocalizedProperty` (nopCommerce's real
per-entity-per-language mechanism, already backing `Ingredient.Name`/`Description`) was considered and
rejected — **round 8 precision fix:** not because no raw-keyed read path exists (it does:
`ILocalizedEntityService.GetLocalizedValueAsync(languageId, entityId, localeKeyGroup, localeKey)`,
`src/Libraries/Nop.Services/Localization/ILocalizedEntityService.cs:35`, takes plain string keys, no
property required), but because the **write** side has no equivalent escape hatch:
`ILocalizedEntityService.SaveLocalizedValueAsync<T,TPropType>` enforces the identical real-`PropertyInfo`
reflection requirement `ILocalizationService.GetLocalizedAsync` does (`ILocalizedEntityService.cs:61`,
throws unless the key resolves to a real property — `LocalizedEntityService.cs:244-253`). A read-only
path without a matching write path doesn't fit here, so using `LocalizedProperty` would still mean either
adding real properties to core `Product` (contradicts the zero-core-touch decision already made for
these two fields) or writing directly against `IRepository<LocalizedProperty>`, bypassing the service
layer entirely for the write half. Instead: **one `GenericAttribute` row per (product, language)**, key
shape
`ProductionLabels.StorageConditions.{languageId}` / `ProductionLabels.CountryOfOrigin.{languageId}` —
a minimal extension of the mechanism already chosen, still zero core touch, still no
`WHERE`/`ORDER BY` need.

## 6. Admin & storefront surface

No storefront surface (see §4 — no `PublicWidgetZones` entry). Admin surface, confirmed as **both**:

- A tab on the admin product-edit page (`AdminWidgetZones.ProductDetailsBlock`) scoped to that
  product's batches — mirrors `ProductIngredientsAdminViewComponent`
  (`src/Plugins/Nop.Plugin.Misc.Ingredients/Admin/Components/ProductIngredientsAdminViewComponent.cs`).
- A standalone "Production" admin menu section (`AdminMenuCreatedEvent`, per
  `Docs/ai-harness/02-extensibility-and-plugins.md` step 7) listing batches across all products, with
  product selection when creating a new batch.

Both surfaces read/write through the same `IProductionBatchService` and should share view-model/grid
logic rather than duplicate it. **Resolved (round 2):** list ordering is newest-first on both surfaces.
Each batch row (either surface) gets a "Generate label" action: pick one of **two** preset size
variants — small jar / large jar, same content and layout, geometry only differs (confirmed) — and, when
the store has more than one language configured, a language for the label (defaulting to the store's
default language — §5) — then download the rendered PDF for that one product+batch. **Round 3 gap,
fixed:** `LabelGeneratedOnUtc` is
stamped **only after** the PDF has rendered successfully and is being returned to the admin — never
before render, and never on a failed render. A conversion failure (§10) therefore leaves the batch
unlocked and deletable, which is correct: no real label was produced, so the lock's reason for existing
doesn't apply yet. The narrow window between a successful render and the stamp write is accepted as-is,
consistent with how the rest of this codebase doesn't reach for outbox/distributed-transaction patterns
for single-request operations.

The product-edit tab additionally carries two text inputs — Storage conditions, Country of origin — set
**once per product, per active store language** (round 7), not per batch (§5). One input per configured
language per field (same admin pattern as any other localized field), not a single shared box. Saving
them is gated by `ProductionLabels.Create` (§7); at label-render time, the value for the label's chosen
language (§5) is read via `IGenericAttributeService`, not passed through `IProductionBatchService`.

**Label content — confirmed real retail label (EU 1169/2011-scoped), field by field:**

| Field | Source | Status |
|---|---|---|
| Product name | `Product.Name` | existing |
| Ingredients, descending weight order, composite ingredients expanded inline | `IIngredientService` / `IProductIngredientService` (`GetCompositionsReachableFromAsync`), existing `ProductIngredientMapping.DisplayOrder` **and** `IngredientComposition.DisplayOrder` (both repurposed — see §5 prerequisite) | existing, **repurposed, two fields** — pre-ship data-quality pass required on both (§5) |
| Allergens, emphasized | `Ingredient.AllergenId`/`Allergen`, already returned by the existing calls this ticket reuses | **new label-rendering work only** — no Ingredients-side change (see note below) |
| Net quantity | `Product.Weight` | resolved (round 2) — confirmed sufficient as-is, no separate net/drained-weight field |
| Best-before date, batch code | `ProductionBatch` (this spec) | new |
| Food business operator name/address | `Store.CompanyName` / `CompanyAddress` / `CompanyPhoneNumber` (verified: `src/Libraries/Nop.Core/Domain/Stores/Store.cs:69,74,79`) | existing |
| Storage conditions | `GenericAttribute` `ProductionLabels.StorageConditions.{languageId}` on `Product`, one per language (this spec, §5) | new — per-product, per-language admin input |
| Country of origin | `GenericAttribute` `ProductionLabels.CountryOfOrigin.{languageId}` on `Product`, one per language (this spec, §5) | new — per-product, per-language admin input |
| Nutrition declaration | — | **explicitly out of scope** (§5) — deferred until `GIL-004-ingredient-nutritional-values` data exists |

**Allergens — round 4 correction, refined round 6:** the storefront *view* carries no allergen data —
`PublicIngredientModel` has only `Name`/`Children`, no markup for it anywhere — so there is no existing
*rendering* pattern to reuse, as round 4 correctly found. But round 4 overcorrected in the other
direction: no Ingredients-side interface change is actually needed. `IIngredientService`/
`IProductIngredientService`'s existing public calls (`GetDirectIngredientsByProductIdAsync` →
`GetCompositionsReachableFromAsync` → `GetIngredientsByIdsAsync` — the same three calls the storefront
widget already chains, `IngredientsViewComponent.cs:88-113`) already return full `Ingredient` entities,
and `Ingredient.AllergenId`/`Allergen` are already public properties on it. This ticket's own service
layer reads `AllergenType` directly off the `Ingredient` objects these existing calls return — for
nested children too, since the same calls already walk the composition tree. No cross-plugin
coordination, no Ingredients-side change at all; only new work on the ProductionLabels side (the label
view model/template, which is new regardless).

## 7. Settings, permissions, localization

No new `ISettings` — template variants are hardcoded, not admin-configurable (confirmed).

Permissions (mirroring `IngredientsPermissionConfigManager`'s shape,
`src/Plugins/Nop.Plugin.Misc.Ingredients/Services/IngredientsPermissionConfigManager.cs`, but
**changed from the two-permission CRUD shape** to match §5's immutability): `ProductionLabels.View`
(read + generate/download a label), `ProductionLabels.Create` (log a new batch), and
`ProductionLabels.Delete` (remove a not-yet-labeled batch only — the service layer enforces the
lock, the permission alone does not distinguish "labeled" from "not labeled"). All three under
`StandardPermission.Catalog`, `NopCustomerDefaults.AdministratorsRoleName` by default.

New locale resource keys for admin labels/buttons/validation messages under
`Plugins.Misc.ProductionLabels.*`, including field label + `.Hint` for the two new
`GenericAttribute`-backed inputs (Storage conditions, Country of origin — §5, §6).

`UninstallAsync` must remove all three permission records explicitly (installed automatically via
`IPermissionConfigManager`, removal is not — confirmed by Ingredients' own `UninstallAsync`) and call
`DeleteLocaleResourcesAsync("Plugins.Misc.ProductionLabels")`; the table drops automatically via
`base.UninstallAsync()`. `ProductionBatch` is not `ILocalizedEntity` (no translatable fields), so the
`LocalizedProperty` orphan-cleanup step Ingredients needs for its own entity is N/A here.

**Round 3 gap, fixed; updated round 7 for per-language keys:** `GenericAttribute` is the same kind of
shared core table `LocalizedProperty` is — this plugin's own migration `Down()` won't touch it either, so
these keys would survive as orphans the same way. Since round 7 made the two keys per-language
(`ProductionLabels.StorageConditions.{languageId}` / `ProductionLabels.CountryOfOrigin.{languageId}`),
`UninstallAsync` must purge every language's variant of both, not one fixed key — enumerate configured
languages and call `IGenericAttributeService.DeleteAttributesAsync<Product>(...)` per language/field
combination (exact iteration approach — configured languages at uninstall time vs. every language ever
used — left to `ddd-modeler`).

## 8. Events & scheduled tasks

N/A — no new events published or consumed beyond the standard
`EntityInserted/Updated/DeletedEvent<ProductionBatch>` core already raises; no `IScheduleTask`.

## 9. Caching

**Corrected during drafting** (the first draft of this spec wrongly claimed an existing per-product
cache pattern to mirror): `IngredientCacheEventConsumer` is verified to be plain
`CacheEventConsumer<Ingredient>` boilerplate with no per-product cache key, and
`ProductIngredientService`'s per-product reads are **not cached at all** today — they hit the
repository directly every call. No per-product cache for `ProductionBatch` in v1 either, matching that
existing (uncached) precedent exactly. Revisit only if this list is shown to be a real hot path later —
not assumed here.

**Round 3 gap, fixed:** the two `GenericAttribute` reads (storage conditions, country of origin) go
through `IGenericAttributeService.GetAttributesForEntityAsync`, which is backed by
`IShortTermCacheManager`/`PerRequestCacheManager` — request-scoped, invalidated on save via the
framework's own `GenericAttributeCacheEventConsumer`. No cross-instance coherence concern on ECS; no
custom caching needed here.

## 10. Failure scenarios

- Product with zero `ProductionBatch` rows: "Generate label" is blocked (a label needs a batch code and
  a best-before date to be meaningful) rather than producing a label with empty fields.
- Product with zero mapped ingredients: label renders with an empty ingredients/allergens section, does
  not block generation — same posture `ServingSuggestion` already takes for its own optional steps.
- Delete attempted on a batch that already has a generated label: blocked (§5) — surfaced as a normal
  validation error, not a silent no-op.
- `BestBeforeDateUtc` not after `ProductionDateUtc`, or `Quantity` not greater than zero: rejected at
  the service layer with a validation error (§5).
- Storage conditions / country of origin left blank for a product: label renders without that line
  rather than blocking generation — these are informational, not structurally required the way a batch
  code is.
- **Round 5 precision fix** (round 4's wording was ambiguous about exactly this): the block condition is
  **truncation would actually occur** — a node at GIL-001's 3-level nesting cap
  (`IngredientsDefaults.MaxCompositionDepth`) still has further children being cut off — **not** merely
  that a composition legitimately reaches depth 3. A complete, correctly-entered depth-3 composite is
  normal data (GIL-001's cap allows exactly 3 levels) and renders fully, same as any other product — it
  must not block. Per `IngredientCompositionService.ValidateNewEdgeAsync`, an edge is only ever rejected
  when it would make the realized depth **exceed** 3, so real truncation (the actual block condition)
  should indeed be unreachable for legitimately-entered data — that claim holds under this precise
  reading, not under "any depth-3 sighting blocks." The storefront widget's own truncation is framed as
  cosmetic (`Docs/BusinessLogic/product-ingredients.md:143-144`); on a real printed label the same event
  is a compliance defect, not a display nicety, which is why the label's posture (block) differs from
  the storefront's (silently cut off) for the one case where truncation could occur at all.
- **Round 4 gap, fixed:** storage conditions / country of origin are free-form admin text inserted into
  an HTML template that §13's rendering candidate is a real browser engine (PuppeteerSharp/Chromium) —
  markup in either field must not execute. Both values are HTML-encoded before insertion (standard Razor
  `@`-encoding; the template must not use `Html.Raw` on either field), not treated as trusted markup.
- HTML→PDF conversion failure: normal exception handling, no special swallowing (rule 10 of
  `00-system-instructions.md`) — this is an external-library failure, not a scenario the plugin's own
  logic can meaningfully recover from.

## 11. Test scenarios

- `ProductionBatchService`: create, delete-when-unlabeled, delete-blocked-when-labeled, list-by-product
  (newest first).
- Validation: `BestBeforeDateUtc` not after `ProductionDateUtc` rejected; `Quantity <= 0` rejected.
- Label data assembly (product + ingredients + batch + store company info + storage/origin
  `GenericAttribute` values → label view model): normal case, zero-ingredients case,
  multiple-allergens case, blank storage/origin case, mocked `IIngredientService`.
- Ingredient ordering on the label follows `DisplayOrder` — a test asserting render order matches
  `ProductIngredientMapping.DisplayOrder` ascending at the root, not insertion order or another field.
- **Round 4 additions:** a directly-attached ingredient that is itself composite renders expanded with
  its nested children (not just its own name); within-composite child order follows
  `IngredientComposition.DisplayOrder` ascending, asserted as a scenario distinct from root ordering;
  each rendered node (root and nested) carries its correct `AllergenType`, asserted with a mix of
  allergenic and non-allergenic nodes at different depths; storage conditions/country of origin
  containing HTML/script-like input render as literal text on the label, not as markup; a complete,
  legitimately-entered depth-3 composite renders in full without blocking (the normal case); "Generate
  label" blocks only for the (should-be-unreachable) case where a node at depth 3 has further children
  that would be cut off.
- **Round 5 addition:** label data assembly called with an explicit non-default `languageId` renders
  ingredient names/descriptions in that language, not the ambient working language — asserted against a
  product with at least two configured languages.
- **Round 7 addition:** storage conditions/country of origin render the value saved for the label's
  chosen language, not another language's value or a shared fallback — asserted with different text
  saved per language on the same product; uninstall purges every language's `GenericAttribute` row for
  both fields, not just one.
- Permission gating on both admin surfaces (product tab and standalone section), and on saving the two
  `GenericAttribute` inputs.
- "Generate label" blocked when the product has no batches (§10).
- Generating a label stamps `LabelGeneratedOnUtc` and subsequently blocks delete on that row.
- Batch-code generation produces a value unique enough not to collide under the proposed date+counter
  scheme (exact assertion depends on `ddd-modeler`'s chosen format).

## 12. Documentation impact

New `Docs/Glossary/shop.md` entry for "Production batch" (mirrors the existing `Ingredient` / `Serving
suggestion` entries) — should state the immutability rule explicitly, since that is the detail most
likely to be assumed away by a future reader. New `Docs/BusinessLogic/product-production-labels.md`
(name TBD) documenting the batch history lifecycle, the delete-lock rule, and label generation rules —
written in the same commit as the code, per `AGENTS.md`'s process constraint, not now.

Two more things belong in that same doc, both surfaced during this spec's drafting: both
`DisplayOrder` fields' dual purpose (storefront display **and** legal weight order, §5 — this now
covers `ProductIngredientMapping.DisplayOrder` **and** `IngredientComposition.DisplayOrder`) needs to be
documented so a future `Ingredient` change doesn't silently break label compliance; and
`Docs/Glossary/shop.md`'s `Ingredient`/`Composite ingredient` entries should get a cross-reference note
once both fields carry that second meaning. Also update `Docs/BusinessLogic/product-ingredients.md`
itself — it currently states weight-ordering is out of scope, which stops being accurate once this
ticket ships.

## 13. Deployment & rollout

Needs an HTML→PDF conversion mechanism the current Docker image doesn't have — core's own `IPdfService`
is a document-model API (`iTextSharp` + `PdfRpt`, see
`src/Libraries/Nop.Services/Common/Pdf/PdfDocumentHelper.cs`), not HTML rendering, so this is a new
dependency, not a reuse.

**Open question, deliberately not decided here** (solution design is `ddd-modeler`'s job, not this
spec's): whatever library is chosen must run inside the existing `aspnet:10.0-alpine` runtime image
(repo-root `Dockerfile`) without regressing the documented "build the whole `.sln`, publish only
`Nop.Web.csproj`" step (rule 6 of `00-system-instructions.md`) or silently swapping the base distro away
from Alpine. A desk-research lead from brainstorming (unverified against a real build): PuppeteerSharp +
`apk add chromium`, a documented pattern for headless Chromium on musl/Alpine. **Proposed default:**
verify this with an actual build-and-render smoke test as the first concrete step of `ddd-modeler`/
implementation, rather than carrying it as an unverified assumption further into design. Expect a
meaningful image-size increase (~100-170MB) whichever route is chosen; no hard cap has been set.

Immediate rollout once merged — no staged flag needed at this scope.
