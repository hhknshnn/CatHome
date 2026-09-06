using System;
using CatHome.Economy;
using NUnit.Framework;
using UnityEngine;

public sealed class CloudSaveSyncTests
{
    [Test]
    public void Snapshot_UsesAuthoritativeV11ProgressAndEconomy()
    {
        var data = new CatHomeSaveData
        {
            version = CatHomeSaveSystem.CurrentSaveVersion,
            lastSaveUtc = "2026-08-21T10:30:00.0000000Z",
            coins = 2,
            diamonds = 3,
            homeProgression = new HomeProgressionSaveState { homeXp = 3000 },
            homeStore = new HomeStoreSaveState
            {
                currentRoomId = HomeRoomService.GardenId,
                ownedProductIds = new[] { "a", "b", "c" }
            },
            economy = new EconomySaveState
            {
                balances = new[]
                {
                    new CurrencyBalanceEntry(
                        CurrencyCatalog.GetSaveKey(CurrencyType.Coin), 9876),
                    new CurrencyBalanceEntry(
                        CurrencyCatalog.GetSaveKey(CurrencyType.Diamond), 42)
                }
            }
        };

        Assert.That(CloudSaveSnapshotCodec.TryCreate(
            JsonUtility.ToJson(data, true), null, out CloudSaveSnapshot snapshot), Is.True);
        Assert.That(snapshot.HomeLevel, Is.EqualTo(3));
        Assert.That(snapshot.Coins, Is.EqualTo(9876));
        Assert.That(snapshot.Diamonds, Is.EqualTo(42));
        Assert.That(snapshot.Collected, Is.EqualTo(3));
        Assert.That(snapshot.RoomId, Is.EqualTo(HomeRoomService.GardenId));
        Assert.That(snapshot.SavedUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void SnapshotHash_IsDeterministicAndContentSensitive()
    {
        string first = CloudSaveSnapshotCodec.ComputeHash("cat-home-save");
        Assert.That(first, Is.EqualTo(CloudSaveSnapshotCodec.ComputeHash("cat-home-save")));
        Assert.That(first, Is.Not.EqualTo(CloudSaveSnapshotCodec.ComputeHash("cat-home-save-2")));
        Assert.That(first.Length, Is.EqualTo(64));
    }

    [Test]
    public void Snapshot_RejectsFutureSaveSchema()
    {
        string json = JsonUtility.ToJson(new CatHomeSaveData
        {
            version = CatHomeSaveSystem.CurrentSaveVersion + 1
        });
        Assert.That(CloudSaveSnapshotCodec.TryCreate(json, null, out _), Is.False);
    }

    [TestCase("same", "same", "old", "lock-a", "lock-b", 20, 10,
        CloudSaveResolution.AlreadySynced)]
    [TestCase("local", "cloud", "local", "lock-a", "lock-b", 20, 10,
        CloudSaveResolution.UseCloud)]
    [TestCase("local", "cloud", "old", "lock-a", "lock-a", 10, 20,
        CloudSaveResolution.UseDevice)]
    [TestCase("local", "cloud", "old", "lock-a", "lock-b", 20, 10,
        CloudSaveResolution.UseDevice)]
    [TestCase("local", "cloud", "", "", "lock-b", 10, 20,
        CloudSaveResolution.UseCloud)]
    [TestCase("local", "cloud", "old", "lock-a", "lock-b", 10, 10,
        CloudSaveResolution.UseCloud)]
    public void Resolution_IsAutomaticAndUsesNewestForTwoChangedCopies(
        string local, string cloud,
        string rememberedLocal, string rememberedLock, string currentLock,
        int localMinute, int cloudMinute,
        CloudSaveResolution expected)
    {
        DateTime anchor = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc);
        Assert.That(CloudSaveSnapshotCodec.DecideResolution(local, cloud,
            rememberedLocal, rememberedLock, currentLock,
            anchor.AddMinutes(localMinute), anchor.AddMinutes(cloudMinute)),
            Is.EqualTo(expected));
    }

    [Test]
    public void LegalLinks_AreHttpsAndExposeRequiredPlayerRoutes()
    {
        StringAssert.StartsWith("https://", CatHomeLegalLinks.Privacy);
        StringAssert.EndsWith("/privacy", CatHomeLegalLinks.Privacy);
        StringAssert.EndsWith("/delete-account", CatHomeLegalLinks.DeleteAccount);
        StringAssert.EndsWith("/data", CatHomeLegalLinks.DataRequests);
    }
}
