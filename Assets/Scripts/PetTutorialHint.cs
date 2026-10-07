using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(11000), DisallowMultipleComponent]
public sealed class PetTutorialHint : MonoBehaviour
{
    public const string PlayerPrefsKey = "CatHome_PetTutorialCompleted";
    public const string CatNameKey = "CatHome_CatName";
    public const string IntroductionCompletedKey = "CatHome_IntroductionCompleted";
    public const string IntroductionStepKey = "CatHome_IntroductionStep";
    public const string OnboardingStepKey = "CatHome_OnboardingStep";
    public const string OnboardingCompletedKey = "CatHome_OnboardingCompleted";
    private const int CurrentVisualVersion = 13;
    private const int CelebrationStepIndex = 5;
    private const int CompleteStepIndex = 6;
    private static readonly Vector4 NeedsSpotlightPadding = new Vector4(
        12f, 12f, 12f, 12f
    );

    public enum StepKind { PetTheCat, MoveYourCat, WatchNeeds, FoodAndWater, RestInBed, Celebration, Complete }
    public enum CompletionKind { PetGesture, MoveDistance, TapDialogue }
    public enum IconKind { SwipeHand, None }

    [Serializable]
    public sealed class StepDefinition
    {
        public StepKind kind;
        public string message;
        public Transform target;
        public Transform secondaryTarget;
        public Transform tertiaryTarget;
        public Vector2 screenOffset;
        public bool usePointer = true;
        public IconKind icon;
        public CompletionKind completion;
        public bool showGotIt;
    }

    [Header("References")]
    [SerializeField] private PetInteraction petInteraction;
    [SerializeField] private Transform catTarget;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform hintRoot;
    [SerializeField] private RectTransform cardRoot;
    [SerializeField] private RectTransform pawIcon;
    [SerializeField] private LowPolySwipeGraphic swipeGraphic;
    [SerializeField] private LowPolyTutorialPointerGraphic pointerGraphic;
    [SerializeField] private TMP_Text instructionLabel;
    [SerializeField] private WhileYouWereAwayPopup whileYouWereAwayPopup;
    [SerializeField] private RectTransform actionButton;
    [SerializeField] private StepDefinition[] steps;
    [SerializeField, HideInInspector] private int visualVersion = CurrentVisualVersion;

    [Header("Compact Card Style")]
    [SerializeField] private Vector2 cardSize = new Vector2(320f, 176f);
    [SerializeField, Min(1f)] private float horizontalTravel = 46f;
    [SerializeField, Min(.1f)] private float swipePeriod = 2.1f;
    [SerializeField, Min(.1f)] private float floatPeriod = 3.2f;
    [SerializeField, Min(0f)] private float floatDistance = 3f;
    [SerializeField, Min(.01f)] private float fadeDuration = .22f;
    [SerializeField, Min(0f)] private float targetGapPixels = 28f;
    [SerializeField, Min(0f)] private float catFallbackHeight = .72f;
    [SerializeField, Min(0f)] private float safeAreaPadding = 20f;
    [SerializeField, Min(0f)] private float topUiClearance = 104f;
    [SerializeField, Min(0f)] private float bottomUiClearance = 118f;
    [SerializeField, Range(.2f, .3f)] private float movementDistance = .25f;

    private readonly string[] introduction =
    {
        "Hi! I’m {0}. I’m so happy you’re here!",
        "Keep an eye on my hunger, thirst, and energy.",
        "You can pet me, help me eat and drink, and tuck me into bed when I’m tired.",
        "Let’s make this little home ours!"
    };
    private static string IntroductionLine(int index, string name)
    {
        string[] tr = {
            "Merhaba! Ben {0}. Önce birbirimizi tanıyalım; sonra yuvamızı birlikte keşfedelim.",
            "Beni okşamak için üzerimde parmağını gezdir. Sol alttaki analogla yürüt; daha çok çektiğinde koşarım.",
            "Mama, su ve yatağa yaklaşınca ilgili düğme açılır. Dinlenirken sen Kalk diyene kadar yerimde kalırım.",
            "Kedi komutları düğmesinden miyavlama, oturma ve patilerini toplama seçebilirsin. Ayrıntılı oyun rehberi de orada. Şimdi birlikte deneyelim!"
        };
        string[] en = {
            "Hi! I'm {0}. Let's get to know each other, then explore our home together.",
            "Stroke me with your finger to pet me. Use the lower-left stick to walk; pull farther to run.",
            "Approach food, water or a bed to see its action. When resting, I stay until you choose Get up.",
            "The Cat commands button offers meowing, sitting and loafing, plus a detailed play guide. Let's try things together!"
        };
        return string.Format(GameContentCopy.Text(tr[index], en[index]), name);
    }
    private Coroutine transitionRoutine;
    private Vector2 pawRestPosition, cardRestPosition;
    private Vector3 movementStart;
    private int currentStep, introStep;
    private bool cardVisible, popupWasOpen, movementInputObserved, advancing;
    private int popupReleaseFrame = -1;
    private TutorialSpotlight spotlight;
    private Button spotlightButton;
    private CatDialogueView dialogue;
    private RectTransform spotlightCopy;
    private TMP_Text spotlightTitle, spotlightSubtitle;
    private RectTransform movePortrait;
    private OnboardingCelebrationView celebrationView;
    private float onboardingReleaseTime;
    private CatMovement onboardingMovement;
    private bool ownsOnboardingInputBlock;
    private bool lifecycleReady;
    private bool onboardingSessionActive;
    private CatInputCategory appliedInputPolicy = CatInputCategory.None;
    private readonly Transform[] spotlightTargets = new Transform[3];
    private readonly Vector3[] targetCorners = new Vector3[4];
    private TMP_Text lessonProgress;
    private int lessonProgressStep = -1;
    private GameLanguage lessonProgressLanguage;
    private bool targetsDirty = true;
    private int boundStep = -1;
    private float nextTargetRetry;
    private Transform cachedHeadCat, cachedHead;

    public static bool IsCompleted => PlayerPrefs.GetInt(PlayerPrefsKey, 0) == 1;
    public static bool IsOnboardingCompleted => PlayerPrefs.GetInt(OnboardingCompletedKey, 0) == 1;
    public static bool IsShowingNeedsGuide { get; private set; }
    public static string CatName => CatDialogueView.NormalizeName(PlayerPrefs.GetString(CatNameKey, string.Empty));

