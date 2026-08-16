#if UNITY_EDITOR
using NUnit.Framework;

public sealed class SleepInteractionThresholdTests
{
    [TestCase(100f, true)]
    [TestCase(90f, true)]
    [TestCase(89.9f, false)]
    public void SatisfiedEnergy_UsesInclusiveRuntimeThreshold(float energy, bool expected)
    {
        Assert.That(SleepInteraction.IsSatisfiedEnergy(energy, 90f), Is.EqualTo(expected));
    }
}
#endif
