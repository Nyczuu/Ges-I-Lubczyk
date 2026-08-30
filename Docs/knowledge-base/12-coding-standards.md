# Coding Standards (condensed)

Source: adapted from `developer/tutorials/coding-standards.html`. Enforced in this repo by
`.editorconfig` at the root — when in doubt, defer to what `.editorconfig` says over this summary,
since it's the machine-checked source of truth and may have drifted from the docs page.

## Naming

| Element | Convention | Example |
|---|---|---|
| Interface | `I` + PascalCase | `IBatchTrackingService` |
| Class / struct / enum / method / property / namespace | PascalCase | `BatchTrackingService` |
| Public field | PascalCase | rare — prefer properties |
| Private/protected field | `_camelCase` | `private readonly IRepository<T> _repository;` |
| Local variable / parameter | camelCase | `productId` |
| Constant | `SCREAMING_SNAKE_CASE` | `const int MAX_BATCH_LENGTH = 40;` |
| Generic type parameter | `T` + descriptive PascalCase (or bare `T`/`TInput`/`TOutput` when unambiguous) | `TEntity`, `TSession` |

## Style highlights an AI assistant should default to

- **Allman brace style** — opening brace on its own line, always (`if`, `else`, `try`/`catch`/`finally`,
  method bodies).
- `var` **preferred everywhere**, including built-in types (`var x = 5;`, not `int x = 5;`).
- No `this.` qualifier on fields/properties/methods/events.
- Use the C# keyword alias, not the BCL type name (`int`, not `Int32`; `string`, not `String`).
- Block bodies for methods/constructors/operators (`{ return x; }`), but **expression bodies for
  simple properties/indexers/lambdas** (`public int Age => _age;`).
- Pattern matching over cast-then-check (`if (o is int i)`, not `if (o is int) { var i = (int)o; }`).
- `?.` and `??` over explicit null checks; throw-expressions (`x ?? throw new ArgumentNullException(...)`)
  over throw-statements.
- Object/collection initializers over post-construction property assignment.
- `using` directives: `System.*` first, alphabetical, no blank line separating groups.

## What NOT to do (common AI-generated anti-patterns for this codebase specifically)

- Don't add `[Required]`/`[StringLength]` DataAnnotations to view models — FluentValidation only
  (see [08-settings-permissions-validation.md](08-settings-permissions-validation.md)).
- Don't add navigation properties to a domain entity.
- Don't wrap a synchronous method with `.Result`/`.Wait()` to avoid making a call chain `async` —
  the entire service stack is `async`/`await` end-to-end; propagate it.
