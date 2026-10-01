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

// Bounded QA stance selection and pure source math. No activity is force-started,
// source animation sampled onto the actor, target moved, or limit relaxed.
public sealed class KnockAlternativeSourceEvidenceTests
{
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    GroundContactStartTests fixture;
    readonly List<string> rows=new List<string>(),details=new List<string>();
    sealed class Candidate{public int id;public object source;public Vector3 position;public Quaternion rotation;public float margin,phase,pitch;public CatActivityPose pose;public bool ready;}
    object Invoke(string name,params object[] args)=>typeof(GroundContactStartTests).GetMethod(name,Private).Invoke(fixture,args);
    static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value);
    static string F(float x)=>x.ToString("R",CultureInfo.InvariantCulture);
    static string V(Vector3 p)=>F(p.x)+";"+F(p.y)+";"+F(p.z);
    [SetUp]public void Before(){fixture=new GroundContactStartTests();fixture.Before();rows.Clear();details.Clear();}
    [TearDown]public void After()
    {
        File.WriteAllLines(Root+"/knock-alternative-source-gates.csv",rows);File.WriteAllLines(Root+"/knock-alternative-source-regions.csv",details);fixture?.After();
    }
    [UnityTest,Timeout(180000)]
    public IEnumerator RussianBlue_SideTable_CompactSources_ReportUsableRealGeometry()
    {
        yield return (IEnumerator)Invoke("Prepare","Balcony_Level01");
        var cat=(CatMovement)typeof(GroundContactStartTests).GetField("cat",Private).GetValue(fixture);
        CatBreedService.Select("russian-blue");yield return QaBreedReadiness.WaitForSelected(cat,"russian-blue");
        var activity=CatActivity.Registered.OfType<KnockOffActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BalconySideTableId);
        Assert.That(activity.PerchPoint,Is.Null);
        var reach=(Transform)typeof(KnockOffActivity).GetField("reachPoint",Private).GetValue(activity);
        var targetMethod=typeof(KnockOffActivity).GetMethod("TryGroundedContact",Private);
        var type=typeof(CatPawReachResolver);var transform=type.GetMethod("TransformSource",Static);
        var marginMethod=type.GetMethod("ReachMargin",Static);var fixedMethod=type.GetMethod("FixedTrajectoryClear",Static);var trajectory=type.GetMethod("TrajectoryClear",Static);
        Assert.That(transform!=null&&marginMethod!=null&&fixedMethod!=null&&trajectory!=null,Is.True);
        rows.Add("candidate,position,yaw,pose,target,left,bestMargin,phase,pitch,fixedClear,mathCandidates,safeTrajectories,publicReady,acceptedPhase,acceptedPitch");
        details.Add("case,path,sourcePhase,contactPhase,pitch,region,clear,start,end,radius,boundsMin,boundsMax,maxDepth,blockingCollider,normal");
        var best=new List<Candidate>();int candidate=0,physical=0,plans=0;var clock=System.Diagnostics.Stopwatch.StartNew();
        foreach(float radius in new[]{0f,.10f,.20f,.30f,.41f})
        for(int angle=0;angle<(radius==0f?1:12);angle++)
        foreach(float yaw in new[]{0f,-30f,30f})
        {
            Assert.That(clock.Elapsed.TotalSeconds,Is.LessThan(45),"Bounded alternative-source diagnostic");
            candidate++;var p=reach.position+Quaternion.Euler(0,angle*30,0)*Vector3.forward*radius;p.y=.05f;
            var q=Quaternion.LookRotation(Vector3.ProjectOnPlane(activity.GlassPivot.position-p,Vector3.up))*Quaternion.Euler(0,yaw,0);
            Invoke("Place",p,q);
            if(!CatActivityStartResolver.Facing(cat,reach.position,.42f,activity.GlassPivot.position,45f,out _))continue;
            physical++;object[] targetArgs={p,q,Vector3.zero,false};Assert.That((bool)targetMethod.Invoke(activity,targetArgs),Is.True);
            var target=(Vector3)targetArgs[2];bool left=(bool)targetArgs[3];var visual=cat.GetComponentInChildren<CatBreedVisualTag>();
            foreach(var pose in new[]{left?CatActivityPose.BatLeft:CatActivityPose.BatRight,CatActivityPose.Push,CatActivityPose.Tug,CatActivityPose.Scratch,CatActivityPose.Paw})
            {
                var entry=CatPawReachCatalog.Load().Find(visual.BreedId,pose);Assert.That(entry?.samples,Is.Not.Null,"Real profile must be loaded by production.");
                object source=transform.Invoke(null,new object[]{entry,visual.transform.localToWorldMatrix,visual.transform.lossyScale});Assert.That(source,Is.Not.Null);
                bool fixedClear=(bool)fixedMethod.Invoke(null,new[]{(object)cat,source});float bestMargin=float.NegativeInfinity,bestPhase=0,bestPitch=0;int math=0,safe=0;
                foreach(object sample in (Array)Field(source,"contacts"))for(float pitch=0;pitch<=CatPawReachResolver.MaximumChestPitch;pitch++)
                {
                    float phase=(float)Field(sample,"phase");var bend=Quaternion.AngleAxis(pitch,q*Vector3.right);
                    float margin=(float)marginMethod.Invoke(null,new[]{sample,(object)bend,Field(sample,left?"left":"right"),target});
                    if(margin>bestMargin){bestMargin=margin;bestPhase=phase;bestPitch=pitch;}
                    if(margin<.004f)continue;math++;
                    if(fixedClear&&(bool)trajectory.Invoke(null,new object[]{cat,source,phase,pitch,0f,q*Vector3.right}))safe++;
                }
                bool ready=CatPawReachResolver.TryResolve(cat,target,left,pose,out var accepted);if(ready)plans++;
                rows.Add(string.Join(",",candidate,V(p),F(q.eulerAngles.y),pose,V(target),left,F(bestMargin),F(bestPhase),F(bestPitch),fixedClear,math,safe,ready,ready?F(accepted.SourcePhase):"",ready?F(accepted.ChestPitch):""));
                best.Add(new Candidate{id=candidate,source=source,position=p,rotation=q,margin=bestMargin,phase=ready?accepted.SourcePhase:bestPhase,pitch=ready?accepted.ChestPitch:bestPitch,pose=pose,ready=ready});
                best=best.OrderByDescending(c=>c.ready).ThenByDescending(c=>c.margin).Take(6).ToList();
            }
            yield return null;
        }
        Assert.That(candidate,Is.EqualTo(147));Assert.That(physical,Is.GreaterThan(0),"No physically clear stance was measured.");
        foreach(var c in best)
        {
            Invoke("Place",c.position,c.rotation);
            // The older shared diagnostic writes three boxed floats via
            // string.Join, so Turkish decimal commas shift the CSV columns.
            // Keep this synchronous writer invariant and restore immediately;
            // never change the culture across a frame or alter gameplay state.
            var culture=CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
                Invoke("ScratchDiagnosticRegions",c.id,"alternative-"+c.pose,c.source,c.phase,c.pitch,details);
            }
            finally { CultureInfo.CurrentCulture=culture; }
        }
        rows.Add("# diagnostic-only,physical="+physical+";sourcePlans="+plans+";topSixPreferAcceptedPlans;existing source clips only;not a KnockOff activity/contact PASS");
    }
}
