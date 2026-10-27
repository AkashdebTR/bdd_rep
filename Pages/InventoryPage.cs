using Microsoft.Playwright;
using PlaywrightBdd.Config;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightBdd.Pages;

/// <summary>
/// Models inventory-page readiness and user logout behavior.
/// </summary>
public sealed class InventoryPage : BasePage
{
    /// <summary>
    /// Creates an inventory page object bound to a scenario-owned page.
    /// </summary>
    /// <param name="page">The scenario-scoped Playwright page.</param>
    /// <param name="timeouts">The configured timeout values.</param>
    public InventoryPage(IPage page, TimeoutSettings timeouts) : base(page, timeouts)
    {
    }

    /// <summary>Gets the relative route for the inventory page.</summary>
    public override string Path => "/inventory.html";

    /// <summary>Gets the application brand locator.</summary>
    public ILocator Logo => Page.Locator(".app_logo");

    /// <summary>Gets the inventory page title locator.</summary>
    public ILocator Title => Page.GetByTestId("title");

    /// <summary>Gets the inventory content locator used as the page readiness condition.</summary>
    public ILocator InventoryContainer => Page.GetByTestId("inventory-container");
    private ILocator MenuButton => Page.GetByRole(AriaRole.Button, new() { Name = "Open Menu" });
    private ILocator LogoutLink => Page.GetByText("Logout", new() { Exact = true });

    /// <summary>Waits for the inventory content after document readiness.</summary>
    /// <returns>A task completed when the inventory page is ready.</returns>
    public override Task WaitForReadyAsync() => WaitForPageReadyAsync(InventoryContainer);

    /// <summary>
    /// Opens the navigation menu, logs out, and returns the ready login page.
    /// </summary>
    /// <returns>The login page after logout completes.</returns>
    /// <exception cref="TimeoutException">The menu, logout action, or resulting login page is not ready.</exception>
    public async Task<LoginPage> LogoutAsync()
    {
        await MenuButton.ClickAsync();
        await LogoutLink.ClickAsync();

        var loginPage = new LoginPage(Page, Timeouts);
        await loginPage.WaitForReadyAsync();
        return loginPage;
    }
}
