using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CompactHudArtTests
{
    [Test] public void TitleShortcuts_KeepIllustrationBackgroundsAndSquarePreviews()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/TitleScreen.prefab");
        foreach(string name in new[]{"ShopShortcut","RoomsShortcut","GamesShortcut"})
        {
            var root=prefab.GetComponentsInChildren<Button>(true).First(b=>b.name==name);
            var art=root.GetComponentsInChildren<RawImage>(true).First(a=>a.name=="ShortcutBackdropArt");
            Assert.That(art.texture,Is.Not.Null);Assert.That(art.raycastTarget,Is.False);
            var preview=root.GetComponentsInChildren<RectTransform>(true).First(r=>r.name=="Preview");
            Assert.That(preview.sizeDelta.x,Is.EqualTo(preview.sizeDelta.y));
        }
    }
    [Test] public void HomeIdentityAndBond_ShareTopRow_AndDockArtFitsItsButton()
    {
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=SceneManager.GetSceneByPath(CatHomeAuthoringWorkspace.UiScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(CatHomeAuthoringWorkspace.UiScenePath,OpenSceneMode.Additive);
            var rects=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).ToArray();
            var identity=rects.First(r=>r.name=="CatShopButton");var bond=rects.First(r=>r.name=="BondXpEntry");
            Assert.That(identity.sizeDelta,Is.EqualTo(new Vector2(260,88)));
            Assert.That(bond.anchoredPosition.x-(identity.anchoredPosition.x+identity.sizeDelta.x),Is.GreaterThanOrEqualTo(16));
            Assert.That(Mathf.Abs(identity.anchoredPosition.y-identity.sizeDelta.y/2-(bond.anchoredPosition.y-bond.sizeDelta.y/2)),Is.LessThan(1));
            var dock=rects.First(r=>r.name=="HomeDock");
            foreach(var b in dock.GetComponentsInChildren<Button>(true))
            {
                var icon=b.GetComponentsInChildren<RawImage>(true).First(a=>a.name.StartsWith("Sculpted"));
                Assert.That(icon.texture,Is.Not.Null);Assert.That(icon.rectTransform.anchoredPosition.y,Is.Zero);
                var r=icon.rectTransform;var parent=b.targetGraphic.rectTransform;
                Assert.That(Mathf.Abs(r.anchoredPosition.x)+r.sizeDelta.x/2,Is.LessThan(parent.rect.width/2-8));
                Assert.That(r.sizeDelta.y,Is.LessThan(parent.rect.height-16));
                Assert.That(b.GetComponentsInChildren<RawImage>(true).Any(a=>a.name=="BackdropMotif"&&a.texture!=null),Is.True);
            }
        }
        finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
    }
}
