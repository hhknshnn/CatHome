#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class SupportedPreparationPredictionTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;CatActivity activity;Mesh baked;GameObject blocker;
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Hidden).Invoke(target,args);
    static T Read<T>(object target,string name)=>(T)target.GetType().GetField(name,Hidden).GetValue(target);
    [SetUp]public void Before(){fixture=new PreparedInteractionStartTests();fixture.Before();baked=new Mesh();}
    [UnityTearDown]public IEnumerator After()
    {
        if(blocker!=null)Object.DestroyImmediate(blocker);
        if(activity!=null&&activity.IsRunning)activity.CancelForTransition();
        if(baked!=null)Object.DestroyImmediate(baked);fixture?.After();
        yield return RoomPlayModeSupport.WaitForPendingContactData();
    }
    [UnityTest,Timeout(45000)]
    public IEnumerator NativePreparationSource_IsExact_AndPredictionCannotMoveBonesOrIgnoreWall()
    {
        float deadline=Time.realtimeSinceStartup+35;
        yield return (IEnumerator)Call(fixture,"Prepare","SecondFloor_Level01");
        var cat=Read<CatMovement>(fixture,"cat");
        var cushion=CatActivity.Registered.OfType<TowelNestActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId=="loft.floor-cushions");activity=cushion;
        object[] args={activity,default(CatActivityStart),null};Assert.That((bool)Call(fixture,"FindReadyPose",args),Is.True,(string)args[2]);
        yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(activity.TryStart(cat),Is.True);var animation=cat.GetComponent<CatActivityAnimation>();
        bool captured=false;
        while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
        {
            yield return new WaitForEndOfFrame();
            if(activity.IsWaitingForRestStop&&activity.RestingSeconds>=.2f)activity.RequestRestStop();
            if(animation.CurrentPose==CatActivityPose.StandUp&&!animation.IsNativeJump&&animation.NativeJumpPhase>=.94f){captured=true;break;}
        }
        Assert.That(captured,Is.True,"No actual final supported StandUp.");activity.StopAllCoroutines();
        Assert.That(activity.TryGetPreparedStart(cat,out var accepted),Is.True);
        Vector3 from=cat.transform.position,to=cushion.FloorPoint.position;to.y=accepted.Position.y;
        Quaternion heading=Quaternion.LookRotation(Vector3.ProjectOnPlane(to-from,Vector3.up));
        yield return (IEnumerator)typeof(CatJumpMotion).GetMethod("TurnDirectly",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{cat,from,heading});
        yield return new WaitForEndOfFrame();
        var motion=cat.GetComponent<CatMeasuredSupportMotion>();
        float carriedLift=motion.VisualLift;
        Assert.That(carriedLift,Is.GreaterThanOrEqualTo(0),"Measure the actual preceding support lift carried by BeginNativeJump.");
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>();var entry=CatJumpClearanceCatalog.Load().Find(tag.BreedId);
        var profile=CatCareSkinCatalog.Load().Find(tag.BreedId);var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();
        Assert.That(entry.supportedPreparationVersion,Is.EqualTo(CatJumpClearanceCatalog.SupportedPreparationVersion));
        Assert.That(entry.supportProfileHash,Is.EqualTo(profile.sourceHash));
        // Independently measure the same unadjusted source that BeginNativeJump restores.
        motion.Restore();
        Matrix4x4 tagInRoot=cat.transform.worldToLocalMatrix*tag.transform.localToWorldMatrix;
        Assert.That(animation.TryReadNativeJumpStartOffset(heading,out var startOffset),Is.True);
        animation.BeginNativeJump(false);float maximumError=0;int clearPredictions=0;
        foreach(float phase in entry.samples.Where(s=>s.supportSkinMatrices!=null&&s.supportSkinMatrices.Length>0&&s.phase<=CatJumpMotion.Takeoff+.000001f).Select(s=>s.phase).Distinct().OrderBy(p=>p))
        {
            Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
            motion.Restore();animation.SetNativeJumpSample(CatActivityPose.TowelJumpDown,phase,0f);Call(animation,"LateUpdate");
            var sample=entry.samples.Single(s=>Mathf.Abs(s.phase-phase)<.000001f);
            Matrix4x4 rootMatrix=Matrix4x4.TRS(from,heading,cat.transform.lossyScale),sourceMatrix=rootMatrix*tagInRoot;
            Vector3 correction=rootMatrix.MultiplyVector(startOffset)+Vector3.up*(from.y-sourceMatrix.m13+.008f+carriedLift);
            Matrix4x4 mapping=Matrix4x4.Translate(correction)*sourceMatrix;
            var matrices=sample.supportSkinMatrices.Select(m=>mapping*m).ToArray();
            long sourceBytes=GC.GetAllocatedBytesForCurrentThread();var sourceTimer=System.Diagnostics.Stopwatch.StartNew();
            var freshSource=CatSupportedLimbSkin.FromSource(profile,entry.supportBonePaths,matrices);
            var source=motion.CapturePreparationSource(tag.BreedId,profile,entry.supportBonePaths,mapping,sample.supportSkinMatrices);sourceTimer.Stop();
            Assert.That(source,Is.Not.Null);
            for(int slot=0;slot<source.VertexCount;slot++)
                Assert.That(Vector3.Distance(source.SourcePoint(slot),freshSource.SourcePoint(slot)),Is.LessThan(.000001f),"Reused source differs from a fresh source capture.");
            Assert.That(motion.CapturePreparationSource(tag.BreedId,profile,entry.supportBonePaths,mapping,sample.supportSkinMatrices),Is.SameAs(source),"Immutable source binding should be reused.");
            TestContext.WriteLine("Source phase="+phase+" ms="+sourceTimer.Elapsed.TotalMilliseconds+" bytes="+(GC.GetAllocatedBytesForCurrentThread()-sourceBytes));Assert.That(source,Is.Not.Null);
            var poses=Enumerable.Range(0,4).Select(i=>source.SourcePose(i,Vector3.zero)).ToArray();
            skin.BakeMesh(baked,true);var actual=baked.vertices;Bounds bounds=default;
            for(int slot=0;slot<source.VertexCount;slot++)
            {
                Vector3 predicted=source.CombinedPoint(slot,poses,Vector3.zero);
                Vector3 measured=skin.transform.TransformPoint(actual[source.SourceIndex(slot)]);
                maximumError=Mathf.Max(maximumError,Vector3.Distance(predicted,measured));
                if(slot==0)bounds=new Bounds(predicted,Vector3.zero);else bounds.Encapsulate(predicted);
            }
            Assert.That(maximumError,Is.LessThan(.0001f),"Future numerical preparation differs from actual original source skin.");
            var transforms=cat.GetComponentsInChildren<Transform>(true);
            var positions=transforms.Select(t=>t.position).ToArray();var rotations=transforms.Select(t=>t.rotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
            long planBytes=GC.GetAllocatedBytesForCurrentThread();var planTimer=System.Diagnostics.Stopwatch.StartNew();
            bool unblockedAvailable=motion.TryPredictNativePreparation(source,from,heading,out bool unblockedClear);planTimer.Stop();
            TestContext.WriteLine("Plan phase="+phase+" ms="+planTimer.Elapsed.TotalMilliseconds+" bytes="+(GC.GetAllocatedBytesForCurrentThread()-planBytes)+" available="+unblockedAvailable+" clear="+unblockedClear);
            TestContext.WriteLine("Reason="+motion.NumericPlanReason+" raw="+motion.NumericPlanRawDepth+" planted="+motion.NumericPlanPlantedCount+" sourceCap="+motion.NumericPlanLegacyLiftCap+" reachCap="+motion.NumericPlanReachLiftCap+" pivot="+motion.NumericPlanTiltPivot);
            TestContext.WriteLine("Planted legs="+string.Join(",",Enumerable.Range(0,4).Select(i=>i+":"+motion.NumericPlanFootPlanted(i))));
            for(int attempt=0;attempt<motion.NumericPlanAttemptCount;attempt++)
            {
                var a=motion.NumericPlanAttemptAt(attempt);
                TestContext.WriteLine("Candidate "+attempt+" lift="+a.lift+" tilt="+a.tiltDegrees+" reason="+a.diagnosticCode+" leg="+a.leg+" vertex="+a.sourceVertex+" depth="+a.depth+" margin="+a.reachMargin+" blocker="+a.blocker);
                if(a.sourceVertex>=0)foreach(var solid in cushion.GetComponentsInChildren<MeshCollider>())
                {
                    object[] candidateInside={solid,a.worldPoint,0f,null,default(Vector3)};
                    bool candidateOccupied=(bool)Call(fixture,"InsideMesh",candidateInside);
                    TestContext.WriteLine("Candidate world="+a.worldPoint+" inside="+candidateOccupied+" votes="+Read<int>(fixture,"exactLastVotes")+" actualDepth="+candidateInside[2]);
                }
            }
            if(unblockedAvailable&&unblockedClear)clearPredictions++;
            float independentDepth=(float)Call(fixture,"ActualSkinDepth");
            int rawSlot=source.Slot(motion.NumericPlanRawWitness);
            Vector3 rawPoint=rawSlot>=0?source.SourcePoint(rawSlot):Vector3.zero;
            TestContext.WriteLine("Independent raw depth="+independentDepth+" numeric witness="+motion.NumericPlanRawWitness+" point="+rawPoint);
            foreach(var owner in cushion.GetComponentsInChildren<MeshCollider>())
            {
                if(!owner.bounds.Contains(rawPoint))continue;
                object[] insideArgs={owner,rawPoint,0f,null,default(Vector3)};
                bool inside=(bool)Call(fixture,"InsideMesh",insideArgs);
                TestContext.WriteLine("Independent owner="+owner.name+" inside="+inside+" votes="+Read<int>(fixture,"exactLastVotes")+" depth="+insideArgs[2]);
            }
            AssertUnchanged();
            blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="QA exact-source blocker";
            SceneManager.MoveGameObjectToScene(blocker,cat.gameObject.scene);
            blocker.transform.position=bounds.center;blocker.transform.localScale=bounds.size+Vector3.one*.2f;
            // The production query owns freshness; moving a new scene blocker
            // must not depend on a caller or next physics tick synchronizing it.
            bool available=motion.TryPredictNativePreparation(source,from,heading,out bool clear);
            Assert.That(available&&clear,Is.False,"Shared prediction permitted a real unrelated enclosing wall.");AssertUnchanged();
            Object.DestroyImmediate(blocker);blocker=null;Physics.SyncTransforms();
            void AssertUnchanged()
            {
                for(int i=0;i<transforms.Length;i++)
                {
                    Assert.That(Vector3.Distance(transforms[i].position,positions[i]),Is.LessThan(.000001f),"Prediction moved "+transforms[i].name);
                    Assert.That(Quaternion.Angle(transforms[i].rotation,rotations[i]),Is.LessThan(.001f),"Prediction rotated "+transforms[i].name);
                    Assert.That(transforms[i].localScale,Is.EqualTo(scales[i]),"Prediction rescaled "+transforms[i].name);
                }
            }
        }
        Assert.That(clearPredictions,Is.GreaterThan(0),"Shared planner never accepted a real supported preparation; an always-refusing predictor is not a fix.");
        TestContext.WriteLine("Exact full-contact source prediction maximum world error="+maximumError+"; every catalog preparation sample received read-only planner and unrelated-wall checks. This test is not a full activity completion claim.");
    }
}
#endif
