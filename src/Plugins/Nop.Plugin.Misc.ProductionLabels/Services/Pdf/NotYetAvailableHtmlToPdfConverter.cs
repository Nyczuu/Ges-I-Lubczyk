using Nop.Core;

namespace Nop.Plugin.Misc.ProductionLabels.Services.Pdf;

/// <summary>
/// Placeholder <see cref="IHtmlToPdfConverter"/> registered while the real HTML-to-PDF rendering library
/// choice stays open (spec Section 13, pending a build-and-render smoke test against the Alpine-based
/// runtime image). Without any registration for <see cref="IHtmlToPdfConverter"/> at all,
/// <c>ProductionLabelsAdminController</c>'s constructor could not be satisfied by the DI container for
/// ANY action - not just <c>GenerateLabel</c> - since ASP.NET Core's controller activator resolves every
/// constructor parameter before the action even runs. Registering this placeholder instead lets every
/// other action (List, ProductionBatchCreatePopup, ProductionBatchDelete, GenerateLabelPopup,
/// SaveProductInfo) work normally; only an actual "Generate label" invocation reaches
/// <see cref="ConvertAsync"/>, where it fails clearly rather than silently returning unusable bytes.
/// </summary>
public class NotYetAvailableHtmlToPdfConverter : IHtmlToPdfConverter
{
    /// <summary>
    /// Always throws - no concrete HTML-to-PDF rendering is wired up yet
    /// </summary>
    /// <param name="html">The full HTML document to render</param>
    /// <returns>Never returns; always throws <see cref="NopException"/></returns>
    public Task<byte[]> ConvertAsync(string html)
    {
        throw new NopException("HTML to PDF conversion is not yet configured for this store.");
    }
}
