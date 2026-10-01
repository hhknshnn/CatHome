using UnityEngine;
using UnityEngine.UI;

/// <summary>Title subwindows only; preserves the approved main menu and the Google-branded control.</summary>
public static class StorybookTitleSubwindowPresentation
{
    private static readonly Color Status = new Color32(255, 177, 164, 255);

    public static void Apply(Transform root)
    {
        if (root == null) return;
        var credits = root.Find("SafeArea/CreditsOverlay/CreditsCard");
        if (credits != null)
        {
            StorybookScreenStyle.Shell(credits.GetComponent<LowPolyPanelGraphic>(), 32f);
            StorybookScreenStyle.Text(credits, "CreditsTitle", StorybookScreenStyle.Cream);
            StorybookScreenStyle.Text(credits, "CreditsBody", StorybookScreenStyle.Cream);
            StorybookScreenStyle.Text(credits, "Version", StorybookScreenStyle.Mint);
            Action(credits, "CreditsCloseButton");
        }

        var newGame = root.Find("SafeArea/NewGameOverlay/NewGameCard");
        if (newGame != null)
        {
            StorybookScreenStyle.Shell(newGame.GetComponent<LowPolyPanelGraphic>(), 32f);
            StorybookScreenStyle.Inset(Surface(newGame, "MomentStage"), StorybookScreenStyle.Mint, 28f);
            StorybookScreenStyle.Text(newGame, "MomentStage/MomentCaption", StorybookScreenStyle.Ink);
            StorybookScreenStyle.Text(newGame, "NewGameTitle", StorybookScreenStyle.Cream);
            StorybookScreenStyle.Card(Surface(newGame, "ResetSummary"), 22f);
            StorybookScreenStyle.Text(newGame, "NewGameBody", StorybookScreenStyle.Ink);
            StorybookScreenStyle.Inset(Surface(newGame, "KeptSummary"), StorybookScreenStyle.Mint, 22f);
            StorybookScreenStyle.Text(newGame, "KeptBody", StorybookScreenStyle.Ink);
            StorybookScreenStyle.Text(newGame, "NewGameStatus", Status);
            Action(newGame, "NewGameCancelButton");
            Action(newGame, "NewGameConfirmButton", false, true);
        }

        var account = root.Find("SafeArea/AccountChoiceOverlay/AccountChoiceCard");
        if (account != null)
        {
            StorybookScreenStyle.Shell(account.GetComponent<LowPolyPanelGraphic>(), 32f);
            StorybookScreenStyle.Text(account, "AccountChoiceTitle", StorybookScreenStyle.Cream);
            StorybookScreenStyle.Text(account, "AccountChoiceSubtitle", StorybookScreenStyle.Mint);
            StorybookScreenStyle.Text(account, "GuestNote", StorybookScreenStyle.Mint);
            StorybookScreenStyle.Text(account, "AccountChoiceStatus", Status);
            Action(account, "GuestContinueButton", false, true);
            Action(account, "AccountBackButton", true);
            // GoogleSignInButton retains its authored official white surface, mark and protected text padding.
        }
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }

    private static void Action(Transform root, string path, bool secondary = false, bool coral = false)
    {
        var child = root.Find(path);
        if (child != null) StorybookScreenStyle.Action(child.GetComponent<Button>(), secondary, coral);
    }
}
