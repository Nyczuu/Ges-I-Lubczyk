namespace Nop.Plugin.Misc.ProductionLabels.Services.Pdf;

/// <summary>
/// Isolates the HTML-to-PDF rendering library choice (spec §13 - resolved by a real build-and-render
/// smoke test against the Alpine-based runtime image; see <see cref="PuppeteerSharpHtmlToPdfConverter"/>)
/// behind one seam. Page-size geometry is CSS driven by <see cref="Domain.ProductionLabelSizeVariant"/> on
/// the label view model, not a converter-API parameter, so swapping the library touches nothing else.
/// </summary>
/// <remarks>
/// A test that needs an instance of this interface should use a hand-written fake/mock local to the test
/// project, never <see cref="PuppeteerSharpHtmlToPdfConverter"/> itself - it launches a real Chromium
/// process.
/// </remarks>
public interface IHtmlToPdfConverter
{
    /// <summary>
    /// Converts an HTML document to a PDF file
    /// </summary>
    /// <param name="html">The full HTML document to render</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rendered PDF file bytes
    /// </returns>
    Task<byte[]> ConvertAsync(string html);
}
