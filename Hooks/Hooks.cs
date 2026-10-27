using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using PlaywrightBdd.Config;
using PlaywrightBdd.Reporting;
using PlaywrightBdd.Utilities;
using Reqnroll;
using Reqnroll.BoDi;

namespace PlaywrightBdd.Hooks;

/// <summary>
/// Owns scenario browser resources and coordinates report, screenshot, trace, and cleanup lifecycle.
/// </summary>
[Binding]
public sealed class Hooks
{
    private FrameworkSettings? _settings;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private IPage? _page;
    private bool _traceStarted;
    private string _setupStage = "configuration";
    private Exception? _setupException;
    private ScenarioReportContext? _reportContext;
    private string _scenarioId = "TC-UNASSIGNED";

    /// <summary>
    /// Loads settings, validates the scenario ID, and creates a fresh browser context and page.
    /// </summary>
    /// <param name="scenarioContext">Reqnroll scenario metadata and execution state.</param>
    /// <param name="container">The scenario-scoped dependency container.</param>
    /// <returns>A task completed after browser setup or a setup exception is reported.</returns>
    /// <remarks>Any resources created before a setup failure are disposed before the exception is rethrown.</remarks>
    [BeforeScenario]
    public async Task BeforeScenario(ScenarioContext scenarioContext, IObjectContainer container)
    {
        try
        {
            // Keep the Gherkin ID stable in outputs; NUnit's test ID stays internal to report correlation.
            _scenarioId = CommonUtils.GetScenarioId(scenarioContext.ScenarioInfo.Tags);
            _settings = ConfigReader.Load();
            _reportContext = ExtentReportManager.TryStartScenario(
                TestContext.CurrentContext.Test.ID,
                _scenarioId,
                scenarioContext.ScenarioInfo.Title,
                scenarioContext.ScenarioInfo.Tags,
                _settings);
            _settings.EnsureRetryPolicySupported();
            container.RegisterInstanceAs(_settings);

            _setupStage = "Playwright initialization";
            _playwright = await Playwright.CreateAsync();
            _playwright.Selectors.SetTestIdAttribute("data-test");

            _setupStage = "browser launch";
            var browserType = _settings.Browser.Engine switch
            {
                "chromium" => _playwright.Chromium,
                "firefox" => _playwright.Firefox,
                "webkit" => _playwright.Webkit,
                _ => throw new InvalidOperationException($"Unsupported browser engine '{_settings.Browser.Engine}'.")
            };

            _browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Channel = _settings.Browser.Engine == "chromium" ? _settings.Browser.Channel : null,
                Headless = _settings.Browser.Headless
            });

