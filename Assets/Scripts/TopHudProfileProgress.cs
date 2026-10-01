using TMPro;
using UnityEngine;

/// <summary>Two existing progress sources, read only: Bond XP count and Home Level fill.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class TopHudProfileProgress : MonoBehaviour
{
    private TMP_Text label;
    private RectTransform fill;
    public void Configure(TMP_Text text,RectTransform bar){label=text;fill=bar;Refresh();}
    private void OnEnable(){ProgressionService.StateChanged+=Refresh;HomeProgressionService.Changed+=Refresh;Refresh();}
    private void OnDisable(){ProgressionService.StateChanged-=Refresh;HomeProgressionService.Changed-=Refresh;}
    private void Refresh()
    {
        if(label!=null)label.text="XP " + ProgressionService.BondXp.ToString("N0",System.Globalization.CultureInfo.InvariantCulture);
        if(fill!=null)
        {
            long span=HomeProgressionService.XpForCurrentLevel;
            float ratio=span==0?1f:Mathf.Clamp01((float)((double)HomeProgressionService.XpIntoCurrentLevel/span));
            var track=fill.parent as RectTransform;
            fill.sizeDelta=new Vector2((track!=null?track.rect.width:104)*ratio,10);fill.gameObject.SetActive(ratio>0);
        }
    }
}
