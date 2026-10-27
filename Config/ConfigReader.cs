using Microsoft.Extensions.Configuration;

namespace PlaywrightBdd.Config;

/// <summary>
/// Loads and caches validated framework settings from JSON, optional local credentials, and environment variables.
/// </summary>
public static class ConfigReader
{
    private static readonly Lazy<FrameworkSettings> _settings = new(LoadSettings);

    /// <summary>
    /// Returns the immutable settings snapshot used for the current test process.
    /// </summary>
    /// <returns>The validated framework settings.</returns>
    /// <exception cref="InvalidOperationException">Required settings are missing or invalid.</exception>
    public static FrameworkSettings Load() => _settings.Value;

    /// <summary>Builds the settings snapshot using JSON defaults, optional local credentials, and environment overrides.</summary>
    /// <returns>The validated settings snapshot.</returns>
    private static FrameworkSettings LoadSettings()
    {
        var credentialsPath = FindLocalCredentialsPath();
        var localCredentials = new ConfigurationBuilder()
            .SetBasePath(credentialsPath is null
                ? Directory.GetCurrentDirectory()
                : Path.GetDirectoryName(credentialsPath)!)
            .AddJsonFile("logincredentials.json", optional: true, reloadOnChange: false)
            .Build();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        var settingsConfiguration = configuration
            .AddEnvironmentVariables()
            .Build();

        var settings = settingsConfiguration.Get<FrameworkSettings>()
            ?? throw new InvalidOperationException("Application settings could not be loaded from appsettings.json.");

        var credentials = settings.TestCredentials with
        {
            Username = settings.TestCredentials.Username
                ?? localCredentials["TestCredentials:Username"]
                ?? localCredentials["Username"],
            Password = settings.TestCredentials.Password
                ?? localCredentials["TestCredentials:Password"]
                ?? localCredentials["Password"]
        };

        return (settings with { TestCredentials = credentials }).Validate();
    }

    private static string? FindLocalCredentialsPath()
    {
        var directories = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        foreach (var directory in directories)
        {
            for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
            {
                var candidate = Path.Combine(current.FullName, "logincredentials.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
