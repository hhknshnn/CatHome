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

// Diagnostic only. A separate source-prefab instance supplies exact native skin
// vertices. No refused activity is started and the gameplay actor is never posed.
public sealed class CanopySourceSkinWitnessTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;
    GameObject sourceVisual;
    Mesh baked;
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();
    QaExactMeshContact metric;
    readonly List<string> rows=new List<string>(),witnesses=new List<string>(),gates=new List<string>();
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
    static readonly Vector3[] Directions={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,
        new Vector3(.019f,.023f,1).normalized,-new Vector3(.013f,1,.027f).normalized,
        -new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
    static T Read<T>(object o,string name)=>(T)o.GetType().GetField(name,Private).GetValue(o);
    static string Csv(params object[] values)=>string.Join(",",values.Select(v=>"\""+(v is Vector3 p?FormattableString.Invariant($"{p.x:R};{p.y:R};{p.z:R}"):
        v is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):v?.ToString()??"").Replace("\"","\"\"")+"\""));
    sealed class Maximum
    {public int inside,ambiguous,vertex=-1,votes;public float depth;public Vector3 point;public QaExactMeshContact.Hit exact;}
    [SetUp]public void Before()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();metric=new QaExactMeshContact(topology);baked=new Mesh();
        rows.Add("candidate,sourcePhase,root,yaw,region,collider,capsuleDepth,capsuleRejected,minActualContainment,actualInsideVertices,ambiguousVertices,maxActualExactDepth,skinVertex,votes,skinPoint,closestSurface,triangle,submesh,material,classification");
        gates.Add("candidate,playerRoot,yaw,currentClear,sourceRejected,firstPhase,landing,actualBreed,sourceClip,sourceHash");
        witnesses.Add("candidate,phase,group,vertex,direction,hit,distance,outwardDot");
    }
    [TearDown]public void After()
    {
        if(sourceVisual!=null)Object.DestroyImmediate(sourceVisual);if(baked!=null)Object.DestroyImmediate(baked);
        metric?.Clear();topology.Clear();
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output,"canopy-source-skin.csv"),rows);
        File.WriteAllLines(Path.Combine(Output,"canopy-source-skin-gates.csv"),gates);
        File.WriteAllLines(Path.Combine(Output,"canopy-source-skin-rays.csv"),witnesses);
        fixture?.After();
    }
    [UnityTest,Timeout(180000)]public IEnumerator ThreeRejectedPlayerStances_CompareNativeHeadEnvelopeToActualUnmodifiedSourceSkin()
    {
        yield return (IEnumerator)Call(fixture,"Prepare","Bedroom_Level01");
        var cat=Read<CatMovement>(fixture,"cat");
        var activity=CatActivity.Registered.OfType<CanopyNapActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BedroomStarCanopyId);
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>();Assert.That(tag.BreedId,Is.EqualTo("oriental-shorthair"));
        var catalog=CatBreedCatalog.Load();var breed=catalog.Find(tag.BreedId);
        var entry=CatJumpClearanceCatalog.Load().Find(tag.BreedId);Assert.That(entry,Is.Not.Null);
        Assert.That(entry.sourceClip,Is.Not.Null);Assert.That(entry.sourceClip.name.EndsWith("|Jump",StringComparison.Ordinal),Is.True);
        string asset=UnityEditor.AssetDatabase.GetAssetPath(entry.sourceClip);
        string sourceHash=UnityEditor.AssetDatabase.GetAssetDependencyHash(asset).ToString();
        var solid=activity.GetComponentsInChildren<MeshCollider>().Single(c=>c.enabled&&!c.isTrigger&&c.name=="BedroomStarCanopy_PremiumModel");
        Assert.That(solid.convex,Is.False);Assert.That(solid.sharedMesh,Is.SameAs(solid.GetComponent<MeshFilter>().sharedMesh));
        var guard=Read<CatBodyGuard>(cat,"bodyGuard");
        var penetration=typeof(CatBodyGuard).GetMethod("Penetration",Private);Assert.That(penetration,Is.Not.Null);
        var regionType=Type.GetType("CatPawReachBuilder, CatHome.Editor");Assert.That(regionType,Is.Not.Null);
        var classify=regionType.GetMethod("Region",BindingFlags.Static|BindingFlags.NonPublic);Assert.That(classify,Is.Not.Null);
        Vector3 originalRoot=cat.transform.position;Quaternion originalRotation=cat.transform.rotation;
        var liveAnimator=cat.GetComponentInChildren<Animator>();
        var liveBones=liveAnimator.GetComponentsInChildren<Transform>(true);
        // Factory normalization is exactly the source catalog's authoring path.
        // The copy stays remote, has no CatMovement/activity, and never renders.
        sourceVisual=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null,"QA remote native canopy skin source");
        sourceVisual.transform.position=new Vector3(0,-100,0);
        foreach(var renderer in sourceVisual.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        Assert.That(sourceVisual.GetComponentInChildren<CatMovement>(),Is.Null);
        var animator=sourceVisual.GetComponentInChildren<Animator>();animator.enabled=false;
        var skin=sourceVisual.GetComponentInChildren<SkinnedMeshRenderer>();
        var transforms=sourceVisual.GetComponentsInChildren<Transform>(true);
        var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
        var basePosition=animator.transform.localPosition;var baseRotation=animator.transform.localRotation;var baseScale=animator.transform.localScale;
        var weights=skin.sharedMesh.boneWeights;var groups=new int[weights.Length];
        foreach(int i in breed.ContactVertexIndices)groups[i]=(int)classify.Invoke(null,new object[]{weights[i],skin.bones});
        Matrix4x4 tagInRoot=cat.transform.worldToLocalMatrix*tag.transform.localToWorldMatrix;
        Vector3 zero=tagInRoot.MultiplyPoint3x4(entry.sourceZeroCentre),landing=activity.NestPoint.position;
        var ids=new[]{486,498,950};
        var starts=new[]{new Vector3(-.28f,.05f,1.46f),new Vector3(-.265764952f,.05f,1.5131259f),new Vector3(-.487674057f,.05f,1.5156461f)};
        var headings=new[]{0f,13.5615215f,50.202877f};
        int measurements=0;var deadline=Time.realtimeSinceStartup+140;
        for(int candidate=0;candidate<ids.Length;candidate++)
        {
            Quaternion heading=Quaternion.Euler(0,headings[candidate],0);
            bool current=cat.IsInteractionPoseClear(starts[candidate],heading);
            bool clear=CatJumpClearanceResolver.EndsClear(cat,starts[candidate],landing,heading,true,true,out var rejection);
            gates.Add(Csv(ids[candidate],starts[candidate],headings[candidate],current,!clear,rejection.phase,landing,tag.BreedId,asset,sourceHash));
            Assert.That(current,Is.True,"Known diagnostic stance must remain physically legal.");
            Assert.That(clear,Is.False,"This witness must reproduce a current source-envelope rejection.");
            foreach(var sample in entry.samples.OrderBy(s=>s.phase))
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Bounded native source/mesh measurement");
                // Assert each synchronous operation leaves every live bone and
                // root byte-for-byte alone; normal animation may run on yields.
                var livePos=liveBones.Select(t=>t.localPosition).ToArray();var liveRot=liveBones.Select(t=>t.localRotation).ToArray();
                for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
                entry.sourceClip.SampleAnimation(animator.gameObject,entry.sourceClip.length*sample.phase);
                animator.transform.localPosition=basePosition;animator.transform.localRotation=baseRotation;animator.transform.localScale=baseScale;
                skin.BakeMesh(baked,true);var vertices=baked.vertices;var local=new Vector3[vertices.Length];float minY=float.PositiveInfinity;
                foreach(int index in breed.ContactVertexIndices)
                {local[index]=sourceVisual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index]));minY=Mathf.Min(minY,local[index].y);}
                bool preparation=sample.phase<=CatJumpMotion.Takeoff+.000001f;
                Vector3 root=preparation?starts[candidate]:landing;
                Matrix4x4 rootMatrix=Matrix4x4.TRS(root,heading,cat.transform.lossyScale),matrix=rootMatrix*tagInRoot;
                Vector3 correction=rootMatrix.MultiplyVector(preparation?Vector3.zero:new Vector3(-zero.x,0,-zero.z));
                correction+=Vector3.up*(root.y-matrix.m13+.008f);
                float scale=Mathf.Max(((Vector3)matrix.GetColumn(0)).magnitude,Mathf.Max(((Vector3)matrix.GetColumn(1)).magnitude,((Vector3)matrix.GetColumn(2)).magnitude));
                var raw=sample.probes.Single(p=>p.region=="head");var probe=raw;
                probe.start=matrix.MultiplyPoint3x4(raw.start)+correction;probe.end=matrix.MultiplyPoint3x4(raw.end)+correction;probe.radius*=scale;
                object[] penetrationArgs={probe.start,probe.end,probe.radius,solid,Vector3.zero,0f};
                bool overlap=(bool)penetration.Invoke(guard,penetrationArgs);float capsuleDepth=overlap?(float)penetrationArgs[5]:0f;
                var all=new Maximum();var head=new Maximum();float containment=float.PositiveInfinity;
                foreach(int index in breed.ContactVertexIndices)
                {
                    Vector3 point=matrix.MultiplyPoint3x4(local[index]-Vector3.up*minY)+correction;
                    if(groups[index]==3)
                    {
                        Vector3 axis=probe.end-probe.start;
                        float t=axis.sqrMagnitude>1e-12f?Mathf.Clamp01(Vector3.Dot(point-probe.start,axis)/axis.sqrMagnitude):0;
                        containment=Mathf.Min(containment,probe.radius-Vector3.Distance(point,probe.start+t*axis));
                    }
                    if(!solid.bounds.Contains(point))continue;
                    int votes=InsideVotes(solid,point,null);
                    if(votes>0&&votes<4){all.ambiguous++;if(groups[index]==3)head.ambiguous++;}
                    if(votes<4)continue;
                    var hit=metric.Measure(solid.sharedMesh,solid.transform,point);Include(all,index,point,votes,hit);if(groups[index]==3)Include(head,index,point,votes,hit);
                }
                Write("all-skin",all);Write("head",head);
                // This checks the independent bake and world mapping against
                // the catalog's original 2mm skin shell, not a looser query.
                Assert.That(containment,Is.GreaterThanOrEqualTo(.0018f),"Source bake/catalog mapping mismatch; do not interpret an invalid witness.");
                for(int i=0;i<liveBones.Length;i++){Assert.That(liveBones[i].localPosition,Is.EqualTo(livePos[i]));Assert.That(liveBones[i].localRotation,Is.EqualTo(liveRot[i]));}
                Assert.That(CatActivity.Active,Is.Null);Assert.That(activity.IsRunning,Is.False);
                measurements++;yield return null;
                Assert.That(Vector3.Distance(cat.transform.position,originalRoot),Is.LessThan(.002f));Assert.That(Quaternion.Angle(cat.transform.rotation,originalRotation),Is.LessThan(.001f));
                void Write(string group,Maximum result)
                {
                    string material="";var materials=solid.GetComponent<Renderer>().sharedMaterials;
                    if(result.vertex>=0&&result.exact.submesh>=0&&result.exact.submesh<materials.Length)material=materials[result.exact.submesh].name;
                    string classification=capsuleDepth>.015f?(result.depth>.015f?"capsule-and-real-skin-over15mm":"capsule-only-over15mm-at-sampled-vertices"):"capsule-not-rejected";
                    rows.Add(Csv(ids[candidate],sample.phase,root,headings[candidate],group,solid.name,capsuleDepth,capsuleDepth>.015f,containment,result.inside,result.ambiguous,result.depth,result.vertex,result.votes,result.point,result.exact.point,result.exact.triangle,result.exact.submesh,material,classification));
                    if(result.vertex>=0)InsideVotes(solid,result.point,(direction,hit,distance,dot)=>witnesses.Add(Csv(ids[candidate],sample.phase,group,result.vertex,direction,hit,distance,dot)));
                }
            }
        }
        Assert.That(measurements,Is.EqualTo(3*entry.samples.Length));
        Assert.That(UnityEditor.AssetDatabase.GetAssetDependencyHash(asset).ToString(),Is.EqualTo(sourceHash));
        rows.Add(Csv("diagnostic-only",measurements,"No readiness/skin/completion PASS is implied; classify native evidence before changing any runtime gate."));
    }
    static void Include(Maximum value,int index,Vector3 point,int votes,QaExactMeshContact.Hit hit)
    {value.inside++;if(value.vertex>=0&&hit.distance<=value.depth)return;value.vertex=index;value.point=point;value.depth=hit.distance;value.votes=votes;value.exact=hit;}
    int InsideVotes(MeshCollider collider,Vector3 point,Action<int,bool,float,float> log)
    {
        var data=topology.Get(collider.sharedMesh);bool before=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;int votes=0;
        try
        {
            for(int n=0;n<Directions.Length;n++)
            {
                bool hit=collider.Raycast(new Ray(point,Directions[n]),out var ray,5);float dot=0;int triangle=ray.triangleIndex*3;
                if(hit&&triangle>=0&&triangle+2<data.triangles.Length)
                {
                    Vector3 a=collider.transform.TransformPoint(data.vertices[data.triangles[triangle]]),b=collider.transform.TransformPoint(data.vertices[data.triangles[triangle+1]]),c=collider.transform.TransformPoint(data.vertices[data.triangles[triangle+2]]);
                    dot=Vector3.Dot(Vector3.Cross(b-a,c-a).normalized,Directions[n]);if(dot>0)votes++;
                }
                log?.Invoke(n,hit,hit?ray.distance:float.NaN,dot);
            }
        }
        finally{Physics.queriesHitBackfaces=before;}
        return votes;
    }
}
#endif
