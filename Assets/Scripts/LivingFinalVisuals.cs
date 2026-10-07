using UnityEngine;
using UnityEngine.UI;

// Separate resource: popup/HUD/previous shared atlas remain unchanged.
public static class LivingFinalVisuals
{
    static Texture2D atlas;
    static readonly Sprite[] sprites=new Sprite[4];
    public static Sprite Get(int index)
    {
        if(sprites[index]!=null)return sprites[index];
        if(atlas==null)atlas=Resources.Load<Texture2D>("LivingRoom/FinalVisuals");
        if(atlas==null)return null;
        Rect[] rects={new Rect(22,207,980,287),new Rect(120,25,150,157),new Rect(327,57,99,100),new Rect(475,66,75,76)};
        sprites[index]=Sprite.Create(atlas,rects[index],new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        sprites[index].name="LivingFinal"+index;return sprites[index];
    }
    public static Image Image(Transform parent,string name,int index)
    {
        var child=parent.Find(name);var image=child!=null?child.GetComponent<Image>():null;
        if(image==null){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);image=go.GetComponent<Image>();}
        image.sprite=Get(index);image.color=Color.white;image.raycastTarget=false;return image;
    }
}
