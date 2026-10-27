using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightBdd.Pages;

public abstract class BasePage
{
    protected BasePage(IPage page)
    {
        Page = page;
    }

    protected IPage Page { get; }

    public abstract string Path { get; }

    public async Task GotoAsync()
    {
        await Page.GotoAsync(Path, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await WaitForLoadedAsync();
    }

    public abstract Task WaitForLoadedAsync();
}