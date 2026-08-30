# Glossary

The canonical term for each domain concept, plus the **aliases to avoid** when a concept has accumulated
more than one name across specs, code, and docs. **Check here before naming anything new** — a class, a
column, a locale resource key, a setting. Two names for one concept is a bug that gets more expensive the
longer it lives.

One file: [`shop.md`](shop.md). **This is a nopCommerce store, so the shop's vocabulary is
nopCommerce's vocabulary** — there is no second domain layered on top of it. We sell food, so a few
words carry meaning the platform does not know about; those live in the same file, under *What we sell*.

## What belongs here, and what does not

This is **not** a type reference for nopCommerce. Every core type already carries accurate XML comments;
duplicating them here would restate upstream documentation and go stale on the next upgrade, with nothing
to detect the drift.

A term earns an entry when **naming it wrong is a real risk**:

- The word means something narrower in nopCommerce than in English — a "product attribute" is a purchase
  choice, not any property of a product.
- Several near-synonyms are distinct mechanisms — "associated", "related", "cross-sell", "required" and
  "grouped" products are five different things.
- It has already caused a mistake. Those entries say so, because that is the evidence the entry is
  earning its place.

A term does **not** earn an entry because it sounds like it belongs to our line of business. Entries are
grown from real specs, not invented in advance — the first draft of this glossary shipped nine
speculative food terms nobody had asked for, and they were cut.

For *what each mechanism does and how deep it nests*, the reverse index is
[`../knowledge-base/14-product-relations-map.md`](../knowledge-base/14-product-relations-map.md). The
glossary says what to **call** things; that map says what **exists**. Neither restates the other.

## Who writes it

Seeded from code, then grown incrementally as specs go through `refinement-check` — always **drafted
first and confirmed by a human** before being added, the same discipline as
[`../BusinessLogic/`](../BusinessLogic/README.md).

`refinement-verifier` is the **sole writer** of new or changed entries. `reviewer` only reads the
glossary, to flag naming drift in a diff; it never edits it.

## Entry template

Each field label sits on its own line, with content starting on the next line. A field with two or more
items becomes a `- ` bulleted list, one item per line.

```
### <Term>

**Aliases to avoid:**
<single line if the items need no explanation, otherwise:>
- <alias> (<why to avoid it, and what to say instead>)

**Definition:**
<what it means, referencing the real code identifiers involved>

**Defined in code:** <symbol name>
<or, for 2+ symbols:>
**Defined in code:**
- `ClassName`
- `PropertyName`

**Example usage:**
"<a sentence someone would actually write, using the canonical term>"
```

Notes:

- **Defined in code** names classes, properties, and enum members only, backtick-quoted — **no file
  paths, no line numbers.** Both go stale fast; a symbol name stays greppable and LSP-navigable without
  either, which is the entire reason for naming it here at all.
- **Aliases to avoid** and **Example usage** are optional. Omit the field rather than writing "None".
- A term not yet fully understood is added as a stub — `*(Stub — full definition to be written.)*` at the
  start of **Definition** — rather than skipped or guessed at. A stub is honest; a confident wrong
  definition is worse than silence.
- Name entries after the **generic mechanism**, not the product or spec that surfaced it.
- Avoid numeric descriptors ("three-step process") in definitions — use a bullet list, which survives the
  definition growing without needing a rewrite.
