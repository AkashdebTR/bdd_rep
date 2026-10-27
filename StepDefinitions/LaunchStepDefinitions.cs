using Microsoft.Playwright;
using PlaywrightBdd.Config;
using PlaywrightBdd.Pages;
using Reqnroll;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightBdd.StepDefinitions;

/// <summary>Maps authentication Gherkin steps to page-object operations and UI assertions.</summary>
[Binding]
public sealed class LaunchStepDefinitions
{
    private readonly LoginPage _loginPage;
    private readonly InventoryPage _inventoryPage;
    private readonly FrameworkSettings _settings;

    /// <summary>
    /// Creates scenario step bindings using the injected page and immutable settings.
    /// </summary>
    /// <param name="page">The page registered by the scenario hook.</param>
    /// <param name="settings">The validated scenario settings.</param>
    public LaunchStepDefinitions(IPage page, FrameworkSettings settings)
    {
        _loginPage = new LoginPage(page, settings.Timeouts);
        _inventoryPage = new InventoryPage(page, settings.Timeouts);
        _settings = settings;
    }

    /// <summary>Navigates to and waits for the login page.</summary>
    [Given("I am on the login page")]
    public async Task GivenIAmOnTheLoginPage()
    {
        await _loginPage.GotoAsync();
    }

    /// <summary>Submits the configured valid user credentials.</summary>
    [When("I log in with valid credentials")]
    public async Task WhenILogInWithValidCredentials()
    {
        var credentials = _settings.RequireCredentials();
        await _loginPage.LoginAsync(credentials.Username!, credentials.Password!);
    }

    /// <summary>Submits the configured username with an intentionally invalid password.</summary>
    [When("I attempt to log in with an invalid password")]
    public async Task WhenIAttemptToLogInWithAnInvalidPassword()
    {
        var credentials = _settings.RequireCredentials();
        await _loginPage.SubmitCredentialsAsync(credentials.Username!, "invalid-password");
    }

    /// <summary>Submits empty username and password values.</summary>
    [When("I submit empty credentials")]
    public async Task WhenISubmitEmptyCredentials()
    {
        await _loginPage.SubmitCredentialsAsync(string.Empty, string.Empty);
    }

    /// <summary>Submits the SauceDemo locked-out username and configured password.</summary>
    [When("I log in as the locked-out user with the configured password")]
    public async Task WhenILogInAsTheLockedOutUserWithTheConfiguredPassword()
    {
        var credentials = _settings.RequireCredentials();
        await _loginPage.SubmitCredentialsAsync("locked_out_user", credentials.Password!);
    }

    /// <summary>Asserts that the inventory page is displayed.</summary>
    [Then("I should see the inventory page")]
    public async Task ThenIShouldSeeTheInventoryPage()
    {
        await Expect(_inventoryPage.Logo).ToHaveTextAsync(Constant.SwagLabsLogo);
        await Expect(_inventoryPage.Title).ToHaveTextAsync(Constant.InventoryPageTitle);
        await Expect(_inventoryPage.InventoryContainer).ToBeVisibleAsync();
    }

    /// <summary>Asserts the expected login error text.</summary>
    [Then("I should see the login error {string}")]
    public async Task ThenIShouldSeeTheLoginError(string expectedMessage)
    {
        await Expect(_loginPage.ErrorMessage).ToHaveTextAsync(expectedMessage);
    }

    /// <summary>Logs out through the inventory page object.</summary>
    [When("I log out")]
    public async Task WhenILogOut()
    {
        await _inventoryPage.LogoutAsync();
    }

    /// <summary>Asserts that logout returned to the login page.</summary>
    [Then("I should see the login page")]
    public async Task ThenIShouldSeeTheLoginPage()
    {
        await Expect(_loginPage.Logo).ToHaveTextAsync(Constant.SwagLabsLogo);
    }
}
