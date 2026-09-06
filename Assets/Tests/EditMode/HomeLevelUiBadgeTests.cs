using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public sealed class HomeLevelUiBadgeTests
{
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
    private const string CurrencyHudPrefabPath = "Assets/UI/CurrencyHud.prefab";
    private const string ShopPanelPrefabPath = "Assets/UI/ShopPanel.prefab";

    [SetUp]
    public void SetUp()
    {
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
    }

    [TearDown]
    public void TearDown()
    {
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
    }

    [TestCase("Assets/UI/MainPanel.prefab")]
    [TestCase("Assets/UI/ShopPanel.prefab")]
    public void HomeLevelBadge_UsesReadableBodyTypeAndUpdatesActualLevel(string path)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab,Is.Not.Null);
        var instance=Object.Instantiate(prefab);
        try
        {
            var text=FindComponent<TMP_Text>(instance.transform,"HomeLevelText");
            Assert.That(text,Is.Not.Null);
            Assert.That(text.GetComponent<HomeLevelBadgeLabel>(),Is.Not.Null);
            Assert.That(PremiumTypography.Body,Is.Not.Null);
            Assert.That(text.font,Is.SameAs(PremiumTypography.Body));
            Assert.That((text.fontStyle & FontStyles.Bold),Is.EqualTo(FontStyles.Normal));
            SetLevel(4);
            Assert.That(text.text,Is.EqualTo(GameLanguageService.Format("home.level",4)));
            HomeProgressionService.GrantHomeXp(HomeProgressionService.CumulativeXpForLevel(5)-HomeProgressionService.CumulativeXpForLevel(4));
            Assert.That(text.text,Is.EqualTo(GameLanguageService.Format("home.level",5)));
        }
        finally {Object.DestroyImmediate(instance);}
    }

    [Test]
    public void BottomDockRoomName_AutoFitsEveryCanonicalRoomOnOneCenteredLine()
    {
        bool openedUiScene = false;
        Scene uiScene = SceneManager.GetSceneByPath(UiScenePath);
        if (!uiScene.IsValid() || !uiScene.isLoaded)
        {
            openedUiScene = true;
            uiScene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
        }

        try
        {
            GameObject launchUi = GameObject.Find("RunnerLaunchUI");
            Assert.That(launchUi, Is.Not.Null);
            TMP_Text roomLabel = FindComponent<TMP_Text>(
                launchUi.transform, "RoomProgressLabel");
            Assert.That(roomLabel, Is.Not.Null);
            Assert.That(roomLabel.enableAutoSizing, Is.True);
            Assert.That(roomLabel.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap));
            Assert.That(roomLabel.alignment, Is.EqualTo(TextAlignmentOptions.Center));
            Assert.That(roomLabel.fontSizeMin, Is.GreaterThanOrEqualTo(18f));
            Assert.That(roomLabel.fontSizeMax, Is.GreaterThanOrEqualTo(26f), "The reference dock uses readable action-size room labels.");

            string oldText=roomLabel.text;
            try { foreach (HomeRoomDefinition room in HomeRoomService.Rooms)
            {
                roomLabel.text = room.DisplayName;
                roomLabel.ForceMeshUpdate();
                Assert.That(roomLabel.isTextOverflowing, Is.False,
                    room.DisplayName + " must fit inside the middle dock capsule.");
                Assert.That(roomLabel.textInfo.lineCount, Is.EqualTo(1),
                    room.DisplayName + " must stay on one line.");
            } } finally {roomLabel.text=oldText;}
        }
        finally
        {
            if (openedUiScene && uiScene.IsValid())
                EditorSceneManager.CloseScene(uiScene, true);
        }
    }

    private static RectTransform FindNeedRect(string primary, string fallback = null)
    {
        GameObject found = GameObject.Find(primary) ??
                           (string.IsNullOrEmpty(fallback) ? null : GameObject.Find(fallback));
        return found != null ? found.GetComponent<RectTransform>() : null;
    }

    private static void SetLevel(int level)
    {
        HomeProgressionService.ApplySavedState(new HomeProgressionSaveState
        {
            homeProgressionVersion = HomeProgressionService.SaveVersion,
            homeXp = HomeProgressionService.CumulativeXpForLevel(level)
        });
    }

    private static bool IsInside(RectTransform container, RectTransform content)
    {
        Rect containerRect = WorldRect(container);
        Rect contentRect = WorldRect(content);
        return containerRect.xMin <= contentRect.xMin + 0.01f &&
               containerRect.xMax >= contentRect.xMax - 0.01f &&
               containerRect.yMin <= contentRect.yMin + 0.01f &&
               containerRect.yMax >= contentRect.yMax - 0.01f;
    }

    private static Rect WorldRect(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        float xMin = Mathf.Min(corners[0].x, corners[2].x);
        float xMax = Mathf.Max(corners[0].x, corners[2].x);
        float yMin = Mathf.Min(corners[0].y, corners[2].y);
        float yMax = Mathf.Max(corners[0].y, corners[2].y);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static T FindComponent<T>(Transform root, string name) where T : Component
    {
        Transform found = FindNamed(root, name);
        return found != null ? found.GetComponent<T>() : null;
    }

    private static Transform FindNamed(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform match = FindNamed(child, name);
            if (match != null)
                return match;
        }

        return null;
    }
}
