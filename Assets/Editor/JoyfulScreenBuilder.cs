using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using U=PremiumUiElements;

/// <summary>Final UI authoring pass; all named additions are reused on rebuild.</summary>
public static class JoyfulScreenBuilder
{
    static RectTransform Find(Transform root,string name)
    {foreach(var r in root.GetComponentsInChildren<RectTransform>(true))if(r.name==name)return r;return null;}
    static void Surface(Transform root,string name,Color color,float radius=24)
    {var r=Find(root,name);if(r!=null)JoyfulUiArt.Surface(r.GetComponent<LowPolyPanelGraphic>(),color,radius);}
    static void TextColor(Transform root,Color color)
    {foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))t.color=color;}
    static void Motif(Transform root,string name,float x,float y,float w,float h,Color color)
    {if(root.Find(name)==null)JoyfulUiArt.Motif(root,name,x,y,w,h,color);}
    public static void Polish(Transform root)
    {
        foreach(var graphic in root.GetComponentsInChildren<LowPolyPanelGraphic>(true))
        {
            var rect=graphic.rectTransform.rect;
            if(rect.width<120||rect.height<48||graphic.GetComponent<Mask>()!=null||graphic.name.Contains("Shadow")||graphic.color.a<.98f)continue;
            JoyfulUiArt.Surface(graphic,graphic.color,Mathf.Min(26,rect.height*.3f));
        }
        foreach(var r in root.GetComponentsInChildren<RectTransform>(true))
        {
            switch(r.name)
            {
                case "ActionButton":
                    if(r.GetComponent<Button>()!=null)JoyfulUiArt.ActionStyle(r.GetComponent<Button>(),JoyfulUiArt.Coral);break;
                case "FoodBar": Need(r,new Color32(255,233,199,255),JoyfulUiArt.Coral);break;
                case "ThirstUI": Need(r,JoyfulUiArt.SkyPaper,JoyfulUiArt.Ocean);break;
                case "EnergyUI": Need(r,new Color32(237,225,251,255),JoyfulUiArt.Purple);break;
                case "CatShopButton":
                    Surface(r,"Face",JoyfulUiArt.Ocean);Surface(r,"PortraitMedallion",JoyfulUiArt.Gold,38);
                    foreach(var t in r.GetComponentsInChildren<TMP_Text>(true))t.color=Color.white;
                    break;
                case "HomeDock": Dock(r);break;
                case "PremiumBase":
                    if(r.GetComponentInParent<MobileJoystick>()!=null)JoyfulUiArt.Surface(r.GetComponent<LowPolyPanelGraphic>(),JoyfulUiArt.Ocean,94);
                    break;
                case "YourHomeJourney":
                    JoyfulUiArt.Surface(r.GetComponent<LowPolyPanelGraphic>(),JoyfulUiArt.SkyPaper,22);break;
                case "HomeLevelPill": Surface(r,"Face",JoyfulUiArt.Gold);break;
                case "BrandDockLayout":
                    var play=Find(r,"PlayButton");if(play!=null)JoyfulUiArt.ActionStyle(play.GetComponent<Button>(),JoyfulUiArt.Coral);
                    var word=Find(r,"Wordmark");if(word!=null)word.GetComponent<TMP_Text>().text="CAT <color=#1885AD>HOME</color>";
                    break;
                case "ShopShortcut":case "RoomsShortcut":case "GamesShortcut":
                    Color color=r.name=="ShopShortcut"?JoyfulUiArt.Gold:r.name=="RoomsShortcut"?JoyfulUiArt.SkyPaper:new Color32(240,224,250,255);
                    Surface(r,"Visual",color,26);break;
                case "HomeSidebar":
                    JoyfulUiArt.Surface(r.GetComponent<LowPolyPanelGraphic>(),JoyfulUiArt.Ocean,30);
                    Motif(r,"TravelStars",0,0,382,772,new Color(1,1,1,.20f));break;
                case "RoomSelectorPanelVisual": Rooms(r);break;
                case "ReturnBanner":
                    var returnTitle=Find(r.parent,"Title");if(returnTitle!=null)returnTitle.GetComponent<TMP_Text>().color=Color.white;
                    var duration=Find(r.parent,"Duration");if(duration!=null)duration.GetComponent<TMP_Text>().color=Color.white;
                    break;
                case "WelcomeCardLayout":
                    var welcomeTitle=Find(r,"WelcomeTitle");if(welcomeTitle!=null)welcomeTitle.GetComponent<TMP_Text>().color=Color.white;break;
                case "ResultsCardLayout":
                    var resultTitle=Find(r,"ResultTitle");if(resultTitle!=null)resultTitle.GetComponent<TMP_Text>().color=Color.white;break;
                case "ScorePanel":
                    JoyfulUiArt.Surface(r.GetComponent<LowPolyPanelGraphic>(),JoyfulUiArt.Ocean,22);TextColor(r,Color.white);break;
                case "GamesHubCard":
                    Surface(r,"GamesHubCard",JoyfulUiArt.SkyPaper,32);
                    if(r.Find("JoyfulGamesBanner")==null){var banner=JoyfulUiArt.Panel("JoyfulGamesBanner",r,JoyfulUiArt.Ocean,0,331,1412,154,24);banner.transform.SetAsFirstSibling();}
                    // The root graphic draws behind its children; banner is behind text.
                    var title=Find(r,"GamesTitle");if(title!=null)title.GetComponent<TMP_Text>().color=Color.white;
                    var subtitle=Find(r,"GamesSubtitle");if(subtitle!=null)subtitle.GetComponent<TMP_Text>().color=Color.white;
                    break;
            }
            if(r.name.StartsWith("RoomCard_"))RoomCard(r);
            if(r.name.StartsWith("Product_"))Product(r);
            if(r.name=="HubCatRunnerButton"||r.name=="HubCatCatchButton")GameCard(r,r.name=="HubCatRunnerButton");
        }
    }
    static void Need(RectTransform root,Color paper,Color accent)
    {
        Surface(root,"Background",paper,24);
        var bg=Find(root,"Background");
        if(bg!=null&&bg.Find("NeedArtWell")==null)
        {var well=JoyfulUiArt.Panel("NeedArtWell",bg,accent,-93,0,66,70,19);well.transform.SetAsFirstSibling();}
        var track=Find(root,"FillTrackMask");if(track!=null){U.At(track,31,-20,164,16);JoyfulUiArt.Surface(track.GetComponent<LowPolyPanelGraphic>(),Color.Lerp(accent,Color.white,.76f),7);}
        var fill=Find(root,"Fill");if(fill!=null&&fill.GetComponent<Image>()!=null)fill.GetComponent<Image>().color=accent;
        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
        {label.color=JoyfulUiArt.Ink;if(label.name=="PercentageText"){label.fontSize=26;label.enableAutoSizing=false;}}
    }
    static void Dock(RectTransform root)
    {
        root.sizeDelta=new Vector2(1080,64);
        Surface(root,"DockEnamelTray",JoyfulUiArt.SkyPaper,0);
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            Color color=button.name=="ShopButton"?JoyfulUiArt.Gold:button.name=="PlayCatRunnerButton"?JoyfulUiArt.Coral:JoyfulUiArt.Ocean;
            var rect=(RectTransform)button.transform;
            if(button.name=="ShopButton")rect.anchoredPosition=new Vector2(-430,0);
            else if(button.name=="CurrentRoomStatus")rect.anchoredPosition=new Vector2(-110,0);
            else if(button.name=="PlayCatRunnerButton")rect.anchoredPosition=new Vector2(430,0);
            JoyfulUiArt.ActionStyle(button,color,button.name=="ShopButton");
            Surface(button.transform,"DockArtWell",JoyfulUiArt.Paper,16);
            var motif=Find(button.transform,"BackdropMotif");if(motif!=null)motif.gameObject.SetActive(false);
        }
    }
    static void Rooms(RectTransform root)
    {
        foreach(string name in new[]{"Title","HomeLevel","Subtitle","RoomHint","RoomFeedback"})
        {var t=Find(root,name);if(t!=null)t.GetComponent<TMP_Text>().color=Color.white;}
        Surface(root,"RoomSelectorPanelVisual",JoyfulUiArt.SkyPaper,32);
        var emblem=Find(root,"HomeEmblem");if(emblem!=null){U.At(emblem,-598,292,132,132);emblem.localEulerAngles=new Vector3(0,0,-8);}
    }
    static void RoomCard(RectTransform root)
    {
        Surface(root,"CardVisual",JoyfulUiArt.Paper,22);Surface(root,"ActionFace",JoyfulUiArt.Ocean,16);
        var action=Find(root,"ActionText");if(action!=null)action.GetComponent<TMP_Text>().color=Color.white;
        Surface(root,"CurrentBadge",JoyfulUiArt.Gold,12);
    }
    static void Product(RectTransform root)
    {
        var preview=Find(root,"Preview");if(preview==null)return;
        Surface(root,"Card",JoyfulUiArt.Paper,24);
        var stageRect=root.Find("ProductArtStage") as RectTransform;
        if(stageRect==null)
        {
            var stage=JoyfulUiArt.Panel("ProductArtStage",root,JoyfulUiArt.SkyPaper,0,104,root.rect.width-24,260,18);
            stage.transform.SetSiblingIndex(1);
            Motif(stage.transform,"ProductStars",0,0,root.rect.width-52,230,new Color(.1f,.56f,.67f,.18f));
            stageRect=stage.rectTransform;
        }
        stageRect.anchorMin=new Vector2(0,.5f);stageRect.anchorMax=new Vector2(1,.5f);stageRect.anchoredPosition=new Vector2(0,104);stageRect.sizeDelta=new Vector2(-24,260);
        var stars=stageRect.Find("ProductStars") as RectTransform;if(stars!=null)U.Fill(stars,16);
        var buy=Find(root,"BuyButton");if(buy!=null)JoyfulUiArt.ActionStyle(buy.GetComponent<Button>(),JoyfulUiArt.Ocean);
        Surface(root,"OwnedBadge",JoyfulUiArt.Gold,10);
    }
    static void GameCard(RectTransform root,bool runner)
    {
        Color accent=runner?JoyfulUiArt.Coral:JoyfulUiArt.Ocean;
        Surface(root,"Visual",JoyfulUiArt.Paper,28);Surface(root,"Action",accent,20);
        var action=Find(root,"Action");if(action!=null)TextColor(action,Color.white);
        var preview=Find(root,"HeroPreviewWell");if(preview!=null&&preview.Find("ModeBadge")==null)
        {
            var badge=JoyfulUiArt.Panel("ModeBadge",preview,accent,-200,132,168,46,12);
            U.Label("ModeCopy",badge.transform,PremiumTypography.Emphasis,21,Color.white,0,0,150,40,TextAlignmentOptions.Center).gameObject.AddComponent<BilingualCopyLabel>().Configure(runner?"KOŞ & KEŞFET":"PUSU & YAKALA",runner?"RUN & EXPLORE":"STALK & CATCH");
        }
        if(preview!=null&&preview.Find("ModeBadge") is RectTransform badgeRect)
        {
            U.At(badgeRect,-174,132,220,46);
            var copy=badgeRect.GetComponentInChildren<TMP_Text>(true);
            if(copy!=null){U.At(copy.rectTransform,0,0,204,40);copy.fontSize=18;copy.enableAutoSizing=false;copy.textWrappingMode=TextWrappingModes.NoWrap;PremiumTypography.Apply(copy,true);}
        }
    }
}
