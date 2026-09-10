using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using U=PremiumUiElements;

/// <summary>Shared authored native screens for both mini-games.</summary>
public static class PremiumMiniGameUiBuilder
{
    public static void PolishHud(Transform root)
    {
        foreach(var panel in root.GetComponentsInChildren<LowPolyPanelGraphic>(true))
        {
            if(panel.name.EndsWith("Shadow")){panel.gameObject.SetActive(false);continue;}
            if(panel.name=="PawCoinBadge")continue;
            Color tint=panel.name=="ComboPanel"?JoyfulUiArt.Gold:panel.name=="ScorePanel"?JoyfulUiArt.Ocean:JoyfulUiArt.Paper;
            JoyfulUiArt.Surface(panel,tint,22);
        }
        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if(label.name!="CountdownLabel") label.color=PremiumUiStyle.Ink;
            PremiumTypography.Apply(label);
        }
        if(root.name=="SafeArea")CompactRunnerHud(root);
        foreach(var panel in root.GetComponentsInChildren<LowPolyPanelGraphic>(true))
            if(panel.name=="ScorePanel")foreach(var label in panel.GetComponentsInChildren<TMP_Text>(true))label.color=Color.white;
    }
    private static void Place(RectTransform rect,Transform parent,Vector2 anchor,float x,float y,float w,float h)
    {
        if(rect==null)return;rect.SetParent(parent,false);U.At(rect,x,y,w,h);rect.anchorMin=rect.anchorMax=anchor;
    }
    private static void CompactRunnerHud(Transform root)
    {
        var all=root.GetComponentsInChildren<RectTransform>(true);
        RectTransform Find(string n)=>System.Array.Find(all,r=>r.name==n);
        Place(Find("CoinHudCapsule"),root,new Vector2(.5f,1),-126,-64,232,80);
        Place(Find("TimerHudCapsule"),root,new Vector2(.5f,1),126,-64,232,80);
        Place(Find("DistanceHudCapsule"),root,new Vector2(1,1),-276,-64,244,80);
        Place(Find("ScorePanel"),root,new Vector2(0,1),168,-64,272,80);
        Place(Find("ChancesPanel"),root,new Vector2(0,1),168,-139,272,38);
        Place(Find("HappyBonusPanel"),root,new Vector2(.5f,1),0,-139,484,38);
        Place(Find("PauseButton"),root,new Vector2(1,1),-66,-64,76,76);
        Place(Find("ComboPanel"),root,new Vector2(.5f,1),0,-198,290,46);
        Place(Find("PowerUpPanel"),root,new Vector2(.5f,1),0,-261,620,46);
        var score=Find("ScoreLabel");if(score!=null){score.GetComponent<TMP_Text>().fontSize=34;PremiumTypography.Apply(score.GetComponent<TMP_Text>());}
    }
    private static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath);
    private static void Photograph(Transform parent,string name,string path,float x,float y,float width,float height)
    {
        var well=U.Panel(name+"Frame",parent,PremiumUiStyle.Mint,x,y,width,height,24);
        well.gameObject.AddComponent<Mask>().showMaskGraphic=true;
        var rect=U.Rect(name,well.transform);U.Fill(rect,3);
        var image=rect.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
        image.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(image.texture!=null)image.uvRect=RoomPreviewFit.CoverUv(image.texture.width,image.texture.height,width,height);
    }
    private static void CatPortrait(Transform parent,float x,float y,float width,float height)
    {
        var frame=U.Panel("SelectedCatFrame",parent,PremiumUiStyle.Mint,x,y,width,height,24);
        frame.gameObject.AddComponent<Mask>();var rect=U.Rect("SelectedCat",frame.transform);U.Fill(rect,4);
        rect.gameObject.AddComponent<RawImage>().raycastTarget=false;
        rect.gameObject.AddComponent<CatBreedTurntablePreview>();rect.gameObject.AddComponent<SelectedCatShowcase>();
    }
    public sealed class WelcomeParts
    {
        public GameObject Root; public RectTransform Card;
        public TMP_Text Best,Energy,Missions; public Button Start,Exit,Rewarded;
    }
    public sealed class ResultParts
    {
        public GameObject Root,NewBest; public RectTransform Card;
        public TMP_Text Title,Details,Missions; public Button Home,Retry,Double;
    }
    public sealed class PauseParts
    {
        public GameObject Root; public RectTransform Card;
        public Button Resume,Exit,Motion,Sound,Haptics;
    }
    public sealed class TutorialParts
    { public GameObject Root; public RectTransform Card; public TMP_Text Message; public Button Skip; }

    private static RectTransform Modal(Transform parent,string name,Vector2 size,out GameObject root)
    {
        var full=U.Rect(name,parent); U.Fill(full); root=full.gameObject;
        var image=full.gameObject.AddComponent<Image>(); image.color=new Color32(41,58,59,205); image.raycastTarget=true;
        var safe=U.Rect(name=="WelcomePanel"?"WelcomeSafeArea":name=="ResultsPanel"?"ResultsSafeArea":"PauseSafeArea",full); U.Fill(safe); safe.gameObject.AddComponent<SafeAreaRect>();
        var card=U.Rect(name=="WelcomePanel"?"WelcomeCardLayout":name=="ResultsPanel"?"ResultsCardLayout":"PauseCardLayout",safe);
        U.At(card,0,0,size.x,size.y);
        JoyfulUiArt.Panel(name=="WelcomePanel"?"WelcomeCard":name=="ResultsPanel"?"ResultsCard":"PauseCard",card,JoyfulUiArt.Paper,0,0,size.x,size.y,32,true);
        return card;
    }
    public static WelcomeParts Welcome(Transform parent,bool runner)
    {
        var p=new WelcomeParts(); p.Card=Modal(parent,"WelcomePanel",new Vector2(1400,800),out p.Root);
        Color accent=runner?JoyfulUiArt.Coral:JoyfulUiArt.Ocean;
        Photograph(p.Card,"WelcomeHeroArt",runner?"Assets/Art/Games/RunnerPreview.png":"Assets/Art/Games/CatchPreview.png",-346,122,612,374);
        CatPortrait(p.Card,-574,-161,148,148);
        U.Localize(U.Label("PlayStyle",p.Card,Font,26,JoyfulUiArt.Ink,-262,-164,400,110),runner?"runner.card_intro":"catch.card_intro");
        var header=JoyfulUiArt.Panel("GameIdentityBanner",p.Card,accent,342,293,568,110,24);
        JoyfulUiArt.Motif(header.transform,"GameIdentityStars",185,0,168,82,new Color(1,1,1,.3f));
        U.Label("WelcomeTitle",p.Card,Font,52,Color.white,342,293,516,82).text=runner?"Cat Runner":"Cat Catch";
        U.Localize(U.Label("Tagline",p.Card,Font,24,PremiumUiStyle.Muted,342,197,556,72),runner?"runner.intro":"catch.intro");
        var best=JoyfulUiArt.Panel("BestScorePill",p.Card,new Color32(255,233,172,255),342,108,556,74,22);
        p.Best=U.Label("BestScore",best.transform,Font,29,PremiumUiStyle.Ink,0,0,506,56,TextAlignmentOptions.Center);
        p.Energy=U.Label("WelcomeEnergy",p.Card,Font,22,PremiumUiStyle.Ink,342,35,556,44);
        p.Missions=U.Label("DailyMissions",p.Card,Font,20,PremiumUiStyle.Muted,342,-39,556,80);
        if(!runner) U.Localize(p.Missions,"catch.rules");
        p.Start=U.Action("WelcomeStartButton",p.Card,Font,runner?"runner.start":"catch.start",PremiumUiStyle.Coral,342,-145,556,88,out var label);
        p.Rewarded=U.Action("WelcomeRewardedEnergyButton",p.Card,Font,"games.rewarded",PremiumUiStyle.Coral,342,-145,556,88,out label);
        p.Rewarded.gameObject.SetActive(false);
        p.Exit=U.Action("WelcomeExitButton",p.Card,Font,"games.home",PremiumUiStyle.Mint,342,-250,556,70,out label);
        JoyfulUiArt.ActionStyle(p.Start,accent);JoyfulUiArt.ActionStyle(p.Rewarded,accent);JoyfulUiArt.ActionStyle(p.Exit,JoyfulUiArt.SkyPaper,true);
        string[] symbols=runner?new[]{"← →","↑","↓"}:new[]{"1","2","3"};
        string[] tr=runner?new[]{"Şerit değiştir","Zıpla","Alçaktan geç"}:new[]{"Fareyi seç","Yaklaş ve hizalan","Patiyle yakala"};
        string[] en=runner?new[]{"Change lane","Jump","Duck under"}:new[]{"Pick a mouse","Approach & align","Catch with a paw"};
        for(int i=0;i<3;i++)
        {
            var cell=JoyfulUiArt.Panel("ControlStep"+i,p.Card,JoyfulUiArt.SkyPaper,-554+i*208,-297,196,114,18);
            U.Label("Gesture",cell.transform,Font,32,accent,0,25,178,44,TextAlignmentOptions.Center).text=symbols[i];
            U.Label("Instruction",cell.transform,Font,19,JoyfulUiArt.Ink,0,-24,178,42,TextAlignmentOptions.Center).gameObject.AddComponent<BilingualCopyLabel>().Configure(tr[i],en[i]);
        }
        return p;
    }
    public static ResultParts Results(Transform parent,bool runner)
    {
        var p=new ResultParts(); p.Card=Modal(parent,"ResultsPanel",new Vector2(1220,790),out p.Root);
        p.Title=U.Label("ResultTitle",p.Card,Font,42,PremiumUiStyle.Ink,-156,307,780,86);
        var badge=U.Panel("NewBestBadge",p.Card,PremiumUiStyle.ChampagneLight,428,307,236,52,20);p.NewBest=badge.gameObject;
        U.Localize(U.Label("NewBestLabel",badge.transform,Font,21,PremiumUiStyle.Ink,0,0,196,40,TextAlignmentOptions.Center),"games.new_best");
        CatPortrait(p.Card,-360,49,372,388);
        U.Localize(U.Label("ScoreCaption",p.Card,Font,22,PremiumUiStyle.Muted,212,217,596,36,TextAlignmentOptions.Center),"games.score");
        var score=U.Label("Score",p.Card,Font,92,PremiumUiStyle.Ink,212,148,596,110,TextAlignmentOptions.Center);
        U.Localize(U.Label("RewardCaption",p.Card,Font,21,PremiumUiStyle.Muted,164,53,388,36),"games.earned");
        var coin=U.Rect("PawCoin",p.Card); U.At(coin,-60,2,54,54); PremiumUiFactory.BuildCurrencyIcon(coin,PremiumUiFactory.CurrencyVisual.Coin,false);
        var reward=U.Label("Reward",p.Card,Font,40,PremiumUiStyle.Teal,190,0,400,60);
        p.Root.AddComponent<MiniGameResultView>().Configure(score,reward);
        p.Details=U.Label("ResultDetails",p.Card,Font,22,PremiumUiStyle.Muted,208,-91,636,104,TextAlignmentOptions.Center);
        p.Missions=U.Label("ResultMissions",p.Card,Font,19,PremiumUiStyle.Muted,-242,-219,620,78,TextAlignmentOptions.Center);
        p.Double=U.Action("DoubleCoinsButton",p.Card,Font,"celebration.double",PremiumUiStyle.ChampagneLight,341,-209,412,66,out var label);
        p.Double.gameObject.SetActive(false);
        p.Home=U.Action("CollectButton",p.Card,Font,"games.home",PremiumUiStyle.Mint,-280,-313,536,84,out label);
        p.Retry=U.Action("RetryButton",p.Card,Font,runner?"runner.again":"catch.again",PremiumUiStyle.Coral,280,-313,536,84,out label);
        var resultBanner=JoyfulUiArt.Panel("ResultBanner",p.Card,runner?JoyfulUiArt.Coral:JoyfulUiArt.Ocean,0,304,1148,116,24);
        resultBanner.transform.SetSiblingIndex(1);p.Title.color=Color.white;
        JoyfulUiArt.Surface(badge,JoyfulUiArt.Gold,16);
        JoyfulUiArt.ActionStyle(p.Retry,runner?JoyfulUiArt.Coral:JoyfulUiArt.Ocean);
        JoyfulUiArt.ActionStyle(p.Home,JoyfulUiArt.SkyPaper,true);
        var scoreWell=JoyfulUiArt.Panel("ScoreStage",p.Card,JoyfulUiArt.SkyPaper,212,153,596,176,22);scoreWell.transform.SetSiblingIndex(2);
        var confetti=U.Rect("ResultConfetti",p.Card);U.Fill(confetti);confetti.gameObject.AddComponent<JoyfulConfettiGraphic>();
        return p;
    }
    public static PauseParts Pause(Transform parent,bool preferences)
    {
        var p=new PauseParts();p.Card=Modal(parent,"PausePanel",new Vector2(860,preferences?760:448),out p.Root);
        U.Localize(U.Label("PauseTitle",p.Card,Font,44,PremiumUiStyle.Ink,0,preferences?287:145,728,78,TextAlignmentOptions.Center),"games.paused");
        if(preferences)
        {
            var divider=U.Panel("PreferenceSection",p.Card,PremiumUiStyle.Mint,0,preferences?-29:0,768,286,24);
            divider.transform.SetSiblingIndex(1);
        }
        p.Resume=U.Action("ResumeButton",p.Card,Font,"games.resume",PremiumUiStyle.Coral,0,preferences?173:25,728,86,out var label);
        if(preferences)
        {
            p.Motion=U.Action("ReducedMotionButton",p.Card,Font,null,PremiumUiStyle.WarmIvory,0,55,728,64,out label);
            p.Sound=U.Action("SoundButton",p.Card,Font,null,PremiumUiStyle.WarmIvory,0,-29,728,64,out label);
            p.Haptics=U.Action("HapticsButton",p.Card,Font,null,PremiumUiStyle.WarmIvory,0,-113,728,64,out label);
        }
        p.Exit=U.Action("PauseExitButton",p.Card,Font,"games.home",PremiumUiStyle.Mint,0,preferences?-270:-124,728,72,out label);
        return p;
    }
    public static TutorialParts Tutorial(Transform parent)
    {
        var p=new TutorialParts(); var full=U.Rect("TutorialPanel",parent);U.Fill(full);p.Root=full.gameObject;
        var safe=U.Rect("TutorialSafeArea",full);U.Fill(safe);safe.gameObject.AddComponent<SafeAreaRect>();
        p.Card=U.Rect("TutorialCardLayout",safe);U.At(p.Card,0,-270,820,160);p.Card.anchorMin=p.Card.anchorMax=new Vector2(.5f,1);
        U.Panel("TutorialCard",p.Card,PremiumUiStyle.Ivory,0,0,820,160,24);
        p.Message=U.Label("TutorialMessage",p.Card,Font,28,PremiumUiStyle.Ink,-98,0,570,126,TextAlignmentOptions.Center);
        p.Skip=U.Action("TutorialSkipButton",p.Card,Font,"games.skip",PremiumUiStyle.Mint,298,0,164,66,out var label);
        return p;
    }
}
