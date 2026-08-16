using NUnit.Framework;
using UnityEngine;

public sealed class PremiumWorldAmbientFxTests
{
    [Test]
    public void AmbientFx_TracksAuthoredSparklesAndSupportsReducedMotion()
    {
        GameObject root = new GameObject("PremiumWorldAmbientFx_Test");
        try
        {
            Transform first = new GameObject("First").transform;
            Transform second = new GameObject("Second").transform;
            first.SetParent(root.transform, false);
            second.SetParent(root.transform, false);

            PremiumWorldAmbientFx fx = root.AddComponent<PremiumWorldAmbientFx>();
            fx.EditorConfigure(new[] { first, second }, 0.08f, 7f, 0.02f, 1.1f);

            Assert.That(fx.SparkleCount, Is.EqualTo(2));
            Assert.That(fx.ReducedMotion, Is.False);
            fx.SetReducedMotion(true);
            Assert.That(fx.ReducedMotion, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
