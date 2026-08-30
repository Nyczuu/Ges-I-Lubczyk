# Events & Scheduled Tasks

Source: adapted from `developer/tutorials/events.html`, `developer/design/entity-events-system.html`,
`developer/tutorials/scheduled-tasks.html`.

## Publishing an event

```csharp
public class OrderPlacedEvent
{
    public OrderPlacedEvent(Order order) => Order = order;
    public Order Order { get; }
}

await _eventPublisher.PublishAsync(new OrderPlacedEvent(order));
```

## Consuming an event

```csharp
public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        if (eventMessage?.Order is null) return;
        // business logic — no source-code change to the publisher required
    }
}
```

`IConsumer<T>` implementations are auto-discovered and registered by `ITypeFinder` — there is no
manual subscription list to edit.

## Built-in entity CRUD events (no extra publish code needed)

Any `BaseEntity`-derived type already gets insert/update/delete events for free via
`IEventPublisher` extension methods called internally by the repository layer:

```csharp
public class SyncOnProductChange : IConsumer<EntityInsertedEvent<Product>>,
                                    IConsumer<EntityUpdatedEvent<Product>>,
                                    IConsumer<EntityDeletedEvent<Product>>
{
    public Task HandleEventAsync(EntityInsertedEvent<Product> e) => SyncAsync(e.Entity);
    public Task HandleEventAsync(EntityUpdatedEvent<Product> e) => SyncAsync(e.Entity);
    public Task HandleEventAsync(EntityDeletedEvent<Product> e) => SyncAsync(e.Entity);

    private Task SyncAsync(Product product) { /* e.g. push to an external ERP */ return Task.CompletedTask; }
}
```

This is the correct extension point for "sync X to an external system whenever a core entity
changes" — do **not** try to intercept this by overriding a service method or patching a controller
action.

## Scheduled tasks

```csharp
public class ExpiringBatchAlertTask : IScheduleTask
{
    private readonly IProductService _productService;
    public ExpiringBatchAlertTask(IProductService productService) => _productService = productService;

    public async Task ExecuteAsync()
    {
        // e.g. scan for products nearing GenericAttribute "ExpirationDate" and flag/notify
    }
}
```

Registration is a **database row**, inserted (typically) in the plugin's `InstallAsync`:

```csharp
await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
{
    Name = "Expiring batch alert",
    Seconds = 3600,
    Type = "Nop.Plugin.Misc.GastronomyCompliance.Tasks.ExpiringBatchAlertTask, Nop.Plugin.Misc.GastronomyCompliance",
    Enabled = true,
    StopOnError = false
});
```

`Type` **must** be `Namespace.ClassName, AssemblyName` — a mismatch here fails silently at runtime
(task never fires), not at compile time. Restart the app after inserting a new scheduled task row for
it to be picked up by the task scheduler.
