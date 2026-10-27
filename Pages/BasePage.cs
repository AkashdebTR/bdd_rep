using Microsoft.Playwright;
using PlaywrightBdd.Config;
using PlaywrightBdd.Utilities;

namespace PlaywrightBdd.Pages;

/// <summary>
/// Provides shared navigation and readiness behavior for application page objects.
/// </summary>
public abstract class BasePage
{
    /// <summary>
    /// Creates a page object using the scenario's page and timeout configuration.
    /// </summary>
    /// <param name="page">The scenario-scoped Playwright page.</param>
    /// <param name="timeouts">The configured timeout values.</param>
    protected BasePage(IPage page, TimeoutSettings timeouts)
    {
        Page = page;
        Timeouts = timeouts;
        Wait = new CustomWait(page, timeouts);
    }

    protected IPage Page { get; }
    protected TimeoutSettings Timeouts { get; }
    protected CustomWait Wait { get; }

    /// <summary>
    /// Gets the relative route for this application page.
    /// </summary>
    public abstract string Path { get; }

    /// <summary>
    /// Navigates to this page and waits for its application-specific readiness condition.
    /// </summary>
    /// <returns>A task completed when navigation and page readiness complete.</returns>
    /// <exception cref="Microsoft.Playwright.PlaywrightException">Navigation fails.</exception>
    public async Task GotoAsync()
    {
        await Page.GotoAsync(Path, new PageGotoOptions
        /// <summary>
        /// Waits for the page-specific condition that makes the page ready for interaction.
        /// </summary>
        /// <returns>A task completed when the page is ready.</returns>
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await WaitForReadyAsync();
    }

    protected Task WaitForPageReadyAsync(ILocator readinessLocator) => Wait.WaitForPageReadyAsync(readinessLocator);

    public abstract Task WaitForReadyAsync();
}
