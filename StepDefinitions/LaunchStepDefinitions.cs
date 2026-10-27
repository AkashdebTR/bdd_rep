using Microsoft.Playwright;
using NUnit.Framework;
using PlaywrightBdd.Config;
using PlaywrightBdd.Pages;
using Reqnroll;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightBdd.StepDefinitions;

[Binding]
public sealed class LaunchStepDefinitions(IPage page)
{
    private readonly LoginPage _loginPage = new(page);
    private InventoryPage? _inventoryPage;

    private InventoryPage InventoryPage => _inventoryPage
        ?? throw new InvalidOperationException("Log in before asserting the inventory page.");

    [Given("I launch the browser")]
    public void GivenILaunchTheBrowser()
    {
        Assert.That(page, Is.Not.Null);
    }

    [When("I navigate to the application home page")]
    public async Task WhenINavigateToTheApplicationHomePage()
    {
        await _loginPage.GotoAsync();
    }

    [Then("the login page should be displayed")]
    public async Task ThenTheLoginPageShouldBeDisplayed()
    {
        await Expect(_loginPage.Logo).ToHaveTextAsync(Constant.SwagLabsLogo);
    }

    [When("I log in with valid credentials")]
    public async Task WhenILogInWithValidCredentials()
    {
        _inventoryPage = await _loginPage.LoginAsync(ConfigReader.Username, ConfigReader.Password);
    }

    [Then("the inventory page should be displayed")]
    public async Task ThenTheInventoryPageShouldBeDisplayed()
    {
        await Expect(InventoryPage.Logo).ToHaveTextAsync(Constant.SwagLabsLogo);
        await Expect(InventoryPage.Title).ToHaveTextAsync(Constant.InventoryPageTitle);
        await Expect(InventoryPage.InventoryContainer).ToBeVisibleAsync();
    }
}