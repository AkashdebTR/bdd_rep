using AventStack.ExtentReports;

namespace PlaywrightBdd.Reporting;

/// <summary>
/// Holds one scenario's report entry and the current NUnit attempt node.
/// </summary>
internal sealed class ScenarioReportContext(
    string testId,
    string scenarioId,
    ExtentTest test,
    ExtentTest attemptTest,
    int attemptNumber)
{
    /// <summary>Gets the NUnit runtime test ID used to associate repeated executions.</summary>
    public string TestId { get; } = testId;

    /// <summary>Gets the stable Gherkin scenario ID.</summary>
    public string ScenarioId { get; } = scenarioId;

    /// <summary>Gets the root Extent scenario entry.</summary>
    public ExtentTest Test { get; } = test;

    /// <summary>Gets or sets the Extent node for the current attempt.</summary>
    public ExtentTest AttemptTest { get; set; } = attemptTest;

    /// <summary>Gets or sets the one-based NUnit repeat count.</summary>
    public int AttemptNumber { get; set; } = attemptNumber;

    /// <summary>Gets or sets whether an earlier attempt failed.</summary>
    public bool HadFailedAttempt { get; set; }
}
