using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Authors the approved home HUD without rebuilding rooms or altering gameplay.</summary>
public static class StorybookHudBuilder
{
    public static readonly Color Cream=new Color32(255,246,227,255);
    public static readonly Color Mint=new Color32(139,227,207,255);
    public static readonly Color Top=new Color32(66,96,156,255);
    public static readonly Color Bottom=new Color32(31,51,99,255);
    public static void ApplyToOpenHud()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play before authoring HUD.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/UI/CatHome_UI.unity");
        if(!scene.IsValid()||!scene.isLoaded)throw new InvalidOperationException("Open the normal UI scene.");
        foreach(var root in scene.GetRootGameObjects().Where(g=>g.name=="Canvas"||g.name=="CurrencyHudCanvas"||g.name=="MainPanelCanvas"))
        {
            string prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            Apply(root.transform);
            if(!string.IsNullOrEmpty(prefab))PrefabUtility.SaveAsPrefabAssetAndConnect(root,prefab,InteractionMode.AutomatedAction);
        }
        // The bootstrap contains a second instance of the same menu prefab; keep its HUD appearance consistent.
        var boot=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/GameScene.unity");
        foreach(var root in boot.GetRootGameObjects().Where(g=>g.GetComponent<MainPanelController>()!=null))
        {Apply(root.transform);foreach(var c in root.GetComponentsInChildren<Component>(true))if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        EditorSceneManager.MarkSceneDirty(boot);EditorSceneManager.SaveScene(boot);
    }
    public static void Apply(Transform root)
    {
        if(root==null)return;
        if(root.GetComponent<CurrencyHudController>()!=null)Wallet(root);
        if(root.GetComponent<MainPanelController>()!=null)Identity(root);
        if(root.name=="Canvas"&&root.Find("FoodBar")!=null)Needs(root);
        StorybookHudBottomBuilder.Apply(root);
    }
    private static void Needs(Transform root)
    {
        foreach(string name in new[]{"FoodBar","ThirstUI","EnergyUI"})
        {
            var need=root.Find(name);var background=need.Find("Background").GetComponent<LowPolyPanelGraphic>();
            Surface(background,32);background.enabled=false;
            var well=need.Find("Background/NeedArtWell");if(well!=null)well.gameObject.SetActive(false);
            foreach(var text in need.GetComponentsInChildren<TMP_Text>(true))
            {if(text.name.EndsWith("Shadow")){text.gameObject.SetActive(false);continue;}Type(text,text.name=="PercentageText"?24:27);}
            var track=need.Find("FillTrackMask").GetComponent<LowPolyPanelGraphic>();
            track.ConfigureModernStyle(new Color32(22,39,76,255),new Color32(33,56,92,255),9);track.raycastTarget=false;
            foreach(var art in need.GetComponentsInChildren<RawImage>(true))art.raycastTarget=false;
        }
        var food=root.Find("FoodBar") as RectTransform;
        var band=food.Find("StorybookNeedsBand") as RectTransform;
        if(band==null){band=new GameObject("StorybookNeedsBand",typeof(RectTransform),typeof(CanvasRenderer),typeof(LowPolyPanelGraphic)).GetComponent<RectTransform>();band.SetParent(food,false);}
        At(band,new Vector2(122,0),new Vector2(1088,StorybookHudLayout.TopPanelHeight));Surface(band.GetComponent<LowPolyPanelGraphic>(),32);band.SetAsFirstSibling();
        Get<StorybookHudLayout>(root.gameObject).Configure(food,root.Find("ThirstUI") as RectTransform,root.Find("EnergyUI") as RectTransform);
    }
    private static void Identity(Transform root)
    {
        var identity=root.Find("SafeArea/CatShopButton");if(identity==null)return;
        var identityRect=identity as RectTransform;identityRect.anchorMin=identityRect.anchorMax=new Vector2(0,1);
        var face=identity.Find("Face").GetComponent<LowPolyPanelGraphic>();Surface(face,32);
        // Identity is part of the common plaque; its transparent target retains the existing input hit area.
        face.ConfigureModernStyle(Color.clear,Color.clear,0);
        face.raycastTarget=true;
        var portrait=identity.Find("Face/PortraitMedallion").GetComponent<LowPolyPanelGraphic>();
        portrait.ConfigureStorybookStyle(Mint,new Color32(58,160,167,255),48);
        At(portrait.rectTransform,new Vector2(-85,0),new Vector2(72,72));
        At(identity.Find("Face/SelectedCatFace") as RectTransform,new Vector2(-85,0),new Vector2(64,64));
        Type(identity.Find("Face/CatName").GetComponent<TMP_Text>(),29);
        var level=identity.Find("Face/HomeLevelText").GetComponent<TMP_Text>();Type(level,23);level.color=Mint;
        var menu=root.Find("SafeArea/MenuButton").GetComponent<Button>();Surface(menu.targetGraphic as LowPolyPanelGraphic,26);
        var menuRect=menu.transform as RectTransform;menuRect.anchorMin=menuRect.anchorMax=Vector2.one;
        menu.targetGraphic.raycastTarget=true;
        foreach(var bar in menu.GetComponentsInChildren<Image>(true))bar.color=Cream;
        PremiumUiFactory.PolishButton(menu,false,PremiumTypography.Emphasis);
    }
    private static void Wallet(Transform root)
    {
        var controller=root.GetComponent<CurrencyHudController>();var serialized=new SerializedObject(controller);
        serialized.FindProperty("avoidNeedsHudOverlap").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
        var group=root.Find("SafeArea/Group") as RectTransform;group.localScale=Vector3.one;
        foreach(var entry in root.GetComponentsInChildren<CurrencyEntryLayout>(true))
        {
            var r=entry.transform;Surface(r.Find("Face").GetComponent<LowPolyPanelGraphic>(),24);
            var text=r.Find("Value").GetComponent<TMP_Text>();Type(text,27);
            var plus=r.Find("PlusButton").GetComponent<Button>();
            var face=plus.targetGraphic as LowPolyPanelGraphic;
            if(face!=null)face.ConfigureStorybookStyle(Mint,new Color32(40,169,166,255),15);
            foreach(var glyph in plus.GetComponentsInChildren<Graphic>(true))if(glyph!=face)glyph.color=Cream;
            PremiumUiFactory.PolishButton(plus,false,PremiumTypography.Emphasis);
            var icon=r.Find("Icon") as RectTransform;
            if(r.name=="CoinEntry")
            {
                for(int i=icon.childCount-1;i>=0;i--)Object.DestroyImmediate(icon.GetChild(i).gameObject);
                var raw=Get<RawImage>(icon.gameObject);raw.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(StorybookTitleBuilder.CatIconPath);raw.color=Color.white;raw.raycastTarget=false;
            }
            bool narrow=r.name=="DiamondEntry";
            entry.Configure(icon,text,plus.transform as RectTransform,44,6,8,48,18,narrow?40:78,18,27);
        }
    }
    public static void Surface(LowPolyPanelGraphic face,float radius=26)
    {if(face==null)return;face.ConfigureStorybookStyle(Top,Bottom,radius);face.raycastTarget=false;}
    public static void Type(TMP_Text text,float size)
    {
        text.font=PremiumTypography.Emphasis;text.fontSharedMaterial=text.font.material;text.fontSize=size;
        text.enableAutoSizing=true;text.fontSizeMin=size*.8f;text.fontSizeMax=size;text.color=Cream;
        text.fontStyle=FontStyles.Normal;text.fontWeight=FontWeight.Regular;text.enableVertexGradient=false;text.raycastTarget=false;
    }
    private static void At(RectTransform r,Vector2 pos,Vector2 size)
    {r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;r.localScale=Vector3.one;}
    private static T Get<T>(GameObject go)where T:Component{var c=go.GetComponent<T>();return c==null?go.AddComponent<T>():c;}
}
