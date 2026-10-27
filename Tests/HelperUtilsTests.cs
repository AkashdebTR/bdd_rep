using PlaywrightBdd.Utilities;

namespace PlaywrightBdd.Tests;

/// <summary>Validates filename sanitization and artifact path safety.</summary>
[TestFixture]
public sealed class HelperUtilsTests
{
    [TestCase("login: /invalid?name", "login-invalid-name")]
    [TestCase("   ", "scenario")]
    [TestCase("...", "scenario")]
    public void SanitizeFileName_ReplacesInvalidOrEmptyValues(string value, string expected)
    {
        Assert.That(HelperUtils.SanitizeFileName(value), Is.EqualTo(expected));
    }

    [Test]
    public void CreateUniqueArtifactPath_CreatesNestedDirectoriesAndDoesNotCollide()
    {
        var root = Path.Combine(Path.GetTempPath(), $"playwright-bdd-{Guid.NewGuid():N}");
        var directory = Path.Combine(root, "nested", "artifacts");

        try
        {
            var firstPath = HelperUtils.CreateUniqueArtifactPath(directory, "TC001", "login / scenario", "failure.png", "test-run");
            var secondPath = HelperUtils.CreateUniqueArtifactPath(directory, "TC001", "login / scenario", "failure.png", "test-run");
            var titleWithIdPath = HelperUtils.CreateUniqueArtifactPath(directory, "TC001", "TC001 - login scenario", "failure.png", "test-run");

            Assert.Multiple(() =>
            {
                Assert.That(firstPath, Is.Not.EqualTo(secondPath));
                Assert.That(Path.GetFileName(firstPath), Does.EndWith("-failure.png"));
                Assert.That(Directory.Exists(Path.GetDirectoryName(firstPath)), Is.True);
                Assert.That(firstPath, Does.Contain("TC001-login-scenario"));
                Assert.That(Path.GetFileName(titleWithIdPath), Does.StartWith("TC001-login-scenario-"));
            });
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Test]
    public void EnsureDirectoryExists_CreatesNestedDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"playwright-bdd-{Guid.NewGuid():N}", "nested");
        var root = Directory.GetParent(directory)!.FullName;

        try
        {
            HelperUtils.EnsureDirectoryExists(directory);
            Assert.That(Directory.Exists(directory), Is.True);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
