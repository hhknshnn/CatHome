using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;

public enum CatHomeAccountKind
{
    None = 0,
    Guest = 1,
    Google = 2
}

public readonly struct AccountSignInResult
{
    public AccountSignInResult(bool succeeded, string messageKey)
    {
        Succeeded = succeeded;
        MessageKey = messageKey ?? string.Empty;
    }

    public bool Succeeded { get; }
    public string MessageKey { get; }
}

/// <summary>
/// Owns Cat Home's account choice without putting identity or tokens in the v11
/// gameplay save. Guest play always remains available locally. Unity Authentication
/// supplies the online player ID; Unity Player Accounts supplies the browser flow
/// that includes Google sign-in without exposing a password to Cat Home.
/// </summary>
public static class AccountIdentityService
{
    public const string AccountKindPlayerPrefsKey = "cat-home.account-kind";
    public const string LocalGuestIdPlayerPrefsKey = "cat-home.local-guest-id";
    private const string UnityIdentityProviderId = "unity";

    private static bool onlineRequestInFlight;
    private static string lastMessageKey = string.Empty;

    public static event Action Changed;

    public static CatHomeAccountKind Kind => (CatHomeAccountKind)Mathf.Clamp(
        PlayerPrefs.GetInt(AccountKindPlayerPrefsKey, (int)CatHomeAccountKind.None),
        (int)CatHomeAccountKind.None,
        (int)CatHomeAccountKind.Google);

    public static bool HasChosenAccount => Kind != CatHomeAccountKind.None;
    public static bool IsGoogleConnected => Kind == CatHomeAccountKind.Google;
    public static bool IsBusy => onlineRequestInFlight;
    public static string LastMessageKey => lastMessageKey;

