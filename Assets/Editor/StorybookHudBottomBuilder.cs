using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using U=PremiumUiElements;

/// <summary>Restyles only the home HUD's existing bottom controls; no navigation or movement changes.</summary>
public static class StorybookHudBottomBuilder
{
    private static readonly Color Cream=StorybookHudBottomPresentation.Cream;
    private static readonly Color Mint=new Color32(139,227,207,255);
    private static readonly Color IndigoTop=StorybookHudBottomPresentation.IndigoTop;
    private static readonly Color IndigoBottom=StorybookHudBottomPresentation.IndigoBottom;

    public static void Apply(Transform root)
    {
        if(root==null)return;
        var cat=AssetDatabase.LoadAssetAtPath<Texture2D>(StorybookTitleBuilder.CatIconPath);
        var actions=AssetDatabase.LoadAssetAtPath<Texture2D>(StorybookTitleBuilder.ActionIconPath);
        foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).ToArray())
        {
            if(rect.name=="HomeDock")Dock(rect,actions);
            else if(rect.name=="JoystickBackground"&&rect.GetComponent<MobileJoystick>()!=null)Joystick(rect);
            else if(rect.name=="ActionButton"||rect.name=="ActivityActionButton"||rect.name=="ActivityProgressBadge")Context(rect);
            else if(rect.name=="CompanionShortcut")StorybookHudBottomPresentation.StyleCompanion(rect.GetComponent<Button>(),cat);
        }
        // Only the authored home Canvas owns the deferred runtime shortcut polish.
        if(root.GetComponent<Canvas>()!=null&&root.GetComponentInChildren<MobileJoystick>(true)!=null)
        {
            var strip=root.Find("ModernHomeViewportStrip");
            var presenter=root.GetComponent<StorybookHudBottomPresentation>()??root.gameObject.AddComponent<StorybookHudBottomPresentation>();
            presenter.Configure(cat,strip!=null?strip.GetComponent<Image>():null);
        }
    }

    private static void Dock(RectTransform dock,Texture2D actions)
    {
        // Keep the 80-unit world viewport reservation and the existing responsive layout.
        dock.sizeDelta=new Vector2(1080,64);
        var tray=Find(dock,"DockEnamelTray");
        if(tray!=null)
        {
            var panel=tray.GetComponent<LowPolyPanelGraphic>();
            if(panel!=null){panel.ConfigureModernStyle(StorybookHudBottomPresentation.StripColor,StorybookHudBottomPresentation.StripColor,0);panel.raycastTarget=false;}
        }
        var frame=Find(dock,"StorybookDockFrame");
        if(frame==null)frame=U.Panel("StorybookDockFrame",dock,IndigoBottom,0,0,1088,76,26).rectTransform;
        U.At(frame,0,0,1088,76);frame.SetAsFirstSibling();
        // The full-width opaque tray stays behind the compact sculpted frame.
        if(tray!=null)tray.SetAsFirstSibling();
        var surface=frame.GetComponent<LowPolyPanelGraphic>();surface.ConfigureStorybookStyle(IndigoTop,IndigoBottom,26);surface.raycastTarget=false;
        foreach(var rect in dock.GetComponentsInChildren<RectTransform>(true))
            if(rect.name.StartsWith("ModernDockDivider"))rect.gameObject.SetActive(false);

        foreach(var button in dock.GetComponentsInChildren<Button>(true))
        {
            int slot=button.name=="ShopButton"?0:button.name=="CurrentRoomStatus"?1:button.name=="PlayCatRunnerButton"?3:-1;
            if(slot<0)continue;
            U.At((RectTransform)button.transform,-405+slot*270,0,248,60);
            StorybookHudBottomPresentation.StyleButton(button,slot==1);
            var face=button.targetGraphic.transform;
            foreach(var art in face.GetComponentsInChildren<RectTransform>(true))
                if(art.name=="DockArtWell"||art.name=="BackdropMotif"||art.name.StartsWith("Sculpted"))art.gameObject.SetActive(false);
            Icon(face,"StorybookDockIcon",actions,slot==0?new Rect(0,.5f,.5f,.5f):slot==1?new Rect(.5f,.5f,.5f,.5f):new Rect(.5f,0,.5f,.5f),-83,0,58);
            foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if(label.name=="RunnerEnergyLabel"){label.gameObject.SetActive(false);continue;}
                bool progress=label.name=="RoomProgressLabel";
                Type(label,progress?22:24);
                U.At(label.rectTransform,26,progress?-13:slot==1?12:0,170,progress?26:slot==1?28:48);
                if(progress){label.fontSizeMin=18;label.textWrappingMode=TextWrappingModes.NoWrap;}
            }
        }
    }

    private static void Joystick(RectTransform root)
    {
        var baseRect=Find(root,"PremiumBase");
        if(baseRect!=null)
        {
            var panel=baseRect.GetComponent<LowPolyPanelGraphic>();
            if(panel!=null){panel.ConfigureStorybookStyle(IndigoTop,IndigoBottom,94);panel.raycastTarget=false;}
            foreach(var direction in baseRect.GetComponentsInChildren<TMP_Text>(true))
                if(direction.name.StartsWith("Direction")){direction.color=Mint;direction.raycastTarget=false;direction.gameObject.SetActive(true);}
        }
        var thumb=Find(root,"PremiumHandle");
        if(thumb==null)return;
        U.At(thumb,0,0,100,100);
        var ring=thumb.GetComponent<LowPolyPanelGraphic>();
        if(ring!=null){ring.ConfigureStorybookStyle(new Color32(169,247,226,255),new Color32(66,186,167,255),50);ring.raycastTarget=false;}
        foreach(var child in thumb.GetComponentsInChildren<RectTransform>(true))
            if(child!=thumb&&child.name.StartsWith("Sculpted"))child.gameObject.SetActive(false);
        var center=Find(thumb,"StorybookThumbCap");
        if(center==null)center=U.Panel("StorybookThumbCap",thumb,Cream,0,0,66,66,33).rectTransform;
        U.At(center,0,0,66,66);center.SetAsLastSibling();
        var cap=center.GetComponent<LowPolyPanelGraphic>();cap.ConfigureStorybookStyle(new Color32(255,253,235,255),new Color32(224,203,174,255),33);cap.raycastTarget=false;
    }

    private static void Context(RectTransform root)
    {
        var panel=Find(root,"ContextFace");if(panel==null)return;
        var graphic=panel.GetComponent<LowPolyPanelGraphic>();if(graphic==null)return;
        bool progress=root.name=="ActivityProgressBadge";
        graphic.ConfigureStorybookStyle(progress?IndigoTop:new Color32(255,155,125,255),progress?IndigoBottom:new Color32(237,83,76,255),26);
        graphic.raycastTarget=!progress;
        var button=root.GetComponent<Button>();if(button!=null)StorybookHudBottomPresentation.StyleButton(button,true);
        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if(label.name.ToLowerInvariant().Contains("shadow"))continue;
            Type(label,progress?23:27);label.fontSizeMin=20;
            U.Fill(label.rectTransform,14);label.textWrappingMode=TextWrappingModes.Normal;
        }
        // Existing controller owns activity visibility, label copy, progress and click handlers.
    }

    private static void Type(TMP_Text label,float size)
    {
        label.color=Cream;label.font=PremiumTypography.Emphasis;label.fontSize=size;
        label.enableAutoSizing=true;label.fontSizeMin=size*.8f;label.fontSizeMax=size;
        label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
        label.textWrappingMode=TextWrappingModes.Normal;label.overflowMode=TextOverflowModes.Ellipsis;
        label.enableVertexGradient=false;label.characterSpacing=0;
    }
    private static RectTransform Find(Transform root,string name)=>root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r=>r.name==name);
    private static void Icon(Transform parent,string name,Texture2D texture,Rect uv,float x,float y,float size)
    {
        var rect=parent.Find(name) as RectTransform;if(rect==null)rect=U.Rect(name,parent);
        U.At(rect,x,y,size,size);
        if(rect.GetComponent<CanvasRenderer>()==null)rect.gameObject.AddComponent<CanvasRenderer>();
        var image=rect.GetComponent<RawImage>()??rect.gameObject.AddComponent<RawImage>();
        image.texture=texture;image.uvRect=uv;image.color=Color.white;image.raycastTarget=false;
    }
}
