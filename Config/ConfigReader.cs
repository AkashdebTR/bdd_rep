using Microsoft.Extensions.Configuration;

namespace PlaywrightBdd.Config;

public static class ConfigReader
{
    private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .Build();

    private static readonly IConfigurationRoot CredentialsConfiguration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("logincredentials.json", optional: false, reloadOnChange: false)
        .Build();

    private static readonly BrowserSettings Browser = Configuration.GetSection("Browser").Get<BrowserSettings>()
        ?? throw new InvalidOperationException("Browser settings are missing from appsettings.json.");

    private static readonly LoginCredentials Credentials = CredentialsConfiguration.Get<LoginCredentials>()
        ?? throw new InvalidOperationException("Login credentials are missing from logincredentials.json.");

    public static string BaseUrl { get; } = Configuration["BaseUrl"]
        ?? throw new InvalidOperationException("BaseUrl is missing from appsettings.json.");

    public static string Channel { get; } = Browser.Channel
        ?? throw new InvalidOperationException("Browser:Channel is missing from appsettings.json.");

    public static bool Headless { get; } = Browser.Headless;

    public static string Username { get; } = Credentials.Username
        ?? throw new InvalidOperationException("Username is missing from logincredentials.json.");

    public static string Password { get; } = Credentials.Password
        ?? throw new InvalidOperationException("Password is missing from logincredentials.json.");

    private sealed class BrowserSettings
    {
        public string? Channel { get; init; }
        public bool Headless { get; init; }
    }

    private sealed class LoginCredentials
    {
        public string? Username { get; init; }
        public string? Password { get; init; }
    }
}