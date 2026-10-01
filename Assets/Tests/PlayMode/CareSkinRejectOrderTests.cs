using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class CareSkinRejectOrderTests
{
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic,Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const int Count=128;
    static readonly Vector3 Offset=new Vector3(.008f,.004f,.006f);
    static readonly Vector3[] Rays={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,new Vector3(.019f,.023f,1).normalized,
        -new Vector3(.013f,1,.027f).normalized,-new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");

    [UnityTest,Timeout(60000)]
    public IEnumerator ReorderedFailureWitnesses_MatchExhaustiveFreshSkinAndMovingMesh_WithoutWarmAllocations()
    {
        var loaded=typeof(CatMeshContactSurface).GetField("loaded",Static);object oldCatalog=loaded.GetValue(null);
        var catalog=ScriptableObject.CreateInstance<CatMeshContactSurface>();
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var rig=new GameObject("QA independent care skin");
        var mesh=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);var skinMesh=new Mesh{name="QA current care weighted points"};
        var rows=new List<string>{"case,exhaustiveClear,orderedClear,vertices,rayTests,bvhTriangleTests,fullCoverage"};
        var bones=new Transform[Count];CatCareSkinClearance check=null;bool originalBackfaces=Physics.queriesHitBackfaces;
        try
        {
            Object.DestroyImmediate(cube.GetComponent<Collider>());cube.GetComponent<MeshFilter>().sharedMesh=mesh;
            var solid=cube.AddComponent<MeshCollider>();solid.sharedMesh=mesh;solid.convex=false;
            cube.transform.SetPositionAndRotation(new Vector3(0,3,0),Quaternion.Euler(17,31,43));cube.transform.localScale=new Vector3(.8f,1.3f,1.7f);
            var geometry=new CatMeshContactSurface.Geometry{mesh=mesh,vertices=mesh.vertices,triangles=mesh.triangles};
            typeof(CatMeshContactSurface).GetMethod("BuildHierarchy",Static).Invoke(null,new object[]{geometry});
            typeof(CatMeshContactSurface).GetField("geometries",Private).SetValue(catalog,new[]{geometry});loaded.SetValue(null,catalog);
            var animator=rig.AddComponent<Animator>();animator.enabled=false;
            var skin=rig.AddComponent<SkinnedMeshRenderer>();skin.enabled=false;skinMesh.vertices=new Vector3[Count];skin.sharedMesh=skinMesh;
            var profile=new CatCareSkinCatalog.Profile{meshName=skinMesh.name,meshVertexCount=Count,
                bonePaths=new string[Count],vertexIndices=new int[Count],starts=new int[Count+1],bones=new int[Count],bindPoints=new Vector3[Count],weights=new float[Count]};
            for(int v=0;v<Count;v++)
            {
                var bone=new GameObject("point"+v).transform;bone.SetParent(rig.transform,false);bones[v]=bone;
                profile.bonePaths[v]=bone.name;profile.vertexIndices[v]=v;profile.starts[v]=v;profile.bones[v]=v;profile.bindPoints[v]=Offset;profile.weights[v]=1;
            }
            profile.starts[Count]=Count;
            Assert.That(CatCareWeightedSkin.TryBind(profile,animator,skin,out var weighted),Is.True);
            // Present BEFORE binding, as authored food/content children are.
            // This identical sharedMesh child has a surface at the parent's
            // deep interior and must never substitute its near-zero distance.
            var metricChild=GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(metricChild.GetComponent<Collider>());
            metricChild.GetComponent<MeshFilter>().sharedMesh=mesh;
            metricChild.transform.SetParent(cube.transform,false);
            metricChild.transform.localPosition=Vector3.right*.001f;metricChild.transform.localScale=Vector3.one*.002f;
            // Only substitute the scene-binding setup. IsClear itself is the
            // actual production method, with real MeshCollider raycasts/BVH.
            var partType=typeof(CatCareSkinClearance).GetNestedType("Part",BindingFlags.NonPublic);
            object part=Activator.CreateInstance(partType,true);
            partType.GetField("collider").SetValue(part,solid);
            var parts=Array.CreateInstance(partType,1);parts.SetValue(part,0);
            var constructor=typeof(CatCareSkinClearance).GetConstructor(Private,null,new[]{typeof(CatCareWeightedSkin),parts.GetType(),typeof(float)},null);
            check=(CatCareSkinClearance)constructor.Invoke(new object[]{weighted,parts,0f});
            ResetPoints();Physics.SyncTransforms();yield return null;

            Observe("initialAllClear",true);
            SetPoint(120,cube.transform.position);Observe("firstLateVertexReject",false);
            Assert.That(check.LastVertices,Is.EqualTo(121),"Calibration: first rejection lies late in the ordinary source order.");
            Observe("sameCurrentWitnessAgain",false);
            Assert.That(check.LastVertices,Is.EqualTo(1),"Only ordering changed; the witness must be recalculated and physically retested.");
            // Move the formerly blocked bone: old rejection is not permission.
            ResetPoints();Observe("pastRejectNowNewSourceClear",true);
            SetPoint(1,cube.transform.position);Observe("pastClearNowDifferentVertexReject",false);
            ResetPoints();

            // Overflow the six-index preference and then clear all of them.
            // LastVertices==Count proves every source vertex is still visited
            // exactly once, including indices that were preferred and evicted.
            for(int n=0;n<9;n++)
            {
                ResetPoints();SetPoint(110+n,cube.transform.position);Observe("differentWitness"+n,false);
            }
            ResetPoints();Observe("allWitnessesClearedFullSweep",true);

            SetPoint(124,cube.transform.position);Observe("beforeObstacleMove",false);
            cube.transform.position+=Vector3.up*2;Physics.SyncTransforms();Observe("pastRejectMeshMovedAway",true);
            cube.transform.position-=Vector3.up*2;Physics.SyncTransforms();Observe("pastClearMeshMovedBack",false);
            solid.enabled=false;Observe("disabledObstacle",true);solid.enabled=true;Physics.SyncTransforms();Observe("enabledObstacleAgain",false);

            ResetPoints();SetPoint(117,new Vector3(2,-.004f,2));Observe("freshFloorRejection",false);
            SetPoint(117,new Vector3(2,.001f,2));Observe("floorRecovered",true);

            // Same geometric position, different explicit tolerance: there is
            // no stored distance/verdict that could survive a parameter change.
            Vector3 normal=cube.transform.right,face=cube.transform.TransformPoint(Vector3.right*.5f);
            SetPoint(127,face-normal*.0035f);Observe("deepAtStrictTolerance",false,.0029f);
            Observe("samePointAllowedLargerTolerance",true,.004f);
            Observe("samePointStrictAgain",false,.0029f);

            // Different actual source pose and reflected/rotated obstacle.
            ResetPoints();rig.transform.rotation=Quaternion.Euler(11,-27,19);rig.transform.position=new Vector3(.2f,.1f,-.3f);
            cube.transform.rotation=Quaternion.Euler(-13,71,22);cube.transform.localScale=new Vector3(-.7f,1.1f,1.5f);Physics.SyncTransforms();
            ResetPoints();Observe("newAffineSourceAllClear",true);
            SetPoint(126,cube.transform.position);Observe("newAffineSourceNewContact",false);

            // Warm allocation checks are separate from oracle, reflection,
            // logging and assertion allocations. Both outcomes run64 times.
            MeasureAllocations("warmRejected",false);
            ResetPoints();Observe("beforeWarmClear",true);MeasureAllocations("warmClear",true);

            void SetPoint(int index,Vector3 point)
            {
                var bone=bones[index];bone.position=point-bone.TransformVector(Offset);
            }
            void ResetPoints()
            {
                for(int v=0;v<Count;v++)
                {
                    bones[v].localRotation=Quaternion.Euler(v%7,v%11,v%5);
                    SetPoint(v,cube.transform.TransformPoint(new Vector3(.58f+(v%5)*.01f,-.2f+(v%9)*.04f,-.15f+(v%7)*.035f)));
                }
            }
            void Observe(string label,bool expected,float tolerance=.0029f)
            {
                Physics.queriesHitBackfaces=(rows.Count%2)==0;bool before=Physics.queriesHitBackfaces;
                bool exhaustive=Exhaustive(bones,solid,geometry,tolerance);
                bool result=check.IsClear(tolerance);
                rows.Add($"{label},{exhaustive},{result},{check.LastVertices},{check.LastRays},{check.LastTriangleTests},{!result||check.LastVertices==Count}");
                Assert.That(Physics.queriesHitBackfaces,Is.EqualTo(before),label);
                Assert.That(exhaustive,Is.EqualTo(expected),"Independent oracle calibration: "+label);
                Assert.That(result,Is.EqualTo(exhaustive),"Current-frame exhaustive equivalence: "+label);
                if(result)Assert.That(check.LastVertices,Is.EqualTo(Count),"All vertices exactly once after preference: "+label);
            }
            void MeasureAllocations(string label,bool expected)
            {
                for(int n=0;n<16;n++)check.IsClear();
                long before=GC.GetAllocatedBytesForCurrentThread(),started=System.Diagnostics.Stopwatch.GetTimestamp();int matches=0;
                for(int n=0;n<64;n++)if(check.IsClear()==expected)matches++;
                long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
                double milliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
                rows.Add(FormattableString.Invariant($"{label}_allocated{allocated}_ms{milliseconds:F4},{expected},{matches==64},{check.LastVertices},{check.LastRays},{check.LastTriangleTests},{!expected||check.LastVertices==Count}"));
                Assert.That(matches,Is.EqualTo(64));Assert.That(allocated,Is.Zero,"Warm clearance query must not allocate managed memory: "+label);
            }
        }
        finally
        {
            Physics.queriesHitBackfaces=originalBackfaces;loaded.SetValue(null,oldCatalog);
            Object.DestroyImmediate(rig);Object.DestroyImmediate(cube);Object.DestroyImmediate(mesh);Object.DestroyImmediate(skinMesh);Object.DestroyImmediate(catalog);
            Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"care-reject-order-equivalence.csv"),rows);
        }
    }

    // Independent exhaustive oracle. It reads this fixture's real source bone
    // positions directly, never the optimized weighted-skin cache or ordering.
    // Convex cube outward comes from its known centre; exact face distance is
    // analytic in world metres for its rotated/reflected nonuniform OBB.
    static bool Exhaustive(Transform[] bones,MeshCollider solid,CatMeshContactSurface.Geometry geometry,float tolerance)
    {
        bool previous=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        try
        {
            foreach(var bone in bones)
            {
                Vector3 point=bone.TransformPoint(Offset);if(point.y<-.003f)return false;
                if(!solid.enabled||!solid.gameObject.activeInHierarchy||!solid.bounds.Contains(point))continue;
                int votes=0;
                foreach(var direction in Rays)
                {
                    if(!solid.Raycast(new Ray(point,direction),out var hit,5f))continue;
                    int at=hit.triangleIndex*3;
                    Vector3 a=solid.transform.TransformPoint(geometry.vertices[geometry.triangles[at]]),
                        b=solid.transform.TransformPoint(geometry.vertices[geometry.triangles[at+1]]),c=solid.transform.TransformPoint(geometry.vertices[geometry.triangles[at+2]]);
                    Vector3 normal=Vector3.Cross(b-a,c-a);
                    if(Vector3.Dot(normal,(a+b+c)/3f-solid.transform.position)<0)normal=-normal;
                    if(Vector3.Dot(normal,direction)>0)votes++;
                }
                if(votes<4)continue;
                Vector3 local=solid.transform.InverseTransformPoint(point),scale=solid.transform.lossyScale;
                float distance=Mathf.Min((.5f-Mathf.Abs(local.x))*Mathf.Abs(scale.x),
                    (.5f-Mathf.Abs(local.y))*Mathf.Abs(scale.y),(.5f-Mathf.Abs(local.z))*Mathf.Abs(scale.z));
                if(distance>tolerance)return false;
            }
            return true;
        }
        finally{Physics.queriesHitBackfaces=previous;}
    }
}
