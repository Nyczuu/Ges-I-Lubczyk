# Unit Testing

Source: adapted from `developer/tutorials/unit-tests.html`, then corrected against this repo's actual
test projects. Stack (verified against `src/Tests/Nop.Tests/Nop.Tests.csproj`):

| Package | Version | Role |
|---|---|---|
| `NUnit` | 4.5.1 | test framework (`[Test]`, `[TestFixture]`, `[OneTimeSetUp]`) |
| `Moq` | 4.20.72 | mocking |
| **`AwesomeAssertions`** | 9.4.0 | fluent assertions (`.Should()`), namespace `using AwesomeAssertions;` |
| `Microsoft.Data.Sqlite` + `FluentMigrator.Runner.SQLite` | 10.0.5 / 8.0.1 | in-memory SQLite provider for data-touching tests |

**Do not add `FluentAssertions`.** `AwesomeAssertions` is the community fork of FluentAssertions
created after FluentAssertions v8 moved to a paid commercial licence. The API is identical
(`.Should().Be(...)`), so adding the wrong package compiles cleanly and passes every test while
introducing a licensing problem no automated check in this repo will catch. The correct `using` is
`AwesomeAssertions` — `[verified: src/Tests/Nop.Tests/Nop.Services.Tests/ScheduleTasks/ScheduleTaskServiceTests.cs]`.
Not xUnit, not MSTest, not TUnit, not NSubstitute.

## Project layout

`src/Tests/Nop.Tests` holds shared base classes (`BaseNopTest`, which exposes the IoC container to
tests via a static-constructor-initialized DI container and a `GetService<T>()` helper). Per-layer test
projects: `Nop.Core.Tests`, `Nop.Data.Tests`, `Nop.Services.Tests`, `Nop.Web.Tests`. A service test
class typically derives from `ServiceTest`
(`[verified: src/Tests/Nop.Tests/Nop.Services.Tests/ServiceTest.cs]` — adds core plugin/service
registrations to the test container), not from `BaseNopTest` directly.

## Canonical shape

`[verified: src/Tests/Nop.Tests/Nop.Services.Tests/ScheduleTasks/ScheduleTaskServiceTests.cs]`

```csharp
using AwesomeAssertions;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Services.ScheduleTasks;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ScheduleTasks;

[TestFixture]
public class ScheduleTaskServiceTests : ServiceTest
{
    private IScheduleTaskService _scheduleTaskService;
    private ScheduleTask _task;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _scheduleTaskService = GetService<IScheduleTaskService>();
        _task = new ScheduleTask
        {
            Enabled = false,
            Seconds = 1,
            Name = "test schedule task",
            Type = typeof(TestScheduleTask).FullName
        };
        await _scheduleTaskService.InsertTaskAsync(_task);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _scheduleTaskService.DeleteTaskAsync(_task);
    }

    [Test]
    public async Task CanInsertAndGetTask()
    {
        _task.Id = 0;
        await _scheduleTaskService.InsertTaskAsync(_task);
        var task = await _scheduleTaskService.GetTaskByIdAsync(_task.Id);
        await _scheduleTaskService.DeleteTaskAsync(_task); // clean up BEFORE asserting

        _task.Id.Should().NotBe(0);
        task.Id.Should().Be(_task.Id);
        task.Name.Should().Be(_task.Name);
    }

    [Test]
    public void InsertTaskShouldRaiseExceptionIfTaskIsNull()
    {
        Assert.Throws<AggregateException>(() =>
            _scheduleTaskService.InsertTaskAsync(null).Wait());
    }
}
```

## Rules

- `[OneTimeSetUp]` runs once per fixture; plain `[SetUp]` runs per-test — pick deliberately, don't
  default to per-test setup for expensive container/service resolution.
- **Every test that inserts data must delete it, and deletion happens *before* the assertions that
  might throw** — otherwise a failing assertion leaks rows into shared test-run state and cascades
  failures into unrelated tests.
- Assertions use `.Should().Be(...)` / `.Should().NotBeNull()` / `.Should().BeNull()`, not
  `Assert.AreEqual`/`Assert.IsNotNull`.
- **Asserting that an async method throws — two valid shapes, pick by what you assert on:**
  - `Assert.Throws<AggregateException>(() => asyncCall().Wait())` — dominant in this repo (38
    occurrences). `.Wait()` wraps whatever was thrown in an `AggregateException`, so the type argument
    is `AggregateException`, **not** the original exception type. Writing
    `Assert.Throws<ArgumentNullException>(() => asyncCall().Wait())` fails at runtime.
  - `Assert.ThrowsAsync<TException>(async () => await asyncCall())` — used where the specific
    exception type matters
    (`[verified: src/Tests/Nop.Tests/Nop.Services.Tests/Catalog/PriceFormatterTests.cs]`). Perfectly
    valid here; earlier revisions of this document wrongly said otherwise.
