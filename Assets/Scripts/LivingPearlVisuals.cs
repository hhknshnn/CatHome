using UnityEngine;
using UnityEngine.UI;

public static class LivingPearlVisuals
{
    static Texture2D atlas;
    static readonly Sprite[] sprites=new Sprite[4];
    // Bounds of the four original ImageGen assets; pixels are kept unchanged.
    public static Sprite Sprite(int index)
    {
        if(sprites[index]!=null)return sprites[index];
        if(atlas==null)atlas=Resources.Load<Texture2D>("LivingRoom/PearlAtlas");
        if(atlas==null)return null;
        Rect[] rects={new Rect(44,566,774,299),new Rect(876,535,622,423),new Rect(235,80,370,376),new Rect(951,76,400,368)};
        sprites[index]=UnityEngine.Sprite.Create(atlas,rects[index],new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,index==1?new Vector4(110,100,110,100):Vector4.zero);
        sprites[index].name="LivingPearl"+index;return sprites[index];
    }
    public static Image Image(Transform parent,string name,int sprite)
    {
        var existing=parent.Find(name);
        var result=existing!=null?existing.GetComponent<Image>():null;
        if(result==null){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);result=go.GetComponent<Image>();}
        result.sprite=Sprite(sprite);result.color=Color.white;result.raycastTarget=false;
        return result;
    }
    public static void Stretch(RectTransform rect,Vector2 padding)
    {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=-padding;rect.offsetMax=padding;rect.localScale=Vector3.one;}
}
