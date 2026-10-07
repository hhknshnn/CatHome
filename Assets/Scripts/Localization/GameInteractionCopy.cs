using System.Collections.Generic;
using System.Globalization;
public static class GameInteractionCopy
{
    private static readonly Dictionary<string,string> Turkish=new Dictionary<string,string>
    {
        {"BALANCE","Dengede yürü"},
        {"BASK","Dinlen"},
        {"CLIMB","Tırman"},
        {"CURL UP","Kıvrıl"},
        {"DIG","Eşele"},
        {"DIVE","İçine atla"},
        {"DRINK","Su iç"},
        {"EAT","Mama ye"},
        {"GAZE","Seyret"},
        {"GROOM","Taran"},
        {"HUNT","Avlan"},
        {"KNEAD","Yoğur"},
        {"LOOK","Bak"},
        {"NAP","Kestir"},
        {"REST","Dinlen"},
        {"NEST","Yerleş"},
        {"PAW","Pati at"},
        {"PERCH","Üzerine çık"},
        {"PLAY","Oyna"},
        {"PLAY BALL","Topla oyna"},
        {"JUMP ON SOFA","Koltuğa çık"},
        {"JUMP ON TABLE","Sehpaya çık"},
        {"PLAY YARN","Yumakla oyna"},
        {"POUNCE","Atıl"},
        {"PUSH","İt"},
        {"TIP BASKET","Sepeti devir"},
        {"TOSS THINGS","Eşyaları at"},
        {"RINSE","Duş al"},
        {"SCRATCH","Tırmala"},
        {"SHAKE","Salla"},
        {"SLEEP","Uyu"},
        {"STARE","İncele"},
        {"SUNBATHE","Güneşlen"},
        {"SWAT","Pati at"},
        {"SWAY","Sallan"},
        {"SWING","Sallan"},
        {"WARM UP","Isın"},
        {"WATCH","İzle"},
        {"BIRD WATCH","Kuşları izle"},
        {"ZOOM","Tünelden geç"},
        {"WAKE UP","Uyan"},
        {"ZOOMING THROUGH!","Tünelden geçiyor"},
        {"BALANCING...","Dengede yürüyor"},
        {"NAPPING...","Kestiriyor"},
        {"SWINGING!","Sallanıyor"},
        {"REACHING...","Uzanıyor"},
        {"DIVING...","İçine atlıyor"},
        {"SIPPING...","Su içiyor"},
        {"EXPLORING...","Keşfediyor"},
        {"DIGGING...","Eşeliyor"},
        {"PATTING...","Pati atıyor"},
        {"PUSHING...","İtiyor"},
        {"RINSING...","Duş alıyor"},
        {"WARMING UP...","Isınıyor"},
        {"KNEADING...","Yoğuruyor"},
        {"EATING...","Mama yiyor"},
        {"GROOMING...","Taranıyor"},
        {"SCRATCHING...","Tırmalıyor"},
        {"SPINNING...","Çeviriyor"},
        {"SETTLING...","Yerleşiyor"},
    };
    private static readonly Dictionary<string, string[]> ActivityTitles = new Dictionary<string, string[]>
    {
        { "BALL CHASE", new[] { "Top oyunu", "Ball play" } },
        { "DINING TABLE", new[] { "Yemek masası", "Dining table" } },
        { "BIRD WATCH", new[] { "Kuşlar", "Birds" } },
        { "SCRATCH POST", new[] { "Tırmalama tahtası", "Scratch post" } },
        { "Sehpa oyunu", new[] { "Sehpa", "Coffee table" } },
        { "Koltuk keyfi", new[] { "Koltuk", "Sofa" } },
    };
    public static bool TryActivityTitle(string value, out string copy)
    {
        copy = string.Empty;
        if (value == null || !ActivityTitles.TryGetValue(value, out var pair)) return false;
        copy = pair[GameLanguageService.Current == GameLanguage.Turkish ? 0 : 1];
        return true;
    }
    public static string ActivityTitle(string value) => TryActivityTitle(value, out string copy)
        ? copy : GameLanguageService.Text("product.unknown");

