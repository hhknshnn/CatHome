using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CompanionAnimationBuilder
{
    public const string Folder="Assets/Art/Cat/Polyperfect/CompanionAnimations";
    public static void Build()
    {
        System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(PolyperfectCatIntegrationBuilder.ControllerPath);
        var sources=AssetDatabase.LoadAllAssetsAtPath(PolyperfectCatIntegrationBuilder.SourceModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PolyperfectCatIntegrationBuilder.SourcePrefabPath));
        try
        {
            var bones=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("DEF-")).ToArray();
            foreach(string name in new[]{"Loaf","Meow","SitDown","StandUp"})
            {
                string source=name=="SitDown"?"Idle_to_Sit":name=="StandUp"?"Sitting_to_Idle":name=="Loaf"?"Sleeping":"Idle";
                var original=sources.First(c=>c.name.EndsWith("|"+source,StringComparison.Ordinal));
                float length=name=="Meow"?3f:name=="Loaf"?3f:.7f;
                var clip=new AnimationClip{name="Companion"+name,frameRate=60};
                var curves=bones.Select(_=>Enumerable.Range(0,7).Select(i=>new AnimationCurve()).ToArray()).ToArray();
                for(int f=0;f<=180;f++)
                {
                    float t=f/180f;original.SampleAnimation(root,t*original.length);
                    if(name=="Loaf")
                    {
                        // Sleeping already tucks the real forelegs beneath an upright body.
                        // Keep that joint chain and lift the attentive head for a loaf.
                        var head=bones.First(b=>b.name=="DEF-spine.006");
                        head.rotation=Quaternion.AngleAxis(-9f,root.transform.right)*head.rotation;
                        head.position+=root.transform.up*.006f;
                    }
                    else if(name=="Meow")
                    {
                        float call=Mathf.Sin(Mathf.Clamp01(t/.7f)*Mathf.PI);
                        var head=bones.First(b=>b.name=="DEF-spine.006");head.rotation=Quaternion.AngleAxis(-7*call,root.transform.right)*head.rotation;
                        var jaw=bones.FirstOrDefault(b=>b.name.ToLowerInvariant().Contains("jaw"));if(jaw!=null)jaw.localRotation*=Quaternion.Euler(10*call,0,0);
                    }
                    for(int b=0;b<bones.Length;b++)
                    {var p=bones[b].localPosition;var q=bones[b].localRotation;float[] v={q.x,q.y,q.z,q.w,p.x,p.y,p.z};for(int j=0;j<7;j++)curves[b][j].AddKey(t*length,v[j]);}
                }
                string[] properties={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w","m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z"};
                for(int b=0;b<bones.Length;b++)for(int j=0;j<7;j++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(bones[b],root.transform),typeof(Transform),properties[j]),curves[b][j]);
                clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=name=="Loaf";AnimationUtility.SetAnimationClipSettings(clip,settings);
                string path=Folder+"/"+clip.name+".anim";var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(saved==null){AssetDatabase.CreateAsset(clip,path);saved=clip;}else{EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);}
                var machine=controller.layers[0].stateMachine;var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==saved.name)??machine.AddState(saved.name);
                state.motion=saved;state.speed=1;state.writeDefaultValues=true;
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
