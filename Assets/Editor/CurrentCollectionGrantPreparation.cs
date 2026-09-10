using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Prepares the user's requested collection grant. This helper deliberately has
/// no method that writes the player's save, recovery file, preferences or services.
/// The operator applies the reviewed workspace package after QA has ended.
/// </summary>
public static class CurrentCollectionGrantPreparation
{
    public const int ExpectedRoomProducts = 80, ExpectedCatProducts = 17;
    public static readonly string[] AllowedJsonPaths = {
        "homeStore.ownedProductIds", "homeStore.storedProductIds"
    };

    [Serializable]
    public sealed class Package
    {
        public string preparedJsonPath, manifestPath, sourceSha256, preparedSha256;
    }

    public static string[] CurrentRoomProductIds() => HomeStoreService.Products
        .Where(p => HomeRoomService.Rooms.Any(r => HomeStoreService.IsProductInRoomCollection(r.Id, p.Id)))
        .Select(p => p.Id).Distinct(StringComparer.Ordinal).ToArray();

    public static string[] CurrentCatProductIds() => HomeStoreService.Products
        .Where(p => CatCollectionPolicy.IsCatItem(p.Id)).Select(p => p.Id).ToArray();

    public static string[] RoomAccessProductIds() => HomeRoomService.Rooms
        .Where(r => !r.IsAlwaysUnlocked).Select(r => r.RequiredOwnershipId).Distinct(StringComparer.Ordinal).ToArray();

    /// <summary>Pure, usable by isolated QA as well as the prepared save package.</summary>
    public static HomeStoreSaveState TransformStore(HomeStoreSaveState source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        string[] rooms = CurrentRoomProductIds(), cats = CurrentCatProductIds(), access = RoomAccessProductIds();
        RequireCurrentCatalog(rooms, cats, access);

        // Keep the player's original order and unknown/history IDs. Only append
        // missing current products; a future runtime migration owns old records.
        var owned = new List<string>(source.ownedProductIds ?? Array.Empty<string>());
        var oldOwned = new HashSet<string>(owned, StringComparer.Ordinal);
        var allOwned = new HashSet<string>(owned, StringComparer.Ordinal);
        foreach (string id in rooms.Concat(cats).Concat(access)) if (allOwned.Add(id)) owned.Add(id);
        var roomSet = new HashSet<string>(rooms, StringComparer.Ordinal);
        var stored = new List<string>((source.storedProductIds ?? Array.Empty<string>()).Where(id => !roomSet.Contains(id)));
        var storedSet = new HashSet<string>(stored, StringComparer.Ordinal);
        foreach (string id in cats)
            if (!oldOwned.Contains(id) && storedSet.Add(id)) stored.Add(id);

        // This is the same stable catalog-order 5/1 rule used when the game
        // loads an invalid legacy/cloud collection. Valid displays are untouched.
        int displayed = 0; bool hasBed = false;
        foreach (string id in cats)
        {
            if (storedSet.Contains(id)) continue;
            bool bed = CatCollectionPolicy.IsBed(id);
            if (displayed >= CatCollectionPolicy.Capacity || (bed && hasBed))
            {
                if (storedSet.Add(id)) stored.Add(id);
            }
            else { displayed++; hasBed |= bed; }
        }

        var result = new HomeStoreSaveState {
            storeVersion = source.storeVersion,
            ownedProductIds = owned.ToArray(), storedProductIds = stored.ToArray(),
            placements = source.placements == null ? null : source.placements.Select(p => p?.Clone()).ToArray(),
            currentRoomId = source.currentRoomId
        };
        ValidateDisplayPolicy(result);
        return result;
    }

