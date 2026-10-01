using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>Top HUD V2 presentation; services and input remain with their existing owners.</summary>
[ExecuteAlways, DefaultExecutionOrder(350), DisallowMultipleComponent]
public sealed class StorybookHudLayout : MonoBehaviour
{
    public const float TopPanelHeight = 90f;
    public const float TopPanelCenter = 53f;
    public const float TopPanelBottom = 106f;
    [SerializeField] private RectTransform food, water, energy;
    private RectTransform identity, menu, coin, diamond, bond;
    private MainPanelController mainOwner, detailsOwner;
    private TMP_Text[] percentages;
    private UnityEngine.UI.Image[] needFills;
    private static readonly Color[] FillColors={Color.white,Color.white,Color.white};
    private float nextResolve;
    private static readonly Dictionary<TMP_Text, StorybookHudLayout> percentageOwners = new();
    private static readonly Color Ink = new Color32(255,253,242,255);
    private static readonly Color Critical = new Color32(255,230,175,255);
    public static Color NeedPercentageColor(TMP_Text label,int percentage,Color legacyColor) =>
        label!=null && percentageOwners.TryGetValue(label,out var owner) && owner!=null && owner.isActiveAndEnabled
            ? (percentage==0?Critical:Ink) : legacyColor;
    private void ReleasePercentages()
    {
        if(percentages==null)return;
        foreach(var label in percentages)
            if(!ReferenceEquals(label,null)&&percentageOwners.TryGetValue(label,out var owner)&&owner==this)percentageOwners.Remove(label);
        percentages=null;needFills=null;
    }
    public void Configure(RectTransform f,RectTransform w,RectTransform e)
    {ReleasePercentages();food=f;water=w;energy=e;detailsOwner=null;Refresh();}
    private void OnEnable(){nextResolve=0;ReleasePercentages();detailsOwner=null;Canvas.preWillRenderCanvases-=Refresh;Canvas.preWillRenderCanvases+=Refresh;}
    private void OnDisable(){Canvas.preWillRenderCanvases-=Refresh;ReleasePercentages();}
    private void LateUpdate()=>Refresh();
    public void Refresh()
    {
        if(food==null||water==null||energy==null)return;
        if((identity==null||coin==null||menu==null||mainOwner==null||!mainOwner.gameObject.activeInHierarchy)&&Time.realtimeSinceStartup>=nextResolve)
        {
            nextResolve=Time.realtimeSinceStartup+.5f;
            var main=FindObjectsByType<MainPanelController>(FindObjectsInactive.Include).Where(m=>m.gameObject.scene.IsValid()).OrderByDescending(m=>m.gameObject.activeInHierarchy).ThenByDescending(m=>m.gameObject.scene.name=="CatHome_UI").FirstOrDefault();
            if(main!=null){mainOwner=main;identity=main.transform.Find("SafeArea/CatShopButton") as RectTransform;menu=main.transform.Find("SafeArea/MenuButton") as RectTransform;}
            var wallet=FindObjectsByType<CurrencyHudController>(FindObjectsInactive.Include).FirstOrDefault(m=>m.gameObject.scene.IsValid());
            if(wallet!=null){coin=wallet.transform.Find("SafeArea/Group/CoinEntry") as RectTransform;diamond=wallet.transform.Find("SafeArea/Group/DiamondEntry") as RectTransform;bond=wallet.transform.Find("SafeArea/BondXpEntry") as RectTransform;}
        }
        var safe=Screen.safeArea;float s=Mathf.Min(safe.width/1920f,safe.height/1080f);if(s<=0)return;
        if(mainOwner!=null&&identity!=null&&menu!=null&&coin!=null&&diamond!=null&&bond!=null&&detailsOwner!=mainOwner)
        {StorybookHudDetails.Apply(food,water,energy,identity,menu,coin,diamond,bond);detailsOwner=mainOwner;}
        // Native reference edges, normalized from 1672x941 to 1920x1080.
        Place(identity,safe.xMin+201.5f*s,safe.yMax-53.5f*s,new Vector2(357,103),s);
        float coinWidth=WalletWidth(coin,187),diamondWidth=WalletWidth(diamond,184);
        float menuRight=safe.xMax-21*s;
        Place(menu,menuRight-38.5f*s,safe.yMax-54.5f*s,new Vector2(77,75),s);
        float diamondRight=menuRight-87*s;
        Place(diamond,diamondRight-diamondWidth*.5f*s,safe.yMax-55.5f*s,new Vector2(diamondWidth,73),s);
        float coinRight=diamondRight-(diamondWidth+14)*s;
        Place(coin,coinRight-coinWidth*.5f*s,safe.yMax-55.5f*s,new Vector2(coinWidth,73),s);
        // Long live balances may widen the right group. Reserve its real left edge
        // without allowing it to cover Energy; the ordinary reference values are 1:1.
        float available=(coinRight-coinWidth*s-16*s-(safe.xMin+394*s))/s;
        float needsScale=Mathf.Min(1f,available/1019f);
        Place(food,safe.xMin+(394+174.5f*needsScale)*s,safe.yMax-53*s,new Vector2(349,90),s*needsScale);
        Place(water,safe.xMin+(394+522*needsScale)*s,safe.yMax-53*s,new Vector2(318,90),s*needsScale);
        Place(energy,safe.xMin+(394+854.5f*needsScale)*s,safe.yMax-53*s,new Vector2(329,90),s*needsScale);
        if(percentages==null)
        {percentages=new[]{food,water,energy}.Select(r=>r.Find("PercentageText")?.GetComponent<TMP_Text>()).ToArray();foreach(var text in percentages)if(text!=null)percentageOwners[text]=this;}
        foreach(var text in percentages)if(text!=null)text.color=text.text=="0%"?Critical:Ink;
        // Need controllers own the amount; this presentation owns its panel palette.
        if(needFills==null)needFills=new[]{food,water,energy}.Select(r=>r.GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault(g=>g.name=="Fill")).ToArray();
        for(int i=0;i<needFills.Length;i++)if(needFills[i]!=null&&needFills[i].color!=FillColors[i])needFills[i].color=FillColors[i];
    }
    private static float WalletWidth(RectTransform entry,float minimum)
    {
        if(entry==null)return minimum;
        var layout=entry.GetComponent<CurrencyEntryLayout>();
        return Mathf.Clamp((layout!=null?layout.ContentWidth:166)+18,minimum,248);
    }
    private static void Place(RectTransform r,float x,float y,Vector2 size,float pixelScale)
    {
        if(r==null||!(r.parent is RectTransform parent))return;
        var canvas=r.GetComponentInParent<Canvas>();if(canvas==null)return;
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,new Vector2(x,y),camera,out var local))return;
        r.anchorMin=r.anchorMax=r.name=="CatShopButton"?new Vector2(0,1):r.name=="MenuButton"?Vector2.one:new Vector2(.5f,.5f);
        r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;
        var anchor=new Vector2(Mathf.Lerp(parent.rect.xMin,parent.rect.xMax,r.anchorMin.x),Mathf.Lerp(parent.rect.yMin,parent.rect.yMax,r.anchorMin.y));
        r.anchoredPosition=local-anchor;float scale=pixelScale/Mathf.Max(.001f,parent.lossyScale.x);r.localScale=new Vector3(scale,scale,1);
    }
}

