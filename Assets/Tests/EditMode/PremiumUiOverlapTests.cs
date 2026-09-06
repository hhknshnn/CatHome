using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Project-wide guard for authored UI collisions. Decorative layers may overlap
/// their own parent card, but separate visible controls may not occupy the same
/// pixels. Screen-specific checks lock the two cross-surface gutters that a
/// prefab-only scan cannot see.
/// </summary>
public sealed class PremiumUiOverlapTests
{
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
    private const string HomeEditPrefabPath = "Assets/UI/HomeEditPanel.prefab";
    private const string ShopPrefabPath = "Assets/UI/ShopPanel.prefab";
    private const string OfflinePrefabPath = "Assets/UI/WhileYouWereAwayPopup.prefab";
    private const float MinimumGutter = 16f;

    [Test]
    public void EveryUiPrefab_KeepsDefaultVisibleButtonsSeparate()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/UI" });
        var failures = new List<string>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            GameObject canvasObject = CreateAuditCanvas();
            GameObject instance = UnityEngine.Object.Instantiate(prefab, canvasObject.transform, false);
            try
            {
                RectTransform root = instance.transform as RectTransform;
                if (root != null)
                    Stretch(root);
                CanvasGroup rootGroup = instance.GetComponent<CanvasGroup>();
                if (rootGroup != null)
                    rootGroup.alpha = 1f;
                Canvas.ForceUpdateCanvases();
                if (root != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                Canvas.ForceUpdateCanvases();

                Button[] buttons = instance.GetComponentsInChildren<Button>(true);
                for (int a = 0; a < buttons.Length; a++)
                {
                    if (!IsAuditable(buttons[a]))
                        continue;
                    Rect first = VisibleRect(buttons[a].transform as RectTransform);
                    for (int b = a + 1; b < buttons.Length; b++)
                    {
                        if (!IsAuditable(buttons[b]) ||
                            buttons[a].transform.IsChildOf(buttons[b].transform) ||
                            buttons[b].transform.IsChildOf(buttons[a].transform))
                            continue;
                        Rect second = VisibleRect(buttons[b].transform as RectTransform);
                        if (IntersectionWidth(first, second) > 2f &&
                            IntersectionHeight(first, second) > 2f)
                        {
                            failures.Add(path + ": " + HierarchyPath(buttons[a].transform) +
                                         " overlaps " + HierarchyPath(buttons[b].transform));
                        }
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public void HomeEditActions_KeepSixteenPixelsClearOfPersistentDock()
    {
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        GameObject editInstance = null;
        try
        {
            Scene ui = SceneManager.GetSceneByName("CatHome_UI");
            if (!ui.IsValid() || !ui.isLoaded)
                ui = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
            Canvas canvas = FindNamedCanvas(ui, "Canvas");
            Assert.That(canvas, Is.Not.Null);
            RectTransform dock = FindNamedRect(ui, "HomeDock");
            Assert.That(dock, Is.Not.Null);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HomeEditPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            editInstance = UnityEngine.Object.Instantiate(prefab, canvas.transform, false);
            Stretch(editInstance.transform as RectTransform);
            CanvasGroup group = editInstance.GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = 1f;
            Canvas.ForceUpdateCanvases();

            Rect dockRect = WorldRect(dock);
            string[] actionNames = { "RotateButton", "StoreItemButton", "DoneButton" };
            for (int i = 0; i < actionNames.Length; i++)
            {
                RectTransform action = FindNamedRect(editInstance.transform, actionNames[i]);
                Assert.That(action, Is.Not.Null, actionNames[i]);
                Rect actionRect = WorldRect(action);
                Assert.That(actionRect.yMin - dockRect.yMax, Is.GreaterThanOrEqualTo(MinimumGutter),
                    actionNames[i] + " is too close to HomeDock.");
            }
        }
        finally
        {
            if (editInstance != null)
                UnityEngine.Object.DestroyImmediate(editInstance);
            if (setup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    [Test]
    public void HomeEditActions_StayFullyInsideToolbarCard()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HomeEditPrefabPath);
        Assert.That(prefab, Is.Not.Null);
        RectTransform toolbar = FindNamedRect(prefab.transform, "Toolbar");
        Assert.That(toolbar, Is.Not.Null);
        Rect toolbarRect = RectRelativeTo(toolbar, toolbar);
        string[] actionNames = { "RotateButton", "StoreItemButton", "DoneButton" };
        for (int i = 0; i < actionNames.Length; i++)
        {
            RectTransform action = FindNamedRect(prefab.transform, actionNames[i]);
            Assert.That(action, Is.Not.Null, actionNames[i]);
            Rect actionRect = RectRelativeTo(action, toolbar);
            Assert.That(actionRect.xMin, Is.GreaterThanOrEqualTo(toolbarRect.xMin + 12f),
                actionNames[i] + " crosses the toolbar's left rim.");
            Assert.That(actionRect.xMax, Is.LessThanOrEqualTo(toolbarRect.xMax - 12f),
                actionNames[i] + " crosses the toolbar's right rim.");
            Assert.That(actionRect.yMin, Is.GreaterThanOrEqualTo(toolbarRect.yMin + 12f),
                actionNames[i] + " crosses the toolbar's lower rim.");
            Assert.That(actionRect.yMax, Is.LessThanOrEqualTo(toolbarRect.yMax - 12f),
                actionNames[i] + " crosses the toolbar's upper rim.");
        }
    }

    [Test]
    public void ShopProductCards_KeepStatusBadgeClearOfPriceAndAction()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPrefabPath);
        Assert.That(prefab, Is.Not.Null);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            RectTransform[] rects = instance.GetComponentsInChildren<RectTransform>(true);
            int checkedCards = 0;
            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform badge = rects[i];
                if (badge.name != "OwnedBadge" || badge.parent == null)
                    continue;
                RectTransform card = badge.parent as RectTransform;
                RectTransform price = FindNamedRect(card, "CurrencyPriceGroup");
                RectTransform action = FindNamedRect(card, "BuyButton");
                Assert.That(price, Is.Not.Null, card.name);
                Assert.That(action, Is.Not.Null, card.name);
                Rect badgeRect = RectRelativeTo(badge, card);
                Assert.That(badgeRect.Overlaps(RectRelativeTo(price, card)), Is.False,
                    card.name + " status badge overlaps its price capsule.");
                Assert.That(badgeRect.Overlaps(RectRelativeTo(action, card)), Is.False,
                    card.name + " status badge overlaps its action button.");
                Assert.That(RectRelativeTo(price, card).Overlaps(RectRelativeTo(action, card)),
                    Is.False, card.name + " price capsule overlaps its action button.");
                var title = FindNamedRect(card,"ProductTitle");
                var photo = FindNamedRect(card,"ProductPhoto");
                Assert.That(title,Is.Not.Null);
                Assert.That(RectRelativeTo(title,card).Overlaps(RectRelativeTo(action,card)),Is.False,card.name);
                checkedCards++;
            }
            Assert.That(checkedCards, Is.GreaterThanOrEqualTo(HomeStoreService.Products.Count));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void HomeEdit_RefusesToStackOverOfflinePopup()
    {
        GameObject editPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HomeEditPrefabPath);
        GameObject offlinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OfflinePrefabPath);
        Assert.That(editPrefab, Is.Not.Null);
        Assert.That(offlinePrefab, Is.Not.Null);
        GameObject offline = UnityEngine.Object.Instantiate(offlinePrefab);
        GameObject edit = UnityEngine.Object.Instantiate(editPrefab);
        try
        {
            WhileYouWereAwayPopup popup = offline.GetComponent<WhileYouWereAwayPopup>();
            HomeEditModeController controller = edit.GetComponent<HomeEditModeController>();
            Assert.That(popup, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            typeof(WhileYouWereAwayPopup).GetField(
                "isOpen", System.Reflection.BindingFlags.Instance |
                          System.Reflection.BindingFlags.NonPublic).SetValue(popup, true);

            controller.Open();

            Assert.That(controller.IsOpen, Is.False,
                "EDIT ROOM must wait until the blocking offline popup closes.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(edit);
            UnityEngine.Object.DestroyImmediate(offline);
        }
    }

    private static GameObject CreateAuditCanvas()
    {
        var canvasObject = new GameObject("OverlapAuditCanvas", typeof(RectTransform), typeof(Canvas));
        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920f, 1080f);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        return canvasObject;
    }

    private static bool IsAuditable(Button button)
    {
        if (button == null || !button.gameObject.activeInHierarchy ||
            button.name.IndexOf("Scrim", StringComparison.OrdinalIgnoreCase) >= 0 ||
            button.name.IndexOf("DragSurface", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        RectTransform rect = button.transform as RectTransform;
        if (rect == null || rect.rect.width < 2f || rect.rect.height < 2f)
            return false;
        CanvasGroup[] groups = button.GetComponentsInParent<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
            if (groups[i].alpha < .01f)
                return false;
        return true;
    }

    private static Canvas FindNamedCanvas(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
                if (canvases[i].name == name)
                    return canvases[i];
        }
        return null;
    }

    private static RectTransform FindNamedRect(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            RectTransform found = FindNamedRect(root.transform, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static RectTransform FindNamedRect(Transform root, string name)
    {
        if (root == null)
            return null;
        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
            if (rects[i].name == name)
                return rects[i];
        return null;
    }

    private static Rect VisibleRect(RectTransform rect)
    {
        Rect result=WorldRect(rect);
        for(Transform p=rect.parent;p!=null;p=p.parent)
        {
            var clip=p.GetComponent<RectMask2D>(); var mask=p.GetComponent<Mask>();
            if((clip!=null && clip.isActiveAndEnabled)||(mask!=null && mask.isActiveAndEnabled))
            {
                Rect bounds=WorldRect((RectTransform)p);
                float left=Mathf.Max(result.xMin,bounds.xMin),bottom=Mathf.Max(result.yMin,bounds.yMin);
                float right=Mathf.Min(result.xMax,bounds.xMax),top=Mathf.Min(result.yMax,bounds.yMax);
                if(right<=left || top<=bottom)return new Rect(left,bottom,0,0);
                result=Rect.MinMaxRect(left,bottom,right,top);
            }
        }
        return result;
    }

    private static Rect WorldRect(RectTransform rect)
    {
        if (rect == null)
            return default;
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    private static Rect RectRelativeTo(RectTransform rect, RectTransform relativeTo)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 min = relativeTo.InverseTransformPoint(corners[0]);
        Vector3 max = relativeTo.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static float IntersectionWidth(Rect a, Rect b) =>
        Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));

    private static float IntersectionHeight(Rect a, Rect b) =>
        Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));

    private static string HierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
