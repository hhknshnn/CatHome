using CatHome.Economy;
using TMPro;
using UnityEngine;

/// <summary>
/// Live-binds the premium main-menu top HUD (coin / gem / home level / XP bar)
/// to the real economy and progression services so the menu never shows a stale
/// baked value. UI-only: reads services every frame, writes no state.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuTopHudBinder : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text gemText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private RectTransform xpFill;
    [SerializeField, Min(1f)] private float xpTrackWidth = 284f;

    private void OnEnable() => Refresh();

    private void Update() => Refresh();

    private void Refresh()
    {
        if (coinText != null)
            coinText.text = EconomyService.Coins.ToString("N0");
        if (gemText != null)
            gemText.text = EconomyService.Diamonds.ToString("N0");
        if (levelText != null)
            levelText.text = HomeProgressionService.HomeLevel.ToString();

        long into = HomeProgressionService.XpIntoCurrentLevel;
        long span = HomeProgressionService.XpForCurrentLevel;
        float ratio = span > 0 ? Mathf.Clamp01(into / (float)span) : 1f;
        if (xpFill != null)
            xpFill.sizeDelta = new Vector2(Mathf.Max(18f, xpTrackWidth * ratio), xpFill.sizeDelta.y);
        if (xpText != null)
            xpText.text = span > 0 ? into.ToString("N0") + " / " + span.ToString("N0") : "MAX";
    }
}
