using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class CatHomeLegalLinks
{
    public const string PlayerCare = "https://cathome-player-care.hhknshnn.chatgpt.site";
    public const string Privacy = PlayerCare + "/privacy";
    public const string DeleteAccount = PlayerCare + "/delete-account";
    public const string DataRequests = PlayerCare + "/data";
    public const string UnityPlayerAccount = "https://player-account.unity.com/";
}

[DisallowMultipleComponent]
public sealed class PrivacyDataPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform panelVisual;
    [SerializeField] private Button scrimButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button privacyButton;
    [SerializeField] private Button deleteInfoButton;
    [SerializeField] private Button dataRequestButton;
    [SerializeField] private Button syncButton;
    [SerializeField] private Button deleteCloudAccountButton;
    [SerializeField] private TMP_Text deleteCloudAccountLabel;
    [SerializeField] private TMP_Text statusText;

    private static PrivacyDataPanel activeInstance;
    private CatMovement catMovement;
    private bool inputBlockHeld;
    [SerializeField] private GameObject confirmationRoot;
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private Button cancelDeleteButton;
    private bool busy;

    public static bool IsAnyOpen => activeInstance != null &&
                                    activeInstance.rootGroup != null &&
                                    activeInstance.rootGroup.blocksRaycasts;

    public static void Open()
    {
        if (activeInstance != null)
            activeInstance.RequestOpen();
    }

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            gameObject.SetActive(false);
            return;
        }
        activeInstance = this;
        StorybookSettingsPresentation.ApplyPrivacy(transform);
        ApplyClosed();
        Bind();
    }

    private void OnEnable()
    {
        Bind();
        Refresh();
    }

    public void RequestOpen()
    {
        SetConfirmation(false);
        AcquireInputBlock();
        SetOpen(true);
        Refresh();
    }

    public void RequestClose()
    {
        if (busy)
            return;
        if (confirmationRoot != null && confirmationRoot.activeSelf) { SetConfirmation(false); return; }
        SetOpen(false);
        ReleaseInputBlock();
    }

    private void Bind()
    {
        Bind(scrimButton, RequestClose);
        Bind(closeButton, RequestClose);
        Bind(privacyButton, () => Application.OpenURL(CatHomeLegalLinks.Privacy));
        Bind(deleteInfoButton, () => Application.OpenURL(CatHomeLegalLinks.DeleteAccount));
        Bind(dataRequestButton, () => Application.OpenURL(CatHomeLegalLinks.DataRequests));
        Bind(syncButton, SyncNow);
        Bind(deleteCloudAccountButton, RequestDeleteCloudAccount);
        Bind(confirmDeleteButton, DeleteCloudAccount);
        Bind(cancelDeleteButton, () => { if (!busy) SetConfirmation(false); });
        CloudSaveSyncService.Changed -= Refresh;
        CloudSaveSyncService.Changed += Refresh;
        GameLanguageService.Changed -= Refresh;
        GameLanguageService.Changed += Refresh;
        AccountIdentityService.Changed -= Refresh;
        AccountIdentityService.Changed += Refresh;
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private async void SyncNow()
    {
        if (busy || !AccountIdentityService.IsGoogleConnected)
            return;
        busy = true;
        CatHomeSaveSystem.SaveNow(true);
        Refresh();
        await CloudSaveSyncService.SyncNowAsync();
        busy = false;
        if (this != null)
            Refresh();
    }

    private void RequestDeleteCloudAccount()
    {
        if (!busy && AccountIdentityService.IsGoogleConnected) SetConfirmation(true);
    }
    private void SetConfirmation(bool value)
    {
        if (confirmationRoot != null) confirmationRoot.SetActive(value);
        if (panelVisual != null) panelVisual.gameObject.SetActive(!value);
    }

    private async void DeleteCloudAccount()
    {
        if (busy || !AccountIdentityService.IsGoogleConnected || confirmationRoot == null || !confirmationRoot.activeSelf)
            return;
        busy = true;
        SetConfirmation(false);
        Refresh();
        await CloudSaveSyncService.DeleteCloudAccountAndDataAsync();
        busy = false;
        if (this != null)
            Refresh();
    }

    private void Refresh()
    {

        bool connected = AccountIdentityService.IsGoogleConnected;
        if (syncButton != null)
            syncButton.interactable = connected && !busy;
        if (deleteCloudAccountButton != null)
            deleteCloudAccountButton.interactable = connected && !busy;
        if (deleteCloudAccountLabel != null)
            deleteCloudAccountLabel.text = GameLanguageService.Text(
                "privacy.delete_cloud");
        if (statusText != null)
        {
            string key = connected
                ? CloudSaveSyncService.LastMessageKey
                : "privacy.guest_status";
            if (connected && (string.IsNullOrEmpty(key) || key == "cloud.status.offline"))
                key = "cloud.status.ready";
            statusText.text = GameLanguageService.Text(key);
        }
    }

    private void SetOpen(bool open)
    {
        if (rootGroup == null)
            return;
        rootGroup.alpha = open ? 1f : 0f;
        rootGroup.interactable = open;
        rootGroup.blocksRaycasts = open;
        if (panelVisual != null)
            panelVisual.localScale = Vector3.one;
    }

    private void ApplyClosed() => SetOpen(false);

    private void AcquireInputBlock()
    {
        if (inputBlockHeld)
            return;
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        if (catMovement == null)
            return;
        catMovement.AcquireInputBlock(this);
        inputBlockHeld = true;
    }

    private void ReleaseInputBlock()
    {
        if (!inputBlockHeld)
            return;
        if (catMovement != null)
            catMovement.ReleaseInputBlock(this);
        inputBlockHeld = false;
    }

    private void OnDisable() => ReleaseInputBlock();

    private void OnDestroy()
    {
        CloudSaveSyncService.Changed -= Refresh;
        GameLanguageService.Changed -= Refresh;
        AccountIdentityService.Changed -= Refresh;
        if (activeInstance == this)
            activeInstance = null;
        ReleaseInputBlock();
    }
}
