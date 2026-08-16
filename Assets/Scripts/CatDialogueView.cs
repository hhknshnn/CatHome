using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class CatDialogueView : MonoBehaviour
{
    private const float PanelBottomPadding = 32f;
    private const float PanelHeight = 280f;
    private const float PanelSlideDistance = 28f;
    internal static readonly Rect CatFaceUv = new Rect(.30f, .52f, .40f, .40f);

    public event Action Continued;
    public event Action<string> NameConfirmed;

    [SerializeField] private CanvasGroup canvasGroup;
    private RectTransform panel;
    private TMP_Text nameLabel;
    private TMP_Text messageLabel;
    private TMP_Text continueLabel;
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

    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > .001f;

    private void Awake() => EnsureCanvasGroup();

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || Screen.safeArea != lastSafeArea)
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
        Panel(panel,"Shadow",Vector2.zero,PremiumUiStyle.Shadow,34f,7f);
        PremiumUiStyle.SetCenteredShadowStretch(
            panel.Find("Shadow") as RectTransform, 8f);
        LowPolyPanelGraphic face=Panel(panel,"Face",Vector2.zero,PremiumUiStyle.Champagne,34f,5f); face.raycastTarget=true;
        panelButton=panel.gameObject.AddComponent<Button>(); panelButton.targetGraphic=face; panelButton.transition=Selectable.Transition.None; panelButton.onClick.AddListener(HandlePanelClick);
        RectTransform inner=Rect("CreamInner",panel,Vector2.zero,Vector2.one,new Vector2(18f,18f),new Vector2(-18f,-18f));
        Panel(inner,"Surface",Vector2.zero,PremiumUiStyle.Navy,27f,7f);
        RectTransform portrait=Rect("CatPortrait",panel,new Vector2(0f,.5f),new Vector2(0f,.5f),new Vector2(26f,-106f),new Vector2(238f,106f));
        Panel(portrait,"PortraitFrame",Vector2.zero,PremiumUiStyle.Champagne,38f,5f);
        CreateCatPortrait(portrait,new Vector2(12f,12f),new Vector2(-12f,-12f));
        RectTransform speech=Rect("SpeechCard",panel,new Vector2(0f,0f),new Vector2(1f,1f),new Vector2(260f,60f),new Vector2(-34f,-46f));
        Panel(speech,"SpeechRim",Vector2.zero,PremiumUiStyle.Champagne,24f,4f);
        RectTransform speechInner=Rect("SpeechInner",speech,Vector2.zero,Vector2.one,new Vector2(5f,5f),new Vector2(-5f,-5f));
        Panel(speechInner,"Surface",Vector2.zero,PremiumUiStyle.Ivory,20f,3f);
        nameLabel=Text("Name",panel,font,22f,FontStyles.Bold,TextAlignmentOptions.Left,PremiumUiStyle.ChampagneLight,new Vector2(0f,1f),new Vector2(1f,1f),new Vector2(280f,-54f),new Vector2(-360f,-18f));
        nameLabel.enableAutoSizing=true; nameLabel.fontSizeMin=18f; nameLabel.fontSizeMax=24f; nameLabel.overflowMode=TextOverflowModes.Truncate;
        messageLabel=Text("Message",panel,font,28f,FontStyles.Normal,TextAlignmentOptions.MidlineLeft,PremiumUiStyle.Ink,Vector2.zero,Vector2.one,new Vector2(286f,82f),new Vector2(-64f,-78f));
        messageLabel.textWrappingMode=TextWrappingModes.Normal; messageLabel.richText=false; messageLabel.enableAutoSizing=true; messageLabel.fontSizeMin=24f; messageLabel.fontSizeMax=31f; messageLabel.overflowMode=TextOverflowModes.Truncate;
        continueLabel=Text("Continue",panel,font,18f,FontStyles.Bold,TextAlignmentOptions.BottomRight,PremiumUiStyle.Teal,new Vector2(1f,0f),new Vector2(1f,0f),new Vector2(-324f,31f),new Vector2(-76f,61f));
        continueLabel.enableAutoSizing=true; continueLabel.fontSizeMin=14f; continueLabel.fontSizeMax=18f; continueLabel.overflowMode=TextOverflowModes.Truncate;
        continueLabel.text="TAP TO CONTINUE";
        RectTransform arrow=Rect("ContinueArrow",continueLabel.transform,new Vector2(1f,.5f),new Vector2(1f,.5f),new Vector2(5f,-10f),new Vector2(31f,10f));
        arrow.gameObject.AddComponent<CanvasRenderer>();
        var continueArrow=arrow.gameObject.AddComponent<LowPolyTutorialPointerGraphic>(); continueArrow.raycastTarget=false;
        BuildNameInput(font);
        RefreshLayout(true);
        SetVisibleImmediate(false);
    }

    public void ShowMessage(string catName,string message,bool showContinue=true)
    {
        nameMode=false; confirming=false; string safeName=NormalizeName(catName); nameLabel.text=string.IsNullOrEmpty(safeName)?"YOUR CAT":safeName.ToUpperInvariant(); messageLabel.gameObject.SetActive(true); messageLabel.text=message;
        RectTransform mr=messageLabel.rectTransform; mr.anchorMin=Vector2.zero; mr.anchorMax=Vector2.one; mr.offsetMin=new Vector2(286f,82f); mr.offsetMax=new Vector2(-64f,-78f);
        input.gameObject.SetActive(false); confirm.gameObject.SetActive(false); continueLabel.gameObject.SetActive(showContinue); Show();
    }
    public void ShowNamePrompt(string message)
    {
        nameMode=true; confirming=false; nameLabel.text="HELLO!"; messageLabel.gameObject.SetActive(true); messageLabel.text=message;
        RectTransform mr=messageLabel.rectTransform; mr.anchorMin=Vector2.zero; mr.anchorMax=Vector2.one; mr.offsetMin=new Vector2(286f,126f); mr.offsetMax=new Vector2(-64f,-82f);
        input.gameObject.SetActive(true); confirm.gameObject.SetActive(true); input.text=string.Empty; continueLabel.gameObject.SetActive(false); ValidateName(input.text); Show();
    }
    public void Hide()
    {
        if (!gameObject.activeSelf || hiding) return;
        hiding=true;
        if (transition!=null) StopCoroutine(transition); transition=StartCoroutine(FadeTo(0f));
    }
    public void SetSuppressed(bool suppressed)
    {
        EnsureCanvasGroup();
        if (suppressed) { hiding=false; SetVisibleImmediate(false); }
    }

    private void Show()
    {
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
        string trimmed=value.Trim(); var info=new StringInfo(trimmed); int count=Math.Min(14,info.LengthInTextElements);
        return count<=0 ? string.Empty : info.SubstringByTextElements(0,count);
    }
    private void BuildNameInput(TMP_FontAsset font)
    {
        RectTransform field=Rect("NameInput",panel,new Vector2(.27f,.16f),new Vector2(.72f,.48f),Vector2.zero,Vector2.zero);
        var bg=Panel(field,"FieldFace",Vector2.zero,PremiumUiStyle.Ivory,18f,5f); bg.raycastTarget=true;
        input=field.gameObject.AddComponent<TMP_InputField>(); input.targetGraphic=bg; input.richText=false; input.lineType=TMP_InputField.LineType.SingleLine; input.characterLimit=0;
        TMP_Text text=Text("Text",field,font,25f,FontStyles.Normal,TextAlignmentOptions.MidlineLeft,new Color32(91,55,42,255),Vector2.zero,Vector2.one,new Vector2(18f,4f),new Vector2(-18f,-4f)); text.richText=false;
        TMP_Text placeholder=Text("Placeholder",field,font,21f,FontStyles.Normal,TextAlignmentOptions.MidlineLeft,new Color32(139,99,76,150),Vector2.zero,Vector2.one,new Vector2(18f,4f),new Vector2(-18f,-4f)); placeholder.text="ENTER MY NAME…";
        input.textComponent=text; input.placeholder=placeholder; input.onValueChanged.AddListener(ValidateName); input.onSubmit.AddListener(_=>ConfirmName());
        RectTransform button=Rect("Confirm",panel,new Vector2(.74f,.16f),new Vector2(.91f,.48f),Vector2.zero,Vector2.zero);
        var buttonFace=Panel(button,"ButtonFace",Vector2.zero,PremiumUiStyle.Navy,24f,5f); buttonFace.raycastTarget=true;
        confirm=button.gameObject.AddComponent<Button>(); confirm.targetGraphic=buttonFace; confirm.onClick.AddListener(ConfirmName);
        PremiumButtonFx confirmFx=button.gameObject.AddComponent<PremiumButtonFx>();
        confirmFx.Configure(button, buttonFace, confirm, true, false);
        TMP_Text label=Text("Label",button,font,20f,FontStyles.Bold,TextAlignmentOptions.Center,Color.white,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); label.text="CONFIRM";
    }
    private IEnumerator FadeTo(float target)
    {
        EnsureCanvasGroup();
        RefreshLayout(false);
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
            panel.offsetMin = new Vector2(0f, PanelBottomPadding);
            panel.offsetMax = new Vector2(0f, PanelBottomPadding + PanelHeight);
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
    private static RectTransform Rect(string name,Transform parent,Vector2 amin,Vector2 amax,Vector2 omin,Vector2 omax){ var go=new GameObject(name,typeof(RectTransform)); var r=go.GetComponent<RectTransform>(); r.SetParent(parent,false); r.anchorMin=amin;r.anchorMax=amax;r.offsetMin=omin;r.offsetMax=omax;r.localScale=Vector3.one;return r; }
    private static LowPolyPanelGraphic Panel(RectTransform parent,string name,Vector2 offset,Color color,float cut,float bevel){ RectTransform r=Rect(name,parent,Vector2.zero,Vector2.one,offset,offset); r.gameObject.AddComponent<CanvasRenderer>(); var g=r.gameObject.AddComponent<LowPolyPanelGraphic>();g.ConfigureTutorialStyle(color,cut,bevel);g.raycastTarget=false;return g; }
    private static T Graphic<T>(string name,Transform parent,Vector2 amin,Vector2 amax,Vector2 omin,Vector2 omax) where T:Graphic { RectTransform r=Rect(name,parent,amin,amax,omin,omax);r.gameObject.AddComponent<CanvasRenderer>();return r.gameObject.AddComponent<T>(); }
    private static void CreateCatPortrait(Transform parent, Vector2 offsetMin, Vector2 offsetMax)
    {
        RawImage cat = Graphic<RawImage>("Cat",parent,Vector2.zero,Vector2.one,offsetMin,offsetMax);
        cat.texture=FindCatTexture(); cat.uvRect=CatFaceUv; cat.color=Color.white; cat.raycastTarget=false;
    }
    internal static Texture FindCatTexture()
    {
        CatMovement cat=FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        if(cat==null)return null;
        Renderer[] renderers=cat.GetComponentsInChildren<Renderer>(true);
        Texture fallback=null;
        for(int i=0;i<renderers.Length;i++)
        {
            Material[] materials=renderers[i].sharedMaterials;
            for(int j=0;j<materials.Length;j++)
            {
                Texture texture=materials[j]!=null?materials[j].mainTexture:null;
                if(texture==null)continue;
                fallback??=texture;
                if(texture.name.IndexOf("CartoonAnimal_Cat",StringComparison.OrdinalIgnoreCase)>=0)return texture;
            }
        }
        return fallback;
    }
    private static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,float size,FontStyles style,TextAlignmentOptions align,Color color,Vector2 amin,Vector2 amax,Vector2 omin,Vector2 omax){ RectTransform r=Rect(name,parent,amin,amax,omin,omax);r.gameObject.AddComponent<CanvasRenderer>();var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.fontStyle=style;t.alignment=align;t.color=color;t.raycastTarget=false;t.richText=false;return t; }
}
