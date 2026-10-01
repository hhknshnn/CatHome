using System.Collections;
using CatHome.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Launch title / main menu overlay. Covers home on first CatHome_UI load, then
/// fades on PLAY/CONTINUE. No extra gameplay camera, listener or EventSystem.
/// </summary>
[DisallowMultipleComponent]
public sealed class TitleScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button roomsButton;
    [SerializeField] private Button gamesButton;
    [SerializeField] private GameObject shopAfterTourBadge;
    [SerializeField] private GameObject roomsAfterTourBadge;
    [SerializeField] private GameObject gamesAfterTourBadge;
    [SerializeField] private Button creditsCloseButton;
    [SerializeField] private Button newGameCancelButton;
    [SerializeField] private Button newGameConfirmButton;
    [SerializeField] private Button accountGoogleButton;
    [SerializeField] private Button accountGuestButton;
    [SerializeField] private Button accountBackButton;
    [SerializeField] private TMP_Text greetingText;
    [SerializeField] private TMP_Text catNameText;
    [SerializeField] private TMP_Text playLabel;
    [SerializeField] private TMP_Text homeLevelText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text collectionText;
    [SerializeField] private CanvasGroup creditsGroup;
    [SerializeField] private CanvasGroup newGameGroup;
    [SerializeField] private TMP_Text newGameStatusText;
    [SerializeField] private CanvasGroup accountChoiceGroup;
    [SerializeField] private TMP_Text accountStatusText;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.35f;

    private static TitleScreen activeInstance;
    private SettingsPanel settingsPanel;
    private Coroutine fadeRoutine;
    private bool dismissed;
    private bool creditsOpen;
    private bool newGameOpen;
    private bool accountChoiceOpen;
    private bool accountChoiceForNewGame;
    private LaunchAction pendingAction;

    private enum LaunchAction
    {
        None,
        Shop,
        Rooms,
        Games
    }

    public static bool IsShowing =>
        activeInstance != null && !activeInstance.dismissed &&
        activeInstance.isActiveAndEnabled;

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            gameObject.SetActive(false);
            return;
        }
        activeInstance = this;
        if (GetComponent<TitleMusicController>() == null)
            gameObject.AddComponent<TitleMusicController>();
        BindListeners();
        StorybookTitleSubwindowPresentation.Apply(transform);
    }

    private void OnEnable()
    {
        BindListeners();
        RefreshContent();
        Show();
        HomeProgressionService.Changed += RefreshContent;
        EconomyService.BalanceChanged += HandleBalance;
        HomeStoreService.OwnershipChanged += HandleOwned;
        CatIdentityService.Changed += RefreshContent;
        GameLanguageService.Changed += RefreshContent;
    }

    private void LateUpdate()
    {
        bool available=!creditsOpen && !newGameOpen && !accountChoiceOpen &&
            !SettingsPanel.IsAnyOpen && !PrivacyDataPanel.IsAnyOpen && fadeRoutine==null;
        foreach(var button in new[]{playButton,settingsButton,creditsButton,quitButton,newGameButton})
            if(button!=null)button.interactable=available;
        bool shortcuts=available&&PetTutorialHint.IsOnboardingCompleted;
        foreach(var button in new[]{shopButton,roomsButton,gamesButton})if(button!=null)button.interactable=shortcuts;
    }

    private void OnDisable()
    {
        HomeProgressionService.Changed -= RefreshContent;
        EconomyService.BalanceChanged -= HandleBalance;
        HomeStoreService.OwnershipChanged -= HandleOwned;
        CatIdentityService.Changed -= RefreshContent;
        GameLanguageService.Changed -= RefreshContent;
    }

    private void BindListeners()
    {
        Bind(playButton, OnPlay);
        Bind(settingsButton, OnSettings);
        Bind(creditsButton, OnCredits);
        Bind(quitButton, OnQuit);
        Bind(newGameButton, OnNewGame);
        Bind(shopButton, OnShop);
        Bind(roomsButton, OnRooms);
        Bind(gamesButton, OnGames);
        Bind(creditsCloseButton, OnCreditsClose);
        Bind(newGameCancelButton, OnNewGameCancel);
        Bind(newGameConfirmButton, OnNewGameConfirm);
        Bind(accountGoogleButton, OnAccountGoogle);
        Bind(accountGuestButton, OnAccountGuest);
        Bind(accountBackButton, OnAccountBack);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void RefreshContent()
    {
        if (greetingText != null)
        {
            string name = CatIdentityService.CatName;
            greetingText.text = string.IsNullOrEmpty(name)
                ? GameLanguageService.Text("title.greeting.empty")
                : GameLanguageService.Format(
                    "title.greeting.named", CatIdentityService.DisplayName);
        }

        if (catNameText != null)
        {
            catNameText.text = string.IsNullOrEmpty(CatIdentityService.CatName)
                ? GameLanguageService.Text("title.home.empty")
                : GameLanguageService.Format(
                    "title.home.named", CatIdentityService.DisplayName);
        }

        if (playLabel != null)
            playLabel.text = GameLanguageService.Text(
                PetTutorialHint.IsOnboardingCompleted ? "title.continue" : "title.play");

        if (homeLevelText != null)
            homeLevelText.text = GameLanguageService.Format(
                "title.home_level", HomeProgressionService.HomeLevel);
        if (coinsText != null)
            coinsText.text = EconomyService.Coins.ToString("N0");
        if (collectionText != null)
            collectionText.text = GameLanguageService.Format(
                "title.collection",
                CollectionMilestoneService.OwnedCount,
                CollectionMilestoneService.CatalogSize);

        bool shortcutsUnlocked = PetTutorialHint.IsOnboardingCompleted;
        SetShortcutAvailability(shopButton, shopAfterTourBadge, shortcutsUnlocked);
        SetShortcutAvailability(roomsButton, roomsAfterTourBadge, shortcutsUnlocked);
        SetShortcutAvailability(gamesButton, gamesAfterTourBadge, shortcutsUnlocked);

        bool hasJourney = HasJourney();
        if (newGameButton != null)
            newGameButton.gameObject.SetActive(hasJourney);

        if (quitButton != null)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            quitButton.gameObject.SetActive(true);
#else
            quitButton.gameObject.SetActive(false);
#endif
        }

        SetCreditsVisible(creditsOpen);
        SetNewGameVisible(newGameOpen);
        SetAccountChoiceVisible(accountChoiceOpen);
    }

    private static void SetUtilityButtonX(Button button, float x)
    {
        if (button == null)
            return;
        RectTransform rect = button.transform as RectTransform;
        if (rect != null)
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
    }

    private static void SetShortcutAvailability(
        Button button, GameObject afterTourBadge, bool unlocked)
    {
        if (button != null)
            button.interactable = unlocked;
        if (afterTourBadge != null)
            afterTourBadge.SetActive(!unlocked);
    }

    private void HandleBalance(CurrencyBalanceChange change) => RefreshContent();
    private void HandleOwned(string productId) => RefreshContent();

    private void Show()
    {
        dismissed = false;
        creditsOpen = false;
        newGameOpen = false;
        accountChoiceOpen = false;
        accountChoiceForNewGame = false;
        if (rootGroup != null)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }
        SetCreditsVisible(false);
        SetNewGameVisible(false);
        SetAccountChoiceVisible(false);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Reopens the existing title overlay from the in-game drop-down without
    /// reloading scenes, duplicating cameras or changing the current journey.
    /// </summary>
    public void RequestShow()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        bool wasActive = gameObject.activeSelf;
        gameObject.SetActive(true);
        if (!wasActive)
            return; // OnEnable refreshes and calls Show().

        RefreshContent();
        Show();
    }

    private void OnPlay()
    {
        if (!HasJourney() && !AccountIdentityService.HasChosenAccount)
        {
            ShowAccountChoice(false);
            return;
        }
        BeginDismiss(LaunchAction.None);
    }

    private void OnShop()
    {
        if (!PetTutorialHint.IsOnboardingCompleted)
        {
            OnPlay();
            return;
        }
        BeginDismiss(LaunchAction.Shop);
    }

    private void OnRooms()
    {
        if (!PetTutorialHint.IsOnboardingCompleted)
        {
            OnPlay();
            return;
        }
        BeginDismiss(LaunchAction.Rooms);
    }

    private void OnGames()
    {
        if (!PetTutorialHint.IsOnboardingCompleted)
        {
            OnPlay();
            return;
        }
        BeginDismiss(LaunchAction.Games);
    }

    private void BeginDismiss(LaunchAction action)
    {
        if (dismissed)
            return;
        dismissed = true;
        if (!PetTutorialHint.IsOnboardingCompleted) GameAudio.UI(AudioCue.NewGame);
        pendingAction = action;
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        if (isActiveAndEnabled)
            fadeRoutine = StartCoroutine(FadeOut());
        else
            HideImmediate();
    }

    private void OnSettings()
    {
        if (settingsPanel == null)
            settingsPanel = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (settingsPanel != null)
            settingsPanel.RequestOpen();
    }

    private void OnCredits()
    {
        newGameOpen = false;
        SetNewGameVisible(false);
        accountChoiceOpen = false;
        SetAccountChoiceVisible(false);
        creditsOpen = true;
        SetCreditsVisible(true);
    }

    private void OnCreditsClose()
    {
        creditsOpen = false;
        SetCreditsVisible(false);
    }

    private void OnNewGame()
    {
        creditsOpen = false;
        SetCreditsVisible(false);
        accountChoiceOpen = false;
        SetAccountChoiceVisible(false);
        newGameOpen = true;
        if (newGameStatusText != null)
            newGameStatusText.text = string.Empty;
        SetNewGameVisible(true);
    }

    private void OnNewGameCancel()
    {
        newGameOpen = false;
        SetNewGameVisible(false);
    }

    private void OnNewGameConfirm()
    {
        newGameOpen = false;
        SetNewGameVisible(false);
        ShowAccountChoice(true);
    }

    private async void OnAccountGoogle()
    {
        SetAccountButtonsInteractable(false);
        if (accountStatusText != null)
            accountStatusText.text = GameLanguageService.Text("account.status.connecting");

        AccountSignInResult result = await AccountIdentityService.ConnectWithGoogleAsync();
        if (this == null || !accountChoiceOpen)
            return;
        SetAccountButtonsInteractable(true);
        if (!result.Succeeded)
        {
            if (accountStatusText != null)
                accountStatusText.text = GameLanguageService.Text(result.MessageKey);
            return;
        }

        CompleteAccountChoice();
    }

    private void OnAccountGuest()
    {
        AccountIdentityService.ContinueAsGuest();
        CompleteAccountChoice();
    }

    private void OnAccountBack()
    {
        bool returnToNewGame = accountChoiceForNewGame;
        accountChoiceOpen = false;
        accountChoiceForNewGame = false;
        SetAccountChoiceVisible(false);
        if (!returnToNewGame)
            return;
        newGameOpen = true;
        SetNewGameVisible(true);
    }

    private void ShowAccountChoice(bool forNewGame)
    {
        creditsOpen = false;
        newGameOpen = false;
        SetCreditsVisible(false);
        SetNewGameVisible(false);
        accountChoiceForNewGame = forNewGame;
        accountChoiceOpen = true;
        if (accountStatusText != null)
            accountStatusText.text = string.Empty;
        SetAccountButtonsInteractable(true);
        SetAccountChoiceVisible(true);
    }

    private void CompleteAccountChoice()
    {
        if (accountChoiceForNewGame)
        {
            bool succeeded = CatHomeSaveSystem.TryStartNewGame(out string report);
            if (!succeeded)
            {
                Debug.LogWarning(report);
                if (accountStatusText != null)
                    accountStatusText.text = GameLanguageService.Text("new_game.failed");
                return;
            }
            Debug.Log(report);
        }

        accountChoiceOpen = false;
        accountChoiceForNewGame = false;
        SetAccountChoiceVisible(false);
        RefreshContent();
        BeginDismiss(LaunchAction.None);
    }

    private void SetAccountButtonsInteractable(bool interactable)
    {
        if (accountGoogleButton != null)
            accountGoogleButton.interactable = interactable;
        if (accountGuestButton != null)
            accountGuestButton.interactable = interactable;
        if (accountBackButton != null)
            accountBackButton.interactable = true;
    }

    private static bool HasJourney() => PetTutorialHint.IsOnboardingCompleted ||
                                        !string.IsNullOrEmpty(CatIdentityService.CatName);

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
        Application.Quit();