    private void Awake()
    {
        transform.localScale = Vector3.one;
        ResolveReferences();
        SubscribeToPetInteraction();
        EnsureInputInfrastructure();
        EnsureDefaultSteps();
        MigrateLegacyProgress();
        currentStep = Mathf.Clamp(PlayerPrefs.GetInt(OnboardingStepKey, 0), 0, CompleteStepIndex);
        introStep = Mathf.Clamp(PlayerPrefs.GetInt(IntroductionStepKey, 0), 0, introduction.Length);
        EnsureRuntimeViews();
        movementStart = catTarget != null ? catTarget.position : Vector3.zero;
        SetAllHidden();
        lifecycleReady=true;
        RefreshOnboardingInputBlock();
    }

    private void Start() => EvaluateVisibility();

    private void OnEnable()
    {
        SubscribeToPetInteraction();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        CatBreedService.Changed += InvalidateTargets;
        targetsDirty = true;
        if(lifecycleReady)RefreshOnboardingInputBlock();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => InvalidateTargets();
    private void InvalidateTargets() { targetsDirty = true; nextTargetRetry = 0f; cachedHead = null; }

    private void OnDisable()
    {
        IsShowingNeedsGuide=false;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        CatBreedService.Changed -= InvalidateTargets;
        if(petInteraction!=null)petInteraction.SuccessfulPetGesture-=NotifyPettingStarted;
        onboardingSessionActive=false;
        ReleaseOnboardingInputBlock();
        if(transitionRoutine!=null){StopCoroutine(transitionRoutine);transitionRoutine=null;}
        advancing=false;
        movementInputObserved=false;
    }

    private void OnDestroy()
    {
        IsShowingNeedsGuide=false;
        onboardingSessionActive=false;
        ReleaseOnboardingInputBlock();
        if(dialogue!=null){dialogue.Continued-=HandleContinue;dialogue.NameConfirmed-=HandleNameConfirmed;}
        if(celebrationView!=null)celebrationView.PlayRequested-=CompleteCelebration;
        if(petInteraction!=null)petInteraction.SuccessfulPetGesture-=NotifyPettingStarted;
    }

    private void Update()
    {
        if (!IsOnboardingCompleted && (targetsDirty || boundStep != currentStep ||
            (currentStep < steps.Length && (steps[currentStep].target == null || !steps[currentStep].target.gameObject.activeInHierarchy) && Time.unscaledTime >= nextTargetRetry)))
            ResolveCurrentTargets();
        EvaluateVisibility();
        if (!cardVisible || currentStep >= steps.Length) return;
        AnimateCard();
        if (steps[currentStep].icon == IconKind.SwipeHand) AnimatePaw();
        if (steps[currentStep].completion == CompletionKind.MoveDistance) EvaluateMovementCompletion();
    }

    private void LateUpdate()
    {
        if (cardVisible) UpdateScreenPosition(true);
        if (spotlightCopy != null && spotlightCopy.gameObject.activeSelf && currentStep < steps.Length)
            PositionSpotlightCopyBelowTargets(steps[currentStep]);
    }

    public void NotifyPettingStarted()
    {
        if (IntroductionFinished && CurrentKind == StepKind.PetTheCat && !advancing)
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, 1);
            CompleteCurrentStep();
        }
    }

    public void NotifyGotIt() => HandleContinue();

    [ContextMenu("Reset Onboarding Progress")]
    public void ResetTutorialProgress()
    {
        if(transitionRoutine!=null){StopCoroutine(transitionRoutine);transitionRoutine=null;}
        onboardingSessionActive=false;
        ReleaseOnboardingInputBlock();
        ClearProgressKeys(); currentStep=0; introStep=0; advancing=false;
        movementStart=catTarget!=null?catTarget.position:Vector3.zero; SetAllHidden(); RefreshOnboardingInputBlock();
    }

    public static void ClearProgressKeys()
    {
        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        PlayerPrefs.DeleteKey(IntroductionCompletedKey);
        PlayerPrefs.DeleteKey(IntroductionStepKey);
        PlayerPrefs.DeleteKey(OnboardingStepKey);
        PlayerPrefs.DeleteKey(OnboardingCompletedKey);
        PlayerPrefs.Save();
    }

    public static void ClearCatName()
    {
        CatIdentityService.CatName = string.Empty;
        PlayerPrefs.DeleteKey(IntroductionCompletedKey);
        PlayerPrefs.DeleteKey(IntroductionStepKey);
        PlayerPrefs.Save();
    }

    public void RebuildVisuals(TMP_FontAsset preferredFont, bool immediate)
    {
        if (hintRoot == null) return;
        for(int i=hintRoot.childCount-1;i>=0;i--)
        {
            hintRoot.GetChild(i).gameObject.SetActive(false);
            if(immediate&&!Application.isPlaying) DestroyImmediate(hintRoot.GetChild(i).gameObject); else Destroy(hintRoot.GetChild(i).gameObject);
        }
        TMP_FontAsset font=preferredFont!=null?preferredFont:Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        cardSize=new Vector2(360f,204f);
        hintRoot.sizeDelta=new Vector2(cardSize.x+20f,cardSize.y+64f); hintRoot.localScale=Vector3.one;
        cardRoot=CreateRect(hintRoot,"CardFloatRoot",cardSize,Vector2.zero);
        StorybookScreenStyle.RoomShell(CreatePanel(cardRoot,"IvoryFrame",cardSize,Vector2.zero,PremiumUiStyle.Ivory,24f,2f),24f,true);
        lessonProgress=CreateText(cardRoot,"LessonProgress",font,18f,FontStyles.Bold,TextAlignmentOptions.Center,new Vector2(316f,28f),new Vector2(0f,78f));
        lessonProgress.color=StorybookScreenStyle.Muted;
        lessonProgressStep=-1;
        RectTransform pointer=CreateRect(cardRoot,"TargetPointer",new Vector2(58f,48f),new Vector2(0f,-cardSize.y*.5f-15f));
        pointer.gameObject.AddComponent<CanvasRenderer>(); pointerGraphic=pointer.gameObject.AddComponent<LowPolyTutorialPointerGraphic>(); pointerGraphic.raycastTarget=false;
        RectTransform gesture=CreateRect(cardRoot,"SwipeGesture",new Vector2(250f,72f),new Vector2(0f,18f));
        gesture.gameObject.AddComponent<CanvasRenderer>(); swipeGraphic=gesture.gameObject.AddComponent<LowPolySwipeGraphic>(); swipeGraphic.raycastTarget=false;
        pawIcon=CreateRect(cardRoot,"SwipeHand",new Vector2(78f,78f),new Vector2(0f,20f)); pawIcon.gameObject.AddComponent<CanvasRenderer>();
        var hand=pawIcon.gameObject.AddComponent<LowPolyHandGraphic>(); hand.raycastTarget=false;
        movePortrait=CreateRect(cardRoot,"MoveCatPortrait",new Vector2(76f,76f),new Vector2(-112f,20f)); movePortrait.gameObject.AddComponent<CanvasRenderer>();
        var cat=movePortrait.gameObject.AddComponent<Image>(); cat.raycastTarget=false;
        movePortrait.gameObject.AddComponent<SelectedCatPortrait>().Refresh();
        RectTransform joystick=CreateRect(cardRoot,"JoystickCue",new Vector2(190f,48f),new Vector2(40f,20f)); joystick.gameObject.AddComponent<CanvasRenderer>();
        TMP_Text joy=joystick.gameObject.AddComponent<TextMeshProUGUI>(); joy.font=font; joy.fontSize=34f; joy.alignment=TextAlignmentOptions.Center; joy.text="←  ●  →"; joy.color=StorybookScreenStyle.Muted; joy.raycastTarget=false;
        RectTransform labelRect=CreateRect(cardRoot,"Instruction",new Vector2(316f,58f),new Vector2(0f,-52f)); labelRect.gameObject.AddComponent<CanvasRenderer>();
        instructionLabel=labelRect.gameObject.AddComponent<TextMeshProUGUI>(); instructionLabel.font=font; instructionLabel.fontSize=23f; instructionLabel.fontStyle=FontStyles.Bold;
        instructionLabel.alignment=TextAlignmentOptions.Center; instructionLabel.color=StorybookScreenStyle.Ink; instructionLabel.raycastTarget=false;
        instructionLabel.enableAutoSizing=true; instructionLabel.fontSizeMin=21f; instructionLabel.fontSizeMax=25f;
        Image tapSurface=cardRoot.gameObject.GetComponent<Image>()??cardRoot.gameObject.AddComponent<Image>(); tapSurface.color=Color.clear; tapSurface.raycastTarget=false;
        Button cardButton=cardRoot.gameObject.GetComponent<Button>()??cardRoot.gameObject.AddComponent<Button>(); cardButton.targetGraphic=tapSurface; cardButton.transition=Selectable.Transition.None; cardButton.onClick.RemoveAllListeners(); cardButton.onClick.AddListener(HandleContinue);
        cardRestPosition=cardRoot.anchoredPosition;
        RectTransform skipRect=CreateRect(hintRoot,"SkipTour",new Vector2(240f,60f),new Vector2(0f,-120f));
        skipRect.gameObject.AddComponent<CanvasRenderer>();
        LowPolyPanelGraphic skipFace=skipRect.gameObject.AddComponent<LowPolyPanelGraphic>();
        StorybookScreenStyle.Shell(skipFace,18f);
        skipFace.raycastTarget=true;
        RectTransform skipLabelRect=CreateRect(skipRect,"Label",new Vector2(220f,44f),Vector2.zero);
        skipLabelRect.gameObject.AddComponent<CanvasRenderer>();
        TMP_Text skipLabel=skipLabelRect.gameObject.AddComponent<TextMeshProUGUI>();
        skipLabel.font=font; skipLabel.fontSize=23f; skipLabel.fontStyle=FontStyles.Bold;
        skipLabel.alignment=TextAlignmentOptions.Center; skipLabel.color=StorybookScreenStyle.Ink;
        skipLabel.text=GameContentCopy.Text("Turu atla","Skip tour"); skipLabel.raycastTarget=false; skipLabel.characterSpacing=1.2f;
        Button skipButton=skipRect.gameObject.AddComponent<Button>();
        skipButton.targetGraphic=skipFace; skipButton.transition=Selectable.Transition.None;
        skipButton.onClick.AddListener(SkipRemainingTour);
        StorybookScreenStyle.Action(skipButton,secondary:true);
        visualVersion=CurrentVisualVersion; pawRestPosition=pawIcon.anchoredPosition;
    }

    public void SkipRemainingTour()
    {
        if(IsOnboardingCompleted||string.IsNullOrEmpty(CatName))
            return;
        PlayerPrefs.SetInt(IntroductionCompletedKey,1);
        PlayerPrefs.SetInt(IntroductionStepKey,introduction!=null?introduction.Length:0);
        currentStep=CelebrationStepIndex;
        PlayerPrefs.SetInt(OnboardingStepKey,CelebrationStepIndex);
        PlayerPrefs.Save();
        advancing=false;
        EvaluateVisibility();
    }

    public void RebuildCelebration(TMP_FontAsset preferredFont, bool immediate, bool force=false)
    {
        celebrationView=GetComponentInChildren<OnboardingCelebrationView>(true);
        if(celebrationView==null)
        {
            RectTransform root=EnsureStretchRect(transform,"OnboardingCelebration");
            celebrationView=root.gameObject.AddComponent<OnboardingCelebrationView>();
        }
        if(!Application.isPlaying||celebrationView.transform.childCount==0||force)
            celebrationView.Build(preferredFont,immediate);
        celebrationView.PlayRequested-=CompleteCelebration;
        celebrationView.PlayRequested+=CompleteCelebration;
    }

    public void ConfigureDefaultStepsForScene() { steps=null; EnsureDefaultSteps(); }

    private bool IntroductionFinished => PlayerPrefs.GetInt(IntroductionCompletedKey,0)==1;
    private StepKind CurrentKind => currentStep<steps.Length?steps[currentStep].kind:StepKind.PetTheCat;

    private void EnsureInputInfrastructure()
    {
        Canvas canvas=GetComponent<Canvas>();
        if(canvas!=null){ canvas.overrideSorting=true; canvas.sortingOrder=60; }
        CanvasScaler scaler=GetComponent<CanvasScaler>();
        if(scaler!=null){ scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920f,1080f); scaler.matchWidthOrHeight=.5f; }
        if(GetComponent<GraphicRaycaster>()==null) gameObject.AddComponent<GraphicRaycaster>();
        if(canvasGroup==null) canvasGroup=GetComponent<CanvasGroup>()??gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha=1f; canvasGroup.interactable=true; canvasGroup.blocksRaycasts=true;
    }

    private void EnsureRuntimeViews()
    {
        TMP_FontAsset font=hintRoot!=null?hintRoot.GetComponentInChildren<TMP_Text>(true)?.font:null;
        bool rebuildGeneratedViews=visualVersion!=CurrentVisualVersion||cardRoot==null||instructionLabel==null||movePortrait==null;
        if(rebuildGeneratedViews) RebuildVisuals(font,true);
        RectTransform overlay=CreateStretchRect(transform,"TutorialSpotlight"); overlay.gameObject.AddComponent<CanvasRenderer>();
        spotlight=overlay.gameObject.AddComponent<TutorialSpotlight>(); spotlight.raycastTarget=true;
        spotlightButton=overlay.gameObject.AddComponent<Button>(); spotlightButton.targetGraphic=spotlight; spotlightButton.transition=Selectable.Transition.None; spotlightButton.onClick.AddListener(HandleContinue);
        spotlightCopy=CreateRect(transform,"NeedsCaptionGroup",new Vector2(780f,88f),Vector2.zero);
        
        StorybookScreenStyle.Shell(CreatePanel(spotlightCopy,"CaptionFace",new Vector2(750f,78f),Vector2.zero,PremiumUiStyle.Ivory,24f,2f),24f);
        spotlightTitle=CreateText(spotlightCopy,"Title",font,27f,FontStyles.Bold,TextAlignmentOptions.Center,new Vector2(720f,34f),new Vector2(0f,17f));
        spotlightTitle.color=StorybookScreenStyle.Ink; spotlightTitle.characterSpacing=.8f; spotlightTitle.outlineWidth=0f;
        spotlightSubtitle=CreateText(spotlightCopy,"Subtitle",font,18f,FontStyles.Normal,TextAlignmentOptions.Center,new Vector2(720f,26f),new Vector2(0f,-18f));
        spotlightSubtitle.color=StorybookScreenStyle.Muted; spotlightSubtitle.characterSpacing=.3f;
        dialogue=GetComponentInChildren<CatDialogueView>(true);
        RectTransform dialogueRoot=dialogue!=null?dialogue.transform as RectTransform:EnsureStretchRect(transform,"CatDialogue");
        if(dialogue==null) dialogue=dialogueRoot.gameObject.AddComponent<CatDialogueView>();
        dialogue.Build(font);
        // The room may contain a serialized dialogue before the runtime
        // spotlight. Its controls must render and receive taps above the dim.
        dialogue.transform.SetAsLastSibling();
        dialogue.Continued+=HandleContinue; dialogue.NameConfirmed+=HandleNameConfirmed;
        RebuildCelebration(font,true,rebuildGeneratedViews);
    }

    private void EvaluateVisibility()
    {
        RefreshOnboardingInputBlock();
        bool popupOpen=whileYouWereAwayPopup!=null&&whileYouWereAwayPopup.IsOpen;
        if(popupWasOpen&&!popupOpen) popupReleaseFrame=Time.frameCount+1;
        popupWasOpen=popupOpen;
        if (popupOpen){ EndOnboardingSession(); SetAllHidden(); return; }
        if (TitleScreen.IsShowing){ SetAllHidden(); return; }
        if (advancing) return;
        if(Time.frameCount<=popupReleaseFrame){ BeginOnboardingSession(); SetAllHidden(); return; }
        if(!IntroductionFinished){ BeginOnboardingSession(); ShowIntroduction(); return; }
        if(Time.unscaledTime<onboardingReleaseTime){ SetAllHidden(); return; }
        if(IsOnboardingCompleted||currentStep>=CompleteStepIndex){ EndOnboardingSession(); SetAllHidden(); return; }
        if(advancing&&currentStep==CelebrationStepIndex)return;
        if(currentStep==CelebrationStepIndex)
        {
            BeginOnboardingSession();
            HideCardAndSpotlight(); dialogue.Hide();
            if(celebrationView!=null&&!celebrationView.IsOpen) celebrationView.Show();
            return;
        }
        BeginOnboardingSession();
        ShowCurrentOnboardingStep();
    }

    private void ShowIntroduction()
    {
        HideCardAndSpotlight();
        string name=CatName;
        if(string.IsNullOrEmpty(name))
        {
            if(!dialogue.IsVisible) dialogue.ShowNamePrompt(GameContentCopy.Text("Merhaba! Başlamadan önce… adım ne olsun?", "Hi! Before we begin… what should my name be?"));
            return;
        }
        if(introStep>=introduction.Length){ CompleteIntroduction(); return; }
        if(!dialogue.IsVisible) { dialogue.ShowMessage(name,IntroductionLine(introStep,name)); dialogue.SetLessonProgress(introStep+1,4); }
    }

    private void HandleNameConfirmed(string value)
    {
        if(!string.IsNullOrEmpty(CatName)) return;
        string safe=CatDialogueView.NormalizeName(value); if(string.IsNullOrEmpty(safe)) return;
        // Use the shared setter so already-enabled identity labels refresh now.
        CatIdentityService.CatName=safe;
        introStep=0; PlayerPrefs.SetInt(IntroductionStepKey,0); PlayerPrefs.Save();
        dialogue.ShowMessage(safe,IntroductionLine(0,safe));
        dialogue.SetLessonProgress(1,4);
    }

    private void HandleContinue()
    {
        if(advancing||popupWasOpen) return;
        if(!IntroductionFinished)
        {
            if(string.IsNullOrEmpty(CatName)) return;
            introStep++; PlayerPrefs.SetInt(IntroductionStepKey,introStep); PlayerPrefs.Save();
            if(introStep>=introduction.Length){ CompleteIntroduction(); return; }
            dialogue.ShowMessage(CatName,IntroductionLine(introStep,CatName)); dialogue.SetLessonProgress(introStep+1,4); return;
        }
        if(currentStep<steps.Length&&steps[currentStep].completion==CompletionKind.TapDialogue) CompleteCurrentStep();
    }

    private void CompleteIntroduction()
    {
        PlayerPrefs.SetInt(IntroductionCompletedKey,1); PlayerPrefs.SetInt(IntroductionStepKey,introduction.Length); PlayerPrefs.Save();
        onboardingReleaseTime=Time.unscaledTime+fadeDuration+.08f; dialogue.Hide(); StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay(){ yield return new WaitForSecondsRealtime(fadeDuration+.08f); EvaluateVisibility(); }

    private void ShowCurrentOnboardingStep()
    {
        StepDefinition step=steps[currentStep];
        IsShowingNeedsGuide=step.kind==StepKind.WatchNeeds;
        if(step.kind==StepKind.PetTheCat||step.kind==StepKind.MoveYourCat)
        {
            spotlight.Hide(); spotlightCopy.gameObject.SetActive(false); dialogue.Hide();
            ConfigureCard(step); if(!cardVisible) ShowCard();
        }
        else
        {
            HideCard();
            spotlightTargets[0]=step.target; spotlightTargets[1]=step.secondaryTarget; spotlightTargets[2]=step.tertiaryTarget;
            if(step.kind==StepKind.WatchNeeds)
                spotlight.Show(spotlightTargets,gameplayCamera,NeedsSpotlightPadding);
            else
                spotlight.Show(spotlightTargets,gameplayCamera,step.kind==StepKind.RestInBed?26f:24f);
            if(step.kind==StepKind.WatchNeeds)
            {
                spotlightCopy.gameObject.SetActive(true); PositionSpotlightCopyBelowTargets(step);
                spotlightTitle.text=GameContentCopy.Text("İhtiyaçlarıma göz kulak ol", "KEEP THEM HAPPY"); spotlightSubtitle.text=GameContentCopy.Text("Tokluğumu, suyumu ve enerjimi dengede tut.", "Keep hunger, thirst, and energy balanced.");
                if(!dialogue.IsVisible) { dialogue.ShowMessage(CatName,GameContentCopy.Text("Bu göstergeler tokluğumu, suyumu ve enerjimi gösterir.", "These bars show my hunger, thirst, and energy.")); dialogue.SetLessonProgress(currentStep+1,5); }
            }
            else
            {
                spotlightCopy.gameObject.SetActive(false);
                string message=step.kind==StepKind.FoodAndWater
                    ? GameContentCopy.Text("Mama ve su kaplarımdan yiyip içmeme yardım et.", "Keep me fed and hydrated with my food and water bowls.")
                    : GameContentCopy.Text("Yorulduğumda yatağımda dinlenerek enerjimi toplarım.", "When I’m tired, the bed helps me recover my energy.");
                if(!dialogue.IsVisible) { dialogue.ShowMessage(CatName,message); dialogue.SetLessonProgress(currentStep+1,5); }
            }
        }
    }

    private void ConfigureCard(StepDefinition step)
    {
        // Serialized tutorial text is legacy authoring data. Resolve the
        // visible card by its stable kind in the currently selected language.
        instructionLabel.text = GameLanguageService.Text(step.kind switch
        {
            StepKind.PetTheCat => "tutorial.pet_action",
            StepKind.MoveYourCat => "tutorial.move_action",
            StepKind.WatchNeeds => "tutorial.watch_needs",
            StepKind.FoodAndWater => "tutorial.food_water",
            StepKind.RestInBed => "tutorial.rest_bed",
            _ => "interaction.status_fallback"
        });
        bool move=step.kind==StepKind.MoveYourCat;
        movePortrait.gameObject.SetActive(move);
        Transform joystick=cardRoot.Find("JoystickCue"); if(joystick!=null) joystick.gameObject.SetActive(move);
        pawIcon.gameObject.SetActive(step.icon==IconKind.SwipeHand); swipeGraphic.gameObject.SetActive(step.icon==IconKind.SwipeHand);
        pointerGraphic.gameObject.SetActive(step.usePointer);
        instructionLabel.rectTransform.anchoredPosition=new Vector2(0f,-52f);
        GameLanguage language=GameLanguageService.Current;
        if(lessonProgress!=null&&(lessonProgressStep!=currentStep||lessonProgressLanguage!=language))
        {
            lessonProgressStep=currentStep;lessonProgressLanguage=language;
            lessonProgress.text=GameContentCopy.Text("BİRLİKTE KEŞFEDELİM", "LET'S EXPLORE")+"  ·  "+(currentStep+1)+" / 5";
        }
        bool tapToContinue=step.completion==CompletionKind.TapDialogue;
        Image tapSurface=cardRoot.GetComponent<Image>(); if(tapSurface!=null)tapSurface.raycastTarget=tapToContinue;
        Button cardButton=cardRoot.GetComponent<Button>(); if(cardButton!=null)cardButton.enabled=tapToContinue;
    }

    private void CompleteCurrentStep()
    {
        IsShowingNeedsGuide=false;
        if(advancing||currentStep>=steps.Length) return; advancing=true; bool enteringCelebration=currentStep==steps.Length-1; currentStep++;
        PlayerPrefs.SetInt(OnboardingStepKey,currentStep);
        PlayerPrefs.Save();
        RefreshOnboardingInputBlock();
        if(enteringCelebration&&spotlight!=null) spotlight.HideAnimated();
        if(transitionRoutine!=null) StopCoroutine(transitionRoutine); transitionRoutine=StartCoroutine(AdvanceRoutine());
    }

    private void CompleteCelebration()
    {
        if(currentStep!=CelebrationStepIndex||IsOnboardingCompleted)return;
        try
        {
            var reward=StarterCareRewardService.Grant();
            if(!reward.IsSettled)
            {
                Debug.LogWarning($"Starter care reward could not be settled: {reward}",this);
                return;
            }
            currentStep=CompleteStepIndex;
            PlayerPrefs.SetInt(OnboardingStepKey,CompleteStepIndex);
            PlayerPrefs.SetInt(OnboardingCompletedKey,1);
            PlayerPrefs.Save();
        }
        finally
        {
            advancing=false; SetAllHidden(); EndOnboardingSession();
        }
    }

    private IEnumerator AdvanceRoutine()
    {
        HideCard();
        if(currentStep!=CelebrationStepIndex&&spotlight!=null)spotlight.Hide();
        if(spotlightCopy!=null)spotlightCopy.gameObject.SetActive(false);
        dialogue.Hide(); yield return new WaitForSecondsRealtime(.35f);
        movementStart=catTarget!=null?catTarget.position:Vector3.zero; movementInputObserved=false; advancing=false; transitionRoutine=null; EvaluateVisibility();
    }

    private void EvaluateMovementCompletion()
    {
        if(catTarget==null||advancing)return; Vector3 delta=catTarget.position-movementStart; delta.y=0f;
        CatMovement movement=catTarget.GetComponent<CatMovement>(); movementInputObserved|=movement!=null&&movement.IsMovementInputActive;
        if(delta.magnitude>=movementDistance&&movementInputObserved) CompleteCurrentStep();
    }

    private void ResolveReferences()
    {
        if(petInteraction==null)petInteraction=FindAnyObjectByType<PetInteraction>(); if(catTarget==null&&petInteraction!=null)catTarget=petInteraction.transform;
        if(gameplayCamera==null)gameplayCamera=Camera.main; if(canvasGroup==null)canvasGroup=GetComponent<CanvasGroup>();
        if(whileYouWereAwayPopup==null)whileYouWereAwayPopup=FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);
        if(actionButton==null)actionButton=FindSceneTransform("ActionButton") as RectTransform;
    }

    private void SubscribeToPetInteraction()
    {
        if(petInteraction==null)return;
        petInteraction.SuccessfulPetGesture-=NotifyPettingStarted;
        petInteraction.SuccessfulPetGesture+=NotifyPettingStarted;
    }

    private void EnsureDefaultSteps()
    {
        if(steps==null||steps.Length!=5)
        {
            steps=new[]{
                Step(StepKind.PetTheCat,"SWIPE TO PET",CompletionKind.PetGesture,IconKind.SwipeHand),
                Step(StepKind.MoveYourCat,"MOVE TO CONTINUE",CompletionKind.MoveDistance,IconKind.None),
                Step(StepKind.WatchNeeds,"KEEP THEM HAPPY",CompletionKind.TapDialogue,IconKind.None),
                Step(StepKind.FoodAndWater,"FOOD & WATER",CompletionKind.TapDialogue,IconKind.None),
                Step(StepKind.RestInBed,"REST IN BED",CompletionKind.TapDialogue,IconKind.None)};
        }
        ResolveCurrentTargets();
        steps[0].message="SWIPE TO PET"; steps[0].completion=CompletionKind.PetGesture;
        steps[1].message="MOVE TO CONTINUE"; steps[1].completion=CompletionKind.MoveDistance;
        steps[2].completion=CompletionKind.TapDialogue;
        steps[3].completion=CompletionKind.TapDialogue;
        steps[4].completion=CompletionKind.TapDialogue;
        for(int i=0;i<steps.Length;i++)steps[i].showGotIt=false;
        if(movementDistance<.2f||movementDistance>.3f)movementDistance=.25f;
    }

    private void ResolveCurrentTargets()
    {
        if (steps == null || steps.Length != 5) return;
        targetsDirty=false; boundStep=currentStep; nextTargetRetry=Time.unscaledTime+.5f;
        // Explain the controls where their own layout places them. In particular,
        // never reparent/lift the Storybook HUD just to draw a tutorial around it.
        Transform hud = null;
        foreach (var layout in FindObjectsByType<StorybookHudLayout>(FindObjectsInactive.Exclude))
        {
            if(!IsLiveUi(layout.transform))continue;
            if(hud==null||layout.gameObject.scene.name=="CatHome_UI")hud=layout.transform;
        }
        MobileJoystick joystick=null;
        foreach(var candidate in FindObjectsByType<MobileJoystick>(FindObjectsInactive.Exclude))
        {
            if(!IsLiveUi(candidate.transform))continue;
            if(joystick==null||candidate.gameObject.scene.name=="CatHome_UI")joystick=candidate;
        }
        if(hud==null&&joystick!=null)hud=joystick.GetComponentInParent<Canvas>().transform;
        steps[1].target=joystick!=null?joystick.transform:null;
        steps[2].target=FindDescendant(hud,"ThirstUI");
        steps[2].secondaryTarget=FindDescendant(hud,"EnergyUI");
        steps[2].tertiaryTarget=FindDescendant(hud,"FoodBar")??FindDescendant(hud,"HungerUI");

        if(catTarget==null||!catTarget.gameObject.activeInHierarchy)
        {
            var movement=FindAnyObjectByType<CatMovement>();
            if(movement!=null)catTarget=movement.transform;
        }
        if(catTarget!=null)
        {
            var currentPet=catTarget.GetComponent<PetInteraction>();
            if(currentPet!=petInteraction)
            {if(petInteraction!=null)petInteraction.SuccessfulPetGesture-=NotifyPettingStarted;petInteraction=currentPet;SubscribeToPetInteraction();}
            steps[0].target=ResolveLiveHead();
            var bowls=catTarget.GetComponent<BowlInteraction>();
            steps[3].target=bowls!=null?bowls.FoodBowl:null;
            steps[3].secondaryTarget=bowls!=null?bowls.WaterBowl:null;
            var sleep=catTarget.GetComponent<SleepInteraction>();
            steps[4].target=sleep!=null?sleep.TutorialBedTarget:null;
        }
        steps[4].secondaryTarget=null; steps[4].tertiaryTarget=null;
        if(gameplayCamera==null||!gameplayCamera.isActiveAndEnabled)
        {
            // Additive loading briefly parks the incoming camera. Keep that
            // authored camera until a live replacement actually exists.
            Camera liveCamera=Camera.main;if(liveCamera!=null)gameplayCamera=liveCamera;
        }
    }

    private static bool IsLiveUi(Transform target)
    {var canvas=target.GetComponentInParent<Canvas>();return target.gameObject.scene.IsValid()&&target.gameObject.activeInHierarchy&&canvas!=null&&canvas.isActiveAndEnabled;}

    private Transform ResolveLiveHead()
    {
        if(cachedHeadCat==catTarget&&cachedHead!=null&&cachedHead.gameObject.activeInHierarchy)return cachedHead;
        cachedHeadCat=catTarget;
        // The production breed rigs use DEF-spine.006. Resolve the active
        // model once, then invalidate on model/scene replacement instead of
        // repeatedly searching because a legacy "Head" bone does not exist.
        Transform namedHead=null,neck=null,defHead=null;
        if(catTarget!=null)foreach(var bone in catTarget.GetComponentsInChildren<Transform>(false))
        {
            if(string.Equals(bone.name,"DEF-spine.006",StringComparison.OrdinalIgnoreCase))return cachedHead=bone;
            if(string.Equals(bone.name,"DEF-head",StringComparison.OrdinalIgnoreCase))defHead=bone;
            else if(string.Equals(bone.name,"Head",StringComparison.OrdinalIgnoreCase))namedHead=bone;
            else if(string.Equals(bone.name,"Neck",StringComparison.OrdinalIgnoreCase))neck=bone;
        }
        return cachedHead=defHead??namedHead??neck??catTarget;
    }

    private void RefreshOnboardingInputBlock()
    {
        bool shouldOwn=lifecycleReady&&onboardingSessionActive&&isActiveAndEnabled&&!IsOnboardingCompleted&&currentStep<CompleteStepIndex;
        if(!shouldOwn){ReleaseOnboardingInputBlock();return;}
        if(onboardingMovement==null)
            onboardingMovement=catTarget!=null?catTarget.GetComponent<CatMovement>():FindAnyObjectByType<CatMovement>();
        if(onboardingMovement==null)return;
        CatInputCategory policy=GetInputPolicy();
        if(ownsOnboardingInputBlock&&appliedInputPolicy==policy)return;
        onboardingMovement.SetInputBlock(this,policy); ownsOnboardingInputBlock=true; appliedInputPolicy=policy;
    }

    private CatInputCategory GetInputPolicy()
    {
        if(advancing||!IntroductionFinished||currentStep>=steps.Length)
            return CatInputCategory.All;
        switch(steps[currentStep].kind)
        {
            case StepKind.PetTheCat:
                return CatInputCategory.Movement|CatInputCategory.WorldActions;
            case StepKind.MoveYourCat:
                return CatInputCategory.Petting|CatInputCategory.WorldActions;
            default:
                return CatInputCategory.All;
        }
    }

    private void BeginOnboardingSession()
    {
        onboardingSessionActive=true;
        RefreshOnboardingInputBlock();
    }

    private void EndOnboardingSession()
    {
        onboardingSessionActive=false;
        ReleaseOnboardingInputBlock();
    }

    private void ReleaseOnboardingInputBlock()
    {
        // Owner removal is idempotent. Always ask the movement system to remove
        // this exact owner so a stale local ownership flag cannot leak the block.
        if(onboardingMovement!=null)onboardingMovement.ReleaseInputBlock(this);
        ownsOnboardingInputBlock=false; appliedInputPolicy=CatInputCategory.None;
    }

    private static StepDefinition Step(StepKind kind,string message,CompletionKind completion,IconKind icon)=>new StepDefinition{kind=kind,message=message,completion=completion,icon=icon,usePointer=kind==StepKind.PetTheCat||kind==StepKind.MoveYourCat};
    private static void AssignIfMissing(StepDefinition step,Transform target){if(step.target==null)step.target=target;}
    private void MigrateLegacyProgress()
    {
        if(!PlayerPrefs.HasKey(OnboardingStepKey)&&!PlayerPrefs.HasKey(OnboardingCompletedKey)&&PlayerPrefs.GetInt(PlayerPrefsKey,0)==1) PlayerPrefs.SetInt(OnboardingStepKey,1);
        if(PlayerPrefs.GetInt(OnboardingCompletedKey,0)==1&&!PlayerPrefs.HasKey(OnboardingStepKey)) PlayerPrefs.SetInt(OnboardingStepKey,CompleteStepIndex);
        PlayerPrefs.Save();
    }

    private void ShowCard(){ cardVisible=true; hintRoot.gameObject.SetActive(true); cardRoot.localScale=Vector3.one; cardRestPosition=Vector2.zero; cardRoot.anchoredPosition=cardRestPosition; movementInputObserved=false; UpdateScreenPosition(true); }
    private void HideCard(){ cardVisible=false; if(hintRoot!=null)hintRoot.gameObject.SetActive(false); }
    private void HideCardAndSpotlight(){ IsShowingNeedsGuide=false; HideCard(); if(spotlight!=null)spotlight.Hide(); if(spotlightCopy!=null)spotlightCopy.gameObject.SetActive(false); }
    private void SetAllHidden(){ HideCardAndSpotlight(); if(dialogue!=null&&dialogue.IsVisible)dialogue.SetSuppressed(true); }

    private void AnimatePaw(){float a=Time.unscaledTime/swipePeriod*Mathf.PI*2f,w=Mathf.Sin(a),x=w*horizontalTravel;pawIcon.anchoredPosition=pawRestPosition+Vector2.right*x;pawIcon.localRotation=Quaternion.Euler(0,0,-w*4f);swipeGraphic.SetMotion(x*(68f/horizontalTravel),Mathf.Cos(a));}
    private void AnimateCard(){float a=Time.unscaledTime/floatPeriod*Mathf.PI*2f;cardRoot.anchoredPosition=cardRestPosition+Vector2.up*(Mathf.Sin(a)*floatDistance);}

    private void UpdateScreenPosition(bool immediate=false)
    {
        if(hintRoot==null||currentStep>=steps.Length)return;
        StepDefinition step=steps[currentStep]; if(!TryGetTargetScreenPoint(step,out Vector2 targetScreen))return;
        RectTransform root=hintRoot.parent as RectTransform;if(root==null)return;
        Camera uiCamera=OverlayCamera();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,targetScreen,uiCamera,out Vector2 target);
        Rect safe=SafeLocalRect(root,uiCamera);
        float hw=hintRoot.rect.width*.5f,hh=hintRoot.rect.height*.5f;
        Vector2 desired=target+step.screenOffset+Vector2.up*(targetGapPixels+cardSize.y*.5f+24f);
        if(step.kind==StepKind.MoveYourCat)
            desired=target+new Vector2(cardSize.x*.5f+144f,70f);
        float minX=safe.xMin+safeAreaPadding+hw,maxX=safe.xMax-safeAreaPadding-hw;
        float minY=safe.yMin+safeAreaPadding+bottomUiClearance+hh,maxY=safe.yMax-safeAreaPadding-topUiClearance-hh;
        if(minX>maxX)minX=maxX=safe.center.x;if(minY>maxY)minY=maxY=safe.center.y;
        desired.x=Mathf.Clamp(desired.x,minX,maxX);desired.y=Mathf.Clamp(desired.y,minY,maxY);
        hintRoot.position=root.TransformPoint(desired);
        var skip=hintRoot.Find("SkipTour") as RectTransform;
        if(skip!=null)skip.position=root.TransformPoint(new Vector2(safe.xMax-142f,safe.yMax-180f));
        if(pointerGraphic!=null&&pointerGraphic.gameObject.activeSelf)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cardRoot,targetScreen,uiCamera,out Vector2 local);
            Vector2 direction=local.normalized;
            float edgeDistance=1f/Mathf.Max(Mathf.Max(Mathf.Abs(direction.x)/(cardSize.x*.5f),Mathf.Abs(direction.y)/(cardSize.y*.5f)),.001f);
            float length=Mathf.Clamp(local.magnitude-edgeDistance-10f,28f,76f);
            pointerGraphic.rectTransform.sizeDelta=new Vector2(38f,length);
            pointerGraphic.rectTransform.anchoredPosition=direction*(edgeDistance+length*.5f-2f);
            pointerGraphic.rectTransform.localRotation=Quaternion.Euler(0,0,Vector2.SignedAngle(Vector2.down,direction));
            pointerGraphic.SetAim(0);
        }
    }

    private void PositionSpotlightCopyBelowTargets(StepDefinition step)
    {
        RectTransform root=transform as RectTransform;Camera camera=OverlayCamera();
        Vector2 center=Vector2.zero; float bottom=float.PositiveInfinity; int count=0;
        for(int index=0;index<3;index++)
        {
            Transform target=index==0?step.target:index==1?step.secondaryTarget:step.tertiaryTarget;
            if(target is not RectTransform rect)continue;rect.GetWorldCorners(targetCorners);
            Canvas targetCanvas=rect.GetComponentInParent<Canvas>();Camera targetCamera=targetCanvas!=null&&targetCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?targetCanvas.worldCamera:null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(targetCamera,targetCorners[0]),camera,out Vector2 left);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(targetCamera,targetCorners[2]),camera,out Vector2 right);
            center+=(left+right)*.5f;bottom=Mathf.Min(bottom,left.y);count++;
        }
        if(count==0)return;
        Rect safe=SafeLocalRect(root,camera);float width=Mathf.Min(750f,safe.width-40f);
        spotlightCopy.sizeDelta=new Vector2(width,88f);
        var face=spotlightCopy.Find("CaptionFace") as RectTransform;if(face!=null)face.sizeDelta=new Vector2(width,88f);
        spotlightTitle.rectTransform.sizeDelta=new Vector2(width-32f,34f);
        spotlightSubtitle.rectTransform.sizeDelta=new Vector2(width-32f,26f);
        center/=count;center.y=bottom-30f-spotlightCopy.rect.height*.5f;
        center.x=Mathf.Clamp(center.x,safe.xMin+width*.5f+20f,safe.xMax-width*.5f-20f);
        center.y=Mathf.Clamp(center.y,safe.yMin+64f,safe.yMax-64f);
        spotlightCopy.position=root.TransformPoint(center);
    }
    private Camera OverlayCamera(){Canvas canvas=GetComponent<Canvas>();return canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;}
    private static Rect SafeLocalRect(RectTransform root,Camera camera)
    {RectTransformUtility.ScreenPointToLocalPointInRectangle(root,Screen.safeArea.min,camera,out Vector2 min);RectTransformUtility.ScreenPointToLocalPointInRectangle(root,Screen.safeArea.max,camera,out Vector2 max);return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
    private bool TryGetTargetScreenPoint(StepDefinition step,out Vector2 point){point=Vector2.zero;int count=0;if(step.kind!=StepKind.PetTheCat||step.target!=catTarget)AddTargetPoint(step.target,ref point,ref count);AddTargetPoint(step.secondaryTarget,ref point,ref count);AddTargetPoint(step.tertiaryTarget,ref point,ref count);if(count==0&&CurrentKind==StepKind.PetTheCat&&catTarget!=null){Camera c=gameplayCamera!=null?gameplayCamera:Camera.main;if(c==null)return false;Vector3 s=c.WorldToScreenPoint(catTarget.position+Vector3.up*catFallbackHeight);if(s.z<=0)return false;point=s;return true;}if(count==0)return false;point/=count;return true;}
    private void AddTargetPoint(Transform target,ref Vector2 sum,ref int count){if(target==null||!target.gameObject.activeInHierarchy)return;if(target is RectTransform rt&&target.GetComponentInParent<Canvas>()!=null){rt.GetWorldCorners(targetCorners);Canvas cv=target.GetComponentInParent<Canvas>();Camera cam=cv.renderMode==RenderMode.ScreenSpaceOverlay?null:cv.worldCamera;sum+=RectTransformUtility.WorldToScreenPoint(cam,(targetCorners[0]+targetCorners[2])*.5f);count++;return;}Camera camera=gameplayCamera!=null?gameplayCamera:Camera.main;if(camera==null)return;Vector3 s=camera.WorldToScreenPoint(target.position);if(s.z<=0)return;sum+=(Vector2)s;count++;}
    private static Transform FindDescendant(Transform root,string name){if(root==null)return null;foreach(Transform t in root.GetComponentsInChildren<Transform>(false))if(string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase))return t;return null;}
    private static Transform FindSceneTransform(string name){foreach(Transform t in Resources.FindObjectsOfTypeAll<Transform>())if(t.gameObject.scene.IsValid()&&t.name==name)return t;return null;}
    private static RectTransform CreateRect(Transform parent,string name,Vector2 size,Vector2 position){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;r.localScale=Vector3.one;return r;}
    private static RectTransform CreateStretchRect(Transform parent,string name){var r=CreateRect(parent,name,Vector2.zero,Vector2.zero);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    private static RectTransform EnsureStretchRect(Transform parent,string name){Transform existing=parent.Find(name);RectTransform r=existing as RectTransform;if(r==null)r=CreateRect(parent,name,Vector2.zero,Vector2.zero);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    private static LowPolyPanelGraphic CreatePanel(Transform parent,string name,Vector2 size,Vector2 position,Color color,float cut,float bevel){RectTransform r=CreateRect(parent,name,size,position);r.gameObject.AddComponent<CanvasRenderer>();var p=r.gameObject.AddComponent<LowPolyPanelGraphic>();PremiumUiStyle.ConfigureAccentSurface(p,color,color,cut,2f);p.raycastTarget=false;return p;}
    private static TMP_Text CreateText(Transform parent,string name,TMP_FontAsset font,float size,FontStyles style,TextAlignmentOptions align,Vector2 rectSize,Vector2 pos){RectTransform r=CreateRect(parent,name,rectSize,pos);r.gameObject.AddComponent<CanvasRenderer>();var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.fontStyle=style;t.alignment=align;t.color=PremiumUiStyle.Ink;t.raycastTarget=false;t.richText=false;return t;}
}
