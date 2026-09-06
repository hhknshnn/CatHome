public static class GameContentCopy
{
    public static string Text(string turkish,string english) => GameLanguageService.Current == GameLanguage.Turkish ? turkish : english;
    public static string RoomName(string id, string fallback)
    {
        bool tr = GameLanguageService.Current == GameLanguage.Turkish;
        switch (id)
        {
            case HomeRoomService.LivingRoomId: return tr ? "Salon" : "Living room";
            case HomeRoomService.BathroomId: return tr ? "Banyo" : "Bathroom";
            case HomeRoomService.KitchenId: return tr ? "Mutfak" : "Kitchen";
            case HomeRoomService.BedroomId: return tr ? "Yatak odası" : "Bedroom";
            case HomeRoomService.GardenId: return tr ? "Bahçe" : "Garden";
            case HomeRoomService.BalconyId: return tr ? "Balkon" : "Balcony";
            case HomeRoomService.PatioId: return tr ? "Avlu" : "Patio";
            case HomeRoomService.SecondFloorId: return tr ? "Üst kat" : "Upstairs";
            default: return fallback;
        }
    }
}
