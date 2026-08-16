using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// One quest chapter inside the ProgressionConfig asset. A chapter is completed
/// when every quest inside it is claimed. Chapters organize quest content; they
/// are not a player resource and never unlock rooms or gameplay objects.
/// </summary>
[Serializable]
public sealed class LevelDefinition
{
    [Tooltip("Stable unique id. Save data and scene transitions use this value.")]
    [SerializeField] private string levelId;
    [SerializeField] private string levelName;
    [Min(1)]
    [FormerlySerializedAs("requiredPlayerLevel")]
    [SerializeField] private int chapterNumber = 1;
    [Tooltip("Project-relative path of the additive gameplay scene.")]
    [SerializeField] private string scenePath;
    [Tooltip("Stable spawn marker id inside the gameplay scene.")]
    [SerializeField] private string spawnPointId = "default";
    [SerializeField] private QuestDefinition[] quests = Array.Empty<QuestDefinition>();

    public LevelDefinition()
    {
    }

    public LevelDefinition(string levelName, QuestDefinition[] quests)
        : this(levelName, levelName, 1, string.Empty, "default", quests)
    {
    }

    public LevelDefinition(
        string levelId,
        string levelName,
        int chapterNumber,
        string scenePath,
        string spawnPointId,
        QuestDefinition[] quests)
    {
        this.levelId = levelId;
        this.levelName = levelName;
        this.chapterNumber = Mathf.Max(1, chapterNumber);
        this.scenePath = scenePath;
        this.spawnPointId = string.IsNullOrWhiteSpace(spawnPointId)
            ? "default"
            : spawnPointId;
        this.quests = quests ?? Array.Empty<QuestDefinition>();
    }

    public string LevelId => levelId;
    public string LevelName => levelName;
    public int ChapterNumber => Mathf.Max(1, chapterNumber);
    public string ScenePath => scenePath;
    public string SpawnPointId => string.IsNullOrWhiteSpace(spawnPointId)
        ? "default"
        : spawnPointId;
    public IReadOnlyList<QuestDefinition> Quests =>
        quests ?? (IReadOnlyList<QuestDefinition>)Array.Empty<QuestDefinition>();
}
