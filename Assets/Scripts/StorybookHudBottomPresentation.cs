using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation only for the runtime companion shortcut and the reserved HUD strip.</summary>
[ExecuteAlways, DefaultExecutionOrder(10000), DisallowMultipleComponent]
public sealed class StorybookHudBottomPresentation : MonoBehaviour
{
    [SerializeField] private Texture2D catIcon;
    [SerializeField] private Image viewportStrip;
    private RectTransform homeDock, companionShortcut, joystick;
    private Canvas homeCanvas;
    public static readonly Color Cream = new Color32(255,246,227,255);
    public static readonly Color IndigoTop = new Color32(66,96,156,255);
    public static readonly Color IndigoBottom = new Color32(31,51,99,255);
    public static readonly Color StripColor = new Color32(24,40,73,255);

    private void OnEnable()
    {
        RefreshJoystickVisuals();
        RefreshPremiumArtwork();
        if(!Application.isPlaying)return;
        ApplyHudFinish();
        StartCoroutine(PrepareCompanion());
    }

    public void RefreshPremiumArtwork()
    {
        if(viewportStrip!=null && viewportStrip.GetComponent<PremiumHudNavySurface>()==null)
            viewportStrip.gameObject.AddComponent<PremiumHudNavySurface>();
        foreach(var button in GetComponentsInChildren<Button>(true))
            ApplyButtonArtwork(button);
        foreach(var surface in GetComponentsInChildren<LowPolyPanelGraphic>(true))
            if(surface.name=="StorybookDockFrame")
                surface.enabled=false; // Hide only the shared frame; each button retains its own gold artwork.
            else if(surface.name=="DockEnamelTray" && surface.GetComponent<PremiumHudNavySurface>()==null)
                surface.gameObject.AddComponent<PremiumHudNavySurface>();
            else if(surface.name=="ContextFace" && surface.transform.parent.name=="ActivityProgressBadge")
                surface.ConfigureHudArtwork(Resources.Load<Sprite>("PremiumHudFinal/nav-normal"));
    }

    private static void ApplyButtonArtwork(Button button)
    {
        string key;
        switch(button.name)
        {
            case "ActionButton":
            case "ActivityActionButton":
                var actionFace=button.targetGraphic as LowPolyPanelGraphic;
                if(actionFace!=null)
                    actionFace.ConfigureHudArtwork(Resources.Load<Sprite>("PremiumHudFinal/action-coral"));
                return;
            case "ShopButton": key="shop"; break;
            case "CurrentRoomStatus": key="room"; break;
            case "PlayCatRunnerButton": key="games"; break;
            case "CompanionShortcut": key="cat"; break;
            default: return;
        }
        var surface=button.targetGraphic as LowPolyPanelGraphic;
        if(surface==null)return;
        surface.ConfigureHudArtwork(Resources.Load<Sprite>("PremiumHudFinal/"+
            (key=="room"?"nav-selected":"nav-normal")));
        var icon=surface.transform.Find(key=="cat"?"StorybookCatHead":"StorybookDockIcon");
        var image=icon!=null?icon.GetComponent<RawImage>():null;
        var texture=Resources.Load<Texture2D>("PremiumHudFinal/"+key);
        if(image!=null && texture!=null)
        {
            image.texture=texture;
            image.uvRect=new Rect(0,0,1,1);
        }
    }

    public void RefreshJoystickVisuals()
    {
        var control=GetComponentInChildren<MobileJoystick>(true);
        if(control==null)return;
        // Leave the input rect, handle travel and sensitivity entirely unchanged.
        foreach(var rect in control.GetComponentsInChildren<RectTransform>(true))
        {
            if(rect.name=="PremiumBase" || rect.name=="PremiumHandle")
                rect.localScale=new Vector3(.88f,.88f,1f);
            var surface=rect.GetComponent<LowPolyPanelGraphic>();
            if(surface!=null && (rect.name=="PremiumBase"||rect.name=="PremiumHandle"||rect.name=="StorybookThumbCap"))
                surface.ConfigureHudJoystickFinish(rect.name=="PremiumBase"?1:rect.name=="PremiumHandle"?2:3);
            var direction=rect.GetComponent<TMP_Text>();
            if(direction!=null && rect.name.StartsWith("Direction"))direction.color=new Color32(94,221,231,255);
        }
    }

    private void ApplyHudFinish()
    {
        homeCanvas=GetComponent<Canvas>();
        // Only the existing home controls opt in; modal surfaces and the opaque
        // viewport backing retain their own presentation and visibility owners.
        foreach(var root in GetComponentsInChildren<RectTransform>(true))
        {
            if(root.name=="HomeDock")
            {
                homeDock=root;
                foreach(var button in root.GetComponentsInChildren<Button>(true))
                {
                    var face=button.targetGraphic as LowPolyPanelGraphic;
                    if(face!=null)face.ConfigureHudPanelFinish();
                    AlignDockContent(button);
                }
                foreach(var surface in root.GetComponentsInChildren<LowPolyPanelGraphic>(true))
                    if(surface.name=="StorybookDockFrame")surface.ConfigureHudPanelFinish(backdrop:true);
            }
            else if(root.name=="JoystickBackground" && root.GetComponent<MobileJoystick>()!=null)
            {
                joystick=root;
                foreach(var surface in root.GetComponentsInChildren<LowPolyPanelGraphic>(true))
                    if(surface.name=="PremiumBase")
                        surface.ConfigureHudPanelFinish(backdrop:true);
                    else if(surface.name=="PremiumHandle" || surface.name=="StorybookThumbCap")
                        surface.ConfigureHudPanelFinish();
            }
            else if(root.name=="ActionButton" || root.name=="ActivityActionButton" || root.name=="ActivityProgressBadge")
            {
                foreach(var surface in root.GetComponentsInChildren<LowPolyPanelGraphic>(true))
                    if(surface.name=="ContextFace")surface.ConfigureHudPanelFinish();
            }
        }
    }

