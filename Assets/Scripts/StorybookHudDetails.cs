using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Blender-rendered top HUD artwork on the existing bound controls.</summary>
public static class StorybookHudDetails
{
    private static readonly Color Ink = new Color32(36,65,97,255);
    private static readonly Color Paper = new Color32(255,253,243,255);
    private static readonly Dictionary<string,Sprite> Sprites = new();
    public static void Apply(RectTransform food, RectTransform water, RectTransform energy,
        RectTransform identity, RectTransform menu, RectTransform coin, RectTransform diamond, RectTransform bond)
    {
        Hide(food,"StorybookNeedsBand");
        int i=0;
        foreach(var need in new[]{food,water,energy})
        {
            string key=new[]{"food","water","energy"}[i++];
            foreach(string old in new[]{"HudTrackRim","NeedArtWell","tinyIcon","labelShadow","EnergyGoldStar","NeedGlint","WaterSweep"}) Hide(need,old);
            var background=need.Find("Background").GetComponent<LowPolyPanelGraphic>();
            Hide(background.transform,"NeedArtWell"); Cover(background,"panel-"+key,false);
            var label=need.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name.EndsWith("LabelFront"));
            Text(label,new Vector2(10,16),new Vector2(140,38),29);label.color=Paper;
            var value=need.Find("PercentageText").GetComponent<TMP_Text>();
            Text(value,new Vector2(need==food?115:need==water?100:105,16),new Vector2(78,36),28);value.color=Paper;
            var track=need.Find("FillTrackMask") as RectTransform;
            Rect(track,new Vector2(38,-17),new Vector2(need==food?228:need==water?204:214,19));
            // The bound Image supplies horizontal fill; the rendered strip has its own rounded silhouette.
            var mask=track.GetComponent<Mask>();if(mask!=null)mask.enabled=false;
            LegacyOff(track);
            var trough=Art(need,"BlenderTrack","bar-track",new Vector2(38,-17),new Vector2(need==food?228:need==water?204:214,19),false);
            trough.transform.SetAsLastSibling();
            track.SetAsLastSibling();
            var fill=track.GetComponentsInChildren<Image>(true).FirstOrDefault(t=>t.name=="Fill");
            if(fill!=null){fill.sprite=Sprite("fill-"+key);fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;fill.color=Color.white;}
            Hide(need,new[]{"SculptedFood","SculptedWater","SculptedEnergy"}[i-1]);
            float iconX=need==food?-125:need==water?-112.5f:-114.8f;
            Art(need,"V2Well","well-"+key,new Vector2(iconX,0),new Vector2(96,96),true);
            // Match visible weight; the open crescent uses an optical offset so its star and tips clear the rim.
            var iconSize=need==food?new Vector2(66,68):need==water?new Vector2(70,74):new Vector2(70,72);
            var iconOffset=need==food?new Vector2(0,-1.30f):need==water?new Vector2(0,4.11f):new Vector2(4,-1.30f);
            Art(need,"V2Icon",key,new Vector2(iconX,0)+iconOffset,iconSize,true).transform.SetAsLastSibling();
        }
        bond.gameObject.SetActive(false);
        var face=identity.Find("Face");
        TouchTarget(identity);
        Rect((RectTransform)face,new Vector2(0,-1),new Vector2(357,90));
        ButtonArt(identity,Cover(face.GetComponent<LowPolyPanelGraphic>(),"profile",false));
        var medallion=face.Find("PortraitMedallion").GetComponent<LowPolyPanelGraphic>();
        Rect(medallion.rectTransform,new Vector2(-128,1),new Vector2(103,103));
        Cover(medallion,"portrait-ring",false);
        var portrait=face.Find("SelectedCatFace").GetComponent<Image>();
        Rect(portrait.rectTransform,new Vector2(-128,1),new Vector2(86,86));portrait.raycastTarget=false;
        if(portrait.GetComponent<StorybookPortraitCrop>()==null)portrait.gameObject.AddComponent<StorybookPortraitCrop>();
        Art(face,"ProfileGoldBadge","badge",new Vector2(-88,-31),new Vector2(36,36),true);
        Hide(face,"BadgeShine");Hide(face,"CatName");Hide(face,"V2XpRim");
        var level=face.Find("HomeLevelText").GetComponent<TMP_Text>();Text(level,new Vector2(36,15),new Vector2(190,46),32);
        var xp=Label(face,"V2Xp",level);Text(xp,new Vector2(-13,-20),new Vector2(88,25),19);
        var xpTrack=Node(face,"V2XpTrack",new Vector2(86,-20),new Vector2(137,12));
        LegacyOff(xpTrack);Art(xpTrack,"BlenderSurface","xp-track",Vector2.zero,new Vector2(137,12),false).transform.SetAsFirstSibling();
        var xpFill=Node(xpTrack,"Fill",Vector2.zero,new Vector2(0,10));LegacyOff(xpFill);
        xpFill.pivot=new Vector2(0,.5f);xpFill.anchorMin=xpFill.anchorMax=new Vector2(0,.5f);
        xpFill.localScale=Vector3.one;
        Stretch(Art(xpFill,"BlenderSurface","fill-xp",Vector2.zero,Vector2.zero,false));
        var view=face.GetComponent<TopHudProfileProgress>();if(view==null)view=face.gameObject.AddComponent<TopHudProfileProgress>();view.Configure(xp,xpFill);
        foreach(var entry in new[]{coin,diamond})
        {
            Cover(entry.Find("Face").GetComponent<LowPolyPanelGraphic>(),"currency",false);
            var value=entry.Find("Value").GetComponent<TMP_Text>();value.color=Ink;
            Hide(entry,"Icon");
            var icon=Art(entry,"V2Icon",entry==coin?"coin":"diamond",Vector2.zero,new Vector2(60,60),true);
            Hide(icon.transform,"WalletGlint");
            var plus=entry.Find("PlusButton") as RectTransform;
            entry.GetComponent<CurrencyEntryLayout>().Configure(icon.rectTransform,value,plus,60,14,14,50,24,78,18,30);
            TouchTarget(plus);
            var old=plus.GetComponentsInChildren<LowPolyPanelGraphic>(true).First(t=>t.name=="Face");
            Rect(old.rectTransform,Vector2.zero,new Vector2(50,50));
            foreach(var g in plus.GetComponentsInChildren<Graphic>(true))if(g.transform!=plus)g.enabled=false;
            ButtonArt(plus,Cover(old,"plus",false));
        }
        TouchTarget(menu);
        var menuFace=menu.GetComponentsInChildren<LowPolyPanelGraphic>(true).First(t=>t.name=="Face");
        Rect(menuFace.rectTransform,Vector2.zero,new Vector2(77,75));
        foreach(var g in menuFace.GetComponentsInChildren<Graphic>(true))g.enabled=false;
        ButtonArt(menu,Cover(menuFace,"menu",false));
    }
    private static Sprite Sprite(string key)
    {
        if(!Sprites.TryGetValue(key,out var sprite)||sprite==null)
        {sprite=Resources.LoadAll<Sprite>("TopHudExact/"+key).FirstOrDefault();Sprites[key]=sprite;}
        return sprite;
    }
    private static Image Cover(LowPolyPanelGraphic old,string key,bool sliced)
    {
        old.enabled=false;
        var image=Art(old.transform,"BlenderSurface",key,Vector2.zero,Vector2.zero,false);
        image.type=sliced?Image.Type.Sliced:Image.Type.Simple;
        image.pixelsPerUnitMultiplier=3f;
        Stretch(image);image.transform.SetAsFirstSibling();return image;
    }
    private static void Stretch(Image image)
    {var r=image.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    private static Image Art(Transform parent,string name,string key,Vector2 pos,Vector2 size,bool aspect)
    {
        var t=parent.Find(name);
        var g=t==null?new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>():t.GetComponent<Image>();
        g.transform.SetParent(parent,false);Rect(g.rectTransform,pos,size);g.sprite=Sprite(key);g.type=Image.Type.Simple;
        g.preserveAspect=aspect;g.raycastTarget=false;g.color=Color.white;g.enabled=true;g.gameObject.SetActive(true);return g;
    }
    private static void ButtonArt(RectTransform root,Image visual)
    {
        foreach(var fx in root.GetComponentsInChildren<PremiumButtonFx>(true))fx.enabled=false;
        var button=root.GetComponent<Button>();if(button==null)return;
        button.targetGraphic=visual;button.transition=Selectable.Transition.ColorTint;
        var colors=ColorBlock.defaultColorBlock;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;
        colors.disabledColor=Color.white;colors.pressedColor=new Color(.82f,.82f,.82f,1);colors.fadeDuration=.08f;button.colors=colors;
    }
    private static void TouchTarget(RectTransform r)
    {var g=r.GetComponent<Graphic>();if(g==null)g=r.gameObject.AddComponent<Image>();g.color=Color.clear;g.raycastTarget=true;}
    private static void LegacyOff(Transform t){var g=t.GetComponent<LowPolyPanelGraphic>();if(g!=null)g.enabled=false;}
    private static void Hide(Transform parent,string path){var t=parent.Find(path);if(t!=null)t.gameObject.SetActive(false);}
    private static void Text(TMP_Text t,Vector2 p,Vector2 size,float font){Rect(t.rectTransform,p,size);t.color=Ink;t.fontSize=font;t.enableAutoSizing=false;t.raycastTarget=false;}
    private static TMP_Text Label(Transform parent,string name,TMP_Text source)
    {
        var t=parent.Find(name);var label=t==null?new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)).GetComponent<TMP_Text>():t.GetComponent<TMP_Text>();
        label.transform.SetParent(parent,false);label.font=source.font;label.fontStyle=FontStyles.Normal;label.alignment=TextAlignmentOptions.MidlineLeft;return label;
    }
    private static RectTransform Node(Transform parent,string name,Vector2 pos,Vector2 size)
    {var t=parent.Find(name) as RectTransform;if(t==null){t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.SetParent(parent,false);}Rect(t,pos,size);return t;}
    private static void Rect(RectTransform r,Vector2 pos,Vector2 size){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;r.localScale=Vector3.one;}
}
