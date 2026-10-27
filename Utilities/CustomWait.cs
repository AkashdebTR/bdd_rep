using Microsoft.Playwright;
using PlaywrightBdd.Config;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightBdd.Utilities;

/// <summary>
/// Provides explicit, timeout-configured waits for Playwright page and locator conditions.
/// </summary>
/// <remarks>Use Playwright's built-in action auto-waiting first; page readiness combines DOMContentLoaded with an application-specific locator.</remarks>
public sealed class CustomWait
{
    private readonly IPage _page;
    private readonly TimeoutSettings _timeouts;

    /// <summary>
    /// Creates waits scoped to one page and the framework's timeout settings.
    /// </summary>
    /// <param name="page">The scenario-owned Playwright page.</param>
    /// <param name="timeouts">The immutable action, assertion, and navigation timeouts.</param>
    /// <exception cref="ArgumentNullException">A required dependency is null.</exception>
    public CustomWait(IPage page, TimeoutSettings timeouts)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(timeouts);
        _page = page;
        _timeouts = timeouts;
    }

    /// <summary>
    /// Waits until a locator is visible using Playwright's locator-state wait.
    /// </summary>
    /// <param name="locator">The locator to observe.</param>
    /// <returns>A task completed when the locator becomes visible.</returns>
    /// <exception cref="TimeoutException">The locator does not become visible within the configured assertion timeout.</exception>
    public Task WaitForVisibleAsync(ILocator locator) => locator.WaitForAsync(new LocatorWaitForOptions
    {
        State = WaitForSelectorState.Visible,
        Timeout = _timeouts.AssertionMs
    });

    /// <summary>
    /// Waits until a locator is hidden or absent.
    /// </summary>
    /// <param name="locator">The locator to observe.</param>
    /// <returns>A task completed when the locator is hidden or absent.</returns>
    /// <exception cref="TimeoutException">The locator remains visible past the configured assertion timeout.</exception>
    public Task WaitForHiddenAsync(ILocator locator) => locator.WaitForAsync(new LocatorWaitForOptions
    {
        State = WaitForSelectorState.Hidden,
        Timeout = _timeouts.AssertionMs
    });

    /// <summary>
    /// Waits until a locator is enabled using Playwright's auto-retrying assertion.
    /// </summary>
    /// <param name="locator">The control to observe.</param>
    /// <returns>A task completed when the control is enabled.</returns>
    /// <exception cref="PlaywrightException">The control does not become enabled within the configured assertion timeout.</exception>
    public Task WaitForEnabledAsync(ILocator locator) =>
        Expect(locator).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions
        {
            Timeout = _timeouts.AssertionMs
        });

    /// <summary>
    /// Waits for the page URL to match Playwright's URL pattern.
    /// </summary>
    /// <param name="url">The URL or Playwright glob pattern to wait for.</param>
    /// <returns>A task completed when the page reaches the requested URL.</returns>
    /// <exception cref="TimeoutException">The page does not reach the requested URL within the navigation timeout.</exception>
    public Task WaitForUrlAsync(string url) => _page.WaitForURLAsync(url, new PageWaitForURLOptions
    {
        Timeout = _timeouts.NavigationMs
    });

    /// <summary>
    /// Waits for DOMContentLoaded and then for the page object's meaningful readiness locator.
    /// </summary>
    /// <param name="readinessLocator">A page-specific locator representing application readiness.</param>
    /// <returns>A task completed when the document and readiness locator are ready.</returns>
    /// <exception cref="TimeoutException">The document or readiness locator does not become ready within its configured timeout.</exception>
    public async Task WaitForPageReadyAsync(ILocator readinessLocator)
    {
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions
        {
            Timeout = _timeouts.NavigationMs
        });
        await WaitForVisibleAsync(readinessLocator);
    }
}
