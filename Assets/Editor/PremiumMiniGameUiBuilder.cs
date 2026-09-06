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
            panel.SetPremiumBaseColor(panel.name=="ComboPanel"?PremiumUiStyle.ChampagneLight:PremiumUiStyle.Ivory);
        }
        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
            if(label.name!="CountdownLabel") label.color=PremiumUiStyle.Ink;
    }
    private static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath);
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
        U.Panel(name=="WelcomePanel"?"WelcomeCard":name=="ResultsPanel"?"ResultsCard":"PauseCard",card,PremiumUiStyle.Ivory,0,0,size.x,size.y,32,true);
        return card;
    }
    public static WelcomeParts Welcome(Transform parent,bool runner)
    {
        var p=new WelcomeParts(); p.Card=Modal(parent,"WelcomePanel",new Vector2(1400,800),out p.Root);
        var portraitWell=U.Panel("WelcomeHeroFrame",p.Card,PremiumUiStyle.Mint,-346,2,612,612,24);
        portraitWell.gameObject.AddComponent<Mask>().showMaskGraphic=true;
        var image=U.Rect("WelcomeHeroArt",portraitWell.transform); U.Fill(image,5);
        image.gameObject.AddComponent<RawImage>(); image.gameObject.AddComponent<CatBreedTurntablePreview>(); image.gameObject.AddComponent<SelectedCatShowcase>();
        U.Label("WelcomeTitle",p.Card,Font,52,PremiumUiStyle.Ink,342,293,556,82).text=runner?"Cat Runner":"Cat Catch";
        U.Localize(U.Label("Tagline",p.Card,Font,24,PremiumUiStyle.Muted,342,211,556,78),runner?"runner.intro":"catch.intro");
        var best=U.Panel("BestScorePill",p.Card,PremiumUiStyle.Mint,342,108,556,74,22);
        p.Best=U.Label("BestScore",best.transform,Font,29,PremiumUiStyle.Ink,0,0,506,56,TextAlignmentOptions.Center);
        p.Energy=U.Label("WelcomeEnergy",p.Card,Font,22,PremiumUiStyle.Ink,342,35,556,44);
        p.Missions=U.Label("DailyMissions",p.Card,Font,20,PremiumUiStyle.Muted,342,-39,556,80);
        if(!runner) U.Localize(p.Missions,"catch.rules");
        p.Start=U.Action("WelcomeStartButton",p.Card,Font,runner?"runner.start":"catch.start",PremiumUiStyle.Coral,342,-145,556,88,out var label);
        p.Rewarded=U.Action("WelcomeRewardedEnergyButton",p.Card,Font,"games.rewarded",PremiumUiStyle.Coral,342,-145,556,88,out label);
        p.Rewarded.gameObject.SetActive(false);
        p.Exit=U.Action("WelcomeExitButton",p.Card,Font,"games.home",PremiumUiStyle.Mint,342,-250,556,70,out label);
        U.Localize(U.Label("Controls",p.Card,Font,19,PremiumUiStyle.Muted,-346,-344,612,55,TextAlignmentOptions.Center),runner?"runner.controls":"catch.controls");
        return p;
    }
    public static ResultParts Results(Transform parent,bool runner)
    {
        var p=new ResultParts(); p.Card=Modal(parent,"ResultsPanel",new Vector2(1000,800),out p.Root);
        p.Title=U.Label("ResultTitle",p.Card,Font,42,PremiumUiStyle.Ink,-108,312,650,86);
        var badge=U.Panel("NewBestBadge",p.Card,PremiumUiStyle.ChampagneLight,329,306,224,52,20);p.NewBest=badge.gameObject;
        U.Localize(U.Label("NewBestLabel",badge.transform,Font,21,PremiumUiStyle.Ink,0,0,196,40,TextAlignmentOptions.Center),"games.new_best");
        U.Localize(U.Label("ScoreCaption",p.Card,Font,22,PremiumUiStyle.Muted,-220,225,360,36,TextAlignmentOptions.Center),"games.score");
        var score=U.Label("Score",p.Card,Font,86,PremiumUiStyle.Ink,-220,155,370,110,TextAlignmentOptions.Center);
        U.Localize(U.Label("RewardCaption",p.Card,Font,22,PremiumUiStyle.Muted,220,225,360,36,TextAlignmentOptions.Center),"games.earned");
        var coin=U.Rect("PawCoin",p.Card); U.At(coin,116,150,74,74); PremiumUiFactory.BuildCurrencyIcon(coin,PremiumUiFactory.CurrencyVisual.Coin,false);
        var reward=U.Label("Reward",p.Card,Font,54,PremiumUiStyle.Teal,267,152,280,100,TextAlignmentOptions.Center);
        p.Root.AddComponent<MiniGameResultView>().Configure(score,reward);
        var stats=U.Panel("ResultStatsSurface",p.Card,PremiumUiStyle.WarmIvory,0,-4,876,150,24);
        p.Details=U.Label("ResultDetails",stats.transform,Font,23,PremiumUiStyle.Ink,0,0,802,132,TextAlignmentOptions.Center);
        p.Missions=U.Label("ResultMissions",p.Card,Font,19,PremiumUiStyle.Muted,0,-120,876,54,TextAlignmentOptions.Center);
        p.Double=U.Action("DoubleCoinsButton",p.Card,Font,"celebration.double",PremiumUiStyle.ChampagneLight,0,-199,430,62,out var label);
        p.Double.gameObject.SetActive(false);
        p.Home=U.Action("CollectButton",p.Card,Font,"games.home",PremiumUiStyle.Mint,-225,-302,426,84,out label);
        p.Retry=U.Action("RetryButton",p.Card,Font,runner?"runner.again":"catch.again",PremiumUiStyle.Coral,225,-302,426,84,out label);
        return p;
    }
    public static PauseParts Pause(Transform parent,bool preferences)
    {
        var p=new PauseParts();p.Card=Modal(parent,"PausePanel",new Vector2(860,preferences?760:448),out p.Root);
        U.Localize(U.Label("PauseTitle",p.Card,Font,44,PremiumUiStyle.Ink,0,preferences?287:145,728,78,TextAlignmentOptions.Center),"games.paused");
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
