using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class CatMeshReflectionNormalTests
{
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic,Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly Vector3[] Axes={Vector3.right,Vector3.up,Vector3.forward};

    [Test]public void AllReflectionParities_RotatedNonUniformCube_ExactMetricOutwardNormalsAndOccupiedVolume()
    {
        var loaded=typeof(CatMeshContactSurface).GetField("loaded",Static);object previous=loaded.GetValue(null);
        var catalog=ScriptableObject.CreateInstance<CatMeshContactSurface>();var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        var mesh=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);cube.GetComponent<MeshFilter>().sharedMesh=mesh;
        try
        {
            var data=new CatMeshContactSurface.Geometry{mesh=mesh,vertices=mesh.vertices,triangles=mesh.triangles};
            typeof(CatMeshContactSurface).GetMethod("BuildHierarchy",Static).Invoke(null,new object[]{data});
            typeof(CatMeshContactSurface).GetField("geometries",Private).SetValue(catalog,new[]{data});loaded.SetValue(null,catalog);
            // Exercise the same immutable baked-data path used by imported
            // unreadable production meshes, with no importer changes.
            mesh.UploadMeshData(true);Assert.That(mesh.isReadable,Is.False);
            var target=new CatMeshContactSurface.TargetSet(cube.transform);
            Quaternion rotation=Quaternion.Euler(23,37,61);
            for(int mask=0;mask<8;mask++)
            {
                Vector3 scale=new Vector3((mask&1)==0?.6f:-.6f,(mask&2)==0?1.4f:-1.4f,(mask&4)==0?2.1f:-2.1f);
                cube.transform.SetPositionAndRotation(new Vector3(1.3f+mask*.01f,-.7f,2.1f),rotation);cube.transform.localScale=scale;
                for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
                {
                    Vector3 local=Axes[axis]*sign;
                    Vector3 face=cube.transform.TransformPoint(local*.5f);
                    // Independent analytic OBB outward normal. No triangle
                    // winding, determinant or inverse-transpose helper here.
                    Vector3 outward=rotation*(local*Mathf.Sign(scale[axis]));
                    foreach(float depth in new[]{-.013f,.0015f,.003f,.03f})
                    {
                        Vector3 point=face-outward*depth;
                        string label=$"mask{mask} axis{axis} face{sign} depth{depth}";
                        Assert.That(target.TryClosest(point,out var hit),Is.True,label);
                        Assert.That(Vector3.Distance(hit.Point,face),Is.LessThan(.000003f),label);
                        Assert.That(Vector3.Dot(hit.Normal,outward),Is.GreaterThan(.99999f),"Existing Hit.Normal: "+label);
                        Assert.That(CatMeshContactSurface.TryWorldNormal(mesh,cube.transform,hit.Triangle,out var shared),Is.True,label);
                        Assert.That(Vector3.Dot(shared.normalized,outward),Is.GreaterThan(.99999f),"Care winding: "+label);
                        Assert.That(CatMeshContactSurface.TryOriginalTriangleNormal(mesh,cube.transform,hit.Triangle,out var original),Is.True,label);
                        Assert.That(Vector3.Dot(original,outward),Is.GreaterThan(.99999f),"Bodyguard winding: "+label);
                        Assert.That(CatMeshContactSurface.TryMetric(mesh,cube.transform,point,out float distance,out var metricNormal),Is.True,label);
                        Assert.That(distance,Is.EqualTo(Mathf.Abs(depth)).Within(.000003f),label);
                        Assert.That(Vector3.Dot(metricNormal,outward),Is.GreaterThan(.99999f),"Metric normal: "+label);
                        Matrix4x4 current=cube.transform.localToWorldMatrix;
                        Assert.That(CatMeshContactSurface.TryOriginalTriangleNormal(mesh,current,hit.Triangle,out var capturedNormal),Is.True,label);
                        Assert.That(Vector3.Dot(capturedNormal,outward),Is.GreaterThan(.99999f),"Captured-matrix normal: "+label);
                        Assert.That(CatMeshContactSurface.TryMetric(mesh,cube.transform,current,point,out float capturedDistance,out var capturedMetricNormal),Is.True,label);
                        Assert.That(capturedDistance,Is.EqualTo(Mathf.Abs(depth)).Within(.000003f),"Captured-matrix distance: "+label);
                        Assert.That(Vector3.Dot(capturedMetricNormal,outward),Is.GreaterThan(.99999f),"Captured-matrix metric normal: "+label);
                    }
                    Quaternion boxRotation=Quaternion.LookRotation(outward,rotation*Axes[(axis+1)%3]);
                    CatBodyGuardBox Box(Vector3 centre)=>new CatBodyGuardBox{centre=centre,rotation=boxRotation,halfExtents=Vector3.one*.01f};
                    Assert.That(target.BoxesClear(new[]{Box(face+outward*.04f)},.002f),Is.True,"Separated OBB remains clear");
                    Assert.That(target.BoxesClear(new[]{Box(face+outward*.009f)},.002f),Is.True,"1mm contact is unchanged");
                    Assert.That(target.BoxesClear(new[]{Box(face+outward*.007f)},.002f),Is.False,"3mm crossing still refuses");
                }
                Assert.That(target.BoxesClear(new[]{new CatBodyGuardBox{centre=cube.transform.position,rotation=rotation,halfExtents=Vector3.one*.01f}},.002f),Is.False,
                    $"Fully contained cube has no triangle crossing: reflection mask{mask} must retain occupied-volume refusal");
                Assert.That(target.BoxesClear(new[]{new CatBodyGuardBox{centre=cube.transform.position,rotation=rotation,halfExtents=new Vector3(.01f,.0005f,.01f)}},.002f),Is.False,
                    "Thin contained skin cannot become empty after reflection");
            }
        }
        finally{loaded.SetValue(null,previous);Object.DestroyImmediate(cube);Object.DestroyImmediate(mesh);Object.DestroyImmediate(catalog);}
    }

    [Test]public void ReadableOriginalFallback_ParentReflectionAndChildRotation_PreservesPhysicalFaceMetricAndOutward()
    {
        var parent=new GameObject("QA affine parent");var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
        var mesh=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);cube.GetComponent<MeshFilter>().sharedMesh=mesh;
        try
        {
            cube.transform.SetParent(parent.transform,false);
            parent.transform.SetPositionAndRotation(new Vector3(-1,2,.7f),Quaternion.Euler(17,51,9));parent.transform.localScale=new Vector3(-1.7f,.8f,1.3f);
            cube.transform.localRotation=Quaternion.Euler(29,-13,43);cube.transform.localScale=new Vector3(.7f,1.2f,.9f);
            // A rotated child of a nonuniform parent includes shear. Outward
            // is independently oriented away from the known convex centre.
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                Vector3 face=cube.transform.TransformPoint(Axes[axis]*(sign*.5f));
                Vector3 first=cube.transform.TransformVector(Axes[(axis+1)%3]),second=cube.transform.TransformVector(Axes[(axis+2)%3]);
                Vector3 outward=Vector3.Cross(first,second).normalized;
                if(Vector3.Dot(outward,face-cube.transform.position)<0)outward=-outward;
                Vector3 point=face-outward*.003f;
                Assert.That(CatMeshContactSurface.TryMetric(mesh,cube.transform,point,out float distance,out var normal),Is.True);
                Assert.That(distance,Is.EqualTo(.003f).Within(.000003f));Assert.That(Vector3.Dot(normal,outward),Is.GreaterThan(.99999f));
                Assert.That(CatMeshContactSurface.TryMetric(mesh,cube.transform,cube.transform.localToWorldMatrix,point,out float captured,out var capturedNormal),Is.True);
                Assert.That(captured,Is.EqualTo(.003f).Within(.000003f));Assert.That(Vector3.Dot(capturedNormal,outward),Is.GreaterThan(.99999f));
            }
        }
        finally{Object.DestroyImmediate(cube);Object.DestroyImmediate(parent);Object.DestroyImmediate(mesh);}
    }
}
