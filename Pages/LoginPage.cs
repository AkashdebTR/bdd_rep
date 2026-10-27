using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightBdd.Pages;

public sealed class LoginPage(IPage page) : BasePage(page)
{
    private ILocator UsernameInput => Page.GetByTestId("username");
    private ILocator PasswordInput => Page.GetByTestId("password");
    private ILocator LoginButton => Page.GetByTestId("login-button");

    public override string Path => "/";

    public ILocator Logo => Page.Locator(".login_logo");

    public override Task WaitForLoadedAsync() => Expect(Logo).ToBeVisibleAsync();

    public async Task<InventoryPage> LoginAsync(string username, string password)
    {
        await UsernameInput.FillAsync(username);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();

        var inventoryPage = new InventoryPage(Page);
        await inventoryPage.WaitForLoadedAsync();
        return inventoryPage;
    }
}