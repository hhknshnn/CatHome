using NUnit.Framework;

public sealed class LevelContentValidationTests
{
    [Test]
    public void ProjectArchitecture_IsValidForLevelDevelopment()
    {
        LevelValidationReport report = LevelContentValidator.ValidateProject();
        Assert.That(report.IsValid, Is.True, report.ToString());
    }
}
