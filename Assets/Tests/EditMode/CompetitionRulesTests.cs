using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CompetitionRulesTests
{
    [TestCase(CompetitionGame.CatRunner, CompetitionPeriod.Daily, "cat-runner-daily")]
    [TestCase(CompetitionGame.CatRunner, CompetitionPeriod.Weekly, "cat-runner-weekly")]
    [TestCase(CompetitionGame.CatRunner, CompetitionPeriod.AllTime, "cat-runner-all-time")]
    [TestCase(CompetitionGame.CatCatch, CompetitionPeriod.Daily, "cat-catch-daily")]
    [TestCase(CompetitionGame.CatCatch, CompetitionPeriod.Weekly, "cat-catch-weekly")]
    [TestCase(CompetitionGame.CatCatch, CompetitionPeriod.AllTime, "cat-catch-all-time")]
    public void BoardId_IsStable(
        CompetitionGame game,
        CompetitionPeriod period,
        string expected)
    {
        Assert.That(CompetitionRules.BoardId(game, period), Is.EqualTo(expected));
    }

    [Test]
    public void Nickname_UsesOnlySafeDisplayCharacters()
    {
        Assert.That(
            CompetitionRules.TrySanitizeNickname("  Cozy <Cat>   42  ", out string safe),
            Is.True);
        Assert.That(safe, Is.EqualTo("Cozy Cat 42"));
        Assert.That(safe, Does.Not.Contain("<"));
    }

    [TestCase("")]
    [TestCase("A")]
    [TestCase("  -  ")]
    public void Nickname_RejectsTooShortValues(string source)
    {
        Assert.That(CompetitionRules.TrySanitizeNickname(source, out _), Is.False);
    }

    [Test]
    public void CompetitionNickname_UsesTheCanonicalCatName()
    {
        bool hadName = PlayerPrefs.HasKey(PetTutorialHint.CatNameKey);
        string previousName = PlayerPrefs.GetString(PetTutorialHint.CatNameKey, string.Empty);
        try
        {
            CatIdentityService.CatName = "Lokiş";

            Assert.That(CompetitionService.Nickname, Is.EqualTo("Lokiş"));
        }
        finally
        {
            if (hadName)
                PlayerPrefs.SetString(PetTutorialHint.CatNameKey, previousName);
            else
                PlayerPrefs.DeleteKey(PetTutorialHint.CatNameKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void GlobalLeaderboard_KeepsCompleteRankingAndNoSecondNameInput()
    {
        const string uiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
        Scene scene = SceneManager.GetSceneByPath(uiScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(uiScenePath, OpenSceneMode.Additive);

        try
        {
            LeaderboardPanel panel = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                panel = root.GetComponentInChildren<LeaderboardPanel>(true);
                if (panel != null)
                    break;
            }

            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.GetComponentInChildren<TMPro.TMP_InputField>(true), Is.Null,
                "The global board must use the canonical cat name without a second input.");
            var serialized = new SerializedObject(panel);
            Assert.That(serialized.FindProperty("emptyStateText").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("ownRankText").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("rankRows").arraySize,
                Is.EqualTo(CompetitionRules.MaximumVisibleEntries));
            Assert.That(panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true), Is.Not.Null);
        }
        finally
        {
            if (opened && scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void RunnerCanonicalScore_MatchesRuntimeFormula()
    {
        Assert.That(CompetitionRules.RunnerCanonicalScore(321.4f, 12, 80), Is.EqualTo(521));
    }

    [Test]
    public void CatchCanonicalScore_UsesBoundedComboSteps()
    {
        Assert.That(CompetitionRules.CatchCanonicalScore(4, 7), Is.EqualTo(620));
        Assert.That(CompetitionRules.CatchCanonicalScore(2, 99), Is.EqualTo(440));
    }
}
