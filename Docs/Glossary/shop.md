# Glossary — shop

Canonical terms for this shop. This is a nopCommerce store, so its vocabulary **is** nopCommerce's
vocabulary — the entries below are the words where saying it loosely leads to building the wrong thing.

Not a type reference. See [README](README.md) for what earns an entry, and
[`../knowledge-base/14-product-relations-map.md`](../knowledge-base/14-product-relations-map.md) for what
each mechanism does and how deep it nests.

---

## Product relations

Five terms that all sound like "a product linked to a product" and mean five different things. Getting
these wrong already produced a wrong conclusion in a spec, so they lead this file.

### Grouped product

**Aliases to avoid:**
- bundle (a bundle is an `Associated product` — a different mechanism entirely)
- parent product, product family

**Definition:**
A product acting as a container for its variants. Children point at the parent through
`ParentGroupedProductId`, and the parent carries `ProductType.GroupedProduct`. One level only, and it
means **"variant of"**, never "made of".

**Defined in code:**
- `Product.ParentGroupedProductId`
- `ProductType.GroupedProduct`

**Example usage:**
"Jar size is not a grouped product — it changes price and stock, so it is a product attribute."

### Associated product

**Aliases to avoid:**
- related product (a different, marketing-only mechanism — see below)
- component

**Definition:**
nopCommerce's **bundling** mechanism: an attribute value points at another product, so selecting that
value adds the associated product to the cart. Driven by `AttributeValueType.AssociatedToProduct`. Not
recursive, and every associated product is itself a sellable product.

**Defined in code:**
- `ProductAttributeValue.AssociatedProductId`
- `AttributeValueType.AssociatedToProduct`

### Related product

**Aliases to avoid:** associated product, linked product

**Definition:**
Marketing only — "you may also like", shown on the product page. A flat pair with no meaning beyond
display: no composition, no requirement, no pricing effect.

**Defined in code:** `RelatedProduct`

### Cross-sell product

**Aliases to avoid:** related product (a separate list, shown somewhere else)

**Definition:**
Marketing only — suggestions shown in the cart rather than on the product page.

**Defined in code:** `CrossSellProduct`

### Required product

**Aliases to avoid:** dependent product, prerequisite

**Definition:**
"This product cannot be bought without that one", enforced at add-to-cart. Stored as a
**comma-separated string of ids**, not a typed relation — which is why it does not show up in a search
for product-id properties.

**Defined in code:**
- `Product.RequiredProductIds`
- `Product.RequireOtherProducts`

---

## Product data

### Product attribute

**Aliases to avoid:**
- attribute (bare — ambiguous between this, a specification attribute, and a generic attribute)
- option, variant field

**Definition:**
A **customer-selectable choice** that affects price or stock — jar size, spice level. If the customer
does not pick it, it is not a product attribute. `ProductAttributeCombination` is one selected
combination, with its own SKU and stock.

**Defined in code:**
- `ProductAttributeMapping`
- `ProductAttributeValue`
- `ProductAttributeCombination`

**Example usage:**
"The best-before date is not a product attribute — the customer does not choose it."

### Specification attribute

**Aliases to avoid:** specification (bare), product attribute

**Definition:**
Fixed, filterable product metadata shown in comparison and filter UI. Has exactly **one** grouping level
(`SpecificationAttributeGroup`) and **no attribute-to-attribute relation**, so it cannot express one
specification being composed of others.

**Defined in code:**
- `SpecificationAttribute`
- `SpecificationAttributeOption`
- `ProductSpecificationAttribute`
- `SpecificationAttributeGroup`

### Product tag

**Aliases to avoid:** label, keyword

**Definition:**
A flat, facetable label rendered as a storefront filter — "vegan", "small-batch". Already indexed and
searchable. Carries no sub-fields: a tag cannot hold a severity, a quantity, or a date.

**Defined in code:**
- `ProductTag`
- `ProductProductTagMapping`

### Filter level value

**Aliases to avoid:** category (a different, arbitrary-depth tree), filter

**Definition:**
A localized, product-mapped classification with **three fixed levels**, shipping with its own admin
controller, permissions and storefront search. The closest thing nopCommerce has to a localized taxonomy
deeper than one level. Was missed entirely when GIL-001 first surveyed existing mechanisms.

**Defined in code:**
- `FilterLevelValue`
- `FilterLevelValueProductMapping`

### Generic attribute

**Aliases to avoid:** custom field, metadata

**Definition:**
Schema-free key/value data attachable to **any** entity, with no schema change. Nothing can query across
it usefully, so it is the wrong home for anything that has to be filtered, sorted, or reported on.

**Defined in code:** `GenericAttribute`

---

## Localization — the pair worth reading twice

### Locale resource

**Aliases to avoid:** translation (ambiguous — it may mean a localized property instead)

**Definition:**
A **UI string**, keyed by name, added by a plugin in `InstallAsync` and removed in `UninstallAsync`.
Static text: labels, hints, messages. Not per-entity data.

