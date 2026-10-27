using Microsoft.Playwright;
using PlaywrightBdd.Config;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightBdd.Pages;

/// <summary>
/// Models the application's login page and authentication actions.
/// </summary>
public sealed class LoginPage : BasePage
{
    /// <summary>
    /// Creates a login page object bound to a scenario-owned page.
    /// </summary>
    /// <param name="page">The scenario-scoped Playwright page.</param>
    /// <param name="timeouts">The configured timeout values.</param>
    public LoginPage(IPage page, TimeoutSettings timeouts) : base(page, timeouts)
    {
    }

    private ILocator UsernameInput => Page.GetByTestId("username");
    private ILocator PasswordInput => Page.GetByTestId("password");
    private ILocator LoginButton => Page.GetByTestId("login-button");

    /// <summary>Gets the relative route for the login page.</summary>
    public override string Path => "/";

    /// <summary>Gets the login-page brand locator.</summary>
    public ILocator Logo => Page.Locator(".login_logo");

    /// <summary>Gets the authentication error message locator.</summary>
    public ILocator ErrorMessage => Page.GetByTestId("error");

    /// <summary>Waits for the login-page logo after document readiness.</summary>
    /// <returns>A task completed when the login page is ready.</returns>
    public override Task WaitForReadyAsync() => WaitForPageReadyAsync(Logo);

    /// <summary>
    /// Submits valid credentials and returns the ready inventory page.
    /// </summary>
    /// <param name="username">The configured test username.</param>
    /// <param name="password">The configured test password.</param>
    /// <returns>The inventory page after the expected route and readiness condition are reached.</returns>
    /// <exception cref="TimeoutException">The inventory route or readiness condition is not reached.</exception>
    public async Task<InventoryPage> LoginAsync(string username, string password)
    {
        await SubmitCredentialsAsync(username, password);
        await Wait.WaitForUrlAsync("**/inventory.html");

        var inventoryPage = new InventoryPage(Page, Timeouts);
        await inventoryPage.WaitForReadyAsync();
        return inventoryPage;
    }

    /// <summary>
    /// Enters credentials and submits the login form without assuming authentication succeeded.
    /// </summary>
    /// <param name="username">The username to submit.</param>
    /// <param name="password">The password to submit.</param>
    /// <returns>A task completed after the login button action.</returns>
    public async Task SubmitCredentialsAsync(string username, string password)
    {
        await UsernameInput.FillAsync(username);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
    }
}
