using TMPro;
using UnityEngine;
using UnityEngine.UI;
using U=PremiumUiElements;

/// <summary>Shared photographic stage and reward tray for the home's milestone cards.</summary>
public static class PremiumMomentArt
{
    public static RectTransform Stage(Transform parent,float x,float y,float width,float height)
    {
        var stage=JoyfulUiArt.Panel("MomentStage",parent,JoyfulUiArt.Ocean,x,y,width,height,28).rectTransform;
        JoyfulUiArt.Motif(stage,"MomentRays",0,42,width*.96f,width*.96f,new Color(1,1,1,.19f),true);
        JoyfulUiArt.Panel("StageHalo",stage,JoyfulUiArt.Gold,0,36,width*.78f,width*.78f,width*.39f);
        JoyfulUiArt.Sticker(stage,"MomentPaw","Paw",JoyfulUiArt.Paper,-width*.32f,height*.34f,66,-12);
        JoyfulUiArt.Motif(stage,"MomentStars",0,0,width*.91f,height*.93f,new Color(1,1,1,.6f));
        return stage;
    }
    public static TMP_Text Caption(Transform parent,string tr,string en,float x,float y,float width,float height,float size=22)
    {
        bool onStage=parent.name=="MomentStage"||x< -200;
        var label=U.Label("MomentCaption",parent,PremiumTypography.Body,size,onStage?Color.white:JoyfulUiArt.Ink,x,y,width,height,TextAlignmentOptions.Center);
        label.gameObject.AddComponent<BilingualCopyLabel>().Configure(tr,en);return label;
    }
    public static void RewardTray(Transform parent,float x,float y,float width,float height)
    {
        var tray=JoyfulUiArt.Panel("RewardTray",parent,new Color32(255,233,172,255),x,y,width,height,22);
        var coin=U.Rect("RewardCoin",tray.transform);U.At(coin,-width*.5f+54,0,68,68);
        var image=coin.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
        var art=Resources.Load<PremiumMomentArtSet>("PremiumMomentArt");
        if(art!=null)image.texture=art.Coin;
    }
    public static void FitParent(RectTransform panel,float width,float height)
    {
        var holder=U.Rect("MomentSafeFit",panel.parent);U.Fill(holder);
        panel.SetParent(holder,false);
        holder.gameObject.AddComponent<PremiumMomentSafeFit>().Configure(width,height);
    }
}

public sealed class PremiumMomentSafeFit:MonoBehaviour
{
    private float width,height;
    public void Configure(float w,float h){width=w;height=h;}
    private void LateUpdate()
    {
        var canvas=GetComponentInParent<Canvas>();if(canvas==null||width<=0)return;
        float scale=Mathf.Min(1,(Screen.safeArea.width/canvas.scaleFactor-48)/width,
            (Screen.safeArea.height/canvas.scaleFactor-48)/height);
        transform.localScale=Vector3.one*Mathf.Max(.1f,scale);
    }
}
