using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Project-owned clips on the shared Polyperfect skeleton, never the old PolyOne rig.</summary>
public static class CatToyAnimationBuilder
{
    public const string Folder="Assets/Art/Cat/Polyperfect/ToyAnimations";
    public static void Build()
    {
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art/Cat/Polyperfect","ToyAnimations");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(PolyperfectCatIntegrationBuilder.ControllerPath);
        var clips=AssetDatabase.LoadAllAssetsAtPath(PolyperfectCatIntegrationBuilder.SourceModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PolyperfectCatIntegrationBuilder.SourcePrefabPath));
        try
        {
            var bones=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("DEF-")).ToArray();
            foreach(string name in new[]{"Stalk","Pounce","Sniff","BatLeft","BatRight","Push","Tug","Stretch"})
            {
                string source=name=="Pounce"?"Jump":name=="Stalk"?"Walk":name=="Stretch"?"Stretch":"Idle";
                var original=clips.First(c=>c.name.EndsWith("|"+source,StringComparison.Ordinal));
                var clip=new AnimationClip {name="Toy"+name,frameRate=30};
                var curves=bones.Select(_=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()}).ToArray();
                const int steps=36;
                for(int i=0;i<=steps;i++)
                {
                    float t=i/(float)steps;
                    original.SampleAnimation(root,source=="Idle"?t*1.2f:t*original.length);
                    float envelope=Mathf.Sin(t*Mathf.PI);
                    Transform spine=bones.First(b=>b.name=="DEF-spine"), head=bones.First(b=>b.name=="DEF-spine.006");
                    if(name=="Stalk") spine.position-=root.transform.up*.025f;
                    if(name=="Sniff"||name=="Tug")
                        head.rotation=Quaternion.AngleAxis(22f*envelope,root.transform.right)*head.rotation;
                    if(name=="BatLeft"||name=="BatRight"||name=="Push"||name=="Tug")
                    {
                        float reach=Mathf.SmoothStep(0,1,t<.48f?t/.48f:(1-t)/.52f);
                        foreach(string side in new[]{"L","R"})
                        {
                            if(name=="BatLeft"&&side=="R"||name=="BatRight"&&side=="L")continue;
                            var arm=bones.First(b=>b.name=="DEF-upper_arm."+side);
                            var fore=bones.First(b=>b.name=="DEF-forearm."+side);
                            arm.rotation=Quaternion.AngleAxis(-42*reach,root.transform.right)*arm.rotation;
                            fore.rotation=Quaternion.AngleAxis(28*reach,root.transform.right)*fore.rotation;
                        }
                    }
                    for(int b=0;b<bones.Length;b++)
                    {
                        var q=bones[b].localRotation;var p=bones[b].localPosition;
                        float[] values={q.x,q.y,q.z,q.w,p.x,p.y,p.z};
                        for(int c=0;c<7;c++)curves[b][c].AddKey(t,values[c]);
                    }
                }
                string[] properties={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w","m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z"};
                for(int b=0;b<bones.Length;b++)for(int c=0;c<7;c++)
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(bones[b],root.transform),typeof(Transform),properties[c]),curves[b][c]);
                clip.EnsureQuaternionContinuity();
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
                string path=Folder+"/Toy"+name+".anim";var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(saved==null){AssetDatabase.CreateAsset(clip,path);saved=clip;}else{EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);EditorUtility.SetDirty(saved);}
                var machine=controller.layers[0].stateMachine;var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Toy"+name)??machine.AddState("Toy"+name);
                state.motion=saved;state.speed=1;state.writeDefaultValues=true;
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
