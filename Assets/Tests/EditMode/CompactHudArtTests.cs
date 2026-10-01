using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CompactHudArtTests
{
    [Test] public void TitleShortcuts_KeepApprovedIconsAndSquarePreviews()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/TitleScreen.prefab");
        foreach(string name in new[]{"ShopShortcut","RoomsShortcut","GamesShortcut"})
        {
            var root=prefab.GetComponentsInChildren<Button>(true).First(b=>b.name==name);
            var art=root.GetComponentsInChildren<RawImage>(true).First(a=>a.name=="Preview");
            Assert.That(art.texture,Is.Not.Null);Assert.That(art.raycastTarget,Is.False);
            Assert.That(root.GetComponentsInChildren<RawImage>(true).Any(a=>a.name=="ShortcutBackdropArt"&&a.gameObject.activeSelf),Is.False);
            var preview=root.GetComponentsInChildren<RectTransform>(true).First(r=>r.name=="Preview");
            Assert.That(preview.sizeDelta.x,Is.EqualTo(preview.sizeDelta.y));
        }
    }
    [Test] public void HomeHud_ApprovedGroupKeepsBondSeparateAndDockArtInsideButtons()
    {
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=SceneManager.GetSceneByPath(CatHomeAuthoringWorkspace.UiScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(CatHomeAuthoringWorkspace.UiScenePath,OpenSceneMode.Additive);
            var rects=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).ToArray();
            var identity=rects.First(r=>r.name=="CatShopButton");var bond=rects.First(r=>r.name=="BondXpEntry");
            var layout=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StorybookHudLayout>(true)).Single();
            layout.Refresh();
            Assert.That(identity.sizeDelta,Is.EqualTo(new Vector2(260,88)));
            var band=rects.Single(r=>r.name=="StorybookNeedsBand");
            foreach(string name in new[]{"FoodBar","ThirstUI","EnergyUI","CoinEntry","DiamondEntry","MenuButton"})
                Assert.That(rects.First(r=>r.name==name).rect.height,Is.EqualTo(band.rect.height).Within(.1f),
                    name+" must share the compact upper HUD height.");
            var identityCorners=new Vector3[4];var bondCorners=new Vector3[4];identity.GetWorldCorners(identityCorners);bond.GetWorldCorners(bondCorners);
            Assert.That(bondCorners[1].y,Is.LessThan(identityCorners[0].y),"Bond XP has its own row below the identity, not another spendable currency.");
            Assert.That(rects.Single(r=>r.name=="StorybookNeedsBand").GetComponent<LowPolyPanelGraphic>().raycastTarget,Is.False);
            var dock=rects.First(r=>r.name=="HomeDock");
            foreach(var b in dock.GetComponentsInChildren<Button>(true))
            {
                var icon=b.GetComponentsInChildren<RawImage>(true).First(a=>a.name=="StorybookDockIcon");
                Assert.That(icon.texture,Is.Not.Null);Assert.That(icon.rectTransform.anchoredPosition.y,Is.Zero);
                var r=icon.rectTransform;var parent=b.targetGraphic.rectTransform;
                Assert.That(Mathf.Abs(r.anchoredPosition.x)+r.sizeDelta.x/2,Is.LessThan(parent.rect.width/2-8));
                Assert.That(Mathf.Abs(r.anchoredPosition.y)+r.sizeDelta.y/2,Is.LessThanOrEqualTo(parent.rect.height/2),
                    "Dock artwork must remain within the compact button face.");
                Assert.That(icon.raycastTarget,Is.False,"Dock artwork must leave input to its button.");
                Assert.That(b.targetGraphic.raycastTarget,Is.True,"The existing button remains the input owner.");
            }
        }
        finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
    }
}
