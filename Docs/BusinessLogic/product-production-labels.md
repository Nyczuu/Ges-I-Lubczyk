# Production batches and printable product labels

Production staff can log a history of production runs per product and generate a printable PDF label
for a chosen product+batch. Shipped by [GIL-005](../Specs/GIL-005-production-labels/spec.md) as
`Nop.Plugin.Misc.ProductionLabels` — a self-contained plugin, no core change, with a runtime read-only
dependency on `Nop.Plugin.Misc.Ingredients`'s public services. See the
[Glossary](../Glossary/shop.md#what-we-sell) for the term "Production batch".

## What a production batch is

`ProductionBatch` is an **immutable** record of one production run of a product — `BatchCode`
(system-generated, `{ProductionDateUtc:yyyyMMdd}-{counter:D3}`, e.g. `20260903-001`), `ProductionDateUtc`,
`BestBeforeDateUtc`, `Quantity`, and `LabelGeneratedOnUtc` (nullable — set the first time a label is
generated from this row, never before).

Rows are never edited or overwritten. A mistake made before anything reaches a printer is corrected by
**deleting** the row and creating a new one — not by editing it in place — so the history that remains is
a true audit trail, not a record that could have been silently changed after the fact. Once a label has
been generated from a batch, that batch is **locked**: it can no longer be deleted either. Deleting a row
a real label was printed from would break the paper trail the whole feature exists for.

`BatchCode`'s counter is `1 + MAX` of the existing numeric suffix already used for
`(ProductId, ProductionDateUtc.Date)`, deliberately not `COUNT` — since unlabeled batches are deletable,
`COUNT` would reissue an already-used code whenever a middle batch of the day is deleted and a new one
added afterward.

`ProductionBatch` rows are **retained** when their product is soft-deleted — a recall/traceability record
should outlive a discontinued product, unlike `ServingSuggestion` (presentation content, correctly
cascade-deleted on product deletion; see
[product-serving-suggestions.md](product-serving-suggestions.md)). No `Product`-deletion event consumer
exists here — that absence is deliberate, not an oversight.

## The label

"Generate label" renders one of two preset size layouts (small jar / large jar — same content, geometry
only differs) for one product+batch, as a downloadable PDF. The HTML-to-PDF rendering library (spec §13)
is **PuppeteerSharp**, confirmed by a real build-and-render smoke test against this store's Alpine-based
Docker image: `PuppeteerSharpHtmlToPdfConverter` launches headless Chromium (the `chromium` apk package in
the runtime image — PuppeteerSharp's own bundled downloader fetches a glibc build that does not run on
musl/Alpine) and renders the label template through it. The template drives the actual PDF page size
itself, via a `@page { size: ...; margin: 0; }` rule matched to the chosen size variant — not a
converter-API parameter — rendered with PuppeteerSharp's `PreferCSSPageSize`, since its own default would
otherwise ignore the template and print onto a fixed Letter-sized page. Content, field by field:

| Field | Source |
|---|---|
| Product name | `Product.Name`, localized to the label's chosen language |
| Ingredients, descending weight order | `Nop.Plugin.Misc.Ingredients`'s public `IIngredientService`/`IProductIngredientService`, walking directly-attached ingredients and their nested composite children |
| Allergens, emphasized | `Ingredient.Allergen`, read off the same entities the ingredient walk already returns |
| Net quantity, with its unit | `Product.Weight` combined with the store's base weight measure via `IMeasureService` |
| Best-before date, batch code | this batch |
| Food business operator name/address | `Store.CompanyName`/`CompanyAddress`/`CompanyPhoneNumber` |
| Storage conditions, country of origin | per-product, **per-language** `GenericAttribute` values, set once per product on its admin tab |

This is not a claim of full EU Regulation 1169/2011 compliance — the same posture GIL-001 already takes
for allergen classification (see [product-ingredients.md](product-ingredients.md)) — it defines what data
feeds the label and where it comes from, not a legal sign-off.

### Ingredient ordering now carries a second meaning

The label's descending-by-weight ingredient order reuses the existing per-product
`ProductIngredientMapping.DisplayOrder` **and** per-composite `IngredientComposition.DisplayOrder` —
both originally storefront-display-only fields, now also legally relevant to a printed label. Nothing
enforced descending-weight-order semantics on either field before this shipped. **A one-time,
non-code prerequisite**: every existing product's ingredient ordering, and every composite ingredient's
internal ordering, needs a data-quality pass confirming it is actually in descending-weight order before
the label content can be trusted. This is a data-audit task against the real database, not a code change,
and is not something this plugin's tests can verify.

### Composite ingredients are expanded inline, with a hard limit on truncation

A directly-attached ingredient that is itself composite renders expanded with its nested children (the
same "fully expanded, EU-label style" presentation the storefront widget uses — see
[product-ingredients.md](product-ingredients.md)). GIL-001 caps ingredient composition nesting at 3
levels (`IngredientsDefaults.MaxCompositionDepth`); the storefront widget treats reaching that cap as
cosmetic and silently truncates. **The label does not**: if a node at the depth boundary still has
recorded child edges — real truncation, not merely a complete depth-3 composite, which is normal and
renders in full — "Generate label" throws rather than producing an incomplete ingredient declaration.
This should be unreachable for legitimately-entered data, since GIL-001's own write-time validation
(`IngredientCompositionService.ValidateNewEdgeAsync`) never allows a composition to actually exceed the
cap — the throw exists as defense in depth, not because it is expected to fire.

### Ingredients plugin can be uninstalled without breaking label generation

`ProductionLabels` has a runtime dependency on `Nop.Plugin.Misc.Ingredients`'s public services — the
first such cross-plugin dependency in this codebase. If Ingredients is ever uninstalled while
ProductionLabels stays installed, its tables are gone but its DI registrations are not (registration
isn't conditional on install state), so a naive read would surface as an unhandled SQL error. Instead,
the ingredient-read path catches PostgreSQL's `undefined_table` condition (SQLSTATE `42P01`) specifically
— narrow enough that a genuine connection failure or an unrelated query bug still surfaces normally — and
degrades to the same empty-ingredients rendering a product with zero mapped ingredients already gets,
logging a warning so the cause is discoverable later.

### Runtime image requirement

The repo-root `Dockerfile`'s runtime stage installs the `chromium` and `ttf-freefont` apk packages and
sets `PRODUCTIONLABELS_CHROMIUM_EXECUTABLE_PATH=/usr/bin/chromium-browser`, which
`PuppeteerSharpHtmlToPdfConverter` reads to launch that system binary directly instead of attempting its
own (incompatible, on musl/Alpine) download. On a developer machine, where that environment variable is
unset, PuppeteerSharp downloads a compatible Chromium build itself on first use. The browser process is
launched once per running instance (cached in a static field — see the converter's own remarks) and
reused across every label generation, since starting it costs roughly a second.

### Label language

A generated label renders in the store's default language, with an explicit override available at
generation time when the store has more than one language configured — never the ambient request culture
or the generating admin's own session language, since a label's language must match its product's actual
market, not an admin's transient UI state. This applies to the product name and ingredient data alike
(both localized via `ILocalizationService.GetLocalizedAsync` with the chosen language passed explicitly
end to end) and to storage conditions/country of origin, which are genuinely per-language admin input
(see below), not a single shared value translated on the fly.

## Storage conditions and country of origin are per-product, per-language admin input

These two fields are not fixed template text — they are set once per product, once per configured
language, on the same admin tab that shows that product's batch history. Persisted as `GenericAttribute`
values on `Product`, keyed `ProductionLabels.StorageConditions.{languageId}` /
`ProductionLabels.CountryOfOrigin.{languageId}` — not `LocalizedProperty`, which was considered and
rejected: `ILocalizedEntityService.SaveLocalizedValueAsync`'s write path requires a real reflected
property on the entity, and adding one to core `Product` was ruled out to keep this a zero-core-touch
change. A blank value for either field simply renders no line on the label; it does not block
generation, the same posture a product with no mapped ingredients already gets.

Uninstalling the plugin purges every configured language's `GenericAttribute` row for both keys — a
`GenericAttribute` row on a shared core table is not touched by this plugin's own migration `Down()`, so
leaving this step out would orphan the data the same way skipping `LocalizedProperty` cleanup would for
an `ILocalizedEntity`.
