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
        {"JUMP ON SOFA","Koltuğa zıpla"},
        {"JUMP ON TABLE","Sehpaya zıpla"},
        {"PLAY YARN","Yumakla oyna"},
        {"POUNCE","Atıl"},
        {"PUSH","İt"},
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
    public static string ProductAction(string productId, string title, string action, bool needsEnergy)
    {
        if (needsEnergy) return title + "\n" + action;
        if (productId == HomeStoreService.BathroomLitterBoxId)
            return GameContentCopy.Text("Kum kabını kullan", "Use litter tray");
        if (GameLanguageService.Current != GameLanguage.Turkish) return action + " · " + title;
        // RINSE already translates to the complete Turkish phrase "Duş al".
        if (productId == HomeStoreService.BathroomShowerId) return action;
        return title + " " + action.ToLower(CultureInfo.GetCultureInfo("tr-TR"));
    }

    public static string Text(string value)
    {
        if(string.IsNullOrEmpty(value))return string.Empty;
        bool tr=GameLanguageService.Current==GameLanguage.Turkish;
        if(tr && Turkish.TryGetValue(value,out var translated))return translated;
        if(tr && value!=value.ToUpperInvariant())return value;
        string[] prefixes={"CATCH THE BALL  ","CATCH THE MOUSE  ","CHASE THE YARN  "};
        string[] labels=tr?new[]{"Topu yakala  ","Fareyi yakala  ","Yumağı yakala  "}:new[]{"Catch the ball  ","Catch the mouse  ","Chase the yarn  "};
        for(int i=0;i<prefixes.Length;i++)if(value.StartsWith(prefixes[i]))return labels[i]+value.Substring(prefixes[i].Length);
        return CultureInfo.GetCultureInfo("en-US").TextInfo.ToTitleCase(value.ToLowerInvariant()).Replace("...","…");
    }
}
