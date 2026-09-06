using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class PremiumUiBehaviorTests
{
    [Test]
    public void PurchaseAndDiamondOverlays_BlockOnlyTheUnderlyingContent()
    {
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ShopPanel.prefab"));
        try
        {
            var controller=root.GetComponent<ShopPanelController>();
            var data=new SerializedObject(controller);
            var purchase=(CanvasGroup)data.FindProperty("purchaseGroup").objectReferenceValue;
            var confirmation=(CanvasGroup)data.FindProperty("diamondConfirmationGroup").objectReferenceValue;
            var content=Find(root.transform,"StoreContent").GetComponent<CanvasGroup>();
            var parent=(CanvasGroup)data.FindProperty("panelGroup").objectReferenceValue;
            parent.alpha=1; parent.interactable=true; parent.blocksRaycasts=true;
            var rootGroup=root.GetComponent<CanvasGroup>();rootGroup.interactable=true;
            purchase.alpha=0;confirmation.alpha=0; Tick(root);
            Assert.That(content.interactable,Is.True);
            purchase.alpha=1;Tick(root);
            Assert.That(content.interactable,Is.False);
            Assert.That(content.blocksRaycasts,Is.False);
            Assert.That(purchase.interactable,Is.True);
            confirmation.alpha=1;Tick(root);
            Assert.That(purchase.interactable,Is.False);
            Assert.That(content.interactable,Is.False);
            confirmation.alpha=0;Tick(root);
            Assert.That(purchase.interactable,Is.True);
            purchase.alpha=0;Tick(root);
            Assert.That(content.interactable,Is.True);
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void DiamondPackages_ShowNoActionablePriceWithoutAProvider()
    {
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ShopPanel.prefab"));
        try
        {
            var store=root.GetComponentInChildren<DiamondStorePanel>(true);
            store.Open(11);
            var packs=new SerializedObject(store).FindProperty("packs");
            Assert.That(packs.arraySize,Is.EqualTo(6));
            for(int i=0;i<packs.arraySize;i++)
            {
                var button=(Button)packs.GetArrayElementAtIndex(i).FindPropertyRelative("button").objectReferenceValue;
                Assert.That(button.interactable,Is.False,"An unknown platform price must never look purchasable.");
            }
            store.Close();
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void CurrencyPlusTargets_AreLargeEnoughToTap()
    {
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/CurrencyHud.prefab");
        int count=0;
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            if(button.name!="PlusButton")continue;
            var rect=(RectTransform)button.transform;
            Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(44));
            Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(44));count++;
        }
        Assert.That(count,Is.EqualTo(3));
    }

    [Test]
    public void ShopBackdrop_IsDrawnBehindItsReadableContent()
    {
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ShopPanel.prefab");
        var content=Find(root.transform,"StoreContent");
        var backdrop=content.Find("Surface");
        Assert.That(backdrop,Is.Not.Null);
        foreach(var child in new[]{"Title","ProductScroll","CloseButton"})
        {
            var item=Find(content,child);
            while(item.parent!=content)item=item.parent;
            Assert.That(item.GetSiblingIndex(),Is.GreaterThan(backdrop.GetSiblingIndex()),child+" is covered by the store backdrop.");
        }
    }

    private static void Tick(GameObject root)
    {
        foreach(var gate in root.GetComponentsInChildren<ModalContentGate>(true))
            typeof(ModalContentGate).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gate,null);
    }
    private static Transform Find(Transform root,string name)
    {
        foreach(var child in root.GetComponentsInChildren<Transform>(true))if(child.name==name)return child;
        Assert.Fail(name+" is missing.");return null;
    }
}
