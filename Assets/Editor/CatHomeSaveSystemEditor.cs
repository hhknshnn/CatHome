using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CatHomeSaveSystemEditor
{
    private const string ResetMenuPath = "Tools/Cat Home/Reset All Game Progress";
    private const string ResetProgressionMenuPath = "Tools/Cat Home/Reset Progression Test Data";
    private static readonly string[] CatHomePlayerPrefsKeys =
    {
        PetTutorialHint.PlayerPrefsKey,
        PetTutorialHint.CatNameKey,
        PetTutorialHint.IntroductionCompletedKey,
        PetTutorialHint.IntroductionStepKey,
        PetTutorialHint.OnboardingStepKey,
        PetTutorialHint.OnboardingCompletedKey
    };

    static CatHomeSaveSystemEditor()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/Cat Home/Reset Cat Name")]
    private static void ResetCatName()
    {
        PetTutorialHint.ClearCatName();
        Debug.Log("Cat Home cat name was reset. Onboarding progress was preserved.");
    }

    [MenuItem(ResetProgressionMenuPath)]
    private static void ResetProgressionTestData()
    {
        // Belt-and-suspenders: the validate function already greys the menu out
        // during Play Mode, and the save API refuses to run while playing.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning(
                "Reset Progression Test Data is disabled during Play Mode. " +
                "Stop Play Mode and try again."
            );
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Reset Progression Test Data",
            "Reset only the progression portion of the local save?\n\n" +
            "This sets player level to 1 and clears coins, diamonds, bond XP and " +
            "quest progress.\n\n" +
            "Hunger, thirst, energy, cat position, sleep state, onboarding, pet " +
            "tutorial and offline-progress data are preserved.",
            "Reset Progression",
            "Cancel"
        );
        if (!confirmed)
            return;

        if (CatHomeSaveSystem.EditorResetProgressionData(out string report))
            Debug.Log(report);
        else
            Debug.LogWarning(report);
    }

    [MenuItem(ResetProgressionMenuPath, true)]
    private static bool ValidateResetProgressionTestData()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem(ResetMenuPath)]
    private static void ResetAllGameProgress()
    {
        string savePath = CatHomeSaveSystem.SaveFilePath;
        bool confirmed = EditorUtility.DisplayDialog(
            "Reset All Game Progress",
            "Reset Cat Home's local save, known Cat Home preferences, and tutorial progress?\n\n" +
            "Unity and unrelated PlayerPrefs data will not be touched.",
            "Reset",
            "Cancel"
        );
        if (!confirmed)
            return;

        var cleared = new System.Collections.Generic.List<string>();
        try
        {
            DeleteSaveFamily(savePath, cleared);
            foreach (string key in CatHomePlayerPrefsKeys)
            {
                if (!PlayerPrefs.HasKey(key))
                    continue;

                PlayerPrefs.DeleteKey(key);
                cleared.Add($"PlayerPrefs:{key}");
            }
            PlayerPrefs.Save();

            string details = cleared.Count == 0
                ? "no existing Cat Home records were present"
                : string.Join(", ", cleared);
            Debug.Log(
                "Cat Home progress reset complete: " + details + ". " +
                "The next Play session will start at 100/100/100, awake at the scene start pose, " +
                "without an offline-return popup, and with onboarding eligible to restart."
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Cat Home progress reset did not complete: {exception.Message}"
            );
        }
    }

    [MenuItem(ResetMenuPath, true)]
    private static bool ValidateResetAllGameProgress()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void DeleteSaveFamily(
        string savePath,
        System.Collections.Generic.List<string> cleared)
    {
        string directory = Path.GetDirectoryName(savePath);
        string fileName = Path.GetFileName(savePath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return;

        foreach (string path in Directory.EnumerateFiles(directory, fileName + "*"))
        {
            string candidateName = Path.GetFileName(path);
            bool belongsToSaveFamily =
                candidateName == fileName ||
                candidateName == fileName + ".tmp" ||
                candidateName == fileName + ".previous" ||
                candidateName == fileName + CatHomeSaveSystem.RecoveryFileSuffix ||
                candidateName.StartsWith(
                    fileName + CatHomeSaveSystem.NewGameBackupMarker,
                    StringComparison.Ordinal) ||
                candidateName.StartsWith(fileName + ".corrupt-", StringComparison.Ordinal) ||
                candidateName.StartsWith(fileName + ".incompatible-", StringComparison.Ordinal);
            if (!belongsToSaveFamily)
                continue;

            File.Delete(path);
            cleared.Add(candidateName);
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            CatHomeSaveSystem.SaveNow();
    }
}
