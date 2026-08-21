#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using CatHome.Economy;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class NewGameAndLocalizationTests
{
    [TestCase(SystemLanguage.Turkish, GameLanguage.Turkish)]
    [TestCase(SystemLanguage.English, GameLanguage.English)]
    [TestCase(SystemLanguage.German, GameLanguage.English)]
    public void InitialLanguage_UsesTurkishOnlyForATurkishDevice(
        SystemLanguage systemLanguage, GameLanguage expected)
    {
        Assert.That(GameLanguageService.ResolveInitialLanguage(systemLanguage), Is.EqualTo(expected));
    }

    [Test]
    public void LanguageSelection_PersistsAndProvidesBothMenuTranslations()
    {
        bool hadValue = PlayerPrefs.HasKey(GameLanguageService.PlayerPrefsKey);
        int oldValue = PlayerPrefs.GetInt(GameLanguageService.PlayerPrefsKey, 0);
        try
        {
            GameLanguageService.SetLanguage(GameLanguage.Turkish);
            Assert.That(PlayerPrefs.GetInt(GameLanguageService.PlayerPrefsKey),
                Is.EqualTo((int)GameLanguage.Turkish));
            Assert.That(GameLanguageService.Text("title.new_game"), Is.EqualTo("YENİ OYUN"));
            Assert.That(GameLanguageService.Text("settings.row.language"), Is.EqualTo("DİL"));

            GameLanguageService.SetLanguage(GameLanguage.English);
            Assert.That(GameLanguageService.Text("title.new_game"), Is.EqualTo("NEW GAME"));
            Assert.That(GameLanguageService.Text("settings.row.language"), Is.EqualTo("LANGUAGE"));
        }
        finally
        {
            if (hadValue)
                PlayerPrefs.SetInt(GameLanguageService.PlayerPrefsKey, oldValue);
            else
                PlayerPrefs.DeleteKey(GameLanguageService.PlayerPrefsKey);
            PlayerPrefs.Save();
            ResetLanguageRuntimeState();
        }
    }

    [Test]
    public void PremiumFont_CanPopulateEveryTurkishCharacter()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            PremiumUiStyle.PremiumFontAssetPath);
        Assert.That(font, Is.Not.Null);
        Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
        foreach (char character in "İŞĞÜÖÇışğüöç")
            Assert.That(font.HasCharacter(character, true, true), Is.True, character.ToString());
    }

    [Test]
    public void NewGameData_ClearsProgressButPreservesPaidValueAndPreferences()
    {
        DateTime now = new DateTime(2026, 8, 21, 8, 36, 0, DateTimeKind.Utc);
        var economy = new EconomySaveState
        {
            economyVersion = EconomyService.SaveVersion,
            balances = new[]
            {
                new CurrencyBalanceEntry(CurrencyCatalog.GetSaveKey(CurrencyType.Coin), 9800L),
                new CurrencyBalanceEntry(CurrencyCatalog.GetSaveKey(CurrencyType.Diamond), 73L)
            },
            processedTransactionIds = new[] { "iap:diamond-50:receipt-1" }
        };
        RunnerEnergySaveState runnerEnergy = RunnerEnergySaveState.CreateDefault(now);
        runnerEnergy.energy = 0;
        runnerEnergy.unlimitedUntilUtc = "2026-08-28T08:36:00.0000000Z";
        CatRunnerProgressSaveState runner = CatRunnerProgressSaveState.CreateDefault(now);
        runner.bestScore = 45678;
        runner.tutorialCompleted = true;
        runner.reducedMotion = true;
        runner.soundEnabled = false;
        runner.hapticsEnabled = false;
        runner.dailyCoins = 88;
        runner.pendingResult.hasValue = true;
        CatchLivesSaveState catchLives = CatchLivesSaveState.CreateDefault(now);
        catchLives.lives = 0;
        catchLives.tutorialCompleted = true;
        catchLives.unlimitedUntilUtc = "2026-08-29T08:36:00.0000000Z";

        CatHomeSaveData reset = InvokeCreateNewGameData(
            now, economy, runnerEnergy, runner, catchLives);

        Assert.That(reset.version, Is.EqualTo(CatHomeSaveSystem.CurrentSaveVersion));
        Assert.That(reset.hunger, Is.EqualTo(100f));
        Assert.That(reset.thirst, Is.EqualTo(100f));
        Assert.That(reset.energy, Is.EqualTo(100f));
        Assert.That(reset.wasSleeping, Is.False);
        Assert.That(reset.hasCatPose, Is.False);
        Assert.That(reset.coins, Is.Zero);
        Assert.That(reset.diamonds, Is.EqualTo(73L));
        Assert.That(reset.bondXp, Is.Zero);
        Assert.That(reset.playerLevel, Is.EqualTo(1));
        Assert.That(reset.questProgress, Is.Empty);
        Assert.That(GetBalance(reset.economy, CurrencyType.Coin), Is.Zero);
        Assert.That(GetBalance(reset.economy, CurrencyType.Diamond), Is.EqualTo(73L));
        Assert.That(reset.economy.processedTransactionIds,
            Is.EqualTo(new[] { "iap:diamond-50:receipt-1" }));
        Assert.That(reset.homeStore.ownedProductIds, Is.Empty);
        Assert.That(reset.homeStore.currentRoomId, Is.EqualTo(HomeRoomService.LivingRoomId));
        Assert.That(reset.runnerEnergy.energy, Is.EqualTo(RunnerEnergyService.MaximumEnergy));
        Assert.That(reset.runnerEnergy.unlimitedUntilUtc,
            Is.EqualTo(runnerEnergy.unlimitedUntilUtc));
        Assert.That(reset.runnerProgress.bestScore, Is.Zero);
        Assert.That(reset.runnerProgress.tutorialCompleted, Is.False);
        Assert.That(reset.runnerProgress.reducedMotion, Is.True);
        Assert.That(reset.runnerProgress.soundEnabled, Is.False);
        Assert.That(reset.runnerProgress.hapticsEnabled, Is.False);
        Assert.That(reset.runnerProgress.dailyCoins, Is.Zero);
        Assert.That(reset.runnerProgress.pendingResult.hasValue, Is.False);
        Assert.That(reset.catchLives.lives, Is.EqualTo(CatchLivesService.MaximumLives));
        Assert.That(reset.catchLives.tutorialCompleted, Is.False);
        Assert.That(reset.catchLives.unlimitedUntilUtc, Is.EqualTo(catchLives.unlimitedUntilUtc));
        Assert.That(reset.catchBestScore, Is.Zero);
        Assert.That(reset.homeProgression.homeXp, Is.Zero);
        Assert.That(reset.achievements.unlockedIds, Is.Empty);
    }

    [Test]
    public void NewGameSafetyBackup_IsAnExactTimestampedCopy()
    {
        string directory = Path.Combine(
            Path.GetTempPath(), "CatHome-NewGameBackup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string savePath = Path.Combine(directory, CatHomeSaveSystem.SaveFileName);
        File.WriteAllText(savePath, "original-save-bytes");
        try
        {
            MethodInfo method = typeof(CatHomeSaveSystem).GetMethod(
                "TryCreateNewGameBackup", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            object[] args =
            {
                savePath,
                new DateTime(2026, 8, 21, 8, 36, 0, DateTimeKind.Utc),
                null,
                null
            };
            Assert.That((bool)method.Invoke(null, args), Is.True, args[3] as string);
            string backupPath = args[2] as string;
            Assert.That(backupPath, Does.Contain(CatHomeSaveSystem.NewGameBackupMarker));
            Assert.That(File.ReadAllText(backupPath), Is.EqualTo("original-save-bytes"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static CatHomeSaveData InvokeCreateNewGameData(
        DateTime now,
        EconomySaveState economy,
        RunnerEnergySaveState runnerEnergy,
        CatRunnerProgressSaveState runnerProgress,
        CatchLivesSaveState catchLives)
    {
        MethodInfo method = typeof(CatHomeSaveSystem).GetMethod(
            "CreateNewGameData", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        return (CatHomeSaveData)method.Invoke(
            null, new object[] { now, economy, runnerEnergy, runnerProgress, catchLives });
    }

    private static long GetBalance(EconomySaveState state, CurrencyType currency)
    {
        string key = CurrencyCatalog.GetSaveKey(currency);
        foreach (CurrencyBalanceEntry entry in state.balances)
            if (entry != null && entry.currencyKey == key)
                return entry.amount;
        Assert.Fail("Missing balance for " + currency);
        return -1L;
    }

    private static void ResetLanguageRuntimeState()
    {
        MethodInfo method = typeof(GameLanguageService).GetMethod(
            "ResetRuntimeState", BindingFlags.NonPublic | BindingFlags.Static);
        method?.Invoke(null, null);
    }
}
#endif
