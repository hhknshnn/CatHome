using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CatHome.Tests.EditMode
{
    public sealed class CatHomeSaveRecoveryTests
    {
        private string testDirectory;
        private bool previousIgnoreFailingMessages;

        [SetUp]
        public void SetUp()
        {
            testDirectory = Path.Combine(
                Path.GetTempPath(),
                "CatHomeSaveRecoveryTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
            previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }

        [Test]
        public void SuccessfulWrite_RefreshesDurableRecoveryCopy()
        {
            string savePath = SavePath();
            CatHomeSaveData first = CreateSave(25);
            Assert.That(WriteAtPath(first, savePath), Is.True);

            string recoveryPath = savePath + CatHomeSaveSystem.RecoveryFileSuffix;
            Assert.That(File.Exists(savePath), Is.True);
            Assert.That(File.Exists(recoveryPath), Is.True);
            Assert.That(ReadJson(recoveryPath).coins, Is.EqualTo(25));

            CatHomeSaveData second = CreateSave(80);
            Assert.That(WriteAtPath(second, savePath), Is.True);
            Assert.That(ReadJson(recoveryPath).coins, Is.EqualTo(80));
        }

        [Test]
        public void CorruptPrimary_RestoresLastSuccessfulRecoveryCopy()
        {
            string savePath = SavePath();
            Assert.That(WriteAtPath(CreateSave(140), savePath), Is.True);
            File.WriteAllText(savePath, "{ this is not valid json");

            Assert.That(ReadAtPath(savePath, out CatHomeSaveData recovered), Is.True);
            Assert.That(recovered.coins, Is.EqualTo(140));
            Assert.That(ReadJson(savePath).coins, Is.EqualTo(140));
            Assert.That(
                Directory.GetFiles(testDirectory, "*.corrupt-*").Length,
                Is.EqualTo(1));
        }

        [Test]
        public void MissingPrimary_RestoresRecoveryCopy()
        {
            string savePath = SavePath();
            Assert.That(WriteAtPath(CreateSave(210), savePath), Is.True);
            File.Delete(savePath);

            Assert.That(ReadAtPath(savePath, out CatHomeSaveData recovered), Is.True);
            Assert.That(recovered.coins, Is.EqualTo(210));
            Assert.That(File.Exists(savePath), Is.True);
        }

        [Test]
        public void NewerSchema_DoesNotSilentlyRollBackToOlderRecoveryCopy()
        {
            string savePath = SavePath();
            Assert.That(WriteAtPath(CreateSave(320), savePath), Is.True);
            File.WriteAllText(savePath, JsonUtility.ToJson(new CatHomeSaveData
            {
                version = CatHomeSaveSystem.CurrentSaveVersion + 1,
                coins = 999
            }));

            Assert.That(ReadAtPath(savePath, out _), Is.False);
            Assert.That(File.Exists(savePath), Is.False);
            Assert.That(
                Directory.GetFiles(testDirectory, "*.incompatible-*").Length,
                Is.EqualTo(1));
            Assert.That(
                ReadJson(savePath + CatHomeSaveSystem.RecoveryFileSuffix).coins,
                Is.EqualTo(320));
        }

        private string SavePath()
        {
            return Path.Combine(testDirectory, CatHomeSaveSystem.SaveFileName);
        }

        private static CatHomeSaveData CreateSave(long coins)
        {
            return new CatHomeSaveData
            {
                version = CatHomeSaveSystem.CurrentSaveVersion,
                coins = coins,
                lastSaveUtc = DateTime.UtcNow.ToString("O")
            };
        }

        private static CatHomeSaveData ReadJson(string path)
        {
            return JsonUtility.FromJson<CatHomeSaveData>(File.ReadAllText(path));
        }

        private static bool WriteAtPath(CatHomeSaveData data, string path)
        {
            MethodInfo method = typeof(CatHomeSaveSystem).GetMethod(
                "TryWriteSaveDataAtPath",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(null, new object[] { data, path });
        }

        private static bool ReadAtPath(string path, out CatHomeSaveData data)
        {
            MethodInfo method = typeof(CatHomeSaveSystem).GetMethod(
                "TryReadSaveAtPath",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            object[] arguments = { path, null };
            bool success = (bool)method.Invoke(null, arguments);
            data = arguments[1] as CatHomeSaveData;
            return success;
        }
    }
}
