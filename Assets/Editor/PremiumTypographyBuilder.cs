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
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(existing!=null){BakeCharacters(existing);return;}
        var font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(source),90,10,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);
        font.name=name; font.normalStyle=0; font.boldStyle=0;
        BakeCharacters(font);
        font.material.SetFloat("_FaceDilate",0); font.material.SetFloat("_OutlineWidth",0);
        AssetDatabase.CreateAsset(font,path);
        font.material.name=name+" Material"; AssetDatabase.AddObjectToAsset(font.material,font);
        foreach(var atlas in font.atlasTextures){atlas.name=name+" Atlas";AssetDatabase.AddObjectToAsset(atlas,font);}
        EditorUtility.SetDirty(font);
    }
    static void BakeCharacters(TMP_FontAsset font)
    {
        string chars="";
        for(int code=32;code<=383;code++)chars+=(char)code;
        chars+="ĞğİıŞş₺←↑→↓✓×•…–—’“”";
        font.TryAddCharacters(chars,out string missing);
        // These are pre-baked UI atlases. Build cleanup must not erase Turkish glyphs.
        var serialized=new SerializedObject(font);
        serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue=false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(font);
        foreach(var atlas in font.atlasTextures)EditorUtility.SetDirty(atlas);
    }
    public static string ApplyToAllScreens()
    {
        BuildFonts(); int labels=0;
        var originalScene=SceneManager.GetActiveScene();
        try
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/UI"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var root=PrefabUtility.LoadPrefabContents(path);
                try{PremiumUiFactory.PolishHierarchy(root.transform,PremiumTypography.Display);labels+=root.GetComponentsInChildren<TMP_Text>(true).Length;PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(string path in new[]{"Assets/Scenes/UI/CatHome_UI.unity","Assets/Scenes/Runner/CatRunner.unity","Assets/Scenes/Catch/CatCatch.unity"})
                labels+=PolishScene(path,false);
            // Tutorial and celebration overlays are authored in room scenes.
            // Restrict this coverage to Canvas hierarchies, leaving world roots alone.
            foreach(var room in HomeRoomService.Rooms)
                labels+=PolishScene(room.ScenePath,true);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if(originalScene.IsValid()&&originalScene.isLoaded)SceneManager.SetActiveScene(originalScene);
        }
        return labels+" text elements refined, shared prefab and scene styles saved.";
    }

    private static int PolishScene(string path,bool canvasOnly)
    {
        var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            int labels=0;
            foreach(var root in scene.GetRootGameObjects())
            {
                if(!canvasOnly){labels+=PolishRoot(root.transform);continue;}
                foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    // A nested Canvas is already traversed with its outer Canvas.
                    if(HasCanvasAncestor(canvas.transform))continue;
                    labels+=PolishRoot(canvas.transform);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return labels;
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }

    private static int PolishRoot(Transform root)
    {
        PremiumUiFactory.PolishHierarchy(root,PremiumTypography.Display);
        return root.GetComponentsInChildren<TMP_Text>(true).Length;
    }

    private static bool HasCanvasAncestor(Transform root)
    {
        for(var parent=root.parent;parent!=null;parent=parent.parent)
            if(parent.GetComponent<Canvas>()!=null)return true;
        return false;
    }
}
