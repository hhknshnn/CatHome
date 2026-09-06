from pathlib import Path
import json
r=Path(__file__).resolve().parents[1]
def edit(path,a,b):
 p=r/path;s=p.read_text(encoding='utf-8-sig');assert a in s,(path,a[:70]);p.write_text(s.replace(a,b),encoding='utf-8')
actions={'BALANCE':'Dengede yürü','BASK':'Dinlen','CLIMB':'Tırman','CURL UP':'Kıvrıl','DIG':'Eşele','DIVE':'İçine atla','DRINK':'Su iç','EAT':'Mama ye','GAZE':'Seyret','GROOM':'Taran','KNEAD':'Yoğur','LOOK':'Bak','NAP':'Kestir','NEST':'Yerleş','PAW':'Pati at','PERCH':'Üzerine çık','PLAY':'Oyna','PLAY YARN':'Yumakla oyna','POUNCE':'Atıl','PUSH':'İt','RINSE':'Duş al','SCRATCH':'Tırmala','SHAKE':'Salla','SLEEP':'Uyu','STARE':'İncele','SUNBATHE':'Güneşlen','SWAT':'Pati at','SWAY':'Sallan','SWING':'Sallan','WARM UP':'Isın','WATCH':'İzle','ZOOM':'Tünelden geç','WAKE UP':'Uyan',
'ZOOMING THROUGH!':'Tünelden geçiyor','BALANCING...':'Dengede yürüyor','NAPPING...':'Kestiriyor','SWINGING!':'Sallanıyor','REACHING...':'Uzanıyor','DIVING...':'İçine atlıyor','SIPPING...':'Su içiyor','EXPLORING...':'Keşfediyor','DIGGING...':'Eşeliyor','PATTING...':'Pati atıyor','PUSHING...':'İtiyor','RINSING...':'Duş alıyor','WARMING UP...':'Isınıyor','KNEADING...':'Yoğuruyor','EATING...':'Mama yiyor','GROOMING...':'Taranıyor','SCRATCHING...':'Tırmalıyor','SPINNING...':'Çeviriyor','SETTLING...':'Yerleşiyor'}
rows='\n'.join('        {'+json.dumps(a,ensure_ascii=False)+','+json.dumps(b,ensure_ascii=False)+'},' for a,b in actions.items())
(r/'Assets/Scripts/Localization/GameInteractionCopy.cs').write_text('''using System.Collections.Generic;
using System.Globalization;
public static class GameInteractionCopy
{
    private static readonly Dictionary<string,string> Turkish=new Dictionary<string,string>
    {
'''+rows+'''
    };
    public static string Text(string value)
    {
        if(string.IsNullOrEmpty(value))return string.Empty;
        bool tr=GameLanguageService.Current==GameLanguage.Turkish;
        if(tr && Turkish.TryGetValue(value,out var translated))return translated;
        string[] prefixes={"CATCH THE BALL  ","CATCH THE MOUSE  ","CHASE THE YARN  "};
        string[] labels=tr?new[]{"Topu yakala  ","Fareyi yakala  ","Yumağı yakala  "}:new[]{"Catch the ball  ","Catch the mouse  ","Chase the yarn  "};
        for(int i=0;i<prefixes.Length;i++)if(value.StartsWith(prefixes[i]))return labels[i]+value.Substring(prefixes[i].Length);
        return CultureInfo.GetCultureInfo("en-US").TextInfo.ToTitleCase(value.ToLowerInvariant()).Replace("...","…");
    }
}
''',encoding='utf-8')
edit('Assets/Scripts/BowlInteraction.cs','    private void SetButtonText(string text)\n    {','    private void SetButtonText(string text)\n    {\n        text=GameInteractionCopy.Text(text);')
for signature in ['private void ApplyButtonAppearance(BowlSetup bowl)','private void ApplySleepButtonAppearance()']:
 edit('Assets/Scripts/BowlInteraction.cs',signature+'\n    {',signature+'''\n    {
        if(interactionButton!=null && interactionButton.targetGraphic is LowPolyPanelGraphic surface)
        {
            surface.SetPremiumBaseColor(PremiumUiStyle.Coral);
            if(buttonText!=null)buttonText.color=PremiumUiStyle.Ink;
            if(buttonShadowText!=null)buttonShadowText.gameObject.SetActive(false);
            return;
        }
''')
edit('Assets/Scripts/Activities/ActivityPromptController.cs','    private void RefreshImmediate()\n    {','''    private void RefreshImmediate()
    {
        if(TitleScreen.IsShowing || GamesHubPanel.IsAnyOpen || LeaderboardPanel.IsAnyOpen ||
           ShopPanelController.IsAnyOpen || QuestPanelController.IsAnyOpen || CatBreedShopPanel.IsAnyOpen ||
           RoomSelectorPanel.IsAnyOpen || SettingsPanel.IsAnyOpen || PrivacyDataPanel.IsAnyOpen ||
           WhileYouWereAwayPopup.IsAnyOpen || HomeLevelUpCelebrationView.IsAnyOpen || CollectionCompleteCelebrationView.IsAnyOpen)
        {candidate=null;HideAction();HideProgress();return;}
''')
edit('Assets/Scripts/Activities/ActivityPromptController.cs','progressLabel.text = text;','progressLabel.text = GameInteractionCopy.Text(text);')
edit('Assets/Scripts/Activities/ActivityPromptController.cs','return $"NEED {Mathf.CeilToInt(activity.EnergyCost)} ENERGY";','return GameContentCopy.Text($"{Mathf.CeilToInt(activity.EnergyCost)} enerji gerekli",$"Need {Mathf.CeilToInt(activity.EnergyCost)} energy");')
edit('Assets/Scripts/Activities/ActivityPromptController.cs','return activity.ActionText;','return GameInteractionCopy.Text(activity.ActionText);')
edit('Assets/Tests/EditMode/ActivityUnlockTests.cs','Is.EqualTo("NEED 12 ENERGY")','Is.EqualTo(GameContentCopy.Text("12 enerji gerekli","Need 12 energy"))')
edit('Assets/Tests/EditMode/ActivityUnlockTests.cs','Is.EqualTo(mouse.ActionText)','Is.EqualTo(GameInteractionCopy.Text(mouse.ActionText))')
edit('Assets/Editor/PremiumUiRefreshBuilder.cs','        Button button = target.GetComponent<Button>();','''        if(target.name=="ActionButton" || target.name=="ActivityActionButton" || target.name=="ActivityProgressBadge")
        { StyleContextAction(target,premiumFont); return; }
        Button button = target.GetComponent<Button>();''')
