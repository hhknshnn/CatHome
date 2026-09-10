using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BathroomPaperContactGeometryTests
{
    [Test]
    public void AuthoredPaperContact_LiesOnTheMeasuredCylinder_AndDoesNotSpinWithTheRoll()
    {
        var root = PrefabUtility.LoadPrefabContents("Assets/Art/StoreProducts/Prefabs/BathroomToilet.prefab");
        try
        {
            var paper = root.GetComponent<PaperSpinActivity>();
            Assert.That(paper.UsesPaperTears, Is.True);
            var point = (Transform)typeof(PaperSpinActivity).GetField("paperContactPoint", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(paper);
            Assert.That(point, Is.Not.Null, "The built toilet must bind its measured paper surface anchor.");
            Assert.That(point.IsChildOf(paper.RollPivot), Is.False, "Contact stays on the room-facing side while the cylinder rotates.");
            Vector3 axis = root.transform.InverseTransformPoint(paper.RollPivot.position);
            Vector3 offset = root.transform.InverseTransformPoint(point.position) - axis;
            // Independent model-source contract: Blender's roll R=.115 at
            // Y=.760, width=.180; uniform fitting scales all three together.
            float sourceScale = axis.y / .760f;
            Assert.That(new Vector2(offset.y, offset.z).magnitude, Is.EqualTo(.115f * sourceScale).Within(.003f));
            Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(.09f * sourceScale));
            Assert.That(offset.y, Is.LessThan(0f)); Assert.That(offset.z, Is.GreaterThan(0f));
            Vector3 worldContact = paper.PaperContactPosition;
            paper.RollPivot.localRotation *= Quaternion.Euler(130f, 0f, 0f);
            Assert.That(Vector3.Distance(paper.PaperContactPosition, worldContact), Is.LessThan(.00001f));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [Test]
    public void RecordPlayer_RetainsItsIndependentDiscActivity()
    {
        var root = PrefabUtility.LoadPrefabContents("Assets/Art/StoreProducts/Prefabs/LoftRecordPlayer.prefab");
        try
        {
            var record = root.GetComponent<PaperSpinActivity>();
            Assert.That(record.Kind, Is.EqualTo(CatActivityKind.RecordSpin));
            Assert.That(record.UsesPaperTears, Is.False);
            Assert.That(record.SpinAxis, Is.EqualTo(Vector3.up));
            Assert.That(root.GetComponent<CatPaperTearFx>(), Is.Null);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
