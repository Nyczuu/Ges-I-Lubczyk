using AwesomeAssertions;
using Moq;
using Nop.Plugin.Misc.ProductionLabels;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Plugin.Misc.ProductionLabels.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;
using Nop.Web.Framework.Mvc.Routing;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ProductionLabels;

/// <summary>
/// No sibling precedent for this test (Ingredients/ServingSuggestions have no equivalent) - required
/// unconditionally by testing-standards-check's new-IConsumer&lt;T&gt; gate. Pure NUnit + Moq: precise
/// control over IPluginManager's plugin-installed/not-installed branches is what this test needs to prove,
/// which a real ServiceTest/SQLite fixture cannot easily force either way on demand.
/// </summary>
[TestFixture]
public class ProductionLabelsMenuEventConsumerTests
{
    private Mock<ILocalizationService> _localizationServiceMock;
    private Mock<INopUrlHelper> _nopUrlHelperMock;
    private Mock<IPermissionService> _permissionServiceMock;
    private Mock<IPluginManager<IPlugin>> _pluginManagerMock;
    private ProductionLabelsMenuEventConsumer _consumer;

    [SetUp]
    public void SetUp()
    {
        _localizationServiceMock = new Mock<ILocalizationService>();
        _nopUrlHelperMock = new Mock<INopUrlHelper>();
        _permissionServiceMock = new Mock<IPermissionService>();
        _pluginManagerMock = new Mock<IPluginManager<IPlugin>>();

        _localizationServiceMock.Setup(x => x.GetResourceAsync(It.IsAny<string>()))
            .ReturnsAsync("Production");
        _nopUrlHelperMock.Setup(x => x.RouteUrl(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("/Admin/ProductionLabels/List");

        _consumer = new ProductionLabelsMenuEventConsumer(
            _localizationServiceMock.Object,
            _nopUrlHelperMock.Object,
            _permissionServiceMock.Object,
            _pluginManagerMock.Object);
    }

    private static AdminMenuItem BuildRootMenuWithFilterLevelValues()
    {
        var filterLevelValues = new AdminMenuItem { SystemName = "Filter level values", Title = "Filter level values" };
        var catalog = new AdminMenuItem { SystemName = "Catalog", Title = "Catalog" };
        catalog.ChildNodes.Add(filterLevelValues);

        var root = new AdminMenuItem { SystemName = "Root", Title = "Root" };
        root.ChildNodes.Add(catalog);

        return root;
    }

    [Test]
    public async Task HandleEventAsync_WhenNotAuthorized_DoesNotInsertAMenuItem()
    {
        _permissionServiceMock.Setup(x => x.AuthorizeAsync(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW)).ReturnsAsync(false);

        var rootMenuItem = BuildRootMenuWithFilterLevelValues();
        var eventMessage = new AdminMenuCreatedEvent(Mock.Of<IAdminMenu>(), rootMenuItem);

        await _consumer.HandleEventAsync(eventMessage);

        rootMenuItem.ContainsSystemName(ProductionLabelsDefaults.ProductionLabelsMenuSystemName).Should().BeFalse();
        _pluginManagerMock.Verify(x => x.LoadPluginBySystemNameAsync(It.IsAny<string>(), It.IsAny<global::Nop.Core.Domain.Customers.Customer>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// The null-payload path BaseAdminMenuCreatedEventConsumer.HandleEventAsync guards: LoadPluginBySystemNameAsync
    /// only ever returns an already-installed plugin, and the AdminMenuCreatedEvent can fire before
    /// installation completes - this must not throw and must not insert anything in that window.
    /// </summary>
    [Test]
    public async Task HandleEventAsync_WhenPluginIsNotYetInstalled_DoesNotInsertAMenuItemOrThrow()
    {
        _permissionServiceMock.Setup(x => x.AuthorizeAsync(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW)).ReturnsAsync(true);
        _pluginManagerMock.Setup(x => x.LoadPluginBySystemNameAsync(ProductionLabelsDefaults.SystemName, null, 0)).ReturnsAsync((IPlugin)null);

        var rootMenuItem = BuildRootMenuWithFilterLevelValues();
        var eventMessage = new AdminMenuCreatedEvent(Mock.Of<IAdminMenu>(), rootMenuItem);

        await _consumer.HandleEventAsync(eventMessage);

        rootMenuItem.ContainsSystemName(ProductionLabelsDefaults.ProductionLabelsMenuSystemName).Should().BeFalse();
    }

    [Test]
    public async Task HandleEventAsync_WhenAuthorizedAndPluginInstalled_InsertsTheMenuItemAfterFilterLevelValues()
    {
        _permissionServiceMock.Setup(x => x.AuthorizeAsync(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW)).ReturnsAsync(true);
        _pluginManagerMock.Setup(x => x.LoadPluginBySystemNameAsync(ProductionLabelsDefaults.SystemName, null, 0)).ReturnsAsync(Mock.Of<IPlugin>());

        var rootMenuItem = BuildRootMenuWithFilterLevelValues();
        var eventMessage = new AdminMenuCreatedEvent(Mock.Of<IAdminMenu>(), rootMenuItem);

        await _consumer.HandleEventAsync(eventMessage);

        var catalog = rootMenuItem.ChildNodes.Single(node => node.SystemName == "Catalog");
        var filterLevelValuesIndex = catalog.ChildNodes.ToList().FindIndex(node => node.SystemName == "Filter level values");
        var productionIndex = catalog.ChildNodes.ToList().FindIndex(node => node.SystemName == ProductionLabelsDefaults.ProductionLabelsMenuSystemName);

        productionIndex.Should().Be(filterLevelValuesIndex + 1);
        catalog.ChildNodes.Single(node => node.SystemName == ProductionLabelsDefaults.ProductionLabelsMenuSystemName)
            .PermissionNames.Should().Contain(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW);
    }
}
