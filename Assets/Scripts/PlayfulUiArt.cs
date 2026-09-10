using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PlayfulUiArt
{
    public static readonly Color Navy=new Color32(31,43,83,255);
    public static readonly Color Teal=new Color32(17,148,158,255);
    public static readonly Color Coral=new Color32(238,82,104,255);
    public static readonly Color Violet=new Color32(112,76,207,255);
    public static readonly Color Gold=new Color32(255,192,67,255);
    public static void Action(Button button,Color accent,bool secondary=false)
    {
        ModernUiArt.Action(button,secondary);
        if(button==null)return;
        var surface=button.targetGraphic as LowPolyPanelGraphic;
        if(surface!=null)
        {
            if(secondary)surface.ConfigureModernStyle(Color.white,Color.Lerp(accent,Color.white,.89f),18,true);
            else surface.ConfigurePlayfulAction(Color.Lerp(accent,Color.white,.16f),accent,18);
        }
        foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
            label.color=secondary||accent==Gold?Navy:Color.white;
    }
}
