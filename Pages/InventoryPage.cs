using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightBdd.Pages;

public sealed class InventoryPage(IPage page) : BasePage(page)
{
    public override string Path => "/inventory.html";

    public ILocator Logo => Page.Locator(".app_logo");
    public ILocator Title => Page.GetByTestId("title");
    public ILocator InventoryContainer => Page.GetByTestId("inventory-container");

    public override Task WaitForLoadedAsync() => Expect(InventoryContainer).ToBeVisibleAsync();
}