    public static string ProductAction(string productId, string title, string action, bool needsEnergy)
    {
        if (needsEnergy) return title + "\n" + action;
        if (productId == HomeStoreService.ScratchPostId) return GameContentCopy.Text("Tırmala", "Scratch");
        if (string.IsNullOrEmpty(productId) && (action == Action("JUMP ON SOFA") || action == Action("JUMP ON TABLE"))) return action;
        if (productId == "loft.record-player") return GameLanguageService.Text("record.action");
        if (productId == "patio.potted-ferns") return GameLanguageService.Text("interaction.fern.action");
        if (productId == HomeStoreService.BedroomWindowDaybedId) return GameContentCopy.Text("Divanda yat", "Nap on daybed");
        if (productId == HomeStoreService.BedroomYarnBasketId) return GameContentCopy.Text("Topla oyna", "Play with yarn ball");
        if (productId == HomeStoreService.BathroomLitterBoxId)
            return GameContentCopy.Text("Kum kabını kullan", "Use litter tray");
        if(productId==HomeStoreService.KitchenFruitBasketId)return GameContentCopy.Text("Sepeti devir","Tip the basket");
        if(string.IsNullOrEmpty(productId)&&action==Text("TOSS THINGS"))return GameContentCopy.Text("Masadan eşya at","Toss things off table");
        if (GameLanguageService.Current != GameLanguage.Turkish) return action + " · " + title;
        // RINSE already translates to the complete Turkish phrase "Duş al".
        if (productId == HomeStoreService.BathroomShowerId) return action;
        return title + " " + action.ToLower(CultureInfo.GetCultureInfo("tr-TR"));
    }

    // A title can contain a player-authored name; never lowercase unknown text.
    public static string Text(string value) => TryText(value, out string translated) ? translated : value ?? string.Empty;
    public static string Action(string value) => TryText(value, out string translated) ? translated :
        GameLanguageService.Text("interaction.action_fallback");
    public static string Status(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty :
        TryText(value, out string translated) ? translated : GameLanguageService.Text("interaction.status_fallback");

    public static bool TryText(string value, out string translated)
    {
        translated = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;
        bool tr = GameLanguageService.Current == GameLanguage.Turkish;
        if (KnownCopy.TryGetValue(value, out var pair))
        { translated = pair[tr ? 0 : 1]; return true; }
        foreach (var count in CountPrefixes)
            for (int i = 0; i < count.Length; i++)
                if (value.StartsWith(count[i], System.StringComparison.Ordinal))
                { translated = count[tr ? 1 : 2] + value.Substring(count[i].Length); return true; }
        return false;
    }
    private static string EnglishLabel(string token) => CultureInfo.GetCultureInfo("en-US").TextInfo
        .ToTitleCase(token.ToLowerInvariant()).Replace("...", "…");
    private static readonly string[][] StatusCopy = {
        new[] { "Dinleniyor · Enerji topluyor", "Resting · Recovering energy" },
        new[] { "Birlikte oyun zamanı", "Playtime together" },
        new[] { "Acaba düşer mi?", "Will it fall?" },
        new[] { "Koltuk keyfi", "Sofa time" },
        new[] { "Ortalığı dağıtıyor", "Making a mess" },
        new[] { "Yaramazlık peşinde", "Up to mischief" },
        new[] { "Patilerini toplayıp dinleniyor", "Loaf time" },
        new[] { "Birlikte oturalım", "Sit with me" },
        new[] { "Miyav!", "Meow!" },
        new[] { "Yaprakları inceliyor", "Watching the leaves" },
        new[] { "Seyrediyor", "Watching" },
    };
    private static readonly string[][] CountPrefixes = {
        new[] { "CATCH THE BALL  ", "Topu yakala  ", "Catch the ball  " },
        new[] { "CATCH THE MOUSE  ", "Fareyi yakala  ", "Catch the mouse  " },
        new[] { "CHASE THE YARN  ", "Yumağı yakala  ", "Chase the yarn  " },
        new[] { "Bat and chase · ", "Pati ve takip · ", "Bat and chase · " }
    };
    private static readonly Dictionary<string, string[]> KnownCopy = BuildKnownCopy();
    private static Dictionary<string, string[]> BuildKnownCopy()
    {
        var result = new Dictionary<string, string[]>(System.StringComparer.Ordinal);
        foreach (var item in Turkish)
        {
            var pair = new[] { item.Value, EnglishLabel(item.Key) };
            result[item.Key] = pair;
            if (!result.ContainsKey(pair[0])) result[pair[0]] = pair;
            if (!result.ContainsKey(pair[1])) result[pair[1]] = pair;
        }
        foreach (var pair in StatusCopy) { result[pair[0]] = pair; result[pair[1]] = pair; }
        return result;
    }

}
