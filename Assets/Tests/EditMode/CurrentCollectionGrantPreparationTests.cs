using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;

public sealed class CurrentCollectionGrantPreparationTests
{
#pragma warning disable 0649 // Populated by JsonUtility in the preservation probes.
    [Serializable] sealed class PositionProbe { public double x, y, z; }
    [Serializable] sealed class MetadataProbe { public string custom; public long counter; }
    [Serializable] sealed class PlacementProbe { public string futurePlacementMetadata; }
    [Serializable] sealed class StoreProbe { public MetadataProbe futureStoreMetadata; public PlacementProbe[] placements; }
    [Serializable] sealed class SaveProbe { public long coins; public PositionProbe position; public StoreProbe homeStore; }
    [Serializable] sealed class ManifestProbe { public bool realSaveWritten; public int currentRoomProductCount, currentCatProductCount, roomAccessProductCount; }
#pragma warning restore 0649
    static string[] Cats => CurrentCollectionGrantPreparation.CurrentCatProductIds();
    static string[] Display => Cats.Where(id => !CatCollectionPolicy.IsBed(id)).Take(4)
        .Concat(Cats.Where(CatCollectionPolicy.IsBed).Take(1)).ToArray();

    static HomeStoreSaveState ExistingStore() => new HomeStoreSaveState {
        storeVersion = 8,
        ownedProductIds = Display.Concat(new[] { "future.existing-owned", HomeStoreService.SofaId }).ToArray(),
        storedProductIds = new[] { "future.existing-stored", CurrentCollectionGrantPreparation.CurrentRoomProductIds()[0] },
        currentRoomId = HomeRoomService.BedroomId,
        placements = new[] { new HomeStorePlacementEntry(Display[0], new Vector3(-1.4f, 0f, -.6f), 180f),
            new HomeStorePlacementEntry("future.existing-owned", new Vector3(.25f, 0f, -2f), 33f) }
    };

    [Test]
    public void CurrentGrant_PreservesFiveVisibleCatsAndUnknownOwnership_WithoutAddingHistoricalDecor()
    {
        var before = ExistingStore();
        string original = JsonUtility.ToJson(before);
        var after = CurrentCollectionGrantPreparation.TransformStore(before);
        Assert.That(JsonUtility.ToJson(before), Is.EqualTo(original), "Pure transform must not mutate its input.");
        Assert.That(CurrentCollectionGrantPreparation.CurrentRoomProductIds(), Has.Length.EqualTo(80));
        Assert.That(Cats, Has.Length.EqualTo(17));
        var allCurrent = CurrentCollectionGrantPreparation.CurrentRoomProductIds().Concat(Cats).ToArray();
        Assert.That(allCurrent.All(after.ownedProductIds.Contains), Is.True);
        Assert.That(CurrentCollectionGrantPreparation.RoomAccessProductIds().All(after.ownedProductIds.Contains), Is.True);
        CollectionAssert.AreEqual(CurrentCollectionGrantPreparation.DisplayedCats(before), CurrentCollectionGrantPreparation.DisplayedCats(after));
        Assert.That(after.ownedProductIds, Does.Contain("future.existing-owned"));
        Assert.That(after.ownedProductIds, Does.Contain(HomeStoreService.SofaId), "Existing historical ownership must remain.");
        Assert.That(after.storedProductIds, Does.Contain("future.existing-stored"));
        Assert.That(CurrentCollectionGrantPreparation.CurrentRoomProductIds().Any(after.storedProductIds.Contains), Is.False);
        Assert.That(Cats.Except(before.ownedProductIds).All(after.storedProductIds.Contains), Is.True);
        var added = after.ownedProductIds.Except(before.ownedProductIds).ToArray();
        var permitted = allCurrent.Concat(CurrentCollectionGrantPreparation.RoomAccessProductIds()).ToArray();
        Assert.That(added.Except(permitted), Is.Empty, "No other historical catalog item may be newly granted.");
        Assert.That(after.currentRoomId, Is.EqualTo(before.currentRoomId));
        Assert.That(after.storeVersion, Is.EqualTo(before.storeVersion));
        for (int i = 0; i < before.placements.Length; i++)
        {
            Assert.That(after.placements[i], Is.Not.SameAs(before.placements[i]));
            Assert.That(JsonUtility.ToJson(after.placements[i]), Is.EqualTo(JsonUtility.ToJson(before.placements[i])));
        }
        foreach (var room in HomeRoomService.Rooms.Where(r => !r.IsAlwaysUnlocked))
        {
            Assert.That(after.ownedProductIds, Does.Contain(room.RequiredOwnershipId));
            string required = HomeStoreService.GetRequiredProductId(room.RequiredOwnershipId);
            if (!string.IsNullOrEmpty(required)) Assert.That(after.ownedProductIds, Does.Contain(required));
        }
    }

