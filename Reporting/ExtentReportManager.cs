using System.Collections.Concurrent;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using NUnit.Framework;
using PlaywrightBdd.Config;
using PlaywrightBdd.Utilities;

namespace PlaywrightBdd.Reporting;

/// <summary>
/// Owns the run-scoped ExtentSpark report and serializes scenario writes and the final flush.
/// </summary>
internal static class ExtentReportManager
{
    private static readonly object _syncRoot = new();
    private static readonly ConcurrentDictionary<string, ScenarioReportContext> _scenarios = new();
    private static ExtentReports? _extent;
    private static string? _reportPath;

    /// <summary>Creates or resumes a report entry for one NUnit scenario execution.</summary>
    /// <param name="testId">NUnit's runtime test ID, distinct from the stable Gherkin scenario ID.</param>
    /// <param name="scenarioId">The stable <c>TC###</c> scenario identifier.</param>
    /// <param name="scenarioName">The Gherkin scenario title.</param>
    /// <param name="tags">The scenario's Reqnroll tags.</param>
    /// <param name="settings">The validated run settings.</param>
    /// <returns>The report context, or null if reporting initialization failed.</returns>
    /// <remarks>Reporting errors are logged and do not prevent browser setup.</remarks>
    public static ScenarioReportContext? TryStartScenario(
        string testId,
        string scenarioId,
        string scenarioName,
        IEnumerable<string> tags,
        FrameworkSettings settings)
    {
        try
        {
            // ExtentReports is shared for the process; serialize writes with flush operations.
            lock (_syncRoot)
            {
                EnsureInitialized(settings.Reporting.OutputDirectory);
                var attemptNumber = TestContext.CurrentContext.CurrentRepeatCount + 1;
                var context = _scenarios.GetOrAdd(testId, _ =>
                {
                    var reportName = scenarioName.StartsWith($"{scenarioId} -", StringComparison.OrdinalIgnoreCase)
                        ? scenarioName
                        : $"{scenarioId} - {scenarioName}";
                    var scenario = _extent!.CreateTest(reportName);
                    var scenarioTags = tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray();
                    if (scenarioTags.Length > 0)
                    {
                        scenario.AssignCategory(scenarioTags);
                    }

                    scenario.Info($"Scenario ID: {scenarioId}");
                    scenario.Info($"Browser: {GetBrowserName(settings)}");
                    scenario.Info($"Environment: {CommonUtils.GetEnvironmentName()}");
                    scenario.Info($"Run ID: {CommonUtils.GetRunId()}");
                    return new ScenarioReportContext(
                        testId,
                        scenarioId,
                        scenario,
                        scenario.CreateNode($"Attempt {attemptNumber}"),
                        attemptNumber);
                });

                if (context.AttemptNumber != attemptNumber)
                {
                    context.AttemptNumber = attemptNumber;
                    context.AttemptTest = context.Test.CreateNode($"Attempt {attemptNumber}");
                }

                context.AttemptTest.Info($"Started: {DateTimeOffset.UtcNow:O}");
                context.AttemptTest.Info($"Retry policy enabled: {settings.Retry.Enabled}; max retries: {settings.Retry.MaxRetries}");
                return context;
            }
        }
        catch (Exception exception)
        {
            WriteReportingError("Could not create ExtentReports scenario entry", exception);
            return null;
        }
    }

