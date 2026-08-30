---
name: testing-standards-check
description: >-
  Load this when writing or reviewing tests, or when writing production code that requires a test —
  a service, an entity method, an event consumer, a migration. Use it alongside the code change, not
  as an afterthought: this repo treats a missing test for new or changed behaviour as a gap in the
  change itself, and the assertion library here is AwesomeAssertions, not FluentAssertions.
---

# Testing Standards Check

Full doc: [`Docs/knowledge-base/13-testing.md`](../../../Docs/knowledge-base/13-testing.md).
This is the checklist form.

## Stack (do not introduce alternatives)

- **NUnit 4.5.1** — `[TestFixture]`, `[Test]`, `[OneTimeSetUp]`, `[OneTimeTearDown]`.
- **Moq 4.20.72** — mocking.
- **AwesomeAssertions 9.4.0** — `using AwesomeAssertions;`, `.Should().Be(...)`.
- **`BaseNopTest`** exposes the container via `GetService<T>()`; service tests derive from **`ServiceTest`**,
  which adds core plugin/service registrations.
- **SQLite** (`SqLiteNopDataProvider`) backs data-touching tests; migrations run against it.

**Never add `FluentAssertions`.** It is a paid commercial licence from v8; `AwesomeAssertions` is the
free fork with an identical API, which means the wrong package compiles and passes every test. Also
banned: xUnit, MSTest, TUnit, NSubstitute.

## Placement

- [ ] Test lives in the project matching the layer under test: `Nop.Core.Tests`, `Nop.Data.Tests`,
      `Nop.Services.Tests`, `Nop.Web.Tests`.
- [ ] Service tests derive from `ServiceTest`, not `BaseNopTest` directly.

## Shape

- [ ] Arrange–Act–Assert, every test.
- [ ] No `if`/`else`/`switch` in a test — split into one `[Test]` per scenario.
- [ ] `[OneTimeSetUp]` vs `[SetUp]` chosen deliberately. Expensive container/service resolution belongs
      in `[OneTimeSetUp]`; per-test mutable state does not.
- [ ] **Every test that inserts data deletes it, and the delete happens *before* the assertions that
      might throw.** A failing assertion after an un-deleted insert leaks rows into shared test state
      and cascades failures into unrelated tests — which is how one broken test becomes twenty.

## Asserting that an async method throws

Two valid shapes, and the type argument differs between them:

- `Assert.Throws<AggregateException>(() => asyncCall().Wait())` — the dominant form here. `.Wait()`
  wraps whatever was thrown, so the type argument is `AggregateException`. Writing the original
  exception type with `.Wait()` fails at runtime.
- `Assert.ThrowsAsync<TException>(async () => await asyncCall())` — when the specific exception type is
  what you are asserting. Also used in this repo.

## Coverage gates (part of the change, not follow-up)

- [ ] **New or changed service method** → a test through `ServiceTest`.
- [ ] **New or changed entity method / domain rule** → a unit test.
- [ ] **New `IConsumer<T>`** → a test that the handler does the right thing for its event, including the
      null-payload path it guards.
- [ ] **New migration** → exercised by a test that uses the new schema through its service.
- [ ] **Bug fix** → a regression test that **demonstrably fails against the old code path** and passes
      against the new one. A round-trip test that would have passed either way does not prove the fix.
- [ ] **Changed persistence shape** → both directions: new-shape round trip, and that pre-change data
      still reads without throwing.

## Before calling testing work done

- [ ] `using AwesomeAssertions;` — no `FluentAssertions` anywhere in the diff.
- [ ] Every new/changed service method, entity method, consumer, and migration has its test in this change.
- [ ] Bug fix has a genuinely failing-before regression test.
- [ ] Every insert has a delete, placed before the assertions.
- [ ] `dotnet test src --configuration Release` run and its real output read — not assumed.
