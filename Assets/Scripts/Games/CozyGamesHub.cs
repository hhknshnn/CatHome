using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class CozyGamesHub
{
    public static void Build(GamesHubPanel hub,Button runner,Button catcher,out TMP_Text runnerLives,out TMP_Text catchLives)
    {
        runnerLives=catchLives=null;var card=hub.transform.Find("SafeArea/GamesHubCard");if(card==null)return;
        var title=card.Find("GamesTitle");if(title!=null){var loc=title.GetComponent<LocalizedLabel>();if(loc!=null)loc.enabled=false;title.GetComponent<TMP_Text>().text=CozyGameUi.Copy("Birlikte oyun zamanı","A little playtime together");}
        var intro=card.Find("GamesSubtitle");if(intro!=null){var loc=intro.GetComponent<LocalizedLabel>();if(loc!=null)loc.enabled=false;intro.GetComponent<TMP_Text>().text=CozyGameUi.Copy("Dört oyun, ortak canlar · Yeni tur 1 can","Four games, shared lives · New round 1 life");}
        // Reserve one shared, non-interactive counter in the header.
        var heading=(RectTransform)title;heading.anchoredPosition=new Vector2(-360,346);heading.sizeDelta=new Vector2(600,72);
        var headingText=title.GetComponent<TMP_Text>();headingText.enableAutoSizing=true;headingText.fontSizeMin=32;headingText.fontSizeMax=44;headingText.textWrappingMode=TextWrappingModes.NoWrap;
        var subtitle=(RectTransform)intro;subtitle.anchoredPosition=new Vector2(-185,286);subtitle.sizeDelta=new Vector2(950,42);
        var shared=CozyGameUi.Panel("SharedLives",card,new Vector2(.53f,.852f),new Vector2(.92f,.978f));shared.raycastTarget=false;
        var count=CozyGameUi.Text("Count",shared.transform,"",new Vector2(.035f,.39f),new Vector2(.97f,.98f),31,true);
        count.color=StorybookScreenStyle.CoralTop;
        CozyGameUi.Text("Refill",shared.transform,"",new Vector2(.035f,.015f),new Vector2(.97f,.44f),18);
        runnerLives=Card(runner,0,"Runner",CozyGameUi.Copy("Eve Dönüş","Homeward Run"),CozyGameUi.Copy("KOŞ · KEŞFET","RUN · EXPLORE"),CozyGameUi.Copy("75 saniyede mahalleden evine.","Through the neighbourhood in 75 seconds."));
        catchLives=Card(catcher,1,"Catch",CozyGameUi.Copy("Pati Avı","Paw Hunt"),CozyGameUi.Copy("İZLE · YAKALA","CHASE · CATCH"),CozyGameUi.Copy("Fareye dokun, sıçra ve yakala.","Tap a mouse to pounce and catch."));
        var yarn=CozyGameUi.Button("HubYarnButton",card,"",Vector2.zero,Vector2.one,()=>hub.PlayCozy(CozyGameKind.Yarn));
        var pond=CozyGameUi.Button("HubPondButton",card,"",Vector2.zero,Vector2.one,()=>hub.PlayCozy(CozyGameKind.Pond));
        Card(yarn,2,"Yarn",CozyGameUi.Copy("Yumak Rotası","Yarn Trail"),CozyGameUi.Copy("DÜŞÜN · YUVARLA","AIM · ROLL"),CozyGameUi.Copy("24 bölüm. Süreye karşı bir yumak macerası.","24 puzzles. A yarn adventure against the clock." )).text=CozyGameUi.Copy("3 dakika · 24 bölüm","3 minutes · 24 puzzles");
        Card(pond,3,"Pond",CozyGameUi.Copy("Gölet Keyfi","Pond Moments"),CozyGameUi.Copy("YÖNLENDİR · YAKALA","MOVE · CATCH"),CozyGameUi.Copy("Kıyıda hizalan, seri yap, bonus balığı yakala.","Line up on the shore. Build a streak. Catch the bonus fish.")).text=CozyGameUi.Copy("75 saniye · 6 balık","75 seconds · 6 fish");
    }
    public static void Refresh(GamesHubPanel hub)
    {
        var card=hub.transform.Find("SafeArea/GamesHubCard");if(card==null)return;
        card.Find("GamesTitle").GetComponent<TMP_Text>().text=CozyGameUi.Copy("Birlikte oyun zamanı","A little playtime together");
        card.Find("GamesSubtitle").GetComponent<TMP_Text>().text=CozyGameUi.Copy("Dört oyun, ortak canlar · Yeni tur 1 can","Four games, shared lives · New round 1 life");
        foreach(var item in card.GetComponentsInChildren<CozyHubCardCopy>())item.Refresh();
    }
    private static TMP_Text Card(Button button,int index,string preview,string title,string tag,string description)
    {
        button.gameObject.AddComponent<CozyHubCardCopy>().index=index;
        foreach(Transform child in button.transform)child.gameObject.SetActive(false);
        var rt=(RectTransform)button.transform;rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(index%2==0?-340:340,index<2?104:-200);rt.sizeDelta=new Vector2(652,282);
        var oldFx=button.GetComponent<PremiumButtonFx>();if(oldFx!=null)oldFx.enabled=false;
        if(button.GetComponent<CanvasRenderer>()==null)button.gameObject.AddComponent<CanvasRenderer>();
        var panel=button.GetComponent<LowPolyPanelGraphic>();if(panel==null)panel=button.gameObject.AddComponent<LowPolyPanelGraphic>();StorybookScreenStyle.Card(panel,23);button.targetGraphic=panel;
        var frame=CozyGameUi.Panel("PhotoFrame",rt,new Vector2(.012f,.03f),new Vector2(.43f,.97f));frame.raycastTarget=false;frame.gameObject.AddComponent<Mask>().showMaskGraphic=false;
        var photo=CozyGameUi.Rect("LivePhoto",frame.transform,Vector2.zero,Vector2.one,Vector2.zero).gameObject.AddComponent<RawImage>();photo.texture=Resources.Load<Texture2D>("CozyGames/"+preview+"Preview");photo.raycastTarget=false;
        // Crop the landscape photograph to the portrait window without stretching the world.
        photo.uvRect=new Rect(.212f,0,.576f,1);
        CozyGameUi.Text("Genre",rt,tag,new Vector2(.455f,.76f),new Vector2(.98f,.94f),17);
        CozyGameUi.Text("Name",rt,title,new Vector2(.455f,.56f),new Vector2(.98f,.80f),35,true);
        CozyGameUi.Text("Description",rt,description,new Vector2(.455f,.34f),new Vector2(.97f,.57f),20);
        var lives=CozyGameUi.Text("LiveStatus",rt,"",new Vector2(.455f,.19f),new Vector2(.98f,.35f),17);
        var action=CozyGameUi.Panel("PlayPlate",rt,new Vector2(.46f,.035f),new Vector2(.96f,.19f));StorybookScreenStyle.Enamel(action,StorybookScreenStyle.CoralTop,StorybookScreenStyle.Coral,17,false);action.raycastTarget=false;
        var label=CozyGameUi.Text("Play",action.transform,CozyGameUi.Copy("Oyna  →","Play  →"),Vector2.zero,Vector2.one,22,true);label.alignment=TextAlignmentOptions.Center;
        if(index<2)lives.text=CozyGameUi.Copy(index==0?"75 saniye · 3 mahalle":"60 saniye · 3 dalga",index==0?"75 seconds · 3 neighbourhoods":"60 seconds · 3 waves");
        return lives;
    }
}

