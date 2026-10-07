using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>Small shared UI vocabulary for the four home mini-games.</summary>
public static class CozyGameUi
{
    public static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 inset)
    {
        var r=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=inset;r.offsetMax=-inset;return r;
    }
    public static LowPolyPanelGraphic Panel(string name,Transform parent,Vector2 min,Vector2 max,bool shell=false)
    {
        var p=Rect(name,parent,min,max,Vector2.zero).gameObject.AddComponent<LowPolyPanelGraphic>();
        if(shell)StorybookScreenStyle.RoomShell(p,28);else StorybookScreenStyle.Card(p,20);
        p.raycastTarget=true;return p;
    }
    public static TMP_Text Text(string name,Transform parent,string text,Vector2 min,Vector2 max,float size=28,bool heading=false)
    {
        var t=Rect(name,parent,min,max,new Vector2(8,4)).gameObject.AddComponent<TextMeshProUGUI>();
        t.font=heading?PremiumTypography.Display:PremiumTypography.Body;t.fontSize=size;t.color=StorybookScreenStyle.Ink;
        t.text=text;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;
        t.enableAutoSizing=true;t.fontSizeMin=size*.78f;t.fontSizeMax=size;return t;
    }
    public static Button Button(string name,Transform parent,string text,Vector2 min,Vector2 max,UnityAction action,bool secondary=false)
    {
        var p=Panel(name,parent,min,max);var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p;
        var label=Text("Label",p.transform,text,Vector2.zero,Vector2.one,27,true);label.alignment=TextAlignmentOptions.Center;
        StorybookScreenStyle.Action(b,secondary);if(action!=null)b.onClick.AddListener(action);
        var cb=b.colors;cb.pressedColor=new Color(.75f,.85f,.83f);cb.disabledColor=new Color(.45f,.5f,.5f,.7f);b.colors=cb;
        return b;
    }
    public static void Label(Button button,string text) { var t=button.GetComponentInChildren<TMP_Text>();if(t!=null)t.text=text; }
    public static string Copy(string tr,string en)=>GameContentCopy.Text(tr,en);
}
