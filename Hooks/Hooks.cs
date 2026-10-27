using Microsoft.Playwright;
using PlaywrightBdd.Config;
using Reqnroll;
using Reqnroll.BoDi;

namespace PlaywrightBdd.Hooks;

[Binding]
public sealed class Hooks
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private IPage? _page;

    [BeforeScenario]
    public async Task BeforeScenario(IObjectContainer container)
    {
        _playwright = await Playwright.CreateAsync();
        _playwright.Selectors.SetTestIdAttribute("data-test");
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = ConfigReader.Channel,
            Headless = ConfigReader.Headless
        });
        _browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = ConfigReader.BaseUrl
        });
        _page = await _browserContext.NewPageAsync();
        container.RegisterInstanceAs(_page);
    }

    [AfterScenario]
    public async Task AfterScenario(ScenarioContext scenarioContext)
    {
        if (scenarioContext.TestError is not null && _page is not null)
        {
            var screenshotDirectory = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(screenshotDirectory);
            var scenarioName = string.Join("_", scenarioContext.ScenarioInfo.Title.Split(Path.GetInvalidFileNameChars()));
            var screenshotPath = Path.Combine(screenshotDirectory, $"{scenarioName}.png");
            await _page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
        }

        if (_browserContext is not null)
        {
            await _browserContext.CloseAsync();
        }

        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }
}