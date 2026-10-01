using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OnboardingCelebrationView : MonoBehaviour
{
    private const int ConfettiCount = 24;
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform safeRoot, panel, burst, catPortrait, catVisual;
    [SerializeField] private CatCelebrationPreview catPreview;
    [SerializeField] private Button playButton;
    [SerializeField] private RectTransform[] confetti;
    [SerializeField] private Vector2[] directions;
    [SerializeField] private float[] rotations;
    [SerializeField] private CatMovement catMovement;
    private Coroutine animationRoutine;
    private PremiumModalBackdrop backdrop;
    private bool isOpen, closing;
    private Rect lastSafeArea;
    private float layoutScale=1f;

    public static bool IsAnyOpen { get; private set; }
    public bool IsOpen => isOpen;
    public event Action PlayRequested;

    private void Awake()
    {
        catMovement=FindAnyObjectByType<CatMovement>();
        if(rootGroup==null)Build(FindFont(),true);
        ConfigureInteractionLayer();
        SetHiddenImmediate();
    }

    private void OnDisable(){if(animationRoutine!=null){StopCoroutine(animationRoutine);animationRoutine=null;}catPreview?.Cleanup();isOpen=false;closing=false;IsAnyOpen=false;ReleaseInput();SetHiddenImmediate();}
    private void OnDestroy(){catPreview?.Cleanup();if(playButton!=null)playButton.onClick.RemoveListener(HandlePlay);isOpen=false;IsAnyOpen=false;ReleaseInput();}
    private void LateUpdate(){if(isOpen&&Screen.safeArea!=lastSafeArea)ApplySafeArea();}

    public void Build(TMP_FontAsset font,bool immediate)
    {
        RectTransform root=transform as RectTransform;
        root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        for(int i=root.childCount-1;i>=0;i--){GameObject child=root.GetChild(i).gameObject;child.SetActive(false);if(immediate&&!Application.isPlaying)DestroyImmediate(child);else Destroy(child);}
        rootGroup=GetComponent<CanvasGroup>();if(rootGroup==null)rootGroup=gameObject.AddComponent<CanvasGroup>();
        Image dim=CreateRect(root,"ModalDim",Vector2.zero,Vector2.zero).gameObject.AddComponent<Image>();
        dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.offsetMin=dim.rectTransform.offsetMax=Vector2.zero;dim.color=new Color(0.03f,.06f,.09f,.76f);dim.raycastTarget=true;
        safeRoot=CreateRect(root,"SafeArea",Vector2.zero,Vector2.zero);safeRoot.anchorMin=Vector2.zero;safeRoot.anchorMax=Vector2.one;safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
        panel=CreateRect(safeRoot,"CelebrationPanel",new Vector2(1060f,620f),Vector2.zero);
        CreatePanel(panel,"IvoryFace",new Vector2(1060,620),Vector2.zero,PremiumUiStyle.Ivory,32,2);
        PremiumMomentArt.Stage(panel,-326,0,338,552);
        PremiumMomentArt.Caption(panel,"İlk günümüz,\nilk güzel anımız.","Our first day,\nour first happy moment.",-326,-174,286,94,26);
        PremiumMomentArt.Caption(panel,"ARTIK BİRBİRİMİZİ TANIYORUZ","NOW WE KNOW EACH OTHER",176,240,574,40,19);
        PremiumMomentArt.RewardTray(panel,176,-42,574,96);
        PremiumMomentArt.Caption(panel,"Kedi komutları düğmesinden komutları ve Oyun rehberi sekmesini açabilirsin.","Open Cat commands for commands and the Play guide tab.",176,-134,574,62,22);
        burst=CreateRect(panel,"SunBurst",new Vector2(360f,270f),new Vector2(-326f,62f));burst.gameObject.AddComponent<CanvasRenderer>();
        burst.gameObject.AddComponent<OnboardingCelebrationGraphic>().Configure(OnboardingCelebrationGraphic.ShapeKind.Burst,new Color32(255,215,147,55));
        catPortrait=CreateRect(panel,"HappyCat",new Vector2(268f,244f),new Vector2(-326f,62f));
        catVisual=CreateRect(catPortrait,"CatPreview",new Vector2(286f,266f),Vector2.zero);catVisual.gameObject.AddComponent<CanvasRenderer>();
        RawImage previewImage=catVisual.gameObject.AddComponent<RawImage>();previewImage.color=Color.white;previewImage.raycastTarget=false;catPreview=catVisual.gameObject.AddComponent<CatCelebrationPreview>();
        TMP_Text title=CreateText(panel,"Title",font,GameLanguageService.Text("celebration.care"),44f,FontStyles.Bold,new Vector2(574f,128f),new Vector2(176f,136f),PremiumUiStyle.Ink);
        TMP_Text subtitle=CreateText(panel,"Subtitle",font,GameLanguageService.Text("celebration.care_reward"),26f,FontStyles.Bold,new Vector2(454f,78f),new Vector2(218f,-42f),PremiumUiStyle.Teal);
        RectTransform buttonRoot=CreateRect(panel,"LetsPlayButton",new Vector2(574f,80f),new Vector2(176f,-232f));
        LowPolyPanelGraphic face=CreatePanel(buttonRoot,"ButtonFace",new Vector2(574f,80f),Vector2.zero,PremiumUiStyle.Coral,38f,2f);
        CreateText(face.transform,"Label",font,GameLanguageService.Text("celebration.play"),30f,FontStyles.Bold,new Vector2(350f,70f),new Vector2(0f,5f),Color.white);
        face.raycastTarget=true;playButton=buttonRoot.gameObject.AddComponent<Button>();playButton.targetGraphic=face;playButton.transition=Selectable.Transition.None;playButton.onClick.AddListener(HandlePlay);
        PremiumButtonFx playFx=buttonRoot.gameObject.AddComponent<PremiumButtonFx>();
        playFx.Configure(face.rectTransform,face,playButton,true);
        ModernUiArt.Action(playButton);
        confetti=new RectTransform[ConfettiCount];directions=new Vector2[ConfettiCount];rotations=new float[ConfettiCount];
        for(int i=0;i<ConfettiCount;i++)
        {
            float angle=(i+.35f*(i%3))*Mathf.PI*2f/ConfettiCount;float distance=170f+(i%5)*25f;
            directions[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*distance;rotations[i]=(i%2==0?1f:-1f)*(150f+(i%7)*35f);
            RectTransform piece=CreateRect(panel,"Confetti_"+i,new Vector2(20f+(i%3)*5f,20f+(i%2)*7f),new Vector2(-326f,62f));piece.gameObject.AddComponent<CanvasRenderer>();
            var graphic=piece.gameObject.AddComponent<OnboardingCelebrationGraphic>();
            var kind=i%5==0?OnboardingCelebrationGraphic.ShapeKind.Heart:i%3==0?OnboardingCelebrationGraphic.ShapeKind.Star:OnboardingCelebrationGraphic.ShapeKind.Diamond;
            graphic.Configure(kind,i%2==0?new Color32(255,174,42,255):new Color32(239,91,55,255));piece.SetSiblingIndex(4+i);confetti[i]=piece;
        }
        catPortrait.SetAsLastSibling();title.rectTransform.SetAsLastSibling();subtitle.rectTransform.SetAsLastSibling();buttonRoot.SetAsLastSibling();
        ConfigureInteractionLayer();ApplySafeArea();SetHiddenImmediate();
    }

    public void Show()
    {
        if(isOpen||closing)return;isOpen=true;closing=false;IsAnyOpen=true;gameObject.SetActive(true);transform.SetAsLastSibling();ApplySafeArea();ConfigureInteractionLayer();ResetVisualState();
        rootGroup.alpha=1f;rootGroup.interactable=true;rootGroup.blocksRaycasts=true;gameObject.SetActive(true);
        if(backdrop!=null)backdrop.enabled=true;
        if(catMovement==null)catMovement=FindAnyObjectByType<CatMovement>();if(catMovement!=null)catMovement.AcquireInputBlock(this);
        catPreview?.Begin(catMovement);
        if(CatRunnerProgressService.ReducedMotion)
        {
            panel.localScale=Vector3.one*layoutScale;
            catPortrait.anchoredPosition=new Vector2(-326f,62f);
            catPortrait.localScale=Vector3.one;
        }
        else StartAnimation(OpenRoutine());
    }

    private void HandlePlay(){if(!isOpen||closing)return;
        if(CatRunnerProgressService.ReducedMotion){isOpen=false;IsAnyOpen=false;catPreview?.Cleanup();ReleaseInput();SetHiddenImmediate();PlayRequested?.Invoke();return;}
        closing=true;playButton.interactable=false;StartAnimation(CloseRoutine());}
    private IEnumerator OpenRoutine()
    {
        panel.localScale=Vector3.one*(layoutScale*.96f);catPortrait.anchoredPosition=new Vector2(-326f,42f);catPortrait.localScale=Vector3.one*.96f;
        SetAlpha(catVisual,1f);if(burst!=null){SetAlpha(burst,1f);burst.localScale=Vector3.one;burst.localRotation=Quaternion.identity;}
        for(int i=0;i<confetti.Length;i++){confetti[i].anchoredPosition=new Vector2(-326f,62f);confetti[i].localScale=Vector3.zero;SetAlpha(confetti[i],1f);}
        float elapsed=0f,duration=1.05f;
        while(elapsed<duration&&!CatRunnerProgressService.ReducedMotion){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration);
            float reveal=EaseOut(Mathf.Clamp01(elapsed/.28f));panel.localScale=Vector3.one*(layoutScale*Mathf.Lerp(.96f,1f,reveal));
            catPortrait.anchoredPosition=new Vector2(-326f,Mathf.Lerp(42f,62f,reveal));
            catPortrait.localScale=Vector3.one*Mathf.Lerp(.96f,1f,reveal);
            float burstPop=Mathf.Clamp01(t/.18f);if(burst!=null){burst.localScale=Vector3.one*Mathf.Lerp(.72f,1f,EaseOut(burstPop));burst.localRotation=Quaternion.Euler(0f,0f,t*10f);}
            for(int i=0;i<confetti.Length;i++){float p=Mathf.Clamp01((t-.08f)/(i%4*.025f+.62f));confetti[i].anchoredPosition=new Vector2(-326f,62f)+directions[i]*EaseOut(p);confetti[i].localRotation=Quaternion.Euler(0f,0f,rotations[i]*p);confetti[i].localScale=Vector3.one*Mathf.Sin(p*Mathf.PI)*1.15f;SetAlpha(confetti[i],1f-Mathf.Clamp01((p-.65f)/.35f));}
            yield return null;}
        // One short reveal, then a still reading state. The modal remains
        // open for its real action; no repeated confetti or portrait squash.
        ResetVisualState();
        animationRoutine=null;
    }
    private IEnumerator CloseRoutine()
    {
        float press=0f;while(press<.12f&&!CatRunnerProgressService.ReducedMotion){press+=Time.unscaledDeltaTime;playButton.targetGraphic.rectTransform.localScale=Vector3.one*Mathf.Lerp(1f,.92f,Smooth(press/.12f));yield return null;}
        float elapsed=0f;while(elapsed<.24f&&!CatRunnerProgressService.ReducedMotion){elapsed+=Time.unscaledDeltaTime;float t=Smooth(elapsed/.24f);rootGroup.alpha=1f-t;panel.localScale=Vector3.one*(layoutScale*Mathf.Lerp(.98f,.86f,t));yield return null;}
        isOpen=false;closing=false;IsAnyOpen=false;catPreview?.Cleanup();ReleaseInput();SetHiddenImmediate();animationRoutine=null;PlayRequested?.Invoke();
    }
    private void StartAnimation(IEnumerator routine){if(animationRoutine!=null)StopCoroutine(animationRoutine);animationRoutine=StartCoroutine(routine);}
    private void SetHiddenImmediate(){if(rootGroup!=null){rootGroup.alpha=0f;rootGroup.interactable=false;rootGroup.blocksRaycasts=false;}if(backdrop!=null)backdrop.enabled=false;ResetVisualState();}
    private void ResetVisualState()
    {
        if(panel!=null)panel.localScale=Vector3.one*layoutScale;
        if(catPortrait!=null){catPortrait.anchoredPosition=new Vector2(-326f,62f);catPortrait.localScale=Vector3.one;}if(catVisual!=null)SetAlpha(catVisual,1f);
        if(burst!=null){burst.localScale=Vector3.one;burst.localRotation=Quaternion.identity;SetAlpha(burst,1f);}
        if(confetti!=null)for(int i=0;i<confetti.Length;i++)if(confetti[i]!=null){confetti[i].anchoredPosition=new Vector2(-326f,62f);confetti[i].localScale=Vector3.zero;confetti[i].localRotation=Quaternion.identity;SetAlpha(confetti[i],0f);}
        if(playButton!=null){playButton.interactable=true;playButton.targetGraphic.rectTransform.localScale=Vector3.one;}
    }
    private void ConfigureInteractionLayer()
    {
        if(panel!=null)
        {
            Graphic[] graphics=panel.GetComponentsInChildren<Graphic>(true);
            for(int i=0;i<graphics.Length;i++)graphics[i].raycastTarget=false;
        }
        Transform dim=transform.Find("ModalDim");Image dimImage=dim!=null?dim.GetComponent<Image>():null;if(dimImage!=null)dimImage.raycastTarget=true;
        if(dim!=null)
        {
            backdrop=dim.GetComponent<PremiumModalBackdrop>();
            if(backdrop==null)backdrop=dim.gameObject.AddComponent<PremiumModalBackdrop>();
        }
        if(playButton==null)return;
        playButton.onClick.RemoveListener(HandlePlay);playButton.onClick.AddListener(HandlePlay);playButton.enabled=true;playButton.interactable=true;
        Graphic target=playButton.targetGraphic;if(target==null)target=playButton.transform.Find("ButtonFace")?.GetComponent<Graphic>();
        if(target!=null){target.raycastTarget=true;playButton.targetGraphic=target;}
        // Existing room-authored celebrations bypass Build at runtime.
        // Resolve their action role whenever the view is prepared as well.
        playButton.transform.SetAsLastSibling();
        StorybookMilestonePresentation.Apply(panel);
    }
    private void ReleaseInput(){if(catMovement!=null)catMovement.ReleaseInputBlock(this);if(!isOpen)IsAnyOpen=false;}
    private void ApplySafeArea(){lastSafeArea=Screen.safeArea;if(safeRoot==null||Screen.width<=0||Screen.height<=0)return;safeRoot.anchorMin=new Vector2(lastSafeArea.xMin/Screen.width,lastSafeArea.yMin/Screen.height);safeRoot.anchorMax=new Vector2(lastSafeArea.xMax/Screen.width,lastSafeArea.yMax/Screen.height);safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;Canvas.ForceUpdateCanvases();float width=safeRoot.rect.width-48;float height=safeRoot.rect.height-48;layoutScale=Mathf.Min(1,width/1060f,height/620f);panel.sizeDelta=new Vector2(1060f,620f);panel.localScale=Vector3.one*layoutScale;}
    private TMP_FontAsset FindFont(){return GetComponentInParent<Canvas>()?.GetComponentInChildren<TMP_Text>(true)?.font??Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");}
    private static void SetAlpha(RectTransform rect,float alpha){if(rect==null)return;Graphic g=rect.GetComponent<Graphic>();if(g!=null){Color c=g.color;c.a=alpha;g.color=c;}}
    private static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*(3f-2f*t);}
    private static float EaseOut(float t){t=Mathf.Clamp01(t);return 1f-(1f-t)*(1f-t)*(1f-t);}
    private static RectTransform CreateRect(Transform parent,string name,Vector2 size,Vector2 pos){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;r.localScale=Vector3.one;return r;}
    private static LowPolyPanelGraphic CreatePanel(Transform parent,string name,Vector2 size,Vector2 pos,Color color,float cut,float bevel){RectTransform r=CreateRect(parent,name,size,pos);r.gameObject.AddComponent<CanvasRenderer>();var g=r.gameObject.AddComponent<LowPolyPanelGraphic>();g.ConfigureTutorialStyle(color,cut,bevel);g.raycastTarget=false;return g;}
    private static TMP_Text CreateText(Transform parent,string name,TMP_FontAsset font,string value,float size,FontStyles style,Vector2 rect,Vector2 pos,Color color){RectTransform r=CreateRect(parent,name,rect,pos);r.gameObject.AddComponent<CanvasRenderer>();var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.fontStyle=style;t.alignment=TextAlignmentOptions.Center;t.color=color;t.raycastTarget=false;t.richText=false;PremiumTypography.Apply(t);return t;}
}
