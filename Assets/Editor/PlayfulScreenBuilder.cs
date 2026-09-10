using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using U=PremiumUiElements;

/// <summary>Final authored layouts. Keeps every live controller and input reference.</summary>
public static class PlayfulScreenBuilder
{
    static Color Navy=>PlayfulUiArt.Navy;
    static Color Violet=>PlayfulUiArt.Violet;
    static Color Gold=>PlayfulUiArt.Gold;
    static Color Teal=>PlayfulUiArt.Teal;
    static Color Coral=>PlayfulUiArt.Coral;
    public static void Polish(Transform root)
    {
        foreach(var popup in root.GetComponentsInChildren<WhileYouWereAwayPopup>(true))Return(popup.transform);
        foreach(var card in root.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="WelcomeCardLayout").ToArray())Welcome(card);
        bool runner=root.GetComponentInParent<CatRunnerGameController>()!=null||root.GetComponentInChildren<CatRunnerGameController>(true)!=null;
        bool catcher=root.GetComponentInParent<CatCatchGameController>()!=null||root.GetComponentInChildren<CatCatchGameController>(true)!=null;
        if(runner||catcher)Hud(root,runner);
    }
    static RectTransform Find(Transform root,string name)=>root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t=>t.name==name);
    static void At(Transform root,string name,float x,float y,float w,float h)
    {var t=Find(root,name);if(t!=null)U.At(t,x,y,w,h);}
    static void Tone(Transform root,string name,Color bottom,Color top,float radius=22)
    {var t=Find(root,name);var p=t!=null?t.GetComponent<LowPolyPanelGraphic>():null;if(p!=null)p.ConfigureModernStyle(top,bottom,radius,true);}
    static void Type(Transform root,string name,Color color,float size=0)
    {var t=Find(root,name);var label=t!=null?t.GetComponent<TMP_Text>():null;if(label!=null){label.color=color;if(size>0){label.fontSize=size;label.fontSizeMax=size;}}}
    static LowPolyPanelGraphic Panel(Transform root,string name,Color bottom,Color top,float x,float y,float w,float h,float radius=24)
    {
        var t=root.Find(name) as RectTransform;
        var p=t!=null?t.GetComponent<LowPolyPanelGraphic>():U.Panel(name,root,bottom,x,y,w,h,radius);
        U.At(p.rectTransform,x,y,w,h);p.ConfigureModernStyle(top,bottom,radius,true);p.raycastTarget=false;return p;
    }
    static TMP_Text Copy(Transform root,string name,string tr,string en,Color color,float size,float x,float y,float w,float h,TextAlignmentOptions alignment=TextAlignmentOptions.Left)
    {
        var t=root.Find(name);var label=t!=null?t.GetComponent<TMP_Text>():U.Label(name,root,PremiumTypography.Emphasis,size,color,x,y,w,h,alignment);
        U.At(label.rectTransform,x,y,w,h);label.color=color;label.fontSize=size;label.alignment=alignment;
        (label.GetComponent<BilingualCopyLabel>()??label.gameObject.AddComponent<BilingualCopyLabel>()).Configure(tr,en);return label;
    }
    static void Action(Transform root,string name,Color accent,bool secondary=false)
    {var t=Find(root,name);if(t!=null)PlayfulUiArt.Action(t.GetComponent<Button>(),accent,secondary);}
    static void Return(Transform root)
    {
        var body=Find(root,"AnimationContainer");if(body==null)return;
        Tone(root,"ReturnFace",new Color32(240,235,255,255),Color.white,32);
        At(root,"ReturnBanner",0,194,928,236);Tone(root,"ReturnBanner",Navy,Violet,26);
        At(root,"Title",-111,228,640,70);Type(root,"Title",Color.white,43);
        At(root,"AwayDurationBadge",-106,155,630,44);At(root,"Duration",0,0,630,44);Type(root,"Duration",new Color32(226,223,255,255),23);
        Copy(body,"ReturnKicker","YİNE BİR ARADAYIZ","TOGETHER AGAIN",new Color32(255,222,143,255),18,-109,285,638,28);
        At(root,"PortraitMedallion",334,200,194,194);Tone(root,"PortraitMedallion",Gold,new Color32(255,233,166,255),66);
        At(root,"CatPortrait",334,204,182,182);
        var medallion=Find(root,"PortraitMedallion");var portrait=Find(root,"CatPortrait");
        if(medallion!=null&&portrait!=null)
        {
            (medallion.GetComponent<Mask>()??medallion.gameObject.AddComponent<Mask>()).showMaskGraphic=true;
            portrait.SetParent(medallion,false);U.Fill(portrait,6);
        }
        var stars=Find(root,"WelcomeStars");if(stars!=null)U.At(stars,0,0,872,200);
        At(root,"ReturnSummaryBadge",0,15,872,100);At(root,"SummaryFace",0,0,872,100);
        Tone(root,"SummaryFace",new Color32(223,244,239,255),Color.white,22);
        At(root,"ReturnSummary",42,0,694,88);Type(root,"ReturnSummary",Navy,27);
        Color[] accents={Coral,Teal,Violet};string[] names={"HungerRewardCard","ThirstRewardCard","EnergyRewardCard"};
        for(int i=0;i<3;i++)
        {
            var card=Find(root,names[i]);if(card==null)continue;U.At(card,(i-1)*294,-126,272,140);
            Tone(root,names[i],Color.Lerp(accents[i],Color.white,.79f),Color.Lerp(accents[i],Color.white,.93f),22);
            Panel(card,"NeedAccent",accents[i],Color.Lerp(accents[i],Color.white,.22f),0,-62,222,5,2);
            Type(card,"NeedLabel",Navy,23);Type(card,"Value",Navy,27);
        }
        At(root,"WelcomeBackButton",0,-265,566,82);Action(root,"WelcomeBackButton",Violet);
    }
    static void Welcome(RectTransform card)
    {
        var title=Find(card,"WelcomeTitle");bool runner=title!=null&&title.GetComponent<TMP_Text>().text.Contains("Runner");
        Color accent=runner?Coral:Teal;Color poster=runner?new Color32(26,72,108,255):new Color32(13,89,104,255);
        Tone(card,"WelcomeCard",new Color32(238,244,254,255),Color.white,32);
        var panel=Panel(card,"PlayfulPoster",poster,runner?new Color32(57,114,157,255):new Color32(35,160,157,255),-343,0,654,744,26);
        panel.rectTransform.SetSiblingIndex(1);
        var old=Find(card,"GameIdentityBanner");if(old!=null)old.gameObject.SetActive(false);
        At(card,"WelcomeTitle",-342,314,580,70);Type(card,"WelcomeTitle",Color.white,50);
        At(card,"WelcomeHeroArtFrame",-343,80,612,360);Tone(card,"WelcomeHeroArtFrame",Color.white,Color.white,20);
        var photo=Find(card,"WelcomeHeroArt");if(photo!=null)
        {var image=photo.GetComponent<RawImage>();if(image!=null&&image.texture!=null)image.uvRect=RoomPreviewFit.CoverUv(image.texture.width,image.texture.height,612,360);}
        At(card,"SelectedCatFrame",-582,-175,112,112);At(card,"PlayStyle",-274,-174,444,102);Type(card,"PlayStyle",Color.white,24);
        for(int i=0;i<3;i++)
        {
            var cell=Find(card,"ControlStep"+i);if(cell==null)continue;U.At(cell,-551+i*208,-302,194,104);
            Tone(card,cell.name,Color.Lerp(poster,Color.white,.10f),Color.Lerp(poster,Color.white,.18f),18);
            Type(cell,"Gesture",Gold,32);Type(cell,"Instruction",Color.white,18);
        }
        Copy(card,"PlayfulReady",runner?"Koşuya hazır mısın?":"Patiler hazır mı?",runner?"Ready to run?":"Paws at the ready?",Navy,34,349,312,548,62);
        if(runner)
        {
            At(card,"PlayfulReady",298,312,446,62);
            Art(card,"ScoreStarArt","BonusScoreStar",585,315,104);
        }
        At(card,"Tagline",349,226,548,90);Type(card,"Tagline",ModernUiArt.Muted,24);
        At(card,"BestScorePill",349,128,548,80);Tone(card,"BestScorePill",new Color32(255,215,116,255),new Color32(255,241,197,255),20);
        Type(card,"BestScore",Navy,29);
        At(card,"WelcomeEnergy",349,58,548,36);Type(card,"WelcomeEnergy",Navy,22);
        At(card,"DailyMissions",349,-10,548,82);Type(card,"DailyMissions",ModernUiArt.Muted,20);
        var perk=Panel(card,"PlayfulPerks",Color.Lerp(accent,Color.white,.89f),Color.white,349,-92,548,60,17);
        Copy(perk.transform,"PerkCopy",runner?"2× skor yıldızı  ·  Sürpriz bonuslar":"60 saniye  ·  Hızlı patiler  ·  Kombo",runner?"2× score star  ·  Surprise bonuses":"60 seconds  ·  Quick paws  ·  Combos",runner?Coral:Teal,21,0,0,512,50,TextAlignmentOptions.Center);
        if(runner){Art(perk.transform,"MysteryGiftArt","BonusGift",-232,1,66);At(perk.transform,"PerkCopy",32,0,446,50);}
        At(card,"WelcomeStartButton",349,-193,548,90);At(card,"WelcomeRewardedEnergyButton",349,-193,548,90);
        At(card,"WelcomeExitButton",349,-291,548,64);
        Action(card,"WelcomeStartButton",accent);Action(card,"WelcomeRewardedEnergyButton",accent);Action(card,"WelcomeExitButton",accent,true);
    }
    static void Art(Transform parent,string name,string file,float x,float y,float size)
    {
        var rect=parent.Find(name) as RectTransform;if(rect==null)rect=U.Rect(name,parent);
        U.At(rect,x,y,size,size);var image=rect.GetComponent<RawImage>()??rect.gameObject.AddComponent<RawImage>();
        image.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/MiniGames/Playful/Icons/"+file+".png");image.raycastTarget=false;
    }
    static void Hud(Transform root,bool runner)
    {
        Color accent=runner?Coral:Teal;
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            if(button.name.Contains("Welcome"))continue;
            if(button.name=="PauseButton")PlayfulUiArt.Action(button,Navy);
            else if(button.name.Contains("Resume")||button.name.Contains("Retry")||button.name.Contains("Collect"))PlayfulUiArt.Action(button,accent);
        }
        string[] names={"ScorePanel","CoinHudCapsule","TimerHudCapsule","DistanceHudCapsule","ChancesPanel","HappyBonusPanel","ComboPanel","PowerUpPanel"};
        Color[] colors={Navy,Gold,Teal,Violet,accent,new Color32(225,244,237,255),Gold,Violet};
        for(int i=0;i<names.Length;i++)
        {
            var panel=Find(root,names[i]);if(panel==null)continue;
            Tone(root,names[i],colors[i],Color.Lerp(colors[i],Color.white,.15f),i>3?14:20);
            foreach(var text in panel.GetComponentsInChildren<TMP_Text>(true))text.color=i==1||i==5||i==6?Navy:Color.white;
            if(names[i]=="PowerUpPanel")
            {
                panel.sizeDelta=new Vector2(960,52);
                foreach(var text in panel.GetComponentsInChildren<TMP_Text>(true))
                {U.Fill(text.rectTransform,12);text.fontSize=23;text.enableAutoSizing=true;text.fontSizeMin=19;text.fontSizeMax=23;}
            }
        }
    }
}
