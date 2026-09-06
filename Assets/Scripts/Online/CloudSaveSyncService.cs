using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatHome.Economy;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using UnityEngine;

public enum CloudSaveSyncState
{
    Offline = 0,
    Syncing = 1,
    Synced = 2,
    Error = 3
}

public enum CloudSaveResolution
{
    AlreadySynced = 0,
    UseCloud = 1,
    UseDevice = 2
}

public sealed class CloudSaveSnapshot
{
    public string Json { get; internal set; }
    public string Hash { get; internal set; }
    public DateTime SavedUtc { get; internal set; }
    public DateTime? CloudModifiedUtc { get; internal set; }
    public int HomeLevel { get; internal set; }
    public long Coins { get; internal set; }
    public long Diamonds { get; internal set; }
    public int Collected { get; internal set; }
    public string RoomId { get; internal set; }
}

/// <summary>
/// Pure save-summary/hash codec shared by runtime Cloud Save and EditMode tests.
/// It never reads account identifiers or mutates gameplay state.
/// </summary>
public static class CloudSaveSnapshotCodec
{
    public static CloudSaveResolution DecideResolution(string localHash, string cloudHash,
        string rememberedLocalHash, string rememberedWriteLock, string currentWriteLock,
        DateTime localSavedUtc, DateTime cloudSavedUtc)
    {
        if (string.Equals(localHash, cloudHash, StringComparison.Ordinal))
            return CloudSaveResolution.AlreadySynced;

        bool localUnchanged = string.Equals(rememberedLocalHash, localHash,
            StringComparison.Ordinal);
        bool cloudUnchanged = !string.IsNullOrEmpty(rememberedWriteLock) &&
                              string.Equals(rememberedWriteLock, currentWriteLock,
                                  StringComparison.Ordinal);
        if (localUnchanged && !cloudUnchanged)
            return CloudSaveResolution.UseCloud;
        if (cloudUnchanged && !localUnchanged)
            return CloudSaveResolution.UseDevice;

        // Both copies changed (or this is the first differing two-copy sync).
        // Product policy is seamless CONTINUE: newest wins, cloud wins exact ties.
        if (localSavedUtc == DateTime.MinValue)
            return CloudSaveResolution.UseCloud;
        if (cloudSavedUtc == DateTime.MinValue)
            return CloudSaveResolution.UseDevice;
        return localSavedUtc.ToUniversalTime() > cloudSavedUtc.ToUniversalTime()
            ? CloudSaveResolution.UseDevice
            : CloudSaveResolution.UseCloud;
    }

    public static bool TryCreate(string json, DateTime? cloudModifiedUtc,
        out CloudSaveSnapshot snapshot)
    {
        snapshot = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        CatHomeSaveData data;
        try
        {
            data = JsonUtility.FromJson<CatHomeSaveData>(json);
        }
        catch
        {
            return false;
        }

        if (data == null || data.version <= 0 ||
            data.version > CatHomeSaveSystem.CurrentSaveVersion)
            return false;

        DateTime savedUtc = DateTime.MinValue;
        DateTime.TryParse(data.lastSaveUtc, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal, out savedUtc);
        long homeXp = data.homeProgression?.homeXp ?? 0L;
        snapshot = new CloudSaveSnapshot
        {
            Json = json,
            Hash = ComputeHash(json),
            SavedUtc = savedUtc,
            CloudModifiedUtc = cloudModifiedUtc?.ToUniversalTime(),
            HomeLevel = HomeProgressionService.LevelForXp(homeXp),
            Coins = Balance(data, CurrencyType.Coin, data.coins),
            Diamonds = Balance(data, CurrencyType.Diamond, data.diamonds),
            Collected = data.homeStore?.ownedProductIds?.Length ?? 0,
            RoomId = string.IsNullOrWhiteSpace(data.homeStore?.currentRoomId)
                ? HomeRoomService.LivingRoomId
                : data.homeStore.currentRoomId
        };
        return true;
    }

