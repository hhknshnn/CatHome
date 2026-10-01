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

public sealed class SupportedFootCandidateDiagnosticTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    PreparedInteractionStartTests fixture;
    Mesh baked;
    readonly List<string> rows=new List<string>();
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();
    QaExactMeshContact metric;
    static object Invoke(object value,string method,params object[] args)=>value.GetType().GetMethod(method,Hidden).Invoke(value,args);
    static T Read<T>(object value,string field)=>(T)value.GetType().GetField(field,Hidden).GetValue(value);
    static object Leg(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    [SetUp]public void Before()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();baked=new Mesh();metric=new QaExactMeshContact(topology);
        rows.Add("activity,frame,pose,phase,leg,witness,candidate,rawHit,hitNormalDot,originalNormalDot,rawTarget,preparedTarget,supportGate,sceneBoxGate,lateralShift,footResidual,predictionError,actualOwnerLimbDepth,deepestVertex,deepestCollider,witnessActualDepth,sourceRootDelta,sourceYawDelta");
    }
    [UnityTearDown]public IEnumerator After()
    {
        fixture?.After();File.WriteAllLines(Root+"/supported-foot-candidate-diagnostic.csv",rows);
        if(baked!=null)Object.DestroyImmediate(baked);metric?.Clear();topology.Clear();
        yield return RoomPlayModeSupport.WaitForPendingContactData();
    }
    [UnityTest,Timeout(90000)]
    public IEnumerator TwoWorstPaws_SevenMeasuredSupportCandidates_SeparateGeometryFromBoxRefusal()
    {
        foreach(string product in new[]{"garden.hammock","loft.floor-cushions"})
        {
            yield return (IEnumerator)Invoke(fixture,"Prepare",product.StartsWith("garden")?"Garden_Level01":"SecondFloor_Level01");
            var cat=Read<CatMovement>(fixture,"cat");var activity=CatActivity.Registered.Single(a=>a.StoreProductId==product&&a.gameObject.scene==cat.gameObject.scene);
            object[] args={activity,default(CatActivityStart),null};Assert.That((bool)Invoke(fixture,"FindReadyPose",args),Is.True,(string)args[2]);
            yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();RoomPlayModeSupport.ProvisionNeeds();
            Assert.That(activity.TryStart(cat),Is.True);bool captured=false;float deadline=Time.realtimeSinceStartup+30;
            while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();var animation=cat.GetComponent<CatActivityAnimation>();
                if(activity.IsWaitingForRestStop&&activity.RestingSeconds>=1.2f)activity.RequestRestStop();
                bool match=product.StartsWith("garden")?animation.CurrentPose==CatActivityPose.StandUp&&animation.NativeJumpPhase>=.94f:
                    animation.CurrentPose==CatActivityPose.TowelJumpUp&&animation.NativeJumpPhase>=.90f&&animation.NativeJumpPhase<1f;
                if(!match)continue;
                Capture(cat,activity,product.StartsWith("garden")?3:0,product.StartsWith("garden")?3042:536);captured=true;break;
            }
            if(activity.IsRunning)activity.CancelForTransition();yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(captured,Is.True,product+" diagnostic source phase was not reached");
        }
        Assert.That(rows.Count,Is.GreaterThanOrEqualTo(3));
    }
    void Capture(CatMovement cat,CatActivity activity,int legIndex,int witness)
    {
        var motion=cat.GetComponent<CatMeasuredSupportMotion>();Assert.That(motion!=null&&motion.IsActive,Is.True);
        var animation=cat.GetComponent<CatActivityAnimation>();var animator=cat.GetComponentInChildren<Animator>();
        var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();var tag=cat.GetComponentInChildren<CatBreedVisualTag>();
        var prediction=CatSupportedLimbSkin.Bind(CatCareSkinCatalog.Load().Find(tag.BreedId),animator,skin);Assert.That(prediction,Is.Not.Null);
        var data=Read<Array>(motion,"legs");object leg=data.GetValue(legIndex);
        var transforms=animator.GetComponentsInChildren<Transform>(true);
        var p=transforms.Select(t=>t.localPosition).ToArray();var q=transforms.Select(t=>t.localRotation).ToArray();var s=transforms.Select(t=>t.localScale).ToArray();
        bool adjusted=Read<bool>(motion,"adjusted");Vector3 originalTarget=(Vector3)Leg(leg,"target"),desired=(Vector3)Leg(leg,"desiredLower");
        Vector3 root=cat.transform.position;Quaternion heading=cat.transform.rotation;
        Vector3 up=Read<Vector3>(motion,"up"),visualPost=animator.transform.position;
        Vector3[] current=World(skin);var colliders=activity.GetComponentsInChildren<MeshCollider>().Where(c=>c.enabled&&c.sharedMesh!=null&&!c.isTrigger).ToArray();
        bool wasBackfaces=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        try
        {
            motion.Restore();Vector3 sourceVisual=animator.transform.position;Assert.That(prediction.Capture(),Is.True);
            var sourceP=transforms.Select(t=>t.localPosition).ToArray();var sourceQ=transforms.Select(t=>t.localRotation).ToArray();var sourceS=transforms.Select(t=>t.localScale).ToArray();
            Restore(p,q,s);Vector3 translation=visualPost-sourceVisual;
            var candidates=new List<(string name,Vector3 target,bool hit,float normal,float original)>{("current",originalTarget,false,0,0)};
            foreach(var axis in new[]{up,-up,cat.transform.right,-cat.transform.right,cat.transform.forward,-cat.transform.forward})
            {
                MeshCollider selected=null;RaycastHit nearest=default;float distance=float.PositiveInfinity;
                foreach(var collider in colliders)
                    if(collider.bounds.Contains(current[witness])&&collider.Raycast(new Ray(current[witness],axis),out var hit,.6f)&&hit.distance<distance)
                    {distance=hit.distance;nearest=hit;selected=collider;}
                if(selected==null)continue;
                var shape=topology.Get(selected.sharedMesh);int at=nearest.triangleIndex*3;
                Vector3 a=selected.transform.TransformPoint(shape.vertices[shape.triangles[at]]),b=selected.transform.TransformPoint(shape.vertices[shape.triangles[at+1]]),c=selected.transform.TransformPoint(shape.vertices[shape.triangles[at+2]]);
                Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
                candidates.Add(("exit-"+candidates.Count,originalTarget+axis*(distance+.008f),true,Vector3.Dot(nearest.normal,axis),Vector3.Dot(normal,axis)));
            }
            foreach(var candidate in candidates)
            {
                Restore(p,q,s);leg.GetType().GetField("target").SetValue(leg,originalTarget);
                object[] prepared={leg,candidate.target,false};bool allowed=(bool)Invoke(motion,"TrySupportCandidate",prepared);Vector3 target=(Vector3)prepared[1];
                bool scene=(bool)Invoke(motion,"CandidateSceneClear",leg,target);
                if(!prediction.TryPose(legIndex,target,desired,translation,out var pose))continue;
                Restore(sourceP,sourceQ,sourceS);animator.transform.position+=translation;
                leg.GetType().GetField("target").SetValue(leg,target);Invoke(motion,"Solve",leg);
                var actual=World(skin);float error=0,maximum=0,witnessDepth=0;int deepest=-1;string obstacle=string.Empty;
                foreach(int slot in prediction.Vertices(legIndex))
                {
                    int vertex=prediction.SourceIndex(slot);error=Mathf.Max(error,Vector3.Distance(prediction.Point(legIndex,slot,pose),actual[vertex]));
                    foreach(var collider in colliders)
                    {
                        float depth=InsideDepth(collider,actual[vertex]);
                        if(vertex==witness)witnessDepth=Mathf.Max(witnessDepth,depth);
                        if(depth<=maximum)continue;maximum=depth;deepest=vertex;obstacle=collider.name;
                    }
                }
                float residual=Vector3.Distance(((Transform)Leg(leg,"foot")).position,target);
                rows.Add(string.Join(",",activity.StoreProductId,Time.frameCount,animation.CurrentPose,F(animation.NativeJumpPhase),legIndex,witness,candidate.name,candidate.hit,F(candidate.normal),F(candidate.original),V(candidate.target),V(target),allowed,scene,
                    F(Vector3.ProjectOnPlane(target-(Vector3)Leg(leg,"sourceFoot"),up).magnitude),F(residual),F(error),F(maximum),deepest,obstacle,F(witnessDepth),F(Vector3.Distance(root,cat.transform.position)),F(Quaternion.Angle(heading,cat.transform.rotation))));
                Assert.That(error,Is.LessThanOrEqualTo(.0001f),"Pure weighted two-link candidate differs from actual same-frame source skin");
            }
            void Restore(Vector3[] positions,Quaternion[] rotations,Vector3[] scales)
            {for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}}
        }
        finally
        {
            for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=p[i];transforms[i].localRotation=q[i];transforms[i].localScale=s[i];}
            leg.GetType().GetField("target").SetValue(leg,originalTarget);typeof(CatMeasuredSupportMotion).GetField("adjusted",Hidden).SetValue(motion,adjusted);
            Physics.queriesHitBackfaces=wasBackfaces;
        }
        var final=World(skin);for(int v=0;v<current.Length;v++)Assert.That(Vector3.Distance(current[v],final[v]),Is.LessThan(.000002f),"Diagnostic must restore the same visible frame");
        Assert.That(cat.transform.position,Is.EqualTo(root));Assert.That(cat.transform.rotation,Is.EqualTo(heading));
    }
    float InsideDepth(MeshCollider collider,Vector3 point)
    {
        if(!collider.bounds.Contains(point))return 0;var shape=topology.Get(collider.sharedMesh);int votes=0;
        foreach(var axis in new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
        {
            if(!collider.Raycast(new Ray(point,axis),out var hit,3))continue;int at=hit.triangleIndex*3;
            Vector3 a=collider.transform.TransformPoint(shape.vertices[shape.triangles[at]]),b=collider.transform.TransformPoint(shape.vertices[shape.triangles[at+1]]),c=collider.transform.TransformPoint(shape.vertices[shape.triangles[at+2]]);
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),axis)>0)votes++;
        }
        return votes>=4?metric.Measure(collider.sharedMesh,collider.transform,point).distance:0;
    }
    Vector3[] World(SkinnedMeshRenderer skin){skin.BakeMesh(baked,true);return baked.vertices.Select(skin.transform.TransformPoint).ToArray();}
    static string F(float value)=>value.ToString("R",CultureInfo.InvariantCulture);
    static string V(Vector3 value)=>"\""+F(value.x)+";"+F(value.y)+";"+F(value.z)+"\"";
}
#endif
