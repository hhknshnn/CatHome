using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GardenGentleInteractionAssetTests
{
    [Test]
    public void DaisyAndGrillUseTheirGentleStyles_WithoutChangingOtherMats()
    {
        const string folder = "Assets/Art/StoreProducts/Prefabs/";
        var daisy = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "GardenDaisyBed.prefab")
            .GetComponent<MatKneadActivity>();
        Assert.That(daisy.UsesGentleKneading, Is.True);
        var grill = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "GardenGrill.prefab")
            .GetComponent<SitLookActivity>();
        Assert.That(grill.ReactionKind, Is.EqualTo(SitLookReaction.Sit));
        Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(folder + "BathroomBathMat.prefab")
            .GetComponent<MatKneadActivity>().UsesGentleKneading, Is.True,
            "The later bathroom revision also uses a gentle press.");
        foreach (string name in new[] { "KitchenPawMat", "BedroomPawRug", "PatioStoneRug", "LoftFloorRunner" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + name + ".prefab");
            Assert.That(prefab, Is.Not.Null, name);
            var activity = prefab.GetComponent<MatKneadActivity>();
            Assert.That(activity, Is.Not.Null, name);
            Assert.That(activity.UsesGentleKneading, Is.False, name + " retains its existing routine.");
        }
    }
}
