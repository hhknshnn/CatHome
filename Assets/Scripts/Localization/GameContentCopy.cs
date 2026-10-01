using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class GameContentCopy
{
    public static string Text(string turkish, string english) => GameLanguageService.Current == GameLanguage.Turkish ? turkish : english;

    // Scene/prefab tokens and both rendered languages resolve through one table.
    // Unknown gameplay speech never falls back to English or an internal ID.
    public static string CatReaction(string message) => string.IsNullOrWhiteSpace(message) ? string.Empty :
        TryCatReaction(message, out string copy) ? copy : GameLanguageService.Text("interaction.feedback_fallback");

    public static bool TryCatReaction(string message, out string copy)
    {
        copy = string.Empty;
        if (string.IsNullOrWhiteSpace(message)) return false;
        if (ReactionKeys.TryGetValue(message, out string key))
        { copy = GameLanguageService.Text(key); return true; }
        if (Reactions.TryGetValue(message, out string[] pair))
        { copy = Text(pair[0], pair[1]); return true; }
        var bond = Regex.Match(message, @"^NEEDS (\d+) BOND$");
        if (bond.Success)
        { copy = GameLanguageService.Format("interaction.bond_required", bond.Groups[1].Value); return true; }
        return false;
    }

    private static readonly Dictionary<string, string[]> Reactions = BuildReactions();
    private static readonly Dictionary<string, string> ReactionKeys = BuildReactionKeys();
    private static Dictionary<string, string[]> BuildReactions()
    {
        var result = new Dictionary<string, string[]>(System.StringComparer.Ordinal);
        Add(result, "SWEET DREAMS!", "Mışıl mışıl dinlendim.", "Sweet dreams.");
        Add(result, "SWEET DREAMS.", "Tatlı rüyalar.", "Sweet dreams.");
        Add(result, "SEED RAIN!", "Kuşlara küçük bir ziyafet!", "A little feast for the birds!");
        Add(result, "IT MOVES!", "Bak, hareket ediyor!", "Look, it moves!");
        Add(result, "YARN CHAMPION!", "Yumağı yakaladım!", "Got the yarn!");
        Add(result, "YUM!", "Nefis!", "Yum!");
        Add(result, "ALL COVERED UP!", "İşte, tertemiz.", "All covered up.");
        Add(result, "LITTER_STINKY", "Off, koktu!", "Phew, that smells!");
        Add(result, "LITTER_PEE", "Çiş yaptım, hehe!", "Had a wee, hehe!");
        Add(result, "FOUND ME!", "Beni buldun!", "You found me!");
        Add(result, "MAKING BISCUITS!", "Patilerim iş başında.", "Making biscuits.");
        Add(result, "OOPS.", "Ben bir şey yapmadım…", "It wasn’t me…");
        Add(result, "SO FLUFFY!", "Yumuşacık oldum.", "So fluffy.");
        Add(result, "MIGHTY HUNTER!", "Yakaladım seni!", "Got you!");
        Add(result, "TOASTY!", "Sıcacık bir köşe.", "A cosy warm spot.");
        Add(result, "PAPER EVERYWHERE!", "Biraz dağıttım galiba…", "I may have made a little mess…");
        Add(result, "REFRESHING!", "Oh, ferahladım.", "So refreshing.");
        Add(result, "SQUEAKY CLEAN!", "Pırıl pırılım.", "All clean.");
        Add(result, "CLAWS FEEL GREAT!", "Patilerime iyi geldi.", "My paws feel great.");
        Add(result, "NOTHING UP HERE!", "Burası da benim oldu.", "Another spot to explore.");
        Add(result, "WHEEE!", "Biraz daha sallanalım mı?", "Another little swing?");
        Add(result, "TUNNEL CHAMPION!", "Öbür taraftan çıktım!", "Made it through!");
        Add(result, "STILL DRY!", "Patilerim hâlâ kuru.", "Still dry.");
        Add(result, "TUB_BALANCE", "Düşmedim… bilerek sallandım!", "Didn't fall… that wobble was on purpose!");
        Add(result, "GOOD SPOT!", "Ne güzel bir köşe.", "Such a lovely spot.");
        Add(result, "SO COZY!", "Tam bana göre.", "So cosy.");
        Add(result, "WHAT A VIEW!", "Manzaraya bak!", "What a view!");
        Add(result, "HELLO BIRDS!", "Merhaba kuşlar!", "Hello, birds!");
        Add(result, "BEST SEAT!", "En güzel yer benim.", "The best seat is mine.");
        Add(result, "CAFE NAP!", "Küçük bir kafe molası.", "A little cafe nap.");
        Add(result, "GOT IT!", "Yakaladım!", "Got it!");
        Add(result, "GOT THE YARN!", "Yumak bende!", "Got the yarn!");
        Add(result, "MY COZY CHAIR!", "Benim rahat koltuğum.", "My cosy chair.");
        Add(result, "MY DESK!", "Masa artık benim.", "My desk now.");
        Add(result, "MY POUFFE!", "Pufuma yerleştim.", "My cosy pouffe.");
        Add(result, "MY STOOL!", "Burayı sevdim.", "I like this seat.");
        Add(result, "OPEN IT!", "İçinde ne var acaba?", "What’s inside?");
        Add(result, "PRETTY!", "Ne kadar güzel.", "So pretty.");
        Add(result, "PURRR!", "Mırrr…", "Purrr…");
        Add(result, "SMELLS GOOD!", "Mis gibi kokuyor.", "Smells lovely.");
        Add(result, "SO ARTY!", "Bunu beğendim.", "I like this one.");
        Add(result, "SO PEACEFUL.", "Ne kadar huzurlu.", "So peaceful.");
        Add(result, "SO SHADY!", "Gölgeye geçtim.", "A lovely shady spot.");
        Add(result, "SO SINKY!", "İçine gömülüverdim.", "So soft and snug.");
        Add(result, "SO SOFT!", "Yumuşacık.", "So soft.");
        Add(result, "SO WARM!", "Sıcacık.", "So warm.");
        Add(result, "SO WARM.", "Sıcacık.", "So warm.");
        Add(result, "SUN NAP!", "Güneşte küçük bir mola.", "A little sun nap.");
        Add(result, "TABLE NAP!", "Burada da dinlenebilirim.", "A little table nap.");
        Add(result, "WHO IS THAT?", "Bu da kim?", "Who’s that?");
        Add(result, "WIGGLY!", "Kıpır kıpır!", "So wiggly!");
        Add(result, "LET'S GET A LITTLE CLOSER!", "Biraz daha yaklaşalım.", "Let’s get a little closer.");
        Add(result, "I NEED A NAP FIRST!", "Önce biraz dinlenmeliyim.", "I need a little rest first.");
        Add(result, "GREAT PLAY!", "Birlikte oynamak güzeldi.", "That was fun together.");
        Add(result, "Plak dönüyor!", "Plak dönüyor!", "The record is spinning!");
        Add(result, "COZY BY THE WINDOW!", "Pencere kenarı ne rahat.", "So cosy by the window.");
        Add(result, "That felt good!", "Çok iyi geldi!", "That felt good!");
        Add(result, "Let's find a little open space.", "Biraz açık alana geçelim.", "Let's find a little open space.");
        Add(result, "I like being with you.", "Yanında olmak güzel.", "I like being with you.");
        Add(result, "One more paw?", "Bir pati daha mı?", "One more paw?");
        Add(result, "It wasn't me!", "Ben bir şey yapmadım!", "It wasn't me!");
        Add(result, "Such a cosy spot.", "Ne güzel bir köşe.", "Such a cosy spot.");
        Add(result, "I need to get closer to the roll.", "Ruloya biraz daha yaklaşmalıyım.", "I need to get closer to the roll.");
        Add(result, "Paper everywhere!", "Kâğıtlar uçuşuyor!", "Paper everywhere!");
        Add(result, "Wasn't me!", "Ben yapmadım!", "Wasn't me!");
        Add(result, "I'm not sleepy right now!", "Şu an uykum yok!", "I'm not sleepy right now!");
        Add(result, "Face the bowl and move a little closer.", "Kaba dönüp biraz yaklaşalım.", "Face the bowl and move a little closer.");
        Add(result, "Move to the front of the sofa and face it.", "Koltuğun önüne yaklaşıp ona dönelim.", "Move to the front of the sofa and face it.");
        Add(result, "The ball needs a little open space.", "Top için biraz açık alan gerekiyor.", "The ball needs a little open space.");
        Add(result, "There's no room to jump here.", "Burada zıplayacak yer yok.", "There's no room to jump here.");
        Add(result, "The kitchen counter is needed first.", "Önce mutfak tezgâhı gerekiyor.", "The kitchen counter is needed first.");
        return result;
    }
    private static void Add(Dictionary<string, string[]> result, string token, string tr, string en)
    {
        var pair = new[] { tr, en };
        result[token] = pair;
        if (!result.ContainsKey(tr)) result[tr] = pair;
        if (!result.ContainsKey(en)) result[en] = pair;
    }
    private static Dictionary<string, string> BuildReactionKeys()
    {
        var result = new Dictionary<string, string>(System.StringComparer.Ordinal);
        result["interaction.unavailable"] = "interaction.unavailable";
        result["THE BOWL IS NOT READY"] = "interaction.unavailable";
        result["THE TAP IS NOT READY"] = "interaction.unavailable";
        result["THE TRAY IS NOT READY"] = "interaction.unavailable";
        result["THE TENT IS NOT READY"] = "interaction.unavailable";
        result["THE HAMPER IS NOT READY"] = "interaction.unavailable";
        result["THE PANTRY IS NOT READY"] = "interaction.unavailable";
        result["THE SHOWER IS NOT READY"] = "interaction.unavailable";
        result["THE NICHE IS NOT READY"] = "interaction.unavailable";
        result["THE TUB IS NOT READY"] = "interaction.unavailable";
        result["THE SWING IS NOT READY"] = "interaction.unavailable";
        result["THE CART IS NOT READY"] = "interaction.unavailable";
        result["THE MAT IS NOT READY"] = "interaction.unavailable";
        result["THE OVEN IS NOT READY"] = "interaction.unavailable";
        result["THE TOY IS NOT READY"] = "interaction.unavailable";
        result["YARN IS NOT READY"] = "interaction.unavailable";
        result["MOUSE TOY IS NOT READY"] = "interaction.unavailable";
        result["SCRATCH POST IS NOT READY"] = "interaction.unavailable";
        result["TUNNEL IS NOT READY"] = "interaction.unavailable";
        result["NOT READY YET"] = "interaction.unavailable";
        result["Şu anda kullanamıyorum."] = "interaction.unavailable";
        result["I can’t use this right now."] = "interaction.unavailable";
        result["interaction.nothing_reach"] = "interaction.nothing_reach";
        result["NOTHING TO REACH"] = "interaction.nothing_reach";
        result["Uzanabileceğim bir şey yok."] = "interaction.nothing_reach";
        result["There’s nothing to reach."] = "interaction.nothing_reach";
        result["interaction.nothing_push"] = "interaction.nothing_push";
        result["NOTHING TO PUSH"] = "interaction.nothing_push";
        result["İtecek bir şey yok."] = "interaction.nothing_push";
        result["There’s nothing to push."] = "interaction.nothing_push";
        result["interaction.no_paper"] = "interaction.no_paper";
        result["NO PAPER TO PLAY WITH"] = "interaction.no_paper";
        result["Oynayacak kâğıt yok."] = "interaction.no_paper";
        result["There’s no paper to play with."] = "interaction.no_paper";
        result["interaction.no_room_above"] = "interaction.no_room_above";
        result["NO ROOM UP THERE"] = "interaction.no_room_above";
        result["Yukarıda yeterli yer yok."] = "interaction.no_room_above";
        result["There’s not enough room up there."] = "interaction.no_room_above";
        result["interaction.make_room"] = "interaction.make_room";
        result["LET'S MAKE SOME ROOM!"] = "interaction.make_room";
        result["Let's make some room"] = "interaction.make_room";
        result["Let's make some room."] = "interaction.make_room";
        result["LET'S MAKE ROOM BESIDE THE BOWL!"] = "interaction.make_room";
        result["Let's make room beside the bowl."] = "interaction.make_room";
        result["Kabın yanında biraz yer açalım."] = "interaction.make_room";
        result["Burada biraz daha boşluk gerekli."] = "interaction.make_room";
        result["I need a little more space here."] = "interaction.make_room";
        result["interaction.store_required"] = "interaction.store_required";
        result["AVAILABLE IN STORE"] = "interaction.store_required";
        result["Mağazadan edinmelisin."] = "interaction.store_required";
        result["Available in the shop."] = "interaction.store_required";
        result["interaction.fern.complete"] = "interaction.fern.complete";
        result["FERN_INSPECTED"] = "interaction.fern.complete";
        result["Yaprakları seyretmek ne güzel."] = "interaction.fern.complete";
        result["The leaves are lovely to watch."] = "interaction.fern.complete";
        result["care.not_hungry"] = "care.not_hungry";
        result["Şu an aç değilim!"] = "care.not_hungry";
        result["I'm not hungry right now!"] = "care.not_hungry";
        result["Aç değilim."] = "care.not_hungry";
        result["Şu an aç değilim."] = "care.not_hungry";
        result["I’m not hungry right now."] = "care.not_hungry";
        result["care.not_thirsty"] = "care.not_thirsty";
        result["Şu an susamadım!"] = "care.not_thirsty";
        result["I'm not thirsty right now!"] = "care.not_thirsty";
        result["Susamadım."] = "care.not_thirsty";
        result["Şu an susamadım."] = "care.not_thirsty";
        result["I’m not thirsty right now."] = "care.not_thirsty";
        result["care.unavailable"] = "care.unavailable";
        result["Şu anda bakım yapamıyorum."] = "care.unavailable";
        result["Care is unavailable right now."] = "care.unavailable";
        result["record.on"] = "record.on";
        result["Müzik zamanı!"] = "record.on";
        result["Music time!"] = "record.on";
        result["record.off"] = "record.off";
        result["Biraz sessizlik."] = "record.off";
        result["A little quiet."] = "record.off";
        result["record.not_ready"] = "record.not_ready";
        result["Plak henüz hazır değil."] = "record.not_ready";
        result["The record is not ready yet."] = "record.not_ready";
        result["idle.hungry"] = "idle.hungry";
        result["I'M HUNGRY..."] = "idle.hungry";
        result["Karnım acıktı…"] = "idle.hungry";
        result["I’m hungry…"] = "idle.hungry";
        result["idle.thirsty"] = "idle.thirsty";
        result["I NEED A DRINK..."] = "idle.thirsty";
        result["Biraz su içsem…"] = "idle.thirsty";
        result["I need a drink…"] = "idle.thirsty";
        result["idle.tired"] = "idle.tired";
        result["I NEED A NAP..."] = "idle.tired";
        result["Biraz dinlenmeliyim…"] = "idle.tired";
        result["I need a nap…"] = "idle.tired";
        result["idle.sleepy"] = "idle.sleepy";
        result["SLEEPY..."] = "idle.sleepy";
        result["Uykum geldi…"] = "idle.sleepy";
        result["Sleepy…"] = "idle.sleepy";
        result["idle.bored"] = "idle.bored";
        result["I'M BORED..."] = "idle.bored";
        result["Canım sıkıldı…"] = "idle.bored";
        result["I’m bored…"] = "idle.bored";
        return result;
    }
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
