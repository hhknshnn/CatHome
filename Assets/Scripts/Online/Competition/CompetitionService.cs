using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CatHome.Economy;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

/// <summary>
/// Read-only Leaderboards client plus the trusted Cloud Code submission seam.
/// A failed network request never blocks or rewrites local gameplay progress.
/// </summary>
public static class CompetitionService
{
    public const string CloudCodeModule = "CatHomeCompetition";
    private const string CachePrefix = "cat-home.leaderboard-cache.";

    public static event Action Changed;
    public static bool IsBusy { get; private set; }
    public static string LastMessage { get; private set; } = string.Empty;
    /// <summary>
    /// Leaderboards use the cat name chosen during onboarding. Cat Home never asks
    /// for a second public identity and never falls back to a Google profile name.
    /// </summary>
    public static string Nickname =>
        CompetitionRules.TrySanitizeNickname(CatIdentityService.CatName, out string nickname)
            ? nickname
            : string.Empty;
    public static bool CanSubmit =>
        !EditorQaSession.IsActive && AccountIdentityService.IsGoogleConnected &&
        UnityServices.State == ServicesInitializationState.Initialized &&
        AuthenticationService.Instance.IsSignedIn;

    public static async Task<CompetitionSnapshot> LoadAsync(
        CompetitionGame game,
        CompetitionPeriod period)
    {
        string boardId = CompetitionRules.BoardId(game, period);
        SetBusy(true, "LOADING RANKINGS...");
        try
        {
            await EnsureOnlineSessionAsync();
            LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(
                boardId,
                new GetScoresOptions
                {
                    Offset = 0,
                    Limit = CompetitionRules.MaximumVisibleEntries,
                    IncludeMetadata = true
                });

            string currentId = AuthenticationService.Instance.IsSignedIn
                ? AuthenticationService.Instance.PlayerId
                : string.Empty;
            CompetitionSnapshot snapshot = FromPage(boardId, page, currentId);
            try
            {
                LeaderboardEntry own = await LeaderboardsService.Instance.GetPlayerScoreAsync(
                    boardId, new GetPlayerScoreOptions { IncludeMetadata = true });
                snapshot.currentPlayer = FromEntry(own, currentId);
            }
            catch (Exception)
            {
                snapshot.currentPlayer = null;
            }

            SaveCache(snapshot);
            LastMessage = snapshot.entries.Count == 0 ? "NO SCORES YET" : "RANKINGS UPDATED";
            return snapshot;
        }
        catch (Exception exception)
        {
            Debug.Log("Cat Home rankings are using the offline copy: " + exception.Message);
            CompetitionSnapshot cached = LoadCache(boardId);
            LastMessage = cached != null ? "OFFLINE COPY" : "RANKINGS UNAVAILABLE";
            return cached ?? Empty(boardId, true);
        }
        finally
        {
            SetBusy(false, LastMessage);
        }
    }

    public static async Task SubmitRunnerAsync(
        CatRunnerResult result,
        int comboBonus,
        int claimedScore)
    {
        if (!CanSubmit)
            return;
        string nickname = Nickname;
        if (string.IsNullOrWhiteSpace(nickname))
        {
            SetBusy(false, "NAME YOUR CAT WITH 3-14 CHARACTERS");
            return;
        }
        await SubmitAsync("SubmitRunnerScore", new Dictionary<string, object>
        {
            { "runId", result.RunId },
            { "durationSeconds", result.DurationSeconds },
            { "distance", result.Distance },
            { "coinsCollected", result.CoinsCollected },
            { "collisions", result.Collisions },
            { "comboBonus", Mathf.Max(0, comboBonus) },
            { "claimedScore", Mathf.Max(0, claimedScore) },
            { "nickname", nickname }
        });
    }

    public static async Task SubmitCatchAsync(
        string huntId,
        float durationSeconds,
        int catches,
        int comboStepsTotal,
        int strikesResolved,
        int claimedScore)
    {
        if (!CanSubmit)
            return;
        string nickname = Nickname;
        if (string.IsNullOrWhiteSpace(nickname))
        {
            SetBusy(false, "NAME YOUR CAT WITH 3-14 CHARACTERS");
            return;
        }
        await SubmitAsync("SubmitCatchScore", new Dictionary<string, object>
        {
            { "huntId", huntId ?? string.Empty },
            { "durationSeconds", Mathf.Max(0f, durationSeconds) },
            { "catches", Mathf.Max(0, catches) },
            { "comboStepsTotal", Mathf.Max(0, comboStepsTotal) },
            { "strikesResolved", Mathf.Max(0, strikesResolved) },
            { "claimedScore", Mathf.Max(0, claimedScore) },
            { "nickname", nickname }
        });
    }

