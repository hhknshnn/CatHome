using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

// Bounded native physics fixture: synthetic original closed mesh and a remote
// query primitive. No level load, breed selection, save, animation or player edit.
public sealed class CatMeshReflectionPhysicsTests
{
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic,Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly Vector3[] Axes={Vector3.right,Vector3.up,Vector3.forward};
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");

    [UnityTest,Timeout(60000)]
    public IEnumerator ReflectedClosedCube_NativeInsideVotesAndGuardRefusal_KeepExactWorldTolerance()
    {
        var rows=new List<string>{"reflectionMask,axis,face,depth,exactDistance,normalDot,inside,triangleClear,backfaceSettingRestored"};
        var loaded=typeof(CatMeshContactSurface).GetField("loaded",Static);object previousCatalog=loaded.GetValue(null);
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var remote=new GameObject("QA reflection remote trigger");
        Mesh mesh=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);var catalog=ScriptableObject.CreateInstance<CatMeshContactSurface>();
        bool originalBackfaces=Physics.queriesHitBackfaces;
        try
        {
            cube.name="QA reflected original closed cube";Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<MeshFilter>().sharedMesh=mesh;
            var solid=cube.AddComponent<MeshCollider>();solid.sharedMesh=mesh;solid.convex=false;
            var data=new CatMeshContactSurface.Geometry{mesh=mesh,vertices=mesh.vertices,triangles=mesh.triangles};
            typeof(CatMeshContactSurface).GetMethod("BuildHierarchy",Static).Invoke(null,new object[]{data});
            typeof(CatMeshContactSurface).GetField("geometries",Private).SetValue(catalog,new[]{data});loaded.SetValue(null,catalog);
            remote.transform.position=new Vector3(0,-10000,0);var query=remote.AddComponent<BoxCollider>();query.isTrigger=true;
            var deepInside=typeof(CatBodyGuard).GetMethod("DeepInside",Private);
            var boxQuery=typeof(CatBodyGuard).GetField("boxQuery",Private);
            var set=new CatMeshContactSurface.TargetSet(cube.transform);Quaternion rotation=Quaternion.Euler(23,37,61);
            for(int mask=0;mask<8;mask++)
            {
                cube.transform.SetPositionAndRotation(new Vector3(3.7f,9.3f,-2.1f),rotation);
                Vector3 scale=new Vector3((mask&1)==0?.6f:-.6f,(mask&2)==0?1.4f:-1.4f,(mask&4)==0?2.1f:-2.1f);cube.transform.localScale=scale;
                Physics.SyncTransforms();yield return new WaitForFixedUpdate();
                // Initialises only the same remote primitive precondition as
                // a normal guard. The real public triangle API and native mesh
                // raycasts below execute unchanged production code.
                var guard=new CatBodyGuard(remote.transform,null);boxQuery.SetValue(guard,query);
                Assert.That(set.BoxesClear(new[]{new CatBodyGuardBox{centre=cube.transform.position,rotation=rotation,halfExtents=Vector3.one*.01f}},.002f),Is.False,
                    "No crossing triangles: a reflected fully contained skin box must still refuse");
                for(int axis=0;axis<3;axis++)for(int faceSign=-1;faceSign<=1;faceSign+=2)
                {
                    Vector3 local=Axes[axis]*faceSign,outward=rotation*(local*Mathf.Sign(scale[axis]));
                    Vector3 face=cube.transform.TransformPoint(local*.5f),u=rotation*Axes[(axis+1)%3],v=rotation*Axes[(axis+2)%3];
                    foreach(float depth in new[]{-.03f,.0015f,.003f,.03f})
                    {
                        Vector3 point=face-outward*depth;
                        Assert.That(CatMeshContactSurface.TryMetric(mesh,cube.transform,point,out float distance,out var normal),Is.True);
                        Assert.That(distance,Is.EqualTo(Mathf.Abs(depth)).Within(.000004f));Assert.That(Vector3.Dot(normal,outward),Is.GreaterThan(.99999f));
                        Physics.queriesHitBackfaces=(mask%2)==0;
                        bool before=Physics.queriesHitBackfaces;
                        bool inside=(bool)deepInside.Invoke(guard,new object[]{point,solid,.002f});
                        bool restored=Physics.queriesHitBackfaces==before;
                        Assert.That(restored,Is.True);Assert.That(inside,Is.EqualTo(depth>.002f),$"mask{mask},axis{axis},face{faceSign},depth{depth}");
                        // The shallow sample verifies exact inside metric only;
                        // a 1mm query shell is a distinct surface-overlap test.
                        bool triangleClear=true;
                        if(depth<0||depth>=.003f)
                        {
                            triangleClear=guard.IsWorldTriangleClear(point+u*.003f,point-u*.003f+v*.003f,point-u*.003f-v*.003f,solid,.002f);
                            Assert.That(triangleClear,Is.EqualTo(depth<0),"Actual production source-face refusal must agree with occupied volume");
                            Assert.That(Physics.queriesHitBackfaces,Is.EqualTo(before));
                        }
                        rows.Add(FormattableString.Invariant($"{mask},{axis},{faceSign},{depth:R},{distance:R},{Vector3.Dot(normal,outward):R},{inside},{triangleClear},{restored}"));
                    }
                }
                // Requery the same TargetSet after a parity/position change.
                // A world normal and inside permission must never be cached.
                cube.transform.position+=Vector3.up*2f;Physics.SyncTransforms();
                Assert.That(set.BoxesClear(new[]{new CatBodyGuardBox{centre=new Vector3(3.7f,9.3f,-2.1f),rotation=rotation,halfExtents=Vector3.one*.01f}},.002f),Is.True);
            }
        }
        finally
        {
            Physics.queriesHitBackfaces=originalBackfaces;loaded.SetValue(null,previousCatalog);
            Object.DestroyImmediate(cube);Object.DestroyImmediate(remote);Object.DestroyImmediate(mesh);Object.DestroyImmediate(catalog);
            Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"mesh-reflection-native.csv"),rows);
        }
    }
}
