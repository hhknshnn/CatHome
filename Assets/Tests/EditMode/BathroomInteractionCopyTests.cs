using NUnit.Framework;

public sealed class BathroomInteractionCopyTests
{
    [Test]
    public void ShowerPromptUsesItsCompleteVerb_WithoutRepeatingTheProductOrChangingOtherRooms()
    {
        var original = GameLanguageService.Current;
        try
        {
            GameLanguageService.SetLanguage(GameLanguage.Turkish);
            Assert.That(GameInteractionCopy.ProductAction(HomeStoreService.BathroomShowerId, "Duş", GameInteractionCopy.Text("RINSE"), false), Is.EqualTo("Duş al"));
            Assert.That(GameInteractionCopy.ProductAction("other", "TV", "İzle", false), Is.EqualTo("TV izle"));
            Assert.That(GameInteractionCopy.ProductAction("other", "Kitaplık", "İncele", false), Is.EqualTo("Kitaplık incele"));
            Assert.That(GameInteractionCopy.ProductAction(HomeStoreService.BathroomShowerId, "Duş", "Enerji gerekli", true), Is.EqualTo("Duş\nEnerji gerekli"));
            GameLanguageService.SetLanguage(GameLanguage.English);
            Assert.That(GameInteractionCopy.ProductAction(HomeStoreService.BathroomShowerId, "Shower", "Rinse", false), Is.EqualTo("Rinse · Shower"));
        }
        finally { GameLanguageService.SetLanguage(original); }
    }
}
