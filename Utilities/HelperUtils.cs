using System.Text.RegularExpressions;

namespace PlaywrightBdd.Utilities;

/// <summary>
/// Provides filesystem and naming helpers for reports and diagnostic artifacts.
/// </summary>
public static partial class HelperUtils
{
    /// <summary>
    /// Replaces characters that are unsafe for a portable artifact filename.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>A safe filename component, or <c>scenario</c> when the value is empty.</returns>
    public static string SanitizeFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "scenario";
        }

        var sanitized = InvalidFileNameCharacters().Replace(value.Trim(), "-").Trim('-', '.', ' ');
        return string.IsNullOrWhiteSpace(sanitized) ? "scenario" : sanitized;
    }

    /// <summary>
    /// Creates a unique artifact path containing the run, stable scenario ID, and scenario title.
    /// </summary>
    /// <param name="directory">The absolute directory or project-root-relative output directory.</param>
    /// <param name="scenarioId">The stable Gherkin scenario ID, such as <c>TC001</c>.</param>
    /// <param name="scenarioName">The human-readable scenario title.</param>
    /// <param name="fileName">A filename without directory segments.</param>
    /// <param name="runId">The CI run ID, or <c>local</c> when omitted.</param>
    /// <returns>A unique full path whose parent directory has been created.</returns>
    /// <exception cref="ArgumentException">A required path component is empty or the filename contains directory segments.</exception>
    public static string CreateUniqueArtifactPath(
        string directory,
        string scenarioId,
        string scenarioName,
        string fileName,
        string? runId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName)
            || !string.Equals(safeFileName, fileName, StringComparison.Ordinal)
            || safeFileName is "." or "..")
        {
            throw new ArgumentException("The artifact file name must be a simple file name without directory segments.", nameof(fileName));
        }

        safeFileName = SanitizeFileName(safeFileName);
        var safeScenarioId = SanitizeFileName(scenarioId);
        var safeScenarioName = SanitizeFileName(scenarioName);
        var scenarioIdTitlePrefix = $"{safeScenarioId}-";
        if (safeScenarioName.StartsWith(scenarioIdTitlePrefix, StringComparison.OrdinalIgnoreCase))
        {
            safeScenarioName = safeScenarioName[scenarioIdTitlePrefix.Length..].Trim('-');
        }

        if (string.IsNullOrWhiteSpace(safeScenarioName))
        {
            safeScenarioName = "scenario";
        }

        var artifactDirectory = Path.Combine(GetAbsolutePath(directory), SanitizeFileName(runId));
        EnsureDirectoryExists(artifactDirectory);
        // A GUID per artifact prevents collisions when a scenario or retry repeats with the same title.
        var uniqueFileName = $"{safeScenarioId}-{safeScenarioName}-{Guid.NewGuid():N}-{safeFileName}";
        return Path.Combine(artifactDirectory, uniqueFileName);
    }

    /// <summary>
    /// Creates the specified absolute or project-root-relative directory, including missing parents.
    /// </summary>
    /// <param name="directory">The directory path to create.</param>
    /// <exception cref="ArgumentException">The directory path is empty or whitespace.</exception>
    public static void EnsureDirectoryExists(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(GetAbsolutePath(directory));
    }

    /// <summary>
    /// Resolves an absolute path or a path relative to the discovered project root.
    /// </summary>
    /// <param name="path">The absolute or project-root-relative path.</param>
    /// <returns>The normalized absolute path.</returns>
    /// <exception cref="ArgumentException">The path is empty or whitespace.</exception>
    public static string GetAbsolutePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(FindProjectRoot(), path));
    }

    private static string FindProjectRoot()
    {
        foreach (var startDirectory in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var current = new DirectoryInfo(startDirectory); current is not null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "PlaywrightBdd.csproj")))
                {
                    return current.FullName;
                }
            }
        }

        return Directory.GetCurrentDirectory();
    }

    [GeneratedRegex("[^A-Za-z0-9._-]+", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidFileNameCharacters();
}
