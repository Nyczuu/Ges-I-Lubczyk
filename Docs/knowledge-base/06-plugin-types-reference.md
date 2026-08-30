# Specific Plugin Interfaces

Source: adapted from `developer/plugins/payment-method.html`, `developer/plugins/shipping-plugin.html`,
`developer/design/widgets.html`, `developer/plugins/menu-item.html`.

`IPlugin` has several specific derived interfaces. Pick the narrowest one that matches the feature —
don't implement `IMiscPlugin` for something that is really a payment method, widget, etc., since the
specific interfaces plug into dedicated admin sections and checkout/shipping pipelines automatically.

| Interface | Namespace | Use for |
|---|---|---|
| `IPaymentMethod` | `Nop.Services.Payments` | Payment gateways |
| `IShippingRateComputationMethod` | `Nop.Services.Shipping` | Carrier rate/tracking integrations |
| `IPickupPointProvider` | `Nop.Services.Shipping.Pickup` | Pickup-point networks |
| `ITaxProvider` | `Nop.Services.Tax` | Tax rate calculation |
| `IExchangeRateProvider` | `Nop.Services.Directory` | Currency exchange rates |
| `IDiscountRequirementRule` | `Nop.Services.Discounts` | Custom discount conditions |
| `IExternalAuthenticationMethod` | `Nop.Services.Authentication.External` | Social/SSO login |
| `IMultiFactorAuthenticationMethod` | `Nop.Services.Authentication.MultiFactor` | 2FA/MFA providers (4.40+) |
| `IWidgetPlugin` | `Nop.Services.Cms` | Renders into a `PublicWidgetZone` |
| `IMiscPlugin` | `Nop.Services.Plugins` | Anything not covered above |

## IPaymentMethod — key members

```csharp
public class MyPaymentProcessor : BasePlugin, IPaymentMethod
{
    Task<List<string>> ValidatePaymentFormAsync(IFormCollection form); // input validation, empty list if none needed
    Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form); // parse checkout form input
    Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request); // called before order is placed
    Task PostProcessPaymentAsync(PostProcessPaymentRequest request); // called after order is placed (e.g. redirect to gateway)
    Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart);
    Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart);
    Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest request);
    Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest request);
    Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest request);
    bool SupportCapture { get; } // gate for showing the admin "Capture" button
    PaymentMethodType PaymentMethodType { get; } // Standard | Redirection | Button
}
```

Public checkout UI is a `NopViewComponent` returned from a `GetPublicViewComponent`-style hook, placed
in `/Components`, rendering `~/Plugins/{Group}.{Name}/Views/Public/PaymentInfo.cshtml`.

## IShippingRateComputationMethod — key members

```csharp
Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest request); // used during checkout method selection
Task<decimal?> GetFixedRateAsync(GetShippingOptionRequest request); // used earlier (e.g. cart page) when only one flat rate applies; null = "calculated during checkout"
Task<IShipmentTracker> GetShipmentTrackerAsync();
```

Relevant to the gastronomy domain: a carrier constraint like "temperature-controlled / fragile jar
packaging surcharge" belongs in `GetShippingOptionsAsync`/`GetFixedRateAsync`, not hacked into the
generic shipping settings.

## IWidgetPlugin — rendering into a zone

```csharp
public class ProductBadgeWidget : BasePlugin, IWidgetPlugin
{
    public bool HideInWidgetList => true;
    public Type GetWidgetViewComponent(string widgetZone) => typeof(ProductBadgeViewComponent);
    public Task<IList<string>> GetWidgetZonesAsync() =>
        Task.FromResult<IList<string>>(new List<string> { PublicWidgetZones.ProductDetailsTop });
}
```

The companion `NopViewComponent` receives `additionalData` (e.g. a `ProductDetailsModel`) and returns
a `Content("")`-guarded partial — always null-check `additionalData is ProductDetailsModel model`
before use.

## Adding an admin menu item (nopCommerce 4.80+)

Do **not** edit any sitemap/config file. Subscribe to `AdminMenuCreatedEvent`:

```csharp
public class EventConsumer : IConsumer<AdminMenuCreatedEvent>
{
    private readonly IPermissionService _permissionService;
    public EventConsumer(IPermissionService permissionService) => _permissionService = permissionService;

    public async Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
            return;

        eventMessage.RootMenuItem.InsertBefore("Local plugins", new AdminMenuItem
        {
            SystemName = "GastronomyCompliance",
            Title = "Gastronomy Compliance",
            Url = eventMessage.GetMenuItemUrl("GastronomyCompliance", "Configure"),
            IconClass = "far fa-dot-circle",
            Visible = true
        });
    }
}
```
(4.70 and below used `IAdminMenuPlugin.ManageSiteMapAsync` + `sitemap.config` — irrelevant for this
5.00 codebase, don't generate that pattern.)
