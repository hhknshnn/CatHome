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

// Observes a normally accepted full activity. Its last supported StandUp frame
// supplies the same horizontal centering and measured lift that BeginNativeJump
// will read before sampling the departure clip. No rejected activity is forced.
public sealed class CanopyDepartureSourceTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;
    readonly List<string> states=new List<string>(),gates=new List<string>(),blocks=new List<string>();
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
    static T Read<T>(object o,string name)=>(T)o.GetType().GetField(name,Private).GetValue(o);
    static string Csv(params object[] values)=>string.Join(",",values.Select(v=>"\""+(v is Vector3 p?FormattableString.Invariant($"{p.x:R};{p.y:R};{p.z:R}"):
        v is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):v?.ToString()??"").Replace("\"","\"\"")+"\""));
    [TearDown]public void After()
    {
        Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"canopy-departure-states.csv"),states);
        File.WriteAllLines(Path.Combine(Output,"canopy-departure-gates.csv"),gates);File.WriteAllLines(Path.Combine(Output,"canopy-departure-blockers.csv"),blocks);
        fixture?.After();
    }
    [UnityTest,Timeout(100000)]public IEnumerator AcceptedCanopyCycle_CompareDepartureSourceAgainstItsActualCarriedSupportLift()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();yield return (IEnumerator)Call(fixture,"Prepare","Bedroom_Level01");
        var cat=Read<CatMovement>(fixture,"cat");
        var activity=CatActivity.Registered.OfType<CanopyNapActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BedroomStarCanopyId);
        Vector3 start=new Vector3(-.28f,.05f,1.46f);Call(fixture,"Place",start,Quaternion.identity);
        yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
        Assert.That(activity.TryGetStartPose(cat,out _),Is.True);RoomPlayModeSupport.ProvisionNeeds();Assert.That(activity.TryStart(cat),Is.True);
        var animation=cat.GetComponent<CatActivityAnimation>();var tag=cat.GetComponentInChildren<CatBreedVisualTag>();
        var entry=CatJumpClearanceCatalog.Load().Find(tag.BreedId);Assert.That(entry,Is.Not.Null);
        var solids=Read<Collider[]>(fixture,"roomSolids");
        var guard=Read<CatBodyGuard>(cat,"bodyGuard");var penetrate=typeof(CatBodyGuard).GetMethod("Penetration",Private);
        var isSolid=typeof(CatBodyGuard).GetMethod("Solid",Private);
        states.Add("frame,time,pose,phase,native,running,root,yaw,visualOffset,supportLift,completed,stopRequested");
        gates.Add("frame,sourcePosePhase,actualSupportLift,direction,extension,landing,yaw,floorClear,liveSourceClear,liveRejectPhase,withActualLiftClear,liftedRejectPhase,groundLandingFound,groundLanding,groundHeading");
        blocks.Add("frame,direction,extension,variant,phase,region,collider,type,depth,normal,start,end,radius,root,actualSupportLift");
        int complete=0,after=0,snapshots=0,nativeFrames=0;bool stopped=false;
        Action<CatActivity> completion=a=>{if(a==activity)complete++;};CatActivity.Completed+=completion;
        float began=Time.time,deadline=Time.realtimeSinceStartup+50;
        try
        {
            while((activity.IsRunning||after<3)&&Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();if(!activity.IsRunning)after++;
                var support=cat.GetComponent<CatMeasuredSupportMotion>();float lift=support!=null?support.VisualLift:0;
                float phase=Read<float>(animation,"phase");animation.TryReadNativeJumpStartOffset(cat.transform.rotation,out var offset);
                states.Add(Csv(Time.frameCount,Time.time-began,animation.CurrentPose,phase,animation.IsNativeJump,activity.IsRunning,
                    cat.transform.position,cat.transform.eulerAngles.y,offset,lift,complete,stopped));
                if(animation.IsNativeJump)nativeFrames++;
                if(activity.IsRunning&&animation.CurrentPose==CatActivityPose.StandUp&&phase>=.8f&&snapshots<6)
                {
                    Snapshot(cat,activity,animation,entry,tag,solids,guard,penetrate,isSolid,phase,lift);snapshots++;
                }
                if(!stopped&&activity.IsWaitingForRestStop&&activity.RestingSeconds>=1.2f)
                {stopped=true;Assert.That(activity.RequestRestStop(),Is.True);}
            }
            Assert.That(activity.IsRunning,Is.False,"Natural cycle did not terminate within50seconds");
            Assert.That(nativeFrames,Is.GreaterThan(10));Assert.That(snapshots,Is.GreaterThan(0),"No actual supported departure state was observed.");
            TestContext.WriteLine("Diagnostic only: completion="+complete+", departure snapshots="+snapshots+". Inspect state and blocker files; a cancellation is not a completed-cycle PASS.");
        }
        finally {CatActivity.Completed-=completion;if(activity.IsRunning)activity.CancelForTransition();}
    }
    void Snapshot(CatMovement cat,CanopyNapActivity activity,CatActivityAnimation animation,CatJumpClearanceCatalog.Entry entry,
        CatBreedVisualTag visual,Collider[] solids,CatBodyGuard guard,MethodInfo penetrate,MethodInfo isSolid,float sourcePosePhase,float lift)
    {
        Vector3 before=cat.transform.position;Quaternion beforeYaw=cat.transform.rotation;
        var bones=cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        var positions=bones.Select(t=>t.localPosition).ToArray();var rotations=bones.Select(t=>t.localRotation).ToArray();
        Assert.That(activity.TryGetPreparedStart(cat,out var accepted),Is.True);
        Vector3 from=activity.NestPoint.position,preferred=Read<Transform>(activity,"doorPoint").position;preferred.y=accepted.Position.y;
        Vector3 measured=activity.transform.TransformDirection(CatFurnitureJumpClearance.DownHeading(activity.StoreProductId));
        bool resolved=CatActivityStartResolver.GroundLanding(cat,from,preferred,out var actualLanding,out var actualHeading,measured);
        Vector3 outward=Vector3.ProjectOnPlane(preferred-from,Vector3.up);float distance=outward.magnitude;
        outward=distance>.0001f?outward.normalized:cat.transform.forward;
        var directions=new List<Vector3>{outward};measured.y=0;
        if(measured.sqrMagnitude>.0001f)directions.Add(measured.normalized);
        foreach(float angle in new[]{-15f,15f,-30f,30f,-45f,45f,-60f,60f,-75f,75f,-90f,90f})directions.Add(Quaternion.AngleAxis(angle,Vector3.up)*outward);
        Matrix4x4 tagInRoot=cat.transform.worldToLocalMatrix*visual.transform.localToWorldMatrix;
        for(int d=0;d<directions.Count;d++)for(int extension=0;extension<=6;extension++)
        {
            Vector3 landing=from+directions[d]*(distance+extension*.06f);landing.y=preferred.y;
            Quaternion heading=Quaternion.LookRotation(directions[d]);bool floor=cat.IsInteractionPoseClear(landing,heading);
            if(!floor)continue;
            var boundary=HomeRoomBoundary.FindFor(cat.gameObject.scene);
            if(boundary!=null&&(landing.x<boundary.MinimumXZ.x||landing.x>boundary.MaximumXZ.x||landing.z<boundary.MinimumXZ.y||landing.z>boundary.MaximumXZ.y))continue;
            bool live=CatJumpClearanceResolver.EndsClear(cat,from,landing,heading,false,false,out var rejection);
            bool predicted=Compare(true,out float liftedPhase);
            bool mirrored=Compare(false,out _);Assert.That(mirrored,Is.EqualTo(live),"The diagnostic must first reproduce the live source gate.");
            gates.Add(Csv(Time.frameCount,sourcePosePhase,lift,d,extension,landing,heading.eulerAngles.y,floor,live,rejection.phase,predicted,liftedPhase,resolved,actualLanding,actualHeading.eulerAngles.y));
            // Preparation is independent of the final travel distance, just as
            // the production resolver skips useless extensions on that ray.
            if(!live&&rejection.phase<=CatJumpMotion.Takeoff)break;
            bool Compare(bool addActualLift,out float rejectedPhase)
            {
                rejectedPhase=0;Assert.That(animation.TryReadNativeJumpStartOffset(heading,out var offset),Is.True);
                foreach(var sample in entry.samples)
                {
                    bool prep=sample.phase<=CatJumpMotion.Takeoff+.000001f;
                    if(!prep&&sample.phase<CatJumpMotion.Touchdown-.000001f)continue;
                    Vector3 root=prep?from:landing;Matrix4x4 rootMatrix=Matrix4x4.TRS(root,heading,cat.transform.lossyScale),matrix=rootMatrix*tagInRoot;
                    Vector3 correction=rootMatrix.MultiplyVector(prep?offset:Vector3.zero)+Vector3.up*(root.y-matrix.m13+.008f+(prep&&addActualLift?lift:0));
                    float scale=Mathf.Max(((Vector3)matrix.GetColumn(0)).magnitude,Mathf.Max(((Vector3)matrix.GetColumn(1)).magnitude,((Vector3)matrix.GetColumn(2)).magnitude));
                    CatBodyGuardCatalog.Probe[] World(IEnumerable<CatBodyGuardCatalog.Probe> source)=>source.Select(raw=>new CatBodyGuardCatalog.Probe{
                        region=raw.region,start=matrix.MultiplyPoint3x4(raw.start)+correction,end=matrix.MultiplyPoint3x4(raw.end)+correction,radius=raw.radius*scale}).ToArray();
                    bool refined=sample.headCoverageVersion==CatJumpClearanceCatalog.HeadCoverageVersion&&sample.headTriangleCount>0&&
                        sample.headProbes!=null&&sample.headProbes.Length>0&&sample.headProbes.Length<=CatJumpClearanceCatalog.MaxHeadProbes;
                    var probes=World(refined?sample.probes.Take(3):sample.probes);
                    if(cat.IsInteractionBodyClear(probes))
                    {
                        if(!refined)continue;
                        if(cat.IsInteractionBodyClear(World(new[]{sample.headEnvelope})))continue;
                        probes=World(sample.headProbes);
                        if(cat.IsInteractionBodyClear(probes))continue;
                    }
                    rejectedPhase=sample.phase;
                    foreach(var probe in probes)foreach(var solid in solids)
                    {
                        if(solid==null||!(bool)isSolid.Invoke(guard,new object[]{solid}))continue;
                        object[] args={probe.start,probe.end,probe.radius,solid,Vector3.zero,0f};
                        if(!(bool)penetrate.Invoke(guard,args)||(float)args[5]<=.015f)continue;
                        blocks.Add(Csv(Time.frameCount,d,extension,addActualLift?"actual-carried-lift":"current-no-lift",sample.phase,probe.region,
                            solid.name,solid.GetType().Name,(float)args[5],(Vector3)args[4],probe.start,probe.end,probe.radius,root,lift));
                    }
                    return false;
                }
                return true;
            }
        }
        Assert.That(cat.transform.position,Is.EqualTo(before));Assert.That(cat.transform.rotation,Is.EqualTo(beforeYaw));
        for(int i=0;i<bones.Length;i++){Assert.That(bones[i].localPosition,Is.EqualTo(positions[i]));Assert.That(bones[i].localRotation,Is.EqualTo(rotations[i]));}
    }
}
#endif
