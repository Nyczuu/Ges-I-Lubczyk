using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Nop.Plugin.Misc.ProductionLabels.Services.Pdf;

/// <summary>
/// Renders a label's HTML through headless Chromium via PuppeteerSharp - the library spec Section 13
/// named as its build-and-render smoke test candidate, confirmed working against this repo's actual
/// Alpine-based runtime image: the <c>chromium</c> apk package launches headless and prints to PDF once
/// "--no-sandbox" is passed (the image's container process runs as root, and Chromium's setuid sandbox
/// refuses to run as root at all).
/// </summary>
/// <remarks>
/// Launching a browser process costs roughly a second, so it is cached in a static field and reused by
/// every call across every request for the lifetime of the process, rather than launched per
/// <see cref="ConvertAsync"/> call or tied to this class's own (scoped, matching every other service in
/// this plugin) DI lifetime.
/// </remarks>
public class PuppeteerSharpHtmlToPdfConverter : IHtmlToPdfConverter
{
    #region Fields

    private static readonly SemaphoreSlim _launchLock = new(1, 1);
    private static IBrowser _browser;

    #endregion

    #region Utilities

    private static async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is { IsClosed: false })
            return _browser;

        await _launchLock.WaitAsync();
        try
        {
            if (_browser is { IsClosed: false })
                return _browser;

            var executablePath = Environment.GetEnvironmentVariable(ProductionLabelsDefaults.ChromiumExecutablePathEnvironmentVariable);

            if (string.IsNullOrEmpty(executablePath))
                //no system Chromium configured (e.g. a developer machine) - fetch a compatible build;
                //never reached in the runtime image, where the environment variable above always points
                //at the apk-installed binary, which PuppeteerSharp's own downloader cannot run anyway
                //(it fetches a glibc build, and the runtime image is musl/Alpine)
                await new BrowserFetcher().DownloadAsync();

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = string.IsNullOrEmpty(executablePath) ? null : executablePath,
                Args =
                [
                    //the runtime container runs as root, and Chromium's setuid sandbox refuses to run as
                    //root at all
                    "--no-sandbox",
                    //the container's /dev/shm defaults to Docker's own 64MB, too small for Chromium's
                    //shared-memory use and a common cause of renderer crashes under load
                    "--disable-dev-shm-usage"
                ]
            });

            return _browser;
        }
        finally
        {
            _launchLock.Release();
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Converts an HTML document to a PDF file by rendering it through headless Chromium
    /// </summary>
    /// <param name="html">The full HTML document to render</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rendered PDF file bytes
    /// </returns>
    public async Task<byte[]> ConvertAsync(string html)
    {
        ArgumentException.ThrowIfNullOrEmpty(html);

        var browser = await GetBrowserAsync();

        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);

        return await page.PdfDataAsync(new PdfOptions
        {
            PrintBackground = true,
            //page-size geometry is CSS driven (the label template's own @page rule per size variant -
            //see IHtmlToPdfConverter's own remarks), so the PDF page must follow it rather than
            //PdfOptions' own default (a fixed Letter-sized page regardless of the template's @page size)
            PreferCSSPageSize = true,
            //the template's .label element already carries its own padding for the printable inset -
            //a PDF-level page margin on top of that would double it up
            MarginOptions = new MarginOptions
            {
                Top = "0",
                Bottom = "0",
                Left = "0",
                Right = "0"
            }
        });
    }

    #endregion
}
