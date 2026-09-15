using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed partial class JumpContinuityTests
{
    [UnityTest] public IEnumerator Fountain_TenBreeds_LeavesDrinkContinuously()
        => Sip_TenBreeds_LeavesDrinkContinuously("Patio_Level01",HomeRoomService.PatioId,HomeStoreService.PatioWaterFountainId);
    [UnityTest] public IEnumerator BirdBath_TenBreeds_LeavesDrinkContinuously()
        => Sip_TenBreeds_LeavesDrinkContinuously("Garden_Level01",HomeRoomService.GardenId,HomeStoreService.GardenBirdBathId);
    IEnumerator Sip_TenBreeds_LeavesDrinkContinuously(string scene,string room,string product)
    {
        yield return Prepare(scene,room);
        var activity=CatActivity.Registered.Single(a=>a.StoreProductId==product);
        foreach(var breed in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(breed.Id);yield return null;yield return null;
            string id=activity.StoreProductId+"-"+breed.Id;
            yield return Record(room,id,activity,true);
            VerifyDirectTurn(JsonUtility.FromJson<Trace>(File.ReadAllText(Folder+"/"+id+"-motion.json")),false);
        }
    }
    [UnityTest,Timeout(600000)] public IEnumerator AllRooms_JumpingActivities_UseDirectTurns()
    {
        var errors=new List<string>();int count=0;
        foreach(var room in HomeRoomService.Rooms)
        {
            yield return Prepare(room.SceneName,room.Id);
            foreach(var activity in CatActivity.Registered.Where(FurnitureBodyClearanceTests.IsJumpActivity).ToArray())
            {
                string id=string.IsNullOrEmpty(activity.StoreProductId)?activity.ActivityId:activity.StoreProductId;
                yield return Record(room.Id,id,activity);
                var trace=JsonUtility.FromJson<Trace>(File.ReadAllText(Folder+"/"+id+"-motion.json"));
                try{Verify(trace);VerifyDirectTurn(trace,false);}catch(AssertionException e){errors.Add(id+": "+e.Message);}
                count++;
            }
        }
        File.WriteAllText(Folder+"/direct-rooms-summary.txt","Routines: "+count+"\n"+string.Join("\n",errors));
        Assert.That(count,Is.EqualTo(39));Assert.That(errors,Is.Empty,string.Join("\n",errors));
    }
    [UnityTest,Timeout(600000)] public IEnumerator LivingSurfaces_TenBreedsThreeRates_TurnDirectly()
    {
        yield return Prepare("LivingRoom_Level01",HomeRoomService.LivingRoomId);
        var activities=CatActivity.Registered.OfType<LivingFurnitureActivity>().ToArray();
        Assert.That(activities.Length,Is.EqualTo(2));
        foreach(var breed in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(breed.Id);yield return null;yield return null;
            foreach(int fps in new[]{15,30,60})foreach(var activity in activities)
            {
                Time.captureFramerate=fps;string id=activity.Kind+"-"+breed.Id+"-"+fps;
                yield return Record(HomeRoomService.LivingRoomId,id,activity,true);
                var trace=JsonUtility.FromJson<Trace>(File.ReadAllText(Folder+"/"+id+"-motion.json"));
                VerifyDirectTurn(trace);
                if(activity.Kind==CatActivityKind.CoffeeTablePlay)
                {Assert.That(activity.DidPush,Is.True,id+" must still hit the prop");Assert.That(activity.ContactDistance,Is.LessThan(.10f),id+" actual paw reach");}
            }
        }
    }

    static void VerifyDirectTurn(Trace trace,bool requireTurn=true)
    {
        Assert.That(trace.frames.Any(f=>f.turning),Is.False,trace.product+" still uses marching steps");
        float duration=0;int count=0;
        for(int i=1;i<trace.frames.Count;i++)
        {
            var a=trace.frames[i-1];var b=trace.frames[i];
            if(a.nativeJump&&b.nativeJump&&a.nativePhase>=CatJumpMotion.Takeoff&&b.nativePhase<CatJumpMotion.Touchdown&&b.nativePhase>a.nativePhase)
            {
                Assert.That(Vector3.Angle(a.forward,b.forward),Is.LessThan(.1f),trace.product+" turns in the air");
                var travel=Vector3.ProjectOnPlane(b.root-a.root,Vector3.up);
                if(travel.sqrMagnitude>.000001f)Assert.That(Vector3.Angle(b.forward,travel),Is.LessThan(.1f),trace.product+" jumps sideways instead of facing its route");
            }
            // Swing rocking and an authored activity's own motion are separate
            // from the stationary turn immediately around a furniture jump.
            bool poseTurn=(b.nativeJump&&b.nativePhase>=.99f)||b.pose==nameof(CatActivityPose.StandUp);
            bool yaw=poseTurn&&b.root.y>.12f&&Vector3.Angle(a.forward,b.forward)>.1f;
            if(yaw)
            {
                duration+=b.time-a.time;count++;
                Assert.That(Vector3.Distance(a.root,b.root),Is.LessThan(.0001f),trace.product+" moves backwards/forwards during supported turn");
                VerifyLegAnatomy(trace.product,b);
            }
            else if(duration>0)
            {Assert.That(duration,Is.LessThan(.65f),trace.product+" long turn");duration=0;}
        }
        Assert.That(duration,Is.LessThan(.65f),trace.product+" long final turn");
        if(requireTurn)Assert.That(count,Is.GreaterThan(0),trace.product+" no measured turn");
    }
}
