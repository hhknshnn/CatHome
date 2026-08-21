using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires the premium main-menu hub buttons (SHOP / ROOMS / GAMES / TASKS / gear)
/// to the real gameplay panels. Cards that lead into the home hub dismiss the
/// title overlay first (their panels sort below the title); Settings sorts above
/// the title so it opens directly over the menu.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuHubLauncher : MonoBehaviour
{
    public void OpenShop()
    {
        Dismiss();
        var p = FindAnyObjectByType<ShopPanelController>(FindObjectsInactive.Include);
        if (p != null) p.RequestOpen();
    }

    public void OpenRooms()
    {
        Dismiss();
        var p = FindAnyObjectByType<RoomSelectorPanel>(FindObjectsInactive.Include);
        if (p != null) p.RequestOpen();
    }

    public void OpenGames()
    {
        Dismiss();
        var g = FindAnyObjectByType<GamesHubPanel>(FindObjectsInactive.Include);
        if (g != null) g.Show();
    }

    public void OpenTasks()
    {
        Dismiss();
        var p = FindAnyObjectByType<QuestPanelController>(FindObjectsInactive.Include);
        if (p != null) p.RequestOpen();
    }

    public void OpenSettings()
    {
        var p = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (p != null) p.RequestOpen();
    }

    private void Dismiss()
    {
        var ts = FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
        if (ts == null) return;
        foreach (var b in ts.GetComponentsInChildren<Button>(true))
        {
            if (b.name == "PlayButton")
            {
                b.onClick.Invoke();
                return;
            }
        }
    }
}
