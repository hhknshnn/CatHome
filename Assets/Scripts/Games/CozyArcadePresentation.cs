using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class CozyArcadePresentation
{
    public static void Apply(Transform canvas,bool runner)
    {
        if(canvas==null)return;
        ApplyHud(canvas,runner);
        var card=canvas.Find("WelcomePanel/WelcomeSafeArea/WelcomeCardLayout");if(card==null)return;
        Set(card,"WelcomeTitle",CozyGameUi.Copy(runner?"Eve Dönüş":"Pati Avı",runner?"Homeward Run":"Paw Hunt"));
        Set(card,"Tagline",CozyGameUi.Copy(runner?"Bahçeden pazara, oradan sıcak evine.\nKüçük bir koşu, güzel bir dönüş.":"Fareye dokun; kedin yaklaşsın ve atlasın.\nÜç turda avcılık yeteneğini göster.",runner?"From the garden to the market, then home.\nA little run. A lovely return.":"Tap a mouse to approach and pounce.\nThree waves of playful hunting."));
        Set(card,"PlayStyle",CozyGameUi.Copy(runner?"75 saniyelik eve dönüş":"60 saniye · 3 küçük tur",runner?"A 75-second journey home":"60 seconds · 3 little waves"));
        Set(card,"PlayfulPerks/PerkCopy",CozyGameUi.Copy(runner?"Eve ulaş · Jeton topla · Çarpmadan bitir":"Fareye dokun · Kedinin avını izle · Serini koru",runner?"Reach home · Collect coins · Avoid bumps":"Tap a mouse · Watch your cat pounce · Keep your streak"));
        if(!runner){Set(card,"ControlStep1/Instruction",CozyGameUi.Copy("Fareye dokun ve yakala","Tap a mouse to catch"));return;}
        var game=canvas.GetComponentInParent<CatRunnerGameController>();if(game==null)return;
        var mission=card.Find("DailyMissions");if(mission!=null)mission.gameObject.SetActive(false);
        var mode=CozyGameUi.Button("RouteMode",card,CozyGameUi.Copy("Mod: Eve Dönüş · 75 sn","Mode: Homeward · 75s"),new Vector2(.55f,.46f),new Vector2(.945f,.54f),null,true);
        mode.onClick.AddListener(()=>{game.SetEndlessMode(!game.EndlessMode);CozyGameUi.Label(mode,game.EndlessMode?CozyGameUi.Copy("Mod: Sonsuz koşu","Mode: Endless run"):CozyGameUi.Copy("Mod: Eve Dönüş · 75 sn","Mode: Homeward · 75s"));});
    }
    private static void ApplyHud(Transform canvas,bool runner)
    {
        foreach(var panel in canvas.GetComponentsInChildren<LowPolyPanelGraphic>(true))
        {
            if(panel.GetComponentInParent<PremiumModalBackdrop>()!=null)continue;
            string name=panel.name;
            if(name.EndsWith("HudCapsule")||name=="ScorePanel"||name=="ChancesPanel"||name=="HappyBonusPanel"||name=="ComboPanel"||name=="ScorePill"||name=="TimerPill"||name=="CatchPill"||name=="HintPill")
            {
                StorybookScreenStyle.RoomShell(panel,18);
                foreach(var label in panel.GetComponentsInChildren<TMP_Text>(true))label.color=StorybookScreenStyle.Ink;
            }
        }
        foreach(var button in canvas.GetComponentsInChildren<Button>(true))
            if(button.name=="PauseButton")StorybookScreenStyle.Action(button,true);
        foreach(var label in canvas.GetComponentsInChildren<TMP_Text>(true))
            if(label.name=="ScoreLabel"||label.name=="CoinLabel"||label.name=="TimerLabel"||label.name=="DistanceLabel"||label.name=="ChancesLabel"||label.name=="HappyBonusLabel"||label.name=="ComboLabel")label.color=StorybookScreenStyle.Ink;
    }
    private static void Set(Transform root,string path,string value)
    {
        var t=root.Find(path);if(t==null)return;var loc=t.GetComponent<LocalizedLabel>();if(loc!=null)loc.enabled=false;var label=t.GetComponent<TMP_Text>();if(label!=null)label.text=value;
    }
}
