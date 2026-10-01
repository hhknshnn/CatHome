#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public sealed class InteractionLocalizationCoverageTests
{
    GameLanguage previous;
    bool hadPreference;
    int previousPreference;
    [SetUp] public void Before()
    {
        previous = GameLanguageService.Current;
        hadPreference = PlayerPrefs.HasKey(GameLanguageService.PlayerPrefsKey);
        previousPreference = PlayerPrefs.GetInt(GameLanguageService.PlayerPrefsKey);
    }
    [TearDown] public void After()
    {
        GameLanguageService.SetLanguage(previous);
        if (hadPreference) PlayerPrefs.SetInt(GameLanguageService.PlayerPrefsKey, previousPreference);
        else PlayerPrefs.DeleteKey(GameLanguageService.PlayerPrefsKey);
        PlayerPrefs.Save();
    }

    [Test] public void ActualActionStatusFailureAndPrefabTokens_HaveBothLanguages()
    {
        // Enumerate the real call sites and serialized authored content. Adding
        // an unlocalized action to a future room must fail this test.
        var tokens = new List<(string category, string value, string origin)>();
        foreach (string path in Directory.GetFiles("Assets/Scripts/Activities", "*.cs"))
        {
            string source = File.ReadAllText(path);
            foreach (var spec in new[] {
                ("reaction", @"(?:ShowSpeech|CompleteActivity)\s*\([^;]+;"),
                ("reaction", @"(?:failureReason|reason|completeMessage)\s*=\s*[^;]+;"),
                ("status", @"ProgressLabel\s*=>[^;]+;"),
                ("action", @"actionText\s*=\s*[^;]+;") })
            foreach (Match block in Regex.Matches(source, spec.Item2, RegexOptions.Singleline))
            foreach (Match literal in Regex.Matches(block.Value, "\\$?\"((?:\\\\.|[^\"\\\\])*)\""))
            {
                string value = Regex.Unescape(literal.Groups[1].Value);
                value = Regex.Replace(value, @"\{[^{}]+\}", "1");
                if (string.IsNullOrEmpty(value) || Regex.IsMatch(value, @"^[a-z][a-z_.]+$")) continue;
                tokens.Add((spec.Item1, value, path + ":" + (1 + source.Take(block.Index).Count(c => c == '\n'))));
            }
        }
        string[] authoredFiles = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles("Assets/Scenes/Levels", "*.unity")).ToArray();
        foreach (string path in authoredFiles)
        foreach (string line in File.ReadLines(path))
        {
            var match = Regex.Match(line, @"^\s*(actionText|completeMessage|buttonText):\s*(.*)$");
            if (!match.Success) continue;
            string value = match.Groups[2].Value.Trim();
            if (string.IsNullOrEmpty(value) || value.StartsWith("{fileID:")) continue;
            if (value.StartsWith("\"") && value.EndsWith("\"")) value = Regex.Unescape(value.Substring(1, value.Length - 2));
            else if (value.StartsWith("'") && value.EndsWith("'")) value = value.Substring(1, value.Length - 2).Replace("''", "'");
            tokens.Add((match.Groups[1].Value == "completeMessage" ? "reaction" : "action", value, path));
        }
        foreach (string path in authoredFiles)
        foreach (Match block in Regex.Matches(File.ReadAllText(path), @"^--- !u!114.*?(?=^--- !u!|\z)", RegexOptions.Multiline | RegexOptions.Singleline))
        {
            if (!Regex.IsMatch(block.Value, @"^\s*activityId:", RegexOptions.Multiline)) continue;
            var store = Regex.Match(block.Value, @"^\s*storeProductId:[ \t]*([^\r\n]*)", RegexOptions.Multiline);
            if (store.Success && !string.IsNullOrWhiteSpace(store.Groups[1].Value)) continue;
            var title = Regex.Match(block.Value, @"^\s*displayName:[ \t]*([^\r\n]*)", RegexOptions.Multiline);
            if (title.Success && !string.IsNullOrWhiteSpace(title.Groups[1].Value))
                tokens.Add(("title", title.Groups[1].Value.Trim(), path));
        }
        Assert.That(tokens.Count, Is.GreaterThan(200), "The inventory must include source and prefabs.");
        foreach (var token in tokens)
        {
            string[] copies = new string[2];
            for (int i = 0; i < 2; i++)
            {
                GameLanguageService.SetLanguage(i == 0 ? GameLanguage.Turkish : GameLanguage.English);
                bool known = token.category == "title" ? GameInteractionCopy.TryActivityTitle(token.value, out copies[i]) : token.category == "reaction"
                    ? GameContentCopy.TryCatReaction(token.value, out copies[i])
                    : GameInteractionCopy.TryText(token.value, out copies[i]);
                Assert.That(known, Is.True, token.origin + " / " + token.category + " / " + token.value);
                Assert.That(copies[i], Is.Not.Null.And.Not.Empty, token.origin);
                Assert.That(copies[i], Does.Not.Contain("IS NOT READY"), token.origin);
            }
            Assert.That(copies[0], Is.Not.EqualTo(copies[1]), token.origin + " / " + token.value);
        }
    }

    [Test] public void ShopLegacyFeedbackAndTutorialDefaults_ResolveAtTheirPresentationBoundary()
    {
        string shop = File.ReadAllText("Assets/Scripts/ShopPanelController.cs");
        foreach (Match call in Regex.Matches(shop, "SetFeedback\\(\"([^\"]+)\"\\)"))
        {
            string legacy = call.Groups[1].Value;
            GameLanguageService.SetLanguage(GameLanguage.Turkish);
            string tr = GameStatusCopy.Text(legacy);
            GameLanguageService.SetLanguage(GameLanguage.English);
            string en = GameStatusCopy.Text(legacy);
            Assert.That(tr, Is.Not.EqualTo(legacy).And.Not.EqualTo(en), legacy);
        }
        string edit = File.ReadAllText("Assets/Scripts/Store/HomeEditModeController.cs");
        Assert.That(Regex.IsMatch(edit, "(?:UpdateUi|SaveActivePlacement)\\(\"[A-Z]"), Is.False,
            "Placement feedback must use a language key, including early return paths.");
        string tutorial = File.ReadAllText("Assets/Scripts/PetTutorialHint.cs");
        Assert.That(tutorial, Does.Not.Contain(" : step.message;"), "Authored English tutorial defaults cannot be shown directly.");
        foreach (var key in new[] { "tutorial.pet_action", "tutorial.move_action", "tutorial.watch_needs", "tutorial.food_water", "tutorial.rest_bed" })
        {
            GameLanguageService.SetLanguage(GameLanguage.Turkish); string tr = GameLanguageService.Text(key);
            GameLanguageService.SetLanguage(GameLanguage.English); string en = GameLanguageService.Text(key);
            Assert.That(tr, Is.Not.EqualTo(en).And.Not.EqualTo(key), key);
        }
    }

    [Test] public void AllLanguageKeys_HaveMatchingNonEmptyTranslationsAndFormatSlots()
    {
        var fields = BindingFlags.Static | BindingFlags.NonPublic;
        var tr = (Dictionary<string, string>)typeof(GameLanguageService).GetField("Turkish", fields).GetValue(null);
        var en = (Dictionary<string, string>)typeof(GameLanguageService).GetField("English", fields).GetValue(null);
        Assert.That(tr.Keys, Is.EquivalentTo(en.Keys));
        foreach (var key in tr.Keys)
        {
            Assert.That(tr[key], Is.Not.Null.And.Not.Empty, key);
            Assert.That(en[key], Is.Not.Null.And.Not.Empty, key);
            var trSlots = Regex.Matches(tr[key], @"\{\d+(?:[^{}]*)\}").Cast<Match>().Select(m => m.Value).OrderBy(x => x).ToArray();
            var enSlots = Regex.Matches(en[key], @"\{\d+(?:[^{}]*)\}").Cast<Match>().Select(m => m.Value).OrderBy(x => x).ToArray();
            Assert.That(trSlots, Is.EqualTo(enSlots), key);
        }
    }

    [Test] public void SafeFallbacksAndFormattedNames_PreserveTheSelectedLanguageAndCustomCase()
    {
        const string name = "Mİnoş / Mr. Whiskers the Third";
        foreach (var language in new[] { GameLanguage.Turkish, GameLanguage.English })
        {
            GameLanguageService.SetLanguage(language);
            string approach=language==GameLanguage.Turkish?"Kaba dönüp biraz yaklaşalım.":"Face the bowl and move a little closer.";
            Assert.That(GameContentCopy.CatReaction(approach),Is.EqualTo(approach));
            Assert.That(GameInteractionCopy.Text(name), Is.EqualTo(name));
            Assert.That(CatIdlePersonality.AttentionLine(CatIdleMood.Happy, name, 100, 100, 100), Does.Contain(name));
            Assert.That(GameLanguageService.Format("shop.buy_first", name), Does.Contain(name));
            Assert.That(GameContentCopy.CatReaction("AN UNKNOWN ENGLISH ERROR"),
                Is.EqualTo(GameLanguageService.Text("interaction.feedback_fallback")));
            Assert.That(GameInteractionCopy.Action("UNKNOWN_ACTION_ID"),
                Is.EqualTo(GameLanguageService.Text("interaction.action_fallback")));
            Assert.That(GameLanguageService.Text("missing.internal.key"),
                Is.EqualTo(GameLanguageService.Text("localization.unavailable")));
            Assert.That(GameInteractionCopy.ProductAction("patio.potted-ferns", "unused", "WATCH", false),
                Is.EqualTo(language == GameLanguage.Turkish ? "Saksıları incele" : "Inspect the planters"));
            Assert.That(GameContentCopy.CatReaction("FERN_INSPECTED"),
                Is.EqualTo(GameLanguageService.Text("interaction.fern.complete")));
            Assert.That(GameInteractionCopy.ProductAction("loft.record-player", "unused", "SWAT", false),
                Is.EqualTo(GameLanguageService.Text("record.action")));
            foreach (string key in new[] { "record.on", "record.off", "record.not_ready", "care.not_hungry", "care.not_thirsty" })
                Assert.That(GameContentCopy.CatReaction(GameLanguageService.Text(key)), Is.EqualTo(GameLanguageService.Text(key)), key);
        }
    }

    [Test] public void EveryCatalogProduct_HasTurkishCopyWithoutChangingItsIdentity()
    {
        string[] before = HomeStoreService.Products.Select(p => p.Id).ToArray();
        GameLanguageService.SetLanguage(GameLanguage.Turkish);
        foreach (var product in HomeStoreService.Products)
        {
            Assert.That(product.Title, Is.Not.EqualTo(GameLanguageService.Text("product.unknown")), product.Id);
            Assert.That(product.Title, Does.Not.Contain(product.Id), product.Id);
        }
        Assert.That(HomeStoreService.Products.Select(p => p.Id), Is.EqualTo(before));
    }
}
#endif
