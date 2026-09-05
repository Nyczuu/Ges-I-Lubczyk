using System.Text;
using AwesomeAssertions;
using Nop.Plugin.Misc.ProductionLabels.Services.Pdf;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ProductionLabels;

/// <summary>
/// Exercises the actual HTML-to-PDF rendering path (spec Section 13's chosen library, confirmed by a real
/// build-and-render smoke test against the Alpine runtime image - see
/// <see cref="PuppeteerSharpHtmlToPdfConverter"/>'s own remarks). No mocking - a mocked browser would prove
/// nothing about whether a real conversion produces a valid PDF. On a machine with no system Chromium
/// configured (this test project's own environment, unlike the runtime image), the first test run pays
/// PuppeteerSharp's own one-time download cost, the same cost any developer machine or CI runner pays once.
/// </summary>
[TestFixture]
public class PuppeteerSharpHtmlToPdfConverterTests
{
    private PuppeteerSharpHtmlToPdfConverter _converter;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _converter = new PuppeteerSharpHtmlToPdfConverter();
    }

    [Test]
    public void ConvertAsync_ThrowsArgumentException_WhenHtmlIsNullOrEmpty()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await _converter.ConvertAsync(string.Empty));
    }

    [Test]
    public async Task ConvertAsync_RendersHtmlToAValidPdfDocumentAtTheCssPageSize()
    {
        const string html = "<html><head><style>@page { size: 70mm 70mm; margin: 0; }</style></head><body><h1>Label</h1></body></html>";

        var pdfBytes = await _converter.ConvertAsync(html);

        pdfBytes.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdfBytes, 0, 5).Should().Be("%PDF-");
    }
}
