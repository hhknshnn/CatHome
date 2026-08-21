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

    [Test]
    public void HudHomeLevelBadge_UsesFredoka_ShowsBackendValue_AndAvoidsNeedBars()
    {
        bool openedUiScene = false;
        Scene uiScene = SceneManager.GetSceneByPath(UiScenePath);
        if (!uiScene.IsValid() || !uiScene.isLoaded)
        {
            openedUiScene = true;
            uiScene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Single);
        }
        try
        {
            SetLevel(3);

            TMP_FontAsset fredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                PremiumUiStyle.PremiumFontAssetPath);
            Assert.That(fredoka, Is.Not.Null);

            GameObject hudCanvas = GameObject.Find("CurrencyHudCanvas");
            Assert.That(hudCanvas, Is.Not.Null);
            TMP_Text badgeText = FindComponent<TMP_Text>(hudCanvas.transform, "HomeLevelText");
            Assert.That(badgeText, Is.Not.Null);
            Assert.That(badgeText.text, Is.EqualTo("HOME LV. " + HomeProgressionService.HomeLevel));
            Assert.That(badgeText.font, Is.EqualTo(fredoka));

            RectTransform safeArea = FindNamed(hudCanvas.transform, "SafeArea") as RectTransform;
            Assert.That(safeArea, Is.Not.Null);
            Assert.That(IsInside(safeArea, badgeText.rectTransform), Is.True);

            RectTransform hunger = FindNeedRect("HungerUI", "FoodBar");
            RectTransform thirst = FindNeedRect("ThirstUI");
            RectTransform energy = FindNeedRect("EnergyUI");
            Rect badgeRect = WorldRect(badgeText.rectTransform);
            Assert.That(hunger, Is.Not.Null);
            Assert.That(thirst, Is.Not.Null);
            Assert.That(energy, Is.Not.Null);
            Assert.That(badgeRect.Overlaps(WorldRect(hunger)), Is.False);
            Assert.That(badgeRect.Overlaps(WorldRect(thirst)), Is.False);
            Assert.That(badgeRect.Overlaps(WorldRect(energy)), Is.False);
        }
        finally
        {
            if (openedUiScene && uiScene.IsValid())
                EditorSceneManager.CloseScene(uiScene, true);
        }
    }

    [Test]
    public void ShopHomeLevelBadge_UsesFredoka_ShowsBackendValue_AndAvoidsHeaderElements()
    {
        TMP_FontAsset fredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            PremiumUiStyle.PremiumFontAssetPath);
        Assert.That(fredoka, Is.Not.Null);

        SetLevel(4);
        GameObject shopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPanelPrefabPath);
        Assert.That(shopPrefab, Is.Not.Null);

        GameObject shop = Object.Instantiate(shopPrefab);
        try
        {
            Canvas.ForceUpdateCanvases();

            TMP_Text badgeText = FindComponent<TMP_Text>(shop.transform, "HomeLevelText");
            Assert.That(badgeText, Is.Not.Null);
            Assert.That(badgeText.text, Is.EqualTo("HOME LV. " + HomeProgressionService.HomeLevel));
            Assert.That(badgeText.font, Is.EqualTo(fredoka));

            Rect badge = WorldRect(badgeText.rectTransform);
            RectTransform wallet = FindNamed(shop.transform, "Wallet") as RectTransform;
            RectTransform collection = FindNamed(shop.transform, "CollectionProgressRim") as RectTransform;
            RectTransform title = FindNamed(shop.transform, "Title") as RectTransform;
            RectTransform tabCat = FindNamed(shop.transform, "Tab_CATRim") as RectTransform;
            RectTransform tabRoom = FindNamed(shop.transform, "Tab_ROOMRim") as RectTransform;
            RectTransform tabHome = FindNamed(shop.transform, "Tab_HOMERim") as RectTransform;
            Assert.That(wallet, Is.Not.Null);
            Assert.That(collection, Is.Not.Null);
            Assert.That(title, Is.Not.Null);
            Assert.That(tabCat, Is.Not.Null);
            Assert.That(tabRoom, Is.Not.Null);
            Assert.That(tabHome, Is.Not.Null);
            Assert.That(badge.Overlaps(WorldRect(wallet)), Is.False);
            Assert.That(badge.Overlaps(WorldRect(collection)), Is.False);
            Assert.That(badge.Overlaps(WorldRect(title)), Is.False);
            Assert.That(badge.Overlaps(WorldRect(tabCat)), Is.False);
            Assert.That(badge.Overlaps(WorldRect(tabRoom)), Is.False);
            Assert.That(badge.Overlaps(WorldRect(tabHome)), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(shop);
        }
    }

    [Test]
    public void HomeLevelBadges_RefreshWhenHomeXpIncreases()
    {
        GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurrencyHudPrefabPath);
        GameObject shopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPanelPrefabPath);
        Assert.That(hudPrefab, Is.Not.Null);
        Assert.That(shopPrefab, Is.Not.Null);

        GameObject hud = Object.Instantiate(hudPrefab);
        GameObject shop = Object.Instantiate(shopPrefab);
        try
        {
            Canvas.ForceUpdateCanvases();

            TMP_Text hudText = FindComponent<TMP_Text>(hud.transform, "HomeLevelText");
            TMP_Text shopText = FindComponent<TMP_Text>(shop.transform, "HomeLevelText");
            Assert.That(hudText, Is.Not.Null);
            Assert.That(shopText, Is.Not.Null);
            Assert.That(hudText.text, Is.EqualTo("HOME LV. 1"));
            Assert.That(shopText.text, Is.EqualTo("HOME LV. 1"));

            long levelThreeXp = HomeProgressionService.CumulativeXpForLevel(3);
            HomeProgressionService.GrantHomeXp(levelThreeXp);
            Assert.That(hudText.text, Is.EqualTo("HOME LV. 3"));
            Assert.That(shopText.text, Is.EqualTo("HOME LV. 3"));
        }
        finally
        {
            Object.DestroyImmediate(shop);
            Object.DestroyImmediate(hud);
        }
    }

    [Test]
    public void BottomDockRoomName_AutoFitsEveryCanonicalRoomOnOneCenteredLine()
    {
        bool openedUiScene = false;
        Scene uiScene = SceneManager.GetSceneByPath(UiScenePath);
        if (!uiScene.IsValid() || !uiScene.isLoaded)
        {
            openedUiScene = true;
            uiScene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Single);
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
            Assert.That(roomLabel.fontSizeMin, Is.LessThanOrEqualTo(11f));
            Assert.That(roomLabel.fontSizeMax, Is.EqualTo(19f).Within(0.01f));

            foreach (HomeRoomDefinition room in HomeRoomService.Rooms)
            {
                roomLabel.text = room.DisplayName + "  •  LEVEL 1";
                roomLabel.ForceMeshUpdate();
                Assert.That(roomLabel.isTextOverflowing, Is.False,
                    room.DisplayName + " must fit inside the middle dock capsule.");
                Assert.That(roomLabel.textInfo.lineCount, Is.EqualTo(1),
                    room.DisplayName + " must stay on one line.");
            }
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
