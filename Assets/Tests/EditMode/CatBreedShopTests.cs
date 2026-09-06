using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class CatBreedShopTests
{
    [Test]
    public void Catalog_ContainsTenCompleteUniquePackageCats()
    {
        CatBreedCatalog catalog = AssetDatabase.LoadAssetAtPath<CatBreedCatalog>(
            PolyperfectCatIntegrationBuilder.CatalogPath);
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.GameplayController, Is.Not.Null);
        Assert.That(catalog.Count, Is.EqualTo(10));

        var ids = new HashSet<string>();
        for (int i = 0; i < catalog.Count; i++)
        {
            CatBreedCatalog.Entry entry = catalog.Get(i);
            Assert.That(entry, Is.Not.Null);
            Assert.That(ids.Add(entry.Id), Is.True, "Duplicate breed id: " + entry.Id);
            Assert.That(entry.DisplayName, Is.Not.Empty);
            Assert.That(entry.SourcePrefab, Is.Not.Null, entry.Id + " has no model.");
            Assert.That(entry.Portrait, Is.Not.Null, entry.Id + " has no real portrait.");
        }
    }

    [Test]
    public void MainPanel_UsesCatShopButtonInRemovedLightSlot()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/UI/MainPanel.prefab");
        Assert.That(prefab, Is.Not.Null);
        Transform catShop = Find(prefab.transform, "CatShopButton");
        Transform generalShop = Find(prefab.transform, "ShopButton");
        Transform menu = Find(prefab.transform, "MenuButton");
        Assert.That(catShop, Is.Not.Null);
        Assert.That(generalShop, Is.Not.Null);
        Assert.That(menu, Is.Not.Null);
        Assert.That(Find(prefab.transform, "SelectedCatFace"), Is.Not.Null);
        Assert.That(Find(prefab.transform, "LightButton"), Is.Null);
        Assert.That(Find(prefab.transform, "ClockDisplay"), Is.Null);
        Assert.That(prefab.GetComponent<BrightnessPanelView>(), Is.Null);

        Assert.That(generalShop.gameObject.activeSelf, Is.False, "The general shop belongs in the dock.");
        RectTransform catRect=(RectTransform)catShop;
        RectTransform menuRect=(RectTransform)menu;
        Assert.That(catRect.anchorMin.x, Is.EqualTo(0));
        Assert.That(menuRect.anchorMin.x, Is.EqualTo(1));
        Assert.That(catShop.GetComponentInChildren<HomeLevelBadgeLabel>(true), Is.Not.Null);

    }

    [Test]
    public void CatShopPrefab_HasTenRealCardsAndOneTurntable()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            CatBreedShopPanelBuilder.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponents<CatBreedShopPanel>().Length, Is.EqualTo(1));
        Assert.That(prefab.GetComponentsInChildren<CatBreedTurntablePreview>(true).Length,
            Is.EqualTo(1));

        int cards = 0;
        foreach (Button button in prefab.GetComponentsInChildren<Button>(true))
        {
            if (!button.name.StartsWith("BreedCard_"))
                continue;
            cards++;
            Assert.That(button.interactable, Is.True);
            Transform portrait = Find(button.transform, "RealCatPortrait");
            Assert.That(portrait, Is.Not.Null);
            Assert.That(portrait.GetComponent<Image>().sprite, Is.Not.Null);
        }
        Assert.That(cards, Is.EqualTo(10));
    }

    [Test]
    public void EveryBreed_CreatesGameplayReadyAnimatorVisual()
    {
        CatBreedCatalog catalog = AssetDatabase.LoadAssetAtPath<CatBreedCatalog>(
            PolyperfectCatIntegrationBuilder.CatalogPath);
        Assert.That(catalog, Is.Not.Null);
        var host = new GameObject("BreedFactoryTestHost");
        try
        {
            for (int i = 0; i < catalog.Count; i++)
            {
                GameObject visual = CatBreedVisualFactory.Create(
                    catalog.Get(i), catalog.GameplayController, host.transform);
                Assert.That(visual, Is.Not.Null, catalog.Get(i).Id);
                Animator animator = visual.GetComponentInChildren<Animator>(true);
                Assert.That(animator, Is.Not.Null, catalog.Get(i).Id);
                Assert.That(animator.runtimeAnimatorController,
                    Is.SameAs(catalog.GameplayController), catalog.Get(i).Id);
                Assert.That(animator.transform.Find("Cat_Domestic_Shorthair"),
                    Is.Not.Null, catalog.Get(i).Id + " has an incompatible clip binding root.");
                foreach (string state in PolyperfectCatIntegrationBuilder.RequiredStates)
                {
                    Assert.That(animator.HasState(0,
                            Animator.StringToHash("Base Layer." + state)),
                        Is.True, catalog.Get(i).Id + " is missing state " + state + ".");
                }

                Transform animatedBone = CatBreedVisualFactory.FindDescendant(
                    animator.transform, "DEF-upper_arm.L");
                Assert.That(animatedBone, Is.Not.Null, catalog.Get(i).Id);
                animator.Rebind();
                animator.SetFloat("Speed", 1f);
                animator.Play("Idle", 0, 0f);
                animator.Update(0f);
                Quaternion before = animatedBone.localRotation;
                animator.Update(.17f);
                Assert.That(Quaternion.Angle(before, animatedBone.localRotation),
                    Is.GreaterThan(.01f), catalog.Get(i).Id + " did not animate its skeleton.");
                Assert.That(CatBreedVisualFactory.FindDescendant(
                    visual.transform, "HeartSpawn"), Is.Not.Null, catalog.Get(i).Id);
                Object.DestroyImmediate(visual);
            }
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private static Transform Find(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name)
                return child;
        return null;
    }

    private static float VisibleHorizontalGap(RectTransform left, RectTransform right)
    {
        float leftRightEdge = left.anchoredPosition.x + left.rect.width * 0.5f;
        float rightLeftEdge = right.anchoredPosition.x - right.rect.width * 0.5f;
        return rightLeftEdge - leftRightEdge;
    }
}
