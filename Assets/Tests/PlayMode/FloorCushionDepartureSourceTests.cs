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
using Object=UnityEngine.Object;

// One naturally accepted cycle; snapshot the real final supported StandUp.
// The remote source clone never supplies permission to the live activity.
public sealed class FloorCushionDepartureSourceTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly string[] Regions={"pelvis","chest","neck","head"};
    static readonly Vector3[] Rays={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,
        new Vector3(.019f,.023f,1).normalized,-new Vector3(.013f,1,.027f).normalized,
        -new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    readonly List<string> states=new List<string>(),gates=new List<string>(),blocks=new List<string>(),skinRows=new List<string>();
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();
    readonly List<Candidate> candidates=new List<Candidate>();
    PreparedInteractionStartTests fixture;
    CatMovement cat;
    TowelNestActivity activity;
    CatJumpClearanceCatalog.Entry entry;
    Collider[] solids;
    CatBodyGuard guard;
    MethodInfo solidFilter,penetrate;
    QaExactMeshContact metric;
    GameObject sourceVisual;
    Mesh baked;
    Matrix4x4 tagInRoot;
    Vector3 departure,preferred;
    string breedId;
    float sourceLift;
    int frame;
    sealed class Candidate { public int direction,extension;public Vector3 landing,offset;public Quaternion heading;public bool floor,boundary,clear,visited;public CatJumpClearanceResolver.Rejection rejection; }
    sealed class SkinMaximum { public int count,vertex=-1,votes;public float depth;public Vector3 point,closest,normal;public int triangle=-1; }
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Hidden).Invoke(target,args);
    static T Read<T>(object target,string name)=>(T)target.GetType().GetField(name,Hidden).GetValue(target);
    static string Csv(params object[] values)=>string.Join(",",values.Select(v=>"\""+(v is Vector3 p?FormattableString.Invariant($"{p.x:R};{p.y:R};{p.z:R}"):
        v is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):v?.ToString()??"").Replace("\"","\"\"")+"\""));

    [SetUp] public void Before()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();metric=new QaExactMeshContact(topology);baked=new Mesh();
        states.Add("frame,pose,phase,native,running,root,yaw,visualOffset,measuredLift,completionCount");
        gates.Add("frame,direction,extension,authoredFrom,authoredLanding,candidateLanding,yaw,floorClear,inBoundary,productionWouldVisit,endsClear,rejectPhase,rejectRoot,startOffset,actualSupportLift,groundLandingFound,selectedLanding,selectedYaw");
        blocks.Add("frame,direction,extension,phase,region,level,part,collider,type,depth,normal,start,end,radius");
        skinRows.Add("frame,direction,extension,phase,region,collider,coarseDepth,maxPartDepth,insideVertices,maxExactDepth,vertex,votes,skinPoint,closest,normal,triangle,minPartContainment,actualSupportLift,sourceClip,sourceHash,classification");
    }
    [UnityTearDown] public IEnumerator After()
    {
        if(sourceVisual!=null)Object.DestroyImmediate(sourceVisual);if(baked!=null)Object.DestroyImmediate(baked);
        metric?.Clear();topology.Clear();Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output,"floor-cushion-departure-states.csv"),states);
        File.WriteAllLines(Path.Combine(Output,"floor-cushion-departure-gates.csv"),gates);
        File.WriteAllLines(Path.Combine(Output,"floor-cushion-departure-blockers.csv"),blocks);
        File.WriteAllLines(Path.Combine(Output,"floor-cushion-departure-skin.csv"),skinRows);
        fixture?.After();yield return RoomPlayModeSupport.WaitForPendingContactData();
    }

    [UnityTest,Timeout(120000)]
    public IEnumerator AcceptedCushionCycle_LastStandUp_DiagnosesAllDepartureGatesAndClosestSourceSkin()
    {
        yield return (IEnumerator)Call(fixture,"Prepare","SecondFloor_Level01");
        cat=Read<CatMovement>(fixture,"cat");
        activity=CatActivity.Registered.OfType<TowelNestActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId=="loft.floor-cushions");
        object[] args={activity,default(CatActivityStart),null};
        Assert.That((bool)Call(fixture,"FindReadyPose",args),Is.True,"The diagnostic must use a real accepted start: "+args[2]);
        yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
        Assert.That(activity.TryGetStartPose(cat,out _),Is.True);
        RoomPlayModeSupport.ProvisionNeeds();Assert.That(activity.TryStart(cat),Is.True);
        var animation=cat.GetComponent<CatActivityAnimation>();
        int complete=0,after=0,snapshots=0;bool stopped=false;
        Action<CatActivity> completed=a=>{if(a==activity)complete++;};CatActivity.Completed+=completed;
        float deadline=Time.realtimeSinceStartup+45;
        try
        {
            while((activity.IsRunning||after<3)&&Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();if(!activity.IsRunning)after++;
                float phase=Read<float>(animation,"phase");
                var support=cat.GetComponent<CatMeasuredSupportMotion>();float lift=support!=null?support.VisualLift:0;
                animation.TryReadNativeJumpStartOffset(cat.transform.rotation,out var offset);
                states.Add(Csv(Time.frameCount,animation.CurrentPose,phase,animation.IsNativeJump,activity.IsRunning,cat.transform.position,cat.transform.eulerAngles.y,offset,lift,complete));
                if(activity.IsRunning&&animation.CurrentPose==CatActivityPose.StandUp&&!animation.IsNativeJump&&phase>=.94f&&snapshots==0)
                { CaptureDeparture(animation,lift);snapshots++; }
                if(!stopped&&activity.IsWaitingForRestStop&&activity.RestingSeconds>=1.2f)
                {stopped=true;Assert.That(activity.RequestRestStop(),Is.True);}
            }
            Assert.That(activity.IsRunning,Is.False,"The naturally accepted cycle must finish or cancel within45seconds.");
            Assert.That(snapshots,Is.EqualTo(1),"No final source StandUp was observed; do not substitute an invented departure pose.");
            Assert.That(candidates.Any(v=>v.floor&&v.boundary),Is.True,"Need a real floor-clear departure candidate for source diagnosis.");
            yield return SourceProof(candidates.Where(v=>v.floor&&v.boundary).OrderBy(v=>(v.landing-preferred).sqrMagnitude).First());
            TestContext.WriteLine("Diagnostic only; completion="+complete+". A source rejection/cancellation is not a completed-cycle PASS.");
        }
        finally {CatActivity.Completed-=completed;if(activity.IsRunning)activity.CancelForTransition();}
    }

    void CaptureDeparture(CatActivityAnimation animation,float lift)
    {
        frame=Time.frameCount;sourceLift=lift;var visual=cat.GetComponentInChildren<CatBreedVisualTag>();breedId=visual.BreedId;
        entry=CatJumpClearanceCatalog.Load().Find(breedId);Assert.That(entry?.samples,Is.Not.Null);
        tagInRoot=cat.transform.worldToLocalMatrix*visual.transform.localToWorldMatrix;
        guard=Read<CatBodyGuard>(cat,"bodyGuard");solidFilter=typeof(CatBodyGuard).GetMethod("Solid",Hidden);penetrate=typeof(CatBodyGuard).GetMethod("Penetration",Hidden);
        solids=Object.FindObjectsByType<Collider>().Where(v=>v!=null&&(bool)solidFilter.Invoke(guard,new object[]{v})).ToArray();
        Assert.That(activity.TryGetPreparedStart(cat,out var accepted),Is.True);
        departure=Read<Transform>(activity,"nestPoint").position;preferred=Read<Transform>(activity,"floorPoint").position;preferred.y=accepted.Position.y;
        Vector3 rootBefore=cat.transform.position;Quaternion rotationBefore=cat.transform.rotation;
        var bones=cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        var positions=bones.Select(v=>v.localPosition).ToArray();var rotations=bones.Select(v=>v.localRotation).ToArray();var scales=bones.Select(v=>v.localScale).ToArray();
        Vector3 measured=activity.transform.TransformDirection(CatFurnitureJumpClearance.DownHeading(activity.StoreProductId));
        bool found=CatActivityStartResolver.GroundLanding(cat,departure,preferred,out var landing,out var heading,measured);
        Vector3 outward=Vector3.ProjectOnPlane(preferred-departure,Vector3.up);float distance=outward.magnitude;
        outward=distance>.0001f?outward.normalized:cat.transform.forward;
        var directions=new List<Vector3>{outward};measured.y=0;if(measured.sqrMagnitude>.0001f)directions.Add(measured.normalized);
        foreach(float angle in new[]{-15f,15f,-30f,30f,-45f,45f,-60f,60f,-75f,75f,-90f,90f})directions.Add(Quaternion.AngleAxis(angle,Vector3.up)*outward);
        var boundary=HomeRoomBoundary.FindFor(cat.gameObject.scene);bool productionDone=false;
        for(int d=0;d<directions.Count;d++)
        {
            bool skipExtensions=false;
            for(int extension=0;extension<=6;extension++)
            {
                var candidate=new Candidate{direction=d,extension=extension,heading=Quaternion.LookRotation(directions[d])};
                candidate.landing=departure+directions[d]*(distance+extension*.06f);candidate.landing.y=preferred.y;
                candidate.floor=cat.IsInteractionPoseClear(candidate.landing,candidate.heading);
                candidate.boundary=boundary==null||(candidate.landing.x>=boundary.MinimumXZ.x&&candidate.landing.x<=boundary.MaximumXZ.x&&candidate.landing.z>=boundary.MinimumXZ.y&&candidate.landing.z<=boundary.MaximumXZ.y);
                candidate.visited=!productionDone&&!skipExtensions;
                animation.TryReadNativeJumpStartOffset(candidate.heading,out candidate.offset);
                if(candidate.floor&&candidate.boundary)
                {
                    candidate.clear=CatJumpClearanceResolver.EndsClear(cat,departure,candidate.landing,candidate.heading,false,false,out candidate.rejection);
                    if(candidate.visited)
                    { if(candidate.clear)productionDone=true;else if(candidate.rejection.phase<=CatJumpMotion.Takeoff)skipExtensions=true; }
                    if(extension==0&&!candidate.clear)ReadBlockers(candidate);
                }
                candidates.Add(candidate);
                gates.Add(Csv(frame,d,extension,departure,preferred,candidate.landing,candidate.heading.eulerAngles.y,candidate.floor,candidate.boundary,candidate.visited,candidate.clear,
                    candidate.rejection.phase,candidate.rejection.root,candidate.offset,lift,found,landing,heading.eulerAngles.y));
            }
        }
        var closest=candidates.Where(v=>v.floor&&v.boundary).OrderBy(v=>(v.landing-preferred).sqrMagnitude).FirstOrDefault();
        if(closest!=null&&closest.extension!=0&&!closest.clear)ReadBlockers(closest);
        Assert.That(cat.transform.position,Is.EqualTo(rootBefore));Assert.That(cat.transform.rotation,Is.EqualTo(rotationBefore));
        for(int i=0;i<bones.Length;i++)
        {Assert.That(bones[i].localPosition,Is.EqualTo(positions[i]));Assert.That(bones[i].localRotation,Is.EqualTo(rotations[i]));Assert.That(bones[i].localScale,Is.EqualTo(scales[i]));}
    }

    (Matrix4x4 matrix,Vector3 correction,float scale) Mapping(Candidate candidate,float phase)
    {
        bool preparation=phase<=CatJumpMotion.Takeoff+.000001f;
        Vector3 root=preparation?departure:candidate.landing;
        Matrix4x4 rootMatrix=Matrix4x4.TRS(root,candidate.heading,cat.transform.lossyScale),matrix=rootMatrix*tagInRoot;
        Vector3 correction=rootMatrix.MultiplyVector(preparation?candidate.offset:Vector3.zero)+Vector3.up*(root.y-matrix.m13+.008f);
        float scale=Mathf.Max(((Vector3)matrix.GetColumn(0)).magnitude,Mathf.Max(((Vector3)matrix.GetColumn(1)).magnitude,((Vector3)matrix.GetColumn(2)).magnitude));
        return(matrix,correction,scale);
    }
    static CatBodyGuardCatalog.Probe World(CatBodyGuardCatalog.Probe value,Matrix4x4 matrix,Vector3 correction,float scale)
    {value.start=matrix.MultiplyPoint3x4(value.start)+correction;value.end=matrix.MultiplyPoint3x4(value.end)+correction;value.radius*=scale;return value;}
    float Depth(CatBodyGuardCatalog.Probe probe,Collider solid,out Vector3 normal)
    {object[] args={probe.start,probe.end,probe.radius,solid,Vector3.zero,0f};bool hit=(bool)penetrate.Invoke(guard,args);normal=(Vector3)args[4];return hit?(float)args[5]:0;}
    void ReadBlockers(Candidate candidate)
    {
        var sample=entry.samples.OrderBy(v=>Mathf.Abs(v.phase-candidate.rejection.phase)).First();var map=Mapping(candidate,sample.phase);
        Assert.That(sample.bodyCoverageVersion,Is.EqualTo(CatJumpClearanceCatalog.BodyCoverageVersion));Assert.That(sample.bodyCoverage.Length,Is.EqualTo(4));
        foreach(var region in sample.bodyCoverage)
        {
            Check(region.envelope,"envelope",-1);
            for(int index=0;index<region.parts.Length;index++)Check(region.parts[index],"part",index);
            void Check(CatBodyGuardCatalog.Probe raw,string level,int index)
            {
                var probe=World(raw,map.matrix,map.correction,map.scale);
                foreach(var solid in solids)
                {float depth=Depth(probe,solid,out var normal);if(depth>.015f)blocks.Add(Csv(frame,candidate.direction,candidate.extension,sample.phase,region.region,level,index,PathOf(solid.transform),solid.GetType().Name,depth,normal,probe.start,probe.end,probe.radius));}
            }
        }
    }

    IEnumerator SourceProof(Candidate candidate)
    {
        var catalog=CatBreedCatalog.Load();var breed=catalog.Find(breedId);var clip=entry.sourceClip;
        Assert.That(clip,Is.Not.Null);string asset=UnityEditor.AssetDatabase.GetAssetPath(clip),hash=UnityEditor.AssetDatabase.GetAssetDependencyHash(asset).ToString();
        var sample=entry.samples.OrderBy(v=>Mathf.Abs(v.phase-(candidate.clear?0:candidate.rejection.phase))).First();var map=Mapping(candidate,sample.phase);
        sourceVisual=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null,"QA remote floor cushion source geometry");sourceVisual.transform.position=new Vector3(0,-100,0);
        foreach(var renderer in sourceVisual.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        var animator=sourceVisual.GetComponentInChildren<Animator>();animator.enabled=false;var skin=sourceVisual.GetComponentInChildren<SkinnedMeshRenderer>();
        var p=animator.transform.localPosition;var q=animator.transform.localRotation;var s=animator.transform.localScale;
        clip.SampleAnimation(animator.gameObject,(clip.isLooping?Mathf.Repeat(sample.phase,1):sample.phase)*clip.length);
        animator.transform.localPosition=p;animator.transform.localRotation=q;animator.transform.localScale=s;
        skin.BakeMesh(baked,true);var vertices=baked.vertices;var points=new Vector3[vertices.Length];float minimum=float.PositiveInfinity;
        foreach(int index in breed.ContactVertexIndices){points[index]=sourceVisual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index]));minimum=Mathf.Min(minimum,points[index].y);}
        foreach(int index in breed.ContactVertexIndices)points[index]=map.matrix.MultiplyPoint3x4(points[index]-Vector3.up*minimum)+map.correction;
        var builder=Type.GetType("CatJumpClearanceBuilder, CatHome.Editor");Assert.That(builder,Is.Not.Null);
        var classify=builder.GetMethod("ClassifyBodyRegion",BindingFlags.Static|BindingFlags.Public);var weights=skin.sharedMesh.boneWeights;
        var groups=Enumerable.Repeat(-1,vertices.Length).ToArray();foreach(int index in breed.ContactVertexIndices)groups[index]=(int)classify.Invoke(null,new object[]{weights[index],skin.bones});
        var targets=new HashSet<Collider>();
        foreach(var region in sample.bodyCoverage)foreach(var raw in region.parts)
        {var probe=World(raw,map.matrix,map.correction,map.scale);foreach(var solid in solids)if(Depth(probe,solid,out _)>.015f)targets.Add(solid);}
        // A now-clear closest gate still gets an owner geometry witness.
        if(targets.Count==0)foreach(var solid in activity.GetComponentsInChildren<MeshCollider>())if(solid.enabled)targets.Add(solid);
        float deadline=Time.realtimeSinceStartup+20;int actualRows=0;
        foreach(var solid in targets.Take(8))
        {
            var maxima=new[]{new SkinMaximum(),new SkinMaximum(),new SkinMaximum(),new SkinMaximum(),new SkinMaximum()};
            var containment=Enumerable.Repeat(float.PositiveInfinity,4).ToArray();
            var coarse=new float[4];var parts=new float[4];
            for(int r=0;r<4;r++)
            {
                var region=sample.bodyCoverage.Single(v=>v.region==Regions[r]);var envelope=World(region.envelope,map.matrix,map.correction,map.scale);
                coarse[r]=Depth(envelope,solid,out _);var worldParts=region.parts.Select(v=>World(v,map.matrix,map.correction,map.scale)).ToArray();
                parts[r]=worldParts.Max(v=>Depth(v,solid,out _));
                foreach(int index in breed.ContactVertexIndices.Where(v=>groups[v]==r))
                    containment[r]=Mathf.Min(containment[r],worldParts.Max(v=>Containment(v,points[index])));
                Assert.That(containment[r],Is.GreaterThanOrEqualTo(-.00002f),"Source/profile containment mismatch "+Regions[r]);
            }
            foreach(int index in breed.ContactVertexIndices)
            {
                Vector3 point=points[index];if(!solid.bounds.Contains(point))continue;
                var hit=ActualDepth(solid,point);if(hit.depth<=0)continue;
                Include(maxima[0],index,point,hit);if(groups[index]>=0)Include(maxima[groups[index]+1],index,point,hit);
            }
            Write("all-skin",maxima[0],coarse.Max(),parts.Max(),containment.Min());
            for(int r=0;r<4;r++)Write(Regions[r],maxima[r+1],coarse[r],parts[r],containment[r]);
            Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Source proof stays bounded to eight colliders and one rejected sample.");
            yield return null;
            void Write(string region,SkinMaximum result,float broad,float fine,float contains)
            {skinRows.Add(Csv(frame,candidate.direction,candidate.extension,sample.phase,region,PathOf(solid.transform),broad,fine,result.count,result.depth,result.vertex,result.votes,result.point,result.closest,result.normal,result.triangle,contains,sourceLift,asset,hash,fine>.015f?(result.depth>.015f?"parts-and-skin-rejected":"parts-only-at-sampled-skin"):"region-parts-clear"));actualRows++;}
        }
        Assert.That(targets.Count,Is.LessThanOrEqualTo(8),"More than eight blockers require an explicitly wider diagnostic, not silent truncation.");
        Assert.That(actualRows,Is.GreaterThanOrEqualTo(5));
        Assert.That(UnityEditor.AssetDatabase.GetAssetDependencyHash(asset).ToString(),Is.EqualTo(hash));
    }
    static float Containment(CatBodyGuardCatalog.Probe probe,Vector3 point)
    {Vector3 axis=probe.end-probe.start;float t=axis.sqrMagnitude>1e-12f?Mathf.Clamp01(Vector3.Dot(point-probe.start,axis)/axis.sqrMagnitude):0;return probe.radius-Vector3.Distance(point,probe.start+axis*t);}
    (float depth,int votes,Vector3 closest,Vector3 normal,int triangle) ActualDepth(Collider solid,Vector3 point)
    {
        if(solid is MeshCollider mesh)
        {
            var data=topology.Get(mesh.sharedMesh);int votes=0;bool old=Physics.queriesHitBackfaces;
            try{Physics.queriesHitBackfaces=true;foreach(var direction in Rays)
            {if(!mesh.Raycast(new Ray(point,direction),out var ray,5))continue;int t=ray.triangleIndex*3;if(t<0||t+2>=data.triangles.Length)continue;
            Vector3 a=mesh.transform.TransformPoint(data.vertices[data.triangles[t]]),b=mesh.transform.TransformPoint(data.vertices[data.triangles[t+1]]),c=mesh.transform.TransformPoint(data.vertices[data.triangles[t+2]]);
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),direction)>0)votes++;}}
            finally{Physics.queriesHitBackfaces=old;}
            if(votes<4)return default;var hit=metric.Measure(mesh.sharedMesh,mesh.transform,point);return(hit.distance,votes,hit.point,hit.normal,hit.triangle);
        }
        if(solid is BoxCollider box)
        {
            Vector3 local=box.transform.InverseTransformPoint(point)-box.center,half=box.size*.5f;
            if(Mathf.Abs(local.x)>half.x||Mathf.Abs(local.y)>half.y||Mathf.Abs(local.z)>half.z)return default;
            Vector3 scale=box.transform.lossyScale,gap=half-new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z));
            float depth=Mathf.Min(gap.x*Mathf.Abs(scale.x),Mathf.Min(gap.y*Mathf.Abs(scale.y),gap.z*Mathf.Abs(scale.z)));return(depth,6,Vector3.zero,Vector3.zero,-1);
        }
        if(solid is SphereCollider sphere)
        {Vector3 centre=sphere.transform.TransformPoint(sphere.center),scale=sphere.transform.lossyScale;float radius=sphere.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Max(Mathf.Abs(scale.y),Mathf.Abs(scale.z)));return(Mathf.Max(0,radius-Vector3.Distance(point,centre)),6,Vector3.zero,Vector3.zero,-1);}
        Assert.Fail("Unsupported blocker geometry must not become zero skin penetration: "+solid.GetType().Name);return default;
    }
    static void Include(SkinMaximum value,int vertex,Vector3 point,(float depth,int votes,Vector3 closest,Vector3 normal,int triangle) hit)
    {value.count++;if(value.vertex>=0&&hit.depth<=value.depth)return;value.vertex=vertex;value.point=point;value.depth=hit.depth;value.votes=hit.votes;value.closest=hit.closest;value.normal=hit.normal;value.triangle=hit.triangle;}
    static string PathOf(Transform value){string path=value.name;while(value.parent!=null){value=value.parent;path=value.name+"/"+path;}return path;}
}
#endif
