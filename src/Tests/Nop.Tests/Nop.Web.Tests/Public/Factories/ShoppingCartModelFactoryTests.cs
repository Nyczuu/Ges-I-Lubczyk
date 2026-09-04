using System.Reflection;
using AwesomeAssertions;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Web.Factories;
using Nop.Web.Models.ShoppingCart;
using NUnit.Framework;

namespace Nop.Tests.Nop.Web.Tests.Public.Factories;

[TestFixture]
public class ShoppingCartModelFactoryTests : WebTest
{
    private IShoppingCartModelFactory _shoppingCartModelFactory;
    private IShoppingCartService _shoppingCartService;
    private IWorkContext _workContext;
    private IProductService _producService;
    private ILocalizationService _localizationService;
    private ShoppingCartItem _shoppingCartItem;
    private ShoppingCartItem _wishlistItem;
    private ICustomerService _customerService;
    private ShippingSettings _shippingSettings;
    private ShippingSettings _orderTotalCalculationShippingSettings;
    private Product _product;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _shoppingCartModelFactory = GetService<IShoppingCartModelFactory>();
        _shoppingCartService = GetService<IShoppingCartService>();
        _workContext = GetService<IWorkContext>();
        _producService = GetService<IProductService>();
        _localizationService = GetService<ILocalizationService>();
        _customerService = GetService<ICustomerService>();

        //ShippingSettings is registered Transient in this test harness - every GetService<ShippingSettings>()
        //call loads a disconnected new instance, so mutating one has no effect on the already-constructed
        //_shoppingCartModelFactory above, which captured its own instance once at construction. Re-resolving
        //IShoppingCartModelFactory itself per test (instead of reusing the shared field) was tried and made
        //the whole fixture crash the test host with a stack overflow deep in the DI container's IL-emitted
        //resolver - repeated whole-graph Transient resolution of this factory is not safe in this harness.
        //Reflecting into the shared factory's own field mutates the exact object it already reads, with no
        //re-resolution and no database write.
        _shippingSettings = (ShippingSettings)typeof(ShoppingCartModelFactory)
            .GetField("_shippingSettings", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_shoppingCartModelFactory);

        //the free-shipping-reached branch is decided by IOrderTotalCalculationService.IsFreeShippingAsync,
        //not by the factory's own copy above - ShippingSettings being Transient means the
        //IOrderTotalCalculationService instance the factory holds captured a THIRD, separate
        //ShippingSettings instance at its own construction, so it needs its own reflection target too
        var orderTotalCalculationService = typeof(ShoppingCartModelFactory)
            .GetField("_orderTotalCalculationService", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_shoppingCartModelFactory);
        _orderTotalCalculationShippingSettings = (ShippingSettings)orderTotalCalculationService.GetType()
            .GetField("_shippingSettings", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(orderTotalCalculationService);

        var store = await GetService<IStoreContext>().GetCurrentStoreAsync();

        var customer = await _workContext.GetCurrentCustomerAsync();

        _shoppingCartItem = new ShoppingCartItem
        {
            ProductId = 1,
            Quantity = 1,
            CustomerId = customer.Id,
            ShoppingCartType = ShoppingCartType.ShoppingCart,
            StoreId = store.Id
        };

        _wishlistItem = new ShoppingCartItem
        {
            ProductId = 2,
            Quantity = 1,
            CustomerId = customer.Id,
            ShoppingCartType = ShoppingCartType.Wishlist
        };

        var shoppingCartRepo = GetService<IRepository<ShoppingCartItem>>();

        await shoppingCartRepo.InsertAsync(new List<ShoppingCartItem> { _shoppingCartItem, _wishlistItem });

        customer.HasShoppingCartItems = true;
        await _customerService.UpdateCustomerAsync(customer);

        //the fixture's cart product ("Build your own computer") ships with IsFreeShipping = true in the
        //seed data - IsFreeShippingAsync's product-level check short-circuits to true before the X-value
        //threshold is ever evaluated, so the free-shipping-bar tests below must temporarily flip it off to
        //actually exercise FreeShippingOverXValue rather than this unrelated per-product flag
        _product = await _producService.GetProductByIdAsync(_shoppingCartItem.ProductId);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _shoppingCartService.DeleteShoppingCartItemAsync(_shoppingCartItem);
        await _shoppingCartService.DeleteShoppingCartItemAsync(_wishlistItem);

        var customer = await _workContext.GetCurrentCustomerAsync();
        customer.HasShoppingCartItems = false;
        await _customerService.UpdateCustomerAsync(customer);
    }

