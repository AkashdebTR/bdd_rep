namespace PlaywrightBdd.Config;

/// <summary>
/// Immutable application, browser, timeout, diagnostics, reporting, retry, and credential settings.
/// </summary>
public sealed record FrameworkSettings
{
    /// <summary>Gets the target application's absolute HTTP/HTTPS base URL.</summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>Gets the browser engine, channel, and headless settings.</summary>
    public BrowserSettings Browser { get; init; } = new();

    /// <summary>Gets the action, assertion, and navigation timeouts in milliseconds.</summary>
    public TimeoutSettings Timeouts { get; init; } = new();

    /// <summary>Gets screenshot, trace, and artifact output settings.</summary>
    public DiagnosticsSettings Diagnostics { get; init; } = new();

    /// <summary>Gets Extent report output settings.</summary>
    public ReportingSettings Reporting { get; init; } = new();

    /// <summary>Gets retry settings; positive dynamic retries are currently unsupported by this runner integration.</summary>
    public RetrySettings Retry { get; init; } = new();

    /// <summary>Gets optional test credentials, which are never emitted to diagnostics.</summary>
    public TestCredentials TestCredentials { get; init; } = new();

    /// <summary>
    /// Validates required settings and normalizes supported browser and trace values.
    /// </summary>
    /// <returns>A validated settings copy.</returns>
    /// <exception cref="InvalidOperationException">A required URL, browser, timeout, path, retry, or credential setting is invalid.</exception>
    public FrameworkSettings Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Configuration 'BaseUrl' must be an absolute HTTP or HTTPS URL.");
        }

        var engine = Browser.Engine.Trim().ToLowerInvariant();
        if (engine is not ("chromium" or "firefox" or "webkit"))
        {
            throw new InvalidOperationException("Configuration 'Browser:Engine' must be chromium, firefox, or webkit.");
        }

        if (engine != "chromium" && !string.IsNullOrWhiteSpace(Browser.Channel))
        {
            throw new InvalidOperationException("Configuration 'Browser:Channel' is supported only with the chromium engine. Clear the channel to use firefox or webkit.");
        }

        if (Timeouts.ActionMs <= 0 || Timeouts.AssertionMs <= 0 || Timeouts.NavigationMs <= 0)
        {
            throw new InvalidOperationException("All timeout settings must be positive values in milliseconds.");
        }

        var traceMode = Diagnostics.TraceMode.Trim().ToLowerInvariant();
        if (traceMode is not ("off" or "retain-on-failure"))
        {
            throw new InvalidOperationException("Configuration 'Diagnostics:TraceMode' must be 'off' or 'retain-on-failure'.");
        }

        if (string.IsNullOrWhiteSpace(Diagnostics.ArtifactDirectory))
        {
            throw new InvalidOperationException("Configuration 'Diagnostics:ArtifactDirectory' cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(Reporting.OutputDirectory))
        {
            throw new InvalidOperationException("Configuration 'Reporting:OutputDirectory' cannot be empty.");
        }

        if (Retry.MaxRetries is < 0 or > 5)
        {
            throw new InvalidOperationException("Configuration 'Retry:MaxRetries' must be between 0 and 5.");
        }

        if (string.IsNullOrWhiteSpace(TestCredentials.Username) != string.IsNullOrWhiteSpace(TestCredentials.Password))
        {
            throw new InvalidOperationException("Provide both TestCredentials:Username and TestCredentials:Password, or leave both unset.");
        }

        return this with
        {
            Browser = Browser with { Engine = engine },
            Diagnostics = Diagnostics with
            {
                TraceMode = traceMode,
                ScreenshotDirectory = Diagnostics.ScreenshotDirectory ?? Diagnostics.ArtifactDirectory,
                TraceDirectory = Diagnostics.TraceDirectory ?? Diagnostics.ArtifactDirectory
            }
        };
    }

    /// <summary>
    /// Rejects retries that this generated Reqnroll/NUnit integration cannot apply dynamically.
    /// </summary>
    /// <remarks>NUnit 4.3.2 retry attributes are static and method-level; configured positive retry counts are not silently ignored.</remarks>
    /// <exception cref="NotSupportedException">Retries are enabled with a positive maximum retry count.</exception>
    public void EnsureRetryPolicySupported()
    {
        if (Retry.Enabled && Retry.MaxRetries > 0)
        {
            throw new NotSupportedException("Dynamic retries are not supported for the generated Reqnroll NUnit tests in this project. NUnit RetryAttribute is static and method-only; leave Retry:Enabled false or set Retry:MaxRetries to 0.");
        }
    }

    /// <summary>
    /// Gets the credentials required by a login step.
    /// </summary>
    /// <returns>The username/password pair.</returns>
    /// <exception cref="InvalidOperationException">Either credential is missing.</exception>
    public TestCredentials RequireCredentials()
    {
        if (string.IsNullOrWhiteSpace(TestCredentials.Username) || string.IsNullOrWhiteSpace(TestCredentials.Password))
        {
            throw new InvalidOperationException("Login scenarios require TestCredentials__Username and TestCredentials__Password environment variables, or an optional local logincredentials.json file.");
        }

        return TestCredentials;
    }
}

