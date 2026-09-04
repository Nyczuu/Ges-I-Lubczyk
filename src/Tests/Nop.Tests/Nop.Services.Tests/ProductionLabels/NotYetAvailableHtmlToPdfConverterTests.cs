using AwesomeAssertions;
using Nop.Core;
using Nop.Plugin.Misc.ProductionLabels.Services.Pdf;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ProductionLabels;

/// <summary>
/// NotYetAvailableHtmlToPdfConverter is the placeholder IHtmlToPdfConverter registered in
/// PluginServiceRegistrar while the real HTML-to-PDF library choice stays open (spec Section 13). It
/// exists purely so ProductionLabelsAdminController can be constructed by the DI container for every
/// action - List, ProductionBatchCreatePopup, ProductionBatchDelete, GenerateLabelPopup, SaveProductInfo -
/// with nothing registered for IHtmlToPdfConverter previously breaking the controller's activation
/// entirely, not just GenerateLabel. Only an actual GenerateLabel invocation should ever reach
/// ConvertAsync, and it must fail clearly (a normal, unswallowed exception - spec Section 10's posture)
/// rather than silently produce garbage bytes.
/// </summary>
[TestFixture]
public class NotYetAvailableHtmlToPdfConverterTests
{
    [Test]
    public void ConvertAsync_ThrowsNopExceptionWithAClearMessage()
    {
        var converter = new NotYetAvailableHtmlToPdfConverter();

        var exception = Assert.ThrowsAsync<NopException>(async () => await converter.ConvertAsync("<html></html>"));

        exception.Message.Should().NotBeNullOrWhiteSpace();
    }
}
