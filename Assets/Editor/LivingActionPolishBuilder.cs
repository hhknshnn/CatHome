using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Only the requested curtain, shelf and plant alignment.</summary>
public static class LivingActionPolishBuilder
{
    static Transform Find(string name)=>LivingCompositionBuilder.Find(name);
    static Bounds BoundsOf(Transform root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
        if(renderers.Length==0)throw new InvalidOperationException("Missing visual: "+root.name);
        var bounds=renderers[0].bounds;
        foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        return bounds;
    }
    static void Move(Transform t,Vector3 position)
    {
        Undo.RecordObject(t,"Align living room details");t.position=position;
        if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        EditorUtility.SetDirty(t);
    }
    public static string ApplyAndSave()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        CatHomeEditPreview.Clear();
        var window=BoundsOf(Find("Window"));
        var curtains=Find("ShortWindowCurtains");
        // Source cloth hem -0.51, rod centre 0.62. Rod sits above the actual
        // frame, while the cloth clears the sill; both panels stay symmetric.
        float hem=window.min.y+.04f,rod=window.max.y+.045f;
        float height=(rod-hem)/1.13f;
        Undo.RecordObject(curtains,"Fit curtain rod and cloth to measured window");
        curtains.localScale=new Vector3((window.size.z+.14f)/1.724f,height,.72f);
        // The frame bounds include the projecting sill. Fit the cloth close
        // to the frame face instead of placing the entire curtain in front of it.
        Move(curtains,new Vector3(window.center.x-.04f,hem+.51f*height,window.center.z));
        var shelf=Find("TallBookshelf");
        float lift=window.min.y-BoundsOf(shelf).min.y;
        foreach(var t in new[]{shelf,Find("ColorfulBookSet")})
        {
            Move(t,t.position+Vector3.up*lift);
            var entry=t.Find("InteractionAnchor");
            if(entry!=null)Move(entry,new Vector3(entry.position.x,0,entry.position.z));
        }
        var plant=Find("TallHouseplant");var unit=BoundsOf(Find("TvUnit"));
        float shift=unit.max.x-.025f-BoundsOf(plant).max.x;
        Move(plant,plant.position+Vector3.right*shift);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(LivingCompositionBuilder.Room);
        EditorSceneManager.SaveScene(LivingCompositionBuilder.Room);
        return "Shelf bottom="+BoundsOf(shelf).min.y+", window bottom="+window.min.y+
            "; plant front="+BoundsOf(plant).max.x+", unit front="+unit.max.x+
            "; curtain rod="+rod+", hem="+hem;
    }
}
