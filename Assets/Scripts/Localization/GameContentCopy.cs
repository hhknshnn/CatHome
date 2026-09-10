public static class GameContentCopy
{
    public static string Text(string turkish,string english) => GameLanguageService.Current == GameLanguage.Turkish ? turkish : english;
    public static string CatReaction(string message)
    {
        switch(message)
        {
            case "SWEET DREAMS!": return Text("Mışıl mışıl dinlendim.","Sweet dreams.");
            case "SWEET DREAMS.": return Text("Tatlı rüyalar.","Sweet dreams.");
            case "SEED RAIN!": return Text("Kuşlara küçük bir ziyafet!","A little feast for the birds!");
            case "IT MOVES!": return Text("Bak, hareket ediyor!","Look, it moves!");
            case "YARN CHAMPION!": return Text("Yumağı yakaladım!","Got the yarn!");
            case "YUM!": return Text("Nefis!","Yum!");
            case "ALL COVERED UP!": return Text("İşte, tertemiz.","All covered up.");
            case "FOUND ME!": return Text("Beni buldun!","You found me!");
            case "MAKING BISCUITS!": return Text("Patilerim iş başında.","Making biscuits.");
            case "OOPS.": return Text("Ben bir şey yapmadım…","It wasn’t me…");
            case "SO FLUFFY!": return Text("Yumuşacık oldum.","So fluffy.");
            case "MIGHTY HUNTER!": return Text("Yakaladım seni!","Got you!");
            case "TOASTY!": return Text("Sıcacık bir köşe.","A cosy warm spot.");
            case "PAPER EVERYWHERE!": return Text("Biraz dağıttım galiba…","I may have made a little mess…");
            case "REFRESHING!": return Text("Oh, ferahladım.","So refreshing.");
            case "SQUEAKY CLEAN!": return Text("Pırıl pırılım.","All clean.");
            case "CLAWS FEEL GREAT!": return Text("Patilerime iyi geldi.","My paws feel great.");
            case "NOTHING UP HERE!": return Text("Burası da benim oldu.","Another spot to explore.");
            case "WHEEE!": return Text("Biraz daha sallanalım mı?","Another little swing?");
            case "TUNNEL CHAMPION!": return Text("Öbür taraftan çıktım!","Made it through!");
            case "STILL DRY!": return Text("Patilerim hâlâ kuru.","Still dry.");
            case "GOOD SPOT!": return Text("Ne güzel bir köşe.","Such a lovely spot.");
            case "SO COZY!": return Text("Tam bana göre.","So cosy.");
            case "WHAT A VIEW!": return Text("Manzaraya bak!","What a view!");
            case "HELLO BIRDS!": return Text("Merhaba kuşlar!","Hello, birds!");
            case "BEST SEAT!": return Text("En güzel yer benim.","The best seat is mine.");
            case "CAFE NAP!": return Text("Küçük bir kafe molası.","A little cafe nap.");
            case "GOT IT!": return Text("Yakaladım!","Got it!");
            case "GOT THE YARN!": return Text("Yumak bende!","Got the yarn!");
            case "MY COZY CHAIR!": return Text("Benim rahat koltuğum.","My cosy chair.");
            case "MY DESK!": return Text("Masa artık benim.","My desk now.");
            case "MY POUFFE!": return Text("Pufuma yerleştim.","My cosy pouffe.");
            case "MY STOOL!": return Text("Burayı sevdim.","I like this seat.");
            case "OPEN IT!": return Text("İçinde ne var acaba?","What’s inside?");
            case "PRETTY!": return Text("Ne kadar güzel.","So pretty.");
            case "PURRR!": return Text("Mırrr…","Purrr…");
            case "SMELLS GOOD!": return Text("Mis gibi kokuyor.","Smells lovely.");
            case "SO ARTY!": return Text("Bunu beğendim.","I like this one.");
            case "SO PEACEFUL.": return Text("Ne kadar huzurlu.","So peaceful.");
            case "SO SHADY!": return Text("Gölgeye geçtim.","A lovely shady spot.");
            case "SO SINKY!": return Text("İçine gömülüverdim.","So soft and snug.");
            case "SO SOFT!": return Text("Yumuşacık.","So soft.");
            case "SO WARM!": return Text("Sıcacık.","So warm.");
            case "SO WARM.": return Text("Sıcacık.","So warm.");
            case "SUN NAP!": return Text("Güneşte küçük bir mola.","A little sun nap.");
            case "TABLE NAP!": return Text("Burada da dinlenebilirim.","A little table nap.");
            case "WHO IS THAT?": return Text("Bu da kim?","Who’s that?");
            case "WIGGLY!": return Text("Kıpır kıpır!","So wiggly!");
            case "LET'S GET A LITTLE CLOSER!": return Text("Biraz daha yaklaşalım.","Let’s get a little closer.");
            case "I NEED A NAP FIRST!": return Text("Önce biraz dinlenmeliyim.","I need a little rest first.");
            case "GREAT PLAY!": return Text("Birlikte oynamak güzeldi.","That was fun together.");
            case "Plak dönüyor!": return Text("Plak dönüyor!","The record is spinning!");
            default: return message;
        }
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
