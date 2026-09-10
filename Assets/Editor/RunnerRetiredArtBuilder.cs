using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Remove replaced decorations, never the selectable variant roots or pooled templates.</summary>
public static class RunnerRetiredArtBuilder
{
    public static int Clean(Transform root)
    {
        int removed=0;
        foreach(var segment in root.GetComponentsInChildren<CatRunnerScenerySegment>(true))
        {
            var near=segment.transform.Find("NearScenery");
            if(near!=null && near.Cast<Transform>().Any(t=>t.name.StartsWith("BoulevardFacade")&&t.gameObject.activeSelf))
                removed+=RemoveReplacedChildren(near);
            foreach(string name in CatRunnerContentBuilder.SceneryVariantNames)
            {
                var variant=segment.transform.Find(name);
                if(variant!=null&&variant.Cast<Transform>().Any(t=>t.name.StartsWith("BoulevardLandmark")&&t.gameObject.activeSelf))
                    removed+=RemoveReplacedChildren(variant);
            }
            var settings=new SerializedObject(segment);
            foreach(string name in new[]{"floaters","spinners"})
            {
                var list=settings.FindProperty(name);
                var live=Enumerable.Range(0,list.arraySize).Select(i=>list.GetArrayElementAtIndex(i).objectReferenceValue).Where(o=>o!=null).ToArray();
                list.arraySize=live.Length;
                for(int i=0;i<live.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=live[i];
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var item in root.GetComponentsInChildren<CatRunnerTrackObject>(true))
        {
            // A template itself is inactive by design; examine its children's activeSelf.
            if(item.transform.Cast<Transform>().Any(t=>t.gameObject.activeSelf&&t.name.StartsWith("Runner")))
                removed+=RemoveReplacedChildren(item.transform);
        }
        return removed;
    }

    private static int RemoveReplacedChildren(Transform parent)
    {
        int count=0;
        foreach(var child in parent.Cast<Transform>().ToArray())
        {
            if(child.gameObject.activeSelf || child.name.Contains("Telegraph") || child.name.Contains("Warning"))continue;
            Object.DestroyImmediate(child.gameObject);count++;
        }
        return count;
    }
}
