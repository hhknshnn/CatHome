using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Premium competition overlay shared by Cat Runner and Cat Catch.
/// It displays cached rankings immediately and refreshes them online when possible.
/// </summary>
[DisallowMultipleComponent]
public sealed class LeaderboardPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button runnerButton;
    [SerializeField] private Button catchButton;
    [SerializeField] private Button dailyButton;
    [SerializeField] private Button weeklyButton;
    [SerializeField] private Button allTimeButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TMP_Text catNameText;
    [SerializeField] private TMP_Text connectionHintText;
    [SerializeField] private TMP_Text gameTitle;
    [SerializeField] private TMP_Text periodTitle;
    [SerializeField] private TMP_Text globalSummaryText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text ownRankText;
    [SerializeField] private TMP_Text[] podiumNameTexts;
    [SerializeField] private TMP_Text[] podiumScoreTexts;
    [SerializeField] private TMP_Text[] rankRows;
    [SerializeField] private TMP_Text emptyStateText;

    private CompetitionGame game = CompetitionGame.CatRunner;
    private CompetitionPeriod period = CompetitionPeriod.Daily;
    private int requestGeneration;
    private bool open;
    private static LeaderboardPanel activeInstance;
    private CatMovement movement;
    private Button yarnButton,pondButton;
    public static bool IsAnyOpen=>activeInstance!=null && activeInstance.open;

    public bool IsOpen => open;

    private void Awake()
    {
        activeInstance=this;
        StorybookGamesPresentation.ApplyLeaderboard(transform);
        BuildFourGameTabs();
        Bind(closeButton, CloseToGames);
        // The scrim only catches rays. A root Button also receives clicks that
        // bubble from labels/empty card space, which used to dismiss this panel.
        var backdropButton = GetComponent<Button>();
        if (backdropButton != null)
        {
            backdropButton.onClick.RemoveAllListeners();
            backdropButton.enabled = false;
        }
        Bind(runnerButton, () => SelectGame(CompetitionGame.CatRunner));
        Bind(catchButton, () => SelectGame(CompetitionGame.CatCatch));
        Bind(yarnButton,()=>SelectGame(CompetitionGame.YarnRoute));
        Bind(pondButton,()=>SelectGame(CompetitionGame.PondPlay));
        Bind(dailyButton, () => SelectPeriod(CompetitionPeriod.Daily));
        Bind(weeklyButton, () => SelectPeriod(CompetitionPeriod.Weekly));
        Bind(allTimeButton, () => SelectPeriod(CompetitionPeriod.AllTime));
        Bind(refreshButton, Refresh);
        CatIdentityService.Changed += RefreshIdentity;
        HideImmediate();
    }

    private void OnDestroy()
    {
        if(movement!=null)movement.ReleaseInputBlock(this);
        if(activeInstance==this)activeInstance=null;
        CatIdentityService.Changed -= RefreshIdentity;
    }

    public void Show()
    {
        if (HomeUiFlow.IsMiniGameVisible)
            return;
        open = true;
        movement=FindAnyObjectByType<CatMovement>();if(movement!=null)movement.AcquireInputBlock(this);
        gameObject.SetActive(true);
        if (rootGroup != null)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }
        RefreshIdentity();
        RefreshHeaders();
        Refresh();
        ClaimRewards();
    }

    public void Hide() => HideImmediate();

    public void CloseToGames()
    {
        if (!open)
            return;
        HideImmediate();
        FindAnyObjectByType<GamesHubPanel>(FindObjectsInactive.Include)?.Show();
    }
    private void OnDisable() => HideImmediate();

    private void HideImmediate()
    {
        open = false;
        if(movement!=null)movement.ReleaseInputBlock(this);
        requestGeneration++;
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
    }

    private void SelectGame(CompetitionGame value)
    {
        if (game == value)
            return;
        game = value;
        RefreshHeaders();
        Refresh();
    }

    private void SelectPeriod(CompetitionPeriod value)
    {
        if (period == value)
            return;
        period = value;
        RefreshHeaders();
        Refresh();
    }

    private async void Refresh()
    {
        if (!open)
            return;
        int generation = ++requestGeneration;
        SetStatus(GameContentCopy.Text("Sıralama yükleniyor…", "Loading rankings…"));
        if(emptyStateText!=null) { emptyStateText.gameObject.SetActive(true); emptyStateText.text=GameContentCopy.Text("Oyuncular yükleniyor…", "Finding the players…"); }
        SetControls(false);
        CompetitionSnapshot snapshot = await CompetitionService.LoadAsync(game, period);
        if (!open || generation != requestGeneration)
            return;
        ApplySnapshot(snapshot);
        SetControls(true);
    }

    private async void ClaimRewards()
    {
        CompetitionRewardSummary summary = await CompetitionService.ClaimAvailableRewardsAsync();
        if (!open || !summary.HasRewards)
            return;
        SetStatus(GameContentCopy.Text($"Ödüller alındı · {summary.Coins} jeton + {summary.Diamonds} elmas",$"Rewards collected · {summary.Coins} coins + {summary.Diamonds} diamonds"));
    }

    private void ApplySnapshot(CompetitionSnapshot snapshot)
    {
        int totalPlayers = snapshot?.totalPlayers ?? 0;
        int count = snapshot?.entries?.Count ?? 0;
        bool personal=snapshot!=null&&snapshot.isPersonalRecord;
        if(globalSummaryText!=null)globalSummaryText.text=personal?CozyGameUi.Copy("Bu cihazdaki rekor","Best on this device"):GameContentCopy.Text($"{totalPlayers:N0} oyuncu",$"{totalPlayers:N0} players");
        if(emptyStateText!=null)
        {
            emptyStateText.gameObject.SetActive(count==0);
            emptyStateText.text=personal?CozyGameUi.Copy(snapshot.personalBest>0?$"En iyi turun\n{snapshot.personalBest:N0} puan":"Henüz bir skor yok.\nİlk turunu tamamla.",snapshot.personalBest>0?$"Your best round\n{snapshot.personalBest:N0} points":"No score yet.\nComplete your first round."):snapshot != null && snapshot.isOfflineCopy
                ? GameContentCopy.Text("Sıralamaya ulaşılamadı.\nBağlantını kontrol edip yeniden dene.","Rankings are unavailable.\nCheck your connection and try again.")
                : GameContentCopy.Text("Henüz bir skor yok.\nİlk oyununla listede yerini al.","No scores just yet.\nPlay a game to join the list.");
        }
        if(rankRows!=null)for(int i=0;i<rankRows.Length;i++)
        {
            var row=rankRows[i];if(row==null)continue;
            var entry=i<count?snapshot.entries[i]:null;
            row.transform.parent.gameObject.SetActive(entry!=null);
            if(entry==null)continue;
            // Names are user data: escape TMP markup before adding alignment tags.
            var nickname=(entry.nickname??string.Empty).Replace("<","‹").Replace(">","›");
            row.text=$"#{entry.rank}<pos=13%>{nickname}<pos=78%>{entry.score:N0}";
            StorybookGamesPresentation.ApplyRankRow(row, entry.isCurrentPlayer);
        }

        if (ownRankText != null)
        {
            CompetitionEntry own = snapshot?.currentPlayer;
            ownRankText.text = own == null
                ? (string.IsNullOrEmpty(CompetitionService.Nickname)
                    ? GameContentCopy.Text("Katılmak için kedine isim ver", "Name your cat to join")
                    : GameContentCopy.Text("Sıran · İlk oyununla katıl", "Your rank · Play to join"))
                : GameContentCopy.Text($"Sıran #{own.rank} · {own.score:N0} puan",$"Your rank #{own.rank} · {own.score:N0} points");
        }
        SetStatus(string.IsNullOrEmpty(CompetitionService.Nickname)
            ? GameContentCopy.Text("Kedi adı 3–14 harf veya rakam içermeli.", "Cat names need 3–14 letters or numbers.")
            : snapshot != null && snapshot.isOfflineCopy
                ? (count > 0 ? GameContentCopy.Text("Kaydedilmiş liste · Bağlanınca yenilenir", "Saved list · Refreshes when connected") : GameStatusCopy.Text("RANKINGS UNAVAILABLE"))
                : GameStatusCopy.Text(CompetitionService.LastMessage));
        if(personal)
        {
            if(ownRankText!=null)ownRankText.text=CozyGameUi.Copy("Kişisel sonuç · Dünya sıralaması değildir","Personal result · Not a world ranking");
            SetStatus(CozyGameUi.Copy("Çevrimiçi listeye ulaşılamadı. Yerel rekorun korunuyor.","The online board is unavailable. Your local best is saved."));
        }
    }

    private void RefreshHeaders()
    {
        Paint(runnerButton,game==CompetitionGame.CatRunner);Paint(catchButton,game==CompetitionGame.CatCatch);
        Paint(yarnButton,game==CompetitionGame.YarnRoute);Paint(pondButton,game==CompetitionGame.PondPlay);
        var names=new[]{CozyGameUi.Copy("Eve Dönüş","Homeward Run"),CozyGameUi.Copy("Pati Avı","Paw Hunt"),CozyGameUi.Copy("Yumak Rotası","Yarn Trail"),CozyGameUi.Copy("Gölet Keyfi","Pond Moments")};
        var buttons=new[]{runnerButton,catchButton,yarnButton,pondButton};for(int i=0;i<4;i++)if(buttons[i]!=null)CozyGameUi.Label(buttons[i],names[i]);
        Paint(dailyButton,period==CompetitionPeriod.Daily);Paint(weeklyButton,period==CompetitionPeriod.Weekly);Paint(allTimeButton,period==CompetitionPeriod.AllTime);
        if (gameTitle != null)
            gameTitle.text = names[(int)game];
        if (periodTitle != null)
            periodTitle.text = period == CompetitionPeriod.Daily
                ? GameContentCopy.Text("Her gün 00.00 UTC’de yenilenir", "Resets daily at 00:00 UTC")
                : period == CompetitionPeriod.Weekly
                    ? GameContentCopy.Text("Pazartesi yenilenir", "Resets on Monday")
                    : GameContentCopy.Text("Tüm zamanlar", "All time");
    }

    private static void Paint(Button button,bool selected)
    {
        StorybookScreenStyle.Action(button, !selected, selected);
    }

    private void BuildFourGameTabs()
    {
        var card=transform.Find("SafeArea/LeaderboardCard");if(card==null)return;
        yarnButton=CozyGameUi.Button("YarnTab",card,"",Vector2.zero,Vector2.one,null,true);
        pondButton=CozyGameUi.Button("PondTab",card,"",Vector2.zero,Vector2.one,null,true);
        var games=new[]{runnerButton,catchButton,yarnButton,pondButton};
        for(int i=0;i<4;i++)PlaceTab(games[i],-571+i*214,198);
        PlaceTab(dailyButton,300,140);PlaceTab(weeklyButton,454,140);PlaceTab(allTimeButton,608,140);
    }
    private static void PlaceTab(Button button,float x,float width)
    {
        if(button==null)return;var r=(RectTransform)button.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,276);r.sizeDelta=new Vector2(width,62);
        var label=button.GetComponentInChildren<TMP_Text>();if(label!=null){label.enableAutoSizing=true;label.fontSizeMin=17;label.fontSizeMax=23;label.textWrappingMode=TextWrappingModes.NoWrap;}
    }

    private void SetControls(bool enabled)
    {
        if (refreshButton != null)
            refreshButton.interactable = enabled;
        RefreshIdentity();
    }

    private void RefreshIdentity()
    {
        string catName = CatIdentityService.CatName;
        if (catNameText != null)
            catNameText.text = string.IsNullOrWhiteSpace(catName)
                ? GameContentCopy.Text("Kedine isim ver", "Name your cat")
                : catName;
        if (connectionHintText != null)
            connectionHintText.text = CompetitionService.CanSubmit
                ? GameContentCopy.Text("Google bağlı\nSkorların otomatik kaydedilir.", "Google connected\nScores are submitted automatically.")
                : GameContentCopy.Text("Skorlarını paylaşmak için ayarlardan Google hesabını bağla.", "Connect Google in Settings to submit scores and claim rewards.");
    }

    private void SetStatus(string value)
    {
        if (statusText != null)
            statusText.text = value ?? string.Empty;
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        CanvasGroup group,
        Button close,
        Button runner,
        Button catCatch,
        Button daily,
        Button weekly,
        Button allTime,
        Button refresh,
        TMP_Text catName,
        TMP_Text connectionHint,
        TMP_Text selectedGame,
        TMP_Text selectedPeriod,
        TMP_Text globalSummary,
        TMP_Text status,
        TMP_Text ownRank,
        TMP_Text[] podiumNames,
        TMP_Text[] podiumScores,
        TMP_Text[] rows)
    {
        rootGroup = group;
        closeButton = close;
        runnerButton = runner;
        catchButton = catCatch;
        dailyButton = daily;
        weeklyButton = weekly;
        allTimeButton = allTime;
        refreshButton = refresh;
        catNameText = catName;
        connectionHintText = connectionHint;
        gameTitle = selectedGame;
        periodTitle = selectedPeriod;
        globalSummaryText = globalSummary;
        statusText = status;
        ownRankText = ownRank;
        podiumNameTexts = podiumNames ?? Array.Empty<TMP_Text>();
        podiumScoreTexts = podiumScores ?? Array.Empty<TMP_Text>();
        rankRows = rows ?? Array.Empty<TMP_Text>();
        HideImmediate();
    }
#endif
}
