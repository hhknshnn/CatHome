using System.Collections.Generic;
using UnityEngine;

// Presentation only: never grants rewards or changes interaction admission.
public static class LivingRoomSpeech
{
    sealed class Pool { public string[] trBefore,trAfter,enBefore,enAfter; }
    static readonly Dictionary<string,Pool> pools=new Dictionary<string,Pool>();
    static readonly Dictionary<string,int> cursors=new Dictionary<string,int>();
    public static bool IsLiving => HomeRoomService.CurrentRoomId==HomeRoomService.LivingRoomId&&UnityEngine.SceneManagement.SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath).isLoaded;
    public static int ContextCount=>pools.Count;
    static LivingRoomSpeech()
    {
        Add("scratch","Patiler hazır!|Biraz uzanayım.|Tırnak bakımı zamanı.","Oh, rahatladım!|Tam kıvamında!|Patilerim yenilendi.","Paws ready!|A little stretch.|Time for a scratch.","That feels good!|Just right!|Fresh paws!");
        Add("tunnel","İçeride ne var?|Küçük bir keşif!|Öbür uçta görüşürüz.","İşte buradayım!|Gizli yol bulundu.|Bir uçtan öbürüne!","What's inside?|A tiny adventure!|See you on the other side.","Here I am!|Secret route found.|Through and through!");
        Add("spring","Kıpırdadığını gördüm!|Bir pati dokunuşu…|Hazırım, minik yay!","Hop, geri geldi!|Yine yakaladım.|Neşeli bir zıpırtı!","I saw that wiggle!|One little tap…|Ready, little spring!","Boing, back again!|Caught it again.|Such a happy bounce!");
        Add("mouse","Seni gördüm, minik!|Sessizce yaklaşıyorum.|Kuyruğun ele verdi.","Yakaladım seni!|Güzel bir avdı.|Şimdilik dinlen, minik.","I see you, little one!|Quiet paws now.|Your tail gave you away.","Got you!|A lovely little hunt.|Rest now, little one.");
        Add("food","Mis gibi kokuyor.|Mama molası!|Küçük bir ziyafet.","Karnım mutlu!|Çok lezzetliydi.|Bıyıklarımı sileyim.","Smells lovely.|Snack break!|A little feast.","Happy tummy!|That was tasty.|Let me tidy my whiskers.");
        Add("water","Bir yudum alayım.|Serin su zamanı.|Minik bir su molası.","Oh, serinledim!|Tam ihtiyacım olan.|Bir damla mutluluk.","Just a little sip.|Cool water time.|A tiny water break.","So refreshing!|Just what I needed.|A drop of happiness.");
        Add("sleep","Rüyalar beni bekler.|Biraz kıvrılayım.|Gözlerim kapanıyor.","Mis gibi uyudum.|Yeni maceralara hazırım.|Güzel bir rüyaydı.","Dreams are calling.|Time to curl up.|Sleepy little eyes.","Such a lovely nap.|Ready for adventures.|That was a sweet dream.");
        Add("rest","Burası pek rahat.|Küçük bir mola.|Manzaram güzel.","İyi geldi!|Biraz da gezeyim.|Keyfim yerine geldi.","So cosy here.|A little pause.|What a lovely view.","That felt nice!|Time for a wander.|Feeling cosy and bright.");
        Add("ribbon","Uçuşan şeyi gördüm!|Tüy kadar hafif.|Bir ucundan tutayım.","Ucunu yakaladım!|Ne tatlı bir dans!|Biraz da o dinlensin.","I saw that flutter!|Light as a feather.|Let me catch the end.","Caught the tip!|What a lovely dance!|Let it rest a little.");
        Add("ball","Yuvarlanmaya hazır mı?|Bir küçük itiş.|Nereye gidecek?","Güzel yuvarlandı!|Pati pası tamam.|Yine bana döndü.","Ready to roll?|One tiny push.|Where will it go?","A lovely roll!|Paw pass complete.|Back to me again.");
        Add("grass","Ne taze bir koku!|Bir koklayayım.|Yeşil bir merhaba.","Burnum çok mutlu.|Taptaze bir mola.|Doğadan küçük bir hediye.","Such a fresh scent!|Let me have a sniff.|A little green hello.","Happy little nose.|A fresh little break.|A tiny gift from nature.");
        Add("play","Burada ne varmış?|Biraz oynayalım.|Meraklı patiler hazır.","Çok keyifliydi!|Güzel bir oyun.|Biraz daha keşfedeyim.","What have we here?|Let's play a little.|Curious paws ready.","That was fun!|A lovely game.|More things to explore.");
        Add("watch","Biraz seyredeyim.|Merakımı uyandırdı.|Şuna bir bakayım.","Ne güzel bir köşe.|Bakmaya değerdi.|Evimi seviyorum.","Let me watch a little.|That looks interesting.|Let's have a look.","Such a lovely corner.|Worth a little look.|I love my home.");
        Add("hide","Küçük sığınağım!|Buraya sığar mıyım?|Beni bulabilir misin?","İşte buradayım!|Sıcacık bir köşeydi.|Saklambaç bitti.","My little hideaway!|Will I fit in here?|Can you find me?","Here I am!|A cosy little corner.|Peekaboo!");
        Add("puzzle","Ödülün kokusunu aldım.|Biraz düşünelim.|Hangi taraftan açılır?","Lezzetli bir keşif!|Patilerim çözdü.|Uğraşmaya değdi.","I smell a little treat.|Let's figure it out.|Which way does it open?","A tasty discovery!|My paws solved it.|Worth the little puzzle.");
    }
    static void Add(string key,string a,string b,string c,string d)=>pools[key]=new Pool{trBefore=a.Split('|'),trAfter=b.Split('|'),enBefore=c.Split('|'),enAfter=d.Split('|')};
    public static string Context(CatActivity activity)
    {
        string id=activity.StoreProductId;
        if(activity is ScratchPostActivity)return "scratch";
        if(id==HomeStoreService.PlayTunnelId)return "tunnel";
        if(id==HomeStoreService.ToyMouseId)return "mouse";
        if(id==HomeStoreService.FeatherToyId)return "spring";
        if(id==HomeStoreService.CeramicBowlId)return "food";
        if(id==HomeStoreService.TreatJarId||id==HomeStoreService.KibbleBagId)return "puzzle";
        if(activity is LivingFurnitureActivity||activity is PerchNapActivity)return "rest";
        if(activity is SitLookActivity)return "watch";
        if(id==HomeStoreService.CardboardHideoutId)return "hide";
        if(CatCollectionPolicy.IsBed(id))return "sleep";
        if(id==HomeStoreService.SofaId||id==HomeStoreService.ArmchairId||id==HomeStoreService.CoffeeTableId)return "rest";
        if(id==HomeStoreService.CatnipPlantId)return "grass";
        if(id==HomeStoreService.LeashId)return "ribbon";
        if(id==HomeStoreService.BallBasketId||id==HomeStoreService.BellCollarId||id==HomeStoreService.CollarId)return "ball";
        return "play";
    }
    public static string Next(string context,bool after)
    {
        if(!pools.TryGetValue(context,out var pool))pool=pools["play"];
        bool tr=GameLanguageService.Current==GameLanguage.Turkish;
        var values=tr?(after?pool.trAfter:pool.trBefore):(after?pool.enAfter:pool.enBefore);
        string key=context+(after?".after":".before")+(tr?".tr":".en");
        cursors.TryGetValue(key,out int cursor);cursors[key]=(cursor+1)%values.Length;
        return values[cursor%values.Length];
    }
    public static bool Show(CatMovement cat,Object owner,string context,bool after)
    {
        if(!IsLiving||cat==null)return false;
        CatSpeechBubble.EnsureOn(cat).ShowLocalizedOwned(owner,Next(context,after));return true;
    }
}
