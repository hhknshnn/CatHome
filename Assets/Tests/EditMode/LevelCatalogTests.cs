using System.Collections.Generic;
using NUnit.Framework;

public sealed class LevelCatalogTests
{
    [Test]
    public void ProgressionConfig_HasStableUniqueChapterAndQuestIds()
    {
        Assert.That(ProgressionConfig.TryGetActive(out ProgressionConfig config), Is.True);

        var levelIds = new HashSet<string>();
        var questIds = new HashSet<string>();

        for (int chapterNumber = 1; chapterNumber <= config.ChapterCount; chapterNumber++)
        {
            LevelDefinition level = config.GetChapter(chapterNumber);
            Assert.That(level, Is.Not.Null);
            Assert.That(level.LevelId, Is.Not.Empty);
            Assert.That(levelIds.Add(level.LevelId), Is.True, $"Duplicate level id: {level.LevelId}");
            Assert.That(config.GetChapterById(level.LevelId), Is.SameAs(level));
            Assert.That(config.TryGetChapterNumber(level.LevelId, out int resolvedNumber), Is.True);
            Assert.That(resolvedNumber, Is.EqualTo(chapterNumber));
            Assert.That(level.ChapterNumber, Is.EqualTo(chapterNumber));
            Assert.That(level.ScenePath, Does.EndWith(".unity"));
            Assert.That(level.SpawnPointId, Is.Not.Empty);

            foreach (QuestDefinition quest in level.Quests)
            {
                Assert.That(quest, Is.Not.Null);
                Assert.That(quest.QuestId, Is.Not.Empty);
                Assert.That(questIds.Add(quest.QuestId), Is.True, $"Duplicate quest id: {quest.QuestId}");
            }
        }
    }

    [Test]
    public void ChapterLookup_IsOrderedAndIndependentFromPlayerLevel()
    {
        Assert.That(ProgressionConfig.TryGetActive(out ProgressionConfig config), Is.True);

        Assert.That(config.GetChapter(0), Is.Null);
        Assert.That(config.GetChapter(1).LevelId, Is.EqualTo("first-meals"));
        Assert.That(config.GetChapter(2).LevelId, Is.EqualTo("sweet-dreams"));
        Assert.That(config.GetChapter(3).LevelId, Is.EqualTo("best-friends"));
        Assert.That(config.GetChapter(4), Is.Null);
    }
}