a='    private static GameObject FindRoot(Scene scene, string name)'
edit('Assets/Editor/PremiumUiRefreshBuilder.cs',a,'''    private static void StyleContextAction(Transform root,TMP_FontAsset font)
    {
        bool progress=root.name=="ActivityProgressBadge";
        foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))
            if(!(graphic is TMP_Text))graphic.enabled=false;
        foreach(var fx in root.GetComponentsInChildren<Shadow>(true))Object.DestroyImmediate(fx);
        foreach(var press in root.GetComponents<LowPolyButtonPress>())Object.DestroyImmediate(press);
        var old=root.Find("ContextFace");
        var surface=old!=null?old.GetComponent<LowPolyPanelGraphic>():PremiumUiElements.Panel("ContextFace",root,progress?PremiumUiStyle.Mint:PremiumUiStyle.Coral,0,0,1,1,24,!progress);
        surface.enabled=true;surface.SetPremiumBaseColor(progress?PremiumUiStyle.Mint:PremiumUiStyle.Coral);
        var bounds=(RectTransform)root;bounds.anchorMin=bounds.anchorMax=new Vector2(progress?.5f:1f,0f);bounds.pivot=new Vector2(.5f,.5f);
        bounds.sizeDelta=progress?new Vector2(420,66):new Vector2(280,94);bounds.anchoredPosition=progress?new Vector2(0,190):new Vector2(-190,204);
        PremiumUiElements.Fill(surface.rectTransform);
        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if(label.name.ToUpperInvariant().Contains("SHADOW")){label.gameObject.SetActive(false);continue;}
            label.transform.SetParent(surface.transform,false);PremiumUiElements.Fill(label.rectTransform,12);
            label.font=font;label.fontSize=26;label.enableAutoSizing=true;label.fontSizeMin=20;label.fontSizeMax=26;
            label.color=PremiumUiStyle.Ink;label.characterSpacing=.5f;label.alignment=TextAlignmentOptions.Center;
        }
        var button=root.GetComponent<Button>();
        if(button!=null){button.targetGraphic=surface;PremiumUiFactory.PolishButton(button,true,font);}
    }

'''+a)
edit('Assets/Scripts/Home/AchievementService.cs','        Title = title;','        englishTitle = title;')
edit('Assets/Scripts/Home/AchievementService.cs','    public string Title { get; }','''    private readonly string englishTitle;
    public string Title => GameLanguageService.Current==GameLanguage.Turkish ? TurkishTitle : englishTitle;
    private string TurkishTitle => Id switch
    {
        "ach.first-run" => "İlk koşu", "ach.first-shop" => "İlk eşyan", "ach.home-level-3" => "Sıcak bir yuva",
        "ach.home-level-5" => "Bahçeye hazır", "ach.bond-80" => "Pencere arkadaşı", "ach.bond-250" => "Kuş dostu",
        "ach.login-streak-7" => "Birlikte bir hafta", _ => englishTitle
    };''')
print('Context actions and remaining achievement copy polished.')