    [Test]
    public async Task CanPrepareEstimateShippingModel()
    {
        var model = await _shoppingCartModelFactory.PrepareEstimateShippingModelAsync(await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync()));

        model.AvailableCountries.Any().Should().BeTrue();
        model.AvailableStates.Any().Should().BeTrue();
        model.Enabled.Should().BeTrue();
        model.ZipPostalCode.Should().Be("10021");
        model.CountryId.Should().BeNull();
        model.StateProvinceId.Should().BeNull();
    }

    [Test]
    public async Task CanPrepareShoppingCartModel()
    {
        var model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(new ShoppingCartModel(),
            new List<ShoppingCartItem> { _shoppingCartItem });

        model.IsEditable.Should().BeTrue();
        model.Items.Any().Should().BeTrue();
        model.Items.Count.Should().Be(1);
        model.Warnings.Count.Should().Be(0);

        model.OrderReviewData.Should().NotBeNull();
        model.OrderReviewData.Display.Should().BeFalse();
        model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(new ShoppingCartModel(),
            new List<ShoppingCartItem> { _shoppingCartItem }, true, true, true);
        model.OrderReviewData.Should().NotBeNull();
        model.OrderReviewData.Display.Should().BeTrue();
    }

    [Test]
    public async Task CanPrepareWishlistModel()
    {
        var model = await _shoppingCartModelFactory.PrepareWishlistModelAsync(new WishlistModel(),
            new List<ShoppingCartItem> { _wishlistItem });

        var customer = await _workContext.GetCurrentCustomerAsync();

        model.CustomerFullname.Should().Be("John Smith");
        model.CustomerGuid.Should().Be(customer.CustomerGuid);
        model.EmailWishlistEnabled.Should().BeTrue();
        model.IsEditable.Should().BeTrue();
        model.Items.Any().Should().BeTrue();
        model.Items.Count.Should().Be(1);
        model.Warnings.Count.Should().Be(0);
    }

    [Test]
    public async Task CanPrepareMiniShoppingCartModel()
    {
        var model = await _shoppingCartModelFactory.PrepareMiniShoppingCartModelAsync();

        model.CurrentCustomerIsGuest.Should().BeFalse();
        model.Items.Any().Should().BeTrue();
        model.Items.Count.Should().Be(1);
        model.TotalProducts.Should().Be(1);
        model.SubTotal.Should().Be("$1,200.00");
    }

    //keeps the factory's own ShippingSettings copy and the one its IOrderTotalCalculationService dependency
    //captured separately (see [OneTimeSetUp]) in sync - both are read by PrepareMiniShoppingCartModelAsync's
    //free-shipping-bar block, and being two distinct Transient-resolved objects, only setting one would
    //leave the other on its stale default (FreeShippingOverXEnabled = false)
    private void SetFreeShippingOverX(bool enabled, decimal value)
    {
        _shippingSettings.FreeShippingOverXEnabled = enabled;
        _shippingSettings.FreeShippingOverXValue = value;
        _orderTotalCalculationShippingSettings.FreeShippingOverXEnabled = enabled;
        _orderTotalCalculationShippingSettings.FreeShippingOverXValue = value;
    }

    [Test]
    public async Task CanPrepareMiniShoppingCartModelWithFreeShippingDisabled()
    {
        SetFreeShippingOverX(enabled: false, value: 0M);

        try
        {
            var model = await _shoppingCartModelFactory.PrepareMiniShoppingCartModelAsync();

            model.DisplayFreeShippingBar.Should().BeFalse();
        }
        finally
        {
            SetFreeShippingOverX(enabled: false, value: 0M);
        }
    }

    [Test]
    public async Task CanPrepareMiniShoppingCartModelWhenBelowFreeShippingThreshold()
    {
        SetFreeShippingOverX(enabled: true, value: 2000M);
        _product.IsFreeShipping = false;
        await _producService.UpdateProductAsync(_product);

        try
        {
            var model = await _shoppingCartModelFactory.PrepareMiniShoppingCartModelAsync();

            model.DisplayFreeShippingBar.Should().BeTrue();
            model.FreeShippingReached.Should().BeFalse();
            model.AmountToFreeShipping.Should().NotBeNullOrEmpty();
            model.FreeShippingProgressPercentage.Should().BeInRange(1, 99);
        }
        finally
        {
            SetFreeShippingOverX(enabled: false, value: 0M);
            _product.IsFreeShipping = true;
            await _producService.UpdateProductAsync(_product);
        }
    }

    [Test]
    public async Task CanPrepareMiniShoppingCartModelWhenFreeShippingReached()
    {
        SetFreeShippingOverX(enabled: true, value: 500M);
        _product.IsFreeShipping = false;
        await _producService.UpdateProductAsync(_product);

        try
        {
            var model = await _shoppingCartModelFactory.PrepareMiniShoppingCartModelAsync();

            model.FreeShippingReached.Should().BeTrue();
            model.AmountToFreeShipping.Should().BeNull();
            model.FreeShippingProgressPercentage.Should().Be(0);
        }
        finally
        {
            SetFreeShippingOverX(enabled: false, value: 0M);
            _product.IsFreeShipping = true;
            await _producService.UpdateProductAsync(_product);
        }
    }

    [Test]
    public async Task CanPrepareOrderTotalsModel()
    {
        var model = await _shoppingCartModelFactory.PrepareOrderTotalsModelAsync(new List<ShoppingCartItem> { _shoppingCartItem }, true);

        model.SubTotal.Should().Be("$1,200.00");
        model.OrderTotal.Should().Be("$1,200.00");

        model.GiftCards.Any().Should().BeFalse();
        model.Shipping.Should().Be("$0.00");
        model.Tax.Should().Be("$0.00");
        model.WillEarnRewardPoints.Should().Be(120);
    }

    [Test]
    public async Task CanPrepareEstimateShippingResultModel()
    {
        var model = await _shoppingCartModelFactory.PrepareEstimateShippingResultModelAsync(new List<ShoppingCartItem> { _shoppingCartItem }, new EstimateShippingModel(), true);
        model.Errors.Any().Should().BeFalse();
    }

    [Test]
    public async Task CanPrepareWishlistEmailAFriendModel()
    {
        var model = await _shoppingCartModelFactory.PrepareWishlistEmailAFriendModelAsync(new WishlistEmailAFriendModel(),
            false);

        model.YourEmailAddress.Should().Be(NopTestsDefaults.AdminEmail);
    }

    [Test]
    public async Task CanPrepareCartItemPictureModel()
    {
        var product = await _producService.GetProductByIdAsync(_shoppingCartItem.ProductId);

        var model = await _shoppingCartModelFactory.PrepareCartItemPictureModelAsync(_shoppingCartItem, 100, true, await _localizationService.GetLocalizedAsync(product, x => x.Name));

        model.AlternateText.Should().Be("Picture of Build your own computer");
        model.ImageUrl.Should()
            .Be($"http://{NopTestsDefaults.HostIpAddress}/images/thumbs/0000020_build-your-own-computer_100.jpeg");
        model.Title.Should().Be("Show details for Build your own computer");

        model.FullSizeImageUrl.Should().Be($"http://{NopTestsDefaults.HostIpAddress}/images/thumbs/0000020_build-your-own-computer.jpeg");
        model.ThumbImageUrl.Should().BeNull();
    }
}