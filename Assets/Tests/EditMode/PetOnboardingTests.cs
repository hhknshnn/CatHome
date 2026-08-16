using NUnit.Framework;

public sealed class PetOnboardingTests
{
    [TestCase(null, "")]
    [TestCase("   ", "")]
    [TestCase("  Misket  ", "Misket")]
    [TestCase("Çağrı", "Çağrı")]
    public void NormalizeName_TrimsAndSupportsUnicode(string input, string expected)
    {
        Assert.AreEqual(expected, CatDialogueView.NormalizeName(input));
    }

    [Test]
    public void NormalizeName_LimitsTextElementsInsteadOfUtf16Units()
    {
        string result = CatDialogueView.NormalizeName("1234567890123🐈extra");
        Assert.AreEqual("1234567890123🐈", result);
    }
}
