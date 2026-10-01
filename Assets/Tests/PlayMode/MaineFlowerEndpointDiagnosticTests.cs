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

// Predicted full weighted source skin, never a played rejected animation.
public sealed class MaineFlowerEndpointDiagnosticTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic;
    const string Root="Docs/QA/INTERACTION_POLISH_110MIN_2026-09-17/maine-endpoint-diagnostic";
    MaineFlowerReadinessDiagnosticTests fixture;
    readonly List<string> rows=new List<string>();
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();
    QaExactMeshContact metric;
    static object Get(object item,string field)=>item?.GetType().GetField(field,F)?.GetValue(item);
    static string Csv(params object[] values)=>string.Join(",",values.Select(value=>"\""+
        (value is IFormattable formattable?formattable.ToString(null,CultureInfo.InvariantCulture):value?.ToString()??"").Replace("\"","\"\"")+"\""));
    static string V(Vector3 p)=>p.x.ToString("G9",CultureInfo.InvariantCulture)+";"+p.y.ToString("G9",CultureInfo.InvariantCulture)+";"+p.z.ToString("G9",CultureInfo.InvariantCulture);
    static string PathOf(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
    [SetUp]public void Before()
    {
        fixture=new MaineFlowerReadinessDiagnosticTests();fixture.Before();metric=new QaExactMeshContact(topology);
        rows.Add("plan,contactPhase,pitch,yaw,activeHand,target,targetNormal,productionEndpointClear,repeatedEndpointClear,rejectCollider,colliderType,mesh,region,blockedHand,predictedVertices,predictedFaces,firstRejectedFace,triangleA,triangleB,triangleC,deepestPredictedDepth,insidePoints,deepPointsOver2mm,deepestSourceVertex,deepestBone,deepestPoint,nearestPoint,nearestNormal,nearestTriangle,nearestSubmesh,maxVotes,originalWindingAvailable,runtimeWindingAvailable,detailComplete,elapsedSeconds");
    }
    [TearDown]public void After()
    {
        try{Directory.CreateDirectory(Root);File.WriteAllLines(Root+"/maine-endpoint-witness.csv",rows);}
        finally{metric?.Clear();topology.Clear();fixture?.After();}
    }
    [UnityTest,Timeout(90000)]public IEnumerator MaineCoon_SixRejectedEndpoints_ReportRealMeshWitnesses()
    {
        yield return fixture.MaineCoon_ExactAuthoredStance_EightSecondSearchStageDiagnostic();
        var cat=Object.FindAnyObjectByType<CatMovement>();
        var cache=(IDictionary)typeof(CatPawReachResolver).GetField("surfaceGeometryCache",S).GetValue(null);
        object geometry=cache.Values.Cast<object>().FirstOrDefault();Assert.That(geometry,Is.Not.Null);
        var requests=(IDictionary)Get(geometry,"requests");
        var pairs=new List<DictionaryEntry>();
        foreach(DictionaryEntry pair in requests)pairs.Add(pair);
        var chosen=pairs.Where(pair=>((IList)Get(pair.Value,"candidates")).Count>0)
            .OrderBy(pair=>((Vector3)Get(pair.Key,"point")-cat.transform.position).sqrMagnitude).Take(3).ToArray();
        var plans=new List<CatPawReachPlan>();
        foreach(var pair in chosen)
        {
            var unique=((IList)Get(pair.Value,"candidates")).Cast<object>().Select(c=>(CatPawReachPlan)Get(c,"plan"))
                .GroupBy(p=>Csv(p.SourcePhase,p.ChestPitch,p.ChestYaw,p.Left)).Select(g=>g.First()).ToArray();
            if(unique.Length>0)plans.Add(unique[0]);
            if(unique.Length>1)plans.Add(unique[unique.Length-1]);
        }
        var fill=typeof(CatPawReachResolver).GetMethod("FillSurfacePawRegions",S);
        var clear=typeof(CatPawReachResolver).GetMethod("SurfaceBoxesClear",S);
        var boxes=(CatBodyGuardBox[])Get(geometry,"endpointScratch");
        var entry=(CatPawReachCatalog.Entry)Get(geometry,"entry");
        var timer=System.Diagnostics.Stopwatch.StartNew();int measured=0;
        foreach(var plan in plans)
        {
            if(timer.Elapsed.TotalSeconds>=8)break;
            yield return null;
            int sample=Array.FindIndex(entry.samples,s=>Mathf.Abs(s.phase-plan.SourcePhase)<.00001f);
            bool reachable=(bool)fill.Invoke(null,new object[]{geometry,plan,cat.transform.right,boxes,sample,0});
            Assert.That(reachable,Is.True,"Recorded contact math must reproduce before collider diagnosis.");
            bool production=(bool)clear.Invoke(null,new object[]{cat,geometry,plan,boxes,sample});
            var original=(Func<int,Collider,bool>)Get(geometry,"refinement");
            Collider rejected=null;int region=-1;
            bool repeated=cat.IsInteractionBoxesClear(boxes,.002f,(index,solid)=>
            {
                bool result=original(index,solid);
                if(!result&&rejected==null){rejected=solid;region=index;}
                return result;
            });
            Assert.That(repeated,Is.EqualTo(production),"The observational wrapper must not change permission.");
            string hand="",meshPath="",bone="";int vertex=-1,face=-1,inside=0,deep=0,maxVotes=0,submesh=-1,triangle=-1;
            float depth=0;Vector3 point=default,nearest=default,normal=default,a=default,b=default,c=default;
            bool complete=true,winding=true,runtimeWinding=true;int vertexCount=0,faceCount=0;
            if(rejected!=null)
            {
                int leftCount=((int[][])Get(geometry,"leftRegions")).Length;
                bool left=region<leftCount;hand=left?"left":"right";
                var skin=(Vector3[])Get(geometry,left?"leftArmScratch":"rightArmScratch");
                var definitions=left?entry.leftArmSkin:entry.rightArmSkin;
                var faces=((int[][])Get(geometry,left?"leftFaces":"rightFaces"))[left?region:region-leftCount];
                var indices=faces.Distinct().ToArray();vertexCount=indices.Length;faceCount=faces.Length/3;
                var points=new List<(Vector3 point,int vertex)>();
                foreach(int v in indices)points.Add((skin[v],v));
                for(int t=0;t<faces.Length;t+=3)
                {
                    if(timer.Elapsed.TotalSeconds>=8){complete=false;break;}
                    Vector3 aa=skin[faces[t]],bb=skin[faces[t+1]],cc=skin[faces[t+2]];
                    if(face<0&&!cat.IsInteractionTriangleClear(aa,bb,cc,rejected,.002f))
                    {face=t/3;a=aa;b=bb;c=cc;}
                    points.Add(((aa+bb+cc)/3f,-1));
                }
                foreach(var witness in points)
                {
                    if(timer.Elapsed.TotalSeconds>=8){complete=false;break;}
                    int votes;bool known;QaExactMeshContact.Hit hit;
                    float measuredDepth=Depth(rejected,witness.point,out votes,out known,out hit);
                    maxVotes=Mathf.Max(maxVotes,votes);winding&=known;
                    if(votes>=4)inside++;if(measuredDepth>.002f)deep++;
                    if(measuredDepth<=depth)continue;
                    depth=measuredDepth;point=witness.point;nearest=hit.point;normal=hit.normal;triangle=hit.triangle;submesh=hit.submesh;
                    vertex=witness.vertex>=0?definitions[witness.vertex].vertexIndex:-1;
                    bone=witness.vertex>=0?definitions[witness.vertex].influences.OrderByDescending(i=>i.weight).First().bonePath:"triangle-centroid";
                }
                if(rejected is MeshCollider collider)
                {
                    meshPath=UnityEditor.AssetDatabase.GetAssetPath(collider.sharedMesh);
                    runtimeWinding=CatMeshContactSurface.TryOriginalTriangleNormal(collider.sharedMesh,collider.transform,0,out _);
                }
            }
            rows.Add(Csv(measured++,plan.SourcePhase,plan.ChestPitch,plan.ChestYaw,plan.Left?"left":"right",V(plan.Surface.Point),V(plan.Surface.Normal),
                production,repeated,rejected!=null?PathOf(rejected.transform):"no-refinement-rejection",rejected!=null?rejected.GetType().Name:"",meshPath,region,hand,
                vertexCount,faceCount,face,V(a),V(b),V(c),depth,inside,deep,vertex,bone,V(point),V(nearest),V(normal),triangle,submesh,maxVotes,winding,runtimeWinding,complete,timer.Elapsed.TotalSeconds));
            if(!complete)break;
        }
        Assert.That(measured,Is.GreaterThan(0),"Diagnostic must observe existing endpoint candidates; pass is not contact success.");
    }
    float Depth(Collider collider,Vector3 point,out int votes,out bool winding,out QaExactMeshContact.Hit hit)
    {
        votes=0;winding=true;hit=default;
        if(!collider.bounds.Contains(point))return 0;
        if(collider is BoxCollider box)
        {
            Vector3 local=box.transform.InverseTransformPoint(point)-box.center;
            Vector3 extent=box.size*.5f-new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z));
            if(extent.x<0||extent.y<0||extent.z<0)return 0;
            votes=6;Vector3 scale=box.transform.lossyScale;
            return Mathf.Min(extent.x*Mathf.Abs(scale.x),Mathf.Min(extent.y*Mathf.Abs(scale.y),extent.z*Mathf.Abs(scale.z)));
        }
        if(!(collider is MeshCollider mesh)){winding=false;return 0;}
        var data=topology.Get(mesh.sharedMesh);
        Vector3[] dirs={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,new Vector3(.019f,.023f,1).normalized,
            -new Vector3(.013f,1,.027f).normalized,-new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
        bool previous=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        try
        {
            foreach(var direction in dirs)
            {
                if(!mesh.Raycast(new Ray(point,direction),out var ray,mesh.bounds.size.magnitude+.01f))continue;
                int t=ray.triangleIndex*3;
                if(t<0||t+2>=data.triangles.Length){winding=false;continue;}
                Vector3 a=data.vertices[data.triangles[t]],b=data.vertices[data.triangles[t+1]],c=data.vertices[data.triangles[t+2]];
                Vector3 normal=mesh.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(Vector3.Cross(b-a,c-a)).normalized;
                if(Vector3.Dot(normal,direction)>.0001f)votes++;
            }
        }
        finally{Physics.queriesHitBackfaces=previous;}
        hit=metric.Measure(mesh.sharedMesh,mesh.transform,point);
        return votes>=4?hit.distance:0;
    }
}
