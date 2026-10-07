using UnityEngine;

/// <summary>One responsive layout owner for the revised title, within the existing safe area.</summary>
public sealed class WelcomeGlossTitleLayout : MonoBehaviour
{
    Vector2 lastSize; bool lastQuit,lastLocked;
    void LateUpdate()=>Apply(false);
    public void Apply(bool force)
    {
        var safe=transform as RectTransform;var dock=transform.Find("BrandDockLayout") as RectTransform;
        var links=transform.Find("MainMenuShortcuts") as RectTransform;var utilities=transform.Find("TitleUtilities") as RectTransform;
        if(dock==null||links==null||utilities==null)return;
        var quit=utilities.Find("QuitButton");bool hasQuit=quit!=null&&quit.gameObject.activeSelf;
        var badge=links.Find("ShopShortcut/Visual/AfterTourBadge");bool locked=badge!=null&&badge.gameObject.activeSelf;
        if(!force&&lastSize==safe.rect.size&&hasQuit==lastQuit&&locked==lastLocked)return;
        lastSize=safe.rect.size;lastQuit=hasQuit;lastLocked=locked;
        float scale=Mathf.Min(1,safe.rect.height/1080f,safe.rect.width/1660f);
        dock.anchorMin=dock.anchorMax=new Vector2(0,.5f);dock.sizeDelta=new Vector2(640,960);
        dock.anchoredPosition=new Vector2(355*scale,0);dock.localScale=Vector3.one*scale;
        links.anchorMin=links.anchorMax=new Vector2(0,.5f);links.sizeDelta=new Vector2(568,180);
        links.anchoredPosition=new Vector2(355*scale,-327*scale);links.localScale=Vector3.one*scale;
        foreach(string name in new[]{"ShopShortcut","RoomsShortcut","GamesShortcut"})
        {
            var p=links.Find(name+"/Visual/Preview");WelcomeGlossPresentation.At(p,new Vector2(0,locked?6:17),Vector2.one*(locked?54:86));
            var selected=p.Find("SelectedBreed");if(selected!=null)WelcomeGlossPresentation.At(selected,Vector2.zero,Vector2.one*(locked?54:86));
            WelcomeGlossPresentation.At(links.Find(name+"/Visual/AfterTourBadge"),new Vector2(0,55),new Vector2(154,21));
        }
        utilities.anchorMin=utilities.anchorMax=Vector2.one;utilities.sizeDelta=new Vector2(hasQuit?244:158,70);
        utilities.anchoredPosition=new Vector2(-(hasQuit?160:117)*scale,-64*scale);utilities.localScale=Vector3.one*scale;
        int i=0;foreach(Transform t in utilities){if(!t.gameObject.activeSelf)continue;WelcomeGlossPresentation.At(t,new Vector2((i++-(hasQuit?1:.5f))*84,0),Vector2.one*70);}
        var veil=transform.parent.Find("LeftPearlVeil") as RectTransform;
        if(veil!=null)veil.sizeDelta=new Vector2(880*scale,0);
    }
}
