using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class MeshOnlyPawTargetTests
{
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic,Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [Test]public void RealTriangleHit_TracksAffineMovement_RejectsCrossingAndFullyContainedBoxes_WithoutCollider()
    {
        var loaded=typeof(CatMeshContactSurface).GetField("loaded",Static);object previous=loaded.GetValue(null);
        var catalog=ScriptableObject.CreateInstance<CatMeshContactSurface>();var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        try
        {
            var filter=cube.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;
            var data=new CatMeshContactSurface.Geometry{mesh=mesh,vertices=mesh.vertices,triangles=mesh.triangles};
            typeof(CatMeshContactSurface).GetMethod("BuildHierarchy",Static).Invoke(null,new object[]{data});
            typeof(CatMeshContactSurface).GetField("geometries",Private).SetValue(catalog,new[]{data});loaded.SetValue(null,catalog);
            var set=new CatMeshContactSurface.TargetSet(cube.transform);
            Assert.That(set.TryClosest(new Vector3(0,0,1),out var hit),Is.True);
            Assert.That(Vector3.Distance(hit.Point,new Vector3(0,0,.5f)),Is.LessThan(.000001f));
            Assert.That(Vector3.Dot(hit.Normal,Vector3.forward),Is.GreaterThan(.9999f));
            Assert.That(cube.GetComponentsInChildren<Collider>().Length,Is.Zero);
            CatBodyGuardBox Box(float z,float half=.01f)=>new CatBodyGuardBox{centre=new Vector3(0,0,z),rotation=Quaternion.identity,halfExtents=Vector3.one*half};
            Assert.That(set.BoxesClear(new[]{Box(0)},.002f),Is.False,"Fully contained closed-mesh blocker must reject");
            Assert.That(set.BoxesClear(new[]{new CatBodyGuardBox{centre=Vector3.zero,rotation=Quaternion.identity,halfExtents=new Vector3(.01f,.0005f,.01f)}},.002f),Is.False,"Thin fully contained skin is still blocked");
            Assert.That(set.BoxesClear(new[]{Box(.6f)},.002f),Is.True);
            Assert.That(set.BoxesClear(new[]{Box(.509f)},.002f),Is.True,"1 mm boundary contact");
            Assert.That(set.BoxesClear(new[]{Box(.507f)},.002f),Is.False,"3 mm crossing cannot hide behind a missing Collider");
            cube.transform.SetPositionAndRotation(new Vector3(3,-1,4),Quaternion.Euler(13,61,9));cube.transform.localScale=new Vector3(.4f,1.7f,.8f);
            Vector3 expected=cube.transform.TransformPoint(new Vector3(0,0,.5f));
            Assert.That(Vector3.Distance(hit.Point,expected),Is.LessThan(.000001f));
            Vector3 normal=cube.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(Vector3.forward).normalized;
            Assert.That(Vector3.Dot(hit.Normal,normal),Is.GreaterThan(.9999f));
            Assert.That(set.BoxesClear(new[]{new CatBodyGuardBox{centre=cube.transform.position,rotation=cube.transform.rotation,halfExtents=Vector3.one*.01f}},.002f),Is.False);
            cube.GetComponent<Renderer>().enabled=false;Assert.That(hit.IsValid,Is.False);
        }
        finally{loaded.SetValue(null,previous);Object.DestroyImmediate(cube);Object.DestroyImmediate(catalog);}
    }
}
