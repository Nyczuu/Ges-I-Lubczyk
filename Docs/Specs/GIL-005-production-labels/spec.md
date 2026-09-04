---
id: GIL-005
kind: Task
title: Production batches and printable PDF labels per product
status: In Progress
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

## Technical design (ddd-modeler)

Every load-bearing citation in this spec was re-verified against current on-disk source (not re-trusted
from the spec's own prose) before this design was written — no corrections were needed; every citation
checked out exactly.

### Placement

`Nop.Plugin.Misc.ProductionLabels`, `IMiscPlugin` **and** `IWidgetPlugin`, registering only
`AdminWidgetZones.ProductDetailsBlock` — confirmed this zone already hosts both Ingredients' and
ServingSuggestions' own admin cards side by side today, so a third occupant is the same mechanism, not a
new one.

`.csproj` needs a `ProjectReference` to `Nop.Plugin.Misc.Ingredients.csproj` — **the first
plugin-to-plugin `ProjectReference` in this codebase** (every existing plugin `.csproj` references only
`Nop.Web`/`Nop.Web.Framework`). Verified safe against `WebAppTypeFinder.InitData()`, which skips loading
a duplicate assembly by `AssemblyName.FullName` — only one `Assembly` object is ever loaded, and this
holds because both plugins always build from the same `.sln` in the same Docker build step. Real
residual fragility: neither plugin's service/repository classes are marked `internal`, so "reference the
interface, not the internals" is convention-enforced, not compiler-enforced.

### Domain model

```csharp
public class ProductionBatch : BaseEntity
{
    public int ProductId { get; set; }
    public string BatchCode { get; set; }
    public DateTime ProductionDateUtc { get; set; }
    public DateTime BestBeforeDateUtc { get; set; }
    public int Quantity { get; set; }
    public DateTime? LabelGeneratedOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}
```

`Data/Mapping/Builders/ProductionBatchBuilder.cs` explicitly maps only `ProductId`
(`.AsInt32().ForeignKey<Product>()`, default `Rule.Cascade` — verified inert regardless, since `Product`
is `ISoftDeletedEntity` and `ProductService.DeleteProductAsync` only ever issues a soft-delete `UPDATE`,
never a physical `DELETE`) and `BatchCode` (`.AsString(50).NotNullable()`); every other column
auto-maps correctly from its C# type via `FluentMigratorExtensions`, the same convention
`IngredientBuilder.cs` already relies on.

No `Update` method exists anywhere in the service — rows are immutable per spec, so there is
deliberately no update path to omit-by-forgetting. Invariants enforced in the service, not the schema:
`BestBeforeDateUtc > ProductionDateUtc`; `Quantity > 0`; delete rejected once
`LabelGeneratedOnUtc.HasValue` — all via `NopException`, the established idiom for service-layer
business-rule rejection in this codebase (`IngredientCompositionService.ValidateNewEdgeAsync`,
`IngredientService.DeleteIngredientAsync`). `BatchCode` has no `.Unique()` constraint, matching the
spec's "uniqueness is a consequence of generation."

Migration: `[NopMigration("2026-09-03 00:00:00", "Misc.ProductionLabels schema", MigrationProcessType.Installation)]`,
`Up()` → `this.CreateTableIfNotExists<ProductionBatch>()`, `Down()` →
`this.DeleteTableIfExists<ProductionBatch>()` — mirrors `ServingSuggestions/SchemaMigration.cs` exactly.

### Extension decisions (re-verified)

1. **`ProductionBatch` is a new entity/table**, not an existing mechanism — `GenericAttribute` doesn't
   fit a per-product list of many dated, orderable rows; `ProductTag`/`SpecificationAttribute`/
   `ProductAttribute` all fail the "repeating, immutable, per-product, listed/sorted/filtered by date"
   requirement the same way the spec already reasoned.
2. **`GenericAttribute` per (product, language)** for storage conditions/country of origin — re-verified
   the write-path claim precisely: `ILocalizedEntityService.SaveLocalizedValueAsync<T,TPropType>` takes
   an `Expression<Func<T,TPropType>>` and throws `ArgumentException` unless it resolves to a real
   `PropertyInfo` on `T`, confirmed verbatim against `LocalizedEntityService.cs:244-253`. Since `Product`
   has no such property (and adding one would contradict the zero-core-touch decision), `LocalizedProperty`
   genuinely has no usable write path here — the spec's conclusion holds exactly as stated.

### Services — two, deliberately split

- **`IProductionBatchService`** — CRUD/listing only, owns no Ingredients/Store/GenericAttribute reads.
  `GetAllProductionBatchesAsync(int? productId, pageIndex, pageSize)` — newest-first, one shared search
  model (`ProductionBatchSearchModel : BaseSearchModel` with an optional `ProductId`) driving both admin
  surfaces, modeled on the real existing core precedent `ProductReviewSearchModel.SearchProductId` (used
  by `ProductReviewModelFactory` for the identical "scope an otherwise-global admin list to one product"
  shape). **Uncached** — plain repository queries, not the `GetByIdAsync(id, cache => ...)` shortcut,
  since even `cache => default` routes through the long-lived static cache manager and would need its own
  cache-invalidation consumer, which the spec explicitly doesn't want here.
  `InsertProductionBatchAsync` validates, generates `BatchCode`, stamps `CreatedOnUtc`.
  `DeleteProductionBatchAsync` throws `NopException` if already labeled; plain check-then-act, no
  transaction (matches the spec's explicit last-write-wins posture for this race).
  `MarkLabelGeneratedAsync` sets `LabelGeneratedOnUtc = DateTime.UtcNow`; called from exactly one place.

  **`BatchCode` format**: `{ProductionDateUtc:yyyyMMdd}-{counter:D3}` (e.g. `20260903-001`). Counter =
  `1 + MAX` of the existing numeric suffix for `(ProductId, ProductionDateUtc.Date)`, deliberately
  **not** `COUNT` — since unlabeled batches can be deleted, `COUNT` would produce a genuine (not merely
  racy) duplicate whenever a middle batch of the day is deleted and a new one added afterward. No
  serializable transaction around the read-then-insert; a truly simultaneous double-insert could
  theoretically collide, accepted under the same "not worth more, given manual-admin-action volume" bar
  the spec already sets for the delete race.

- **`IProductionLabelModelFactory`** — pure content assembly, no PDF dependency (matches spec §11's own
  separate "label data assembly" test grouping). Walks the ingredient graph with a **new, parallel**
  recursive builder rather than reusing `IngredientsViewComponent.BuildNodeAsync` (that method is
  `protected` on a storefront view component, outside the sanctioned public-interface reuse surface, and
  its output model carries no `AllergenType`) — but calls the *exact same three public methods* the
  storefront already chains (`GetDirectIngredientsByProductIdAsync` → `GetCompositionsReachableFromAsync`
  → `GetIngredientsByIdsAsync`), confirming zero Ingredients-side change is needed. Recursion bound reads
  `IngredientsDefaults.MaxCompositionDepth` rather than a hardcoded `3`. Truncation check mirrors
  `BuildNodeAsync`'s own guard, inverted: throws `NopException` only when a node at the depth boundary
  still has recorded child edges (real truncation) — confirmed via `IngredientCompositionService.
  ValidateNewEdgeAsync` that legitimately-entered data can reach exactly depth 3 but never exceed it, so
  this throw path is unreachable for good data, matching the spec's own claim precisely. Ordering:
  `GetCompositionsReachableFromAsync`/`GetDirectIngredientsByProductIdAsync` already return
  `DisplayOrder`-sorted lists, and grouping an already-sorted list preserves order (`Enumerable.GroupBy`),
  so the factory groups rather than re-sorts at either level. Reads `Ingredient.Name`/`Description` via
  `GetLocalizedAsync` with an **explicit, non-null `languageId`** end to end (the storefront passes none,
  falling back to the ambient working language — this factory must not).

  **Graceful degrade when Ingredients is uninstalled** (added after Gate 1, developer-requested):
  the three-call ingredient-read chain is extracted into its own method and wrapped in
  `catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)`, returning an
  empty ingredient list — reusing the same empty-list path the already-designed zero-ingredients case
  renders, not a second, separately-maintained "empty because of an error" shape — plus one
  `_logger.WarningAsync(...)` line so an operator can discover why a label lost its ingredients section.
  Scoped deliberately narrow: `PostgresException` (not the broader `NpgsqlException`) excludes genuine
  connection failures, which typically never reach a structured server ERROR response; the `SqlState`
  filter excludes other query bugs (wrong column, syntax error carry different SQLSTATEs); and the catch
  is physically scoped to only the Ingredients-table calls, so a schema problem on this plugin's own
  `ProductionBatch` table, or on `Product`/`Store`/`GenericAttribute`, still surfaces normally since those
  reads happen elsewhere in the factory, outside this try block. No precedent for this failure class
  exists elsewhere in the repo — this is the first. Open item carried into implementation: confirm at
  first build that `Npgsql.PostgresException` is publicly constructible against this repo's pinned
  Npgsql version for the unit test; if not, extract the classification into a
  `protected virtual bool IsIngredientsSchemaMissing(Exception ex)` predicate so a test subclass can
  substitute an easily-constructed stand-in. A real drop-the-table integration test is not achievable in
  the existing suite (it runs on SQLite, which throws a different exception type) — treat "uninstall
  Ingredients on the real Postgres target, then generate a label" as a manual pre-release smoke check,
  the same treatment already given to the PDF-library assumption below. Considered and rejected:
  declaring `DependsOnSystemNames` in `plugin.json` (would make the *admin-UI* uninstall path refuse
  outright) — developer chose the catch alone; not adding the extra declaration.

- **`IHtmlToPdfConverter`** — `Task<byte[]> ConvertAsync(string html)`. Isolates the still-open PDF
  library choice behind one seam; page-size geometry is CSS driven by a `SizeVariant` property on the
  label view model, not a converter-API parameter, so swapping the eventual library touches nothing else.

**"Generate label" flow** (one controller action, reached from either admin surface): build the label
model (throws on real truncation — nothing renders or stamps) → `RenderPartialViewToStringAsync` (a real,
already-existing `BaseController` method, confirmed callable from `BasePluginController`) → convert to
PDF (library failure propagates normally, no swallowing) → only then `MarkLabelGeneratedAsync`. The
stamp-only-after-success ordering falls out of plain sequential code, no extra flag needed. "Product with
zero batches blocks Generate label" needs no separate guard — the action is a per-batch-row button, so a
product with zero rows simply has none to click.

### Permissions, menu, localization

Three permissions (`ProductionLabels.{View,Create,Delete}`) under `StandardPermission.Catalog`,
`AdministratorsRoleName` default — same shape as Ingredients'/ServingSuggestions' own permission
managers. Admin menu entry anchored `AfterMenuSystemName = "Filter level values"` — **not** off
Ingredients' own menu item, because `BaseAdminMenuCreatedEventConsumer` silently drops a menu item with a
missing anchor, and anchoring off Ingredients would hide "Production" from an admin who has
`ProductionLabels.View` but lacks `Ingredients.View`, or if Ingredients is ever uninstalled. Per-language
storage-conditions/country-of-origin editor reuses the existing `Html.LocalizedEditorAsync` UI helper
(confirmed persistence-agnostic — it only depends on `ILocalizedModel<TLocal>.Locales`), populated via
`ILocalizedModelFactory.PrepareLocalizedModelsAsync` across **all system-configured languages** — a
deliberately different scope from the **label-generation-time** language picker, which is scoped to the
*store's* configured languages per the spec's round-5 wording. `UninstallAsync` removes all three
permissions, calls `DeleteLocaleResourcesAsync("Plugins.Misc.ProductionLabels")`, and enumerates every
configured language to purge both `GenericAttribute` keys per language (round 7's requirement — a naive
copy of Ingredients'/ServingSuggestions' own uninstall would miss this, since neither has per-language
`GenericAttribute` keys to enumerate).

### Caching, events

No new cache — no `ProductionBatchCacheEventConsumer`; the two `GenericAttribute` reads ride the
existing `GenericAttributeCacheEventConsumer`/`IShortTermCacheManager` machinery unchanged. No new events
published or consumed beyond the standard `EntityInserted/Updated/DeletedEvent<ProductionBatch>`; no
`EntityDeletedEvent<Product>` consumer, deliberately, so batches outlive a discontinued product.

### Blast radius

`AdminWidgetZones.ProductDetailsBlock` and `StandardPermission.Catalog` are both already shared by
multiple existing plugins/features — purely additive here. New `GenericAttribute` keys are
uniquely prefixed, grepped for collisions, none found. `IIngredientService`/`IProductIngredientService`
gain a new read-only caller with unchanged contracts. **The one genuinely new risk**: this is the first
plugin with a hard *runtime* DI dependency on another plugin's service — addressed by the graceful-degrade
catch above, which is why that addition exists.

### Installed-store impact

One new, empty table on fresh or existing installs (purely additive, safe under a rolling ECS deploy).
New, uniquely-keyed `GenericAttribute` rows — no existing rows touched. Three new permission rows,
auto-installed, not retroactively granted to any non-Administrator role. New locale keys under a
plugin-unique prefix, with uninstall purging every language variant of both `GenericAttribute` keys.
Zero storefront impact — no `PublicWidgetZones` entry at all. No new settings.

### Pre-ship blocking item (carried forward from the spec, not optional)

Before this ships, a data-quality pass must confirm every existing product's
`ProductIngredientMapping.DisplayOrder` **and** every composite ingredient's
`IngredientComposition.DisplayOrder` are actually in descending-weight order — a data-audit task, not a
code change, but it gates correctness of the label content itself.

### Open questions carried into implementation

- **PDF rendering library** — deliberately left open per spec §13, pending a real build-and-render smoke
  test against the Alpine-based runtime image. `IHtmlToPdfConverter` is renderer-agnostic so the eventual
  choice doesn't ripple elsewhere.
- **`PostgresException` constructibility** for the graceful-degrade unit test — confirm at first build;
  fallback (an extracted, overridable predicate) is already designed above if needed.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-04
**Revision notes:** Gate 1 approved in two parts — the base design as first proposed, then one addition
(graceful degrade when `Nop.Plugin.Misc.Ingredients` is uninstalled while `ProductionLabels` stays
installed) requested against a risk the base design itself surfaced. `DependsOnSystemNames` in
`plugin.json` was considered as a further belt-and-suspenders measure and explicitly declined — the
catch alone is the accepted mitigation.

## Implementation plan (implementation-planner)

File-by-file plan for the standalone Task, each file mirroring an existing analogous file in
`Nop.Plugin.Misc.Ingredients` and/or `Nop.Plugin.Misc.ServingSuggestions` unless noted as having no
mirror. No further domain decisions made here — only how the approved design becomes concrete files.

### New plugin skeleton

- **`Nop.Plugin.Misc.ProductionLabels.csproj`** — mirrors `Nop.Plugin.Misc.Ingredients.csproj`, plus a
  **second** `ProjectReference` (to `Nop.Plugin.Misc.Ingredients.csproj` — the design's flagged first
  plugin-to-plugin reference). No `Public\Views\*` content entries at all (no storefront surface).
- **`plugin.json`** — `SystemName: "Misc.ProductionLabels"`, `Group: "Misc"`, `SupportedVersions: ["5.00"]`.
- **`logo.png`** — placeholder, `Content`/`PreserveNewest`.
- **`ProductionLabelsDefaults.cs`** — `SystemName`, `ProductionLabelsMenuSystemName`, route-name
  constants, and the two `GenericAttribute` key **prefixes** (`ProductionLabels.StorageConditions.`,
  `ProductionLabels.CountryOfOrigin.`, language id appended at call sites).

### Domain / data

- **`Domain/ProductionBatch.cs`** — the entity exactly as given in the approved design above.
- **`Domain/ProductionLabelSizeVariant.cs`** — `enum { SmallJar, LargeJar }`, never persisted (a
  per-request rendering choice, unlike `AllergenType`).
- **`Data/Mapping/Builders/ProductionBatchBuilder.cs`** — maps only `ProductId`
  (`.AsInt32().ForeignKey<Product>()`) and `BatchCode` (`.AsString(50).NotNullable()`); every other
  column auto-maps from its CLR type.
- **`Data/Migrations/SchemaMigration.cs`** — `[NopMigration("2026-09-04 00:00:00", "Misc.ProductionLabels schema", MigrationProcessType.Installation)]`,
  `Up()` → `CreateTableIfNotExists<ProductionBatch>()`, `Down()` → `DeleteTableIfExists<ProductionBatch>()`.
  Mirrors `ServingSuggestions/Data/Migrations/SchemaMigration.cs` exactly — note
  `.claude/skills/migration-standards-check/SKILL.md`'s own top example is stale (shows
  `[NopSchemaMigration]`/`ForwardOnlyMigration`); follow the real sibling shape, not that example.

### Services

- **`IProductionBatchService`/`ProductionBatchService`** — `GetAllProductionBatchesAsync(int? productId, pageIndex, pageSize)`
  (newest-first, uncached plain repository query — not the `GetByIdAsync(id, cache => ...)` shortcut),
  `GetProductionBatchByIdAsync`, `InsertProductionBatchAsync` (validates, generates `BatchCode` as
  `{ProductionDateUtc:yyyyMMdd}-{counter:D3}` where counter is `1 + MAX` of the existing numeric suffix
  for `(ProductId, ProductionDateUtc.Date)` — deliberately not `COUNT`, which would collide after a
  mid-day batch delete), `DeleteProductionBatchAsync` (throws `NopException` if `LabelGeneratedOnUtc.HasValue`),
  `MarkLabelGeneratedAsync`.
- **`Services/ProductionLabelModel.cs`** — `ProductionLabelModel` (ProductName, Ingredients tree,
  NetQuantity **with its measure-unit string, resolved via `IMeasureService` against the store's base
  weight measure — round 10 resolution, not a bare decimal**, BatchCode, BestBeforeDateUtc, Company
  fields, StorageConditions/CountryOfOrigin, SizeVariant) + `ProductionLabelIngredientModel`
  (Name, AllergenType, nested Children).
- **`IProductionLabelModelFactory`/`ProductionLabelModelFactory`** — `PrepareProductionLabelModelAsync(ProductionBatch, languageId, sizeVariant)`.
  Walks the ingredient graph via the same three public calls the storefront already chains
  (`GetDirectIngredientsByProductIdAsync` → `GetCompositionsReachableFromAsync` → `GetIngredientsByIdsAsync`)
  through a **new** recursive builder (not `IngredientsViewComponent.BuildNodeAsync`, which is `protected`
  and carries no `AllergenType`), bounded by `IngredientsDefaults.MaxCompositionDepth`, throwing
  `NopException` only on real truncation (a depth-boundary node with further recorded children).
  `Product.Name` **and** ingredient names/descriptions both read via `GetLocalizedAsync(..., languageId, ...)`
  with the label's explicit language passed end to end — never the ambient working language (round 10
  closes a gap the plan itself flagged: the design was explicit for ingredients but silent on
  `Product.Name`; same rule applies to both).
  **Graceful-degrade addition**: the three-call ingredient chain is extracted into its own method,
  wrapped in `catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)`
  returning an empty ingredient list plus one `_logger.WarningAsync(...)` line — with a
  `protected virtual bool IsIngredientsSchemaMissing(Exception ex)` extraction point if
  `Npgsql.PostgresException` proves awkward to construct directly in a unit test (confirm at first build).
