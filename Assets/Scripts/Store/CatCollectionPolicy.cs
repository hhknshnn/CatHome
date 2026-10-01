using UnityEngine;

/// <summary>Ownership is unlimited; the living room has five display slots and one bed.</summary>
public static class CatCollectionPolicy
{
    public const int Capacity = 5;
    public static bool IsCatItem(string id) => !HomeStoreService.IsRetiredProduct(id) && HomeStoreService.TryGetProduct(id, out var p) &&
        p.StoreCategory == HomeStoreCategory.Cat && p.IsPlaceable;
    public static bool IsBed(string id) => id == "cat.cozy-pod-bed" || id == "cat.cloud-bed" ||
        id == "cat.canopy-bed" || id == "cat.nap-pillow";
    public static int DisplayedCount
    {
        get { int count=0; foreach(var p in HomeStoreService.Products)
            if(IsCatItem(p.Id) && HomeStoreService.IsOwned(p.Id) && !HomeStoreService.IsStored(p.Id)) count++;
            return count; }
    }
    public static bool CanDisplay(string id, out string reason)
    {
        reason = string.Empty;
        if(HomeStoreService.IsRetiredProduct(id)) return false;
        if(!IsCatItem(id)) return true;
        int count=0; bool bed=false;
        foreach(var p in HomeStoreService.Products)
        {
            if(p.Id==id || !IsCatItem(p.Id) || !HomeStoreService.IsOwned(p.Id) || HomeStoreService.IsStored(p.Id)) continue;
            count++; bed |= IsBed(p.Id);
        }
        bool tr=GameLanguageService.Current==GameLanguage.Turkish;
        if(IsBed(id) && bed) reason=tr?"Odada bir yatak var. Önce onu koleksiyona kaldır.":"One bed is already in the room. Put it away first.";
        else if(count>=Capacity) reason=tr?"5 eşya odada. Yeni bir eşya eklemek için birini kaldır.":"5 items in the room. Put one away to add another.";
        return reason.Length==0;
    }
    public static string Summary => GameLanguageService.Current==GameLanguage.Turkish ?
        $"{DisplayedCount} / {Capacity} odada · En fazla 1 yatak" : $"{DisplayedCount} / {Capacity} in room · 1 bed maximum";
}
