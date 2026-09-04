namespace Nop.Plugin.Misc.ProductionLabels.Services.Pdf;

/// <summary>
/// Isolates the still-open HTML-to-PDF rendering library choice (spec §13 - deliberately left open pending
/// a real build-and-render smoke test against the Alpine-based runtime image) behind one seam. Page-size
/// geometry is CSS driven by <see cref="Domain.ProductionLabelSizeVariant"/> on the label view model, not a
/// converter-API parameter, so swapping the eventual library touches nothing else.
/// </summary>
/// <remarks>
/// No real rendering implementation exists yet - <c>Infrastructure/PluginServiceRegistrar.cs</c> registers
/// <see cref="NotYetAvailableHtmlToPdfConverter"/> in the meantime, purely so the admin controller that
/// depends on this interface can still be constructed by DI; a test that needs an instance of this
/// interface for anything beyond that placeholder's own behaviour should use a hand-written fake/mock
/// local to the test project, never a real converter.
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