/// <summary>Browser selection settings; channels are valid only for Chromium.</summary>
public sealed record BrowserSettings
{
    /// <summary>Gets the Playwright engine name: chromium, firefox, or webkit.</summary>
    public string Engine { get; init; } = "chromium";

    /// <summary>Gets an optional Chromium channel, such as the installed Chrome browser.</summary>
    public string? Channel { get; init; }

    /// <summary>Gets whether the browser runs without a visible window.</summary>
    public bool Headless { get; init; } = true;
}

/// <summary>Timeout values, in milliseconds, used by browser actions and explicit waits.</summary>
public sealed record TimeoutSettings
{
    /// <summary>Gets the default Playwright action timeout.</summary>
    public float ActionMs { get; init; } = 10000;

    /// <summary>Gets the default assertion and locator-state timeout.</summary>
    public float AssertionMs { get; init; } = 5000;

    /// <summary>Gets the default navigation timeout.</summary>
    public float NavigationMs { get; init; } = 30000;
}

/// <summary>Failure screenshot, trace, and output-directory settings.</summary>
public sealed record DiagnosticsSettings
{
    /// <summary>Gets whether to capture a screenshot on scenario failure.</summary>
    public bool CaptureScreenshotOnFailure { get; init; } = true;

    /// <summary>Gets the trace policy: off or retain-on-failure.</summary>
    public string TraceMode { get; init; } = "retain-on-failure";

    /// <summary>Gets the fallback root directory for diagnostic artifacts.</summary>
    public string ArtifactDirectory { get; init; } = "TestResults/artifacts";

    /// <summary>Gets the screenshot directory, or null to use <see cref="ArtifactDirectory"/>.</summary>
    public string? ScreenshotDirectory { get; init; }

    /// <summary>Gets the trace directory, or null to use <see cref="ArtifactDirectory"/>.</summary>
    public string? TraceDirectory { get; init; }
}

/// <summary>Extent report output settings.</summary>
public sealed record ReportingSettings
{
    /// <summary>Gets the directory where the run-scoped HTML report is written.</summary>
    public string OutputDirectory { get; init; } = "TestResults/Reports";
}

/// <summary>Retry configuration; positive retry counts are rejected by the current generated NUnit integration.</summary>
public sealed record RetrySettings
{
    /// <summary>Gets whether dynamic retries are requested.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets the maximum retry count, validated from zero through five.</summary>
    public int MaxRetries { get; init; } = 1;

    /// <summary>Gets the reserved per-failed-attempt screenshot option; dynamic retries are not currently available.</summary>
    public bool CaptureScreenshotOnEveryFailedAttempt { get; init; } = true;

    /// <summary>Gets the reserved trace-on-retry option; dynamic retries are not currently available.</summary>
    public bool TraceOnRetry { get; init; } = true;
}

/// <summary>Optional test-only username and password loaded from environment variables or ignored local configuration.</summary>
public sealed record TestCredentials
{
    /// <summary>Gets the test username, if configured.</summary>
    public string? Username { get; init; }

    /// <summary>Gets the test password, if configured.</summary>
    public string? Password { get; init; }
}
