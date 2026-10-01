using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Applies the approved title UI without rebuilding the room, cats or other screens.</summary>
public static class StorybookTitleBuilder
{
    public const string CatIconPath = "Assets/Art/UI/StorybookTitle/CatHead.png";
    public const string ActionIconPath = "Assets/Art/UI/StorybookTitle/ActionIcons.png";
    private static readonly Color Cream = new Color32(255,246,227,255);
    private static readonly Color Mint = new Color32(139,227,207,255);
    private static readonly Color IndigoTop = new Color32(66,96,156,255);
    private static readonly Color IndigoBottom = new Color32(31,51,99,255);

    [MenuItem("Tools/Cat Home/UI/Apply Approved Main Menu")]
    public static void ApplyToOpenTitle()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play before authoring the title.");
        var title=Object.FindObjectsByType<TitleScreen>(FindObjectsInactive.Include).FirstOrDefault();
        if(title==null)throw new InvalidOperationException("Open the normal CatHome_UI scene first.");
        ConfigureTexture(CatIconPath,512);ConfigureTexture(ActionIconPath,1024);
        var root=title.gameObject;
        if(PrefabUtility.IsPartOfPrefabInstance(root))
            PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        Apply(root.transform);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root,TitleScreenBuilder.PrefabPath,InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
    }

    public static void Apply(Transform root)
    {
        if(root==null||root.GetComponent<TitleScreen>()==null)return;
        var cat=AssetDatabase.LoadAssetAtPath<Texture2D>(CatIconPath);
        var actions=AssetDatabase.LoadAssetAtPath<Texture2D>(ActionIconPath);
        if(cat==null||actions==null)throw new InvalidOperationException("Approved title icon assets are missing.");
        var safe=root.Find("SafeArea");
        var dock=safe.Find("BrandDockLayout") as RectTransform;
        var links=safe.Find("MainMenuShortcuts") as RectTransform;
        var utilities=safe.Find("TitleUtilities") as RectTransform;
        var panel=root.Find("LeftPearlVeil") as RectTransform;
        var veil=panel.GetComponent<PremiumReadingVeil>();if(veil!=null)Object.DestroyImmediate(veil);
        var backdrop=Get<StorybookTitleBackdrop>(panel.gameObject);backdrop.raycastTarget=false;
        panel.anchorMin=new Vector2(0,0);panel.anchorMax=new Vector2(0,1);panel.pivot=new Vector2(0,.5f);
        panel.anchoredPosition=Vector2.zero;panel.sizeDelta=new Vector2(755,0);
        dock.sizeDelta=new Vector2(640,960);
        Text(dock.Find("Wordmark"),66,Cream,new Vector2(66,403),new Vector2(440,90));
        dock.Find("Wordmark").GetComponent<TMP_Text>().text="CAT <color=#8BE3CF>HOME</color>";
        Icon(dock.Find("PawWordmark") as RectTransform,cat,new Rect(0,0,1,1),new Vector2(-227,403),98);
        Text(dock.Find("Greeting"),26,new Color32(168,201,219,255),new Vector2(0,303),new Vector2(560,46),false);
        Text(dock.Find("CatName"),96,Cream,new Vector2(0,160),new Vector2(560,224));
        Text(dock.Find("ExperiencePromise"),29,Cream,new Vector2(0,11),new Vector2(560,58),false);
        var play=Button(dock.Find("PlayButton"),new Vector2(0,-139),new Vector2(600,140),44,true);
        var playLabel=play.transform.Find("Visual/Label") as RectTransform;
        playLabel.offsetMin=new Vector2(115,12);playLabel.offsetMax=new Vector2(-80,-12);
        Icon(play.transform.Find("Visual/PawMedal") as RectTransform,cat,new Rect(0,0,1,1),new Vector2(-212,0),100);
        var arrow=play.transform.Find("Visual/ContinueArrow");
        Text(arrow,48,Cream,new Vector2(232,0),new Vector2(70,72));
        arrow.GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
        var fresh=Button(dock.Find("NewGameButton"),new Vector2(-108,-291),new Vector2(384,92),32,false);
        var newLabel=fresh.transform.Find("Visual/Label") as RectTransform;
        newLabel.offsetMin=new Vector2(65,8);newLabel.offsetMax=new Vector2(-16,-8);
        Glyph(fresh.targetGraphic.transform,"RestartIcon",StorybookTitleGlyph.Symbol.Restart,new Vector2(-145,0),48,Cream);
        var level=dock.Find("HomeLevelPill");At(level as RectTransform,new Vector2(-71,-420),new Vector2(458,86));
        level.GetComponent<LowPolyPanelGraphic>().ConfigureStorybookStyle(new Color32(51,89,127,255),new Color32(27,56,88,255),30);
        level.GetComponent<LowPolyPanelGraphic>().raycastTarget=false;
        Text(level.Find("Label"),28,Cream,new Vector2(28,0),new Vector2(344,55));
        level.Find("Label").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
        Glyph(level,"ProgressIcon",StorybookTitleGlyph.Symbol.Progress,new Vector2(-176,0),42,Mint);
        dock.Find("YourHomeJourney").gameObject.SetActive(false);
        Utility(utilities.Find("SettingsButton"),248,27,StorybookTitleGlyph.Symbol.Settings);
        Utility(utilities.Find("CreditsButton"),276,27,StorybookTitleGlyph.Symbol.Credits);
        Utility(utilities.Find("QuitButton"),140,24,StorybookTitleGlyph.Symbol.Exit);
        links.sizeDelta=new Vector2(700,198);
        Get<CanvasRenderer>(links.gameObject);
        var linkSurface=Get<LowPolyPanelGraphic>(links.gameObject);
        linkSurface.ConfigureStorybookStyle(IndigoTop,IndigoBottom,44);linkSurface.raycastTarget=false;
        Shortcut(links.Find("ShopShortcut"),-222,cat,new Rect(0,0,1,1));
        Shortcut(links.Find("RoomsShortcut"),0,actions,new Rect(.5f,.5f,.5f,.5f));
        Shortcut(links.Find("GamesShortcut"),222,actions,new Rect(.5f,0,.5f,.5f));
        Get<TitleScreenLayout>(safe.gameObject).EditorConfigureStorybook(dock,links,utilities,panel);
    }

    private static void Shortcut(Transform root,float x,Texture2D texture,Rect uv)
    {
        var b=Button(root,new Vector2(x,0),new Vector2(204,162),27,false);
        var face=b.targetGraphic.transform;
        var old=face.Find("ShortcutBackdropArt");if(old!=null)old.gameObject.SetActive(false);
        var preview=face.Find("Preview") as RectTransform;
        var portrait=preview.GetComponent<SelectedCatPortrait>();if(portrait!=null)Object.DestroyImmediate(portrait);
        var sprite=preview.GetComponent<Image>();if(sprite!=null)Object.DestroyImmediate(sprite);
        Icon(preview,texture,uv,new Vector2(0,26),104);
        Text(face.Find("Label"),27,Cream,new Vector2(0,-47),new Vector2(184,40));
        face.Find("Label").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
        Text(face.Find("AfterTourBadge"),14,new Color32(214,233,240,255),new Vector2(0,69),new Vector2(185,20),false);
        face.Find("AfterTourBadge").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
        face.Find("AfterTourBadge").SetAsLastSibling();
    }
    private static void Utility(Transform root,float width,float fontSize,StorybookTitleGlyph.Symbol symbol)
    {
        var b=Button(root,Vector2.zero,new Vector2(width,80),fontSize,false);
        Glyph(b.targetGraphic.transform,"UtilityIcon",symbol,new Vector2(-width*.5f+43,0),42,Cream);
        var label=root.Find("Visual/Label") as RectTransform;
        label.offsetMin=new Vector2(70,8);label.offsetMax=new Vector2(-16,-8);
    }
    private static Button Button(Transform root,Vector2 position,Vector2 size,float fontSize,bool primary)
    {
        At(root as RectTransform,position,size);
        var button=root.GetComponent<Button>();var face=button.targetGraphic as LowPolyPanelGraphic;
        face.rectTransform.anchorMin=Vector2.zero;face.rectTransform.anchorMax=Vector2.one;
        face.rectTransform.offsetMin=face.rectTransform.offsetMax=Vector2.zero;
        face.ConfigureStorybookStyle(primary?new Color32(255,158,122,255):IndigoTop,
            primary?new Color32(237,83,76,255):IndigoBottom,primary?42:30);
        button.transition=Selectable.Transition.None;
        PremiumUiFactory.PolishButton(button,primary,PremiumTypography.Emphasis);
        var label=root.Find("Visual/Label").GetComponent<TMP_Text>();
        Type(label,fontSize,Cream,true);label.alignment=TextAlignmentOptions.Center;
        var rt=label.rectTransform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(16,8);rt.offsetMax=new Vector2(-16,-8);
        return button;
    }
    private static void Text(Transform target,float size,Color tint,Vector2 position,Vector2 dimensions,bool emphasis=true)
    {
        At(target as RectTransform,position,dimensions);var text=target.GetComponent<TMP_Text>();
        Type(text,size,tint,emphasis);text.alignment=TextAlignmentOptions.Left;
    }
    private static void Type(TMP_Text text,float size,Color tint,bool emphasis)
    {
        text.font=emphasis?PremiumTypography.Emphasis:PremiumTypography.Body;
        text.fontSharedMaterial=text.font.material;text.fontStyle=FontStyles.Normal;text.fontWeight=FontWeight.Regular;
        text.color=tint;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=size*.83f;text.fontSizeMax=size;
        text.characterSpacing=0;text.wordSpacing=0;text.lineSpacing=-3;text.extraPadding=true;
        text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Overflow;
        text.enableVertexGradient=false;text.raycastTarget=false;
    }
    private static void Icon(RectTransform rect,Texture2D texture,Rect uv,Vector2 position,float size)
    {
        for(int i=rect.childCount-1;i>=0;i--)Object.DestroyImmediate(rect.GetChild(i).gameObject);
        At(rect,position,new Vector2(size,size));var image=Get<RawImage>(rect.gameObject);
        image.texture=texture;image.uvRect=uv;image.color=Color.white;image.raycastTarget=false;
    }
    private static void Glyph(Transform parent,string name,StorybookTitleGlyph.Symbol symbol,Vector2 position,float size,Color tint)
    {
        var rect=parent.Find(name) as RectTransform;
        if(rect==null){rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);}
        At(rect,position,new Vector2(size,size));Get<StorybookTitleGlyph>(rect.gameObject).Configure(symbol,tint);
    }
    private static void At(RectTransform rect,Vector2 position,Vector2 size)
    {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;}
    private static T Get<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    private static void ConfigureTexture(string path,int maxSize)
    {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException(path);
        importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
        importer.maxTextureSize=maxSize;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
    }
}
