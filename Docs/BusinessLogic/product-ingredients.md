# Product ingredients

Every product can carry a list of ingredients, shown on its product page. Shipped by
[GIL-001](../Specs/GIL-001-product-ingredients/spec.md) as `Nop.Plugin.Misc.Ingredients` — a self-
contained plugin, no core change. See the [Glossary](../Glossary/shop.md#what-we-sell) for the terms
"Ingredient" and "Composite ingredient".

## What an ingredient is

An ingredient (`Ingredient`) is either:

- **Simple** — salt, water, onion. No children.
- **Composite** — made of other ingredients, to a bounded depth (below). Beef broth is bones, water,
  carrot, celery, salt; onion soup lists beef broth as one ingredient rather than re-listing its
  composition.

An ingredient is defined once in a shared catalogue and attached to any number of products
(`ProductIngredientMapping`), independent of any one product's ingredient list. Editing beef broth's
composition changes what every product that uses it (directly or through nesting) displays — there is no
per-product copy of an ingredient's definition.

Composition is a directed acyclic graph, not a list: the same ingredient (e.g. salt) can be reached from
one composite ingredient through more than one path. The direct edges (`IngredientComposition`,
parent → child) are the source of truth; `IngredientClosure` is a derived table holding every ancestor →
descendant pair reachable at any depth, kept correct by recomputing it from scratch inside the same
transaction as every composition write (see "Closure maintenance" below) — it is never independently
edited and never cached.

## Maximum nesting depth

A composite ingredient may nest **at most 3 ingredient-to-ingredient edges deep**. This counts only
`IngredientComposition` edges between ingredients — the hop from a product to its directly-attached
ingredients does not count towards the limit. A product can attach a composite ingredient that is itself
3 levels deep; that is the intended shape, not an edge case.

The limit is enforced by **longest**, not shortest, realizable path: because the graph allows the same
ingredient to be reachable through more than one chain (e.g. "salt" reachable from "onion soup" both
directly and via "beef broth"), judging depth by the shortest path would let a genuinely-too-deep chain
through as long as some shorter alternate path also existed. Adding a candidate edge `(parent, child)` is
rejected if `(longest known depth from any ancestor of parent to parent) + 1 + (longest known depth from
child to any descendant of child)` exceeds 3.

This is a **developer-set ceiling**, not a business rule with a documented external source — real recipes
are expected to need 2 levels; 3 is deliberately generous headroom, re-checked at storefront render time
as a defensive cut-off against bad data rather than as a second business rule.

## Cycle prevention

Adding an edge `(parent, child)` is rejected if `child` is already an ancestor of `parent` — i.e. a row
`(child, parent, *)` already exists in the closure. Self-loops (`parent == child`) are rejected
unconditionally.

The validate-then-write is wrapped in a single serializable database transaction
(`INopDataProvider.CreateTransactionScope()`, `IsolationLevel.Serializable`), not a best-effort
read-then-write: two admins each adding one edge of what would jointly become a cycle cannot both commit,
because the losing transaction gets a serialization-failure abort rather than silently completing after
the other one changed the closure it validated against. The losing admin sees a generic "someone else
changed this at the same time, please try again" error — there is no automatic retry.

**Why the concurrent case isn't exercised by an automated test.** The test suite runs against SQLite
(`SqLiteNopDataProvider`), which does not provide true serializable-isolation semantics the way
PostgreSQL's Serializable level does, so a `Task.WhenAll`-based test racing two transactions against the
test double could not demonstrate the abort this section describes — at best it would prove something
about SQLite's own locking, not about this business rule. This is also not specific to the cycle scenario:
because the closure is fully deleted and rebuilt on every write (see "Closure maintenance" below), *any*
two concurrent composition writes touch the same rows and would conflict under Serializable isolation, not
only two writes that would jointly create a cycle. The cycle-prevention rule itself is covered by a
sequential test (add one edge, then attempt the edge that would close the cycle) — that proves the
validation logic; the transactional concurrency guarantee is architectural
(`IsolationLevel.Serializable` via `INopDataProvider.CreateTransactionScope()`) and does not need
re-proving per business rule.

## Allergen classification

Each ingredient carries **at most one** allergen classification, from the 14 EU Regulation 1169/2011
Annex II allergens (`AllergenType`: cereals containing gluten, crustaceans, eggs, fish, peanuts, soybeans,
milk, nuts, celery, mustard, sesame seeds, sulphur dioxide and sulphites, lupin, molluscs), plus `None`.
It is a real enum column (`Ingredient.AllergenId`), not free text or a flag — so a future "does this
product contain celery" query is a straightforward filter against existing data, not a text search.

An ingredient that genuinely carries more than one EU allergen (soy sauce: soybeans *and* wheat) is
modeled as a 2-level composite (soy sauce → soybean extract, wheat) rather than as multiple allergen tags
on one row. This is the same composition mechanism used for any other nested ingredient — there is no
second, allergen-specific relation.

Allergen severity, cross-contamination warnings ("may contain traces of"), quantities/percentages, full
EU-labelling compliance, ingredient import/export, and storefront filtering by ingredient or allergen are
explicitly **out of scope for v1** — anticipated future work the data model does not block, not a
rejected feature. A future allergen/diet filter reads the same always-current `AllergenId` column and
closure directly; it does not require a data model rework.

**Descending-by-weight ordering is no longer out of scope** as of
[GIL-005](../Specs/GIL-005-production-labels/spec.md): the existing per-product `ProductIngredientMapping.DisplayOrder`
and per-composite `IngredientComposition.DisplayOrder` fields were repurposed to carry a second,
legally-relevant meaning — descending order by weight, for GIL-005's printed product label — on top of
their original storefront-display purpose. Nothing enforced that second meaning before GIL-005; a
pre-ship data-quality pass confirming every existing product's and composite ingredient's `DisplayOrder`
is actually in descending-weight order is that ticket's own responsibility, not this one's, but any
future change to either field's semantics must account for both purposes now, not just storefront
display.

## Nutritional values

Shipped by [GIL-004](../Specs/GIL-004-ingredient-nutritional-values/spec.md). Every ingredient carries
four numeric facts, each expressed **per 100g** — the standard basis on Polish food labels — as real,
non-nullable schema columns (`Ingredient.CaloriesPer100g`, `.ProteinPer100g`, `.FatPer100g`,
`.CarbohydratePer100g`), not `GenericAttribute` or free text: these are structured facts a future
recipe-level calorie/nutrition table is meant to sum, not a descriptive note.

All four are **required**, not optional. An admin cannot save an ingredient (create or edit) without
providing every one — an ingredient genuinely at zero (water, salt) is entered as `0`, not left blank.
`0` had to remain a legal, meaningful value here rather than double as an "unknown" sentinel, since a
future recipe-aggregation feature summing these values across a composition cannot tell a real zero from
a missing one if both look the same on the row. Negative values are rejected by admin-form validation;
"required" itself is enforced structurally (a non-nullable `decimal` model property fails ASP.NET Core
model binding on a blank submission before any validator rule runs), not by a `NotEmpty`/`NotNull`
validation rule, which would have wrongly rejected a genuine `0`.

Ingredient rows that existed before GIL-004 shipped have no real value for these four columns. The
migration that adds them backfills every existing row to `0` as part of the same `ALTER TABLE ... NOT
NULL DEFAULT 0` statement, so the constraint is satisfiable immediately with no separate backfill pass and
no window where it is unsatisfied. That `0` is a one-time technical default, not a claim that those
ingredients are actually zero-calorie — an admin corrects them to real values afterward like any other
backfilled field.

Admin-only in this task: the Ingredient Create/Edit form collects and displays all four values; there is
no ingredient-list grid column and no storefront or recipe-level surface yet. Computing a whole
product/recipe's nutrition from the ingredients it is composed of is out of scope here —
`IngredientComposition`/`ProductIngredientMapping` carry no quantity/weight field today, so there is
nothing yet to multiply a per-100g value by; that aggregation is a separate future task once quantity
tracking exists.

## Deletion is blocked while in use

Deleting an ingredient is rejected — no cascade — while it is still:

- a component of another (composite) ingredient (`IngredientComposition.ChildIngredientId`), or
- attached to any product (`ProductIngredientMapping.IngredientId`).

The error names what still uses it (the composite ingredients, the products, or both), so an admin does
not have to hunt for the blocker. Deleting an ingredient with no remaining references removes its own
outgoing composition edges (it cannot have incoming ones, by the rule above) and recomputes the closure
in the same transaction.

## Closure maintenance

`IngredientClosure` is rebuilt **from scratch** on every composition write (add edge, remove edge, delete
ingredient) rather than maintained incrementally: delete every closure row, reseed a reflexive `(X, X, 0)`
row for every ingredient, then run a fixed-point join against the current `IngredientComposition` edges,
bounded to 3 rounds (the depth cap itself bounds how many rounds could ever produce a new row), keeping
`Depth = max(existing, candidate)` per ancestor/descendant pair.

This is a deliberate simplification over incremental delta maintenance of the closure (a substantially
harder problem for a DAG under deletion), accepted given the expected scale — tens to low hundreds of
ingredient rows for one store. The recompute is silent: it does not raise
`EntityInserted/Updated/DeletedEvent<IngredientClosure>` per row, because the closure is an internal
derived table with no admin editing surface and no cache of its own.

## Why the closure exists at all — caching decision

The composition and closure are **never cached**; the closure itself is the authoritative, always-current
answer, maintained transactionally at write time rather than computed and cached at read time. There is no
derived, cacheable read-time artifact whose staleness could show a wrong ingredient list, and no
dependency on cross-instance cache coherence for correctness — relevant given `Docs/ai-harness/`'s
multi-instance ECS deployment target. An optional in-process render cache could still be layered on later
purely for performance; any staleness there would be a cosmetic display lag, not an incorrect answer,
since it is never the source of truth for a future filter query.

## Storefront rendering

Rendered only on the product detail page (not listing or quick-view), via `IngredientsViewComponent` in
widget zone `productdetails_before_collateral`. It inherits whatever visibility the product page itself
already enforces — the ingredient list is not independently ACL'd or store-mapped.

The render reads the always-current closure with two focused queries, not a per-level recursive
traversal:

1. The ingredients directly attached to the product (`ProductIngredientMapping`).
2. Every `IngredientComposition` edge reachable from those root ingredients at any depth, via one
   `IngredientClosure`-scoped subquery.

The nested tree is then built in memory from those two result sets and rendered **fully expanded inline,
EU-label style** — e.g. "beef broth (bones, water, carrot, celery, salt)" — bounded to the same maximum
depth (3) as a defensive cut-off against bad data, not because a correctly-written composition could ever
exceed it.

## Localization

`Ingredient` is `ILocalizedEntity` — the first plugin-owned entity in this codebase to be. Its `Name` and
`Description` are localized the same way `Category.Name` is, through `LocalizedProperty` rows keyed by
`LocaleKeyGroup = "Ingredient"`. Because `LocaleKeyGroup` is the entity's **unqualified** type name, this
plugin shares that key space with any core type that might ever be named `Ingredient` — see the
[Glossary](../Glossary/shop.md#localized-property) entry on `LocalizedProperty` for why that matters.

Uninstalling the plugin drops its own tables automatically (the framework runs the migration's `Down()`
right after `UninstallAsync()`), but a shared core table like `LocalizedProperty` is not touched by that
table drop. `UninstallAsync()` therefore explicitly bulk-deletes `LocalizedProperty` rows where
`LocaleKeyGroup == "Ingredient"`, so no orphaned translations survive the plugin's own tables being gone —
the one cleanup step this plugin needs that most schema-owning plugins do not.

## Future direction, not built now

- **Store mapping.** A single store exists today, and the ingredient catalogue is not store-mapped.
  When additional stores are introduced, ingredient catalogues are expected to become store-specific — an
  additive migration adding `IStoreMappingSupported` to `Ingredient`, not a rework.
- **`IngredientCategory`.** No dairy/meat/plant-origin grouping table in v1 — deferred until a concrete
  filtering feature needs it.
