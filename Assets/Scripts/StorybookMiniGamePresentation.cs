using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared mini-game overlay finish. The live HUD, world, portraits and game state remain untouched.</summary>
public static class StorybookMiniGamePresentation
{
    public static void Apply(Transform canvas)
    {
        if (canvas == null) return;
        Welcome(canvas.Find("WelcomePanel/WelcomeSafeArea/WelcomeCardLayout"));
        Pause(canvas.Find("PausePanel/PauseSafeArea/PauseCardLayout"));
        Results(canvas.Find("ResultsPanel/ResultsSafeArea/ResultsCardLayout"));
        Tutorial(canvas.Find("TutorialPanel/TutorialSafeArea/TutorialCardLayout"));
    }

    private static void Welcome(Transform card)
    {
        if (card == null) return;
        StorybookScreenStyle.Shell(Surface(card, "WelcomeCard"), 32f);
        StorybookScreenStyle.Inset(Surface(card, "PlayfulPoster"), new Color32(23, 66, 92, 255), 26f);
        StorybookScreenStyle.Inset(Surface(card, "GameIdentityBanner"), new Color32(23, 66, 92, 255), 24f);
        StorybookScreenStyle.Card(Surface(card, "WelcomeHeroArtFrame"), 20f);
        PortraitFrame(card);
        foreach (string name in new[] { "WelcomeTitle", "PlayStyle", "PlayfulReady", "WelcomeEnergy" })
            StorybookScreenStyle.Text(card, name, StorybookScreenStyle.Cream);
        foreach (string name in new[] { "Tagline", "DailyMissions" })
            StorybookScreenStyle.Text(card, name, StorybookScreenStyle.Mint);
        StorybookScreenStyle.Card(Surface(card, "BestScorePill"), 20f);
        StorybookScreenStyle.Text(card, "BestScorePill/BestScore", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Card(Surface(card, "PlayfulPerks"), 17f);
        StorybookScreenStyle.Text(card, "PlayfulPerks/PerkCopy", StorybookScreenStyle.Ink);
        for (int i = 0; i < 3; i++)
        {
            string path = "ControlStep" + i;
            StorybookScreenStyle.Card(Surface(card, path), 18f);
            StorybookScreenStyle.Text(card, path + "/Gesture", StorybookScreenStyle.Ink);
            StorybookScreenStyle.Text(card, path + "/Instruction", StorybookScreenStyle.Ink);
        }
        Action(card, "WelcomeStartButton", true);
        Action(card, "WelcomeRewardedEnergyButton", true);
        Action(card, "WelcomeExitButton");
        Action(card, "WelcomeGamesButton");
        StorybookScreenStyle.CurrencyIcons(card);
    }

    private static void Pause(Transform card)
    {
        if (card == null) return;
        StorybookScreenStyle.Shell(Surface(card, "PauseCard"), 32f);
        StorybookScreenStyle.Text(card, "PauseTitle", StorybookScreenStyle.Cream);
        StorybookScreenStyle.Inset(Surface(card, "PreferenceSection"), new Color32(22, 36, 76, 255), 24f);
        Action(card, "ResumeButton", true);
        foreach (string name in new[] { "ReducedMotionButton", "SoundButton", "HapticsButton", "PauseExitButton", "PauseGamesButton" })
            Action(card, name);
        StorybookScreenStyle.CurrencyIcons(card);
    }

    private static void Results(Transform card)
    {
        if (card == null) return;
        StorybookScreenStyle.Shell(Surface(card, "ResultsCard"), 32f);
        StorybookScreenStyle.Inset(Surface(card, "ResultBanner"), new Color32(23, 66, 92, 255), 24f);
        StorybookScreenStyle.Text(card, "ResultTitle", StorybookScreenStyle.Cream);
        StorybookScreenStyle.Card(Surface(card, "NewBestBadge"), 16f);
        StorybookScreenStyle.Text(card, "NewBestBadge/NewBestLabel", StorybookScreenStyle.Ink);
        PortraitFrame(card);
        StorybookScreenStyle.Card(Surface(card, "ScoreStage"), 22f);
        StorybookScreenStyle.Text(card, "Score", StorybookScreenStyle.Ink);
        var score = card.Find("Score") as RectTransform;
        // The 92-point numeral needs 111.33 units with the current font metrics.
        if (score != null) score.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 116f);
        StorybookScreenStyle.Text(card, "ScoreCaption", StorybookScreenStyle.Muted);
        foreach (string name in new[] { "Reward", "ResultDetails" })
            StorybookScreenStyle.Text(card, name, StorybookScreenStyle.Cream);
        foreach (string name in new[] { "RewardCaption", "ResultMissions" })
            StorybookScreenStyle.Text(card, name, StorybookScreenStyle.Mint);
        Action(card, "CollectButton");
        Action(card, "ResultGamesButton");
        Action(card, "RetryButton", true);
        Action(card, "DoubleCoinsButton");
        StorybookScreenStyle.CurrencyIcons(card);
    }

    private static void Tutorial(Transform card)
    {
        if (card == null) return;
        StorybookScreenStyle.Shell(Surface(card, "TutorialCard"), 24f);
        StorybookScreenStyle.Text(card, "TutorialMessage", StorybookScreenStyle.Cream);
        Action(card, "TutorialSkipButton");
        StorybookScreenStyle.CurrencyIcons(card);
    }

    private static void PortraitFrame(Transform card)
    {
        StorybookScreenStyle.Enamel(Surface(card, "SelectedCatFrame"),
            StorybookScreenStyle.Mint, StorybookScreenStyle.Teal, 24f);
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }

    private static void Action(Transform root, string path, bool coral = false)
    {
        var child = root.Find(path);
        if (child != null) StorybookScreenStyle.Action(child.GetComponent<Button>(), false, coral);
    }
}
