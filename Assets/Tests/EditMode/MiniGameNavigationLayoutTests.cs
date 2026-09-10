using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class MiniGameNavigationLayoutTests
{
    [Test]
    public void NavigationPass_IsIdempotent_AndKeepsDistinctButtonsWithinTheirCards()
    {
        var root = new GameObject("Navigation layout test", typeof(RectTransform));
        try
        {
            var welcome = PremiumMiniGameUiBuilder.Welcome(root.transform, true);
            var result = PremiumMiniGameUiBuilder.Results(root.transform, true);
            var pause = PremiumMiniGameUiBuilder.Pause(root.transform, true);
            MiniGameNavigationBuilder.Apply(root.transform);
            var snapshots = root.GetComponentsInChildren<Button>(true)
                .ToDictionary(b => b.name, b => ((RectTransform)b.transform).anchoredPosition);
            MiniGameNavigationBuilder.Apply(root.transform);
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                Assert.That(((RectTransform)button.transform).anchoredPosition, Is.EqualTo(snapshots[button.name]));
            CheckRow(welcome.Card, "WelcomeExitButton", "WelcomeGamesButton");
            CheckRow(pause.Card, "PauseExitButton", "PauseGamesButton");
            CheckRow(result.Card, "CollectButton", "ResultGamesButton", "RetryButton");
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void CheckRow(RectTransform card, params string[] names)
    {
        var buttons = names.Select(name => card.GetComponentsInChildren<Button>(true).Single(b => b.name == name))
            .Select(b => (RectTransform)b.transform).OrderBy(r => r.anchoredPosition.x).ToArray();
        float previousRight = card.rect.xMin;
        foreach (var rect in buttons)
        {
            float left = rect.anchoredPosition.x - rect.rect.width * .5f;
            float right = rect.anchoredPosition.x + rect.rect.width * .5f;
            Assert.That(left, Is.GreaterThanOrEqualTo(previousRight));
            Assert.That(right, Is.LessThanOrEqualTo(card.rect.xMax));
            Assert.That(rect.rect.width, Is.GreaterThan(240));
            Assert.That(rect.anchoredPosition.y - rect.rect.height * .5f, Is.GreaterThanOrEqualTo(card.rect.yMin));
            previousRight = right;
        }
    }
}
