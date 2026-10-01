using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class CatMeshContactMetricTests
{
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic,Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [Test]public void ExactMetric_UsesRequestedMeshOnly_AndKeepsRotatedSurfaceToleranceInMetres()
    {
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var child=GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            child.transform.SetParent(cube.transform,false);child.transform.localScale=Vector3.one*.002f;
            var mesh=cube.GetComponent<MeshFilter>().sharedMesh;
            cube.transform.SetPositionAndRotation(new Vector3(4,2,-3),Quaternion.Euler(0,0,45));
            cube.transform.localScale=new Vector3(1,2,.4f);
            foreach(float depth in new[]{.0015f,.003f,.03f})
            {
                Vector3 point=cube.transform.TransformPoint(new Vector3(.5f-depth,0,0));
                child.transform.position=point; // A child surface must never replace the requested original solid.
                Assert.That(CatMeshContactSurface.TryMetric(mesh,cube.transform,point,out float distance,out var normal),Is.True);
                Assert.That(distance,Is.EqualTo(depth).Within(.000001f));
                Assert.That(Vector3.Dot(normal,cube.transform.right),Is.GreaterThan(.9999f));
            }
            Vector3 moved=cube.transform.position+new Vector3(-7,3,1);cube.transform.position=moved;
            Vector3 fresh=cube.transform.TransformPoint(new Vector3(.497f,0,0));
            Assert.That(CatMeshContactSurface.TryMetric(mesh,cube.transform,fresh,out float result,out _),Is.True);
            Assert.That(result,Is.EqualTo(.003f).Within(.000001f));
        }
        finally{Object.DestroyImmediate(child);Object.DestroyImmediate(cube);}
    }
    [Test]public void BakedBvh_MatchesReadableOriginal_AndUnknownUnreadableRefuses()
    {
        var loaded=typeof(CatMeshContactSurface).GetField("loaded",Static);object old=loaded.GetValue(null);
        var catalog=ScriptableObject.CreateInstance<CatMeshContactSurface>();var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh clone=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);
        try
        {
            var data=new CatMeshContactSurface.Geometry{mesh=clone,vertices=clone.vertices,triangles=clone.triangles};
            typeof(CatMeshContactSurface).GetMethod("BuildHierarchy",Static).Invoke(null,new object[]{data});
            typeof(CatMeshContactSurface).GetField("geometries",Private).SetValue(catalog,new[]{data});loaded.SetValue(null,catalog);
            cube.transform.SetPositionAndRotation(new Vector3(3,-2,8),Quaternion.Euler(23,17,51));cube.transform.localScale=new Vector3(.3f,2.1f,1.8f);
            for(int i=0;i<40;i++)
            {
                Vector3 point=cube.transform.TransformPoint(new Vector3(.49f-i*.002f,.1f,-.1f));
                Assert.That(CatMeshContactSurface.TryMetric(clone,cube.transform,point,out float distance,out _),Is.True);
                Assert.That(distance,Is.EqualTo((.01f+i*.002f)*.3f).Within(.000002f));
            }
            var unknown=Object.Instantiate(clone);unknown.UploadMeshData(true);
            try{Assert.That(CatMeshContactSurface.TryMetric(unknown,cube.transform,cube.transform.position,out float distance,out _),Is.False);Assert.That(float.IsPositiveInfinity(distance),Is.True);}
            finally{Object.DestroyImmediate(unknown);}
        }
        finally{loaded.SetValue(null,old);Object.DestroyImmediate(clone);Object.DestroyImmediate(cube);Object.DestroyImmediate(catalog);}
    }
}
