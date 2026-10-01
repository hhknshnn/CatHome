using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

// Thin observer wrapper: existing full-cycle/start/native-motion checks remain
// authoritative, including the unresolved hanging-chair completion assertion.
public sealed class ReportedSurfaceExactWitnessTests
{
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    PreparedInteractionStartTests fixture;
    DateTime started;
    bool prepared;
    [SetUp] public void Before()
    {
        started=DateTime.UtcNow;fixture=new PreparedInteractionStartTests();
        fixture.Before();prepared=true;fixture.CaptureSkinEvidence=true;
    }
    [TearDown] public void After()
    {
        if(!prepared)return;prepared=false;
        try{fixture.After();}
        finally
        {
            CopyFresh("prepared-full-descent-regression.csv","reported-six-surfaces-cycles.csv");
            CopyFresh("exact-skin-metric-evidence.csv","reported-six-surfaces-exact-witness.csv");
        }
    }
    void CopyFresh(string source,string destination)
    {
        string path=Path.Combine(Root,source);
        if(File.Exists(path)&&File.GetLastWriteTimeUtc(path)>=started)
            File.Copy(path,Path.Combine(Root,destination),true);
    }
    [UnityTest,Timeout(420000)]
    public IEnumerator SixReportedSurfaces_CaptureExactSkinWitnessesAndNativeCompletion()
    {
        var method=typeof(PreparedInteractionStartTests).GetMethod("CompleteFullCycles",BindingFlags.NonPublic|BindingFlags.Instance);
        Assert.That(method,Is.Not.Null);
        var selected=new[]{"bathroom.tub","garden.hammock","balcony.hanging-chair",
            "loft.bean-bag","loft.chaise-lounge","loft.floor-cushions"};
        yield return (IEnumerator)method.Invoke(fixture,new object[]{selected,false});
        // Do not catch an assertion from the existing full-cycle enumerator.
        // A diagnostic output is not evidence that a failed completion passed.
    }
}
