---
name: entity-extension-check
description: >-
  Load this before adding any new field or data to an existing core entity (Product, Order, Customer,
  Category, ...). It forces the decision rule this repo requires — reuse an existing mechanism, or
  GenericAttribute, or a schema migration — and makes you state which and why. Use it BEFORE writing
  code: picking the wrong one is cheap to write and expensive to undo, because the wrong choice only
  hurts later, when a report needs to filter on a field that was stored as a schema-free blob.
---

# Entity Extension Check

Full docs: [`Docs/knowledge-base/04-extending-core-entities.md`](../../../Docs/knowledge-base/04-extending-core-entities.md)
and, for this project's domain, [`Docs/ai-harness/05-domain-gastronomy-guidelines.md`](../../../Docs/ai-harness/05-domain-gastronomy-guidelines.md).
This is the checklist form.

Rule 8 of [`Docs/ai-harness/00-system-instructions.md`](../../../Docs/ai-harness/00-system-instructions.md)
requires the choice to be **stated explicitly**, not made silently.

## Step 1 — does an existing mechanism already cover this?

Most "add a field to Product" asks resolve here. Check in this order:

- [ ] **`ProductTag`** — a flat, facetable label the storefront filters on (dietary tags, "small-batch").
      Already indexed, searchable, and rendered as a facet. Do not reinvent it as a custom table.
- [ ] **`SpecificationAttribute` / `SpecificationAttributeOption`** — structured, filterable product
      specifications that belong in comparison and filter UI (jar size, weight class, ingredients).
- [ ] **`ProductAttribute` / `ProductAttributeCombination`** — a customer-selectable variant that affects
      price or stock, as opposed to fixed metadata.
- [ ] **An existing core field** — e.g. `Product.Weight` may already cover a shipping-weight need before
      you add a second weight column.
- [ ] **`Category` / `Manufacturer`** — a new product line is usually catalogue data, not a new entity.

If one fits, use it. Note in the spec which and why the alternatives were rejected.

## Step 2 — GenericAttribute or schema migration?

| Choose | When |
|---|---|
| **`GenericAttribute`** | Free-form or rarely-queried metadata: notes, display-only values, a flag nothing filters on. Zero schema change, zero core touch. Via `IGenericAttributeService.SaveAttributeAsync` / `GetAttributeAsync<T>`. |
| **Schema migration** | The value must be filtered, sorted, joined, or reported on in SQL. Add the property to the domain class, then `this.AddOrAlterColumnFor<TEntity>(x => x.Prop).Nullable()` in a `[NopSchemaMigration]`. |

The decisive question is not "is this important data" but **"will something need a `WHERE` or `ORDER BY`
on it?"** An "expiring within N days" admin report answers yes; a display-only ingredient list answers no.

- [ ] The choice is written down in the spec, with the reason.

## Step 3 — is this a plugin-owned entity instead?

If the data has its own structure — sub-fields, its own lifecycle, many rows per product — it is not a
field on `Product`, it is a **plugin-owned entity** (`ProductBatch` with its own expiry, quantity, and
warehouse; `ProductAllergen` with severity). A flat tag cannot express sub-fields; do not stretch one.

## Step 4 — if it touches core

A schema migration on a core entity means editing a class under `src/Libraries/Nop.Core/Domain/`.

- [ ] The property is **additive and nullable**. This is the one sanctioned core modification (rule 3);
      anything more than that needs explicit human confirmation before you write it.
- [ ] Everything else stays outside core: the migration, the view model, the validator, the view, and
      the controller mapping all live in the plugin.

## Step 5 — the admin round trip

Adding the column is not the feature. For either mechanism, the change also needs:

- [ ] Property on the view model with `[NopResourceDisplayName("...")]`.
- [ ] A FluentValidation rule in the matching `BaseNopValidator<TModel>` — including a length rule for a
      bounded string, since Postgres does not enforce it (see `data-access-standards-check`).
- [ ] A `_CreateOrUpdate.*` partial view section.
- [ ] Explicit `Model → Entity` / `Entity → Model` mapping wherever AutoMapper's 1:1 convention does not
      already cover it.
- [ ] Locale resources for the label and its `.Hint`, added on install and removed on uninstall.

## Before calling this done

- [ ] The mechanism choice is stated with a reason, and the rejected alternatives named.
- [ ] Nothing was added to core beyond an additive nullable property (or confirmation was obtained).
- [ ] The full admin round trip exists, not just the storage.