public sealed class CozyHubCardCopy:MonoBehaviour
{
    public int index;
    static readonly string[,] Tr={{"Eve Dönüş","KOŞ · KEŞFET","75 saniyede mahalleden evine."},{"Pati Avı","İZLE · YAKALA","Fareye dokun, sıçra ve yakala."},{"Yumak Rotası","DÜŞÜN · YUVARLA","24 bölüm. Süreye karşı bir yumak macerası."},{"Gölet Keyfi","YÖNLENDİR · YAKALA","Kıyıda hizalan, seri yap, bonus balığı yakala."}};
    static readonly string[,] En={{"Homeward Run","RUN · EXPLORE","Through the neighbourhood in 75 seconds."},{"Paw Hunt","CHASE · CATCH","Tap a mouse to pounce and catch."},{"Yarn Trail","AIM · ROLL","24 puzzles. A yarn adventure against the clock."},{"Pond Moments","MOVE · CATCH","Line up on the shore. Build a streak. Catch the bonus fish."}};
    public void Refresh()
    {
        string[] fields={"Name","Genre","Description"};
        for(int i=0;i<3;i++)transform.Find(fields[i]).GetComponent<TMP_Text>().text=CozyGameUi.Copy(Tr[index,i],En[index,i]);
        transform.Find("PlayPlate/Play").GetComponent<TMP_Text>().text=CozyGameUi.Copy("Oyna  →","Play  →");
        if(index<2)transform.Find("LiveStatus").GetComponent<TMP_Text>().text=CozyGameUi.Copy(index==0?"75 saniye · 3 mahalle":"60 saniye · 3 dalga",index==0?"75 seconds · 3 neighbourhoods":"60 seconds · 3 waves");
        if(index>=2)transform.Find("LiveStatus").GetComponent<TMP_Text>().text=CozyGameUi.Copy(index==2?"3 dakika · 24 bölüm":"75 saniye · 6 balık",index==2?"3 minutes · 24 puzzles":"75 seconds · 6 fish");
    }
}
