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

// Samples only source animation, immediately restores it, then maps the baked
// points remotely. Never places the live cat in a refused stance or starts care.
public sealed class CarePersianSkinWitnessTests
{
    const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    [Serializable] sealed class Data
    {public string breed,sourceAssetGuid,sourceDependencyHash,skinMeshName;public int vertexCount;public int[] bodyIndices,neckIndices;}
    sealed class Maximum
    {public int inside,ambiguous,vertex=-1,votes;public float depth;public Vector3 point;public QaExactMeshContact.Hit exact;}
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();
    QaExactMeshContact metric;CareAlignmentPolishTests fixture;bool prepared;
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
    static T Read<T>(object o,string field)=>(T)o.GetType().GetField(field,Instance).GetValue(o);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Instance).Invoke(o,args);
    static readonly Vector3[] Directions={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,
        new Vector3(.019f,.023f,1).normalized,-new Vector3(.013f,1,.027f).normalized,
        -new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    [SetUp]public void Before(){fixture=new CareAlignmentPolishTests();fixture.Before();prepared=true;metric=new QaExactMeshContact(topology);}
    [TearDown]public void After(){metric?.Clear();topology.Clear();if(prepared)fixture.After();prepared=false;}

    [UnityTest,Timeout(180000)]
    public IEnumerator PersianThreeReachableStances_ActualIdleWalkSkinAgainstOriginalStationTriangles()
    {
        var rows=new List<string>{"radius,angle,root,yaw,clip,phase,group,collider,mesh,renderedSameMesh,insideVertices,ambiguousVertices,maximumExactDepth,vertex,outwardVotes,point,closestSurface,normal,triangle,submesh,triangleTests"};
        var gates=new List<string>{"radius,angle,root,yaw,bodyClear,controllerClear,interactionClear,stationMesh,stationBounds,target,meshHash"};
        var witnesses=new List<string>{"radius,clip,phase,group,vertex,direction,hit,distance,outwardDot"};
        int samples=0;
        try
        {
            var data=JsonUtility.FromJson<Data>(File.ReadAllText(Path.Combine(Output,"care-persian-skin-indices.json")));
            Assert.That(data.breed,Is.EqualTo("persian"));
            string source=UnityEditor.AssetDatabase.GUIDToAssetPath(data.sourceAssetGuid);
            Assert.That(UnityEditor.AssetDatabase.GetAssetDependencyHash(source).ToString(),Is.EqualTo(data.sourceDependencyHash));
            yield return (IEnumerator)Call(fixture,"Home");
            yield return (IEnumerator)Call(fixture,"Room",HomeRoomService.KitchenId);
            yield return (IEnumerator)Call(fixture,"Breed","persian");
            var cat=Read<CatMovement>(fixture,"cat");var guard=Read<CatBodyGuard>(cat,"bodyGuard");
            var meal=CatActivity.Registered.OfType<MealTimeActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene);
            var animator=cat.GetComponentInChildren<Animator>();var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.That(skin.sharedMesh.vertexCount,Is.EqualTo(data.vertexCount));Assert.That(skin.sharedMesh.name,Is.EqualTo(data.skinMeshName));
            var masks=new HashSet<int>(data.neckIndices);Assert.That(masks.Count,Is.GreaterThan(0));
            var solids=meal.GetComponentsInChildren<MeshCollider>().Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy&&
                (bool)Call(guard,"Solid",c)).ToArray();Assert.That(solids.Length,Is.GreaterThan(0));
            foreach(var solid in solids)
            {
                Assert.That(solid.convex,Is.False,"Classification uses original nonconvex station mesh");
                var filter=solid.GetComponent<MeshFilter>();var renderer=solid.GetComponent<Renderer>();
                Assert.That(filter!=null&&renderer!=null&&renderer.enabled&&filter.sharedMesh==solid.sharedMesh,Is.True,
                    "Do not equate a navigation proxy with the visible bowl: "+PathOf(solid.transform));
            }
            var all=animator.GetComponentsInChildren<Transform>(true);
            var positions=all.Select(t=>t.localPosition).ToArray();var rotations=all.Select(t=>t.localRotation).ToArray();var scales=all.Select(t=>t.localScale).ToArray();
            var mesh=new Mesh();
            Vector3 outward=Read<Transform>(meal,"standPoint").position-meal.BowlPoint.position;outward.y=0;outward.Normalize();
            try
            {
                foreach(float radius in new[]{.30f,.32f,.34f})
                {
                    Vector3 side=Quaternion.Euler(0,15,0)*outward,position=meal.BowlPoint.position+side*radius;position.y=.05f;
                    Quaternion heading=Quaternion.LookRotation(-side);
                    Physics.SyncTransforms();bool body=cat.IsBodyPoseClear(position,heading),cc=guard.IsControllerClear(position,heading);
                    Assert.That(body,Is.False,"Previously refused neck stance remains refused");Assert.That(cc,Is.True,"Original controller unchanged");
                    foreach(var solid in solids)
                    {
                        string asset=UnityEditor.AssetDatabase.GetAssetPath(solid.sharedMesh);
                        gates.Add(Csv(radius,15,position,heading.eulerAngles.y,body,cc,cat.IsInteractionPoseClear(position,heading),PathOf(solid.transform),
                            solid.bounds,meal.BowlPoint.position,UnityEditor.AssetDatabase.GetAssetDependencyHash(asset)));
                    }
                    foreach(string kind in new[]{"Idle","Walk"})
                    {
                        var clip=animator.runtimeAnimatorController.animationClips.First(c=>c.name.EndsWith("|"+kind,StringComparison.Ordinal));
                        for(int frame=0;frame<32;frame++)
                        {
                            Vector3 sourceRoot=cat.transform.position;Quaternion sourceHeading=cat.transform.rotation;
                            Quaternion map=heading*Quaternion.Inverse(sourceHeading);
                            Vector3[] points;
                            try
                            {
                                Restore();clip.SampleAnimation(animator.gameObject,clip.length*frame/32f);skin.BakeMesh(mesh,true);
                                points=mesh.vertices;for(int i=0;i<points.Length;i++)points[i]=position+map*(skin.transform.TransformPoint(points[i])-sourceRoot);
                            }
                            finally{Restore();}
                            foreach(var solid in solids)
                            {
                                var bodyMax=new Maximum();var neckMax=new Maximum();Bounds bounds=solid.bounds;
                                foreach(int index in data.bodyIndices)
                                {
                                    Vector3 point=points[index];if(!bounds.Contains(point))continue;
                                    int votes=InsideVotes(solid,point,null);
                                    if(votes>0&&votes<4){bodyMax.ambiguous++;if(masks.Contains(index))neckMax.ambiguous++;}
                                    if(votes<4)continue;
                                    var exact=metric.Measure(solid.sharedMesh,solid.transform,point);
                                    Include(bodyMax,index,point,votes,exact);if(masks.Contains(index))Include(neckMax,index,point,votes,exact);
                                }
                                Write("body",bodyMax);Write("neck",neckMax);
                                void Write(string group,Maximum maximum)
                                {
                                    rows.Add(Csv(radius,15,position,heading.eulerAngles.y,kind,frame/32f,group,PathOf(solid.transform),
                                        UnityEditor.AssetDatabase.GetAssetPath(solid.sharedMesh),true,maximum.inside,maximum.ambiguous,maximum.depth,maximum.vertex,
                                        maximum.votes,maximum.point,maximum.exact.point,maximum.exact.normal,maximum.exact.triangle,maximum.exact.submesh,maximum.exact.triangleTests));
                                    if(maximum.vertex>=0)InsideVotes(solid,maximum.point,(direction,hit,distance,dot)=>
                                        witnesses.Add(Csv(radius,kind,frame/32f,group,maximum.vertex,direction,hit,distance,dot)));
                                }
                            }
                            Assert.That(cat.transform.position,Is.EqualTo(sourceRoot));Assert.That(cat.transform.rotation,Is.EqualTo(sourceHeading));
                            Assert.That(CatActivity.Active,Is.Null,"Metrology never starts the refused action");samples++;
                            if(frame%8==0)CareAlignmentPolishTests.TransitionMark("Persian skin r="+radius+" "+kind+" phase="+frame);
                            yield return null;
                        }
                        File.WriteAllLines(Path.Combine(Output,"care-persian-actual-skin.csv"),rows);
                    }
                }
            }
            finally{Restore();Object.DestroyImmediate(mesh);}
            void Restore(){for(int i=0;i<all.Length;i++){all[i].localPosition=positions[i];all[i].localRotation=rotations[i];all[i].localScale=scales[i];}}
            Assert.That(samples,Is.EqualTo(192));
        }
        finally
        {
            Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"care-persian-actual-skin.csv"),rows);
            File.WriteAllLines(Path.Combine(Output,"care-persian-actual-skin-gates.csv"),gates);
            File.WriteAllLines(Path.Combine(Output,"care-persian-actual-skin-witness-rays.csv"),witnesses);
        }
    }
    static void Include(Maximum result,int vertex,Vector3 point,int votes,QaExactMeshContact.Hit exact)
    {
        result.inside++;if(result.vertex>=0&&exact.distance<=result.depth)return;
        result.vertex=vertex;result.depth=exact.distance;result.point=point;result.votes=votes;result.exact=exact;
    }
    int InsideVotes(MeshCollider mesh,Vector3 point,Action<int,bool,float,float> report)
    {
        var data=topology.Get(mesh.sharedMesh);bool previous=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;int votes=0;
        try
        {
            for(int n=0;n<Directions.Length;n++)
            {
                Vector3 direction=Directions[n];bool hit=mesh.Raycast(new Ray(point,direction),out var ray,5f);float dot=0;
                int tri=ray.triangleIndex*3;
                if(hit&&tri>=0&&tri+2<data.triangles.Length)
                {
                    Vector3 a=mesh.transform.TransformPoint(data.vertices[data.triangles[tri]]),b=mesh.transform.TransformPoint(data.vertices[data.triangles[tri+1]]),
                        c=mesh.transform.TransformPoint(data.vertices[data.triangles[tri+2]]);
                    dot=Vector3.Dot(Vector3.Cross(b-a,c-a).normalized,direction);if(dot>0)votes++;
                }
                report?.Invoke(n,hit,hit?ray.distance:float.NaN,dot);
            }
        }
        finally{Physics.queriesHitBackfaces=previous;}
        return votes;
    }
    static string PathOf(Transform t){string path=t.name;while(t.parent!=null){t=t.parent;path=t.name+"/"+path;}return path;}
    static string Csv(params object[] values)=>string.Join(",",values.Select(v=>"\""+(v is Vector3 p?FormattableString.Invariant($"{p.x:F7};{p.y:F7};{p.z:F7}"):
        v is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):v?.ToString()??"").Replace("\"","\"\"")+"\""));
}
#endif
