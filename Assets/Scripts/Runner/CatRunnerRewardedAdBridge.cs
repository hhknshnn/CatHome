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

    private static ICatRunnerRewardedAdProvider provider;
    private static bool requestInFlight;
    private static long activeRequestId;
    private static float requestStartedAt;

    public static bool HasReadyProvider
    {
        get
        {
            RecoverTimedOutRequest();
            if (provider == null || requestInFlight)
                return false;
            try
            {
                return provider.IsRewardedAdReady(EnergyPlacementId);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
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
        RecoverTimedOutRequest();
        if (!HasReadyProvider)
        {
            return false;
        }

        requestInFlight = true;
        requestStartedAt = Time.realtimeSinceStartup;
        long requestId = ++activeRequestId;
        bool callbackReceived = false;
        try
        {
            provider.ShowRewardedAd(
                EnergyPlacementId,
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