    [Test]
    public void InvalidLegacyDisplay_KeepsOwnershipAndStoresOverflowUsingTheCanonicalFiveOnePolicy()
    {
        var before = HomeStoreSaveState.CreateDefault(); before.ownedProductIds = Cats;
        Assert.Throws<InvalidOperationException>(() => CurrentCollectionGrantPreparation.ValidateDisplayPolicy(before));
        var after = CurrentCollectionGrantPreparation.TransformStore(before);
        var display = CurrentCollectionGrantPreparation.DisplayedCats(after);
        Assert.That(display.Length, Is.EqualTo(CatCollectionPolicy.Capacity));
        Assert.That(display.Count(CatCollectionPolicy.IsBed), Is.LessThanOrEqualTo(1));
        Assert.That(Cats.All(after.ownedProductIds.Contains), Is.True);
        Assert.That(Cats.Except(display).All(after.storedProductIds.Contains), Is.True);
        Assert.That(before.storedProductIds, Is.Empty);
        Assert.DoesNotThrow(() => CurrentCollectionGrantPreparation.ValidateDisplayPolicy(after));
        Assert.That(JsonUtility.ToJson(CurrentCollectionGrantPreparation.TransformStore(after)), Is.EqualTo(JsonUtility.ToJson(after)));
    }

    static string ExistingJson()
    {
        string store = JsonUtility.ToJson(ExistingStore());
        store = store.Replace("\"slotIndex\":0", "\"futurePlacementMetadata\":\"retain this too\",\"slotIndex\":0");
        store = store.Substring(0, store.Length - 1) + ",\"futureStoreMetadata\":{\"custom\":\"keep me\",\"counter\":9007199254740993}}";
        return "{\"saveVersion\":17,\"coins\":9007199254740993,\"diamonds\":37,\"hunger\":0.0,\"thirst\":34.125,\"energy\":99.0,\"score\":9877,\"lives\":3,\"breedId\":\"sphynx\",\"position\":{\"x\":-1.1223344556677889,\"y\":0.05,\"z\":0.025},\"futureUnknown\":{\"nested\":[true,null,\"2026-09-09T10:20:30.1234567Z\"],\"ownedProductIds\":[\"unrelated\"]},\"homeStore\":" + store + "}";
    }

    [Test]
    public void JsonGrant_ChangesOnlyTheTwoOwnershipArrays_AndRetainsFutureData()
    {
        string original = ExistingJson();
        string prepared = CurrentCollectionGrantPreparation.TransformJson(original);
        CurrentCollectionGrantPreparation.AssertOnlyAuthorizedFieldsChanged(original, prepared);
        var before = JsonUtility.FromJson<SaveProbe>(original); var after = JsonUtility.FromJson<SaveProbe>(prepared);
        Assert.That(after.coins, Is.EqualTo(9007199254740993L), "Large balances must never pass through a float.");
        Assert.That(after.position.x, Is.EqualTo(before.position.x));
        Assert.That(after.position.y, Is.EqualTo(before.position.y));
        Assert.That(after.position.z, Is.EqualTo(before.position.z));
        Assert.That(after.homeStore.futureStoreMetadata.custom, Is.EqualTo("keep me"));
        Assert.That(after.homeStore.futureStoreMetadata.counter, Is.EqualTo(9007199254740993L));
        Assert.That(after.homeStore.placements[0].futurePlacementMetadata, Is.EqualTo("retain this too"));
        Assert.That(prepared, Does.Contain("2026-09-09T10:20:30.1234567Z"));
        Assert.That(CurrentCollectionGrantPreparation.TransformJson(prepared), Is.EqualTo(prepared));
        Assert.Throws<InvalidDataException>(() => CurrentCollectionGrantPreparation.AssertOnlyAuthorizedFieldsChanged(original, prepared.Replace("9007199254740993", "0")));
        Assert.Throws<InvalidDataException>(() => CurrentCollectionGrantPreparation.AssertOnlyAuthorizedFieldsChanged(original, prepared.Replace("retain this too", "changed placement")));
        Assert.Throws<InvalidDataException>(() => CurrentCollectionGrantPreparation.AssertOnlyAuthorizedFieldsChanged(original, prepared.Replace("unrelated", "changed nested field")));
    }