**Defined in code:** `LocaleStringResource`

### Localized property

**Aliases to avoid:** locale resource (the confusion this pair exists to prevent)

**Definition:**
The translated value of **one property of one entity** — the Polish name of a specific record. Stored as
rows keyed by entity id, property name, and language, on entities marked `ILocalizedEntity`.

These are **data, not resources**. Removing a plugin's locale resources on uninstall does not touch
them, so a spec promising "locale resources are removed on uninstall" has said nothing about the entity
translations — an omission GIL-001 made. The `LocaleKeyGroup` is the **unqualified** type name, so a
plugin entity shares a global key space with any core type of the same name.

**Defined in code:**
- `LocalizedProperty`
- `ILocalizedEntity`

---

## Platform

### SystemName

**Aliases to avoid:** plugin name, plugin id

**Definition:**
A plugin's **permanent identity**. Settings, permission records, and installed state all hang off it.
Changing it after release orphans every one of those rows: the store silently loses the plugin's
configuration and the plugin reappears as new and unconfigured. Treat it like a primary key.

**Defined in code:** `PluginDescriptor.SystemName`

### Widget zone

**Aliases to avoid:** hook, injection point, slot

**Definition:**
A named point in a view where an `IWidgetPlugin` renders markup without the view being edited. The
sanctioned alternative to forking a layout. Zone constant names shift between nopCommerce minor
versions, so confirm one against the checked-out class before using it.

**Defined in code:** `PublicWidgetZones`

### Schedule task

**Aliases to avoid:** cron job, background job

**Definition:**
A recurring task registered as a **database row**, whose `Type` must be
`Namespace.ClassName, AssemblyName` — a mismatch fails silently at runtime, not at compile time. The
scheduler runs inside every application instance, so on multi-instance ECS a task fires redundantly
unless its own logic is idempotent.

**Defined in code:**
- `IScheduleTask`
- `ScheduleTask`

### Store

**Aliases to avoid:** site, tenant, shop (in this repo "the shop" means the whole thing, not one `Store`)

**Definition:**
One storefront within a single installation. nopCommerce supports several stores over one catalogue, so
entities marked `IStoreMappingSupported` can be limited to a subset. Anything cached or queried per
store carries the store in its key or its filter, or one store's data leaks into another.

**Defined in code:**
- `Store`
- `StoreMapping`
- `IStoreMappingSupported`

### Setting

**Aliases to avoid:** config, option

**Definition:**
A per-store configuration value an admin can edit. The strongly-typed form — a class implementing
`ISettings`, injected directly and saved through `ISettingService` — is the default; raw key/value access
is reserved for genuinely dynamic keys. Distinct from `appsettings.json`, which is infrastructure
configuration and not admin-editable.

**Defined in code:**
- `Setting`
- `ISettings`

---

## What we sell

The shop sells food, so a few words carry meaning nopCommerce does not know about. **Only terms a real
spec has put in play appear here** — the rest get added when a spec needs them, per
[README](README.md), not invented in advance.

### Ingredient

**Aliases to avoid:**
- component (means `Associated product` here — a sellable product)
- element, constituent

**Definition:**
A substance listed as part of what is in the jar. Either simple (salt, water, onion) or composite.
Defined once, in its own plugin-owned entity, and attached to any number of products — never a catalogue
`Product` (GIL-001 Q1: a `Product` row carries pricing, stock, and visibility fields an ingredient has no
use for). Carries a localized name and description, and a single allergen classification from the 14 EU
Regulation 1169/2011 Annex II allergens.

**Defined in code:**
- `Ingredient`
- `AllergenType`
- `ProductIngredientMapping`

**Example usage:**
"Beef broth is an ingredient of onion soup, and is itself composite."

### Composite ingredient

**Aliases to avoid:**
- recipe (a recipe is instructions; this is a composition)
- bill of materials (a manufacturing term — nopCommerce has no BOM mechanism, and borrowing the phrase
  invites the assumption that one exists)
- sub-ingredient (describes the child's role, not the parent's nature — the child of a composite
  ingredient is simply an ingredient)

**Definition:**
An ingredient itself made of ingredients, up to a hard ceiling of 3 nested ingredient-to-ingredient edges
(the product-to-ingredient attachment does not count towards that limit). Beef broth is bones, water,
carrot, celery, salt; onion soup lists beef broth as one ingredient. The direct edges are the composition;
the full reachable set at every depth is precomputed into a closure so a page render never needs a
recursive read. See [Business logic](../BusinessLogic/product-ingredients.md) for the depth rule, cycle
prevention, and closure maintenance.

The nesting is why no existing mechanism fits: every one of them stops at one level, or three fixed levels
for `Filter level value` — and even that one is three flat columns, not a real parent-child tree.

**Defined in code:**
- `IngredientComposition`
- `IngredientClosure`
