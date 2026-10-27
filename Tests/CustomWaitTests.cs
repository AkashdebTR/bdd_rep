using Microsoft.Playwright;
using PlaywrightBdd.Config;
using PlaywrightBdd.Utilities;

namespace PlaywrightBdd.Tests;

/// <summary>Exercises CustomWait against an isolated Playwright browser context.</summary>
[TestFixture]
public sealed class CustomWaitTests
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;
    private CustomWait _customWait = null!;

    [OneTimeSetUp]
    public async Task StartBrowserAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _playwright.Selectors.SetTestIdAttribute("data-test");
        var settings = ConfigReader.Load();
        var browserType = settings.Browser.Engine switch
        {
            "chromium" => _playwright.Chromium,
            "firefox" => _playwright.Firefox,
            "webkit" => _playwright.Webkit,
            _ => throw new InvalidOperationException($"Unsupported browser engine '{settings.Browser.Engine}'.")
        };

        _browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = settings.Browser.Engine == "chromium" ? settings.Browser.Channel : null,
            Headless = true
        });
    }

    [SetUp]
    public async Task CreatePageAsync()
    {
        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        _customWait = new CustomWait(_page, new TimeoutSettings
        {
            AssertionMs = 300,
            NavigationMs = 1000
        });
    }

    [TearDown]
    public async Task ClosePageAsync()
    {
        if (_context is not null)
        {
            await _context.CloseAsync();
        }
    }

    [OneTimeTearDown]
    public async Task CloseBrowserAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }

    [Test]
    public async Task WaitForPageReadyAsync_WaitsForDomAndApplicationLocator()
    {
        await _page.SetContentAsync("<main><h1 data-test='ready'>Ready</h1></main>");

        await _customWait.WaitForPageReadyAsync(_page.GetByTestId("ready"));

        Assert.That(await _page.GetByTestId("ready").IsVisibleAsync(), Is.True);
    }

    [Test]
    public async Task WaitForVisibleAsync_ThrowsWhenLocatorDoesNotBecomeVisible()
    {
        var exception = Assert.ThrowsAsync<TimeoutException>(async () =>
            await _customWait.WaitForVisibleAsync(_page.GetByTestId("missing")));

        Assert.That(exception!.Message, Does.Contain("Timeout"));
    }

    [Test]
    public async Task WaitForHiddenAsync_CompletesWhenLocatorIsAbsent()
    {
        await _customWait.WaitForHiddenAsync(_page.GetByTestId("missing"));
    }

    [Test]
    public async Task WaitForEnabledAsync_CompletesWhenControlIsEnabled()
    {
        await _page.SetContentAsync("<button data-test='enabled'>Continue</button>");

        await _customWait.WaitForEnabledAsync(_page.GetByTestId("enabled"));
    }
}