#endif
    }

    private void SetCreditsVisible(bool visible)
    {
        if (creditsGroup == null)
            return;
        creditsGroup.alpha = visible ? 1f : 0f;
        creditsGroup.interactable = visible;
        creditsGroup.blocksRaycasts = visible;
        creditsGroup.gameObject.SetActive(true);
    }

    private void SetNewGameVisible(bool visible)
    {
        if (newGameGroup == null)
            return;
        newGameGroup.alpha = visible ? 1f : 0f;
        newGameGroup.interactable = visible;
        newGameGroup.blocksRaycasts = visible;
        newGameGroup.gameObject.SetActive(true);
    }

    private void SetAccountChoiceVisible(bool visible)
    {
        if (accountChoiceGroup == null)
            return;
        accountChoiceGroup.alpha = visible ? 1f : 0f;
        accountChoiceGroup.interactable = visible;
        accountChoiceGroup.blocksRaycasts = visible;
        accountChoiceGroup.gameObject.SetActive(true);
    }

    private IEnumerator FadeOut()
    {
        if (rootGroup != null)
            rootGroup.interactable = false;
        float elapsed = 0f;
        float start = rootGroup != null ? rootGroup.alpha : 1f;
        float duration = CatRunnerProgressService.ReducedMotion ? 0.05f : fadeDuration;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (rootGroup != null)
                rootGroup.alpha = Mathf.Lerp(start, 0f, t);
            yield return null;
        }
        LaunchAction action = pendingAction;
        HideImmediate();
        OpenRequestedSurface(action);
    }

    private static void OpenRequestedSurface(LaunchAction action)
    {
        switch (action)
        {
            case LaunchAction.Shop:
                CatBreedShopPanel cats =
                    FindAnyObjectByType<CatBreedShopPanel>(FindObjectsInactive.Include);
                if (cats != null)
                    cats.RequestOpen();
                break;
            case LaunchAction.Rooms:
                RoomSelectorPanel rooms =
                    FindAnyObjectByType<RoomSelectorPanel>(FindObjectsInactive.Include);
                if (rooms != null)
                    rooms.RequestOpen();
                break;
            case LaunchAction.Games:
                GamesHubPanel games =
                    FindAnyObjectByType<GamesHubPanel>(FindObjectsInactive.Include);
                if (games != null)
                    games.Show();
                break;
        }
    }

    private void HideImmediate()
    {
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        fadeRoutine = null;
        pendingAction = LaunchAction.None;
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
    }
}
