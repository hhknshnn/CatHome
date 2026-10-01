using NUnit.Framework;

public sealed class PetOnboardingTests
{
    [TestCase(null, "")]
    [TestCase("   ", "")]
    [TestCase("  Misket  ", "Misket")]
    [TestCase("Çağrı", "Çağrı")]
    [TestCase("pamuk", "Pamuk")]
    [TestCase("ipek", "İpek")]
    [TestCase("ışık", "Işık")]
    [TestCase("çağrı", "Çağrı")]
    [TestCase("şeker", "Şeker")]
    [TestCase("pAMUK", "PAMUK")]
    [TestCase("pamuk prenses", "Pamuk prenses")]
    [TestCase("gu\u0308mu\u0308s\u0327", "Gümüş")]
    [TestCase("c\u0327ag\u0306rı", "Çağrı")]
    [TestCase("🐈minnoş", "🐈Minnoş")]
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

    [TestCase("en-US")]
    [TestCase("tr-TR")]
    public void NormalizeName_TurkishInitialDoesNotDependOnDeviceCulture(string culture)
    {
        var previous=System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo(culture);
            Assert.That(CatDialogueView.NormalizeName("ipek"),Is.EqualTo("İpek"));
            Assert.That(CatDialogueView.NormalizeName("ışık"),Is.EqualTo("Işık"));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture=previous; }
    }
}
