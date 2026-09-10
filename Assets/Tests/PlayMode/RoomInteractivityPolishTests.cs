using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RoomInteractivityPolishTests
{
    HomeStoreSaveState saved; string breed;
    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive,Is.True,"Use the isolated QA save.");
        saved=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;
    }
    [TearDown] public void After()
    {
        Time.timeScale=1;
        if(CatActivity.Active!=null)CatActivity.Active.enabled=false;
        RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(saved);CatBreedService.Select(breed);
    }

    [UnityTest]
    public IEnumerator EveryNonLivingProduct_RemainsInteractiveWithFullNeeds()
    {
        var rows=new List<string>{"room,product,activity,finished,gestures,inspection"};
        var checkedProducts=new HashSet<string>();
        foreach(var room in HomeRoomService.Rooms)
        {
            if(room.Id==HomeRoomService.LivingRoomId)continue;
            yield return RoomPlayModeSupport.LoadRoomAlone(Path.GetFileNameWithoutExtension(room.ScenePath));
            typeof(CatHomeSaveSystem).GetField("initialized",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,false);
            var state=HomeStoreSaveState.CreateDefault();
            state.ownedProductIds=HomeStoreService.Products.Where(p=>HomeStoreService.IsProductInRoomCollection(room.Id,p.Id)).Select(p=>p.Id).ToArray();
            HomeStoreService.ApplySavedState(state); yield return null;
            var cat=Object.FindAnyObjectByType<CatMovement>();
            Vector3 scale=cat.transform.localScale;
            var activities=Object.FindObjectsByType<CatActivity>(FindObjectsSortMode.None).Where(a=>HomeStoreService.IsProductInRoomCollection(room.Id,a.StoreProductId)).ToArray();
            Assert.That(activities.Select(a=>a.StoreProductId).Distinct().Count(),Is.EqualTo(10),room.Id);
            foreach(var activity in activities)
            {
                var energy=RoomPlayModeSupport.ProvisionNeeds();energy.ApplySavedValue(100);
                var hunger=Object.FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);
                if(hunger==null)hunger=energy.gameObject.AddComponent<HungerSystem>();
                var thirst=Object.FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
                if(thirst==null)thirst=energy.gameObject.AddComponent<ThirstSystem>();
                hunger.ApplySavedValue(100);thirst.ApplySavedValue(100);
                Assert.That(activity.TryStart(cat),Is.True,activity.StoreProductId+" refused a full-needs interaction");
                Time.timeScale=10;float deadline=Time.realtimeSinceStartup+12;
                bool animated=false;float gazeMotion=0;
                while(activity.IsRunning && Time.realtimeSinceStartup<deadline)
                {
            RoomPlayModeSupport.StopObservedRest(activity);
                    yield return null;
                    var pose=cat.GetComponent<CatActivityAnimation>();
                    animated |= pose.CurrentPose!=CatActivityPose.Walk;
                    var gaze=cat.GetComponent<CatFurnitureGaze>();if(gaze!=null)gazeMotion=Mathf.Max(gazeMotion,gaze.Deflection);
                }
                Assert.That(activity.IsRunning,Is.False,activity.StoreProductId+" timeout");
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(animated,Is.True,activity.StoreProductId);
                Assert.That(cat.transform.localScale,Is.EqualTo(scale));
                Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);
                Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.24f),Is.True,activity.StoreProductId);
                if(activity is SitLookActivity look)
                {
                    Assert.That(look.GestureBeats,Is.EqualTo(3));
                    Assert.That(gazeMotion,Is.GreaterThan(.5f),activity.StoreProductId+" did not track its target");
                    Assert.That(cat.GetComponent<CatFurnitureGaze>().Deflection,Is.Zero,"Gaze must restore on completion");
                }
                bool inspection=activity is SinkSipActivity sip ? sip.InspectingOnly : activity is MealTimeActivity meal && meal.InspectingOnly;
                if(activity is SinkSipActivity || activity is MealTimeActivity)Assert.That(inspection,Is.True);
                checkedProducts.Add(activity.StoreProductId);
                rows.Add(room.Id+","+activity.StoreProductId+","+activity.Kind+",True,"+(activity is SitLookActivity sit?sit.GestureBeats:0)+","+inspection);
            }
            RoomPlayModeSupport.ReleaseRoom();
        }
        Assert.That(checkedProducts.Count,Is.EqualTo(70));
        string reportDirectory = "Docs/QA/ROOM_POLISH_2026-09-07";
#if UNITY_EDITOR
        reportDirectory = UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", reportDirectory);
#endif
        Directory.CreateDirectory(reportDirectory);
        File.WriteAllLines(Path.Combine(reportDirectory, "full-needs.csv"),rows);
    }

    [UnityTest]
    public IEnumerator RestAtZeroEnergy_RecoversDuringPose_AndCancellationReleases()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("SecondFloor_Level01");
        typeof(CatHomeSaveSystem).GetField("initialized",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,false);
        var state=HomeStoreSaveState.CreateDefault();
        string roomId=HomeRoomService.Rooms.First(r=>r.SceneName=="SecondFloor_Level01").Id;
        state.ownedProductIds=HomeStoreService.Products.Where(p=>HomeStoreService.IsProductInRoomCollection(roomId,p.Id)).Select(p=>p.Id).ToArray();
        HomeStoreService.ApplySavedState(state);
        var rest=Object.FindObjectsByType<PerchNapActivity>(FindObjectsSortMode.None).First();
        var cat=Object.FindAnyObjectByType<CatMovement>();var energy=RoomPlayModeSupport.ProvisionNeeds();energy.ApplySavedValue(0);
        Assert.That(rest.TryStart(cat),Is.True);Time.timeScale=3;float deadline=Time.realtimeSinceStartup+12;
        while(!rest.IsRestingOnFurniture && rest.IsRunning && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(rest.IsRestingOnFurniture,Is.True);
        float before=energy.CurrentEnergy;
        yield return new WaitForSeconds(.6f);
        Assert.That(energy.CurrentEnergy,Is.GreaterThan(before+.2f));
        rest.enabled=false;yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(rest.IsRestingOnFurniture,Is.False);Assert.That(CatActivity.Active,Is.Null);
    }
}
