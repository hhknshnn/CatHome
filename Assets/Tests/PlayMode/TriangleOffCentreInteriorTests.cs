using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Closed occupied volume cuts a face away from its original four inside samples.</summary>
public sealed class TriangleOffCentreInteriorTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    TriangleSurfaceRefinementTests fixture;
    T Field<T>(string name)=>(T)typeof(TriangleSurfaceRefinementTests).GetField(name,Private).GetValue(fixture);
    object Call(string name,params object[] args)=>typeof(TriangleSurfaceRefinementTests).GetMethod(name,Private).Invoke(fixture,args);
    [UnitySetUp]public IEnumerator Before(){fixture=new TriangleSurfaceRefinementTests();yield return fixture.Before();}
    [UnityTearDown]public IEnumerator After(){if(fixture!=null)yield return fixture.After();}

    [UnityTest,Timeout(60000)]public IEnumerator OffCentreClosedMesh_CutsFaceAwayFromOriginalVerticesAndCentroid()
    {
        var origin=Field<Vector3>("origin");var a=Field<Vector3>("a");var b=Field<Vector3>("b");var c=Field<Vector3>("c");
        var solid=(MeshCollider)Call("Obstacle",PrimitiveType.Cube,origin+new Vector3(-.105f,0,.04f),.12f,true);
        foreach(var outside in new[]{a,b,c,(a+b+c)/3f})
            Assert.That(solid.bounds.Contains(outside),Is.False,"Calibration: all four original inside samples must miss this solid");
        Vector3 interior=origin+new Vector3(-.10f,0,0);
        Assert.That(solid.bounds.Contains(interior),Is.True);
        Assert.That(CatMeshContactSurface.TryMetric(solid.sharedMesh,solid.transform,interior,out float depth,out _),Is.True);
        Assert.That(depth,Is.GreaterThan(.019f),"Actual face interior enters the original closed cube by 20 mm");
        // This point lies strictly inside the full triangle x+z<0; it is not
        // a corner/centre witness or a contact fabricated outside the face.
        Vector3 local=interior-origin;Assert.That(local.x+local.z,Is.LessThan(-.05f));
        var result=((bool direct,bool broad,bool refined))Call("Observe","offCentreClosedFaceInterior",solid);
        Assert.That(result.direct,Is.False,"Recursive source triangles must not treat MTD=false inside a closed mesh as clear");
        Assert.That(result.refined,Is.False,"The production coarse/refine path must retain this real face intersection");
        yield return null;
    }

    [UnityTest,Timeout(60000)]public IEnumerator SkinnyTriangle_FarInteriorCannotBeCulledByCentroidApproximation()
    {
        var origin=Field<Vector3>("origin");var cat=Field<CatMovement>("cat");
        Vector3 a=origin+new Vector3(0,0,-.01f),b=origin+new Vector3(0,0,.01f),c=origin+Vector3.right;
        var solid=(MeshCollider)Call("Obstacle",PrimitiveType.Cube,origin+Vector3.right*.9f,.06f,true);
        foreach(var outside in new[]{a,b,c,(a+b+c)/3f})
            Assert.That(solid.bounds.Contains(outside),Is.False,"All initial inside witnesses are outside the real blocker");
        Vector3 point=a*.05f+b*.05f+c*.9f;
        Assert.That(solid.bounds.Contains(point),Is.True,"Positive barycentric weights put the witness inside the original face");
        Assert.That(CatMeshContactSurface.TryMetric(solid.sharedMesh,solid.transform,point,out float depth,out _),Is.True);
        Assert.That(depth,Is.GreaterThan(.029f),"The actual original face passes 30 mm inside the closed cube");
        var oldApproximation=new Bounds((a+b+c)/3f,Vector3.one*(Vector3.Distance(a,b)+Vector3.Distance(b,c)+.002f));
        Assert.That(oldApproximation.Intersects(solid.bounds),Is.False,"Calibration: the old centroid/radius shortcut omits this real portion of the face");
        var envelope=new CatBodyGuardBox{centre=origin+Vector3.right*.5f,halfExtents=new Vector3(.501f,.001f,.011f),rotation=Quaternion.identity};
        Assert.That(CatMeshContactSurface.TryBoxBoundary(solid.sharedMesh,solid.transform,envelope,out bool crossing),Is.True);
        Assert.That(crossing,Is.True);
        Assert.That(cat.IsInteractionTriangleClear(a,b,c,solid,.002f),Is.False,"MTD-negative fallback must use an actual encompassing OBB/AABB");
        Assert.That(cat.IsInteractionBoxesClear(new[]{envelope},.002f,(index,blocker)=>cat.IsInteractionTriangleClear(a,b,c,blocker,.002f)),Is.False);
        yield return null;
    }
}
