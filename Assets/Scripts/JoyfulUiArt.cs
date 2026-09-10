using TMPro;
using UnityEngine;
using UnityEngine.UI;
using U=PremiumUiElements;

/// <summary>Cat Home's colourful paper, toy badges and short, optional celebration accents.</summary>
public static class JoyfulUiArt
{
    public static readonly Color Ink=ModernUiArt.Ink;
    public static readonly Color Ocean=ModernUiArt.Azure;
    public static readonly Color OceanLight=ModernUiArt.AzureTop;
    public static readonly Color Coral=ModernUiArt.Coral;
    public static readonly Color Gold=ModernUiArt.Gold;
    public static readonly Color Purple=ModernUiArt.Purple;
    public static readonly Color Paper=ModernUiArt.Paper;
    public static readonly Color SkyPaper=ModernUiArt.Inset;

    public static void Surface(LowPolyPanelGraphic graphic,Color bottom,float radius=24)
    {
        ModernUiArt.Surface(graphic,bottom,radius,graphic!=null&&graphic.rectTransform.rect.height>=120);
    }
    public static LowPolyPanelGraphic Panel(string name,Transform parent,Color color,float x,float y,float w,float h,float radius=24,bool raycast=false)
    {var p=U.Panel(name,parent,color,x,y,w,h,radius,raycast);Surface(p,color,radius);return p;}
    public static RawImage Icon(string name,Transform parent,string icon,float x,float y,float size)
    {
        var rect=U.Rect(name,parent);U.At(rect,x,y,size,size);
        var image=rect.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>("PremiumInterface/"+icon);image.raycastTarget=false;return image;
    }
    public static RectTransform Sticker(Transform parent,string name,string icon,Color color,float x,float y,float size,float tilt=0)
    {
        var p=Panel(name,parent,color,x,y,size,size,size*.28f);
        p.rectTransform.localEulerAngles=new Vector3(0,0,tilt);
        Icon("SculptedArt",p.transform,icon,0,0,size*.72f);return p.rectTransform;
    }
    public static Button Action(string name,Transform parent,string tr,string en,Color color,float x,float y,float w,float h,out TMP_Text label)
    {
        var b=U.Action(name,parent,PremiumTypography.Emphasis,null,color,x,y,w,h,out label);
        ModernUiArt.Action(b,ModernUiArt.IsLight(color));
        label.gameObject.AddComponent<BilingualCopyLabel>().Configure(tr,en);return b;
    }
    public static void ActionStyle(Button button,Color color,bool darkText=false)
    {
        PlayfulUiArt.Action(button,color,darkText);
    }
    public static JoyfulMotifGraphic Motif(Transform parent,string name,float x,float y,float w,float h,Color color,bool rays=false)
    {
        var rect=U.Rect(name,parent);U.At(rect,x,y,w,h);
        var graphic=rect.gameObject.AddComponent<JoyfulMotifGraphic>();graphic.color=color;graphic.raycastTarget=false;graphic.Rays=rays;return graphic;
    }
    public static void Celebrate(RectTransform parent)
    {
        if(parent==null)return;
        var rect=parent.Find("JoyfulConfetti") as RectTransform;
        if(rect==null){rect=U.Rect("JoyfulConfetti",parent);U.Fill(rect);rect.gameObject.AddComponent<JoyfulConfettiGraphic>();}
        rect.SetAsLastSibling();rect.GetComponent<JoyfulConfettiGraphic>().Replay();
    }
}

