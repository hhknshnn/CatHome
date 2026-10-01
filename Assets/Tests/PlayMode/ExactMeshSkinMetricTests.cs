using NUnit.Framework;
using UnityEngine;

public sealed class ExactMeshSkinMetricTests
{
    [Test]
    public void MetricDistance_OnRotatedNonuniformCube_IsAnalyticFaceDistance()
    {
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var topology=new QaMeshTopologyCache();var metric=new QaExactMeshContact(topology);
        try
        {
            var mesh=cube.GetComponent<MeshFilter>().sharedMesh;
            for(int i=0;i<5;i++)
            {
                cube.transform.SetPositionAndRotation(new Vector3(i*.7f,.3f,-i*.2f),Quaternion.Euler(i*17,i*31,i*13));
                cube.transform.localScale=new Vector3(i==4?-3:3,1,2);
                Vector3 local=new Vector3(.47f,.34f,.42f),world=cube.transform.TransformPoint(local);
                var measured=metric.Measure(mesh,cube.transform,world);
                Assert.That(measured.distance,Is.EqualTo(.09f).Within(.000005f));
                Assert.That(Vector3.Distance(world,measured.point),Is.EqualTo(measured.distance).Within(.000001f));
                Assert.That(measured.triangle,Is.GreaterThanOrEqualTo(0));
                Assert.That(measured.submesh,Is.Zero);Assert.That(measured.normal.magnitude,Is.EqualTo(1).Within(.00001f));
            }
        }
        finally{metric.Clear();topology.Clear();Object.DestroyImmediate(cube);}
    }
}
