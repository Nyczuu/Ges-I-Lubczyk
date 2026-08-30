---
name: event-consumer-standards-check
description: >-
  Load this when writing or reviewing an IConsumer implementation, publishing a new event, or adding an
  IScheduleTask. Use it BEFORE writing the code: event publishing here is synchronous and in-process, so
  a consumer that blocks or throws changes the publisher's behaviour rather than failing off to the
  side, and a scheduled task's type string fails silently at runtime rather than at compile time.
---

# Event & Scheduled Task Standards Check

Full doc: [`Docs/knowledge-base/07-events-and-scheduled-tasks.md`](../../../Docs/knowledge-base/07-events-and-scheduled-tasks.md).
This is the checklist form.

## Before publishing a new event — check the free ones first

Every `BaseEntity` type already raises `EntityInsertedEvent<T>`, `EntityUpdatedEvent<T>`, and
`EntityDeletedEvent<T>`, because `IRepository<T>`'s write methods default `publishEvent` to `true`.

- [ ] "React when X changes" is served by consuming the built-in entity event — **not** by publishing a
      hand-rolled change notification next to the repository call, and **not** by overriding a service
      method or patching a controller action to intercept the write.
- [ ] A new custom event is for a *domain* occurrence the entity events cannot express ("order placed"
      as a business fact, not "Order row updated").

## Consumers

- [ ] `IConsumer<TEvent>` is auto-discovered by `ITypeFinder`. There is no subscription list to edit —
      if you are editing one, you are fighting the framework.
- [ ] One class may implement several `IConsumer<T>` interfaces when they share handling; do not split
      into near-identical classes for its own sake.
- [ ] Null-guard the event payload the way the codebase does (`if (eventMessage?.X is null) return;`) —
      but do not add defensive code the architecture already prevents (rule 10).

## Synchronous, in-process — the consequence

`PublishAsync` runs consumers inline, on the publisher's call stack, inside the publisher's transaction.

- [ ] No long-running work in a consumer: no HTTP call to a slow third party, no large batch job. That
      work belongs in an `IScheduleTask`, with the consumer only recording that it is needed.
- [ ] **A consumer that throws propagates into the publisher.** Decide deliberately whether that is what
      you want. For a side-effect that must not break the originating operation (an ERP push, an
      analytics ping), catch and log inside the consumer.
- [ ] No assumption of ordering between two consumers of the same event.

## Scheduled tasks

- [ ] Implements `IScheduleTask`; registration is a **database row** inserted in `InstallAsync` via
      `IScheduleTaskService.InsertTaskAsync`.
- [ ] `Type` is exactly `Namespace.ClassName, AssemblyName`. A mismatch **fails silently at runtime** —
      the task simply never fires, with no compile error and no log entry to look for.
- [ ] The task row is deleted in `UninstallAsync`.
- [ ] `Seconds`, `Enabled`, and `StopOnError` set deliberately, not copied from another plugin.
- [ ] **Idempotent and safe to run redundantly.** This deploys to ECS, where more than one task instance
      can run the scheduler concurrently. A task that assumes it is the only runner — double-sending an
      email, double-writing a row — is a bug here, not a theoretical risk.
- [ ] A new task row requires an app restart to be picked up; note that where it matters for rollout.

## Before calling this done

- [ ] Built-in entity events considered before adding a custom event.
- [ ] No slow or externally-dependent work inline in a consumer.
- [ ] Throw-vs-catch behaviour in the consumer is a deliberate decision, stated.
- [ ] Scheduled task `Type` string verified character by character against the real namespace and
      assembly name.
- [ ] Scheduled task is idempotent under concurrent execution.
