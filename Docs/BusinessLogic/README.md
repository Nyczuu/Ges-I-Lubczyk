# Business Logic

What the mechanisms **we** built actually do — the rules, the corner cases, the reasons a decision went
one way. One file per mechanism, named after the mechanism (`product-batch-tracking.md`,
`cold-chain-shipping-surcharge.md`).

## What belongs here

- Rules a reader could not recover from the code without effort: precedence between two settings, what
  happens on the boundary of a date range, which state transitions are legal.
- Deliberate decisions and the reason behind them, especially where the obvious alternative was rejected.
- Corner cases already found the hard way.

## What does not

- **How nopCommerce works** — that is [`../knowledge-base/`](../knowledge-base/00-index.md).
- **What we plan to build** — that is [`../Specs/`](../Specs/README.md). A shipped spec is a historical
  record of what was asked for; this folder describes what exists now.
- **Term definitions** — that is [`../Glossary/`](../Glossary/README.md). Link to it rather than
  re-defining.

## The rule that matters

A file here **ships in the same commit as the code it describes** — never ahead of it. Documentation
that lands before its code describes something that does not exist yet, and nothing detects the drift
afterwards. This is a real, repeatedly-shipped bug in the harness this one is adapted from, which is why
it is a process constraint in [`../../AGENTS.md`](../../AGENTS.md) rather than a suggestion.

Same applies to changes: if a change alters documented behaviour, the doc update is part of that change.

## Index

- [`product-ingredients.md`](product-ingredients.md) — ingredient composition, depth limit, cycle
  prevention, allergen classification, deletion rules, storefront rendering (GIL-001).
