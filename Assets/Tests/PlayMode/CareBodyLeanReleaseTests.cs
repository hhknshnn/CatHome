#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Actual complete gameplay: no source sampling, manual pose application or
// production contact/reward writes. The common care component owns every frame.
public sealed class CareBodyLeanReleaseTests
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    CareAlignmentPolishTests fixture;bool prepared;
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();QaExactMeshContact exact;
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
    static T Read<T>(object value,string name)=>(T)value.GetType().GetField(name,Fields).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name,Fields).Invoke(value,args);
    [SetUp]public void Before(){fixture=new CareAlignmentPolishTests();fixture.Before();prepared=true;exact=new QaExactMeshContact(topology);}
    [TearDown]public void After(){Time.timeScale=1;exact?.Clear();topology.Clear();if(prepared)fixture.After();prepared=false;}

    [UnityTest,Timeout(180000)]
    public IEnumerator PersianKitchen_ActualMeal_ContactCompletionSkinPauseAndCancel()
    {
        var frames=new List<string>{"run,frame,eating,lean,weight,actualMouth,minimumMouth,pawError,minimumReachReserve,rootDrift,yawDrift,maxSkinDepth,minSkinY,skinVertex,skinBone,skinPoint,closestSurface,triangle,submesh,normal,leanStep,careClearanceAvailable,careFrameClear,clearanceCandidates,weightedVertices,rayTests,bvhTriangleTests,clearanceMilliseconds,kinematicCandidates,settlingOut,sourceState,sourceNormalizedTime"};
        var legs=new List<string>{"run,frame,leg,pawError,boneLengthError,maximumReachMargin,regionalSoleY,pawTarget,actualFoot"};
        var summary=new List<string>{"run,completed,needDelta,minActualMouth,maxSkinDepth,minSkinY,maxLean,maxLeanStep,maxPawError,maxBoneLengthError,paused,restored,exitPaused,maxSolveMilliseconds,totalKinematicCandidates"};
        var mesh=new Mesh();
        try
        {
            yield return (IEnumerator)Call(fixture,"Home");yield return (IEnumerator)Call(fixture,"Room",HomeRoomService.KitchenId);
            yield return (IEnumerator)Call(fixture,"Breed","persian");
            var cat=Read<CatMovement>(fixture,"cat");var hunger=Read<HungerSystem>(fixture,"hunger");
            var meal=CatActivity.Registered.OfType<MealTimeActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene);
            var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();var indices=CatBreedCatalog.Load().Find("persian").ContactVertexIndices;
            var solids=meal.GetComponentsInChildren<MeshCollider>().Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy).ToArray();
            Assert.That(solids.Length,Is.GreaterThan(0));foreach(var c in solids)Assert.That(c.GetComponent<MeshFilter>()?.sharedMesh,Is.SameAs(c.sharedMesh));
            for(int run=0;run<2;run++)
            {
                Call(fixture,"ReadyNeeds");Call(fixture,"FindCareStance",meal.BowlPoint,Read<Transform>(meal,"standPoint"),(Func<bool>)(()=>meal.TryGetPromptDistance(cat,out _)));
                var prompt=Object.FindAnyObjectByType<ActivityPromptController>();Call(cat.GetComponent<BowlInteraction>(),"Update");Call(prompt,"RefreshImmediate");
                Assert.That(Read<CatActivity>(prompt,"candidate"),Is.SameAs(meal));var button=Read<Button>(prompt,"actionButton");
                Assert.That(button.isActiveAndEnabled&&button.interactable,Is.True);Assert.That(button.GetComponentInParent<Canvas>(),Is.Not.Null);
                Vector3 root=cat.transform.position;Quaternion heading=cat.transform.rotation;float before=hunger.CurrentHunger;
                int completions=0;Action<CatActivity> completed=a=>{if(a==meal)completions++;};CatActivity.Completed+=completed;
                float minimum=float.PositiveInfinity,maxSkin=0,minY=float.PositiveInfinity,maxLean=0,maxStep=0,maxPaw=0,maxBone=0,lastLean=0;
                bool paused=false,exitPaused=false,cancelled=false,restored=false;int frame=0,unsafeFrames=0,totalCandidates=0;
                double maxSolve=0;var lengths=new Dictionary<Transform,Vector2>();
                try
                {
                    button.onClick.Invoke();Assert.That(meal.IsRunning&&meal.IsEating,Is.True);float deadline=Time.realtimeSinceStartup+60;
                    while(meal.IsRunning&&Time.realtimeSinceStartup<deadline)
                    {
                        yield return new WaitForEndOfFrame();frame++;
                        var head=cat.GetComponent<CatMealHeadMotion>();float lean=head.BodyLeanDistance,step=Mathf.Abs(lean-lastLean);lastLean=lean;
                        maxLean=Mathf.Max(maxLean,lean);maxStep=Mathf.Max(maxStep,step);maxPaw=Mathf.Max(maxPaw,head.PawPlantError);
                        float distance=(float)Call(fixture,"ActualMouthDistance",meal.BowlPoint);
                        if(meal.IsEating&&Read<float>(head,"weight")>=.999f)minimum=Mathf.Min(minimum,distance);
                        Assert.That(Vector3.Distance(root,cat.transform.position),Is.LessThan(.0001f));Assert.That(Quaternion.Angle(heading,cat.transform.rotation),Is.LessThan(.01f));
                        var hit=new Hit{vertex=-1};Vector3[] points=null;
                        // Exit is part of the physical routine even after its last
                        // bite. Inspect every exit frame, including zero weight.
                        if(frame%3==0||head.IsSettlingOut||!meal.IsEating)
                        {
                            skin.BakeMesh(mesh,true);points=mesh.vertices;hit=Measure(points,skin,indices,solids);
                            maxSkin=Mathf.Max(maxSkin,hit.depth);minY=Mathf.Min(minY,hit.minimumY);
                        }
                        if(head.IsActive)
                        {
                            Assert.That(head.HasCareSkinClearance,Is.True,"The real kitchen geometry must be bound; no legacy fallback may pass this release test.");
                            if(!head.CareFrameClear)unsafeFrames++;
                            // Source animation may animate segment scale/translation.
                            // Compare this exact frame before/after IK, not frame1.
                            lengths=SourceLengths(head,cat);
                            int index=0;
                            foreach(var pair in new[]{Read<Array>(head,"forelegs"),Read<Array>(head,"hindlegs")})foreach(object limb in pair)
                            {
                                var upper=Read<Transform>(limb,"arm");var lower=Read<Transform>(limb,"fore");var foot=Read<Transform>(limb,"hand");
                                Vector2 current=new Vector2(Vector3.Distance(upper.position,lower.position),Vector3.Distance(lower.position,foot.position));
                                if(!lengths.TryGetValue(upper,out var original))lengths[upper]=original=current;
                                float bone=Mathf.Max(Mathf.Abs(current.x-original.x),Mathf.Abs(current.y-original.y));maxBone=Mathf.Max(maxBone,bone);
                                Vector3 target=Read<Vector3>(limb,"pawPosition");float sole=float.PositiveInfinity;
                                if(points!=null)foreach(var p in points){Vector3 world=skin.transform.TransformPoint(p),delta=world-foot.position;if(delta.x*delta.x+delta.z*delta.z<=.0049f)sole=Mathf.Min(sole,world.y);}
                                legs.Add(Csv(run,frame,index++,Vector3.Distance(foot.position,target),bone,current.x+current.y-Vector3.Distance(upper.position,target),sole,target,foot.position));
                            }
                        }
                        maxSolve=Math.Max(maxSolve,head.ClearanceMilliseconds);totalCandidates+=head.KinematicCandidates;
                        var animator=cat.GetComponentInChildren<Animator>();var state=animator.GetCurrentAnimatorStateInfo(0);
                        frames.Add(Csv(run,frame,meal.IsEating,lean,Read<float>(head,"weight"),distance,head.MinimumDistance,head.PawPlantError,head.MinimumLimbReachReserve,
                            Vector3.Distance(root,cat.transform.position),Quaternion.Angle(heading,cat.transform.rotation),hit.depth,hit.minimumY,hit.vertex,hit.bone,hit.point,hit.exact.point,hit.exact.triangle,hit.exact.submesh,hit.exact.normal,step,head.HasCareSkinClearance,head.CareFrameClear,head.ClearanceCandidates,
                            head.ClearanceVertices,head.ClearanceRays,head.ClearanceTriangleTests,head.ClearanceMilliseconds,
                            head.KinematicCandidates,head.IsSettlingOut,state.fullPathHash,state.normalizedTime));
                        bool pauseEating=!paused&&meal.IsEating&&lean>.04f;
                        bool pauseExit=run==0&&!exitPaused&&head.IsSettlingOut&&Read<float>(head,"weight")<.9f;
                        if(pauseEating||pauseExit)
                        {
                            paused|=pauseEating;exitPaused|=pauseExit;Time.timeScale=0;
                            var transforms=animator.GetComponentsInChildren<Transform>(true);
                            var positions=transforms.Select(t=>t.position).ToArray();var rotations=transforms.Select(t=>t.rotation).ToArray();float heldMinimum=head.MinimumDistance;
                            for(int n=0;n<6;n++)
                            {
                                yield return new WaitForEndOfFrame();
                                Assert.That(head.BodyLeanDistance,Is.EqualTo(lean).Within(.000002f));Assert.That(head.MinimumDistance,Is.EqualTo(heldMinimum));
                                for(int t=0;t<transforms.Length;t++){Assert.That(Vector3.Distance(positions[t],transforms[t].position),Is.LessThan(.00001f));Assert.That(Quaternion.Angle(rotations[t],transforms[t].rotation),Is.LessThan(.06f));}
                            }
                            Time.timeScale=1;
                        }
                        if(run==1&&head.CareFrameClear&&head.MinimumDistance<=.045f){meal.CancelForTransition();cancelled=true;}
                        if(!meal.IsEating&&head.BodyLeanDistance<.00001f)restored=true;
                    }
                    Assert.That(meal.IsRunning,Is.False);yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                    var finalHead=cat.GetComponent<CatMealHeadMotion>();restored|=!finalHead.IsActive&&finalHead.BodyLeanDistance==0;
                    summary.Add(Csv(run,completions,hunger.CurrentHunger-before,minimum,maxSkin,minY,maxLean,maxStep,maxPaw,maxBone,paused,restored,exitPaused,maxSolve,totalCandidates));Flush();
                    Assert.That(unsafeFrames,Is.Zero,"No live source frame may fall back to an unsafe body pose.");
                    Assert.That(paused&&restored,Is.True);Assert.That(maxLean,Is.GreaterThan(run==0?.065f:.04f),
                        "The cancelled run ends at first safe contact, which can precede full lean.");Assert.That(maxPaw,Is.LessThan(.0001f));Assert.That(maxBone,Is.LessThan(.00001f));
                    Assert.That(cat.GetComponent<CharacterController>().enabled&&!cat.IsMovementPhysicallyLocked,Is.True);
                    // Cancellation releases ownership before its native Idle
                    // crossfade ends; inspect standing skin after that blend.
                    if(run==1)yield return new WaitForSeconds(.35f);
                    Assert.That(CatMealHeadMotion.TryPrepareCareStart(cat,meal.BowlPoint,out _),Is.True,
                        "Fresh native standing skin refines only the verified care meshes; other blockers still apply.");
                    if(run==0)
                    {
                        Assert.That(exitPaused,Is.True,"Pause must also preserve the real source exit blend.");
                        Assert.That(completions,Is.EqualTo(1));Assert.That(hunger.CurrentHunger-before,Is.GreaterThan(35));Assert.That(minimum,Is.LessThanOrEqualTo(.045f));
                        Assert.That(maxStep,Is.LessThan(.012f),"Normal beginning/end must ease the measured body lean");
                        Assert.That(maxSkin,Is.LessThanOrEqualTo(.003f),"Single-frame candidate is not whole-routine clearance proof");Assert.That(minY,Is.GreaterThanOrEqualTo(-.003f));
                    }
                    else{Assert.That(cancelled,Is.True);Assert.That(completions,Is.Zero);Assert.That(hunger.CurrentHunger-before,Is.LessThan(.01f));}
                }
                finally{Time.timeScale=1;CatActivity.Completed-=completed;CatActionState.CancelForTransition(cat);Flush();}
            }
        }
        finally{Object.DestroyImmediate(mesh);Flush();}
        void Flush(){Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"care-body-lean-frames.csv"),frames);File.WriteAllLines(Path.Combine(Output,"care-body-lean-legs.csv"),legs);File.WriteAllLines(Path.Combine(Output,"care-body-lean-summary.csv"),summary);}
    }
    static Dictionary<Transform,Vector2> SourceLengths(CatMealHeadMotion head,CatMovement cat)
    {
        var all=cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        var positions=all.Select(t=>t.localPosition).ToArray();var rotations=all.Select(t=>t.localRotation).ToArray();var scales=all.Select(t=>t.localScale).ToArray();
        var limbs=Read<Array>(head,"forelegs").Cast<object>().Concat(Read<Array>(head,"hindlegs").Cast<object>()).ToArray();
        var elbows=limbs.Select(l=>Read<Vector3>(l,"elbowPosition")).ToArray();var result=new Dictionary<Transform,Vector2>();
        try
        {
            Call(head,"RestoreFrame");
            foreach(var limb in limbs){var upper=Read<Transform>(limb,"arm");var lower=Read<Transform>(limb,"fore");var foot=Read<Transform>(limb,"hand");
                result.Add(upper,new Vector2(Vector3.Distance(upper.position,lower.position),Vector3.Distance(lower.position,foot.position)));}
        }
        finally
        {
            for(int i=0;i<all.Length;i++){all[i].localPosition=positions[i];all[i].localRotation=rotations[i];all[i].localScale=scales[i];}
            for(int i=0;i<limbs.Length;i++)limbs[i].GetType().GetField("elbowPosition",Fields).SetValue(limbs[i],elbows[i]);
        }
        return result;
    }
    struct Hit{public float depth,minimumY;public int vertex;public string bone;public Vector3 point;public QaExactMeshContact.Hit exact;}
    Hit Measure(Vector3[] points,SkinnedMeshRenderer skin,IReadOnlyList<int> indices,MeshCollider[] solids)
    {
        var result=new Hit{minimumY=float.PositiveInfinity,vertex=-1};var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;
        foreach(int vertex in indices)
        {
            Vector3 point=skin.transform.TransformPoint(points[vertex]);result.minimumY=Mathf.Min(result.minimumY,point.y);
            foreach(var solid in solids)
            {
                if(!solid.bounds.Contains(point)||InsideVotes(solid,point)<4)continue;var hit=exact.Measure(solid.sharedMesh,solid.transform,point);
                if(hit.distance<=result.depth)continue;var w=weights[vertex];int b=w.boneIndex0;float weight=w.weight0;
                if(w.weight1>weight){b=w.boneIndex1;weight=w.weight1;}if(w.weight2>weight){b=w.boneIndex2;weight=w.weight2;}if(w.weight3>weight)b=w.boneIndex3;
                result.depth=hit.distance;result.vertex=vertex;result.point=point;result.exact=hit;result.bone=bones[b].name;
            }
        }
        return result;
    }
    static readonly Vector3[] Directions={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,new Vector3(.019f,.023f,1).normalized,-new Vector3(.013f,1,.027f).normalized,-new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    int InsideVotes(MeshCollider mesh,Vector3 point)
    {
        var data=topology.Get(mesh.sharedMesh);int votes=0;bool previous=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        try{foreach(var direction in Directions)if(mesh.Raycast(new Ray(point,direction),out var hit,5f)){int t=hit.triangleIndex*3;if(t<0||t+2>=data.triangles.Length)continue;Vector3 a=mesh.transform.TransformPoint(data.vertices[data.triangles[t]]),b=mesh.transform.TransformPoint(data.vertices[data.triangles[t+1]]),c=mesh.transform.TransformPoint(data.vertices[data.triangles[t+2]]);if(Vector3.Dot(Vector3.Cross(b-a,c-a),direction)>0)votes++;}}
        finally{Physics.queriesHitBackfaces=previous;}return votes;
    }
    static string Csv(params object[] values)=>string.Join(",",values.Select(value=>"\""+(value is Vector3 p?FormattableString.Invariant($"{p.x:F7};{p.y:F7};{p.z:F7}"):value is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):value?.ToString()??"").Replace("\"","\"\"")+"\""));
}
#endif
