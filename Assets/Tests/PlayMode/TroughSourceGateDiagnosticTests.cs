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
public sealed class TroughSourceGateDiagnosticTests
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
        File.WriteAllLines(Root+"/trough-source-gates.csv",rows);
        File.WriteAllLines(Root+"/trough-source-blockers.csv",blockers);
        fixture?.After();
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator CurrentAnchor_AndMeasuredLongAxis_LogSourceRejectionWithoutChangingReadiness()
    {
        var room = HomeRoomService.Rooms.First(r => r.Id == HomeRoomService.PatioId);
        yield return (IEnumerator)Invoke("Prepare",room.SceneName);
        cat=(CatMovement)typeof(PreparedInteractionStartTests).GetField("cat",Private).GetValue(fixture);
        activity=CatActivity.Registered.Single(a => a.gameObject.scene==cat.gameObject.scene && a.StoreProductId==HomeStoreService.PatioHerbTroughId);
        guard=(CatBodyGuard)typeof(CatMovement).GetField("bodyGuard",Private).GetValue(cat);
        solidMethod=typeof(CatBodyGuard).GetMethod("Solid",Private);
        penetrationMethod=typeof(CatBodyGuard).GetMethod("Penetration",Private);
        Assert.That(guard,Is.Not.Null); Assert.That(solidMethod,Is.Not.Null); Assert.That(penetrationMethod,Is.Not.Null);
        activity.TryGetStartPose(cat,out var start); centre=start.ZoneCentre; landing=start.ActionTarget;
        support=activity.GetComponentsInChildren<CatActivitySurface>(true).OrderBy(s => Vector3.Distance(s.transform.position,landing)).First().transform;
        area=support.GetComponent<CatActivitySurface>();
        Assert.That(Vector3.Distance(support.position,landing),Is.LessThan(.001f));
        var axis = Vector3.ProjectOnPlane(area.Size.x >= area.Size.y ? support.right : support.forward,Vector3.up).normalized;
        var normal=Vector3.ProjectOnPlane(centre-landing,Vector3.up); normal-=axis*Vector3.Dot(normal,axis); normal.Normalize();
        Assert.That(normal.sqrMagnitude,Is.GreaterThan(.99f));
        File.WriteAllText(Root+"/trough-source-metadata.txt", "centre="+V(centre)+"\nlanding="+V(landing)+"\nsurfaceSize="+F(area.Size.x)+";"+F(area.Size.y)+"\naxis="+V(axis)+"\nopenNormal="+V(normal)+"\n");

        foreach(float radius in new[]{0f,.055f,.11f,.165f,.215f})
        for(int angle=0;angle<(radius==0f?1:24);angle++)
        {
            Vector3 position=centre+Quaternion.Euler(0,angle*15f,0)*Vector3.forward*radius; position.y=.05f;
            Quaternion direct=Quaternion.LookRotation(Vector3.ProjectOnPlane(landing-position,Vector3.up));
            foreach(float yaw in new[]{0f,-15f,15f,-30f,30f})
            {
                Measure("authored",position,Quaternion.AngleAxis(yaw,Vector3.up)*direct);
                if(sequence%12==0) yield return null;
            }
        }
        // The following positions measure a possible shared surface-end zone.
        // They do not grant readiness, change an anchor, or run a bypassed action.
        foreach(float end in new[]{-1f,1f})
        foreach(float along in new[]{.4f,.6f,.8f})
        foreach(float outward in new[]{.12f,.24f})
        {
            Vector3 position=landing+axis*(end*along)+normal*outward; position.y=.05f;
            Quaternion parallel=Quaternion.LookRotation(-axis*end);
            Measure("axis-parallel",position,parallel);
            Quaternion direct=Quaternion.LookRotation(Vector3.ProjectOnPlane(landing-position,Vector3.up));
            foreach(float yaw in new[]{0f,-15f,15f,-30f,30f}) Measure("axis-cone",position,Quaternion.AngleAxis(yaw,Vector3.up)*direct);
            yield return null;
        }
        File.WriteAllText(Root+"/trough-source-summary.txt", "candidates="+sequence+"\nauthoredPublicReady="+authoredReady+"\naxisPublicReady="+axisReady+"\naxisPhysicalConeAndSourceClear="+physicalAxisReady+"\nThis is a diagnostic, not a completed activity/contact/skin-clearance pass.\n");
        Assert.That(sequence,Is.EqualTo(557),"The bounded candidate inventory changed.");
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

    void Diagnose(int id,string domain,CatJumpClearanceResolver.Rejection rejected,out string firstRegion,out string firstCollider,out float firstDepth)
    {
        firstRegion="";firstCollider="";firstDepth=0;
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>(); var entry=CatJumpClearanceCatalog.Load().Find(tag.BreedId);
        var sample=entry.samples.Single(s => Mathf.Abs(s.phase-rejected.phase)<.000001f);
        Matrix4x4 local=cat.transform.worldToLocalMatrix*tag.transform.localToWorldMatrix;
        Matrix4x4 root=Matrix4x4.TRS(rejected.root,rejected.heading,cat.transform.lossyScale),matrix=root*local;
        Vector3 zero=local.MultiplyPoint3x4(entry.sourceZeroCentre);
        bool preparation=sample.phase<=CatJumpMotion.Takeoff+.000001f;
        Vector3 correction=root.MultiplyVector(preparation?Vector3.zero:new Vector3(-zero.x,0,-zero.z));
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
