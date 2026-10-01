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

/// <summary>Measurement only: never changes a runtime readiness rule or an authored anchor.</summary>
public sealed class SourceGateEvidenceTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;
    CatMovement cat;
    CatActivity activity;
    CatBodyGuard guard;
    MethodInfo solidMethod, penetrationMethod;
    readonly List<string> rows = new List<string>();
    readonly List<string> blockers = new List<string>();
    int sequence, authoredReady, axisReady, physicalAxisReady;
    Vector3 centre, landing;
    Transform support;
    CatActivitySurface area;
    static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);
    static string V(Vector3 v) => F(v.x) + ";" + F(v.y) + ";" + F(v.z);
    static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
    static string PathOf(Transform t) { var s = t.name; while (t.parent != null) { t=t.parent; s=t.name+"/"+s; } return s; }
    object Invoke(string name, params object[] args) => typeof(PreparedInteractionStartTests).GetMethod(name, Private).Invoke(fixture, args);

    [SetUp] public void Before()
    {
        fixture = new PreparedInteractionStartTests(); fixture.Before();
        rows.Clear(); blockers.Clear(); sequence=authoredReady=axisReady=physicalAxisReady=0;
        rows.Add("candidate,domain,position,yaw,zoneDistance,flightDistance,headingError,fixtureClear,currentClear,localApproach,promptReady,endsClear,firstPhase,firstRegion,firstCollider,firstDepth");
        blockers.Add("candidate,domain,phase,root,heading,region,start,end,radius,collider,type,depth,normal,boundsMin,boundsMax");
    }
    [TearDown] public void After()
    {
        Directory.CreateDirectory(Root);
        File.WriteAllLines(Root+"/source-gates-"+TestContext.CurrentContext.Test.Name+".csv",rows);
        File.WriteAllLines(Root+"/source-blockers-"+TestContext.CurrentContext.Test.Name+".csv",blockers);
        fixture?.After();
    }

    IEnumerator Prepare(string scene, Func<CatActivity,bool> match)
    {
        yield return (IEnumerator)Invoke("Prepare",scene);
        cat=(CatMovement)typeof(PreparedInteractionStartTests).GetField("cat",Private).GetValue(fixture);
        activity=CatActivity.Registered.Single(a=>a.gameObject.scene==cat.gameObject.scene&&match(a));
        guard=(CatBodyGuard)typeof(CatMovement).GetField("bodyGuard",Private).GetValue(cat);
        solidMethod=typeof(CatBodyGuard).GetMethod("Solid",Private);
        penetrationMethod=typeof(CatBodyGuard).GetMethod("Penetration",Private);
        Assert.That(guard,Is.Not.Null);Assert.That(solidMethod,Is.Not.Null);Assert.That(penetrationMethod,Is.Not.Null);
        activity.TryGetStartPose(cat,out var start);centre=start.ZoneCentre;landing=start.ActionTarget;
        rows.Add("# metadata,"+Q(scene+"/"+activity.Kind+"/"+activity.StoreProductId+" selected="+CatBreedService.SelectedBreedId+
            ";visual="+cat.GetComponentInChildren<CatBreedVisualTag>().BreedId+";product="+V(activity.transform.position)+";centre="+V(centre)+";landing="+V(landing)));
    }
    [UnityTest,Timeout(180000)]
    public IEnumerator ThreeAuthoredEntries_ReportSourcePhaseRegionAndCollider_WithoutBypassingGate()
    {
        string[] scenes={"LivingRoom_Level01","Bedroom_Level01","Patio_Level01"};
        for(int room=0;room<scenes.Length;room++)
        {
            int selection=room;
            yield return Prepare(scenes[room],a=>selection==0?a.Kind==CatActivityKind.SofaLounge:
                a.StoreProductId==(selection==1?"bedroom.star-canopy":"patio.herb-trough"));
            int before=sequence,offered=authoredReady;
            foreach(float radius in new[]{0f,.055f,.11f,.165f,.215f})
            for(int angle=0;angle<(radius==0f?1:24);angle++)
            {
                Vector3 position=centre+Quaternion.Euler(0,angle*15f,0)*Vector3.forward*radius;position.y=.05f;
                Quaternion direct=Quaternion.LookRotation(Vector3.ProjectOnPlane(landing-position,Vector3.up));
                foreach(float yaw in new[]{0f,-15f,15f,-30f,30f})
                {
                    int old=authoredReady;Measure("authored",position,Quaternion.AngleAxis(yaw,Vector3.up)*direct);
                    var last=rows[rows.Count-1];rows[rows.Count-1]=last.Replace(",authored,",","+Q(scenes[room]+"/"+activity.Kind)+",");
                    if(sequence%12==0)yield return null;
                }
            }
            rows.Add("# diagnostic-only,"+Q(scenes[room]+" measured="+(sequence-before)+" publicOffers="+(authoredReady-offered)));
            Assert.That(sequence-before,Is.EqualTo(485));
        }
    }
    [UnityTest,Timeout(90000)]
    public IEnumerator HangingChair_ActualStandingExit_ReportsEveryBoundedDescentRejection()
    {
        yield return Prepare("Balcony_Level01",a=>a.StoreProductId=="balcony.hanging-chair");
        object[] args={activity,default(CatActivityStart),string.Empty};
        Assert.That((bool)Invoke("FindReadyPose",args),Is.True,(string)args[2]);var accepted=(CatActivityStart)args[1];
        RoomPlayModeSupport.ProvisionNeeds();Assert.That(activity.TryStart(cat),Is.True);
        var animation=cat.GetComponent<CatActivityAnimation>();bool measured=false;float deadline=Time.realtimeSinceStartup+35;
        while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
        {
            RoomPlayModeSupport.StopObservedRest(activity);yield return new WaitForEndOfFrame();
            if(measured||animation.CurrentPose!=CatActivityPose.StandUp||animation.NativeJumpPhase<.90f)continue;
            measured=true;Vector3 from=cat.transform.position;
            var preferred=(Transform)activity.GetType().GetField("doorPoint",Private).GetValue(activity);
            Vector3 floor=preferred.position;floor.y=accepted.Position.y;
            Vector3 outward=Vector3.ProjectOnPlane(floor-from,Vector3.up);float distance=outward.magnitude;outward.Normalize();
            var directions=new List<Vector3>{outward};
            Vector3 profile=activity.transform.TransformDirection(CatFurnitureJumpClearance.DownHeading(activity.StoreProductId));profile.y=0;
            if(profile.sqrMagnitude>.0001f)directions.Add(profile.normalized);
            foreach(float angle in new[]{-15f,15f,-30f,30f,-45f,45f,-60f,60f,-75f,75f,-90f,90f})directions.Add(Quaternion.AngleAxis(angle,Vector3.up)*outward);
            foreach(var direction in directions)for(int extension=0;extension<=6;extension++)
            {
                Vector3 end=from+direction*(distance+extension*.06f);end.y=floor.y;Quaternion heading=Quaternion.LookRotation(direction);
                if(!cat.IsInteractionPoseClear(end,heading))continue;
                var boundary=HomeRoomBoundary.FindFor(cat.gameObject.scene);
                if(boundary!=null&&(end.x<boundary.MinimumXZ.x||end.x>boundary.MaximumXZ.x||end.z<boundary.MinimumXZ.y||end.z>boundary.MaximumXZ.y))continue;
                Vector3 before=cat.transform.position;Quaternion rotation=cat.transform.rotation;
                bool clear=CatJumpClearanceResolver.EndsClear(cat,from,end,heading,false,false,out var rejection);
                string region="",collider="";float depth=0;
                if(!clear)Diagnose(++sequence,"hanging-exit",rejection,out region,out collider,out depth,false,false);
                rows.Add("# exit,"+Q("liveStandPhase="+F(animation.NativeJumpPhase)+";from="+V(from)+";to="+V(end)+";yaw="+F(heading.eulerAngles.y)+
                    ";clear="+clear+";rejectPhase="+F(rejection.phase)+";region="+region+";collider="+collider+";depth="+F(depth)));
                Assert.That(cat.transform.position,Is.EqualTo(before));Assert.That(cat.transform.rotation,Is.EqualTo(rotation));
                if(!clear&&rejection.phase<=CatJumpMotion.Takeoff)break;
            }
        }
        Assert.That(measured,Is.True,"No real supported StandUp exit was observed; diagnostic is invalid.");
        rows.Add("# diagnostic-only,"+Q("running="+activity.IsRunning+";standExitMeasured="+measured+";not a completion/skin PASS"));
    }

    void Measure(string domain, Vector3 position, Quaternion heading)
    {
        int id=++sequence;
        bool fixtureClear=(bool)Invoke("ControllerAndBodyClear",position,heading);
        bool currentClear=cat.IsInteractionPoseClear(position,heading);
        bool approach=false, ready=false, ends=false;
        float phase=-1, firstDepth=0; string firstRegion="",firstCollider="";
        float error=Vector3.Angle(heading*Vector3.forward,Vector3.ProjectOnPlane(landing-position,Vector3.up));
        if(fixtureClear && currentClear)
        {
            object[] args={position,heading,0f}; approach=(bool)Invoke("HasLocalApproach",args);
            Invoke("Place",position,heading);
            ready=activity.TryGetStartPose(cat,out _) && activity.TryGetPromptDistance(cat,out _);
            ends=CatJumpClearanceResolver.EndsClear(cat,position,landing,heading,true,true,out var rejection);
            if(!ends) { phase=rejection.phase; Diagnose(id,domain,rejection,out firstRegion,out firstCollider,out firstDepth); }
            if(domain=="authored" && ready) authoredReady++;
            if(domain!="authored" && ready) axisReady++;
            if(domain!="authored" && approach && ends && error<=35f) physicalAxisReady++;
        }
        Vector3 flatPosition=position,flatCentre=centre,flatLanding=landing; flatPosition.y=flatCentre.y=flatLanding.y=0;
        rows.Add(string.Join(",",id,domain,Q(V(position)),F(heading.eulerAngles.y),F(Vector3.Distance(flatPosition,flatCentre)),F(Vector3.Distance(flatPosition,flatLanding)),F(error),fixtureClear,currentClear,approach,ready,ends,F(phase),Q(firstRegion),Q(firstCollider),F(firstDepth)));
    }

    void Diagnose(int id,string domain,CatJumpClearanceResolver.Rejection rejected,out string firstRegion,out string firstCollider,out float firstDepth, bool landOnSupport=true, bool preparedGroundLaunch=true)
    {
        firstRegion="";firstCollider="";firstDepth=0;
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>(); var entry=CatJumpClearanceCatalog.Load().Find(tag.BreedId);
        var sample=entry.samples.Single(s => Mathf.Abs(s.phase-rejected.phase)<.000001f);
        Matrix4x4 local=cat.transform.worldToLocalMatrix*tag.transform.localToWorldMatrix;
        Matrix4x4 root=Matrix4x4.TRS(rejected.root,rejected.heading,cat.transform.lossyScale),matrix=root*local;
        Vector3 zero=local.MultiplyPoint3x4(entry.sourceZeroCentre);
        bool preparation=sample.phase<=CatJumpMotion.Takeoff+.000001f;
        Vector3 startOffset=Vector3.zero;
        if(!preparedGroundLaunch)
            Assert.That(cat.GetComponent<CatActivityAnimation>().TryReadNativeJumpStartOffset(rejected.heading,out startOffset),Is.True);
        Vector3 endOffset=landOnSupport?new Vector3(-zero.x,0,-zero.z):Vector3.zero;
        Vector3 correction=root.MultiplyVector(preparation?startOffset:endOffset);
        correction+=Vector3.up*(rejected.root.y-matrix.m13+.008f);
        float scale=Mathf.Max(((Vector3)matrix.GetColumn(0)).magnitude,Mathf.Max(((Vector3)matrix.GetColumn(1)).magnitude,((Vector3)matrix.GetColumn(2)).magnitude));
        foreach(var source in sample.probes)
        {
            var p=source;p.start=matrix.MultiplyPoint3x4(p.start)+correction;p.end=matrix.MultiplyPoint3x4(p.end)+correction;p.radius*=scale;
            foreach(var collider in Physics.OverlapCapsule(p.start,p.end,p.radius,~0,QueryTriggerInteraction.Ignore))
            {
                if(!(bool)solidMethod.Invoke(guard,new object[]{collider})) continue;
                object[] args={p.start,p.end,p.radius,collider,Vector3.zero,0f};
                if(!(bool)penetrationMethod.Invoke(guard,args)) continue;
                float depth=(float)args[5]; if(depth<=.015f) continue;
                string path=PathOf(collider.transform);
                if(firstDepth==0){firstDepth=depth;firstRegion=p.region;firstCollider=path;}
                blockers.Add(string.Join(",",id,domain,F(sample.phase),Q(V(rejected.root)),F(rejected.heading.eulerAngles.y),Q(p.region),Q(V(p.start)),Q(V(p.end)),F(p.radius),Q(path),collider.GetType().Name,F(depth),Q(V((Vector3)args[4])),Q(V(collider.bounds.min)),Q(V(collider.bounds.max))));
            }
        }
        if(firstDepth==0) firstRegion="NO_NARROW_BLOCKER_REPRODUCED";
    }
}
