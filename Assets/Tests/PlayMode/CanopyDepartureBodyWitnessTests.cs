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

// Pure source geometry diagnosis, never a forced successful KnockOff activity.
// Three measured departure headings use the actual final StandUp centering.
// Every unchanged native source end sample is compared with all four capsules.
public sealed class CanopyDepartureBodyWitnessTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly string[] Regions={"pelvis","chest","neck","head"};
    PreparedInteractionStartTests fixture;
    GameObject sourceVisual;
    Mesh baked;
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();
    QaExactMeshContact metric;
    readonly List<string> rows=new List<string>(),rays=new List<string>(),gates=new List<string>();
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
    static readonly Vector3[] Directions={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,
        new Vector3(.019f,.023f,1).normalized,-new Vector3(.013f,1,.027f).normalized,
        -new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
    static T Read<T>(object o,string name)=>(T)o.GetType().GetField(name,Private).GetValue(o);
    static string Csv(params object[] values)=>string.Join(",",values.Select(v=>"\""+(v is Vector3 p?FormattableString.Invariant($"{p.x:R};{p.y:R};{p.z:R}"):
        v is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):v?.ToString()??"").Replace("\"","\"\"")+"\""));
    sealed class Maximum{public int inside,ambiguous,vertex=-1,votes;public float depth;public Vector3 point;public QaExactMeshContact.Hit exact;}
    [SetUp]public void Before()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();metric=new QaExactMeshContact(topology);baked=new Mesh();
        rows.Add("candidate,pose,phase,root,yaw,region,capsuleDepth,capsuleRejected,minActualContainment,insideVertices,ambiguousVertices,maxExactDepth,skinVertex,votes,skinPoint,closestSurface,triangle,submesh,material,classification");
        gates.Add("candidate,departureRoot,yaw,floorClear,measuredStartOffset,landing,breed,sourceClip,sourceHash");
        rays.Add("candidate,phase,group,vertex,direction,hit,distance,outwardDot");
    }
    [TearDown]public void After()
    {
        if(sourceVisual!=null)Object.DestroyImmediate(sourceVisual);if(baked!=null)Object.DestroyImmediate(baked);
        metric?.Clear();topology.Clear();Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output,"canopy-departure-body.csv"),rows);
        File.WriteAllLines(Path.Combine(Output,"canopy-departure-body-gates.csv"),gates);
        File.WriteAllLines(Path.Combine(Output,"canopy-departure-body-rays.csv"),rays);fixture?.After();
    }
    [UnityTest,Timeout(150000)]public IEnumerator ThreeDepartureHeadings_CompareMeasuredNativeTrunkEnvelopeWithActualSkin()
    {
        yield return (IEnumerator)Call(fixture,"Prepare","Bedroom_Level01");
        var cat=Read<CatMovement>(fixture,"cat");yield return QaBreedReadiness.WaitForSelected(cat,"oriental-shorthair");
        var activity=CatActivity.Registered.OfType<CanopyNapActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BedroomStarCanopyId);
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>();var catalog=CatBreedCatalog.Load();var breed=catalog.Find(tag.BreedId);
        var entry=CatJumpClearanceCatalog.Load().Find(tag.BreedId);Assert.That(entry?.samples,Is.Not.Null);
        var builder=Type.GetType("CatJumpClearanceBuilder, CatHome.Editor");Assert.That(builder,Is.Not.Null);
        var classify=builder.GetMethod("ClassifyBodyRegion",BindingFlags.Static|BindingFlags.Public);
        var clip=entry.sourceClip;
        Assert.That(clip,Is.Not.Null);string asset=UnityEditor.AssetDatabase.GetAssetPath(clip),hash=UnityEditor.AssetDatabase.GetAssetDependencyHash(asset).ToString();
        var solid=activity.GetComponentsInChildren<MeshCollider>().Single(c=>c.enabled&&!c.isTrigger&&c.name=="BedroomStarCanopy_PremiumModel");
        Assert.That(solid.convex,Is.False);Assert.That(solid.sharedMesh,Is.SameAs(solid.GetComponent<MeshFilter>().sharedMesh));
        var guard=Read<CatBodyGuard>(cat,"bodyGuard");var penetration=typeof(CatBodyGuard).GetMethod("Penetration",Private);
        Vector3 liveRoot=cat.transform.position;Quaternion liveHeading=cat.transform.rotation;
        var liveBones=cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        sourceVisual=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null,"QA remote native departure geometry");
        sourceVisual.transform.position=new Vector3(0,-100,0);
        foreach(var renderer in sourceVisual.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        Assert.That(sourceVisual.GetComponentInChildren<CatMovement>(),Is.Null);
        var animator=sourceVisual.GetComponentInChildren<Animator>();animator.enabled=false;
        var skin=sourceVisual.GetComponentInChildren<SkinnedMeshRenderer>();var transforms=sourceVisual.GetComponentsInChildren<Transform>(true);
        var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
        var basePosition=animator.transform.localPosition;var baseRotation=animator.transform.localRotation;var baseScale=animator.transform.localScale;
        var weights=skin.sharedMesh.boneWeights;var groups=new int[weights.Length];
        foreach(int i in breed.ContactVertexIndices)groups[i]=(int)classify.Invoke(null,new object[]{weights[i],skin.bones});
        var ids=new[]{180,165,195};var yaw=new[]{180f,165f,195f};
        Vector3 departure=activity.NestPoint.position;
        Vector3 measuredOffset=new Vector3(.00399667025f,0,-.147938251f);
        // Exact last supported StandUp offset from the preceding native witness
        // (frame207, phase.969697, zero carried lift). Root/source unchanged.
        Assert.That(Vector3.Distance(departure,new Vector3(-.28f,.194618613f,2.08000016f)),Is.LessThan(.00001f));
        Matrix4x4 tagInRoot=cat.transform.worldToLocalMatrix*tag.transform.localToWorldMatrix;
        int measured=0;float deadline=Time.realtimeSinceStartup+110;
        for(int c=0;c<3;c++)
        {
            Quaternion heading=Quaternion.Euler(0,yaw[c],0);
            Vector3 landing=departure+heading*Vector3.forward*.62f;landing.y=.05f;
            bool floor=cat.IsInteractionPoseClear(landing,heading);Assert.That(floor,Is.True);
            gates.Add(Csv(ids[c],departure,yaw[c],floor,measuredOffset,landing,tag.BreedId,asset,hash));
            foreach(var sample in entry.samples.OrderBy(s=>s.phase))
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
                var livePos=liveBones.Select(t=>t.localPosition).ToArray();var liveRot=liveBones.Select(t=>t.localRotation).ToArray();
                for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
                float time=(clip.isLooping?Mathf.Repeat(sample.phase,1):sample.phase)*clip.length;clip.SampleAnimation(animator.gameObject,time);
                animator.transform.localPosition=basePosition;animator.transform.localRotation=baseRotation;animator.transform.localScale=baseScale;
                skin.BakeMesh(baked,true);var vertices=baked.vertices;var local=new Vector3[vertices.Length];float minY=float.PositiveInfinity;
                foreach(int index in breed.ContactVertexIndices){local[index]=sourceVisual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index]));minY=Mathf.Min(minY,local[index].y);}
                bool preparation=sample.phase<=CatJumpMotion.Takeoff+.000001f;
                Vector3 root=preparation?departure:landing;
                Matrix4x4 rootMatrix=Matrix4x4.TRS(root,heading,cat.transform.lossyScale),matrix=rootMatrix*tagInRoot;
                Vector3 lift=rootMatrix.MultiplyVector(preparation?measuredOffset:Vector3.zero)+Vector3.up*(root.y-matrix.m13+.008f);
                var probes=new CatBodyGuardCatalog.Probe[4];var depths=new float[4];var contains=Enumerable.Repeat(float.PositiveInfinity,4).ToArray();
                var maxima=new[]{new Maximum(),new Maximum(),new Maximum(),new Maximum(),new Maximum()};
                float scale=Mathf.Max(((Vector3)matrix.GetColumn(0)).magnitude,Mathf.Max(((Vector3)matrix.GetColumn(1)).magnitude,((Vector3)matrix.GetColumn(2)).magnitude));
                for(int r=0;r<4;r++)
                {
                    var p=sample.probes.Single(v=>v.region==Regions[r]);p.start=matrix.MultiplyPoint3x4(p.start)+lift;p.end=matrix.MultiplyPoint3x4(p.end)+lift;p.radius*=scale;probes[r]=p;
                    object[] args={p.start,p.end,p.radius,solid,Vector3.zero,0f};if((bool)penetration.Invoke(guard,args))depths[r]=(float)args[5];
                }
                foreach(int index in breed.ContactVertexIndices)
                {
                    Vector3 point=matrix.MultiplyPoint3x4(local[index]-Vector3.up*minY)+lift;int group=groups[index];
                    if(group>=0)
                    {
                        var p=probes[group];Vector3 axis=p.end-p.start;float t=axis.sqrMagnitude>1e-12f?Mathf.Clamp01(Vector3.Dot(point-p.start,axis)/axis.sqrMagnitude):0;
                        contains[group]=Mathf.Min(contains[group],p.radius-Vector3.Distance(point,p.start+t*axis));
                    }
                    if(!solid.bounds.Contains(point))continue;int votes=Votes(solid,point,null);
                    if(votes>0&&votes<4){maxima[0].ambiguous++;if(group>=0)maxima[group+1].ambiguous++;}
                    if(votes<4)continue;var exact=metric.Measure(solid.sharedMesh,solid.transform,point);
                    Include(maxima[0],index,point,votes,exact);if(group>=0)Include(maxima[group+1],index,point,votes,exact);
                }
                Write("all-skin",maxima[0],depths.Max(),contains.Min());
                for(int r=0;r<4;r++){Write(Regions[r],maxima[r+1],depths[r],contains[r]);Assert.That(contains[r],Is.GreaterThanOrEqualTo(.0018f),"Native catalog/source skin mismatch in "+Regions[r]);}
                for(int i=0;i<liveBones.Length;i++){Assert.That(liveBones[i].localPosition,Is.EqualTo(livePos[i]));Assert.That(liveBones[i].localRotation,Is.EqualTo(liveRot[i]));}
                Assert.That(CatActivity.Active,Is.Null);Assert.That(activity.IsRunning,Is.False);measured++;yield return null;
                Assert.That(Vector3.Distance(cat.transform.position,liveRoot),Is.LessThan(.002f));Assert.That(Quaternion.Angle(cat.transform.rotation,liveHeading),Is.LessThan(.001f));
                void Write(string group,Maximum result,float capsuleDepth,float containment)
                {
                    string material="";var materials=solid.GetComponent<Renderer>().sharedMaterials;
                    if(result.vertex>=0&&result.exact.submesh>=0&&result.exact.submesh<materials.Length)material=materials[result.exact.submesh].name;
                    string resultKind=capsuleDepth>.015f?(result.depth>.015f?"capsule-and-real-skin-over15mm":"capsule-only-over15mm-at-sampled-vertices"):"capsule-not-rejected";
                    rows.Add(Csv(ids[c],"NativeJump",sample.phase,root,yaw[c],group,capsuleDepth,capsuleDepth>.015f,containment,result.inside,result.ambiguous,result.depth,result.vertex,result.votes,result.point,result.exact.point,result.exact.triangle,result.exact.submesh,material,resultKind));
                    if(result.vertex>=0)Votes(solid,result.point,(direction,hit,distance,dot)=>rays.Add(Csv(ids[c],sample.phase,group,result.vertex,direction,hit,distance,dot)));
                }
            }
        }
        Assert.That(measured,Is.EqualTo(3*entry.samples.Length));Assert.That(UnityEditor.AssetDatabase.GetAssetDependencyHash(asset).ToString(),Is.EqualTo(hash));
        rows.Add(Csv("diagnostic-only",measured,"No runtime source selection, forced readiness, or contact/completion PASS."));
    }
    static void Include(Maximum value,int index,Vector3 point,int votes,QaExactMeshContact.Hit hit)
    {value.inside++;if(value.vertex>=0&&hit.distance<=value.depth)return;value.vertex=index;value.point=point;value.depth=hit.distance;value.votes=votes;value.exact=hit;}
    int Votes(MeshCollider collider,Vector3 point,Action<int,bool,float,float> log)
    {
        var data=topology.Get(collider.sharedMesh);bool before=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;int votes=0;
        try{for(int n=0;n<Directions.Length;n++)
        {bool hit=collider.Raycast(new Ray(point,Directions[n]),out var ray,5);float dot=0;int t=ray.triangleIndex*3;
        if(hit&&t>=0&&t+2<data.triangles.Length){Vector3 a=collider.transform.TransformPoint(data.vertices[data.triangles[t]]),b=collider.transform.TransformPoint(data.vertices[data.triangles[t+1]]),c=collider.transform.TransformPoint(data.vertices[data.triangles[t+2]]);dot=Vector3.Dot(Vector3.Cross(b-a,c-a).normalized,Directions[n]);if(dot>0)votes++;}
        log?.Invoke(n,hit,hit?ray.distance:float.NaN,dot);}}
        finally{Physics.queriesHitBackfaces=before;}return votes;
    }
}
#endif
