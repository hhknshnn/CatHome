using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed partial class JumpContinuityTests
{
    [UnityTest, Timeout(600000)] public IEnumerator Armchair_TenBreedsAndFrameRates_DirectEntryAndExit()
    {
        yield return Prepare("LivingRoom_Level01",HomeRoomService.LivingRoomId);
        var activity=(PerchNapActivity)CatActivity.Registered.Single(a=>a.StoreProductId=="room.armchair");
        foreach(var entry in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(entry.Id);yield return null;yield return null;
            foreach(int fps in new[]{15,30,60})
            {
                Time.captureFramerate=fps;
                string id="chair-"+entry.Id+"-"+fps;
                yield return Record(HomeRoomService.LivingRoomId,id,activity,true);
                var trace=JsonUtility.FromJson<Trace>(File.ReadAllText(Folder+"/"+id+"-motion.json"));
                Assert.That(trace.frames.Any(f=>f.turning),Is.False,id+" uses paw stepping");
                var firstFlight=trace.frames.FindIndex(f=>f.nativeJump&&f.nativePhase>CatJumpMotion.Takeoff);
                Assert.That(firstFlight,Is.GreaterThan(0));
                foreach(var f in trace.frames.Take(firstFlight))
                    Assert.That(Vector3.ProjectOnPlane(f.root-activity.FloorPoint.position,Vector3.up).magnitude,
                        Is.LessThan(.005f),id+" detours before takeoff");
                var turns=new System.Collections.Generic.List<System.Collections.Generic.List<Frame>>();
                System.Collections.Generic.List<Frame> current=null;
                for(int i=1;i<trace.frames.Count;i++)
                {
                    var a=trace.frames[i-1];var b=trace.frames[i];
                    if(b.root.y>.25f&&Vector3.Angle(a.forward,b.forward)>.1f)
                    {
                        if(current==null){current=new System.Collections.Generic.List<Frame>{a};turns.Add(current);}
                        current.Add(b);
                        Assert.That(Vector3.Distance(a.root,b.root),Is.LessThan(.0001f),id+" moves back and forth during turn");
                        Assert.That(b.nativeJump && b.nativePhase<.99f,Is.False,id+" turns during flight");
                        VerifyLegAnatomy(id,b);
                    }
                    else current=null;
                }
                Assert.That(turns.Count,Is.EqualTo(2),id+" should turn once after landing and once before descent");
                foreach(var turn in turns)
                {
                    Assert.That(turn.Last().time-turn.First().time,Is.LessThan(.6f),id+" turn lingers");
                    float signed=Vector3.SignedAngle(turn.First().forward,turn.Last().forward,Vector3.up);
                    Assert.That(Mathf.Abs(signed),Is.InRange(80f,95f),id+" over-rotates");
                    for(int i=1;i<turn.Count;i++)
                        Assert.That(Vector3.SignedAngle(turn[i-1].forward,turn[i].forward,Vector3.up)*signed,
                            Is.GreaterThan(0),id+" reverses its turn");
                }
                var landing=trace.frames.Last(f=>f.nativeJump&&f.pose==nameof(CatActivityPose.TowelJumpDown));
                Assert.That(Vector3.ProjectOnPlane(landing.root-activity.FloorPoint.position,Vector3.up).magnitude,
                    Is.LessThan(.005f),id+" lands away from the open entrance");
            }
        }
    }

    [UnityTest] public IEnumerator Armchair_PauseCancel_ReleasesWithoutMovingOnPause()
    {
        yield return Prepare("LivingRoom_Level01",HomeRoomService.LivingRoomId);
        var activity=CatActivity.Registered.Single(a=>a.StoreProductId=="room.armchair");
        foreach(bool descending in new[]{false,true})
        {
            var cc=cat.GetComponent<CharacterController>();cc.enabled=false;
            var entry=activity.RoutineEntryPoint.position;entry.y=cat.transform.position.y;
            cat.transform.SetPositionAndRotation(entry,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();
            RoomPlayModeSupport.ProvisionNeeds();yield return null;
            var visual=cat.GetComponentInChildren<Animator>().transform;Vector3 visualStart=visual.localPosition;
            Assert.That(activity.TryStart(cat),Is.True);
            float deadline=Time.realtimeSinceStartup+20;bool found=false;
            var animation=cat.GetComponent<CatActivityAnimation>();
            var previous=cat.transform.rotation;
            while(Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();
                if(activity.IsWaitingForRestStop)activity.RequestRestStop();
                if(cat.transform.position.y>.25f&&Quaternion.Angle(previous,cat.transform.rotation)>1f&&
                    (descending?animation.CurrentPose==CatActivityPose.StandUp:animation.IsNativeJump&&animation.NativeJumpPhase>=.99f))
                {found=true;break;}
                previous=cat.transform.rotation;
            }
            Assert.That(found,Is.True,"Direct turn missing: descending="+descending);
            Time.timeScale=0;
            var bones=new[]{"DEF-spine","DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"}
                .Select(n=>CatBreedVisualFactory.FindDescendant(cat.transform,n)).ToArray();
            var positions=bones.Select(b=>b.position).ToArray();var rotation=cat.transform.rotation;
            yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();
            Assert.That(Quaternion.Angle(rotation,cat.transform.rotation),Is.LessThan(.001f));
            for(int i=0;i<bones.Length;i++)Assert.That(Vector3.Distance(positions[i],bones[i].position),Is.LessThan(.0001f),"Paused chair pose moved");
            CatActionState.CancelForTransition(cat);Time.timeScale=1;yield return null;yield return null;
            Assert.That(cat.IsMovementPhysicallyLocked,Is.False);Assert.That(cc.enabled,Is.True);
            Assert.That(Vector3.Distance(visualStart,visual.localPosition),Is.LessThan(.001f));
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True);
        }
        yield return Record(HomeRoomService.LivingRoomId,"chair-after-cancel",activity,true);
    }

    [Serializable] sealed class ChairGeometry
    {
        public Vector3 position, angles, scale, floor, seat, entry;
        public string[] colliders;
    }
    [UnityTest] public IEnumerator Armchair_RecordsDirectTurn()
    {
        yield return Prepare("LivingRoom_Level01", HomeRoomService.LivingRoomId);
        var activity = (PerchNapActivity)CatActivity.Registered.Single(a=>a.StoreProductId=="room.armchair");
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder+"/chair-geometry.json",JsonUtility.ToJson(new ChairGeometry{
            position=activity.transform.position,angles=activity.transform.eulerAngles,scale=activity.transform.lossyScale,
            floor=activity.FloorPoint.position,seat=activity.PerchPoint.position,entry=activity.RoutineEntryPoint.position,
            colliders=activity.GetComponentsInChildren<MeshCollider>().Select(c=>c.name+" "+c.bounds).ToArray()},true));
        var capture=cat.StartCoroutine(CaptureChair(activity));
        try { yield return Record(HomeRoomService.LivingRoomId,"chair",activity,true); }
        finally { cat.StopCoroutine(capture); }
    }
    IEnumerator CaptureChair(CatActivity activity)
    {
        int index=0; float next=Time.time;
        while(true)
        {
            yield return new WaitForEndOfFrame();
            if(!activity.IsRunning||Time.time<next)continue;
            next=Time.time+.32f;
            Directory.CreateDirectory(Folder+"/chair-visuals");
            AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("RoomInteractionReview")).First(t=>t!=null)
                .GetMethod("ActivityDetail",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{Folder+"/chair-visuals/"+(index++).ToString("D3")+".png",activity,cat});
        }
    }
}