    public static async Task<CompetitionRewardSummary> ClaimAvailableRewardsAsync()
    {
        if (!CanSubmit)
            return default;

        int periods = 0;
        long coins = 0;
        long diamonds = 0;
        foreach (CompetitionGame game in Enum.GetValues(typeof(CompetitionGame)))
        {
            foreach (CompetitionPeriod period in new[]
                     { CompetitionPeriod.Daily, CompetitionPeriod.Weekly })
            {
                string boardId = CompetitionRules.BoardId(game, period);
                try
                {
                    var versions = await LeaderboardsService.Instance.GetVersionsAsync(
                        boardId, new GetVersionsOptions { Limit = 5 });
                    if (versions?.Results == null)
                        continue;
                    foreach (var version in versions.Results)
                    {
                        CompetitionRewardResult reward = await CloudCodeService.Instance
                            .CallModuleEndpointAsync<CompetitionRewardResult>(
                                CloudCodeModule,
                                "ClaimLeaderboardReward",
                                new Dictionary<string, object>
                                {
                                    { "leaderboardId", boardId },
                                    { "leaderboardVersionId", version.Id }
                                });
                        if (reward == null || !reward.accepted ||
                            string.IsNullOrWhiteSpace(reward.transactionId))
                            continue;
                        EconomyTransactionResult grant = EconomyService.GrantReward(
                            RewardBundle.FromQuestRewards(reward.coins, 0L, reward.diamonds),
                            EconomySource.Event,
                            reward.transactionId);
                        if (grant.Status == EconomyTransactionStatus.Success)
                        {
                            periods++;
                            coins += Math.Max(0, reward.coins);
                            diamonds += Math.Max(0, reward.diamonds);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.Log("Cat Home period rewards will retry later: " + exception.Message);
                }
            }
        }
        return new CompetitionRewardSummary(periods, coins, diamonds);
    }

    private static async Task SubmitAsync(string endpoint, Dictionary<string, object> payload)
    {
        try
        {
            CompetitionSubmitResult result = await CloudCodeService.Instance
                .CallModuleEndpointAsync<CompetitionSubmitResult>(
                CloudCodeModule, endpoint, payload);
            LastMessage = result?.message ?? "SCORE COULD NOT BE SUBMITTED";
            Changed?.Invoke();
            if (result == null || !result.accepted)
                Debug.Log("Cat Home score was rejected: " + LastMessage);
        }
        catch (Exception exception)
        {
            // Online competition is additive. Local rewards/results remain final.
            LastMessage = "SCORE WILL RETRY NEXT RUN";
            Changed?.Invoke();
            Debug.Log("Cat Home score will not be uploaded this session: " + exception.Message);
        }
    }

    private static async Task EnsureOnlineSessionAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private static CompetitionSnapshot FromPage(
        string boardId,
        LeaderboardScoresPage page,
        string currentId)
    {
        var snapshot = new CompetitionSnapshot
        {
            leaderboardId = boardId,
            refreshedUtc = DateTime.UtcNow.ToString("O"),
            totalPlayers = Mathf.Max(0, page?.Total ?? 0),
            isOfflineCopy = false
        };
        if (page?.Results != null)
            foreach (LeaderboardEntry entry in page.Results)
                snapshot.entries.Add(FromEntry(entry, currentId));
        return snapshot;
    }

    private static CompetitionEntry FromEntry(LeaderboardEntry entry, string currentId)
    {
        if (entry == null)
            return null;
        string safeName = "COZY PLAYER";
        if (!string.IsNullOrWhiteSpace(entry.Metadata))
        {
            try
            {
                CompetitionScoreMetadata metadata =
                    JsonUtility.FromJson<CompetitionScoreMetadata>(entry.Metadata);
                if (metadata != null &&
                    CompetitionRules.TrySanitizeNickname(metadata.nickname, out string sanitized))
                    safeName = sanitized;
            }
            catch (Exception)
            {
                safeName = "COZY PLAYER";
            }
        }
        return new CompetitionEntry
        {
            rank = entry.Rank + 1,
            playerId = entry.PlayerId,
            nickname = safeName,
            score = (long)Math.Round(entry.Score, MidpointRounding.AwayFromZero),
            isCurrentPlayer = string.Equals(entry.PlayerId, currentId, StringComparison.Ordinal)
        };
    }

    private static string CacheKey(string boardId) => CachePrefix + boardId;

    private static void SaveCache(CompetitionSnapshot snapshot)
    {
        PlayerPrefs.SetString(CacheKey(snapshot.leaderboardId), JsonUtility.ToJson(snapshot));
        PlayerPrefs.Save();
    }

    private static CompetitionSnapshot LoadCache(string boardId)
    {
        string json = PlayerPrefs.GetString(CacheKey(boardId), string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            CompetitionSnapshot snapshot = JsonUtility.FromJson<CompetitionSnapshot>(json);
            if (snapshot == null)
                return null;
            snapshot.isOfflineCopy = true;
            snapshot.entries ??= new List<CompetitionEntry>();
            return snapshot;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static CompetitionSnapshot Empty(string boardId, bool offline) =>
        new CompetitionSnapshot
        {
            leaderboardId = boardId,
            refreshedUtc = string.Empty,
            isOfflineCopy = offline,
            totalPlayers = 0,
            entries = new List<CompetitionEntry>()
        };

    private static CompetitionSubmitResult Rejected(string message)
    {
        LastMessage = message ?? string.Empty;
        return new CompetitionSubmitResult { accepted = false, message = LastMessage };
    }

    private static void SetBusy(bool value, string message)
    {
        IsBusy = value;
        LastMessage = message ?? string.Empty;
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        IsBusy = false;
        LastMessage = string.Empty;
        Changed = null;
    }

    [Serializable]
    private sealed class CompetitionScoreMetadata
    {
        public string nickname;
    }
}
