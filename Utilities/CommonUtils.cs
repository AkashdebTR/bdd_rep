using System.Text.RegularExpressions;

namespace PlaywrightBdd.Utilities;

/// <summary>
/// Resolves shared CI and scenario metadata without holding mutable scenario state.
/// </summary>
public static partial class CommonUtils
{
    /// <summary>
    /// Gets the run identifier from supported CI variables, or <c>local</c> outside CI.
    /// </summary>
    /// <returns>The CI run identifier or the local fallback.</returns>
    public static string GetRunId() =>
        Environment.GetEnvironmentVariable("GITHUB_RUN_ID")
        ?? Environment.GetEnvironmentVariable("BUILD_BUILDID")
        ?? Environment.GetEnvironmentVariable("CI_JOB_ID")
        ?? "local";

    /// <summary>
    /// Gets the configured .NET environment name or <c>local</c> when none is set.
    /// </summary>
    /// <returns>The environment name used in test diagnostics.</returns>
    public static string GetEnvironmentName() =>
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? "local";

    /// <summary>
    /// Extracts the unique stable test-case ID from a scenario's Gherkin tags.
    /// </summary>
    /// <param name="scenarioTags">The tags supplied by Reqnroll, without the leading <c>@</c>.</param>
    /// <returns>The uppercase ID, for example <c>TC001</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="scenarioTags"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The scenario does not have exactly one <c>TC###</c> tag.</exception>
    public static string GetScenarioId(IEnumerable<string> scenarioTags)
    {
        ArgumentNullException.ThrowIfNull(scenarioTags);

        // Gherkin tags remain stable across reorderings; NUnit IDs are only runtime correlation keys.
        var scenarioIds = scenarioTags
            .Where(tag => ScenarioIdPattern().IsMatch(tag))
            .Select(tag => tag.ToUpperInvariant())
            .ToArray();

        if (scenarioIds.Length != 1)
        {
            throw new InvalidOperationException("Each Gherkin scenario must have exactly one stable @TC### scenario-ID tag.");
        }

        return scenarioIds[0];
    }

    [GeneratedRegex("^TC[0-9]{3,}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ScenarioIdPattern();
}
