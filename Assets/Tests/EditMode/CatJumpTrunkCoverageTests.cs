using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// EditMode preview scenes only. No catalog build/save, scene save, clip changes,
// new shell/tolerance, or gameplay rig is used to make these checks pass.
public sealed class CatJumpTrunkCoverageTests
{
    static readonly MethodInfo Legacy = typeof(CatPawReachBuilder).GetMethod("Region", BindingFlags.NonPublic | BindingFlags.Static);
    static readonly string[] SourceGuards = {
        "Assets/Editor/CatPawReachBuilder.cs", "Assets/Resources/Home/CatJumpClearanceCatalog.asset",
        "Assets/Resources/Home/CatBodyGuardCatalog.asset", "Assets/Scripts/Activities/CatJumpMotion.cs"
    };
    static float Weight(BoneWeight w, Transform[] bones, Func<string,bool> match)
    {
        int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};
        float[] weights={w.weight0,w.weight1,w.weight2,w.weight3};float sum=0;
        for(int i=0;i<4;i++)if(weights[i]>0&&ids[i]>=0&&ids[i]<bones.Length&&bones[ids[i]]!=null&&match(bones[ids[i]].name))sum+=weights[i];
        return sum;
    }
    static bool Helper(string n)=>n=="DEF-pelvis.C"||n=="DEF-pelvis.L"||n=="DEF-pelvis.R"||n=="DEF-belly.C";
    static bool Spine(string n)=>n=="DEF-spine"||n=="DEF-spine.001"||n=="DEF-spine.002"||n=="DEF-spine.003"||n=="DEF-spine.004"||n=="DEF-spine.005";
    static string SceneSetup()=>string.Join("|",EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+":"+s.isLoaded+":"+s.isActive));

    [Test]
    public void TenCurrentMeshes_CoverTrunkHelpers_PreserveOldRegions_ExcludeDistalPaws()
    {
        Assert.That(Legacy,Is.Not.Null);
        string setup=SceneSetup();var preview=EditorSceneManager.NewPreviewScene();
        var catalog=CatBreedCatalog.Load();var report=new StringBuilder("breed,oldPelvis,oldChest,newPelvis,newChest,newlyCovered,preserved,excludedDistal\n");
        int breeds=0;
        try
        {
            foreach(var breed in catalog.Entries)
            {
                var visual=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null,"QA jump trunk weights");
                SceneManager.MoveGameObjectToScene(visual,preview);
                try
                {
                    var skin=visual.GetComponentInChildren<SkinnedMeshRenderer>();var bones=skin.bones;var weights=skin.sharedMesh.boneWeights;
                    int[] oldCounts=new int[4],newCounts=new int[4];int added=0,distal=0;
                    foreach(int index in breed.ContactVertexIndices)
                    {
                        var w=weights[index];int old=(int)Legacy.Invoke(null,new object[]{w,bones});
                        int next=CatJumpClearanceBuilder.ClassifyBodyRegion(w,bones);
                        if(old>=0){oldCounts[old]++;Assert.That(next,Is.EqualTo(old),breed.Id+" reassigned old vertex "+index);}
                        if(next>=0)newCounts[next]++;
                        if(old<0&&next>=0){added++;Assert.That(Weight(w,bones,Helper),Is.GreaterThan(0),breed.Id+" unmotivated expansion "+index);}
                        if(Weight(w,bones,n=>Helper(n)||Spine(n))>=.5f)
                            Assert.That(next,Is.GreaterThanOrEqualTo(0),breed.Id+" omitted actual trunk vertex "+index);
                        if(Weight(w,bones,n=>n.StartsWith("DEF-f_toe.",StringComparison.Ordinal)||n.StartsWith("DEF-r_toe.",StringComparison.Ordinal))>.99f)
                        {distal++;Assert.That(next,Is.EqualTo(-1),breed.Id+" distal toe incorrectly classified as torso "+index);}
                    }
                    Assert.That(added,Is.GreaterThan(0),breed.Id);Assert.That(distal,Is.GreaterThan(0),breed.Id);
                    Assert.That(newCounts[2],Is.EqualTo(oldCounts[2]),breed.Id+" neck changed");
                    Assert.That(newCounts[3],Is.EqualTo(oldCounts[3]),breed.Id+" head changed");
                    foreach(string name in new[]{"DEF-pelvis.C","DEF-pelvis.L","DEF-pelvis.R","DEF-belly.C"})
                    {
                        int b=Array.FindIndex(bones,t=>t!=null&&t.name==name);Assert.That(b,Is.GreaterThanOrEqualTo(0),breed.Id+" "+name);
                        var unit=new BoneWeight{boneIndex0=b,weight0=1};
                        Assert.That(CatJumpClearanceBuilder.ClassifyBodyRegion(unit,bones),Is.EqualTo(name=="DEF-belly.C"?1:0));
                    }
                    if(breed.Id=="oriental-shorthair")
                    {
                        Assert.That(breed.ContactVertexIndices.Contains(447),Is.True);
                        Assert.That(Weight(weights[447],bones,n=>n=="DEF-pelvis.C"),Is.GreaterThan(.67f),"The reported offending vertex must still be the measured pelvis vertex.");
                        Assert.That((int)Legacy.Invoke(null,new object[]{weights[447],bones}),Is.EqualTo(-1));
                        Assert.That(CatJumpClearanceBuilder.ClassifyBodyRegion(weights[447],bones),Is.EqualTo(0));
                    }
                    report.AppendLine(string.Join(",",breed.Id,oldCounts[0],oldCounts[1],newCounts[0],newCounts[1],added,true,distal));breeds++;
                }
                finally{Object.DestroyImmediate(visual);}
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        Assert.That(breeds,Is.EqualTo(10));Assert.That(SceneSetup(),Is.EqualTo(setup));TestContext.WriteLine(report.ToString());
    }

    [Test]
    public void InstalledCatalog_AllNativeEndSamples_ContainExpandedTrunkAndPreserveNeckHeadFits()
    {
        string setup=SceneSetup();var bytes=SourceGuards.ToDictionary(p=>p,File.ReadAllBytes);
        string report;
        try{report=CatJumpClearanceBuilder.MeasureCoverage();}
        finally
        {
            foreach(var pair in bytes)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),"Measurement modified "+pair.Key);
            Assert.That(SceneSetup(),Is.EqualTo(setup));
        }
        var rows=report.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(s=>s.Split(',')).ToArray();
        var expectedPhases=Enumerable.Range(0,97).Select(i=>i/96f).Concat(new[]{CatJumpMotion.Takeoff,CatJumpMotion.Touchdown})
            .Where(p=>p<=CatJumpMotion.Takeoff||p>=CatJumpMotion.Touchdown).Distinct().Count();
        Assert.That(rows.Length,Is.EqualTo(10*4*expectedPhases));
        float Number(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
        foreach(var row in rows)
        {
            string id=row[0]+" phase="+row[1]+" "+row[2];
            Assert.That(Number(row[11]),Is.GreaterThanOrEqualTo(.00199f),id+" recomputed skin containment");
            Assert.That(Number(row[12]),Is.GreaterThanOrEqualTo(.00199f),id+" installed catalog still omits source skin; rebake the jump-only catalog");
            if(row[2]=="neck"||row[2]=="head")
            {
                Assert.That(row[4],Is.EqualTo(row[3]),id+" changed coverage");
                Assert.That(row[6],Is.EqualTo(row[5]),id+" changed capsule radius");
                Assert.That(row[9],Is.EqualTo(row[7]),id+" changed capsule start");
                Assert.That(row[10],Is.EqualTo(row[8]),id+" changed capsule end");
            }
        }
        TestContext.WriteLine(report);
    }
}
