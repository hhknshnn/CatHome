using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class RunnerBoulevardTests
{
    [Test] public void BuiltStreet_ReplacesLegacySceneryAndRetainsEveryOwnedThemeSlot()
    {
        var scene=EditorSceneManager.OpenScene(CatRunnerLauncher.RunnerScenePath,OpenSceneMode.Additive);
        try
        {
            var segments=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatRunnerScenerySegment>(true)).ToArray();
            Assert.That(segments.Length,Is.EqualTo(10));
            foreach(var s in segments)
            {
                Assert.That(s.transform.Find("ArcadeStreet"),Is.Not.Null);
                Assert.That(s.transform.Find("CandyBoulevard").gameObject.activeSelf,Is.False);
                foreach(var name in CatRunnerContentBuilder.SceneryVariantNames)
                {
                    var v=s.transform.Find(name);Assert.That(v,Is.Not.Null);
                    var active=v.Cast<Transform>().Where(t=>t.gameObject.activeSelf).ToArray();
                    Assert.That(active.Length,Is.EqualTo(1),s.name+" "+name);
                    Assert.That(active[0].name,Does.StartWith("BoulevardLandmark"));
                }
                Assert.That(s.transform.Find("NearScenery").Cast<Transform>().Count(t=>t.gameObject.activeSelf&&t.name.StartsWith("ArcadeFacade")),Is.EqualTo(4));
            }
        }
        finally{EditorSceneManager.CloseScene(scene,true);}
    }
    [Test] public void CompletedMissionCopy_UsesAvailableBodyGlyphsInBothLanguages()
    {
        var now=DateTime.UtcNow;var saved=CatRunnerProgressService.CaptureState(now);var language=GameLanguageService.Current;
        try
        {
            var state=CatRunnerProgressSaveState.CreateDefault(DateTime.UtcNow);
            state.dailyCoins=50;state.dailyJumps=10;state.dailyDistance=500;
            state.dailyCoinsClaimed=state.dailyJumpsClaimed=state.dailyDistanceClaimed=true;
            CatRunnerProgressService.ApplySavedState(state,now);
            foreach(var value in new[]{GameLanguage.Turkish,GameLanguage.English})
            {
                GameLanguageService.SetLanguage(value);
                var copy=CatRunnerProgressService.GetDailyMissionSummary();
                foreach(char c in copy.Where(c=>!char.IsWhiteSpace(c)))
                    Assert.That(PremiumTypography.Body.HasCharacter(c,false,false),Is.True,"Missing "+c+" in "+value);
            }
        }
        finally{CatRunnerProgressService.ApplySavedState(saved,now);GameLanguageService.SetLanguage(language);}
    }
}
