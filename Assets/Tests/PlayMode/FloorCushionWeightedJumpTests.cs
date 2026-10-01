#if UNITY_EDITOR
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class FloorCushionWeightedJumpTests
{
    PreparedInteractionStartTests fixture;
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    [SetUp]public void Before(){fixture=new PreparedInteractionStartTests();fixture.Before();fixture.CaptureSkinEvidence=true;}
    [UnityTearDown]public IEnumerator After()
    {
        if(File.Exists(Root+"/prepared-full-descent-regression.csv"))File.Copy(Root+"/prepared-full-descent-regression.csv",Root+"/floor-cushion-weighted-jump-cycle.csv",true);
        fixture?.After();yield return RoomPlayModeSupport.WaitForPendingContactData();
    }
    [UnityTest,Timeout(100000)]
    public IEnumerator FloorCushion_RealAcceptedArrivalAndDeparture_CompleteOriginalSourceWithFiveMillimetreSkinLimit()
    {
        var entry=CatJumpClearanceCatalog.Load().Find("oriental-shorthair");
        Assert.That(entry.weightedBodyVersion,Is.EqualTo(CatJumpClearanceCatalog.WeightedBodyVersion));
        Assert.That(entry.samples.All(s=>CatJumpWeightedBody.HasData(entry,s)),Is.True,"New weighted gate must actually be installed.");
        var method=typeof(PreparedInteractionStartTests).GetMethod("CompleteFullCycles",BindingFlags.Instance|BindingFlags.NonPublic);
        yield return (IEnumerator)method.Invoke(fixture,new object[]{new[]{"loft.floor-cushions"},false});
        // The shared fixture retains actual prompt/click, original NativeJump,
        // root continuity, completion, landing/controller and control assertions.
        var rows=File.ReadAllLines(Root+"/prepared-full-descent-regression.csv").Skip(1).ToArray();
        Assert.That(rows.Length,Is.EqualTo(1));var fields=rows[0].Split(',');
        Assert.That(fields[1],Is.EqualTo("loft.floor-cushions"));Assert.That(fields[3],Is.EqualTo("1"));
        Assert.That(float.Parse(fields[6],CultureInfo.InvariantCulture),Is.LessThanOrEqualTo(.005f),"Triangle readiness does not exempt actual supported skin from the existing 5 mm requirement.");
    }
}
#endif
