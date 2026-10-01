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
using Object=UnityEngine.Object;

// Query diagnostic only: production current-pose gates and all geometry remain
// active. No activity is force-started, played, or marked completed.
public sealed class FlowerWrapperReadinessDiagnosticTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic;
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    FlowerContactPolishTests fixture;
    readonly List<string> rows=new List<string>();
    static object Get(object item,string name)=>item?.GetType().GetField(name,F)?.GetValue(item);
    static string N(float value)=>value.ToString("G9",CultureInfo.InvariantCulture);
    static string V(Vector3 value)=>N(value.x)+";"+N(value.y)+";"+N(value.z);
    static string Q(Quaternion value)=>N(value.x)+";"+N(value.y)+";"+N(value.z)+";"+N(value.w);
    static string M(Matrix4x4 value)=>string.Join(";",Enumerable.Range(0,16).Select(i=>N(value[i])));
    static string Csv(params object[] values)=>string.Join(",",values.Select(v=>"\""+(v is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):v?.ToString()??"").Replace("\"","\"\"")+"\""));
    [SetUp]public void Before()
    {
        fixture=new FlowerContactPolishTests();fixture.Before();
        rows.Add("stance,phase,frame,call,ready,pending,elapsedMs,indexBefore,indexAfter,math,physics,build,requestCount,matchingRequestCount,knownCursor,knownScan,knownCandidates,knownAccepted,bodyCCClear,actualBreed,actorPosition,actorRotation,visualMatrix,authoredTarget,measuredTarget,measuredNormal,directLeft,candidateCount,knownRank,sight,knownYaw");
    }
    [TearDown]public void After()
    {Directory.CreateDirectory(Root);File.WriteAllLines(Root+"/flower-wrapper-readiness.csv",rows);fixture?.After();}

    [UnityTest,Timeout(90000)]public IEnumerator Longhair_ExactFindStance_DoublePollAndDirectShareOnlyRealCurrentGeometry()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);CatBreedService.Select("domestic-longhair");
        var cat=Object.FindAnyObjectByType<CatMovement>();yield return QaBreedReadiness.WaitForSelected(cat,"domestic-longhair");
        cat.GetComponent<CatIdleBehavior>().enabled=false;
        var activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
        var all=((Vector3[])Get(activity,"visibleLookTargets")).Select(activity.transform.TransformPoint).ToArray();
        // This is deliberately the same authored point, .40 offset and -80
        // multiplication used by FindStance, with no decimal rounding.
        var first=all.OrderBy(p=>Vector3.ProjectOnPlane(p-activity.RoutineEntryPoint.position,Vector3.up).sqrMagnitude).First();
        Vector3 exact=first+activity.transform.forward*.40f;exact.y=.05f;
        Vector3 toward=first-exact;toward.y=0;
        Quaternion exactRotation=Quaternion.LookRotation(toward)*Quaternion.Euler(0,-80,0);
        var known=all.OrderBy(p=>(p-new Vector3(-3.43f,.55f,1.17f)).sqrMagnitude).First();
        Assert.That(CatPawReachResolver.TryMeasureSurface(activity.transform,known,out var surface),Is.True);
        foreach(bool rounded in new[]{false,true})
        {
            var cc=cat.GetComponent<CharacterController>();cc.enabled=false;
            cat.transform.SetPositionAndRotation(rounded?new Vector3(-3.03f,.05f,1.17f):exact,rounded?Quaternion.Euler(0,190,0):exactRotation);
            cc.enabled=true;Physics.SyncTransforms();yield return null;
            string label=rounded?"roundedStandalone":"exactFindStance";
            bool left=Vector3.Dot(surface.point-cat.transform.position,cat.transform.right)<=0;
            for(int phase=0;phase<2;phase++)
            {
                float deadline=Time.realtimeSinceStartup+12;bool wrapperReady=false;
                for(int frame=0;frame<240&&Time.realtimeSinceStartup<deadline;frame++)
                {
                    yield return null;
                    // Phase0 is actual wrapper-only polling twice per frame.
                    // Phase1 alternates who receives the first shared budget;
                    // both still run in the same frame at the identical root.
                    if(phase==1&&frame%2==1)Query(true,"direct-first");
                    wrapperReady=Query(false,"wrapper-0");
                    wrapperReady|=Query(false,"wrapper-1");
                    if(phase==1&&frame%2==0)Query(true,"direct-after");
                    if(frame%30==0||wrapperReady)File.WriteAllLines(Root+"/flower-wrapper-readiness.csv",rows);
                    if(wrapperReady)break;

                    bool Query(bool direct,string call)
                    {
                        int before=(int)Get(activity,"startSearchIndex");var timer=System.Diagnostics.Stopwatch.StartNew();
                        bool ready=direct?CatPawReachResolver.TryResolveSurface(cat,surface,left,CatActivityPose.Scratch,out _,32,35):activity.TryGetPromptDistance(cat,out _);
                        timer.Stop();int math=CatPawReachResolver.SurfaceMathCandidatesLastQuery,physics=CatPawReachResolver.SurfacePhysicsCandidatesLastQuery;
                        bool pending=direct?CatPawReachResolver.SurfaceQueryPending:activity.IsStartSearchPending;
                        var cache=(IDictionary)typeof(CatPawReachResolver).GetField("surfaceGeometryCache",S).GetValue(null);
                        var geometry=cache.Values.Cast<object>().FirstOrDefault();var requests=Get(geometry,"requests") as IDictionary;
                        int matching=0,cursor=0,scan=0,candidates=0;bool accepted=false;
                        if(requests!=null)foreach(DictionaryEntry pair in requests)
                        {
                            if(!((Vector3)Get(pair.Key,"point")).Equals(surface.point)||!((Vector3)Get(pair.Key,"normal")).Equals(surface.normal.normalized))continue;
                            matching++;cursor=(int)Get(pair.Value,"cursor");scan=(int)Get(pair.Value,"scanIndex");
                            candidates=((IList)Get(pair.Value,"candidates")).Count;accepted|=Get(pair.Value,"accepted")!=null;
                        }
                        var valid=all.Select((p,i)=>new{p,i,d=Vector3.ProjectOnPlane(p-cat.transform.position,Vector3.up).magnitude,
                            yaw=Vector3.Angle(cat.transform.forward,Vector3.ProjectOnPlane(p-cat.transform.position,Vector3.up))})
                            .Where(t=>t.d>=.30f&&t.d<=.58f&&t.yaw<=100f).OrderBy(t=>t.d).ThenBy(t=>t.i).ToArray();
                        int rank=Array.FindIndex(valid,t=>t.p.Equals(known));var visual=cat.GetComponentInChildren<CatBreedVisualTag>();
                        rows.Add(Csv(label,phase,Time.frameCount,call,ready,pending,timer.Elapsed.TotalMilliseconds,before,Get(activity,"startSearchIndex"),math,physics,
                            Get(geometry,"buildCursor"),requests?.Count??0,matching,cursor,scan,candidates,accepted,
                            cat.IsInteractionPoseClear(cat.transform.position,cat.transform.rotation),visual?.BreedId,V(cat.transform.position),Q(cat.transform.rotation),
                            visual!=null?M(visual.transform.localToWorldMatrix):"missing",V(known),V(surface.point),V(surface.normal),left,valid.Length,rank,
                            CatActivityApproach.HasClearSight(activity,cat,cat.transform.position+Vector3.up*.4f,known),
                            Vector3.Angle(cat.transform.forward,Vector3.ProjectOnPlane(known-cat.transform.position,Vector3.up))));
                        return ready;
                    }
                }
            }
        }
        Assert.That(rows.Count,Is.GreaterThan(4),"Measurement output is required; this test does not claim a completed interaction");
    }
}
