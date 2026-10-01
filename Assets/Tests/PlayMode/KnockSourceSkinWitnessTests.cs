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
// Two mathematically reachable Paw plans have zero chest bend, so the entire
// unchanged native clip can be compared directly with all four source capsules.
public sealed class KnockSourceSkinWitnessTests
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
        rows.Add("candidate,pose,phase,root,yaw,region,capsuleDepth,capsuleRejected,minActualContainment,insideVertices,ambiguousVertices,maxExactDepth,skinVertex,votes,skinPoint,closestSurface,triangle,submesh,material,actualArmMargin,classification");
        gates.Add("candidate,playerRoot,yaw,currentClear,mathMargin,contactPhase,pitch,publicSourceReady,target,breed,sourceClip,sourceHash");
        rays.Add("candidate,phase,group,vertex,direction,hit,distance,outwardDot");
    }
    [TearDown]public void After()
    {
        if(sourceVisual!=null)Object.DestroyImmediate(sourceVisual);if(baked!=null)Object.DestroyImmediate(baked);
        metric?.Clear();topology.Clear();Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output,"knock-source-skin.csv"),rows);
        File.WriteAllLines(Path.Combine(Output,"knock-source-skin-gates.csv"),gates);
        File.WriteAllLines(Path.Combine(Output,"knock-source-skin-rays.csv"),rays);fixture?.After();
    }
    [UnityTest,Timeout(150000)]public IEnumerator TwoReachableRejectedPawStances_CompareFourSourceCapsulesWithActualSkin()
    {
        yield return (IEnumerator)Call(fixture,"Prepare","Balcony_Level01");
        var cat=Read<CatMovement>(fixture,"cat");CatBreedService.Select("russian-blue");yield return QaBreedReadiness.WaitForSelected(cat,"russian-blue");
        var activity=CatActivity.Registered.OfType<KnockOffActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BalconySideTableId);
        Assert.That(activity.PerchPoint,Is.Null);
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>();var catalog=CatBreedCatalog.Load();var breed=catalog.Find(tag.BreedId);
        var entry=CatPawReachCatalog.Load().Find(tag.BreedId,CatActivityPose.Paw);Assert.That(entry?.samples,Is.Not.Null);
        var builder=Type.GetType("CatPawReachBuilder, CatHome.Editor");Assert.That(builder,Is.Not.Null);
        var classify=builder.GetMethod("Region",BindingFlags.Static|BindingFlags.NonPublic);
        var clip=(AnimationClip)builder.GetMethod("StateClip",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{catalog.GameplayController,entry.stateName});
        Assert.That(clip,Is.Not.Null);string asset=UnityEditor.AssetDatabase.GetAssetPath(clip),hash=UnityEditor.AssetDatabase.GetAssetDependencyHash(asset).ToString();
        var solid=activity.GetComponentsInChildren<MeshCollider>().Single(c=>c.enabled&&!c.isTrigger&&c.name=="BalconySideTable_PremiumModel");
        Assert.That(solid.convex,Is.False);Assert.That(solid.sharedMesh,Is.SameAs(solid.GetComponent<MeshFilter>().sharedMesh));
        var guard=Read<CatBodyGuard>(cat,"bodyGuard");var penetration=typeof(CatBodyGuard).GetMethod("Penetration",Private);
        Vector3 liveRoot=cat.transform.position;Quaternion liveHeading=cat.transform.rotation;
        var liveBones=cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        sourceVisual=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null,"QA remote original Paw geometry");
        sourceVisual.transform.position=new Vector3(0,-100,0);
        foreach(var renderer in sourceVisual.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        Assert.That(sourceVisual.GetComponentInChildren<CatMovement>(),Is.Null);
        var animator=sourceVisual.GetComponentInChildren<Animator>();animator.enabled=false;
        var skin=sourceVisual.GetComponentInChildren<SkinnedMeshRenderer>();var transforms=sourceVisual.GetComponentsInChildren<Transform>(true);
        var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
        var basePosition=animator.transform.localPosition;var baseRotation=animator.transform.localRotation;var baseScale=animator.transform.localScale;
        var weights=skin.sharedMesh.boneWeights;var groups=new int[weights.Length];
        foreach(int i in breed.ContactVertexIndices)groups[i]=(int)classify.Invoke(null,new object[]{weights[i],skin.bones});
        var arm=CatBreedVisualFactory.FindDescendant(sourceVisual.transform,"DEF-upper_arm.L");
        var fore=CatBreedVisualFactory.FindDescendant(sourceVisual.transform,"DEF-forearm.L");
        var hand=CatBreedVisualFactory.FindDescendant(sourceVisual.transform,"DEF-hand.L");
        Assert.That(arm!=null&&fore!=null&&hand!=null,Is.True,"Actual source arm chain");
        var ids=new[]{126,16};var starts=new[]{new Vector3(-2.74492979f,.05f,-.3049999f),new Vector3(-3.01339769f,.05f,-.149999857f)};
        var yaw=new[]{244.254883f,184.719849f};var phases=new[]{.32f,.52f};
        var targets=new[]{new Vector3(-3.01212049f,.369f,-.704939663f),new Vector3(-3.05280471f,.369f,-.6909657f)};
        Matrix4x4 tagInRoot=cat.transform.worldToLocalMatrix*tag.transform.localToWorldMatrix;
        int measured=0;float deadline=Time.realtimeSinceStartup+110;
        for(int c=0;c<2;c++)
        {
            Quaternion heading=Quaternion.Euler(0,yaw[c],0);Matrix4x4 matrix=Matrix4x4.TRS(starts[c],heading,cat.transform.lossyScale)*tagInRoot;
            Vector3 lift=Vector3.up*CatPawReachCatalog.GroundClearance;
            bool clear=cat.IsInteractionPoseClear(starts[c],heading);
            bool sourceReady=CatPawReachResolver.TryResolveAt(cat,starts[c],heading,targets[c],true,CatActivityPose.Paw,out _);
            var contact=entry.samples.Single(s=>Mathf.Abs(s.phase-phases[c])<.0001f);
            Vector3 upper=matrix.MultiplyPoint3x4(contact.left.upper)+lift,elbow=matrix.MultiplyPoint3x4(contact.left.fore)+lift,wrist=matrix.MultiplyPoint3x4(contact.left.hand)+lift;
            float margin=(Vector3.Distance(upper,elbow)+Vector3.Distance(elbow,wrist))*.985f-Vector3.Distance(upper,targets[c]);
            gates.Add(Csv(ids[c],starts[c],yaw[c],clear,margin,phases[c],0,sourceReady,targets[c],tag.BreedId,asset,hash));
            Assert.That(clear,Is.True);Assert.That(margin,Is.GreaterThan(.004f));Assert.That(sourceReady,Is.False,"Reproduce the full source-path rejection, not a synthetic ready plan.");
            foreach(var sample in entry.samples.OrderBy(s=>s.phase))
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
                var livePos=liveBones.Select(t=>t.localPosition).ToArray();var liveRot=liveBones.Select(t=>t.localRotation).ToArray();
                for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
                float time=(clip.isLooping?Mathf.Repeat(sample.phase,1):sample.phase)*clip.length;clip.SampleAnimation(animator.gameObject,time);
                animator.transform.localPosition=basePosition;animator.transform.localRotation=baseRotation;animator.transform.localScale=baseScale;
                skin.BakeMesh(baked,true);var vertices=baked.vertices;var local=new Vector3[vertices.Length];float minY=float.PositiveInfinity;
                foreach(int index in breed.ContactVertexIndices){local[index]=sourceVisual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index]));minY=Mathf.Min(minY,local[index].y);}
                Vector3 Bone(Transform bone)=>matrix.MultiplyPoint3x4(sourceVisual.transform.InverseTransformPoint(bone.position)-Vector3.up*minY)+lift;
                Vector3 a=Bone(arm),b=Bone(fore),h=Bone(hand);
                float actualMargin=(Vector3.Distance(a,b)+Vector3.Distance(b,h))*.985f-Vector3.Distance(a,targets[c]);
                Assert.That(Vector3.Distance(a,matrix.MultiplyPoint3x4(sample.left.upper)+lift),Is.LessThan(.0002f));
                Assert.That(Vector3.Distance(b,matrix.MultiplyPoint3x4(sample.left.fore)+lift),Is.LessThan(.0002f));
                Assert.That(Vector3.Distance(h,matrix.MultiplyPoint3x4(sample.left.hand)+lift),Is.LessThan(.0002f));
                var probes=new CatBodyGuardCatalog.Probe[4];var depths=new float[4];var contains=Enumerable.Repeat(float.PositiveInfinity,4).ToArray();
                var maxima=new[]{new Maximum(),new Maximum(),new Maximum(),new Maximum(),new Maximum()};
                float scale=Mathf.Max(((Vector3)matrix.GetColumn(0)).magnitude,Mathf.Max(((Vector3)matrix.GetColumn(1)).magnitude,((Vector3)matrix.GetColumn(2)).magnitude));
                for(int r=0;r<4;r++)
                {
                    var p=sample.bodyProbes.Single(v=>v.region==Regions[r]);p.start=matrix.MultiplyPoint3x4(p.start)+lift;p.end=matrix.MultiplyPoint3x4(p.end)+lift;p.radius*=scale;probes[r]=p;
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
                    rows.Add(Csv(ids[c],"Paw",sample.phase,starts[c],yaw[c],group,capsuleDepth,capsuleDepth>.015f,containment,result.inside,result.ambiguous,result.depth,result.vertex,result.votes,result.point,result.exact.point,result.exact.triangle,result.exact.submesh,material,actualMargin,resultKind));
                    if(result.vertex>=0)Votes(solid,result.point,(direction,hit,distance,dot)=>rays.Add(Csv(ids[c],sample.phase,group,result.vertex,direction,hit,distance,dot)));
                }
            }
        }
        Assert.That(measured,Is.EqualTo(2*entry.samples.Length));Assert.That(UnityEditor.AssetDatabase.GetAssetDependencyHash(asset).ToString(),Is.EqualTo(hash));
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