    private static void AlignDockContent(Button button)
    {
        if(button.targetGraphic==null)return;
        var icon=button.targetGraphic.transform.Find("StorybookDockIcon") as RectTransform;
        if(icon==null)return;
        icon.anchoredPosition=new Vector2(-87,0);
        float opticalSize=button.name=="ShopButton"||button.name=="CurrentRoomStatus"?58f:52f;
        icon.sizeDelta=new Vector2(opticalSize,opticalSize);
        foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            // The activity owner keeps the energy label hidden and controls copy.
            if(label.name=="RunnerEnergyLabel")continue;
            var rect=label.rectTransform;
            rect.anchoredPosition=new Vector2(29,rect.anchoredPosition.y);
            rect.sizeDelta=new Vector2(166,rect.sizeDelta.y);
        }
    }

    private IEnumerator PrepareCompanion()
    {
        // The companion canvas bootstraps independently of the authored HUD.
        // This startup-only lookup never takes ownership of its visibility or actions.
        for(int attempt=0;attempt<40;attempt++)
        {
            var panel=FindAnyObjectByType<CatCompanionPanel>(FindObjectsInactive.Include);
            var shortcut=panel!=null?panel.transform.Find("SafeArea/CompanionShortcut"):null;
            if(shortcut!=null)
            {
                companionShortcut=shortcut as RectTransform;
                panel.BindHomeDock(homeDock);
                StyleCompanion(shortcut.GetComponent<Button>(),catIcon);
                yield break;
            }
            yield return new WaitForSecondsRealtime(.25f);
        }
    }

    private void LateUpdate()
    {
        // ModernHomeStrip still owns safe-area sizing and mini-game visibility.
        // Its opaque backing must stay present even while the navigation fades.
        if(viewportStrip!=null && viewportStrip.color!=StripColor) viewportStrip.color=StripColor;
        PremiumHomeDockLayout.AlignShortcut(homeDock,companionShortcut);
        if(joystick!=null && homeCanvas!=null)
        {
            float scale=Mathf.Max(.01f,homeCanvas.scaleFactor);
            // Allow the entire dragged thumb (including its shadow) above the
            // 80-unit strip. Input radius and sensitivity remain unchanged.
            joystick.anchoredPosition=new Vector2(190f+Screen.safeArea.xMin/scale,
                220f+Screen.safeArea.yMin/scale);
        }
    }

    public void Configure(Texture2D icon,Image strip)
    {
        catIcon=icon;viewportStrip=strip;
        if(viewportStrip!=null)viewportStrip.color=StripColor;
        RefreshPremiumArtwork();
    }

    public static void StyleButton(Button button,bool coral)
    {
        if(button==null)return;
        var surface=button.targetGraphic as LowPolyPanelGraphic;
        if(surface==null)return;
        surface.ConfigureStorybookStyle(coral?new Color32(255,155,125,255):IndigoTop,
            coral?new Color32(237,83,76,255):IndigoBottom,22f);
        if(Application.isPlaying)surface.ConfigureHudPanelFinish();
        surface.raycastTarget=true;
        button.transition=Selectable.Transition.None;
        var fx=button.GetComponent<PremiumButtonFx>()??button.gameObject.AddComponent<PremiumButtonFx>();
        fx.Configure(surface.rectTransform,surface,button,coral);
        ApplyButtonArtwork(button);
    }

    public static void StyleCompanion(Button button,Texture2D icon)
    {
        if(button==null)return;
        var bounds=(RectTransform)button.transform;
        bounds.sizeDelta=new Vector2(248,60);
        StyleButton(button,false);
        var face=button.targetGraphic.transform;
        var old=face.Find("TogetherPaw");if(old!=null)old.gameObject.SetActive(false);
        var art=face.Find("StorybookCatHead") as RectTransform;
        if(art==null)
        {
            art=new GameObject("StorybookCatHead",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage)).GetComponent<RectTransform>();
            art.SetParent(face,false);
        }
        art.anchorMin=art.anchorMax=art.pivot=new Vector2(.5f,.5f);
        art.anchoredPosition=new Vector2(-87,0);art.sizeDelta=new Vector2(52,52);
        var image=art.GetComponent<RawImage>();image.texture=icon;image.color=Color.white;image.raycastTarget=false;
        ApplyButtonArtwork(button);
        foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            label.color=Cream;label.font=PremiumTypography.Emphasis;
            label.fontSize=23;label.enableAutoSizing=true;label.fontSizeMin=20;label.fontSizeMax=23;
            label.textWrappingMode=TextWrappingModes.Normal;label.overflowMode=TextOverflowModes.Ellipsis;
            label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(29,0);rect.sizeDelta=new Vector2(166,52);
        }
    }
}
