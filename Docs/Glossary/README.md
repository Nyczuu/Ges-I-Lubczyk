# Glossary

The canonical term for each domain concept. **Check here before naming anything new** — a class, a
column, a locale resource key, a setting. Two names for one concept is a bug that gets more expensive the
longer it lives.

Two vocabularies meet in this repo, and they are not the same thing:

## nopCommerce terms

Framework concepts. Defined by the platform, not by us — do not redefine them here, link to the
knowledge-base file that covers them and note only where our usage is narrower than the platform's.

| Term | Means | Reference |
|---|---|---|
| `Product` | a catalogue item; the entity everything else hangs off | [`knowledge-base/04`](../knowledge-base/04-extending-core-entities.md) |
| `ProductTag` | flat, facetable label rendered as a storefront filter | [`ai-harness/05`](../ai-harness/05-domain-gastronomy-guidelines.md) |
| `SpecificationAttribute` | structured, filterable product specification | [`ai-harness/05`](../ai-harness/05-domain-gastronomy-guidelines.md) |
| `ProductAttribute` / `ProductAttributeCombination` | customer-selectable variant affecting price/stock | [`ai-harness/05`](../ai-harness/05-domain-gastronomy-guidelines.md) |
| `GenericAttribute` | schema-free key/value metadata on any entity | [`knowledge-base/04`](../knowledge-base/04-extending-core-entities.md) |
| `SystemName` | a plugin's stable identity; changing it loses that plugin's stored configuration | [`knowledge-base/05`](../knowledge-base/05-plugin-system.md) |
| Widget zone | named injection point in a view, targeted by `IWidgetPlugin` | [`knowledge-base/09`](../knowledge-base/09-theming-and-design.md) |

## Domain terms (gastronomy)

Ours. Define them here, precisely, and use exactly these words in code, locale keys, and admin UI.

| Term | Means | Maps onto |
|---|---|---|
| _(empty — add as the domain is modelled)_ | | |

Candidates the domain guidelines already raise, to be defined when first implemented: batch/lot,
best-before date, shelf life, allergen, dietary tag, net vs drained weight, cold chain.

Rules for an entry: one sentence stating what it **is**, one stating what it is **not** (the near-miss
concept it gets confused with), and where it lives in the model. If a term maps onto an existing
nopCommerce mechanism, say which — that is the decision
[`ai-harness/05-domain-gastronomy-guidelines.md`](../ai-harness/05-domain-gastronomy-guidelines.md)
exists to make, and repeating it in prose here without the mapping is how the two drift apart.
