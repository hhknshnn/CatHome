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

public sealed class BowlCareGateDiagnosticTests
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    CareAlignmentPolishTests fixture;
    static T Read<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Fields).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Fields).SetValue(owner,value);
    static object Call(object owner,string name,params object[] values)=>owner.GetType().GetMethod(name,Fields).Invoke(owner,values);
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
    [SetUp]public void Before(){fixture=new CareAlignmentPolishTests();fixture.Before();}
    [TearDown]public void After(){fixture?.After();fixture=null;}

    [UnityTest,Timeout(180000)]
    public IEnumerator TwoBreeds_RealBowlButton_ObserveBeginReachSkinAndRecovery()
    {
        var rows=new List<string>{"breed,kind,frame,interacting,atContact,headActive,geometryBound,frameClear,weight,lean,minimumMouth,currentMouth,skinMinY,lowestVertex,lowestBone,lastRejectedDepth,recovering,need,sourceState,sourcePhase,animatorMatches,root,yaw,target,weightedVertices,rayTests,solverCandidates,pawError,limbReserve,rejectedSourceVertex,rejectedBone,rejectedCollider,rejectedPoint,exactMetricQueries"};
        var summary=new List<string>{"breed,kind,headFrames,unsafeFrames,contactFrames,recoveryFrames,minActualMouth,minSkinY,minRecordedMouth,needDelta,completed,skinProfile,mesh,sourceVertexCount"};
        var baked=new Mesh();
        try
        {
            yield return (IEnumerator)Call(fixture,"Home");
            foreach(string breed in new[]{"oriental-shorthair","persian"})
            {
                yield return (IEnumerator)Call(fixture,"Breed",breed);
                var cat=Read<CatMovement>(fixture,"cat");var bowls=Read<BowlInteraction>(fixture,"bowls");
                var hunger=Read<HungerSystem>(fixture,"hunger");var thirst=Read<ThirstSystem>(fixture,"thirst");
                foreach(string kind in new[]{"food","water"})
                {
                    Call(fixture,"ReadyNeeds");var setup=Read<BowlInteraction.BowlSetup>(bowls,kind);setup.Fill();
                    Set(bowls,kind=="food"?"eatingDuration":"drinkingDuration",.8f);
                    Transform target=setup.ContactPoint!=null?setup.ContactPoint:setup.Bowl;
                    Func<bool> offered=()=>{Call(bowls,"Update");return ReferenceEquals(Read<BowlInteraction.BowlSetup>(bowls,"currentBowl"),setup);};
                    Call(fixture,"FindCareStance",target,setup.InteractionPoint,offered);
                    var button=(Button)Call(fixture,"BowlButton",setup);
                    var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();var animator=cat.GetComponentInChildren<Animator>();
                    var profile=CatCareSkinCatalog.Load()?.Find(breed);
                    Assert.That(profile,Is.Not.Null,"Actual care skin source profile");
                    float before=kind=="food"?hunger.CurrentHunger:thirst.CurrentThirst;
                    int completed=0,headFrames=0,unsafeFrames=0,contactFrames=0,recoveryFrames=0,frame=0;
                    Action onComplete=()=>completed++;
                    if(kind=="food")hunger.Ate+=onComplete;else thirst.Drank+=onComplete;
                    float minMouth=float.PositiveInfinity,minY=float.PositiveInfinity,minRecorded=float.PositiveInfinity;
                    try
                    {
                        button.onClick.Invoke();Assert.That(bowls.IsInteracting,Is.True);
                        float deadline=Time.realtimeSinceStartup+12;
                        while(bowls.IsInteracting&&Time.realtimeSinceStartup<deadline)
                        {
                            yield return new WaitForEndOfFrame();frame++;
                            var head=cat.GetComponent<CatMealHeadMotion>();bool active=head!=null&&head.IsActive;
                            bool recovering=kind=="food"?hunger.IsEating:thirst.IsDrinking;
                            if(active)headFrames++;if(active&&!head.CareFrameClear)unsafeFrames++;
                            if(bowls.IsAtContact)contactFrames++;if(recovering)recoveryFrames++;
                            float actual=(float)Call(fixture,"ActualMouthDistance",target);minMouth=Mathf.Min(minMouth,actual);
                            if(head!=null)minRecorded=Mathf.Min(minRecorded,head.MinimumDistance);
                            float low=float.PositiveInfinity;int lowest=-1;string lowBone="";
                            if(frame%3==0||active&&!head.CareFrameClear)
                            {
                                skin.BakeMesh(baked,true);var points=baked.vertices;
                                for(int index=0;index<profile.vertexIndices.Length;index++)
                                {
                                    int vertex=profile.vertexIndices[index];float y=skin.transform.TransformPoint(points[vertex]).y;
                                    if(y>=low)continue;low=y;lowest=vertex;
                                    int strongest=profile.starts[index];
                                    for(int n=strongest+1;n<profile.starts[index+1];n++)if(profile.weights[n]>profile.weights[strongest])strongest=n;
                                    lowBone=profile.bonePaths[profile.bones[strongest]];
                                }
                                minY=Mathf.Min(minY,low);
                            }
                            var state=animator.GetCurrentAnimatorStateInfo(0);
                            var clearance=head!=null?Read<CatCareSkinClearance>(head,"careClearance"):null;
                            int rejected=clearance?.LastRejectedSourceVertex??-1;string rejectedBone="";
                            int rejectedProfile=Array.IndexOf(profile.vertexIndices,rejected);
                            if(rejectedProfile>=0)
                            {
                                int strongest=profile.starts[rejectedProfile];
                                for(int n=strongest+1;n<profile.starts[rejectedProfile+1];n++)if(profile.weights[n]>profile.weights[strongest])strongest=n;
                                rejectedBone=profile.bonePaths[profile.bones[strongest]];
                            }
                            if(active)Assert.That(head.Distance,Is.EqualTo(actual).Within(.00001f),"Runtime success metric must equal independent minimum over actual mouth vertices.");
                            rows.Add(Csv(breed,kind,frame,bowls.IsInteracting,bowls.IsAtContact,active,head!=null&&head.HasCareSkinClearance,
                                head==null||head.CareFrameClear,head!=null?Read<float>(head,"weight"):0,head!=null?head.BodyLeanDistance:0,
                                head!=null?head.MinimumDistance:float.PositiveInfinity,actual,low,lowest,lowBone,clearance?.LastMaximumDepth??0,
                                recovering,kind=="food"?hunger.CurrentHunger:thirst.CurrentThirst,state.fullPathHash,state.normalizedTime,
                                Read<Animator>(bowls,"animator")==animator,cat.transform.position,cat.transform.eulerAngles.y,target.position,
                                head!=null?head.ClearanceVertices:0,head!=null?head.ClearanceRays:0,
                                head!=null?head.ClearanceCandidates:0,head!=null?head.PawPlantError:0,head!=null?head.MinimumLimbReachReserve:0,
                                rejected,rejectedBone,clearance?.LastRejectedCollider!=null?clearance.LastRejectedCollider.name:"",
                                clearance?.LastRejectedPoint??Vector3.zero,clearance?.LastMetricQueries??0));
                        }
                        summary.Add(Csv(breed,kind,headFrames,unsafeFrames,contactFrames,recoveryFrames,minMouth,minY,minRecorded,
                            (kind=="food"?hunger.CurrentHunger:thirst.CurrentThirst)-before,completed,profile.meshName,skin.sharedMesh.name,profile.vertexIndices.Length));
                        Flush();
                    }
                    finally
                    {
                        if(kind=="food")hunger.Ate-=onComplete;else thirst.Drank-=onComplete;
                        CatActionState.CancelForTransition(cat);
                    }
                    yield return RoomPlayModeSupport.WaitForMovementRelease(cat);yield return null;
                }
            }
            Assert.That(summary.Count,Is.EqualTo(5),"Measurement covers all four real-button attempts; it does not claim successful care.");
        }
        finally{Object.DestroyImmediate(baked);Flush();}
        void Flush(){Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"bowl-care-gate-frames.csv"),rows);File.WriteAllLines(Path.Combine(Output,"bowl-care-gate-summary.csv"),summary);}
    }
    static string Csv(params object[] values)=>string.Join(",",values.Select(value=>"\""+(value is Vector3 p?FormattableString.Invariant($"{p.x:F7};{p.y:F7};{p.z:F7}"):value is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):value?.ToString()??"").Replace("\"","\"\"")+"\""));
}
#endif
