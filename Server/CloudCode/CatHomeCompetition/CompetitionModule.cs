using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;
using Unity.Services.Leaderboards.Model;

namespace CatHomeCompetition;

public sealed class CompetitionModule
{
    private static readonly Regex AllowedNickname =
        new("^[\\p{L}\\p{N}_-](?:[\\p{L}\\p{N}_ -]{1,14})[\\p{L}\\p{N}_-]$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [CloudCodeFunction("SetNickname")]
    public SubmitResult SetNickname(IExecutionContext context, string nickname)
    {
        RequirePlayer(context);
        string safe = SanitizeNickname(nickname);
        return new SubmitResult(true, "NAME SAVED", 0, safe);
    }

    [CloudCodeFunction("SubmitRunnerScore")]
    public async Task<SubmitResult> SubmitRunnerScore(
        IExecutionContext context,
        IGameApiClient api,
        string runId,
        double durationSeconds,
        double distance,
        int coinsCollected,
        int collisions,
        int comboBonus,
        int claimedScore,
        string nickname)
    {
        RequirePlayer(context);
        RequireAttemptId(runId);
        if (durationSeconds < 1 || durationSeconds > 60 * 60 ||
            distance < 0 || distance > durationSeconds * 80 ||
            coinsCollected < 0 || coinsCollected > durationSeconds * 8 + 10 ||
            collisions < 0 || collisions > 3 ||
            comboBonus < 0 || comboBonus > coinsCollected * 20)
            throw new ArgumentException("Runner result is outside the accepted gameplay envelope.");

        int canonical = Math.Max(0, (int)Math.Round(distance, MidpointRounding.AwayFromZero)) +
                        Math.Max(0, coinsCollected) * 10 + comboBonus;
        if (canonical != claimedScore)
            throw new ArgumentException("Runner score does not match its gameplay metrics.");

        string safeName = SanitizeNickname(nickname);
        if (!await BeginAttempt(context, api, "runner", runId))
            return new SubmitResult(false, "RUN ALREADY SUBMITTED", canonical, safeName);
        await WriteAllPeriods(context, api, "cat-runner", canonical, safeName);
        return new SubmitResult(true, "SCORE SUBMITTED", canonical, safeName);
    }

    [CloudCodeFunction("SubmitCatchScore")]
    public async Task<SubmitResult> SubmitCatchScore(
        IExecutionContext context,
        IGameApiClient api,
        string huntId,
        double durationSeconds,
        int catches,
        int comboStepsTotal,
        int strikesResolved,
        int claimedScore,
        string nickname)
    {
        RequirePlayer(context);
        RequireAttemptId(huntId);
        if (durationSeconds < 50 || durationSeconds > 65 || catches < 0 || catches > 120 ||
            strikesResolved < catches || strikesResolved > 180 ||
            comboStepsTotal < 0 || comboStepsTotal > catches * 5)
            throw new ArgumentException("Catch result is outside the accepted gameplay envelope.");

        int canonical = catches * 120 + comboStepsTotal * 20;
        if (canonical != claimedScore)
            throw new ArgumentException("Catch score does not match its gameplay metrics.");

        string safeName = SanitizeNickname(nickname);
        if (!await BeginAttempt(context, api, "catch", huntId))
            return new SubmitResult(false, "HUNT ALREADY SUBMITTED", canonical, safeName);
        await WriteAllPeriods(context, api, "cat-catch", canonical, safeName);
        return new SubmitResult(true, "SCORE SUBMITTED", canonical, safeName);
    }

    [CloudCodeFunction("ClaimLeaderboardReward")]
    public async Task<RewardResult> ClaimLeaderboardReward(
        IExecutionContext context,
        IGameApiClient api,
        string leaderboardId,
        string leaderboardVersionId)
    {
        RequirePlayer(context);
        bool daily = leaderboardId.EndsWith("-daily", StringComparison.Ordinal);
        bool weekly = leaderboardId.EndsWith("-weekly", StringComparison.Ordinal);
        bool knownGame = leaderboardId.StartsWith("cat-runner-", StringComparison.Ordinal) ||
                         leaderboardId.StartsWith("cat-catch-", StringComparison.Ordinal);
        if (!knownGame || (!daily && !weekly) || string.IsNullOrWhiteSpace(leaderboardVersionId))
            throw new ArgumentException("Unknown reward leaderboard.");

        Guid projectId = Guid.Parse(context.ProjectId!);
        var ownResponse = await api.Leaderboards.GetLeaderboardVersionPlayerScoreAsync(
            context, context.ServiceToken!, projectId, leaderboardId,
            leaderboardVersionId, context.PlayerId!, true);
        var totalResponse = await api.Leaderboards.GetLeaderboardVersionScoresAsync(
            context, context.ServiceToken!, projectId, leaderboardId,
            leaderboardVersionId, false, 0, 1);
        int rank = ownResponse.Data.Rank;
        int total = Math.Max(1, totalResponse.Data.Total);
        Reward reward = RewardFor(rank, total, weekly);
        string transactionId =
            $"leaderboard:{leaderboardId}:{leaderboardVersionId}:{context.PlayerId}";
        string claimKey = $"competition-claim-{leaderboardId}-{leaderboardVersionId}";
        var existing = await api.CloudSaveData.GetItemsAsync(
            context, context.ServiceToken!, context.ProjectId!, context.PlayerId!,
            new List<string> { claimKey });
        bool alreadyClaimed = existing.Data.Results.Count != 0;
        if (!alreadyClaimed)
        {
            await api.CloudSaveData.SetItemAsync(
                context, context.ServiceToken!, context.ProjectId!, context.PlayerId!,
                new SetItemBody(claimKey, DateTime.UtcNow.ToString("O")));
        }
        return new RewardResult(
            true,
            alreadyClaimed,
            alreadyClaimed ? "REWARD ALREADY CLAIMED" : "REWARD READY",
            transactionId,
            reward.Coins,
            reward.Diamonds,
            reward.Badge);
    }

    private static async Task WriteAllPeriods(
        IExecutionContext context,
        IGameApiClient api,
        string gameId,
        int score,
        string nickname)
    {
        Guid projectId = Guid.Parse(context.ProjectId!);
        var metadata = new Dictionary<string, object> { ["nickname"] = nickname };
        foreach (string period in new[] { "daily", "weekly", "all-time" })
        {
            await api.Leaderboards.AddLeaderboardPlayerScoreAsync(
                context,
                context.ServiceToken!,
                projectId,
                $"{gameId}-{period}",
                context.PlayerId!,
                new AddLeaderboardScore(score, metadata));
        }
    }

    private static async Task<bool> BeginAttempt(
        IExecutionContext context,
        IGameApiClient api,
        string game,
        string attemptId)
    {
        string key = $"competition-txn-{game}-{attemptId}";
        var found = await api.CloudSaveData.GetItemsAsync(
            context, context.ServiceToken!, context.ProjectId!, context.PlayerId!,
            new List<string> { key });
        if (found.Data.Results.Count != 0)
            return false;
        await api.CloudSaveData.SetItemAsync(
            context, context.ServiceToken!, context.ProjectId!, context.PlayerId!,
            new SetItemBody(key, DateTime.UtcNow.ToString("O")));
        return true;
    }

    private static Reward RewardFor(int zeroBasedRank, int totalPlayers, bool weekly)
    {
        if (weekly)
        {
            if (zeroBasedRank == 0) return new Reward(1000, 10, "GOLD PAW");
            if (zeroBasedRank <= 2) return new Reward(700, 6, "SILVER PAW");
            if (zeroBasedRank <= 9) return new Reward(400, 3, "BRONZE PAW");
            if (zeroBasedRank < Math.Max(1, (int)Math.Ceiling(totalPlayers * .10)))
                return new Reward(250, 0, string.Empty);
            return new Reward(100, 0, string.Empty);
        }
        if (zeroBasedRank == 0) return new Reward(200, 0, string.Empty);
        if (zeroBasedRank <= 2) return new Reward(150, 0, string.Empty);
        if (zeroBasedRank <= 9) return new Reward(100, 0, string.Empty);
        if (zeroBasedRank < Math.Max(1, (int)Math.Ceiling(totalPlayers * .10)))
            return new Reward(60, 0, string.Empty);
        return new Reward(20, 0, string.Empty);
    }

    private static string SanitizeNickname(string? nickname)
    {
        string safe = Regex.Replace((nickname ?? string.Empty).Trim(), "\\s+", " ");
        if (safe.Length is < 3 or > 16 || !AllowedNickname.IsMatch(safe))
            throw new ArgumentException("Nickname must contain 3-16 letters or numbers.");
        return safe;
    }

    private static void RequirePlayer(IExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.PlayerId))
            throw new InvalidOperationException("An authenticated player is required.");
    }

    private static void RequireAttemptId(string id)
    {
        if (id.Length != 32 || !id.All(Uri.IsHexDigit))
            throw new ArgumentException("Invalid attempt identifier.");
    }
}

public sealed record SubmitResult(bool Accepted, string Message, long Score, string Nickname);
public sealed record Reward(long Coins, long Diamonds, string Badge);
public sealed record RewardResult(
    bool Accepted,
    bool AlreadyClaimed,
    string Message,
    string TransactionId,
    long Coins,
    long Diamonds,
    string Badge);

public sealed class ModuleConfig : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.AddGameApiClient();
    }
}
