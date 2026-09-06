using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum CompetitionGame
{
    CatRunner = 0,
    CatCatch = 1
}

public enum CompetitionPeriod
{
    Daily = 0,
    Weekly = 1,
    AllTime = 2
}

[Serializable]
public sealed class CompetitionEntry
{
    public int rank;
    public string playerId;
    public string nickname;
    public long score;
    public bool isCurrentPlayer;
}

[Serializable]
public sealed class CompetitionSnapshot
{
    public string leaderboardId;
    public string refreshedUtc;
    public bool isOfflineCopy;
    public int totalPlayers;
    public List<CompetitionEntry> entries = new List<CompetitionEntry>();
    public CompetitionEntry currentPlayer;
}

[Serializable]
public sealed class CompetitionSubmitResult
{
    public bool accepted;
    public string message;
    public long score;
    public string nickname;
}

[Serializable]
public sealed class CompetitionRewardResult
{
    public bool accepted;
    public bool alreadyClaimed;
    public string message;
    public string transactionId;
    public long coins;
    public long diamonds;
    public string badge;
}

public readonly struct CompetitionRewardSummary
{
    public CompetitionRewardSummary(int periods, long coins, long diamonds)
    {
        Periods = periods;
        Coins = coins;
        Diamonds = diamonds;
    }

    public int Periods { get; }
    public long Coins { get; }
    public long Diamonds { get; }
    public bool HasRewards => Periods > 0;
}

/// <summary>
/// Stable public contract shared by the game, tests and the Cloud Code module.
/// IDs are deliberately independent from localized UI copy.
/// </summary>
public static class CompetitionRules
{
    public const int MaximumVisibleEntries = 50;
    public const int MinimumNicknameLength = 3;
    public const int MaximumNicknameLength = 16;

    public static string BoardId(CompetitionGame game, CompetitionPeriod period)
    {
        string gameId = game == CompetitionGame.CatCatch ? "cat-catch" : "cat-runner";
        string periodId = period == CompetitionPeriod.Daily
            ? "daily"
            : period == CompetitionPeriod.Weekly ? "weekly" : "all-time";
        return gameId + "-" + periodId;
    }

    public static bool TrySanitizeNickname(string source, out string nickname)
    {
        var result = new StringBuilder(MaximumNicknameLength);
        bool previousWasSpace = true;
        string trimmed = (source ?? string.Empty).Trim();
        for (int i = 0; i < trimmed.Length && result.Length < MaximumNicknameLength; i++)
        {
            char value = trimmed[i];
            bool allowed = char.IsLetterOrDigit(value) || value == '_' || value == '-';
            if (allowed)
            {
                result.Append(value);
                previousWasSpace = false;
            }
            else if (char.IsWhiteSpace(value) && !previousWasSpace && result.Length > 0)
            {
                result.Append(' ');
                previousWasSpace = true;
            }
        }

        nickname = result.ToString().Trim();
        return nickname.Length >= MinimumNicknameLength;
    }

    public static int RunnerCanonicalScore(float distance, int collectedCoins, int comboBonus)
    {
        return Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0f, distance))) +
               Mathf.Max(0, collectedCoins) * 10 +
               Mathf.Max(0, comboBonus);
    }

    public static int CatchCanonicalScore(int catches, int comboStepsTotal)
    {
        int safeCatches = Mathf.Max(0, catches);
        int safeSteps = Mathf.Clamp(comboStepsTotal, 0, safeCatches * 5);
        return safeCatches * CatchScoring.BaseCatchScore +
               safeSteps * CatchScoring.ComboStepScore;
    }
}
