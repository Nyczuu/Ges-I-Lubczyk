# Unit Testing

Source: adapted from `developer/tutorials/unit-tests.html`. Framework: **NUnit** + **FluentAssertions**
(not xUnit/MSTest, not plain `Assert.AreEqual`).

## Project layout

`src/Tests/Nop.Tests` holds shared base classes (`BaseNopTest`, exposes the IoC container to tests via
a static-constructor-initialized DI container and a `GetService<T>()` helper). Per-layer test
projects: `Nop.Core.Tests`, `Nop.Data.Tests`, `Nop.Services.Tests`, `Nop.Web.Tests`. A service test
class typically derives from `ServiceTest` (adds core plugin/service registrations to the test
container), not `BaseNopTest` directly.

## Canonical shape

```csharp
[TestFixture]
public class ScheduleTaskServiceTests : ServiceTest
{
    private IScheduleTaskService _scheduleTaskService;
    private ScheduleTask _task;

    [OneTimeSetUp]
    public void SetUp()
    {
        _scheduleTaskService = GetService<IScheduleTaskService>();
        _task = new ScheduleTask { Enabled = true, Name = "Test task", Seconds = 60, Type = "nop.test.task" };
    }

    [Test]
    public async Task CanInsertAndGetTask()
    {
        _task.Id = 0;
        await _scheduleTaskService.InsertTaskAsync(_task);
        var task = await _scheduleTaskService.GetTaskByIdAsync(_task.Id);
        await _scheduleTaskService.DeleteTaskAsync(_task); // clean up BEFORE asserting — a failed assert must not leave DB state behind for later tests

        _task.Id.Should().NotBe(0);
        task.Id.Should().Be(_task.Id);
    }

    [Test]
    public void InsertTaskShouldRaiseExceptionIfTaskIsNull() =>
        Assert.Throws<ArgumentNullException>(() => _scheduleTaskService.InsertTaskAsync(null).Wait());

    [OneTimeTearDown]
    public async Task TearDown()
    {
        var tasks = await _scheduleTaskService.GetAllTasksAsync(true);
        foreach (var t in tasks.Where(t => t.Type == _task.Type))
            await _scheduleTaskService.DeleteTaskAsync(t);
    }
}
```

Rules an AI assistant should follow when adding tests here:

- `[OneTimeSetUp]` runs once per fixture; plain `[SetUp]` runs per-test — pick deliberately, don't
  default to per-test setup for expensive container/service resolution.
- **Every test that inserts data must delete it**, and deletion happens *before* the assertions that
  might throw, not after — otherwise a failing assertion leaks rows into shared test-run state and
  cascades failures into unrelated tests.
- Assertions use FluentAssertions (`.Should().Be(...)`, `.Should().NotBeNull()`), not
  `Assert.AreEqual`/`Assert.IsNotNull`.
- `Assert.Throws<T>(() => asyncCall().Wait())` is the documented pattern for asserting an async method
  throws synchronously-observable exceptions in this codebase's test style — not `Assert.ThrowsAsync`.