    public static string ComputeHash(string value)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2"));
            return builder.ToString();
        }
    }

    private static long Balance(CatHomeSaveData data, CurrencyType type, long fallback)
    {
        string key = CurrencyCatalog.GetSaveKey(type);
        CurrencyBalanceEntry[] entries = data.economy?.balances;
        if (entries != null)
            for (int i = 0; i < entries.Length; i++)
                if (entries[i] != null && entries[i].currencyKey == key)
                    return Math.Max(0L, entries[i].amount);
        return Math.Max(0L, fallback);
    }
}

/// <summary>
/// Synchronizes Cat Home's complete v11 save through Unity Cloud Save Player Files.
/// A cached hash + write lock distinguishes one-sided edits from true two-device
/// changes. Sync never interrupts CONTINUE: newest wins and the replaced copy is
/// retained as a local support backup.
/// </summary>
public static class CloudSaveSyncService
{
    public const string CloudFileKey = "cat_home_save_v11";
    private const string CachePrefix = "cat-home.cloud-sync.";
    private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

    private static int saveGeneration;
    private static bool subscribed;
    private static string lastMessageKey = "cloud.status.offline";
    private static CloudSaveSyncState state = CloudSaveSyncState.Offline;

    public static event Action Changed;

    public static CloudSaveSyncState State => state;
    public static string LastMessageKey => lastMessageKey;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (subscribed)
            return;
        CatHomeSaveSystem.LocalSaveWritten += HandleLocalSaveWritten;
        subscribed = true;
        if (UnityServices.State == ServicesInitializationState.Initialized &&
            AccountIdentityService.IsGoogleConnected &&
            AuthenticationService.Instance.IsSignedIn)
            _ = SyncNowAsync();
    }

    public static async Task<bool> SyncNowAsync()
    {
        if (EditorQaSession.IsActive) return false;
        if (!CanUseCloud())
        {
            SetState(CloudSaveSyncState.Offline, "cloud.status.offline");
            return false;
        }

        await Gate.WaitAsync();
        try
        {
            SetState(CloudSaveSyncState.Syncing, "cloud.status.syncing");
            if (!TryReadLocal(out CloudSaveSnapshot local))
            {
                CatHomeSaveSystem.SaveNow(true);
                TryReadLocal(out local);
            }

            FileItem remoteMetadata = await FindRemoteMetadataAsync();
            if (remoteMetadata == null)
            {
                if (local == null)
                {
                    SetState(CloudSaveSyncState.Synced, "cloud.status.empty");
                    return true;
                }
                await UploadAsync(local, null);
                return true;
            }

            byte[] bytes = await CloudSaveService.Instance.Files.Player.LoadBytesAsync(
                CloudFileKey);
            string remoteJson = Encoding.UTF8.GetString(bytes);
            if (!CloudSaveSnapshotCodec.TryCreate(
                    remoteJson, remoteMetadata.Modified, out CloudSaveSnapshot cloud))
            {
                SetState(CloudSaveSyncState.Error, "cloud.status.invalid");
                return false;
            }

            if (local == null)
                return ApplyCloud(cloud, remoteMetadata.WriteLock);

            string rememberedHash = PlayerPrefs.GetString(LocalHashKey(), string.Empty);
            string rememberedLock = PlayerPrefs.GetString(WriteLockKey(), string.Empty);
            bool bothChanged = !string.Equals(rememberedHash, local.Hash,
                                   StringComparison.Ordinal) &&
                               (string.IsNullOrEmpty(rememberedLock) ||
                                !string.Equals(rememberedLock, remoteMetadata.WriteLock,
                                    StringComparison.Ordinal));
            switch (CloudSaveSnapshotCodec.DecideResolution(local.Hash, cloud.Hash,
                        rememberedHash, rememberedLock, remoteMetadata.WriteLock,
                        local.SavedUtc, cloud.CloudModifiedUtc ?? cloud.SavedUtc))
            {
                case CloudSaveResolution.AlreadySynced:
                    Remember(remoteMetadata.WriteLock, local.Hash);
                    SetState(CloudSaveSyncState.Synced, "cloud.status.synced");
                    return true;
                case CloudSaveResolution.UseCloud:
                    return ApplyCloud(cloud, remoteMetadata.WriteLock);
                case CloudSaveResolution.UseDevice:
                    if (bothChanged &&
                        !CatHomeSaveSystem.TryPreserveCloudShadow(cloud.Json, out _))
                    {
                        SetState(CloudSaveSyncState.Error, "cloud.status.unavailable");
                        return false;
                    }
                    await UploadAsync(local, remoteMetadata.WriteLock);
                    return true;
                default:
                    return ApplyCloud(cloud, remoteMetadata.WriteLock);
            }
        }
        catch (CloudSaveConflictException exception)
        {
            try
            {
                return await ResolveNewestAfterWriteRaceAsync();
            }
            catch (Exception recoveryException)
            {
                Debug.LogWarning("Cat Home Cloud Save write-race recovery is unavailable: " +
                                 recoveryException.Message + " (original: " +
                                 exception.Message + ")");
                SetState(CloudSaveSyncState.Error, "cloud.status.unavailable");
                return false;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Cat Home Cloud Save sync is unavailable: " + exception.Message);
            SetState(CloudSaveSyncState.Error, "cloud.status.unavailable");
            return false;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<bool> DeleteCloudAccountAndDataAsync()
    {
        if (!CanUseCloud())
            return false;
        await Gate.WaitAsync();
        try
        {
            SetState(CloudSaveSyncState.Syncing, "cloud.status.deleting");
            FileItem metadata = await FindRemoteMetadataAsync();
            if (metadata != null)
                await CloudSaveService.Instance.Files.Player.DeleteAsync(
                    CloudFileKey, new DeleteOptions { WriteLock = metadata.WriteLock });

            ClearRemembered();
            await AuthenticationService.Instance.DeleteAccountAsync();
            if (PlayerAccountService.Instance.IsSignedIn)
                PlayerAccountService.Instance.SignOut();
            AccountIdentityService.ReturnToGuestAfterDeletion();
            SetState(CloudSaveSyncState.Offline, "cloud.status.deleted");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Cat Home account deletion failed safely: " + exception.Message);
            SetState(CloudSaveSyncState.Error, "cloud.status.delete_failed");
            return false;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task UploadAsync(CloudSaveSnapshot local, string writeLock)
    {
        SaveOptions options = string.IsNullOrEmpty(writeLock)
            ? null
            : new SaveOptions { WriteLock = writeLock, RequestTimeout = 30 };
        await CloudSaveService.Instance.Files.Player.SaveAsync(
            CloudFileKey, Encoding.UTF8.GetBytes(local.Json), options);
        FileItem metadata = await CloudSaveService.Instance.Files.Player.GetMetadataAsync(
            CloudFileKey);
        Remember(metadata.WriteLock, local.Hash);
        SetState(CloudSaveSyncState.Synced, "cloud.status.synced");
    }

    private static bool ApplyCloud(CloudSaveSnapshot cloud, string writeLock)
    {
        if (!CatHomeSaveSystem.TryImportCloudSave(cloud.Json, out string report))
        {
            Debug.LogWarning("Cat Home rejected the cloud save: " + report);
            SetState(CloudSaveSyncState.Error, "cloud.status.invalid");
            return false;
        }
        Remember(writeLock, cloud.Hash);
        SetState(CloudSaveSyncState.Synced, "cloud.status.cloud_applied");
        return true;
    }

    private static async Task<FileItem> FindRemoteMetadataAsync()
    {
        List<FileItem> files = await CloudSaveService.Instance.Files.Player.ListAllAsync();
        return files.FirstOrDefault(file => file.Key == CloudFileKey);
    }

    private static async Task<bool> ResolveNewestAfterWriteRaceAsync()
    {
        FileItem metadata = await FindRemoteMetadataAsync();
        if (metadata == null || !TryReadLocal(out CloudSaveSnapshot local))
        {
            SetState(CloudSaveSyncState.Error, "cloud.status.unavailable");
            return false;
        }
        byte[] bytes = await CloudSaveService.Instance.Files.Player.LoadBytesAsync(CloudFileKey);
        if (!CloudSaveSnapshotCodec.TryCreate(Encoding.UTF8.GetString(bytes), metadata.Modified,
                out CloudSaveSnapshot cloud))
        {
            SetState(CloudSaveSyncState.Error, "cloud.status.invalid");
            return false;
        }

        CloudSaveResolution resolution = CloudSaveSnapshotCodec.DecideResolution(
            local.Hash, cloud.Hash, string.Empty, string.Empty, metadata.WriteLock,
            local.SavedUtc, cloud.CloudModifiedUtc ?? cloud.SavedUtc);
        if (resolution != CloudSaveResolution.UseDevice)
            return ApplyCloud(cloud, metadata.WriteLock);
        if (!CatHomeSaveSystem.TryPreserveCloudShadow(cloud.Json, out _))
        {
            SetState(CloudSaveSyncState.Error, "cloud.status.unavailable");
            return false;
        }
        try
        {
            await UploadAsync(local, metadata.WriteLock);
            return true;
        }
        catch (CloudSaveConflictException)
        {
            // A second concurrent writer wins the race. Load that authoritative
            // cloud copy instead of surfacing UI or retrying indefinitely.
            FileItem latest = await FindRemoteMetadataAsync();
            byte[] latestBytes = await CloudSaveService.Instance.Files.Player.LoadBytesAsync(
                CloudFileKey);
            return CloudSaveSnapshotCodec.TryCreate(Encoding.UTF8.GetString(latestBytes),
                       latest?.Modified, out CloudSaveSnapshot latestCloud) &&
                   ApplyCloud(latestCloud, latest?.WriteLock);
        }
    }

    private static bool TryReadLocal(out CloudSaveSnapshot snapshot)
    {
        snapshot = null;
        return CatHomeSaveSystem.TryExportLocalSave(out string json, out _) &&
               CloudSaveSnapshotCodec.TryCreate(json, null, out snapshot);
    }

    private static bool CanUseCloud() =>
        UnityServices.State == ServicesInitializationState.Initialized &&
        AccountIdentityService.IsGoogleConnected &&
        AuthenticationService.Instance.IsSignedIn;

    private static void HandleLocalSaveWritten()
    {
        if (!CanUseCloud())
            return;
        int generation = ++saveGeneration;
        _ = DebouncedSyncAsync(generation);
    }

    private static async Task DebouncedSyncAsync(int generation)
    {
        await Task.Delay(TimeSpan.FromSeconds(3));
        if (generation == saveGeneration)
            await SyncNowAsync();
    }

    private static string ProfileKey()
    {
        string playerId = AuthenticationService.Instance.IsSignedIn
            ? AuthenticationService.Instance.PlayerId
            : "offline";
        string hash = CloudSaveSnapshotCodec.ComputeHash(playerId);
        return hash.Substring(0, 16);
    }

    private static string WriteLockKey() => CachePrefix + ProfileKey() + ".write-lock";
    private static string LocalHashKey() => CachePrefix + ProfileKey() + ".local-hash";

    private static void Remember(string writeLock, string localHash)
    {
        PlayerPrefs.SetString(WriteLockKey(), writeLock ?? string.Empty);
        PlayerPrefs.SetString(LocalHashKey(), localHash ?? string.Empty);
        PlayerPrefs.Save();
    }

    private static void ClearRemembered()
    {
        PlayerPrefs.DeleteKey(WriteLockKey());
        PlayerPrefs.DeleteKey(LocalHashKey());
        PlayerPrefs.Save();
    }

    private static void SetState(CloudSaveSyncState value, string messageKey)
    {
        state = value;
        lastMessageKey = messageKey ?? string.Empty;
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        if (subscribed)
            CatHomeSaveSystem.LocalSaveWritten -= HandleLocalSaveWritten;
        subscribed = false;
        saveGeneration = 0;
        state = CloudSaveSyncState.Offline;
        lastMessageKey = "cloud.status.offline";
        Changed = null;
    }
}
