# Product Relations Map

**Reverse lookup, not a type reference.** Every mechanism below is correctly documented by its own XML
comments in `src/`. What no per-type documentation answers is the question you actually start from:
*"what ways does nopCommerce already have for one product to point at another?"* This file answers that
one question, exhaustively, so a design decision does not rest on an enumeration that turns out to be
incomplete.

Written after a real miss: a spec concluded "there is no bill-of-materials mechanism" from a filename
search over `Domain/Catalog/`, and named two of the six relations below. See
[`Docs/Specs/GIL-001-product-ingredients/spec.md`](../Specs/GIL-001-product-ingredients/spec.md).

## Product → product

| # | Mechanism | Where | Cardinality / depth | What it means |
|---|---|---|---|---|
| 1 | `Product.ParentGroupedProductId` | `Domain/Catalog/Product.cs:23` | one level, parent → many children | **"variant of"**. Only meaningful with `ProductType.GroupedProduct` (`ProductType.cs:16`). Not composition. |
| 2 | `ProductAttributeValue.AssociatedProductId` | `Domain/Catalog/ProductAttributeValue.cs:24` | one level, via an attribute value | **nopCommerce's actual bundling mechanism.** Driven by `AttributeValueType.AssociatedToProduct` (`AttributeValueType.cs:16`), whose XML doc reads "used when configuring bundled products". Not recursive; makes every component a real product. |
| 3 | `RelatedProduct.ProductId1 / ProductId2` | `Domain/Catalog/RelatedProduct.cs:11,16` | flat pairs | Marketing: "you may also like". No semantics beyond display. |
| 4 | `CrossSellProduct.ProductId1 / ProductId2` | `Domain/Catalog/CrossSellProduct.cs:11,16` | flat pairs | Marketing: cart cross-sell. |
| 5 | `Product.RequiredProductIds` (+ `RequireOtherProducts`) | `Domain/Catalog/Product.cs:150,155` | flat list | **A comma-separated string of product ids**, not a typed relation. "Product X requires Product Y" — enforced at add-to-cart. |
| 6 | `CustomerRole.PurchasedWithProductId` | `Domain/Customers/CustomerRole.cs:57` | product → role | Not product-to-product, but a product reference that grants a customer role on purchase. Easy to miss when auditing what points at a product. |

**None of the six is recursive.** The deepest is one level. Anything needing arbitrary-depth composition
is new data, not a reuse of the above.

Note #5: a string column holding ids. It does not appear in a search for `public int *ProductId`, which
is why even a correct property-level search under-counts this list by one.

## Product → classification

| Mechanism | Where | Depth | Notes |
|---|---|---|---|
| `ProductCategory` → `Category.ParentCategoryId` | `Domain/Catalog/ProductCategory.cs:11`, `Category.cs:48` | arbitrary tree | The in-repo precedent for a self-referencing hierarchy. |
| `ProductManufacturer` | `Domain/Catalog/ProductManufacturer.cs:11` | flat | Brand/line. |
| `ProductProductTagMapping` → `ProductTag` | `Domain/Catalog/ProductProductTagMapping.cs:11` | flat | Facetable labels. No sub-fields. |
| `ProductSpecificationAttribute` → `SpecificationAttributeOption` → `SpecificationAttribute` → `SpecificationAttributeGroup` | `ProductSpecificationAttribute.cs:13,23`, `SpecificationAttribute.cs:13-23`, `SpecificationAttributeGroup.cs:8` | **exactly one grouping level** | Structured, filterable specs. No attribute-to-attribute relation. |
| `FilterLevelValueProductMapping` → `FilterLevelValue` | `Domain/FilterLevels/FilterLevelValueProductMapping.cs:11,16`, `FilterLevelValue.cs:9` | **fixed three levels** | Hierarchical, `ILocalizedEntity`, with its own admin controller, permissions (`StandardPermission.cs:67-69`) and storefront search. The closest shipped thing to a hierarchical, localized, product-mapped classification. |

## Product → variants and stock

`ProductAttributeMapping` → `ProductAttributeValue` → `ProductAttributeCombination`
(`ProductAttributeMapping.cs:13`, `ProductAttributeValue.cs:24`, `ProductAttributeCombination.cs:13`) —
customer-selectable choices that affect price and stock. An attribute is a *purchase decision*, not
product metadata; if the customer does not choose it, it does not belong here.

Also referencing a product, for completeness when auditing blast radius: `ProductPicture`,
`ProductVideo`, `Product3dObject`, `TierPrice`, `ProductWarehouseInventory`, `StockQuantityHistory`,
`BackInStockSubscription`, `ProductReview`, `OrderItem`, `ShoppingCartItem`, `PriceListItem`,
`BestsellersReportLine`.

## How to keep this honest

This file is a reverse index over code that changes. Regenerate the product-reference list with a
**property-level** search, never a filename or directory listing:

```bash
grep -rn "public int[?]* [A-Za-z]*ProductId" src/Libraries/Nop.Core/Domain --include="*.cs"
```

Then add back what a typed search cannot see — string-encoded id lists like `RequiredProductIds`, and
enum members like `AttributeValueType.AssociatedToProduct` that carry the semantics rather than the
reference. Both are in this map because both were missed the first time.