    /// <summary>Records the attempt outcome and attaches any captured diagnostic evidence.</summary>
    /// <param name="context">The scenario report context, if report creation succeeded.</param>
    /// <param name="skipped">Whether the scenario was skipped.</param>
    /// <param name="exception">The scenario or setup exception, if failed.</param>
    /// <param name="screenshotPath">The captured screenshot path, if available.</param>
    /// <param name="tracePath">The exported trace path, if available.</param>
    public static void TryCompleteScenario(
        ScenarioReportContext? context,
        bool skipped,
        Exception? exception,
        string? screenshotPath,
        string? tracePath)
    {
        if (context is null)
        {
            return;
        }

        try
        {
            lock (_syncRoot)
            {
                context.AttemptTest.Info($"Finished: {DateTimeOffset.UtcNow:O}");
                if (!string.IsNullOrWhiteSpace(screenshotPath))
                {
                    context.AttemptTest.AddScreenCaptureFromPath(screenshotPath, "Failure screenshot");
                    context.Test.Info($"Screenshot: {screenshotPath}");
                }

                if (!string.IsNullOrWhiteSpace(tracePath))
                {
                    context.AttemptTest.Info($"Playwright trace: {tracePath}");
                    context.Test.Info($"Playwright trace: {tracePath}");
                }

                if (skipped)
                {
                    context.AttemptTest.Log(Status.Skip, "Scenario skipped or not executed.");
                    context.Test.Log(Status.Skip, "Scenario skipped or not executed.");
                    return;
                }

                if (exception is not null)
                {
                    var details = $"{exception.GetType().FullName}: {exception.Message}{Environment.NewLine}{exception}";
                    context.AttemptTest.Log(Status.Fail, details);
                    context.HadFailedAttempt = true;
                    context.Test.Log(Status.Fail, details);
                    return;
                }

                context.AttemptTest.Log(Status.Pass, "Attempt passed.");
                context.Test.Log(Status.Pass, context.HadFailedAttempt
                    ? "Passed after retry (flaky pass)."
                    : context.AttemptNumber > 1
                        ? "Passed after retry (flaky pass)."
                        : "Passed on the first attempt.");
            }
        }
        catch (Exception reportingException)
        {
            WriteReportingError("Could not finalize ExtentReports scenario entry", reportingException);
        }
    }

    /// <summary>Records a secondary screenshot, trace, or report diagnostic error.</summary>
    /// <param name="context">The scenario report context, if available.</param>
    /// <param name="stage">The diagnostic stage that failed.</param>
    /// <param name="exception">The secondary exception.</param>
    public static void TryRecordDiagnosticError(ScenarioReportContext? context, string stage, Exception exception)
    {
        if (context is null)
        {
            WriteReportingError(stage, exception);
            return;
        }

        try
        {
            lock (_syncRoot)
            {
                context.AttemptTest.Warning($"{stage}: {exception}");
            }
        }
        catch (Exception reportingException)
        {
            WriteReportingError(stage, reportingException);
        }
    }

    /// <summary>Flushes the shared report once at the Reqnroll test-run end hook.</summary>
    /// <remarks>Flush exceptions are logged instead of replacing an NUnit scenario result.</remarks>
    public static void TryFlush()
    {
        try
        {
            lock (_syncRoot)
            {
                _extent?.Flush();
                if (_reportPath is not null)
                {
                    TestContext.Progress.WriteLine($"Extent report: {_reportPath}");
                }
            }
        }
        catch (Exception exception)
        {
            WriteReportingError("Could not flush ExtentReports", exception);
        }
    }

    private static void EnsureInitialized(string configuredDirectory)
    {
        if (_extent is not null)
        {
            return;
        }

        var reportDirectory = ResolveOutputDirectory(configuredDirectory);
        HelperUtils.EnsureDirectoryExists(reportDirectory);
        var reportFileName = $"ExtentReport_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.html";
        _reportPath = Path.Combine(reportDirectory, reportFileName);

        var sparkReporter = new ExtentSparkReporter(_reportPath);
        _extent = new ExtentReports();
        _extent.AttachReporter(sparkReporter);
        _extent.AddSystemInfo("Environment", CommonUtils.GetEnvironmentName());
        _extent.AddSystemInfo("Run ID", CommonUtils.GetRunId());
    }

    private static string ResolveOutputDirectory(string configuredDirectory)
    {
        return HelperUtils.GetAbsolutePath(configuredDirectory);
    }

    private static string GetBrowserName(FrameworkSettings settings) =>
        settings.Browser.Engine == "chromium" && !string.IsNullOrWhiteSpace(settings.Browser.Channel)
            ? $"{settings.Browser.Engine} ({settings.Browser.Channel})"
            : settings.Browser.Engine;

    private static void WriteReportingError(string message, Exception exception)
    {
        TestContext.Progress.WriteLine($"ExtentReports warning: {message}: {exception}");
    }
}
