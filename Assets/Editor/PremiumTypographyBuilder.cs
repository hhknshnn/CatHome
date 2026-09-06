using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.SceneManagement;

public static class PremiumTypographyBuilder
{
    public static void BuildFonts()
    {
        Directory.CreateDirectory("Assets/Resources/Typography");
        BuildFont("Assets/Fonts/Fredoka-Medium.ttf", "FredokaDisplay");
        BuildFont("Assets/Fonts/NunitoSans-SemiBold.ttf", "NunitoBody");
        BuildFont("Assets/Fonts/Fredoka-SemiBold.ttf", "FredokaEmphasis");
        AssetDatabase.SaveAssets();
    }
    static void BuildFont(string source, string name)
    {
        string path = "Assets/Resources/Typography/"+name+".asset";
        if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path)!=null) return;
        var font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(source),90,10,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);
        font.name=name; font.normalStyle=0; font.boldStyle=0;
        string chars="";
        for(int code=32; code<=383; code++)chars+=(char)code;
        chars+="ĞğİıŞş₺←↑→↓✓×•…–—’“”";
        font.TryAddCharacters(chars, out string missing);
        font.material.SetFloat("_FaceDilate",0); font.material.SetFloat("_OutlineWidth",0);
        AssetDatabase.CreateAsset(font,path);
        font.material.name=name+" Material"; AssetDatabase.AddObjectToAsset(font.material,font);
        foreach(var atlas in font.atlasTextures){atlas.name=name+" Atlas";AssetDatabase.AddObjectToAsset(atlas,font);}
        EditorUtility.SetDirty(font);
    }
    public static string ApplyToAllScreens()
    {
        BuildFonts(); int labels=0;
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/UI"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var root=PrefabUtility.LoadPrefabContents(path);
            try{PremiumUiFactory.PolishHierarchy(root.transform,PremiumTypography.Display);labels+=root.GetComponentsInChildren<TMP_Text>(true).Length;PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(string path in new[]{"Assets/Scenes/UI/CatHome_UI.unity","Assets/Scenes/Runner/CatRunner.unity","Assets/Scenes/Catch/CatCatch.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            foreach(var root in scene.GetRootGameObjects()){PremiumUiFactory.PolishHierarchy(root.transform,PremiumTypography.Display);labels+=root.GetComponentsInChildren<TMP_Text>(true).Length;}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        AssetDatabase.SaveAssets();return labels+" text elements refined, shared prefab and scene styles saved.";
    }
}
