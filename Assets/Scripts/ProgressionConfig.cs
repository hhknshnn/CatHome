using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data-driven quest chapter definitions. Loaded from
/// Resources/ProgressionConfig with the same pattern as GameBalanceConfig.
/// Create or update the asset via Tools > Cat Home >
/// Create or Update Progression Config.
/// </summary>
[CreateAssetMenu(
    fileName = "ProgressionConfig",
    menuName = "Cat Home/Progression Config"
)]
public sealed class ProgressionConfig : ScriptableObject
{
    private const string ResourcesPath = "ProgressionConfig";
    private static bool missingConfigWarningLogged;

    [SerializeField] private LevelDefinition[] levels = Array.Empty<LevelDefinition>();

    public int ChapterCount => levels != null ? levels.Length : 0;
    public IReadOnlyList<LevelDefinition> Levels =>
        levels ?? (IReadOnlyList<LevelDefinition>)Array.Empty<LevelDefinition>();

    /// <summary>Returns the definition for a 1-based chapter number, or null.</summary>
    public LevelDefinition GetChapter(int chapterNumber)
    {
        if (levels == null || chapterNumber < 1 || chapterNumber > levels.Length)
            return null;

        return levels[chapterNumber - 1];
    }

    public LevelDefinition GetChapterById(string chapterId)
    {
        if (levels == null || string.IsNullOrWhiteSpace(chapterId))
            return null;

        for (int i = 0; i < levels.Length; i++)
        {
            LevelDefinition level = levels[i];
            if (level != null && string.Equals(
                    level.LevelId,
                    chapterId,
                    StringComparison.Ordinal))
            {
                return level;
            }
        }

        return null;
    }

    public bool TryGetChapterNumber(string chapterId, out int chapterNumber)
    {
        if (levels != null && !string.IsNullOrWhiteSpace(chapterId))
        {
            for (int i = 0; i < levels.Length; i++)
            {
                LevelDefinition level = levels[i];
                if (level != null && string.Equals(
                        level.LevelId,
                        chapterId,
                        StringComparison.Ordinal))
                {
                    chapterNumber = i + 1;
                    return true;
                }
            }
        }

        chapterNumber = 0;
        return false;
    }

    public static bool TryGetActive(out ProgressionConfig config)
    {
        config = Resources.Load<ProgressionConfig>(ResourcesPath);
        if (config != null)
            return true;

        if (!missingConfigWarningLogged)
        {
            Debug.LogWarning(
                "ProgressionConfig: Resources/ProgressionConfig could not be loaded. " +
                "Quest progression is disabled until the asset is created via " +
                "Tools > Cat Home > Create or Update Progression Config."
            );
            missingConfigWarningLogged = true;
        }

        return false;
    }

#if UNITY_EDITOR
    /// <summary>Editor-only setter used by the ProgressionConfig builder.</summary>
    public void EditorSetLevels(LevelDefinition[] newLevels)
    {
        levels = newLevels ?? Array.Empty<LevelDefinition>();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        missingConfigWarningLogged = false;
    }
}