    [Test]
    public void EmptyExistingDisplay_StoresEveryNewCat_AndMalformedSaveIsRejected()
    {
        var after = CurrentCollectionGrantPreparation.TransformStore(HomeStoreSaveState.CreateDefault());
        Assert.That(CurrentCollectionGrantPreparation.DisplayedCats(after), Is.Empty);
        Assert.That(Cats.All(after.storedProductIds.Contains), Is.True);
        Assert.Throws<InvalidDataException>(() => CurrentCollectionGrantPreparation.TransformJson("{\"coins\":42}"));
        Assert.Throws<InvalidDataException>(() => CurrentCollectionGrantPreparation.TransformJson("{\"homeStore\":{\"ownedProductIds\":[99]}}"));
        var duplicateFailure = Assert.Catch(() => CurrentCollectionGrantPreparation.TransformJson("{\"homeStore\":{},\"homeStore\":{}}"));
        Assert.That(duplicateFailure.GetType().Name, Is.EqualTo("JsonReaderException"));
    }

    [Test]
    public void WorkspacePackage_RequiresTheRecordedBaseline_AndNeverWritesLiveLocations()
    {
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/CollectionGrantTest-" + Guid.NewGuid().ToString("N")));
        string source = Path.Combine(folder, "baseline.json"), output = Path.Combine(folder, "review");
        Directory.CreateDirectory(folder);
        byte[] original = new UTF8Encoding(false).GetBytes(ExistingJson()); File.WriteAllBytes(source, original);
        string hash = CurrentCollectionGrantPreparation.Sha256(original);
        try
        {
            Assert.Throws<InvalidDataException>(() => CurrentCollectionGrantPreparation.PrepareWorkspacePackageFromBackup(source, new string('0', 64), output));
            Assert.That(Directory.Exists(output), Is.False);
            Assert.Throws<InvalidOperationException>(() => CurrentCollectionGrantPreparation.PrepareWorkspacePackageFromBackup(source, hash, Application.persistentDataPath));
            var package = CurrentCollectionGrantPreparation.PrepareWorkspacePackageFromBackup(source, hash, output);
            Assert.That(CurrentCollectionGrantPreparation.Sha256(File.ReadAllBytes(source)), Is.EqualTo(hash));
            Assert.That(CurrentCollectionGrantPreparation.Sha256(File.ReadAllBytes(package.preparedJsonPath)), Is.EqualTo(package.preparedSha256));
            CurrentCollectionGrantPreparation.AssertOnlyAuthorizedFieldsChanged(File.ReadAllText(source), File.ReadAllText(package.preparedJsonPath));
            var manifest = JsonUtility.FromJson<ManifestProbe>(File.ReadAllText(package.manifestPath));
            Assert.That(manifest.realSaveWritten, Is.False);
            Assert.That(manifest.currentRoomProductCount, Is.EqualTo(80));
            Assert.That(manifest.currentCatProductCount, Is.EqualTo(17));
            Assert.That(manifest.roomAccessProductCount, Is.EqualTo(7));
            Assert.That(Directory.GetFiles(output).Length, Is.EqualTo(2));
        }
        finally
        {
            // Delete only the exact, locally constructed test artifacts.
            foreach (string file in new[] { Path.Combine(output, "prepared-current-collection.json"),
                         Path.Combine(output, "prepared-current-collection-manifest.json"), source })
                if (File.Exists(file)) File.Delete(file);
            if (Directory.Exists(output)) Directory.Delete(output);
            Directory.Delete(folder);
        }
    }
}
