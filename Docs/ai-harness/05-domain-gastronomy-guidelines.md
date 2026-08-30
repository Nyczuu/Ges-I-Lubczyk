# Domain Guidelines — Premium Jarred/Canned Gastronomy (B2C)

Conceptual mapping of the business domain (premium canned/jarred meals, individual B2C clients,
future expansion into confectionery) onto nopCommerce's entity model. Use this alongside
[02-extensibility-and-plugins.md](02-extensibility-and-plugins.md) and
[knowledge-base/04](../knowledge-base/04-extending-core-entities.md) when scoping a feature.

## Entity extension map

| Domain need | Core entity | Mechanism | Rationale |
|---|---|---|---|
| Batch/lot number | `Product` (or per-unit if tracked at `ProductAttributeCombination`/stock level) | **Schema migration** — new nullable column, queryable | Needed for recalls, traceability, admin filtering by batch; must be indexable and reportable |
| Expiration / best-before date | `Product`, and per-batch if batches expire independently | **Schema migration** on `Product` for a simple "shelf life days" model; a **new plugin-owned entity** (`Nop.Plugin.Misc.GastronomyCompliance.Domain.ProductBatch`) if batches are tracked as first-class records with their own expiry, quantity, and warehouse | A "products expiring within N days" admin report requires `WHERE`/`ORDER BY` on this — GenericAttribute is the wrong tool here |
| Dietary tags (vegan, gluten-free, allergen list, kosher, etc.) | `Product` | **`ProductTag`** (nopCommerce's existing product tag entity) for simple, facetable, storefront-filterable tags — this is already exactly what `ProductTag` is for, don't reinvent it as a custom table | Reuse the built-in mechanism; tags are already indexed, searchable, and rendered as storefront facets |
| Allergen structured data (severity, cross-contamination warnings) | New entity | **New plugin-owned entity** (`ProductAllergen`) if it needs structure beyond a tag (e.g. severity level, "may contain traces of") | `ProductTag` is a flat label; use it for simple yes/no facets, a dedicated table for anything with sub-fields |
| Net weight / drained weight (common for jarred/canned goods) | `Product` | **`GenericAttribute`** if display-only; **schema migration** if used in shipping weight calculation or filterable | Existing `Product.Weight` may already suffice for shipping — check before adding a duplicate field |
| Ingredient list / ingredients-of-concern | `Product` | **`GenericAttribute`** (long text, rarely queried) or a rich-text field on the product's specification attributes if already using `SpecificationAttribute` | Prefer nopCommerce's existing `SpecificationAttribute`/`SpecificationAttributeOption` system for structured, filterable spec data before inventing a new column |
| Packaging fragility / temperature control requirement | `Product` (flag) + carrier logic | **`GenericAttribute`** flag on `Product`; consumed inside a custom `IShippingRateComputationMethod.GetShippingOptionsAsync`/`GetFixedRateAsync` to apply a surcharge or restrict certain shipping methods | This is shipping business logic, not a data-modeling problem — belongs in a shipping plugin, not a core change |
| Confectionery expansion (future category) | New `Category`/`ProductAttribute` combinations | No engine change needed | nopCommerce's category/manufacturer/attribute system already supports multiple product lines under one catalog — confectionery is new *catalog data*, not a new entity type |

## Recommended plugin(s) for this domain

Rather than scattering gastronomy-specific fields across ad-hoc migrations, group them into one or two
purpose-built plugins so they version, install, and (eventually) uninstall as a coherent unit:

- **`Nop.Plugin.Misc.GastronomyCompliance`** — batch/lot tracking, expiration dates, allergen
  structured data, "expiring soon" admin report, and the scheduled task that flags near-expiry stock
  (`IScheduleTask`, see [knowledge-base/07](../knowledge-base/07-events-and-scheduled-tasks.md); recall
  from [04-deployment-aws-ecs.md](04-deployment-aws-ecs.md) that this task must be idempotent/safe to
  run redundantly once ECS scales past one task).
- **`Nop.Plugin.Shipping.TemperatureControlled`** (or extend an existing carrier plugin) — implements
  `IShippingRateComputationMethod` to apply packaging/fragility/cold-chain surcharges and restrict
  incompatible shipping methods for jarred/canned goods.

## What to reuse before building new

Before adding any new column or entity, check whether the need is already covered by:

- `ProductTag` — simple facetable labels (dietary tags, "premium", "small-batch").
- `SpecificationAttribute`/`SpecificationAttributeOption` — structured, filterable product
  specifications (ingredients, weight class, jar size) that should appear in storefront comparison/
  filter UI.
- `ProductAttribute`/`ProductAttributeCombination` — customer-selectable variants (jar size, spice
  level) that affect price/stock, as opposed to fixed product metadata.
- `Manufacturer` — if "premium gastronomy" vs. future "confectionery" line should be modeled as
  distinct manufacturers/brands rather than categories, depending on how the business actually wants
  it merchandised (a product decision to confirm with the business owner, not purely technical).

Reserve new domain entities and migrations for data that genuinely doesn't fit these existing,
already-indexed, already-admin-UI-integrated mechanisms — most "add a jar-goods field" asks resolve to
`ProductTag` or `SpecificationAttribute` rather than a new table.
