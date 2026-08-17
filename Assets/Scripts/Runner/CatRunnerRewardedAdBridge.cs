using System;
using UnityEngine;

/// <summary>
/// Adapter implemented by the platform ad package. Currency is never granted by
/// the provider itself; it only reports a verified completion to the launcher.
/// </summary>
public interface ICatRunnerRewardedAdProvider
{
    bool IsRewardedAdReady(string placementId);
    void ShowRewardedAd(string placementId, Action<bool> completed);
}

public static class CatRunnerRewardedAdBridge
{
    public const string EnergyPlacementId = "cat_runner_energy";
    public const string DoubleCoinsPlacementId = "cat_runner_double_coins";

    private static ICatRunnerRewardedAdProvider provider;
    private static bool requestInFlight;
    private static long activeRequestId;
    private static float requestStartedAt;

    public static bool HasReadyProvider => IsPlacementReady(EnergyPlacementId);

    public static bool IsPlacementReady(string placementId)
    {
        RecoverTimedOutRequest();
        if (provider == null || requestInFlight)
            return false;
        string safePlacement = string.IsNullOrWhiteSpace(placementId)
            ? EnergyPlacementId
            : placementId;
        try
        {
            return provider.IsRewardedAdReady(safePlacement);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }
    }

    public static void Register(ICatRunnerRewardedAdProvider value)
    {
        provider = value;
        requestInFlight = false;
        activeRequestId++;
    }

    public static void Unregister(ICatRunnerRewardedAdProvider value)
    {
        if (!ReferenceEquals(provider, value))
            return;
        provider = null;
        requestInFlight = false;
        activeRequestId++;
    }

    public static bool TryShow(Action<bool> completed)
    {
        return TryShow(EnergyPlacementId, completed);
    }

    public static bool TryShow(string placementId, Action<bool> completed)
    {
        RecoverTimedOutRequest();
        string safePlacement = string.IsNullOrWhiteSpace(placementId)
            ? EnergyPlacementId
            : placementId;
        if (provider == null || requestInFlight)
            return false;
        try
        {
            if (!provider.IsRewardedAdReady(safePlacement))
                return false;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }

        requestInFlight = true;
        requestStartedAt = Time.realtimeSinceStartup;
        long requestId = ++activeRequestId;
        bool callbackReceived = false;
        try
        {
            provider.ShowRewardedAd(
                safePlacement,
                verified =>
                {
                    if (callbackReceived || requestId != activeRequestId)
                        return;
                    callbackReceived = true;
                    requestInFlight = false;
                    completed?.Invoke(verified);
                });
            return true;
        }
        catch (Exception exception)
        {
            requestInFlight = false;
            activeRequestId++;
            Debug.LogException(exception);
            return false;
        }
    }

    private static void RecoverTimedOutRequest()
    {
        if (!requestInFlight ||
            Time.realtimeSinceStartup - requestStartedAt <= 120f)
        {
            return;
        }
        requestInFlight = false;
        activeRequestId++;
        Debug.LogWarning("Cat Runner rewarded-ad request timed out and was released.");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        provider = null;
        requestInFlight = false;
        activeRequestId = 0;
        requestStartedAt = 0f;
    }
}