    public static string EnsureLocalGuestIdentity()
    {
        string existing = PlayerPrefs.GetString(LocalGuestIdPlayerPrefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        string created = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(LocalGuestIdPlayerPrefsKey, created);
        PlayerPrefs.Save();
        return created;
    }

    /// <summary>
    /// Guest play never waits for a network request. The anonymous UGS session is
    /// upgraded in the background so leaderboards can be enabled when available.
    /// </summary>
    public static void ContinueAsGuest()
    {
        EnsureLocalGuestIdentity();
        SetKind(CatHomeAccountKind.Guest, "account.status.guest");
        _ = ConnectGuestOnlineAsync();
    }

    public static async Task<AccountSignInResult> ConnectWithGoogleAsync()
    {
        if (onlineRequestInFlight)
            return new AccountSignInResult(false, "account.status.connecting");

        onlineRequestInFlight = true;
        SetMessage("account.status.connecting");
        try
        {
            await EnsureServicesInitializedAsync();

            // A guest upgrading from Settings must first restore the cached
            // anonymous player so linking keeps the same UGS player ID.
            if (!AuthenticationService.Instance.IsSignedIn &&
                Kind == CatHomeAccountKind.Guest)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (AuthenticationService.Instance.IsSignedIn && HasUnityIdentity())
            {
                SetKind(CatHomeAccountKind.Google, "account.status.google");
                await CloudSaveSyncService.SyncNowAsync();
                return new AccountSignInResult(true, "account.google.success");
            }

            if (!PlayerAccountService.Instance.IsSignedIn &&
                !await StartPlayerAccountSignInAsync())
            {
                SetMessage("account.google.cancelled");
                return new AccountSignInResult(false, lastMessageKey);
            }

            if (!PlayerAccountService.Instance.IsSignedIn ||
                string.IsNullOrWhiteSpace(PlayerAccountService.Instance.AccessToken))
            {
                SetMessage("account.google.cancelled");
                return new AccountSignInResult(false, lastMessageKey);
            }

            if (AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.LinkWithUnityAsync(
                    PlayerAccountService.Instance.AccessToken);
            else
                await AuthenticationService.Instance.SignInWithUnityAsync(
                    PlayerAccountService.Instance.AccessToken);

            SetKind(CatHomeAccountKind.Google, "account.status.google");
            await CloudSaveSyncService.SyncNowAsync();
            return new AccountSignInResult(true, "account.google.success");
        }
        catch (AuthenticationException exception) when (
            exception.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // The Google identity already owns another UGS player. Move from the
            // temporary guest identity to that player, then let seamless Cloud Save
            // select the newest journey. The player never sees a save-choice screen.
            Debug.Log("Cat Home is continuing with the existing Google player: " +
                      exception.Message);
            AuthenticationService.Instance.SignOut(true);
            await AuthenticationService.Instance.SignInWithUnityAsync(
                PlayerAccountService.Instance.AccessToken);
            SetKind(CatHomeAccountKind.Google, "account.status.google");
            SetMessage("account.google.already_linked");
            await CloudSaveSyncService.SyncNowAsync();
            return new AccountSignInResult(true, lastMessageKey);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Cat Home Google sign-in is unavailable: " + exception.Message);
            SetMessage("account.google.unavailable");
            return new AccountSignInResult(false, lastMessageKey);
        }
        finally
        {
            onlineRequestInFlight = false;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// In the Editor/Standalone implementation StartSignInAsync waits for the
    /// localhost callback, while Android/iOS return as soon as the system browser
    /// opens. Waiting on Player Accounts events keeps the same code correct on all
    /// three paths and prevents a successful mobile redirect from being mistaken
    /// for an immediate cancellation.
    /// </summary>
    private static async Task<bool> StartPlayerAccountSignInAsync()
    {
        IPlayerAccountService playerAccounts = PlayerAccountService.Instance;
        if (playerAccounts.IsSignedIn)
            return true;

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        void HandleSignedIn() => completion.TrySetResult(true);
        void HandleFailed(RequestFailedException exception) =>
            completion.TrySetException(exception);

        playerAccounts.SignedIn += HandleSignedIn;
        playerAccounts.SignInFailed += HandleFailed;
        try
        {
            await playerAccounts.StartSignInAsync();
            if (playerAccounts.IsSignedIn)
                return true;

            Task finished = await Task.WhenAny(
                completion.Task, Task.Delay(TimeSpan.FromMinutes(2)));
            return finished == completion.Task && await completion.Task;
        }
        finally
        {
            playerAccounts.SignedIn -= HandleSignedIn;
            playerAccounts.SignInFailed -= HandleFailed;
        }
    }

    private static async Task ConnectGuestOnlineAsync()
    {
        if (onlineRequestInFlight)
            return;
        onlineRequestInFlight = true;
        Changed?.Invoke();
        try
        {
            await EnsureServicesInitializedAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // SignInAnonymouslyAsync restores a cached linked player when one
            // exists. Never downgrade that persistent identity to Guest.
            if (HasUnityIdentity())
                SetKind(CatHomeAccountKind.Google, "account.status.google");
            else
                SetMessage("account.status.guest");
        }
        catch (Exception exception)
        {
            // Local guest play is deliberately unaffected by service/config/network
            // availability. This is informational rather than a gameplay failure.
            Debug.Log("Cat Home continues as a local guest: " + exception.Message);
            SetMessage("account.status.guest_offline");
        }
        finally
        {
            onlineRequestInFlight = false;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Called only after Cloud Save data and the Unity Authentication profile
    /// have been deleted. The on-device journey remains available as a guest;
    /// account deletion is deliberately not a hidden NEW GAME.
    /// </summary>
    public static void ReturnToGuestAfterDeletion()
    {
        EnsureLocalGuestIdentity();
        SetKind(CatHomeAccountKind.Guest, "account.status.guest");
    }

    private static async Task EnsureServicesInitializedAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();
    }

    private static bool HasUnityIdentity()
    {
        PlayerInfo player = AuthenticationService.Instance.PlayerInfo;
        if (player?.Identities == null)
            return false;
        foreach (var identity in player.Identities)
            if (string.Equals(identity.TypeId, UnityIdentityProviderId,
                    StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static void SetKind(CatHomeAccountKind kind, string messageKey)
    {
        PlayerPrefs.SetInt(AccountKindPlayerPrefsKey, (int)kind);
        PlayerPrefs.Save();
        lastMessageKey = messageKey ?? string.Empty;
        Changed?.Invoke();
    }

    private static void SetMessage(string messageKey)
    {
        lastMessageKey = messageKey ?? string.Empty;
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        onlineRequestInFlight = false;
        lastMessageKey = string.Empty;
        Changed = null;
    }
}
