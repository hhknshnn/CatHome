using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RoomTransitionSafetyTests
{
    private HomeStoreSaveState savedStore;
    private HomeProgressionSaveState savedProgression;

    [SetUp]
    public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True,
            "Room lifecycle tests must run against an isolated QA save.");
        savedStore = HomeStoreService.CaptureState();
        savedProgression = HomeProgressionService.CaptureState();
    }

    [TearDown]
    public void After()
    {
        CatActionState.CancelForTransition(Object.FindAnyObjectByType<CatMovement>());
        HomeProgressionService.ApplySavedState(savedProgression);
        HomeStoreService.ApplySavedState(savedStore);
    }

    [UnityTest]
    public IEnumerator EveryRoomTransition_StopsOldRestBeforeLoading_AndBalancesInputBlocks()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone) yield return null;
        LevelLoader loader = null;
        float deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline)
        {
            loader = Object.FindAnyObjectByType<LevelLoader>();
            if (loader != null && loader.IsReady) break;
            yield return null;
        }
        Assert.That(loader != null && loader.IsReady, Is.True, "Initial room must finish loading.");

        foreach (var title in Object.FindObjectsByType<TitleScreen>(FindObjectsInactive.Include))
            title.gameObject.SetActive(false);
        foreach (var popup in Object.FindObjectsByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include))
        {
            popup.Close();
            popup.enabled = false;
        }
        // Ownership is a fixture setup, not a player purchase or celebration.
        foreach (var popup in Object.FindObjectsByType<CollectionCompleteCelebrationView>(FindObjectsInactive.Include))
            popup.enabled = false;
        foreach (var popup in Object.FindObjectsByType<HomeLevelUpCelebrationView>(FindObjectsInactive.Include))
            popup.enabled = false;

        HomeProgressionService.ApplySavedState(new HomeProgressionSaveState
        {
            homeXp = HomeProgressionService.CumulativeXpForLevel(30)
        });
        HomeStoreSaveState owned = HomeStoreSaveState.CreateDefault();
        owned.currentRoomId = loader.CurrentRoom.Id;
        owned.ownedProductIds = HomeStoreService.Products.Where(p =>
            CatCollectionPolicy.IsCatItem(p.Id) || HomeRoomService.Rooms.Any(r =>
                r.RequiredOwnershipId == p.Id || HomeStoreService.IsProductInRoomCollection(r.Id, p.Id)))
            .Select(p => p.Id).ToArray();
        owned.storedProductIds = owned.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(owned);
        yield return null;
        yield return null;

        string initial = loader.CurrentRoom.Id;
        string[] destinations = HomeRoomService.Rooms.Where(r => r.Id != initial)
            .Select(r => r.Id).Concat(new[] { initial }).ToArray();
        Assert.That(destinations.Length, Is.EqualTo(8));
        foreach (string roomId in destinations)
        {
            CatMovement previousCat = Object.FindAnyObjectByType<CatMovement>();
            Assert.That(previousCat, Is.Not.Null, roomId);
            CatActionState.CancelForTransition(previousCat);
            LevelSpawnPoint spawn = Object.FindObjectsByType<LevelSpawnPoint>()
                .First(p => p.gameObject.scene == previousCat.gameObject.scene &&
                            p.SpawnPointId == loader.CurrentRoom.SpawnPointId);
            CharacterController controller = previousCat.GetComponent<CharacterController>();
            controller.enabled = false;
            previousCat.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
            controller.enabled = true;
            RoomPlayModeSupport.ProvisionNeeds();
            Physics.SyncTransforms();
            var command = previousCat.GetComponent<CatCommandActivity>() ??
                previousCat.gameObject.AddComponent<CatCommandActivity>();
            Assert.That(command.Issue(CatCompanionCommand.Sit), Is.True,
                loader.CurrentRoom.Id + " should permit a rest at its clear spawn.");
            deadline = Time.realtimeSinceStartup + 5f;
            while (!command.IsWaitingForRestStop && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(command.IsWaitingForRestStop, Is.True);

            // Rejected navigation must not silently cancel a legitimate action.
            Assert.That(loader.LoadRoom("missing-room"), Is.False);
            Assert.That(loader.LoadRoom(loader.CurrentRoom.Id), Is.True);
            Assert.That(command.IsWaitingForRestStop, Is.True);

            bool startObserved = false;
            bool arrivalObserved = false;
            var otherModal = new GameObject("Independent modal owner");
            // Shared UI lives alongside the loader, not in the outgoing room.
            // An owner created in the active room would be destroyed by this
            // transition, so AcquireInputBlock would correctly ignore it.
            SceneManager.MoveGameObjectToScene(otherModal, loader.gameObject.scene);
            CatMovement arrivedCat = null;
            System.Action<HomeRoomDefinition> onStarted = room =>
            {
                startObserved = true;
                Assert.That(command.IsRunning, Is.False, "Cancel before the first additive load yield.");
                Assert.That(CatActivity.Active, Is.Null);
                Assert.That(CatActionState.IsBusy(previousCat), Is.False);
                Assert.That(previousCat.AreWorldActionsBlocked, Is.True);
            };
            System.Action<HomeRoomDefinition> onLoaded = room =>
            {
                arrivalObserved = true;
                arrivedCat = Object.FindAnyObjectByType<CatMovement>();
                Assert.That(arrivedCat, Is.Not.Null);
                Assert.That(arrivedCat.AreWorldActionsBlocked, Is.True,
                    "The loader keeps its block through the RoomLoaded notification.");
                Assert.That(otherModal != null, Is.True, "The shared modal owner must survive room unload.");
                arrivedCat.AcquireInputBlock(otherModal);
            };
            loader.RoomLoadStarted += onStarted;
            loader.RoomLoaded += onLoaded;
            try
            {
                float energyAtDeparture = Object.FindAnyObjectByType<EnergySystem>().CurrentEnergy;
                Assert.That(loader.LoadRoom(roomId), Is.True, roomId);
                Assert.That(startObserved, Is.True);
                Assert.That(previousCat.AreWorldActionsBlocked, Is.True);
                deadline = Time.realtimeSinceStartup + 30f;
                while ((!loader.IsReady || loader.IsTransitioning) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(loader.IsReady && !loader.IsTransitioning, Is.True, roomId);
                Assert.That(loader.CurrentRoom.Id, Is.EqualTo(roomId));
                Assert.That(arrivalObserved, Is.True);
                Assert.That(previousCat == null, Is.True, "Old room cat must be unloaded.");
                Assert.That(Object.FindObjectsByType<CatMovement>(), Has.Length.EqualTo(1));
                Assert.That(CatActivity.Active, Is.Null);
                Assert.That(CatActionState.IsBusy(arrivedCat), Is.False);
                Assert.That(arrivedCat.AreWorldActionsBlocked, Is.True,
                    "Loader cleanup must preserve another modal's block.");
                arrivedCat.ReleaseInputBlock(otherModal);
                Assert.That(arrivedCat.AreWorldActionsBlocked, Is.False,
                    "No loader-owned input block may remain after arrival.");
                yield return null;
                Assert.That(Object.FindAnyObjectByType<EnergySystem>().CurrentEnergy,
                    Is.LessThanOrEqualTo(energyAtDeparture + .02f),
                    "Rest in an unloaded room must not continue recovering shared energy.");
            }
            finally
            {
                loader.RoomLoadStarted -= onStarted;
                loader.RoomLoaded -= onLoaded;
                if (arrivedCat != null) arrivedCat.ReleaseInputBlock(otherModal);
                Object.DestroyImmediate(otherModal);
            }
        }
    }
}
