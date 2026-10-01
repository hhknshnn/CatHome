using System;
using System.Collections;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class CatDialogueView : MonoBehaviour
{
    private static readonly CultureInfo NameCulture = CultureInfo.GetCultureInfo("tr-TR");
    private const float PanelBottomPadding = 32f;
    private const float PanelHeight = 172f;
    private const float NamePanelHeight = 300f;
    // Keep the whole entrance/exit above the 16-unit footer gutter as well.
    private const float PanelSlideDistance = 12f;
    internal static readonly Rect CatFaceUv = new Rect(0f, 0f, 1f, 1f);

    public event Action Continued;
    public event Action<string> NameConfirmed;

    [SerializeField] private CanvasGroup canvasGroup;
    private RectTransform panel;
    private TMP_Text nameLabel;
    private TMP_Text messageLabel;
    private TMP_Text continueLabel;
    private TMP_Text lessonLabel;
    private RectTransform portraitRoot;
    private RectTransform continueFace;
    private TMP_InputField input;
    private Button confirm;
    private Button panelButton;
    private bool nameMode;
    private bool confirming;
    private bool hiding;
    private Coroutine transition;
    private Vector2 visiblePanelPosition;
    private Vector2 hiddenPanelPosition;
    private Rect lastSafeArea;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private bool refreshingLayout;
    private PremiumHomeDockLayout footerLayout;
    private readonly Vector3[] footerCorners=new Vector3[4];
    private float lastFooterClearance=-1f;

    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > .001f;
    private static CatDialogueView visibleInstance;
    public static bool IsAnyVisible => visibleInstance != null && visibleInstance.isActiveAndEnabled && visibleInstance.IsVisible;

    private void Awake() => EnsureCanvasGroup();

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || Screen.safeArea != lastSafeArea ||
            Mathf.Abs(FooterClearance()-lastFooterClearance)>.1f)
            RefreshLayout(false);
    }

    private void EnsureCanvasGroup()
    {
        CanvasGroup attachedGroup = GetComponent<CanvasGroup>();
        if (attachedGroup == null)
            attachedGroup = gameObject.AddComponent<CanvasGroup>();
        if (canvasGroup != attachedGroup)
            canvasGroup = attachedGroup;
    }

    public void Build(TMP_FontAsset font)
    {
        EnsureCanvasGroup();
        if (panel != null) return;
        if (transform.Find("DialoguePanel") != null)
        {
            Rebuild(font, !Application.isPlaying);
            return;
        }
        BuildContent(font);
    }

    public void Rebuild(TMP_FontAsset font, bool immediate)
    {
        Transform existing = transform.Find("DialoguePanel");
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
            if (immediate && !Application.isPlaying)
                DestroyImmediate(existing.gameObject);
            else
                Destroy(existing.gameObject);
        }

        panel = null;
        nameLabel = null;
        messageLabel = null;
        continueLabel = null;
        input = null;
        confirm = null;
        panelButton = null;
        BuildContent(font);
    }

    private void BuildContent(TMP_FontAsset font)
    {
        RectTransform root = transform as RectTransform;
        root.pivot = new Vector2(.5f, .5f);
        panel=Rect("DialoguePanel",transform,new Vector2(.06f,0f),new Vector2(.94f,0f),new Vector2(0f,PanelBottomPadding),new Vector2(0f,PanelBottomPadding+PanelHeight));
        panel.pivot = new Vector2(.5f, 0f);
        LowPolyPanelGraphic face=Panel(panel,"Face",Vector2.zero,PremiumUiStyle.Ivory,28f,2f); face.raycastTarget=true;
        panelButton=panel.gameObject.AddComponent<Button>(); panelButton.targetGraphic=face; panelButton.transition=Selectable.Transition.None; panelButton.onClick.AddListener(HandlePanelClick);
        RectTransform inner=Rect("CreamInner",panel,Vector2.zero,Vector2.one,new Vector2(18f,18f),new Vector2(-18f,-18f));

        RectTransform portrait=Rect("CatPortrait",panel,new Vector2(0f,.5f),new Vector2(0f,.5f),new Vector2(26f,-106f),new Vector2(238f,106f));
        portraitRoot=portrait;
        Panel(portrait,"PortraitFrame",Vector2.zero,ModernUiArt.Inset,28f,2f);
        CreateCatPortrait(portrait,new Vector2(12f,12f),new Vector2(-12f,-12f));
        RectTransform speech=Rect("SpeechCard",panel,new Vector2(0f,0f),new Vector2(1f,1f),new Vector2(260f,60f),new Vector2(-34f,-46f));

        RectTransform speechInner=Rect("SpeechInner",speech,Vector2.zero,Vector2.one,new Vector2(5f,5f),new Vector2(-5f,-5f));

        nameLabel=Text("Name",panel,font,22f,FontStyles.Bold,TextAlignmentOptions.Left,PremiumUiStyle.Teal,new Vector2(0f,1f),new Vector2(1f,1f),new Vector2(280f,-54f),new Vector2(-360f,-18f));
        nameLabel.richText=false; nameLabel.parseCtrlCharacters=false;
        PremiumTypography.Apply(nameLabel,false);
        nameLabel.enableAutoSizing=true; nameLabel.fontSizeMin=18f; nameLabel.fontSizeMax=24f; nameLabel.overflowMode=TextOverflowModes.Truncate;
        messageLabel=Text("Message",panel,font,28f,FontStyles.Normal,TextAlignmentOptions.MidlineLeft,PremiumUiStyle.Ink,Vector2.zero,Vector2.one,new Vector2(286f,82f),new Vector2(-64f,-78f));
        messageLabel.textWrappingMode=TextWrappingModes.Normal; messageLabel.richText=false; messageLabel.enableAutoSizing=true; messageLabel.fontSizeMin=24f; messageLabel.fontSizeMax=31f; messageLabel.overflowMode=TextOverflowModes.Truncate;
        continueLabel=Text("Continue",panel,font,18f,FontStyles.Bold,TextAlignmentOptions.BottomRight,PremiumUiStyle.Teal,new Vector2(1f,0f),new Vector2(1f,0f),new Vector2(-324f,31f),new Vector2(-76f,61f));
        continueLabel.enableAutoSizing=true; continueLabel.fontSizeMin=14f; continueLabel.fontSizeMax=18f; continueLabel.overflowMode=TextOverflowModes.Truncate;
        continueLabel.text=GameContentCopy.Text("Devam","Continue");
        continueLabel.alignment=TextAlignmentOptions.Center;continueLabel.color=Color.white;
        continueFace=Rect("ContinueFace",panel,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-250,-32),new Vector2(-34,32));
        continueFace.gameObject.AddComponent<CanvasRenderer>();
        var continueSurface=continueFace.gameObject.AddComponent<LowPolyPanelGraphic>();
        continueSurface.ConfigureModernStyle(ModernUiArt.AzureTop,ModernUiArt.Azure,16,true,true);continueSurface.raycastTarget=false;
        continueFace.SetSiblingIndex(continueLabel.transform.GetSiblingIndex());
        RectTransform arrow=Rect("ContinueArrow",continueLabel.transform,new Vector2(1f,.5f),new Vector2(1f,.5f),new Vector2(4f,-10f),new Vector2(22f,10f));
        // Navigation uses a quiet white chevron. The tutorial pointer is an
        // orange, down-facing three-layer marker and belongs to spotlights.
        for(int segment=0;segment<2;segment++)
        {
            float y=segment==0?4.25f:-4.25f;
            var stroke=Graphic<Image>("ChevronStroke"+segment,arrow,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-7,y-1.25f),new Vector2(7,y+1.25f));
            stroke.color=Color.white;stroke.raycastTarget=false;stroke.rectTransform.localEulerAngles=new Vector3(0,0,segment==0?-45:45);
        }
        BuildNameInput(font);
        lessonLabel=Text("LessonProgress",panel,PremiumTypography.Body,18f,FontStyles.Normal,TextAlignmentOptions.Right,PremiumUiStyle.Muted,
            new Vector2(1,1),new Vector2(1,1),new Vector2(-390,-52),new Vector2(-40,-22));
        lessonLabel.text=string.Empty;
        StorybookDialoguePresentation.Apply(panel);
        RefreshLayout(true);
        SetVisibleImmediate(false);
    }

    public void ShowMessage(string catName,string message,bool showContinue=true)
    {
        nameMode=false; confirming=false; if(lessonLabel!=null)lessonLabel.text=string.Empty; string safeName=NormalizeName(catName); nameLabel.text=string.IsNullOrEmpty(safeName)?GameContentCopy.Text("Kedin","Your cat"):safeName; messageLabel.gameObject.SetActive(true); messageLabel.text=message;
        RectTransform mr=messageLabel.rectTransform; mr.anchorMin=Vector2.zero; mr.anchorMax=Vector2.one; mr.offsetMin=new Vector2(286f,82f); mr.offsetMax=new Vector2(-64f,-78f);
        input.gameObject.SetActive(false); confirm.gameObject.SetActive(false); continueLabel.gameObject.SetActive(showContinue); if(continueFace!=null)continueFace.gameObject.SetActive(showContinue); Show();
    }
    public void ShowNamePrompt(string message)
    {
        nameMode=true; confirming=false; nameLabel.text=GameContentCopy.Text("Yeni bir dostluk","A new friendship"); if(lessonLabel!=null)lessonLabel.text=GameContentCopy.Text("Önce tanışalım","Let's meet first"); messageLabel.gameObject.SetActive(true); messageLabel.text=message;
        RectTransform mr=messageLabel.rectTransform; mr.anchorMin=Vector2.zero; mr.anchorMax=Vector2.one; mr.offsetMin=new Vector2(286f,126f); mr.offsetMax=new Vector2(-64f,-82f);
        input.gameObject.SetActive(true); confirm.gameObject.SetActive(true); input.text=string.Empty; continueLabel.gameObject.SetActive(false); if(continueFace!=null)continueFace.gameObject.SetActive(false); ValidateName(input.text); Show();
    }
    public void Hide()
    {
        if (!gameObject.activeSelf || hiding) return;
        hiding=true;
        if (transition!=null) StopCoroutine(transition); transition=StartCoroutine(FadeTo(0f));
    }
    public void SetLessonProgress(int current,int total)
    { if(lessonLabel!=null)lessonLabel.text=GameContentCopy.Text("Yuvaya ilk adım","Welcome home")+"  ·  "+current+" / "+total; }
    public void SetSuppressed(bool suppressed)
    {
        EnsureCanvasGroup();
        if (suppressed) { hiding=false; SetVisibleImmediate(false); }
    }

    private void Show()
    {
        visibleInstance=this;
        EnsureCanvasGroup();
        hiding=false; gameObject.SetActive(true); RefreshLayout(true); canvasGroup.interactable=true; canvasGroup.blocksRaycasts=true;
        if (transition!=null) StopCoroutine(transition); transition=StartCoroutine(FadeTo(1f));
    }
    private void HandlePanelClick()
    {
        if (!nameMode) Continued?.Invoke();
    }
    private void ConfirmName()
    {
        if (confirming) return;
        string value=NormalizeName(input.text);
        if (string.IsNullOrEmpty(value)) return;
        confirming=true; confirm.interactable=false; input.DeactivateInputField(); NameConfirmed?.Invoke(value);
    }
    private void ValidateName(string value)
    {
        if (input != null && !string.IsNullOrEmpty(value))
        {
            var info = new StringInfo(value);
            if (info.LengthInTextElements > 14)
            {
                value = info.SubstringByTextElements(0, 14);
                input.SetTextWithoutNotify(value);
                input.caretPosition = value.Length;
            }
        }
        if (confirm!=null) confirm.interactable=!string.IsNullOrEmpty(NormalizeName(value)) && !confirming;
    }
    public static string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        // Compose keyboard combining marks before selecting glyphs or counting
        // letters. Keep the rest of the player's spelling and case unchanged.
        string trimmed=value.Trim().Normalize(NormalizationForm.FormC);
        var info=new StringInfo(trimmed); int count=Math.Min(CatIdentityService.MaxNameLength,info.LengthInTextElements);
        string name=count<=0 ? string.Empty : info.SubstringByTextElements(0,count);
        var elements=StringInfo.GetTextElementEnumerator(name);
        while(elements.MoveNext())
        {
            int index=elements.ElementIndex;
            if(!char.IsLetter(name,index))continue;
            string letter=elements.GetTextElement();
            return name.Substring(0,index)+letter.ToUpper(NameCulture)+name.Substring(index+letter.Length);
        }
        return name;
    }
    private void BuildNameInput(TMP_FontAsset font)
    {
        RectTransform field=Rect("NameInput",panel,new Vector2(.27f,.16f),new Vector2(.72f,.48f),Vector2.zero,Vector2.zero);
        var bg=Panel(field,"FieldFace",Vector2.zero,PremiumUiStyle.Ivory,18f,5f); bg.raycastTarget=true;
        input=field.gameObject.AddComponent<TMP_InputField>(); input.targetGraphic=bg; input.richText=false; input.lineType=TMP_InputField.LineType.SingleLine; input.characterLimit=0;
        TMP_Text text=Text("Text",field,font,25f,FontStyles.Normal,TextAlignmentOptions.MidlineLeft,PremiumUiStyle.Ink,Vector2.zero,Vector2.one,new Vector2(18f,4f),new Vector2(-18f,-4f)); text.richText=false;
        TMP_Text placeholder=Text("Placeholder",field,font,21f,FontStyles.Normal,TextAlignmentOptions.MidlineLeft,PremiumUiStyle.Muted,Vector2.zero,Vector2.one,new Vector2(18f,4f),new Vector2(-18f,-4f)); placeholder.text=GameContentCopy.Text("Bana bir isim ver…","Give me a name…");
        input.textComponent=text; input.placeholder=placeholder; input.onValueChanged.AddListener(ValidateName); input.onSubmit.AddListener(_=>ConfirmName());
        PremiumTypography.ApplyNameInput(input);
        RectTransform button=Rect("Confirm",panel,new Vector2(.74f,.16f),new Vector2(.91f,.48f),Vector2.zero,Vector2.zero);
        var buttonFace=Panel(button,"ButtonFace",Vector2.zero,ModernUiArt.Azure,16f,2f); buttonFace.raycastTarget=true;
        confirm=button.gameObject.AddComponent<Button>(); confirm.targetGraphic=buttonFace; confirm.onClick.AddListener(ConfirmName);
        PremiumButtonFx confirmFx=button.gameObject.AddComponent<PremiumButtonFx>();
        confirmFx.Configure(buttonFace.rectTransform, buttonFace, confirm, true, false);
        TMP_Text label=Text("Label",buttonFace.rectTransform,font,24f,FontStyles.Bold,TextAlignmentOptions.Center,Color.white,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); label.text=GameContentCopy.Text("Tanışalım","Let's meet");
        ModernUiArt.Action(confirm);
    }
    private IEnumerator FadeTo(float target)
    {
        EnsureCanvasGroup();
        RefreshLayout(false);
        if(CatRunnerProgressService.ReducedMotion)
        {canvasGroup.alpha=target;panel.anchoredPosition=target>0?visiblePanelPosition:hiddenPanelPosition;canvasGroup.interactable=canvasGroup.blocksRaycasts=target>0;if(target<=0)gameObject.SetActive(false);hiding=false;transition=null;yield break;}
        float start=canvasGroup.alpha, elapsed=0f; const float duration=.22f;
        Vector2 slideStart=target>0f?hiddenPanelPosition:panel.anchoredPosition;
        Vector2 slideTarget=target>0f?visiblePanelPosition:hiddenPanelPosition;
        while(elapsed<duration){ elapsed+=Time.unscaledDeltaTime; float t=Mathf.Clamp01(elapsed/duration); float eased=t*t*(3f-2f*t); canvasGroup.alpha=Mathf.Lerp(start,target,eased); panel.anchoredPosition=Vector2.Lerp(slideStart,slideTarget,eased); yield return null; }
        canvasGroup.alpha=target; panel.anchoredPosition=slideTarget; canvasGroup.interactable=target>0f; canvasGroup.blocksRaycasts=target>0f; if(target<=0f) gameObject.SetActive(false); hiding=false; transition=null;
    }
    private void SetVisibleImmediate(bool visible){ EnsureCanvasGroup(); canvasGroup.alpha=visible?1f:0f; canvasGroup.interactable=visible; canvasGroup.blocksRaycasts=visible; if(panel!=null)panel.anchoredPosition=visible?visiblePanelPosition:hiddenPanelPosition; gameObject.SetActive(visible); }

    private void RefreshLayout(bool forceCanvasUpdate)
    {
        if (refreshingLayout || panel == null) return;
        refreshingLayout = true;
        try
        {
            RectTransform root = transform as RectTransform;
            RectTransform parent = root != null ? root.parent as RectTransform : null;
            Rect safe = Screen.safeArea;
            if (root != null && parent != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                Vector3[] corners = new Vector3[4];
                parent.GetWorldCorners(corners);
                Vector2 parentMin = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 parentMax = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                Vector2 parentSize = parentMax - parentMin;
                if (parentSize.x > .01f && parentSize.y > .01f)
                {
                    root.anchorMin = new Vector2(
                        Mathf.Clamp01((safe.xMin - parentMin.x) / parentSize.x),
                        Mathf.Clamp01((safe.yMin - parentMin.y) / parentSize.y));
                    root.anchorMax = new Vector2(
                        Mathf.Clamp01((safe.xMax - parentMin.x) / parentSize.x),
                        Mathf.Clamp01((safe.yMax - parentMin.y) / parentSize.y));
                }
                else
                {
                    Vector2 screenSize = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
                    root.anchorMin = new Vector2(safe.xMin / screenSize.x, safe.yMin / screenSize.y);
                    root.anchorMax = new Vector2(safe.xMax / screenSize.x, safe.yMax / screenSize.y);
                }
                root.offsetMin = root.offsetMax = Vector2.zero;
            }

            panel.anchorMin = new Vector2(.06f, 0f);
            panel.anchorMax = new Vector2(.94f, 0f);
            panel.pivot = new Vector2(.5f, 0f);
            float bottom=FooterClearance();
            panel.offsetMin = new Vector2(0f, bottom);
            panel.offsetMax = new Vector2(0f, bottom + (nameMode ? NamePanelHeight : PanelHeight));
            lastFooterClearance=bottom;
            ApplyContentLayout();
            if (forceCanvasUpdate) Canvas.ForceUpdateCanvases();
            visiblePanelPosition = panel.anchoredPosition;
            hiddenPanelPosition = visiblePanelPosition - Vector2.up * PanelSlideDistance;
            if (transition == null && canvasGroup != null && canvasGroup.alpha > .001f)
                panel.anchoredPosition = visiblePanelPosition;
            lastSafeArea = safe;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
        finally { refreshingLayout = false; }
    }
    private float FooterClearance()
    {
        var root=transform as RectTransform;
        if(root==null)return PanelBottomPadding;
        if(footerLayout==null)footerLayout=FindAnyObjectByType<PremiumHomeDockLayout>();
        if(footerLayout==null||!footerLayout.isActiveAndEnabled)return PanelBottomPadding;
        var footer=footerLayout.transform.Find("DockEnamelTray") as RectTransform;
        if(footer==null)footer=footerLayout.transform as RectTransform;
        footer.GetWorldCorners(footerCorners);
        float top=float.NegativeInfinity;
        foreach(var corner in footerCorners)top=Mathf.Max(top,root.InverseTransformPoint(corner).y);
        // Reserve the actual rendered strip, including its safe-area padding
        // and independent canvas/dock scales. The name field shares this rule.
        return Mathf.Max(PanelBottomPadding,top-root.rect.yMin+16f);
    }
    private void ApplyContentLayout()
    {
        if(portraitRoot!=null)
        {
            portraitRoot.anchorMin=portraitRoot.anchorMax=new Vector2(0,.5f);
            portraitRoot.offsetMin=new Vector2(22,nameMode?-105:-64);
            portraitRoot.offsetMax=new Vector2(nameMode?232:150,nameMode?105:64);
        }
        if(nameLabel!=null)
        {
            nameLabel.rectTransform.offsetMin=new Vector2(nameMode?258:182,-52);
            nameLabel.rectTransform.offsetMax=new Vector2(-300,-16);
            nameLabel.color=StorybookQuestPresentation.PositiveText;
        }
        if(messageLabel!=null)
        {
            messageLabel.rectTransform.offsetMin=new Vector2(nameMode?258:182,nameMode?145:32);
            messageLabel.rectTransform.offsetMax=new Vector2(nameMode?-36:-280,nameMode?-86:-55);
            messageLabel.fontSizeMin=nameMode?24:23;messageLabel.fontSizeMax=nameMode?31:28;
        }
        if(continueLabel!=null)
        {
            var rect=continueLabel.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(1,.5f);
            rect.offsetMin=new Vector2(-244,-28);rect.offsetMax=new Vector2(-60,28);
            continueLabel.fontSize=24;continueLabel.fontSizeMin=20;continueLabel.fontSizeMax=24;
        }
        if(input!=null)
        {
            var rect=input.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(1,0);
            rect.offsetMin=new Vector2(258,32);rect.offsetMax=new Vector2(-258,114);
        }
        if(confirm!=null)
        {
            var rect=confirm.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(1,0);
            rect.offsetMin=new Vector2(-236,32);rect.offsetMax=new Vector2(-32,114);
        }
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 amin,Vector2 amax,Vector2 omin,Vector2 omax){ var go=new GameObject(name,typeof(RectTransform)); var r=go.GetComponent<RectTransform>(); r.SetParent(parent,false); r.anchorMin=amin;r.anchorMax=amax;r.offsetMin=omin;r.offsetMax=omax;r.localScale=Vector3.one;return r; }
    private static LowPolyPanelGraphic Panel(RectTransform parent,string name,Vector2 offset,Color color,float cut,float bevel){ RectTransform r=Rect(name,parent,Vector2.zero,Vector2.one,offset,offset); r.gameObject.AddComponent<CanvasRenderer>(); var g=r.gameObject.AddComponent<LowPolyPanelGraphic>();PremiumUiStyle.ConfigureAccentSurface(g,color,color,cut,2f);g.raycastTarget=false;return g; }
    private static T Graphic<T>(string name,Transform parent,Vector2 amin,Vector2 amax,Vector2 omin,Vector2 omax) where T:Graphic { RectTransform r=Rect(name,parent,amin,amax,omin,omax);r.gameObject.AddComponent<CanvasRenderer>();return r.gameObject.AddComponent<T>(); }
    private static void CreateCatPortrait(Transform parent, Vector2 offsetMin, Vector2 offsetMax)
    {
        Image cat = Graphic<Image>("Cat", parent, Vector2.zero, Vector2.one, offsetMin, offsetMax);
        cat.raycastTarget = false;
        cat.gameObject.AddComponent<SelectedCatPortrait>().Refresh();
    }
    internal static Texture FindCatTexture()
    {
        var catalog = CatBreedCatalog.Load();
        if (catalog == null || catalog.Count == 0) return null;
        var entry = catalog.Find(CatBreedService.SelectedBreedId) ?? catalog.Get(0);
        return entry.Portrait != null ? entry.Portrait.texture : null;
    }
    private static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,float size,FontStyles style,TextAlignmentOptions align,Color color,Vector2 amin,Vector2 amax,Vector2 omin,Vector2 omax){ RectTransform r=Rect(name,parent,amin,amax,omin,omax);r.gameObject.AddComponent<CanvasRenderer>();var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.fontStyle=style;t.alignment=align;t.color=color;t.raycastTarget=false;t.richText=false;return t; }
}