- **`Services/Pdf/IHtmlToPdfConverter.cs`** — `Task<byte[]> ConvertAsync(string html)`. **No concrete
  implementation planned here** — blocked on the still-open PDF-library choice (§13); the interface alone
  is enough for every other file to compile against.
- **`Services/ProductionLabelsPermissionConfigManager.cs`** — three `PermissionConfig`s (`View`,
  `Create`, `Delete`), `StandardPermission.Catalog`, `AdministratorsRoleName` default.
- **`Services/Events/ProductionLabelsMenuEventConsumer.cs`** — `BaseAdminMenuCreatedEventConsumer`,
  gated on `PRODUCTION_LABELS_VIEW`, `InsertType.After`, **`AfterMenuSystemName = "Filter level values"`**
  (not Ingredients' own menu item — a missing anchor silently drops the item, and anchoring off
  Ingredients would hide "Production" from an admin without `Ingredients.View`, or after an Ingredients
  uninstall).

### Infrastructure

- **`Infrastructure/NopStartup.cs`** — registers `IProductionBatchService`, `IProductionLabelModelFactory`,
  `ProductionLabelsAdminModelFactory`. The `IHtmlToPdfConverter` registration line is commented/deferred
  pending the library choice.
- **`Infrastructure/RouteProvider.cs`** — `Admin/ProductionLabels/List` → `ProductionLabelsAdminController.List`.
- **`Infrastructure/MapperConfiguration.cs`** — `ProductionBatch` ↔ `ProductionBatchModel` (ignoring
  `ProductName`, populated by the factory, not AutoMapper).
