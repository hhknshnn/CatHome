using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MiniGameAnimationBuilder
{
    public const string Folder="Assets/Art/Cat/Polyperfect/MiniGameAnimations";
    public static void Build()
    {
        System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(PolyperfectCatIntegrationBuilder.ControllerPath);
        var sources=AssetDatabase.LoadAllAssetsAtPath(PolyperfectCatIntegrationBuilder.SourceModelPath).OfType<AnimationClip>().ToArray();
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PolyperfectCatIntegrationBuilder.SourcePrefabPath));
        try
        {
            var bones=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("DEF-")).ToArray();
            var spine=bones.First(t=>t.name=="DEF-spine");
            var idle=sources.First(c=>c.name.EndsWith("|Idle",StringComparison.Ordinal));
            idle.SampleAnimation(root,0);
            var neutralPaws=bones.Where(IsPaw).ToDictionary(b=>b.name,b=>b.position);
            var sourceAnimator=root.GetComponentInChildren<Animator>();if(sourceAnimator!=null)sourceAnimator.enabled=false;
            foreach(string name in new[]{"Jump","Duck","Pounce"})
            {
                var original=sources.First(c=>c.name.EndsWith("|"+(name=="Duck"?"Walk":"Jump"),StringComparison.Ordinal));
                var clip=new AnimationClip{name="MiniGame"+name,frameRate=60};
                var curves=bones.Select(_=>Enumerable.Range(0,7).Select(i=>new AnimationCurve()).ToArray()).ToArray();
                for(int frame=0;frame<=60;frame++)
                {
                    float t=frame/60f;
                    original.SampleAnimation(root,(name=="Duck"?t:Mathf.Lerp(.16f,.87f,t))*original.length);
                    if(name=="Duck")
                    {
                        // A crouch is a short, low walking step. Keep the vendor spine,
                        // shoulder and head relationships; only the legs fold farther.
                        // The old .13m drop plus reversed IK poles inverted the elbows
                        // and knees, while uncorrected paw rotations rolled the toes up.
                        var footTargets=bones.Where(IsPaw).ToDictionary(b=>b.name,b=>
                        {
                            Vector3 neutral=neutralPaws[b.name];
                            Vector3 step=b.position-neutral;
                            return neutral+Vector3.ProjectOnPlane(step,root.transform.up)*.72f+
                                root.transform.up*Vector3.Dot(step,root.transform.up)*.42f;
                        });
                        var pawRotations=bones.Where(IsPaw).ToDictionary(b=>b.name,b=>b.rotation);
                        spine.position-=root.transform.up*.075f;
                        foreach(string side in new[]{"L","R"})
                        {
                            // Cat elbows point back; hind knees point forward. These
                            // anatomical bend planes must never swap during a stride.
                            Solve(bones,"DEF-upper_arm."+side,"DEF-forearm."+side,"DEF-hand."+side,footTargets,-root.transform.forward);
                            Solve(bones,"DEF-thigh."+side,"DEF-shin."+side,"DEF-foot."+side,footTargets,root.transform.forward);
                        }
                        foreach(var paw in bones.Where(IsPaw))paw.rotation=pawRotations[paw.name];
                        ShapeCrouchTail(bones,root.transform,t);
                    }
                    // The body clip must never contribute a second jump: translation belongs to gameplay.
                    float low=float.PositiveInfinity;
                    foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var mesh=new Mesh();skin.BakeMesh(mesh,true);
                        foreach(var v in mesh.vertices)low=Mathf.Min(low,root.transform.InverseTransformPoint(skin.transform.TransformPoint(v)).y);
                        UnityEngine.Object.DestroyImmediate(mesh);
                    }
                    if(!float.IsInfinity(low))spine.position-=root.transform.up*low;
                    for(int b=0;b<bones.Length;b++)
                    {
                        var p=bones[b].localPosition;var q=bones[b].localRotation;float[] v={q.x,q.y,q.z,q.w,p.x,p.y,p.z};
                        for(int j=0;j<7;j++)curves[b][j].AddKey(t,v[j]);
                    }
                }
                string[] properties={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w","m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z"};
                for(int b=0;b<bones.Length;b++)for(int j=0;j<7;j++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(bones[b],root.transform),typeof(Transform),properties[j]),curves[b][j]);
                clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=name=="Duck";AnimationUtility.SetAnimationClipSettings(clip,settings);
                string path=Folder+"/"+clip.name+".anim";var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(saved==null){AssetDatabase.CreateAsset(clip,path);saved=clip;}else{EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);EditorUtility.SetDirty(saved);}
                var machine=controller.layers[0].stateMachine;var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==saved.name)??machine.AddState(saved.name);
                state.motion=saved;state.speed=1;state.writeDefaultValues=true;
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
            BuildBreedContacts();
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    private static void BuildBreedContacts()
    {
        const string folder="Assets/Resources/MiniGameAnimations";
        System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var catalog=CatBreedCatalog.Load();
        foreach(var entry in catalog.Entries)
        {
            var visual=CatBreedVisualFactory.Create(entry,catalog.GameplayController,null);
            try
            {
                var animator=visual.GetComponentInChildren<Animator>();animator.enabled=false;
                var spine=CatBreedVisualFactory.FindDescendant(visual.transform,"DEF-spine");
                var mesh=new Mesh();
                foreach(string kind in new[]{"Duck","Jump","Pounce"})
                {
                    var source=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/MiniGame"+kind+".anim");
                    var clip=UnityEngine.Object.Instantiate(source);clip.name=entry.Id+"_"+kind;
                    var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
                    // Joint interpolation can dip between the 60 authored keys.
                    // Measure contact at quarter frames, not only on the keys.
                    for(int frame=0;frame<=240;frame++)
                    {
                        float t=frame/240f;source.SampleAnimation(animator.gameObject,t);float low=float.PositiveInfinity;
                        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {skin.BakeMesh(mesh,true);foreach(var v in mesh.vertices)low=Mathf.Min(low,skin.transform.TransformPoint(v).y);}
                        spine.position+=Vector3.up*(.012f-low);
                        var p=spine.localPosition;curves[0].AddKey(t,p.x);curves[1].AddKey(t,p.y);curves[2].AddKey(t,p.z);
                    }
                    string path=AnimationUtility.CalculateTransformPath(spine,animator.transform);
                    for(int i=0;i<3;i++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalPosition."+"xyz"[i]),curves[i]);
                    // Unity rebuilds the clip's rotation sampling after inserting the
                    // dense translation track. Calibrate against that FINAL clip.
                    var finalCurves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
                    for(int frame=0;frame<=240;frame++)
                    {
                        float t=frame/240f;clip.SampleAnimation(animator.gameObject,t);float low=float.PositiveInfinity;
                        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {skin.BakeMesh(mesh,true);foreach(var v in mesh.vertices)low=Mathf.Min(low,skin.transform.TransformPoint(v).y);}
                        spine.position+=Vector3.up*(.012f-low);
                        var p=spine.localPosition;finalCurves[0].AddKey(t,p.x);finalCurves[1].AddKey(t,p.y);finalCurves[2].AddKey(t,p.z);
                    }
                    for(int i=0;i<3;i++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalPosition."+"xyz"[i]),finalCurves[i]);
                    string asset=folder+"/"+clip.name+".anim";var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(asset);
                    if(saved==null)AssetDatabase.CreateAsset(clip,asset);else{EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);EditorUtility.SetDirty(saved);}
                }
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            finally{UnityEngine.Object.DestroyImmediate(visual);}
        }
        AssetDatabase.SaveAssets();
    }
    private static bool IsPaw(Transform bone)
    {
        return bone.name=="DEF-hand.L"||bone.name=="DEF-hand.R"||
               bone.name=="DEF-foot.L"||bone.name=="DEF-foot.R";
    }

    private static void ShapeCrouchTail(Transform[] bones,Transform root,float phase)
    {
        var tail=bones.Where(b=>b.name.StartsWith("DEF-tail",StringComparison.Ordinal)).ToArray();
        for(int i=0;i<tail.Length;i++)
        {
            var segment=tail[i];
            Transform next=null;
            foreach(Transform child in segment)
                if(child.name.StartsWith("DEF-tail",StringComparison.Ordinal)){next=child;break;}
            if(next==null)
            {
                // Keep a soft terminal hook instead of the old perfectly rigid rod.
                segment.localRotation=Quaternion.Slerp(segment.localRotation,Quaternion.identity,.8f);
                continue;
            }
            float u=i/(float)Mathf.Max(1,tail.Length-2);
            Vector3 desired=-root.forward+root.up*Mathf.Lerp(-.24f,.16f,u)+
                root.right*(.07f*Mathf.Sin(phase*Mathf.PI*2-i*.55f));
            segment.rotation=Quaternion.FromToRotation(next.position-segment.position,desired)*segment.rotation;
        }
    }

    private static void Solve(Transform[] bones,string a,string b,string c,System.Collections.Generic.Dictionary<string,Vector3> targets,Vector3 pole)
    {
        var upper=bones.FirstOrDefault(t=>t.name==a);var lower=bones.FirstOrDefault(t=>t.name==b);var tip=bones.FirstOrDefault(t=>t.name==c);
        if(upper==null||lower==null||tip==null||!targets.ContainsKey(c))return;
        var target=targets[c];float l1=Vector3.Distance(upper.position,lower.position),l2=Vector3.Distance(lower.position,tip.position);
        Vector3 dir=target-upper.position;float distance=Mathf.Clamp(dir.magnitude,Mathf.Abs(l1-l2)+.002f,l1+l2-.001f);dir.Normalize();
        float along=(l1*l1-l2*l2+distance*distance)/(2*distance);
        Vector3 bend=Vector3.ProjectOnPlane(pole,dir).normalized;
        Vector3 elbow=upper.position+dir*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(tip.position-lower.position,target-lower.position)*lower.rotation;
    }
}
