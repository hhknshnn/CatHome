using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class HomeRoomNavigationTests
{
    [SetUp]
    public void SetUp()
    {
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        HomeRoomService.ApplySavedRoomId(HomeRoomService.LivingRoomId);
        CatRunnerSessionContext.CaptureFromHome();
    }

    [TearDown]
    public void TearDown()
    {
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        HomeRoomService.ApplySavedRoomId(HomeRoomService.LivingRoomId);
        CatRunnerSessionContext.CaptureFromHome();
    }

    [Test]
    public void Catalog_HasStableUniqueLivingBathroomAndKitchenRooms()
    {
        Assert.That(HomeRoomService.Rooms.Count, Is.EqualTo(8));
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (HomeRoomDefinition room in HomeRoomService.Rooms)
        {
            Assert.That(ids.Add(room.Id), Is.True, room.Id);
            Assert.That(room.ScenePath, Does.StartWith("Assets/Scenes/Levels/"));
            Assert.That(room.SceneName, Is.Not.Empty);
            Assert.That(room.SpawnPointId, Is.EqualTo("default"));
        }

        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeRoomsPreviewId,
                out HomeStoreProduct livingPreview),
            Is.True);
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeBathroomPreviewId,
                out HomeStoreProduct bathroomPreview),
            Is.True);
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeKitchenPreviewId,
                out HomeStoreProduct kitchenPreview),
            Is.True);
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeBedroomPreviewId,
                out HomeStoreProduct bedroomPreview),
            Is.True);

        Assert.That(HomeRoomService.GetRequiredHomeLevel(HomeRoomService.LivingRoomId),
            Is.EqualTo(1));
        Assert.That(livingPreview.RequiredLevel, Is.EqualTo(1));
        Assert.That(HomeRoomService.GetRequiredHomeLevel(HomeRoomService.BathroomId),
            Is.EqualTo(2));
        Assert.That(HomeRoomService.GetRequiredHomeLevel(HomeRoomService.KitchenId),
            Is.EqualTo(3));
        Assert.That(HomeRoomService.GetRequiredHomeLevel(HomeRoomService.BedroomId),
            Is.EqualTo(4));
        Assert.That(HomeRoomService.GetRequiredHomeLevel(HomeRoomService.GardenId),
            Is.EqualTo(5));
        Assert.That(bathroomPreview.RequiredLevel, Is.EqualTo(2));
        Assert.That(kitchenPreview.RequiredLevel, Is.EqualTo(3));
        Assert.That(bedroomPreview.RequiredLevel, Is.EqualTo(4));
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeGardenPreviewId,
                out HomeStoreProduct gardenPreview),
            Is.True);
        Assert.That(gardenPreview.RequiredLevel, Is.EqualTo(5));
        Assert.That(HomeRoomService.CurrentRoomSceneName,
            Is.EqualTo("LivingRoom_Level01"));
        Assert.That(HomeRoomService.GetOrLivingRoom(HomeRoomService.BathroomId).SceneName,
            Is.EqualTo("Bathroom_Level01"));
        Assert.That(HomeRoomService.GetOrLivingRoom(HomeRoomService.KitchenId).SceneName,
            Is.EqualTo("Kitchen_Level01"));
        Assert.That(HomeRoomService.GetOrLivingRoom(HomeRoomService.BedroomId).SceneName,
            Is.EqualTo("Bedroom_Level01"));
        Assert.That(HomeRoomService.GetOrLivingRoom(HomeRoomService.GardenId).SceneName,
            Is.EqualTo("Garden_Level01"));
        Assert.That(HomeRoomService.GetRequiredHomeLevel(HomeRoomService.BalconyId),
            Is.EqualTo(9));
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeBalconyPreviewId,
                out HomeStoreProduct balconyPreview),
            Is.True);
        Assert.That(balconyPreview.RequiredLevel, Is.EqualTo(9));
        Assert.That(HomeRoomService.GetOrLivingRoom(HomeRoomService.BalconyId).SceneName,
            Is.EqualTo("Balcony_Level01"));
        Assert.That(HomeRoomService.GetRequiredHomeLevel(HomeRoomService.PatioId),
            Is.EqualTo(10));
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomePatioPreviewId,
                out HomeStoreProduct patioPreview),
            Is.True);
        Assert.That(patioPreview.RequiredLevel, Is.EqualTo(10));
        Assert.That(HomeRoomService.GetOrLivingRoom(HomeRoomService.PatioId).SceneName,
            Is.EqualTo("Patio_Level01"));
        Assert.That(HomeRoomService.GetRequiredHomeLevel(HomeRoomService.SecondFloorId),
            Is.EqualTo(12));
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeSecondFloorPreviewId,
                out HomeStoreProduct secondFloorPreview),
            Is.True);
        Assert.That(secondFloorPreview.RequiredLevel, Is.EqualTo(12));
        Assert.That(HomeRoomService.GetOrLivingRoom(HomeRoomService.SecondFloorId).SceneName,
            Is.EqualTo("SecondFloor_Level01"));
    }

    [Test]
    public void HomeLevelThresholds_FenceRoomProgression()
    {
        SetHomeLevel(1);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.BathroomId), Is.False);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.KitchenId), Is.False);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.BedroomId), Is.False);

        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            storeVersion = HomeStoreService.SaveVersion,
            currentRoomId = HomeRoomService.BathroomId,
            ownedProductIds = MergeDistinct(
                HomeStoreService.LivingRoomCollection,
                new[] { HomeStoreService.HomeBathroomPreviewId })
        });

        SetHomeLevel(2);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.BathroomId), Is.True);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.KitchenId), Is.False);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.BedroomId), Is.False);

        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            storeVersion = HomeStoreService.SaveVersion,
            currentRoomId = HomeRoomService.KitchenId,
            ownedProductIds = MergeDistinct(
                HomeStoreService.LivingRoomCollection,
                HomeStoreService.BathroomCollection,
                HomeStoreService.HomeBathroomPreviewId,
                HomeStoreService.HomeKitchenPreviewId)
        });

        SetHomeLevel(3);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.KitchenId), Is.True);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.BedroomId), Is.False);

        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            storeVersion = HomeStoreService.SaveVersion,
            currentRoomId = HomeRoomService.BedroomId,
            ownedProductIds = MergeDistinct(
                HomeStoreService.LivingRoomCollection,
                HomeStoreService.BathroomCollection,
                HomeStoreService.KitchenCollection,
                HomeStoreService.HomeBathroomPreviewId,
                HomeStoreService.HomeKitchenPreviewId,
                HomeStoreService.HomeBedroomPreviewId)
        });

        SetHomeLevel(4);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.BedroomId), Is.True);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.GardenId), Is.False);

        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            storeVersion = HomeStoreService.SaveVersion,
            currentRoomId = HomeRoomService.GardenId,
            ownedProductIds = MergeDistinct(
                HomeStoreService.LivingRoomCollection,
                HomeStoreService.BathroomCollection,
                HomeStoreService.KitchenCollection,
                HomeStoreService.BedroomCollection,
                HomeStoreService.HomeBathroomPreviewId,
                HomeStoreService.HomeKitchenPreviewId,
                HomeStoreService.HomeBedroomPreviewId,
                HomeStoreService.HomeGardenPreviewId)
        });

        SetHomeLevel(5);
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.GardenId), Is.True);
    }

    [Test]
    public void SaveMigration_UnknownOrLockedRoomFallsBackToLivingRoom()
    {
        Assert.That(HomeRoomService.ApplySavedRoomId("removed-room"),
            Is.EqualTo(HomeRoomService.LivingRoomId));
        Assert.That(HomeRoomService.ApplySavedRoomId(HomeRoomService.BathroomId),
            Is.EqualTo(HomeRoomService.LivingRoomId));
        Assert.That(HomeRoomService.ApplySavedRoomId(HomeRoomService.KitchenId),
            Is.EqualTo(HomeRoomService.LivingRoomId));
        Assert.That(HomeRoomService.ApplySavedRoomId(HomeRoomService.BedroomId),
            Is.EqualTo(HomeRoomService.LivingRoomId));
        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            currentRoomId = HomeRoomService.KitchenId,
            ownedProductIds = new[] { HomeStoreService.HomeKitchenPreviewId }
        });
        Assert.That(HomeRoomService.CurrentRoomId,
            Is.EqualTo(HomeRoomService.LivingRoomId),
            "Kitchen ownership cannot bypass its Bathroom-room prerequisite.");
        Assert.That(HomeRoomService.CurrentRoomId,
            Is.EqualTo(HomeRoomService.LivingRoomId));
    }

    [Test]
    public void OwnedBathroom_RestoresAndRoundTripsThroughCanonicalStoreSave()
    {
        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            storeVersion = HomeStoreService.SaveVersion,
            currentRoomId = HomeRoomService.BathroomId,
            ownedProductIds = new[] { HomeStoreService.HomeBathroomPreviewId }
        });

        Assert.That(HomeStoreService.IsOwned(HomeStoreService.HomeBathroomPreviewId), Is.True);
        Assert.That(HomeRoomService.CurrentRoomId, Is.EqualTo(HomeRoomService.BathroomId));
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.BathroomId), Is.True);

        HomeStoreSaveState captured = HomeStoreService.CaptureState();
        Assert.That(captured.storeVersion, Is.EqualTo(HomeStoreService.SaveVersion));
        Assert.That(captured.currentRoomId, Is.EqualTo(HomeRoomService.BathroomId));
        CollectionAssert.Contains(captured.ownedProductIds,
            HomeStoreService.HomeBathroomPreviewId);
    }

    [Test]
    public void OwnedKitchen_RestoresAndRoundTripsThroughCanonicalStoreSave()
    {
        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            storeVersion = HomeStoreService.SaveVersion,
            currentRoomId = HomeRoomService.KitchenId,
            ownedProductIds = new[]
            {
                HomeStoreService.HomeBathroomPreviewId,
                HomeStoreService.HomeKitchenPreviewId
            }
        });

        Assert.That(HomeRoomService.CurrentRoomId, Is.EqualTo(HomeRoomService.KitchenId));
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.KitchenId), Is.True);
        HomeStoreSaveState captured = HomeStoreService.CaptureState();
        Assert.That(captured.currentRoomId, Is.EqualTo(HomeRoomService.KitchenId));
        CollectionAssert.Contains(captured.ownedProductIds,
            HomeStoreService.HomeKitchenPreviewId);
    }

    [Test]
    public void OwnedBedroom_RestoresAndRoundTripsThroughCanonicalStoreSave()
    {
        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            storeVersion = HomeStoreService.SaveVersion,
            currentRoomId = HomeRoomService.BedroomId,
            ownedProductIds = new[]
            {
                HomeStoreService.HomeBathroomPreviewId,
                HomeStoreService.HomeKitchenPreviewId,
                HomeStoreService.HomeBedroomPreviewId
            }
        });

        Assert.That(HomeRoomService.CurrentRoomId, Is.EqualTo(HomeRoomService.BedroomId));
        Assert.That(HomeRoomService.IsRoomUnlocked(HomeRoomService.BedroomId), Is.True);
        HomeStoreSaveState captured = HomeStoreService.CaptureState();
        Assert.That(captured.currentRoomId, Is.EqualTo(HomeRoomService.BedroomId));
        CollectionAssert.Contains(captured.ownedProductIds,
            HomeStoreService.HomeBedroomPreviewId);
    }

    [Test]
    public void RunnerSession_KeepsTheRoomItWasLaunchedFrom()
    {
        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            storeVersion = HomeStoreService.SaveVersion,
            currentRoomId = HomeRoomService.BathroomId,
            ownedProductIds = new[] { HomeStoreService.HomeBathroomPreviewId }
        });

        CatRunnerSessionContext.CaptureFromHome();
        HomeRoomService.ApplySavedRoomId(HomeRoomService.LivingRoomId);

        Assert.That(CatRunnerSessionContext.ReturnRoomId,
            Is.EqualTo(HomeRoomService.BathroomId));
        Assert.That(CatRunnerSessionContext.ReturnRoomSceneName,
            Is.EqualTo("Bathroom_Level01"));
    }

    [Test]
    public void LosingBathroomOwnership_RepairsPersistedRoomToLivingRoom()
    {
        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            currentRoomId = HomeRoomService.BathroomId,
            ownedProductIds = new[] { HomeStoreService.HomeBathroomPreviewId }
        });
        Assert.That(HomeRoomService.CurrentRoomId, Is.EqualTo(HomeRoomService.BathroomId));

        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            currentRoomId = HomeRoomService.BathroomId,
            ownedProductIds = Array.Empty<string>()
        });

        Assert.That(HomeRoomService.CurrentRoomId, Is.EqualTo(HomeRoomService.LivingRoomId));
    }

    [Test]
    public void LevelLoader_RejectsLockedBathroomWithoutChangingScenes()
    {
        GameObject gameObject = new GameObject("RoomLoaderTest");
        try
        {
            LevelLoader loader = gameObject.AddComponent<LevelLoader>();
            string failedRoom = null;
            string failure = null;
            loader.RoomLoadFailed += (roomId, reason) =>
            {
                failedRoom = roomId;
                failure = reason;
            };

            Assert.That(loader.LoadRoom(HomeRoomService.BathroomId), Is.False);
            Assert.That(failedRoom, Is.EqualTo(HomeRoomService.BathroomId));
            Assert.That(failure, Is.EqualTo("ROOM LOCKED"));
            Assert.That(HomeRoomService.CurrentRoomId,
                Is.EqualTo(HomeRoomService.LivingRoomId));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void LevelLoader_RejectsLockedKitchenWithoutChangingScenes()
    {
        GameObject gameObject = new GameObject("KitchenRoomLoaderTest");
        try
        {
            LevelLoader loader = gameObject.AddComponent<LevelLoader>();
            string failedRoom = null;
            string failure = null;
            loader.RoomLoadFailed += (roomId, reason) =>
            {
                failedRoom = roomId;
                failure = reason;
            };

            Assert.That(loader.LoadRoom(HomeRoomService.KitchenId), Is.False);
            Assert.That(failedRoom, Is.EqualTo(HomeRoomService.KitchenId));
            Assert.That(failure, Is.EqualTo("ROOM LOCKED"));
            Assert.That(HomeRoomService.CurrentRoomId,
                Is.EqualTo(HomeRoomService.LivingRoomId));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void BathroomFixtureKit_IsPresentAndRuntimeOptimized()
    {
        string[] paths =
        {
            "Assets/Art/Bathroom/Models/BathroomTub.fbx",
            "Assets/Art/Bathroom/Models/BathroomVanitySink.fbx",
            "Assets/Art/Bathroom/Models/BathroomToilet.fbx",
            "Assets/Art/Bathroom/Models/BathroomShower.fbx"
        };

        foreach (string path in paths)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path), Is.Not.Null, path);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.importAnimation, Is.False, path);
            Assert.That(importer.importCameras, Is.False, path);
            Assert.That(importer.importLights, Is.False, path);
            Assert.That(importer.isReadable, Is.False, path);
        }
    }

    [Test]
    public void RoomSelectorPrefab_UsesPremiumSafeAreaContract()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            RoomSelectorPanelBuilder.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<RoomSelectorPanel>(), Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<SafeAreaRect>(true), Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<LowPolyPanelGraphic>(true).Length,
            Is.GreaterThanOrEqualTo(10));
        Assert.That(prefab.GetComponentsInChildren<PremiumButtonFx>(true).Length,
            Is.GreaterThanOrEqualTo(5));

        SerializedObject serialized = new SerializedObject(
            prefab.GetComponent<RoomSelectorPanel>());
        Assert.That(serialized.FindProperty("cards").arraySize,
            Is.EqualTo(HomeRoomService.Rooms.Count));
        Assert.That(serialized.FindProperty("roomScroll").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("scrimButton").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("closeButton").objectReferenceValue, Is.Not.Null);
    }

    [Test]
    public void RoomSelectorDismissControls_ReenableWithTheCanvas()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            RoomSelectorPanelBuilder.PrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            RoomSelectorPanel controller = instance.GetComponent<RoomSelectorPanel>();
            var serialized = new SerializedObject(controller);
            Button scrim = serialized.FindProperty("scrimButton").objectReferenceValue as Button;
            Button close = serialized.FindProperty("closeButton").objectReferenceValue as Button;
            Assert.That(scrim, Is.Not.Null);
            Assert.That(close, Is.Not.Null);

            scrim.interactable = false;
            close.interactable = false;
            MethodInfo setCanvasInteractive = typeof(RoomSelectorPanel).GetMethod(
                "SetCanvasInteractive", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(setCanvasInteractive, Is.Not.Null);

            setCanvasInteractive.Invoke(controller, new object[] { true });
            Assert.That(scrim.interactable, Is.True);
            Assert.That(close.interactable, Is.True,
                "A room arrival must not leave the close control disabled on the next open.");

            setCanvasInteractive.Invoke(controller, new object[] { false });
            Assert.That(scrim.interactable, Is.False);
            Assert.That(close.interactable, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void RoomSelectorCloseMark_IsGeometricallyCenteredAndSymmetric()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            RoomSelectorPanelBuilder.PrefabPath);
        Assert.That(prefab, Is.Not.Null);

        RectTransform button = FindNamed(prefab.transform, "CloseButton") as RectTransform;
        RectTransform mark = FindNamed(prefab.transform, "CloseMark") as RectTransform;
        RectTransform slashA = FindNamed(prefab.transform, "SlashA") as RectTransform;
        RectTransform slashB = FindNamed(prefab.transform, "SlashB") as RectTransform;
        Assert.That(button, Is.Not.Null);
        Assert.That(mark, Is.Not.Null);
        Assert.That(slashA, Is.Not.Null);
        Assert.That(slashB, Is.Not.Null);
        Assert.That(mark.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(slashA.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(slashB.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(slashA.sizeDelta, Is.EqualTo(slashB.sizeDelta));
        Assert.That(Mathf.DeltaAngle(slashA.localEulerAngles.z, 45f),
            Is.EqualTo(0f).Within(0.01f));
        Assert.That(Mathf.DeltaAngle(slashB.localEulerAngles.z, -45f),
            Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void RoomSelectorPrefab_KeepsATwoColumnScrollGridWithoutOverlap()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            RoomSelectorPanelBuilder.PrefabPath);
        Assert.That(prefab, Is.Not.Null);

        Transform grid = FindNamed(prefab.transform, "RoomGrid");
        Assert.That(grid, Is.Not.Null);
        Assert.That(grid.GetComponentInParent<ScrollRect>(true), Is.Not.Null);
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        Assert.That(layout, Is.Not.Null);
        Assert.That(layout.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
        Assert.That(layout.constraintCount, Is.EqualTo(2));
        Assert.That(layout.cellSize.x, Is.GreaterThan(600f));
        Assert.That(layout.cellSize.y, Is.GreaterThan(400f));
        Assert.That(layout.spacing.x, Is.GreaterThan(8f));
        Assert.That(layout.spacing.y, Is.GreaterThan(8f));
        Assert.That(grid.GetComponent<ContentSizeFitter>(), Is.Not.Null);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var canvas = instance.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform gridRect = FindNamed(instance.transform, "RoomGrid") as RectTransform;
            Assert.That(gridRect, Is.Not.Null);
            RectTransform scrollRect = FindNamed(instance.transform, "RoomScroll") as RectTransform;
            Assert.That(scrollRect, Is.Not.Null);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
            Canvas.ForceUpdateCanvases();

            AssertNoRoomCardOverlap(gridRect, HomeRoomService.Rooms.Count);

            for (int i = 0; i < 2; i++)
            {
                Transform clone = UnityEngine.Object.Instantiate(gridRect.GetChild(0), gridRect);
                clone.name = "RoomCard_future-" + i;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
            Canvas.ForceUpdateCanvases();
            AssertNoRoomCardOverlap(gridRect, HomeRoomService.Rooms.Count + 2);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void RoomSelectorPrefab_KeepsRoomPhotosAtCapturedAspect()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            RoomSelectorPanelBuilder.PrefabPath);
        Transform preview = FindNamed(prefab.transform, "PreviewWell");
        Assert.That(preview, Is.Not.Null);
        RectTransform previewRect = preview as RectTransform;
        float viewAspect = previewRect.sizeDelta.x / previewRect.sizeDelta.y;
        Assert.That(viewAspect, Is.EqualTo(RoomPreviewFit.PhotoAspect).Within(0.08f));

        RawImage photo = preview.GetComponentInChildren<RawImage>(true);
        Assert.That(photo, Is.Not.Null);
        Assert.That(photo.texture, Is.Not.Null);
        Rect uv = photo.uvRect;
        float uvAspect = (photo.texture.width * uv.width) / (photo.texture.height * uv.height);
        Assert.That(uvAspect, Is.EqualTo(viewAspect).Within(0.08f));
    }

    [Test]
    public void HomeShopAndRoomSelector_UseCanonicalWorldPreviews()
    {
        string[] paths =
        {
            "Assets/Art/RoomPreviews/LivingRoomPreview.png",
            "Assets/Art/RoomPreviews/BathroomPreview.png",
            "Assets/Art/RoomPreviews/KitchenPreview.png",
            "Assets/Art/RoomPreviews/BedroomPreview.png",
            "Assets/Art/RoomPreviews/GardenPreview.png",
            "Assets/Art/RoomPreviews/BalconyPreview.png",
            "Assets/Art/RoomPreviews/PatioPreview.png",
            "Assets/Art/RoomPreviews/SecondFloorPreview.png"
        };
        for (int i = 0; i < paths.Length; i++)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[i]);
            Assert.That(texture, Is.Not.Null, paths[i]);
            Assert.That(texture.width, Is.GreaterThan(256), paths[i]);
            Assert.That(texture.height, Is.GreaterThan(128), paths[i]);
        }

        Assert.That(
            StoreCatalogAssets.GetIconPath(HomeStoreService.HomeRoomsPreviewId),
            Is.EqualTo("Assets/Art/RoomPreviews/LivingRoomPreview.png"));
        Assert.That(
            StoreCatalogAssets.GetIconPath(HomeStoreService.HomeBathroomPreviewId),
            Is.EqualTo("Assets/Art/RoomPreviews/BathroomPreview.png"));
        Assert.That(
            StoreCatalogAssets.GetIconPath(HomeStoreService.HomeKitchenPreviewId),
            Is.EqualTo("Assets/Art/RoomPreviews/KitchenPreview.png"));
        Assert.That(
            StoreCatalogAssets.GetIconPath(HomeStoreService.HomeBedroomPreviewId),
            Is.EqualTo("Assets/Art/RoomPreviews/BedroomPreview.png"));
        Assert.That(
            StoreCatalogAssets.GetIconPath(HomeStoreService.HomeGardenPreviewId),
            Is.EqualTo("Assets/Art/RoomPreviews/GardenPreview.png"));
        Assert.That(
            StoreCatalogAssets.GetIconPath(HomeStoreService.HomeBalconyPreviewId),
            Is.EqualTo("Assets/Art/RoomPreviews/BalconyPreview.png"));
        Assert.That(
            StoreCatalogAssets.GetIconPath(HomeStoreService.HomePatioPreviewId),
            Is.EqualTo("Assets/Art/RoomPreviews/PatioPreview.png"));
        Assert.That(
            StoreCatalogAssets.GetIconPath(HomeStoreService.HomeSecondFloorPreviewId),
            Is.EqualTo("Assets/Art/RoomPreviews/SecondFloorPreview.png"));
    }

    [Test]
    public void GardenCollection_HasProductPreviewIcons()
    {
        for (int i = 0; i < HomeStoreService.GardenCollection.Count; i++)
        {
            string id = HomeStoreService.GardenCollection[i];
            Assert.That(StoreCatalogAssets.TryGet(id, out StoreCatalogAsset asset), Is.True, id);
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(asset.IconPath);
            Assert.That(icon, Is.Not.Null, asset.IconPath);
            Assert.That(icon.width, Is.GreaterThan(64), asset.IconPath);
        }
    }

    [Test]
    public void BedroomCollection_HasProductPreviewIcons()
    {
        for (int i = 0; i < HomeStoreService.BedroomCollection.Count; i++)
        {
            string id = HomeStoreService.BedroomCollection[i];
            Assert.That(StoreCatalogAssets.TryGet(id, out StoreCatalogAsset asset), Is.True, id);
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(asset.IconPath);
            Assert.That(icon, Is.Not.Null, asset.IconPath);
            Assert.That(icon.width, Is.GreaterThan(64), asset.IconPath);
        }
    }

    [Test]
    public void SecondFloorCollection_HasRenderedProductPreviewIconsBoundToShopCards()
    {
        GameObject shop = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/UI/ShopPanel.prefab");
        Assert.That(shop, Is.Not.Null);

        for (int i = 0; i < HomeStoreService.SecondFloorCollection.Count; i++)
        {
            string id = HomeStoreService.SecondFloorCollection[i];
            Assert.That(StoreCatalogAssets.TryGet(id, out StoreCatalogAsset asset),
                Is.True, id);
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(asset.IconPath);
            Assert.That(icon, Is.Not.Null, asset.IconPath);
            Assert.That(icon.width, Is.EqualTo(512), asset.IconPath);
            Assert.That(icon.height, Is.EqualTo(512), asset.IconPath);

            Transform card = FindNamed(shop.transform, "Product_" + id);
            Assert.That(card, Is.Not.Null, id + " must have a SHOP card.");
            RawImage photo = card.GetComponentInChildren<RawImage>(true);
            Assert.That(photo, Is.Not.Null, id);
            Assert.That(photo.texture, Is.EqualTo(icon),
                id + " must use its rendered furniture preview, not the fallback glyph.");
        }
    }

    [Test]
    public void HomeShopLivingRoomCard_UsesCanonicalWorldPreview()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ShopPanel.prefab");
        Assert.That(prefab, Is.Not.Null);
        Transform living = FindNamed(prefab.transform, "Product_home.rooms-preview");
        Assert.That(living, Is.Not.Null, "SHOP HOME must include the Living Room card.");
        RawImage photo = living.GetComponentInChildren<RawImage>(true);
        Assert.That(photo, Is.Not.Null);
        Assert.That(photo.texture, Is.Not.Null);
        Assert.That(photo.texture.name, Does.Contain("LivingRoomPreview"));
    }

    [Test]
    public void RoomPreviewFit_CoverUv_MatchesTheViewAspect()
    {
        Rect square = RoomPreviewFit.CoverUv(1280f, 720f, 184f, 168f);
        float croppedAspect = (1280f * square.width) / (720f * square.height);
        Assert.That(croppedAspect, Is.EqualTo(184f / 168f).Within(0.02f));

        Rect well = RoomPreviewFit.CoverUv(1280f, 720f, 658f, 370f);
        Assert.That(well.x, Is.EqualTo(0f).Within(0.02f));
        Assert.That(well.y, Is.EqualTo(0f).Within(0.02f));
        Assert.That(well.width, Is.EqualTo(1f).Within(0.02f));
        Assert.That(well.height, Is.EqualTo(1f).Within(0.02f));
    }

    [Test]
    public void NeedBars_KeepAVisibleGapBetweenCapsules()
    {
        Assert.That(
            TopHudResponsiveLayout.NeedSpacing - TopHudResponsiveLayout.NeedBarWidth,
            Is.GreaterThanOrEqualTo(24f));
    }

    private static void AssertNoRoomCardOverlap(RectTransform gridRect, int expectedCount)
    {
        var cards = new List<RectTransform>();
        for (int i = 0; i < gridRect.childCount; i++)
        {
            Transform child = gridRect.GetChild(i);
            if (child.name.StartsWith("RoomCard_", StringComparison.Ordinal))
                cards.Add(child as RectTransform);
        }

        Assert.That(cards.Count, Is.EqualTo(expectedCount));
        for (int i = 0; i < cards.Count; i++)
        {
            Rect a = WorldRect(cards[i]);
            Transform preview = FindNamed(cards[i], "PreviewWell");
            Transform info = FindNamed(cards[i], "RoomInfoWell");
            Assert.That(preview, Is.Not.Null, cards[i].name);
            Assert.That(info, Is.Not.Null, cards[i].name);
            Assert.That(WorldRect(info as RectTransform).yMax,
                Is.LessThanOrEqualTo(WorldRect(preview as RectTransform).yMin + 2f),
                cards[i].name + " title block must sit below the photo.");

            for (int j = i + 1; j < cards.Count; j++)
            {
                Rect b = WorldRect(cards[j]);
                Assert.That(a.Overlaps(b), Is.False,
                    cards[i].name + " overlaps " + cards[j].name);
            }
        }
    }

    private static void SetHomeLevel(int level)
    {
        HomeProgressionService.ApplySavedState(new HomeProgressionSaveState
        {
            homeProgressionVersion = HomeProgressionService.SaveVersion,
            homeXp = HomeProgressionService.CumulativeXpForLevel(level)
        });
    }

    private static string[] MergeDistinct(params object[] items)
    {
        var distinct = new HashSet<string>(StringComparer.Ordinal);
        foreach (object item in items)
        {
            if (item is string id)
            {
                distinct.Add(id);
                continue;
            }

            if (item is IEnumerable<string> ids)
            {
                foreach (string productId in ids)
                    distinct.Add(productId);
            }
        }

        string[] merged = new string[distinct.Count];
        distinct.CopyTo(merged);
        return merged;
    }

    private static Transform FindNamed(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamed(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Rect WorldRect(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        float xMin = Mathf.Min(corners[0].x, corners[2].x);
        float xMax = Mathf.Max(corners[0].x, corners[2].x);
        float yMin = Mathf.Min(corners[0].y, corners[2].y);
        float yMax = Mathf.Max(corners[0].y, corners[2].y);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }
}