- **`ProductionLabelsPlugin.cs`** — `BasePlugin, IMiscPlugin, IWidgetPlugin`; `GetWidgetZonesAsync()`
  returns **only** `AdminWidgetZones.ProductDetailsBlock` (no `PublicWidgetZones` entry); single-branch
  `GetWidgetViewComponent` (no zone switch needed, unlike both mirrors, since there's only one zone).
  `UninstallAsync`: removes all three permission records, `DeleteLocaleResourcesAsync`, and — **the one
  step with no direct mirror** — enumerates every configured language and calls
  `IGenericAttributeService.DeleteAttributesAsync<Product>(...)` for both key prefixes per language
  (round 7's requirement; neither sibling has per-language `GenericAttribute` keys to enumerate). No
  `IRepository<LocalizedProperty>` cleanup needed (not `ILocalizedEntity`); table drops automatically via
  `base.UninstallAsync()`.

### Admin surface

- **`Admin/Components/ProductionLabelsAdminViewComponent.cs`** — renders the product-edit tab, gated on
  `PRODUCTION_LABELS_VIEW`.
- **`Admin/Models/`** — `ProductionBatchModel` (plain `BaseNopEntityModel`; `BatchCode` shown, never
  editable — system-generated), `ProductionBatchListModel`, `ProductionBatchSearchModel`
  (`SearchProductId`, `0` = all products for the standalone section, mirrors the real core precedent
  `ProductReviewSearchModel.SearchProductId` — the same "one service, two admin surfaces" shape),
  `ProductionLabelsProductModel` (`ILocalizedModel<ProductionLabelsProductLocalizedModel>` carrying
  `StorageConditions`/`CountryOfOrigin` per language — reuses `Html.LocalizedEditorAsync`, populated via
  `ILocalizedModelFactory.PrepareLocalizedModelsAsync` across **all system-configured languages**, a
  deliberately different scope from the label-generation-time picker which is scoped to the *store's*
  configured languages), `GenerateProductionLabelModel` (batch id, size variant, optional language —
  `null` defaults to the store's default language, or the **first language by `DisplayOrder`** among
  active languages if the store's `DefaultLanguageId` is `0` — round 10 resolution for a case the design
  named the data source for but not the zero-case algorithm).
- **`Admin/Validators/ProductionBatchValidator.cs`** — `BestBeforeDateUtc` greater than
  `ProductionDateUtc`, `Quantity` greater than `0` — mirrored by the identical service-layer
  `NopException` checks (validator for UX, service for every caller, this repo's established
  double-enforcement pattern).
- **`Admin/Factories/ProductionLabelsAdminModelFactory.cs`** — search/list/model preparation for both
  admin surfaces, sharing one factory.
- **`Admin/Controllers/ProductionLabelsAdminController.cs`** — `List` (GET page + POST JSON grid, shared
  by both surfaces), `ProductionBatchCreatePopup` (GET/POST), `ProductionBatchDelete` (catches
  `NopException` → `ErrorNotification`, mirrors `IngredientsAdminController`'s composition-delete
  pattern), `GenerateLabelPopup` (GET, options) + `GenerateLabel` (POST: prepare model → render partial
  to HTML string via `RenderPartialViewToStringAsync` → `IHtmlToPdfConverter.ConvertAsync` → **only on
  success** `MarkLabelGeneratedAsync` → `File(bytes, MimeTypes.ApplicationPdf, fileName)`, mirroring
  `OrderController.PdfInvoice`'s file-return shape), `SaveProductInfo` (POST, per-locale `GenericAttribute`
  writes for storage/origin). Every `View(...)` call uses the explicit
  `~/Plugins/Misc.ProductionLabels/Admin/Views/...` path.
- **Views** — `_ViewImports.cshtml`, `_ViewStart.cshtml`, `List.cshtml` (DataTables grid + row actions),
  `ProductionBatchCreatePopup.cshtml`, `GenerateLabelPopup.cshtml` (size + language picker, submits to a
  file download — the standard nopCommerce popup-window pattern, same shape as
  `ServingSuggestionStepCreatePopup.cshtml`), `Components/ProductionLabels.cshtml` (product-tab grid +
  the two localized text inputs, with the `Model.ProductId > 0` / "save the product first" guard
  `admin-ui-standards-check` requires), `ProductionLabelTemplate.cshtml` (**no existing mirror** — the
  actual label markup: standalone HTML document, CSS keyed off `SizeVariant`, storage/origin rendered
  with plain `@`-encoding, never `Html.Raw`).

### Solution file

`src/NopCommerce.sln` — new project entry, build-configuration block, and nested-project entry, mirroring
`Nop.Plugin.Misc.ServingSuggestions`'s three blocks with a new GUID, nested under the same flat "Plugins"
solution folder every existing plugin uses.

### Order of work

1. `.sln` + `.csproj` (incl. the Ingredients `ProjectReference`) + `plugin.json` + `logo.png`.
2. Domain → builder → migration.
3. `ProductionLabelsDefaults.cs`.
4. Permission manager.
5. `IProductionBatchService`/`ProductionBatchService`.
6. `IHtmlToPdfConverter` interface only (unblocks downstream signatures).
7. Label model → `IProductionLabelModelFactory`/`ProductionLabelModelFactory` (needs the Ingredients
   reference live).
8. `RouteProvider`, `NopStartup`.
9. Admin models → validators → mapper config → admin factory.
10. Admin controller.
11. Admin view component.
12. `ProductionLabelsPlugin.cs`.
13. Menu event consumer.
14. Views + `.csproj` content entries.
15. Tests, written alongside each layer above.
16. **Blocked, does not block anything else compiling**: concrete `IHtmlToPdfConverter` implementation,
    its package reference, any Dockerfile change.
17. **Non-code prerequisite, blocks correctness not compilation**: the `DisplayOrder` data-quality pass
    on every existing product (spec's own pre-ship blocking item).

### Tests

`ProductionBatchServiceTests` (insert/`BatchCode` format, delete-unlabeled succeeds, delete-labeled
throws, validation throws, newest-first ordering), `ProductionBatchValidatorTests`,
`ProductionLabelModelFactoryTests` (NUnit + Moq, not a `ServiceTest`/SQLite fixture — deliberately, since
the graceful-degrade scenario needs to simulate a `PostgresException` a real SQLite fixture can't
produce; covers every §11 scenario: normal, zero-ingredients, composite expansion, within-composite vs.
root ordering as distinct assertions, per-node allergen correctness, blank storage/origin, HTML-injection
literal-text rendering, legitimate depth-3 renders fully, only real truncation throws, explicit
non-default language for both ingredients and `Product.Name`, per-language storage/origin, graceful
degrade returns empty + logs), `ProductionLabelsAdminControllerTests` (delete-throws-notification path;
stamp-only-after-success ordering, injecting a fake `IHtmlToPdfConverter` — doesn't need the real PDF
gap resolved first), `ProductionLabelsPluginTests` (uninstall purges all three permissions and every
language's variant of both `GenericAttribute` keys), `ProductionLabelsMenuEventConsumerTests` (no sibling
precedent for this test — required unconditionally by `testing-standards-check`'s new-`IConsumer<T>`
gate). Deliberately no cache-consumer test (none exists) and no `EntityDeletedEvent<Product>`-consumer
test (none exists, by design).

### Gaps closed at Gate 2

- **Net quantity display format** — resolved: `Product.Weight` combined with the store's base weight
  measure unit via `IMeasureService` (e.g. "250 g"), not a bare decimal — a number with no unit on a real
  EU-1169-scoped label is a compliance gap, not a cosmetic detail.
- **`Product.Name` language** — resolved: same explicit end-to-end `languageId` treatment already
  decided for ingredient names: `GetLocalizedAsync(product, x => x.Name, languageId, ...)`, never the
  ambient working language. The approved design was explicit for ingredients but silent on `Product.Name`
  itself; same principle, no new decision.
- **Store's default language when `Store.DefaultLanguageId == 0`** — resolved: first language by
  `DisplayOrder` among active languages. An implementation-level algorithm choice, not a product
  decision.
- **Product-picker UI (standalone section's create-batch flow) and the "size+language→download" flow** —
  resolved: both reuse nopCommerce's own standard admin popup-window pattern (the same shape already
  used for e.g. cross-sell/related-product association, and for `ServingSuggestionStepCreatePopup.cshtml`)
  — not a new UI mechanism.

### Still open, not resolved here

- **Concrete `IHtmlToPdfConverter` implementation** — per spec §13, pending a real build-and-render
  smoke test against the Alpine runtime image. Nothing in this plan depends on it to compile.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-04
**Revision notes:** none — approved as proposed, with the four gaps above resolved inline rather than
sent back for a second implementation-planner pass.