    public static string[] DisplayedCats(HomeStoreSaveState state)
    {
        var owned = new HashSet<string>(state.ownedProductIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        var stored = new HashSet<string>(state.storedProductIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        return CurrentCatProductIds().Where(id => owned.Contains(id) && !stored.Contains(id)).ToArray();
    }

    public static void ValidateDisplayPolicy(HomeStoreSaveState state)
    {
        var display = DisplayedCats(state);
        if (display.Length > CatCollectionPolicy.Capacity || display.Count(CatCollectionPolicy.IsBed) > 1)
            throw new InvalidOperationException("Prepared CAT display violates the living-room 5-item/1-bed policy.");
    }

    public static string TransformJson(string originalJson)
    {
        var root = Parse(originalJson);
        var store = root["homeStore"] as JObject;
        if (store == null) throw new InvalidDataException("The source save must contain its existing homeStore object.");
        RequireIdArray(store, "ownedProductIds"); RequireIdArray(store, "storedProductIds");
        var state = JsonUtility.FromJson<HomeStoreSaveState>(store.ToString(Formatting.None));
        var transformed = TransformStore(state);
        store["ownedProductIds"] = new JArray(transformed.ownedProductIds);
        store["storedProductIds"] = new JArray(transformed.storedProductIds);
        string result = root.ToString(Formatting.Indented);
        AssertOnlyAuthorizedFieldsChanged(originalJson, result);
        return result;
    }

    public static void AssertOnlyAuthorizedFieldsChanged(string originalJson, string preparedJson)
    {
        var original = Parse(originalJson); var prepared = Parse(preparedJson);
        foreach (var root in new[] { original, prepared })
        {
            var store = root["homeStore"] as JObject;
            if (store == null) throw new InvalidDataException("Missing homeStore during preservation check.");
            store.Remove("ownedProductIds"); store.Remove("storedProductIds");
        }
        if (!JToken.DeepEquals(original, prepared))
            throw new InvalidDataException("An unrelated save value changed while preparing collection ownership.");
    }

    /// <summary>
    /// Reads a workspace backup whose SHA-256 the operator already recorded and
    /// emits a reviewable JSON plus manifest inside the project. No live-save I/O.
    /// </summary>
    public static Package PrepareWorkspacePackageFromBackup(string workspaceBackupPath, string expectedSourceSha256, string outputDirectory)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Prepare the package from Edit Mode.");
        string source = RequireWorkspacePath(workspaceBackupPath), output = RequireWorkspacePath(outputDirectory);
        if (string.IsNullOrWhiteSpace(expectedSourceSha256)) throw new ArgumentException("An expected backup hash is required.");
        byte[] sourceBytes = File.ReadAllBytes(source);
        string sourceHash = Sha256(sourceBytes);
        if (!string.Equals(sourceHash, expectedSourceSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The source backup no longer matches the approved baseline hash.");
        string original = File.ReadAllText(source, Encoding.UTF8);
        string transformed = TransformJson(original);
        byte[] preparedBytes = new UTF8Encoding(false).GetBytes(transformed);
        var before = JsonUtility.FromJson<HomeStoreSaveState>(Parse(original)["homeStore"].ToString(Formatting.None));
        var after = TransformStore(before);
        Directory.CreateDirectory(output);
        var package = new Package {
            sourceSha256 = sourceHash, preparedSha256 = Sha256(preparedBytes),
            preparedJsonPath = Path.Combine(output, "prepared-current-collection.json"),
            manifestPath = Path.Combine(output, "prepared-current-collection-manifest.json")
        };
        // Never overwrite an existing review package with a different baseline.
        if (File.Exists(package.preparedJsonPath) && Sha256(File.ReadAllBytes(package.preparedJsonPath)) != package.preparedSha256)
            throw new IOException("A different prepared package already exists; use a new review directory.");
        var beforeOwned = new HashSet<string>(before.ownedProductIds ?? Array.Empty<string>());
        var manifest = new JObject {
            ["createdUtc"] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["sourceBackupPath"] = source, ["sourceSha256"] = package.sourceSha256,
            ["preparedJsonPath"] = package.preparedJsonPath, ["preparedSha256"] = package.preparedSha256,
            ["allowedJsonPaths"] = new JArray(AllowedJsonPaths),
            ["currentRoomProductCount"] = CurrentRoomProductIds().Length,
            ["currentCatProductCount"] = CurrentCatProductIds().Length,
            ["roomAccessProductCount"] = RoomAccessProductIds().Length,
            ["addedOwnershipIds"] = new JArray(after.ownedProductIds.Where(id => !beforeOwned.Contains(id))),
            ["displayedCatsBefore"] = new JArray(DisplayedCats(before)),
            ["displayedCatsAfter"] = new JArray(DisplayedCats(after)),
            ["newlyStoredOverflowCats"] = new JArray(after.storedProductIds.Except(before.storedProductIds ?? Array.Empty<string>()).Where(CatCollectionPolicy.IsCatItem)),
            ["unrelatedJsonValuesPreserved"] = true,
            ["realSaveWritten"] = false,
            ["applicationRequirement"] = "End QA and keep Play off. Recheck the real save against sourceSha256, back it up immediately, then atomically replace primary and recovery with these identical prepared bytes. Verify hashes and unrelated JSON values before opening Play."
        };
        File.WriteAllBytes(package.preparedJsonPath, preparedBytes);
        File.WriteAllText(package.manifestPath, manifest.ToString(Formatting.Indented), new UTF8Encoding(false));
        return package;
    }

    static void RequireCurrentCatalog(string[] rooms, string[] cats, string[] access)
    {
        if (rooms.Length != ExpectedRoomProducts || cats.Length != ExpectedCatProducts || access.Length != 7)
            throw new InvalidOperationException("The current catalog changed; review the 80 ROOM / 17 CAT / 7 room-access grant scope.");
        var granted = new HashSet<string>(rooms.Concat(cats).Concat(access), StringComparer.Ordinal);
        foreach (string id in granted)
        {
            if (!HomeStoreService.TryGetProduct(id, out _)) throw new InvalidOperationException("Unknown current product: " + id);
            string required = HomeStoreService.GetRequiredProductId(id);
            if (!string.IsNullOrEmpty(required) && !granted.Contains(required))
                throw new InvalidOperationException("Missing prerequisite in current grant scope: " + required);
        }
    }

    static void RequireIdArray(JObject store, string property)
    {
        var token = store[property];
        if (token == null || token.Type == JTokenType.Null) return;
        if (!(token is JArray array) || array.Any(t => t.Type != JTokenType.String && t.Type != JTokenType.Null))
            throw new InvalidDataException(property + " is not a saved string-ID array.");
    }

    static JObject Parse(string json)
    {
        using (var input = new StringReader(json))
        using (var reader = new JsonTextReader(input) { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Decimal })
        {
            var root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read()) throw new InvalidDataException("Unexpected data after the save object.");
            return root;
        }
    }

    static string RequireWorkspacePath(string path)
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..")) + Path.DirectorySeparatorChar;
        string full = Path.GetFullPath(path);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This preparation helper writes and reads only workspace review files.");
        return full;
    }

    public static string Sha256(byte[] bytes)
    {
        using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "");
    }
}
