using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class HomeEditPresentationTests
{
    private const string PrefabPath = "Assets/UI/HomeEditPanel.prefab";

    [Test]
    public void EditRoomPanel_KeepsPremiumControlsAndInputSurface()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null, PrefabPath);
        Assert.That(prefab.GetComponent<HomeEditModeController>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<HomePlacementZoneGuide>(), Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<HomeEditInputSurface>(true), Is.Not.Null);
        Assert.That(prefab.transform.Find("SafeArea/HeaderCard"), Is.Not.Null);
        Assert.That(prefab.transform.Find("SafeArea/Toolbar"), Is.Not.Null);
        Assert.That(FindDeep(prefab.transform, "PlacementRuleBadge"), Is.Not.Null);

        string[] buttons =
        {
            "PreviousItemButton", "NextItemButton", "RotateButton",
            "StoreItemButton", "DoneButton"
        };
        for (int i = 0; i < buttons.Length; i++)
        {
            Transform button = FindDeep(prefab.transform, buttons[i]);
            Assert.That(button, Is.Not.Null, buttons[i]);
            Assert.That(button.GetComponent<Button>(), Is.Not.Null, buttons[i]);
            Assert.That(button.GetComponent<PremiumButtonFx>(), Is.Not.Null, buttons[i]);
        }

        Assert.That(prefab.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(5),
            "The compact editor must not grow back into a nine-button command bar.");
        Assert.That(FindDeep(prefab.transform, "ApplyButton"), Is.Null);
        Assert.That(FindDeep(prefab.transform, "ResetRoomButton"), Is.Null);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }
}
