using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Opt-in presentation: title, return summary and speech only.
public static class WelcomeRefreshPresentation
{
    public static readonly Color Ink=new Color32(27,65,70,255);
    static readonly Color Paper=new Color32(255,250,236,255);
    static readonly Color Muted=new Color32(94,123,120,255);
    static readonly Color Teal=new Color32(42,108,106,255);
    static Sprite conversation;
    public static void Emblem(Transform parent,string name,Vector2 position,float size)
    {
        var texture=Resources.Load<Texture2D>("WelcomeRefresh/PawEmblem");if(texture==null)return;
        var child=parent.Find(name);var image=child==null?null:child.GetComponent<RawImage>();
        if(image==null){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));go.transform.SetParent(parent,false);image=go.GetComponent<RawImage>();}
        image.texture=texture;image.raycastTarget=false;image.color=Color.white;
        var r=image.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=Vector2.one*size;
    }
    public static Sprite Conversation
    {
        get
        {
            if(conversation==null)
            {
                var texture=Resources.Load<Texture2D>("WelcomeRefresh/Conversation");
                if(texture!=null)conversation=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
            }
            return conversation;
        }
    }
    static void Surface(Transform root,string path,Color top,Color bottom,float radius)
    {
        var t=string.IsNullOrEmpty(path)?root:root.Find(path);
        var g=t==null?null:t.GetComponent<LowPolyPanelGraphic>();
        if(g==null)return;
        g.ConfigureHudArtwork(null);g.ConfigureScreenStyle(top,bottom,radius,false,false);
    }
    static void Text(Transform root,string path,Color color,float size=0,bool body=false)
    {
        var t=root.Find(path);var label=t==null?null:t.GetComponent<TMP_Text>();if(label==null)return;
        label.color=color;label.enableVertexGradient=false;
        if(body){label.font=PremiumTypography.Body;label.fontStyle=FontStyles.Normal;}
        if(size>0){label.fontSize=size;label.fontSizeMax=size;label.fontSizeMin=size*.85f;}
    }
    static void Button(Transform root,bool primary=false)
    {
        if(root==null)return;var b=root.GetComponent<Button>();if(b==null)return;
        var g=b.targetGraphic as LowPolyPanelGraphic;
        if(g!=null){g.ConfigureHudArtwork(null);g.ConfigureScreenStyle(primary?new Color32(246,151,124,255):Paper,primary?new Color32(218,105,88,255):new Color32(235,240,226,255),24,primary,false);}
        var contrast=b.GetComponent<StorybookActionContrast>()??b.gameObject.AddComponent<StorybookActionContrast>();
        contrast.Configure(b,primary?Color.white:Ink,Muted);
        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))label.color=primary?Color.white:Ink;
        foreach(var glyph in root.GetComponentsInChildren<StorybookTitleGlyph>(true))glyph.color=primary?Color.white:Teal;
    }
    public static void Title(Transform root) => WelcomeGlossPresentation.Title(root);
    public static void Return(Transform panel) => WelcomeGlossPresentation.Return(panel);
}