            _setupStage = "browser context creation";
            _browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                BaseURL = _settings.BaseUrl
            });
            _browserContext.SetDefaultTimeout(_settings.Timeouts.ActionMs);
            _browserContext.SetDefaultNavigationTimeout(_settings.Timeouts.NavigationMs);
            Assertions.SetDefaultExpectTimeout(_settings.Timeouts.AssertionMs);

            _setupStage = "page creation";
            _page = await _browserContext.NewPageAsync();
            container.RegisterInstanceAs(_page);

            if (_settings.Diagnostics.TraceMode == "retain-on-failure")
            {
                _setupStage = "trace initialization";
                await _browserContext.Tracing.StartAsync(new TracingStartOptions
                {
                    Title = scenarioContext.ScenarioInfo.Title,
                    Screenshots = true,
                    Snapshots = true,
                    Sources = false
                });
                _traceStarted = true;
            }
        }
        catch (Exception setupException)
        {
            _setupException = setupException;
            _reportContext ??= ExtentReportManager.TryStartScenario(
                TestContext.CurrentContext.Test.ID,
                _scenarioId,
                scenarioContext.ScenarioInfo.Title,
                scenarioContext.ScenarioInfo.Tags,
                _settings ?? new FrameworkSettings());
            var cleanupErrors = new List<Exception>();
            await DisposeResourcesAsync(cleanupErrors);
            ReportSecondaryErrors(cleanupErrors);

            if (_setupStage == "browser launch" && setupException is PlaywrightException)
            {
                throw new InvalidOperationException(
                    $"Could not launch the configured {_settings!.Browser.Engine} browser. Install it with the Playwright install script or configure an installed Chromium channel.",
                    setupException);
            }

            throw;
        }
    }

    /// <summary>
    /// Captures configured failure evidence, records the final scenario status, and releases owned resources.
    /// </summary>
    /// <param name="scenarioContext">Reqnroll scenario result and exception state.</param>
    /// <returns>A task completed after evidence handling and cleanup.</returns>
    /// <remarks>Screenshot, trace, or cleanup failures are secondary to the original scenario error.</remarks>
    [AfterScenario]
    public async Task AfterScenario(ScenarioContext scenarioContext)
    {
        var cleanupErrors = new List<Exception>();
        var scenarioError = scenarioContext.TestError ?? _setupException;
        var skipped = scenarioError is null
            && (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Skipped
                || scenarioContext.ScenarioExecutionStatus.ToString().Equals("Skipped", StringComparison.OrdinalIgnoreCase));
        var failed = scenarioError is not null || TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;
        string? screenshotPath = null;
        string? tracePath = null;

        if (failed)
        {
            try
            {
                screenshotPath = await CaptureFailureScreenshotAsync(scenarioContext.ScenarioInfo.Title);
            }
            catch (Exception exception)
            {
                screenshotPath = null;
                cleanupErrors.Add(exception);
                ExtentReportManager.TryRecordDiagnosticError(_reportContext, "Screenshot capture failed", exception);
            }
        }

        if (_traceStarted)
        {
            try
            {
                tracePath = await CaptureTraceAsync(failed, scenarioContext.ScenarioInfo.Title);
            }
            catch (Exception exception)
            {
                tracePath = null;
                cleanupErrors.Add(exception);
                ExtentReportManager.TryRecordDiagnosticError(_reportContext, "Trace export failed", exception);
            }
        }

        await DisposeResourcesAsync(cleanupErrors);

        // Preserve the original test exception; diagnostics and cleanup errors are secondary.
        var finalError = scenarioError;
        if (finalError is null && failed && !skipped)
        {
            var result = TestContext.CurrentContext.Result;
            finalError = new InvalidOperationException(
                $"NUnit reported a failed scenario without a Reqnroll exception. {result.Message}{Environment.NewLine}{result.StackTrace}");
        }

        if (finalError is null && cleanupErrors.Count > 0)
        {
            finalError = new AggregateException("Scenario diagnostics or browser cleanup failed.", cleanupErrors);
        }

        ExtentReportManager.TryCompleteScenario(_reportContext, skipped, finalError, screenshotPath, tracePath);

        if (scenarioError is not null)
        {
            ReportSecondaryErrors(cleanupErrors);
        }
        else if (cleanupErrors.Count > 0)
        {
            throw finalError!;
        }
    }

    /// <summary>Captures and logs a unique screenshot when failure capture is enabled and a page exists.</summary>
    /// <param name="scenarioTitle">The scenario title used in the artifact filename.</param>
    /// <returns>The screenshot path, or null when capture is disabled or no page was created.</returns>
    /// <exception cref="Microsoft.Playwright.PlaywrightException">Playwright cannot capture the current page.</exception>
    private async Task<string?> CaptureFailureScreenshotAsync(string scenarioTitle)
    {
        if (_settings?.Diagnostics.CaptureScreenshotOnFailure != true || _page is null)
        {
            return null;
        }

        var screenshotPath = HelperUtils.CreateUniqueArtifactPath(
            _settings.Diagnostics.ScreenshotDirectory!,
            _scenarioId,
            scenarioTitle,
            "failure.png",
            CommonUtils.GetRunId());
        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
        TestContext.Progress.WriteLine($"Failure screenshot: {screenshotPath}");
        return screenshotPath;
    }

    /// <summary>Stops tracing and exports a unique trace only when the scenario failed.</summary>
    /// <param name="failed">Whether the scenario failed.</param>
    /// <param name="scenarioTitle">The scenario title used in the artifact filename.</param>
    /// <returns>The exported trace path, or null when a successful scenario's trace is discarded.</returns>
    /// <exception cref="Microsoft.Playwright.PlaywrightException">Playwright cannot stop or export the trace.</exception>
    private async Task<string?> CaptureTraceAsync(bool failed, string scenarioTitle)
    {
        if (!_traceStarted || _browserContext is null)
        {
            return null;
        }

        var tracePath = failed && _settings is not null
            ? HelperUtils.CreateUniqueArtifactPath(
                _settings.Diagnostics.TraceDirectory!,
                _scenarioId,
                scenarioTitle,
                "trace.zip",
                CommonUtils.GetRunId())
            : null;

        try
        {
            await _browserContext.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
            if (tracePath is not null && File.Exists(tracePath))
            {
                TestContext.Progress.WriteLine($"Playwright trace: {tracePath}");
                return tracePath;
            }

            return null;
        }
        finally
        {
            _traceStarted = false;
        }
    }

    /// <summary>Flushes the shared Extent report once after the Reqnroll test run.</summary>
    [AfterTestRun]
    public static void AfterTestRun() => ExtentReportManager.TryFlush();

    private async Task DisposeResourcesAsync(List<Exception> errors)
    {
        if (_traceStarted && _browserContext is not null)
        {
            await TryCleanupAsync(() => _browserContext.Tracing.StopAsync(), errors);
            _traceStarted = false;
        }

        if (_browserContext is not null)
        {
            await TryCleanupAsync(() => _browserContext.CloseAsync(), errors);
            _browserContext = null;
        }

        if (_browser is not null)
        {
            await TryCleanupAsync(() => _browser.CloseAsync(), errors);
            _browser = null;
        }

        if (_playwright is not null)
        {
            try
            {
                _playwright.Dispose();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            _playwright = null;
        }
    }

    /// <summary>Attempts one asynchronous cleanup action and appends any exception for secondary reporting.</summary>
    /// <param name="cleanup">The resource cleanup action.</param>
    /// <param name="errors">The list receiving cleanup exceptions.</param>
    /// <returns>A task completed after the cleanup attempt.</returns>
    private static async Task TryCleanupAsync(Func<Task> cleanup, List<Exception> errors)
    {
        try
        {
            await cleanup();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
    }

    /// <summary>Writes secondary cleanup errors without replacing the scenario's original failure.</summary>
    /// <param name="errors">The secondary exceptions to report.</param>
    private static void ReportSecondaryErrors(IEnumerable<Exception> errors)
    {
        foreach (var error in errors)
        {
            TestContext.Progress.WriteLine($"Secondary browser cleanup/diagnostic error: {error}");
        }
    }

}
