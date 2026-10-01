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
    public IEnumerator EveryNonLivingCareProduct_RefusesFullNeedsWithoutProgressOrMovement()
    {
        var checkedProducts = new HashSet<string>();
        foreach (var room in HomeRoomService.Rooms)
        {
            if (room.Id == HomeRoomService.LivingRoomId) continue;
            yield return RoomPlayModeSupport.LoadRoomAlone(Path.GetFileNameWithoutExtension(room.ScenePath));
            typeof(CatHomeSaveSystem).GetField("initialized",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,false);
            var state = HomeStoreSaveState.CreateDefault();
            state.ownedProductIds = HomeStoreService.Products.Where(p => HomeStoreService.IsProductInRoomCollection(room.Id,p.Id)).Select(p => p.Id).ToArray();
            HomeStoreService.ApplySavedState(state); yield return null;
            var cat = Object.FindAnyObjectByType<CatMovement>();
            var energy = RoomPlayModeSupport.ProvisionNeeds();
            var hunger = Object.FindAnyObjectByType<HungerSystem>() ?? energy.gameObject.AddComponent<HungerSystem>();
            var thirst = Object.FindAnyObjectByType<ThirstSystem>() ?? energy.gameObject.AddComponent<ThirstSystem>();
            foreach (var activity in CatActivity.Registered.Where(a => a.gameObject.scene == cat.gameObject.scene && a.isActiveAndEnabled && a.IsUnlocked && a.IsContentVisible && a.RequiredCareNeed != CatCareNeed.None).ToArray())
            {
                hunger.ApplySavedValue(100); thirst.ApplySavedValue(100); energy.ApplySavedValue(0);
                var position = cat.transform.position; var rotation = cat.transform.rotation;
                long bond = ProgressionService.BondXp;
                Assert.That(activity.TryStart(cat), Is.False, activity.StoreProductId);
                Assert.That(activity.IsRunning, Is.False);
                Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
                Assert.That(cat.transform.position, Is.EqualTo(position));
                Assert.That(cat.transform.rotation, Is.EqualTo(rotation));
                Assert.That(hunger.CurrentHunger, Is.EqualTo(100));
                Assert.That(thirst.CurrentThirst, Is.EqualTo(100));
                Assert.That(energy.CurrentEnergy, Is.Zero);
                Assert.That(ProgressionService.BondXp, Is.EqualTo(bond));
                if (activity is MealTimeActivity meal) Assert.That(meal.InspectingOnly, Is.False);
                if (activity is SinkSipActivity sip) Assert.That(sip.InspectingOnly, Is.False);
                checkedProducts.Add(activity.StoreProductId);
            }
            RoomPlayModeSupport.ReleaseRoom();
        }
        Assert.That(checkedProducts.Count, Is.GreaterThanOrEqualTo(5), "Kitchen food plus bathroom/kitchen/garden/patio water.");
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
        var rest=Object.FindObjectsByType<PerchNapActivity>().First();
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
