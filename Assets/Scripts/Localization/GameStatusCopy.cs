using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>Presentation for the legacy store status vocabulary.</summary>
public static class GameStatusCopy
{
    private static readonly Dictionary<string,string[]> Copy=new Dictionary<string,string[]>
    {
        {"GET",new[]{"Edin","Get"}}, {"BUY",new[]{"Satın al","Buy"}},
        {"UNLOCK",new[]{"Aç","Unlock"}}, {"SAVE MORE",new[]{"Biraz biriktir","Save more"}},
        {"CURRENT ROOM",new[]{"Bu odadasın","Current room"}},
        {"IN YOUR ROOM",new[]{"Odanda","In your room"}},
        {"MOVE ITEM",new[]{"Yerleştir","Place"}},
        {"PREVIEW READY",new[]{"Odaya git","Visit room"}},
        {"COMING SOON",new[]{"Yakında","Coming soon"}},
        {"FREE TEST",new[]{"Ücretsiz deneme","Free test"}},
        {"THIS PRODUCT IS NOT AVAILABLE.",new[]{"Bu ürün şu anda kullanılamıyor.","This item is unavailable."}},
        {"YOU ALREADY OWN THIS ITEM.",new[]{"Bu eşya zaten senin.","You already own this item."}},
        {"ADDED TO ITS PLACE IN YOUR ROOM!",new[]{"Odandaki yerine eklendi!","Added to its place in your room!"}},
        {"THIS ITEM CANNOT BE MOVED RIGHT NOW.",new[]{"Bu eşya şu anda yerleştirilemiyor.","This item cannot be placed right now."}},
        {"PURCHASE COULD NOT BE COMPLETED. PLEASE TRY AGAIN.",new[]{"Satın alma tamamlanamadı. Yeniden dene.","Purchase didn’t complete. Try again."}},
        {"THIS ITEM COULD NOT BE ADDED IN TEST MODE.",new[]{"Eşya eklenemedi. Yeniden dene.","The item couldn’t be added. Try again."}},
        {"NO SCORES YET",new[]{"Henüz skor yok","No scores yet"}},
        {"RANKINGS UPDATED",new[]{"Sıralama güncellendi","Rankings updated"}},
        {"RANKINGS UNAVAILABLE",new[]{"Sıralamaya ulaşılamadı. Yeniden dene.","Rankings unavailable. Try again."}}
    };
    public static string Text(string value)
    {
        if(string.IsNullOrEmpty(value))return string.Empty;
        bool tr=GameLanguageService.Current==GameLanguage.Turkish;
        if(Copy.TryGetValue(value,out var copy))return copy[tr?0:1];
        foreach (var known in Copy.Values)
            if (value == known[0] || value == known[1]) return known[tr ? 0 : 1];
        var items=Regex.Match(value,@"^(\d+) ITEMS LEFT$");
        if(items.Success)return tr?$"{items.Groups[1]} eşya kaldı":$"{items.Groups[1]} items left";
        var level=Regex.Match(value,@"^HOME LV\. (\d+) REQUIRED$");
        if(level.Success)return tr?$"Ev seviyesi {level.Groups[1]}":$"Home level {level.Groups[1]}";
        if(value.EndsWith(" ADDED  •  FREE TEST MODE"))return value.Replace(" ADDED  •  FREE TEST MODE",tr?" eklendi · Ücretsiz deneme":" added · Free test");
        if(value.EndsWith(" ADDED TO YOUR HOME!"))return value.Replace(" ADDED TO YOUR HOME!",tr?" evine eklendi!":" added to your home!");
        return value;
    }
}
