using TMPro;
using UnityEngine;
using UnityEngine.UI;
using U=PremiumUiElements;

/// <summary>Compact home shortcut, native companion cards and a reusable illustrated guide.</summary>
public sealed class CatCompanionPanel : MonoBehaviour
{
    private static CatCompanionPanel instance;
    private RectTransform safe,card;
    private GameObject modal,commands,guide;
    private Button dock,stop,previous,next,playTab,guideTab;
    private TMP_Text dockLabel,title,subtitle,status,pageTitle,pageBody,pageNumber,stopLabel;
    private CatMovement cat;
    private MainPanelController menu;
    private ScrollRect guideScroll;
    private RawImage guideIcon;
    private GameObject needsRow;
    private readonly TMP_Text[] needsLabels=new TMP_Text[3];
    private readonly Button[] commandButtons=new Button[3];
    private readonly RawImage[] commandPhotos=new RawImage[3];
    private HungerSystem hunger;
    private ThirstSystem thirst;
    private EnergySystem energy;
    private int page;
    public static bool IsAnyOpen=>instance!=null&&instance.modal!=null&&instance.modal.activeSelf;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {if(instance!=null)return;var root=new GameObject("CatCompanionCanvas",typeof(RectTransform));DontDestroyOnLoad(root);instance=root.AddComponent<CatCompanionPanel>();}
    private void Awake()
    {
        instance=this;
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=135;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        safe=U.Rect("SafeArea",transform);U.Fill(safe);safe.gameObject.AddComponent<SafeAreaRect>();
        dock=U.Action("CompanionShortcut",safe,PremiumTypography.Emphasis,null,ModernUiArt.Paper,135,40,248,56,out dockLabel);
        ModernUiArt.FlatNavigation(dock);
        JoyfulUiArt.Icon("TogetherPaw",dock.targetGraphic.transform,"Paw",-83,0,48);
        U.At(dockLabel.rectTransform,23,0,162,42);dockLabel.fontSize=21;
        var dockRect=(RectTransform)dock.transform;dockRect.anchorMin=dockRect.anchorMax=new Vector2(.5f,0);dock.onClick.AddListener(()=>Show(false));
        var full=U.Rect("CompanionModal",safe);U.Fill(full);modal=full.gameObject;
        var shade=full.gameObject.AddComponent<Image>();shade.color=new Color32(36,53,54,180);shade.raycastTarget=true;
        full.gameObject.AddComponent<PremiumModalBackdrop>();
        card=U.Rect("CompanionCard",full);U.At(card,0,0,1160,820);
        var paper=U.Panel("Paper",card,Color.white,0,0,1160,820,32,true);
        paper.ConfigureModernStyle(Color.white,new Color32(246,249,255,255),32,true);
        var header=U.Panel("HeaderWash",card,new Color32(235,244,251,255),0,292,1096,174,24);
        header.ConfigureModernStyle(new Color32(245,250,255,255),new Color32(233,243,250,255),24);
        JoyfulUiArt.Icon("CompanionHeaderPaw",card,"Paw",-474,311,76);
        title=U.Label("Title",card,PremiumTypography.Emphasis,43,ModernUiArt.Ink,31,328,868,62);
        subtitle=U.Label("Subtitle",card,PremiumTypography.Body,23,ModernUiArt.Muted,31,275,868,48);
        var close=Button("Close",card,"×",PremiumUiStyle.Ivory,513,345,58,58);close.onClick.AddListener(Close);
        ModernUiArt.FlatNavigation(close);
        var tabs=U.Panel("CompanionTabs",card,ModernUiArt.Inset,0,178,1048,68,17);
        tabs.ConfigureModernStyle(ModernUiArt.Inset,ModernUiArt.Inset,17);
        playTab=Button("TogetherTab",card,GameContentCopy.Text("Kedi komutları","Cat commands"),PremiumUiStyle.Ivory,-258,178,510,56);playTab.onClick.AddListener(()=>Show(false));
        guideTab=Button("GuideTab",card,GameContentCopy.Text("Oyun rehberi","Play guide"),PremiumUiStyle.Ivory,258,178,510,56);guideTab.onClick.AddListener(()=>Show(true));
        playTab.GetComponentInChildren<TMP_Text>().gameObject.AddComponent<BilingualCopyLabel>().Configure("Kedi komutları","Cat commands");
        guideTab.GetComponentInChildren<TMP_Text>().gameObject.AddComponent<BilingualCopyLabel>().Configure("Oyun rehberi","Play guide");
        commands=U.Rect("CommandCards",card).gameObject;U.Fill((RectTransform)commands.transform);
        for(int i=0;i<3;i++)
        {
            int choice=i;float x=(i-1)*354;
            var tile=U.Panel("Command"+i,commands.transform,Color.white,x,-40,334,330,24);
            tile.ConfigureModernStyle(Color.white,Color.white,24,true);
            var photoFrame=U.Panel("PoseFrame",tile.transform,new Color32(235,243,250,255),0,51,302,170,18);
            photoFrame.ConfigureModernStyle(new Color32(235,243,250,255),new Color32(235,243,250,255),18);
            photoFrame.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            var art=U.Rect("PosePhoto",photoFrame.transform);U.Fill(art);
            var image=art.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>("Companion/"+(i==0?"Meow":i==1?"Sit":"Loaf"));image.raycastTarget=false;
            commandPhotos[i]=image;
            string caption=i==0?GameContentCopy.Text("Miyavla","Meow"):i==1?GameContentCopy.Text("Otur","Sit"):"Loaf";
            var hint=U.Label("CommandHint",tile.transform,PremiumTypography.Body,19,ModernUiArt.Muted,0,-51,286,32,TextAlignmentOptions.Center);
            hint.gameObject.AddComponent<BilingualCopyLabel>().Configure(i==0?"Küçük bir selam":i==1?"Sakin bir mola":"Rahatça kıvrıl",i==0?"A little hello":i==1?"A quiet moment":"Curl up and relax");
            var action=Button("CommandButton"+i,tile.transform,caption,PremiumUiStyle.Ivory,0,-115,286,62);
            commandButtons[i]=action;
            ModernUiArt.Action(action);
            action.GetComponentInChildren<TMP_Text>().gameObject.AddComponent<BilingualCopyLabel>().Configure(i==0?"Miyavla":i==1?"Otur":"Loaf",i==0?"Meow":i==1?"Sit":"Loaf");
            action.onClick.AddListener(()=>Issue((CatCompanionCommand)choice));
        }
        status=U.Label("CatStatus",commands.transform,PremiumTypography.Body,22,ModernUiArt.Muted,0,-247,1038,48,TextAlignmentOptions.Center);
        stop=U.Action("StopRest",commands.transform,PremiumTypography.Emphasis,null,ModernUiArt.Azure,0,-334,570,72,out stopLabel);stop.onClick.AddListener(()=>{var active=CatActivity.Active;Close();if(active!=null)active.RequestRestStop();});
        ModernUiArt.Action(stop);
        needsRow=U.Rect("CurrentNeeds",commands.transform).gameObject;U.Fill((RectTransform)needsRow.transform);
        string[] needIcons={"Food","Water","Energy"};
        for(int i=0;i<3;i++)
        {
            var chip=U.Panel("Need"+i,needsRow.transform,ModernUiArt.Inset,(i-1)*354,-334,334,72,18);
            chip.ConfigureModernStyle(new Color32(239,245,252,255),new Color32(239,245,252,255),18);
            var icon=U.Rect("Icon",chip.transform);U.At(icon,-105,0,44,44);
            var picture=icon.gameObject.AddComponent<RawImage>();picture.texture=Resources.Load<Texture2D>("PremiumInterface/"+needIcons[i]);picture.raycastTarget=false;
            needsLabels[i]=U.Label("Value",chip.transform,PremiumTypography.Body,22,PremiumUiStyle.Ink,22,0,214,54,TextAlignmentOptions.Center);
        }
        guide=U.Rect("GuidePages",card).gameObject;U.Fill((RectTransform)guide.transform);
        JoyfulUiArt.Panel("GuidePaper",guide.transform,JoyfulUiArt.SkyPaper,0,-90,1010,378,24);
        var iconRect=U.Rect("GuideIcon",guide.transform);U.At(iconRect,-466,64,48,48);guideIcon=iconRect.gameObject.AddComponent<RawImage>();guideIcon.raycastTarget=false;
        pageTitle=U.Label("GuideTitle",guide.transform,PremiumTypography.Emphasis,30,PremiumUiStyle.Teal,36,64,870,52);
        var viewport=U.Rect("GuideViewport",guide.transform);U.At(viewport,0,-108,942,270);
        viewport.gameObject.AddComponent<RectMask2D>();
        var scrollSurface=viewport.gameObject.AddComponent<Image>();scrollSurface.color=Color.clear;scrollSurface.raycastTarget=true;
        pageBody=U.Label("GuideCopy",viewport,PremiumTypography.Body,22,PremiumUiStyle.Ink,0,0,912,292);
        var bodyRect=pageBody.rectTransform;bodyRect.anchorMin=new Vector2(0,1);bodyRect.anchorMax=Vector2.one;bodyRect.pivot=new Vector2(.5f,1);bodyRect.anchoredPosition=Vector2.zero;bodyRect.sizeDelta=new Vector2(-30,0);
        bodyRect.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        guideScroll=viewport.gameObject.AddComponent<ScrollRect>();guideScroll.viewport=viewport;guideScroll.content=bodyRect;guideScroll.horizontal=false;guideScroll.movementType=ScrollRect.MovementType.Clamped;guideScroll.scrollSensitivity=28;
        PremiumScrollInput.Ensure(guideScroll);
        pageNumber=U.Label("GuidePage",guide.transform,PremiumTypography.Body,22,PremiumUiStyle.Muted,0,-314,250,44,TextAlignmentOptions.Center);
        previous=Button("Previous",guide.transform,GameContentCopy.Text("Geri","Back"),PremiumUiStyle.Mint,-350,-314,260,64);previous.onClick.AddListener(()=>{page=Mathf.Max(0,page-1);RefreshGuide();});
        previous.GetComponentInChildren<TMP_Text>().gameObject.AddComponent<BilingualCopyLabel>().Configure("Geri","Back");
        next=Button("Next",guide.transform,GameContentCopy.Text("Devam","Next"),PremiumUiStyle.Coral,350,-314,260,64);next.onClick.AddListener(()=>{if(page==HomeGuideContent.Count-1)Close();else{page++;RefreshGuide();}});
        modal.SetActive(false);
    }
    private static Button Button(string name,Transform parent,string text,Color color,float x,float y,float w,float h)
    {var b=U.Action(name,parent,PremiumTypography.Emphasis,null,color,x,y,w,h,out var label);label.text=text;JoyfulUiArt.ActionStyle(b,color,color==PremiumUiStyle.Mint||color==PremiumUiStyle.Ivory);return b;}
    private bool AnotherPanel=>TitleScreen.IsShowing||HomeUiFlow.IsMiniGameVisible||CatDialogueView.IsAnyVisible||SettingsPanel.IsAnyOpen||ShopPanelController.IsAnyOpen||QuestPanelController.IsAnyOpen||RoomSelectorPanel.IsAnyOpen||CatBreedShopPanel.IsAnyOpen||GamesHubPanel.IsAnyOpen||LeaderboardPanel.IsAnyOpen||PrivacyDataPanel.IsAnyOpen||WhileYouWereAwayPopup.IsAnyOpen||OnboardingCelebrationView.IsAnyOpen||CollectionCompleteCelebrationView.IsAnyOpen||HomeLevelUpCelebrationView.IsAnyOpen;
    private void Update()
    {
        if(cat==null)cat=FindFirstObjectByType<CatMovement>();
        if(menu==null)menu=FindFirstObjectByType<MainPanelController>();
        bool available=cat!=null&&PetTutorialHint.IsOnboardingCompleted&&!AnotherPanel&&(menu==null||!menu.IsOpen);
        dock.gameObject.SetActive(available&&!modal.activeSelf);
        float dockScale=Mathf.Min(1f,(safe.rect.width-32f)/1080f);
        dock.transform.localScale=Vector3.one*dockScale;
        ((RectTransform)dock.transform).anchoredPosition=new Vector2(135*dockScale,40);
        dockLabel.text=GameContentCopy.Text("Kedi komutları","Cat commands");
        if(modal.activeSelf&&(!available||cat==null)){Close();return;}
        if(modal.activeSelf)
        {
            card.localScale=Vector3.one*Mathf.Min(1,(safe.rect.width-40f)/1160f,(safe.rect.height-40f)/820f);
            bool resting=CatActivity.Active!=null&&CatActivity.Active.IsWaitingForRestStop;
            bool busy=CatActionState.IsBusy(cat);
            foreach(var commandButton in commandButtons)commandButton.interactable=!busy;
            needsRow.SetActive(!resting);
            if(!resting)
            {
                if(hunger==null)hunger=FindFirstObjectByType<HungerSystem>();
                if(thirst==null)thirst=FindFirstObjectByType<ThirstSystem>();
                if(energy==null)energy=FindFirstObjectByType<EnergySystem>();
                needsLabels[0].text=GameContentCopy.Text("Tokluk","Fullness")+" · "+(hunger!=null?Mathf.RoundToInt(hunger.CurrentHunger).ToString():"—")+"%";
                needsLabels[1].text=GameContentCopy.Text("Su","Water")+" · "+(thirst!=null?Mathf.RoundToInt(thirst.CurrentThirst).ToString():"—")+"%";
                needsLabels[2].text=GameContentCopy.Text("Enerji","Energy")+" · "+(energy!=null?Mathf.RoundToInt(energy.CurrentEnergy).ToString():"—")+"%";
            }
            stop.gameObject.SetActive(resting);stopLabel.text=GameContentCopy.Text("Kalk · Dinlenmeyi bitir","Get up · Finish resting");
            status.text=resting?GameContentCopy.Text("Acele yok. Dinlenirken enerjim yavaşça doluyor.","No hurry. My energy gently recovers as I rest."):busy?GameContentCopy.Text("Bu hareketi bitirince yeni bir komut seçebilirsin.","Choose another command when this action finishes."):GameContentCopy.Text("Otur ve Loaf sırasında enerji yavaşça dolar.","Sit and Loaf gently restore energy.");
        }
    }
    public static void OpenGuide(){if(instance!=null)instance.Show(true);}
    public void Show(bool help)
    {
        if(cat==null)cat=FindFirstObjectByType<CatMovement>();if(cat==null||AnotherPanel)return;
        cat.AcquireInputBlock(this);modal.SetActive(true);commands.SetActive(!help);guide.SetActive(help);
        for(int i=0;i<3;i++)
        {
            string pose=i==0?"Meow":i==1?"Sit":"Loaf";
            commandPhotos[i].texture=Resources.Load<Texture2D>("Companion/"+CatBreedService.SelectedBreedId+"/"+pose)??Resources.Load<Texture2D>("Companion/"+pose);
        }
        StyleTab(playTab,!help);StyleTab(guideTab,help);
        title.text=GameContentCopy.Text("Sen ve ","You and ")+PetTutorialHint.CatName;
        subtitle.text=GameContentCopy.Text("Bir komut seç, küçük bir anı paylaş.","Choose a command. Share a little moment.");RefreshGuide();
    }
    static void StyleTab(Button tab,bool selected)
    {
        ModernUiArt.FlatNavigation(tab);
        var surface=tab.targetGraphic as LowPolyPanelGraphic;
        var tone=selected?ModernUiArt.Ink:ModernUiArt.Inset;
        if(surface!=null)surface.ConfigureModernStyle(tone,tone,13);
        foreach(var label in tab.GetComponentsInChildren<TMP_Text>())label.color=selected?Color.white:ModernUiArt.Muted;
    }
    private void RefreshGuide()
    {
        pageTitle.text=HomeGuideContent.Title(page);pageBody.text=HomeGuideContent.Body(page);
        string[] icons={"Paw","Food","Rooms","Paw","Games","Energy"};guideIcon.texture=Resources.Load<Texture2D>("PremiumInterface/"+icons[page]);
        pageNumber.text=(page+1)+" / "+HomeGuideContent.Count+"  ·  "+GameContentCopy.Text("Kaydır","Scroll");
        previous.interactable=page>0;next.GetComponentInChildren<TMP_Text>().text=page==HomeGuideContent.Count-1?GameContentCopy.Text("Anladım","Got it"):GameContentCopy.Text("Devam","Next");
        Canvas.ForceUpdateCanvases();guideScroll.verticalNormalizedPosition=1;
    }
    private void Issue(CatCompanionCommand command)
    {var target=cat;Close();if(target!=null){var activity=target.GetComponent<CatCommandActivity>()??target.gameObject.AddComponent<CatCommandActivity>();activity.Issue(command);}}
    public void Close(){if(cat!=null)cat.ReleaseInputBlock(this);if(modal!=null)modal.SetActive(false);}
    private void OnDisable()=>Close();
}
