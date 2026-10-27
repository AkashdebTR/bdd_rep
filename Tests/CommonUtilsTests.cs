using System.Text.RegularExpressions;
using PlaywrightBdd.Utilities;

namespace PlaywrightBdd.Tests;

/// <summary>Validates stable scenario-ID extraction and feature tag uniqueness.</summary>
[TestFixture]
public sealed class CommonUtilsTests
{
    [Test]
    public void GetScenarioId_ReturnsStableIdFromTags()
    {
        var scenarioId = CommonUtils.GetScenarioId(["smoke", "tc012"]);

        Assert.That(scenarioId, Is.EqualTo("TC012"));
    }

    [TestCase("smoke")]
    [TestCase("TC001", "TC002")]
    public void GetScenarioId_RejectsMissingOrAmbiguousIds(params string[] tags)
    {
        Assert.Throws<InvalidOperationException>(() => CommonUtils.GetScenarioId(tags));
    }

    [Test]
    public void FeatureScenarioIds_AreUniqueAcrossFeatureFiles()
    {
        var featureDirectory = HelperUtils.GetAbsolutePath("Features");
        var scenarioIds = Directory.EnumerateFiles(featureDirectory, "*.feature", SearchOption.AllDirectories)
            .SelectMany(File.ReadLines)
            .SelectMany(line => Regex.Matches(line, "@(?<id>TC[0-9]{3,})", RegexOptions.IgnoreCase)
                .Select(match => match.Groups["id"].Value.ToUpperInvariant()))
            .ToArray();

        Assert.That(scenarioIds, Is.Not.Empty);
        Assert.That(scenarioIds, Is.Unique);
    }
}
