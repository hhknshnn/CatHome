using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using U = PremiumUiElements;

/// <summary>Final navigation row pass; does not regenerate the games or their art.</summary>
public static class MiniGameNavigationBuilder
{
    public static void Apply(Transform root)
    {
        if (root == null) return;
        foreach (var panel in root.GetComponentsInChildren<LeaderboardPanel>(true))
        {
            var backgroundButton = panel.GetComponent<Button>();
            if (backgroundButton != null) Object.DestroyImmediate(backgroundButton);
        }
        foreach (var card in root.GetComponentsInChildren<RectTransform>(true).ToArray())
        {
            if (card.name == "WelcomeCardLayout") Pair(card, "WelcomeExitButton", "WelcomeGamesButton");
            else if (card.name == "PauseCardLayout") Pair(card, "PauseExitButton", "PauseGamesButton");
            else if (card.name == "ResultsCardLayout") ResultRow(card);
        }

        var runner = root.GetComponentInParent<CatRunnerGameController>() ??
            root.GetComponentInChildren<CatRunnerGameController>(true);
        if (runner != null)
        {
            runner.EditorConfigureNavigation(FindButton(runner.transform, "WelcomeGamesButton"),
                FindButton(runner.transform, "PauseGamesButton"), FindButton(runner.transform, "ResultGamesButton"));
            EditorUtility.SetDirty(runner);
        }
        var catcher = root.GetComponentInParent<CatCatchGameController>() ??
            root.GetComponentInChildren<CatCatchGameController>(true);
        if (catcher != null)
        {
            catcher.EditorConfigureNavigation(FindButton(catcher.transform, "WelcomeGamesButton"),
                FindButton(catcher.transform, "PauseGamesButton"), FindButton(catcher.transform, "ResultGamesButton"));
            EditorUtility.SetDirty(catcher);
        }
    }

    private static void Pair(RectTransform card, string homeName, string gamesName)
    {
        var home = FindButton(card, homeName);
        if (home == null) return;
        var rect = (RectTransform)home.transform;
        var games = FindButton(card, gamesName);
        float left = rect.anchoredPosition.x - rect.rect.width * .5f;
        float right = rect.anchoredPosition.x + rect.rect.width * .5f;
        if (games != null)
        {
            var existing = (RectTransform)games.transform;
            // Union preserves the authored row when this pass runs twice;
            // if the styling pass expanded Home again it also contains Games.
            left = Mathf.Min(left, existing.anchoredPosition.x - existing.rect.width * .5f);
            right = Mathf.Max(right, existing.anchoredPosition.x + existing.rect.width * .5f);
        }
        float width = (right - left - 16f) * .5f;
        games = EnsureGamesButton(card, gamesName, home);
        At(home, left + width * .5f, rect.anchoredPosition.y, width, rect.rect.height);
        At(games, right - width * .5f, rect.anchoredPosition.y, width, rect.rect.height);
    }

    private static void ResultRow(RectTransform card)
    {
        var home = FindButton(card, "CollectButton");
        var retry = FindButton(card, "RetryButton");
        if (home == null || retry == null) return;
        var homeRect = (RectTransform)home.transform;
        var retryRect = (RectTransform)retry.transform;
        float left = homeRect.anchoredPosition.x - homeRect.rect.width * .5f;
        float right = retryRect.anchoredPosition.x + retryRect.rect.width * .5f;
        float width = (right - left - 32f) / 3f;
        float y = homeRect.anchoredPosition.y;
        float height = homeRect.rect.height;
        var games = EnsureGamesButton(card, "ResultGamesButton", home);
        At(home, left + width * .5f, y, width, height);
        At(games, (left + right) * .5f, y, width, height);
        At(retry, right - width * .5f, y, width, height);
    }

    private static Button EnsureGamesButton(Transform card, string name, Button sample)
    {
        var button = FindButton(card, name);
        TMP_Text label;
        if (button == null)
        {
            var sampleLabel = sample.GetComponentInChildren<TMP_Text>(true);
            button = U.Action(name, card, sampleLabel != null ? sampleLabel.font : null,
                null, PlayfulUiArt.Violet, 0, 0, 300, 72, out label);
        }
        else label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            var copy = label.GetComponent<BilingualCopyLabel>() ?? label.gameObject.AddComponent<BilingualCopyLabel>();
            copy.Configure("Oyunlara dön", "Back to games");
        }
        PlayfulUiArt.Action(button, PlayfulUiArt.Violet, true);
        return button;
    }

    private static void At(Button button, float x, float y, float width, float height)
    {
        U.At((RectTransform)button.transform, x, y, width, height);
        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            label.enableAutoSizing = true;
            label.fontSize = 26;
            label.fontSizeMin = 20;
            label.fontSizeMax = 26;
        }
    }

    private static Button FindButton(Transform root, string name) =>
        root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);
}
