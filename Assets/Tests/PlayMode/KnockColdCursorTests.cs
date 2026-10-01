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

public sealed class KnockColdCursorTests
{
    const string Root="Docs/QA/INTERACTION_POLISH_FINISH_2026-09-16/flower-knock/knock-cursor-stage";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic;
    FlowerContactPolishTests fixture;Action<string,double,int,bool> previous;
    readonly List<string> stages=new List<string>(),queries=new List<string>();
    int stance,query;bool measuring;string lastStage="";double maxStage;
    readonly List<string> verdicts=new List<string>();
    readonly int[] inspected={0,0,0,0};bool inspecting;
    readonly HashSet<string>[] witnessBends={new HashSet<string>(),new HashSet<string>(),new HashSet<string>(),new HashSet<string>()};
    static readonly Vector3[] InsideDirections={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,
        new Vector3(.019f,.023f,1).normalized,-new Vector3(.013f,1,.027f).normalized,
        -new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    static object Get(object value,string name)=>value?.GetType().GetField(name,F)?.GetValue(value);
    static int Integer(object value,string name)=>Get(value,name) is int x?x:-1;
    static string Csv(params object[] values)=>string.Join(",",values.Select(value=>"\""+(value is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):value?.ToString()??"").Replace("\"","\"\"")+"\""));
    [SetUp]public void Before()
    {
        fixture=new FlowerContactPolishTests();fixture.Before();previous=CatPawReachResolver.SurfaceQueryMeasurement;
        CatPawReachResolver.SurfaceQueryMeasurement=Measure;stages.Clear();queries.Clear();query=0;
        verdicts.Clear();for(int i=0;i<inspected.Length;i++){inspected[i]=0;witnessBends[i].Clear();}
        verdicts.Add("stance,query,frame,stage,sourceSample,sourcePhase,pitch,yaw,contactPhase,activeLeft,arcLift,arcNormal,worldClear,targetClear,region,targetRegionClear,maxVertexDepth,maxCentroidDepth,insideVertices,insideCentroids,deepestTarget,metricKnown,firstWorldBlocker,worldBoxIndex,worldVertexDepth,worldCentroidDepth,worldMetricKnown,diagnosticMs");
        stages.Add("stance,query,frame,stage,sample,ms,clear,reachError,shoulderGoalDistance,chainPlusSkinLimit");
        queries.Add("stance,query,frame,ms,ready,pending,math,physics,lastStage,maxStageMs,target,targetCount,targetPoint,targetNormal,left,geometryBuild,requests,cursor,totalExaminedAllTargets,scan,totalScansAllTargets,candidates,allCandidates,bodyCursor,pawCursor,upperScreened,endpointScreened,sourcePhase,pitch,yaw,fixedBodyScreened");
    }
    void Measure(string stage,double ms,int sample,bool clear)
    {
        if(!measuring)return;lastStage=stage;maxStage=Math.Max(maxStage,ms);
        stages.Add(Csv(stance,query,Time.frameCount,stage,sample,ms,clear,CatPawReachResolver.SurfaceReachError,CatPawReachResolver.SurfaceReachDistance,CatPawReachResolver.SurfaceReachLimit));
        if((stage=="body-phase"||stage=="arm-phase")&&!clear&&!inspecting&&inspected[stance]<6)CaptureSurfaceVerdict(stage,sample);
    }
    [TearDown]public void After()
    {
        CatPawReachResolver.SurfaceQueryMeasurement=previous;Directory.CreateDirectory(Root);
        File.WriteAllLines(Root+"/knock-eight-second-stages.csv",stages);
        File.WriteAllLines(Root+"/knock-eight-second-cursors.csv",queries);
        File.WriteAllLines(Root+"/knock-world-target-verdict.csv",verdicts);fixture?.After();
    }
    void CaptureSurfaceVerdict(string stage,int sampleIndex)
    {
        inspecting=true;var timer=System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var cache=(IDictionary)typeof(CatPawReachResolver).GetField("surfaceGeometryCache",S).GetValue(null);
            object geometry=null;CatPawReachCatalog.Entry entry=null;
            foreach(DictionaryEntry pair in cache)if(((CatPawReachCatalog.Entry)pair.Key).pose==CatActivityPose.Paw)
            {geometry=pair.Value;entry=(CatPawReachCatalog.Entry)pair.Key;break;}
            bool arms=stage=="arm-phase"||stage=="endpoint";var context=arms?geometry:Get(geometry,"bodySurface");
            if(context==null||entry==null)return;
            var actor=(CatMovement)Get(context,arms?"checkingActor":"actor");
            var plan=(CatPawReachPlan)Get(context,arms?"checkingPlan":"plan");
            string bend=stage+"/"+sampleIndex+"/"+plan.ChestPitch.ToString("G9",CultureInfo.InvariantCulture)+"/"+plan.ChestYaw.ToString("G9",CultureInfo.InvariantCulture)+"/"+plan.Surface.ApproachLift.ToString("G9",CultureInfo.InvariantCulture)+"/"+plan.Surface.ApproachNormal.ToString("G9");
            if(witnessBends[stance].Contains(bend))return;
            var boxes=(CatBodyGuardBox[])Get(context,arms?"endpointScratch":"phaseScratch");
            var refine=(Func<int,Collider,bool>)Get(context,"refinement");float tolerance=arms ? .002f : .015f;
            Collider firstWorldBlocker=null;int worldBoxIndex=-1;
            bool worldClear=actor.IsInteractionBoxesClear(boxes,tolerance,(index,solid)=>
            {
                bool clear=refine(index,solid);
                if(!clear&&firstWorldBlocker==null){firstWorldBlocker=solid;worldBoxIndex=index;}
                return clear;
            });
            bool targetClear=plan.Surface==null||plan.Surface.TargetBoxesClear(boxes,tolerance);
            // One baseline, then reserve the small witness budget for a
            // target-only refusal or the independently measured raised-head bends.
            if(inspected[stance]>0&&!(worldClear&&!targetClear)&&plan.ChestPitch>-24f)return;
            witnessBends[stance].Add(bend);inspected[stance]++;
            Vector3[][] regions=arms?new[]{(Vector3[])Get(geometry,"leftArmScratch"),(Vector3[])Get(geometry,"rightArmScratch")}:
                Get(context,"points") as Vector3[][];
            bool known=regions!=null&&Integer(context,arms?"checkingFixedSample":"checkedSample")==sampleIndex;
            int leftCount=arms?((IList)Get(geometry,"leftRegions")).Count:0;
            var owner=plan.Surface!=null?(CatMeshContactSurface.TargetSet)Get(plan.Surface.MeshHit,"Owner"):null;
            var parts=Get(owner,"parts") as IEnumerable;
            var exit=typeof(CatMeshContactSurface.TargetSet).GetMethod("NearestExit",S);
            for(int r=0;r<(arms?2:4);r++)
            {
                var regionBoxes=new CatBodyGuardBox[arms?(r==0?leftCount:boxes.Length-leftCount):1];
                Array.Copy(boxes,arms?(r==0?0:leftCount):r,regionBoxes,0,regionBoxes.Length);
                bool targetRegion=plan.Surface==null||plan.Surface.TargetBoxesClear(regionBoxes,tolerance);
                float vertexDepth=0,centroidDepth=0;int insideVertices=0,insideCentroids=0;string deepest="";
                if(known&&parts!=null)foreach(object part in parts)
                {
                    var filter=(MeshFilter)Get(part,"filter");var renderer=(Renderer)Get(part,"renderer");
                    if(filter==null||renderer==null||!renderer.enabled||!filter.gameObject.activeInHierarchy||Get(part,"data")==null)continue;
                    var bounds=(Bounds)Get(part,"bounds");var points=regions[r];
                    float Depth(Vector3 point)
                    {
                        if(!bounds.Contains(point))return 0;
                        int votes=0;foreach(var direction in InsideDirections)
                            if((bool)exit.Invoke(null,new object[]{part,point,direction}))votes++;
                        if(votes<4)return 0;
                        if(!CatMeshContactSurface.TryMetric(filter.sharedMesh,filter.transform,point,out float distance,out _))
                        {known=false;return 0;}
                        if(distance>Mathf.Max(vertexDepth,centroidDepth))deepest=filter.name;
                        return distance;
                    }
                    foreach(var point in points)
                    {float depth=Depth(point);if(depth>0)insideVertices++;vertexDepth=Mathf.Max(vertexDepth,depth);}
                    var faces=arms?(r==0?entry.leftArmTriangles:entry.rightArmTriangles):entry.bodySurface[r].triangles;
                    for(int t=0;t<faces.Length;t+=3)
                    {
                        float depth=Depth((points[faces[t]]+points[faces[t+1]]+points[faces[t+2]])/3f);
                        if(depth>0)insideCentroids++;centroidDepth=Mathf.Max(centroidDepth,depth);
                    }
                }
                float worldVertexDepth=0,worldCentroidDepth=0;bool worldMetricKnown=firstWorldBlocker!=null;
                if(known&&firstWorldBlocker!=null)
                {
                    foreach(var point in regions[r])worldVertexDepth=Mathf.Max(worldVertexDepth,WorldDepth(firstWorldBlocker,point,ref worldMetricKnown));
                    var faces=arms?(r==0?entry.leftArmTriangles:entry.rightArmTriangles):entry.bodySurface[r].triangles;
                    for(int t=0;t<faces.Length;t+=3)
                        worldCentroidDepth=Mathf.Max(worldCentroidDepth,WorldDepth(firstWorldBlocker,
                            (regions[r][faces[t]]+regions[r][faces[t+1]]+regions[r][faces[t+2]])/3f,ref worldMetricKnown));
                }
                // Vertex/centroid evidence is diagnostic, not a whole-face
                // clearance permission. Zero here cannot bypass runtime gates.
                verdicts.Add(Csv(stance,query,Time.frameCount,stage,sampleIndex,entry.samples[sampleIndex].phase,
                    plan.ChestPitch,plan.ChestYaw,plan.SourcePhase,plan.Left,plan.Surface.ApproachLift,plan.Surface.ApproachNormal.ToString("G9"),worldClear,targetClear,arms?(r==0?"left-arm":"right-arm"):entry.bodySurface[r].region,
                    targetRegion,vertexDepth,centroidDepth,insideVertices,insideCentroids,deepest,known,
                    firstWorldBlocker!=null?firstWorldBlocker.name+"/"+firstWorldBlocker.GetType().Name:"",worldBoxIndex,
                    worldVertexDepth,worldCentroidDepth,worldMetricKnown,timer.Elapsed.TotalMilliseconds));
            }
        }
        finally{inspecting=false;}
    }
    static float WorldDepth(Collider solid,Vector3 point,ref bool known)
    {
        if(!solid.bounds.Contains(point))return 0;
        if(solid is BoxCollider box)
        {
            Vector3 local=box.transform.InverseTransformPoint(point)-box.center,half=box.size*.5f,scale=box.transform.lossyScale;
            Vector3 gap=half-new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z));
            if(gap.x<0||gap.y<0||gap.z<0)return 0;
            return Mathf.Min(gap.x*Mathf.Abs(scale.x),gap.y*Mathf.Abs(scale.y),gap.z*Mathf.Abs(scale.z));
        }
        if(!(solid is MeshCollider mesh)||mesh.sharedMesh==null){known=false;return 0;}
        int votes=0;bool previous=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        try
        {
            foreach(var direction in InsideDirections)
            {
                if(!mesh.Raycast(new Ray(point,direction),out var hit,mesh.bounds.size.magnitude+1))continue;
                if(!CatMeshContactSurface.TryOriginalTriangleNormal(mesh.sharedMesh,mesh.transform,hit.triangleIndex,out var normal))
                {known=false;continue;}
                if(Vector3.Dot(normal,direction)>0)votes++;
            }
        }
        finally{Physics.queriesHitBackfaces=previous;}
        if(votes<4)return 0;
        if(CatMeshContactSurface.TryMetric(mesh.sharedMesh,mesh.transform,point,out float distance,out _))return distance;
        known=false;return 0;
    }
    [UnityTest,Timeout(60000)]public IEnumerator TwoLegalStances_EightSecondCandidateAndStageDiagnostic()=>RunReadiness(false);
    [UnityTest,Timeout(60000)]public IEnumerator FourNearbyLegalStances_OfferWithinBoundedSearch()=>RunReadiness(true);
    [UnityTest,Timeout(60000)]public IEnumerator TwoOriginalStances_TwentySecondPerStance_ReadinessAcceptance()=>RunReadiness(false,20,true);
    IEnumerator RunReadiness(bool nearby,float originalSeconds=4,bool requireReady=false)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);CatBreedService.Select("russian-blue");
        var cat=Object.FindAnyObjectByType<CatMovement>();yield return QaBreedReadiness.WaitForSelected(cat,"russian-blue");
        cat.GetComponent<CatIdleBehavior>().enabled=false;
        var activity=CatActivity.Registered.OfType<KnockOffActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BalconySideTableId);
        Vector3[] points={new Vector3(-2.74492979f,.05f,-.3049999f),new Vector3(-3.01339769f,.05f,-.149999857f)};
        float[] yaws={244.254883f,184.719849f};
        Vector3 first=new Vector3(-2.74492979f,.05f,-.3049999f),second=new Vector3(-3.01339769f,.05f,-.149999857f);
        Vector3 firstBack=-(Quaternion.Euler(0,244.254883f,0)*Vector3.forward),secondBack=-(Quaternion.Euler(0,184.719849f,0)*Vector3.forward);
        Vector3[] nearbyPoints={first+firstBack*.04f,first+firstBack*.08f,second+secondBack*.04f,second+secondBack*.08f};
        float[] nearbyYaws={244.254883f,244.254883f,184.719849f,184.719849f};
        if(nearby){points=nearbyPoints;yaws=nearbyYaws;}
        bool found=false;
        for(stance=0;stance<points.Length;stance++)
        {
            var cc=cat.GetComponent<CharacterController>();cc.enabled=false;
            cat.transform.SetPositionAndRotation(points[stance],Quaternion.Euler(0,yaws[stance],0));cc.enabled=true;Physics.SyncTransforms();yield return null;
            bool physical=cat.IsInteractionPoseClear(cat.transform.position,cat.transform.rotation);
            if(!physical&&nearby){stages.Add(Csv(stance,query,Time.frameCount,"physical-refusal",-1,0,false));continue;}
            Assert.That(physical,Is.True);
            Vector3 root=cat.transform.position;Quaternion rotation=cat.transform.rotation;float until=Time.realtimeSinceStartup+(nearby?8:originalSeconds);
            do
            {
                yield return null;query++;lastStage="";maxStage=0;var watch=System.Diagnostics.Stopwatch.StartNew();bool ready;
                measuring=true;try{ready=activity.TryGetStartPose(cat,out _);}finally{measuring=false;watch.Stop();}
                int math=CatPawReachResolver.SurfaceMathCandidatesLastQuery,physics=CatPawReachResolver.SurfacePhysicsCandidatesLastQuery;
                object search=Get(activity,"groundTargets");var targets=Get(search,"candidates") as IList;
                int count=targets?.Count??0,next=Integer(search,"cursor");
                int target=count==0?-1:activity.IsStartSearchPending?(next+count-1)%count:ready?next:-1;
                var hit=target>=0?(CatMeshContactSurface.Hit)Get(targets[target],"hit"):default;
                bool left=target>=0&&(bool)Get(targets[target],"left");
                var cache=(IDictionary)typeof(CatPawReachResolver).GetField("surfaceGeometryCache",S).GetValue(null);
                object geometry=null;
                foreach(DictionaryEntry pair in cache)if(((CatPawReachCatalog.Entry)pair.Key).pose==CatActivityPose.Paw){geometry=pair.Value;break;}
                var requests=Get(geometry,"requests") as IDictionary;object current=null,candidate=null;
                int totalExamined=0,totalScans=0,totalCandidates=0;
                if(requests!=null)foreach(DictionaryEntry pair in requests)
                {
                    totalExamined+=Integer(pair.Value,"cursor");totalScans+=Integer(pair.Value,"scanIndex");
                    totalCandidates+=(Get(pair.Value,"candidates") as IList)?.Count??0;
                    if(target>=0&&((Vector3)Get(pair.Key,"point")).Equals(hit.Point)&&(bool)Get(pair.Key,"left")==left)
                        current=pair.Value;
                }
                var candidates=Get(current,"candidates") as IList;int scan=Integer(current,"scanIndex");
                if(candidates!=null&&scan>=0&&scan<candidates.Count)candidate=candidates[scan];
                CatPawReachPlan plan=candidate!=null?(CatPawReachPlan)Get(candidate,"plan"):default;
                queries.Add(Csv(stance,query,Time.frameCount,watch.Elapsed.TotalMilliseconds,ready,activity.IsStartSearchPending,math,physics,
                    lastStage,maxStage,target,count,hit.IsValid?hit.Point.ToString("G9"):"",hit.IsValid?hit.Normal.ToString("G9"):"",left,
                    Integer(geometry,"buildCursor"),requests?.Count??0,Integer(current,"cursor"),totalExamined,scan,totalScans,
                    candidates?.Count??0,totalCandidates,Integer(candidate,"bodyBuildCursor"),Integer(candidate,"pawBuildCursor"),
                    Get(candidate,"upperScreened")??false,Get(candidate,"endpointScreened")??false,plan.SourcePhase,plan.ChestPitch,plan.ChestYaw,
                    Get(current,"fixedBodyScreened")??false));
                Assert.That(Vector3.Distance(root,cat.transform.position),Is.LessThan(.001f));
                Assert.That(Quaternion.Angle(rotation,cat.transform.rotation),Is.LessThan(.2f));
                Assert.That(activity.IsRunning,Is.False);
                if(ready){found=true;Assert.That(activity.TryGetPromptDistance(cat,out _),Is.True,"A ready plan must also be offered by the prompt");break;}
                if(!activity.IsStartSearchPending)break;
            }while(Time.realtimeSinceStartup<until);
            if(found&&(nearby||requireReady))break;
        }
        if(nearby)Assert.That(found,Is.True,"At least one of four actual clear nearby player stances must offer a fully checked path within8s; no forced start");
        if(requireReady)Assert.That(found,Is.True,"At least one of the two original physically legal player stances must offer a fully checked real-mesh Paw path within20seconds per stance; no forced start or geometry exemption");
        if(!nearby&&!requireReady)Assert.That(queries.Count,Is.GreaterThan(2),"Diagnostic only: data captured is not a successful contact or readiness assertion");
    }
}
