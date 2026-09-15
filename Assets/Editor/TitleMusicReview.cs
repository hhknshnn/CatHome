using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Reversible music preview on a copied save, restored when Play ends.</summary>
[InitializeOnLoad]
public static class TitleMusicReview
{
    private const string Key = "CatHome.TitleMusicReview.Snapshot";
    private const string Root = "Docs/QA/MENU_MUSIC_2026-09-14";
    [Serializable] private sealed class Preference
    {
        public string key, text;
        public bool existed, isString;
        public int number;
    }
    [Serializable] private sealed class Snapshot
    {
        public bool audioMuted;
        public List<Preference> preferences = new List<Preference>();
    }

    static TitleMusicReview()
    {
        EditorApplication.update += () =>
        {
            // delayCall may remain queued while the remote user's editor is unfocused.
            if (!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetString(Key, "") != "")
                Restore();
        };
    }

    [MenuItem("Tools/Cat Home/Müzik Denemesi/Başlat")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorQaSession.IsActive)
            throw new InvalidOperationException("Finish the current test session first.");
        var snapshot = new Snapshot { audioMuted = EditorUtility.audioMasterMute };
        foreach (string key in new[] { "cat.identity.breed.v1", "CatHome_CatName", "CatHome_CollectionMilestones", "cat-home.local-guest-id" })
            snapshot.preferences.Add(new Preference { key=key, existed=PlayerPrefs.HasKey(key), isString=true, text=PlayerPrefs.GetString(key) });
        foreach (string key in new[] { "cat.identity.coat", "home.audio.sound", "home.audio.music", "cat-home.language", "Player_LightLevel", "CatHome_PetTutorialCompleted", "CatHome_IntroductionCompleted", "CatHome_IntroductionStep", "CatHome_OnboardingStep", "CatHome_OnboardingCompleted", "cat-home.account-kind", "CatRunner_BestScore_v1" })
            snapshot.preferences.Add(new Preference { key=key, existed=PlayerPrefs.HasKey(key), number=PlayerPrefs.GetInt(key) });
        UiQaTestSession.Begin();
        string json = JsonUtility.ToJson(snapshot, true);
        SessionState.SetString(Key, json);
        Directory.CreateDirectory(Root);
        File.WriteAllText(Root + "/manual-preview-before.json", json);
        EditorUtility.audioMasterMute = false;
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Cat Home/Müzik Denemesi/Bitir")]
    public static void End()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
        else Restore();
    }

    private static void Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string json = SessionState.GetString(Key, "");
        if (json == "") return;
        var snapshot = JsonUtility.FromJson<Snapshot>(json);
        foreach (var pref in snapshot.preferences)
        {
            if (pref.key == "home.audio.music") HomeAudioService.MusicEnabled = !pref.existed || pref.number != 0;
            if (pref.key == "home.audio.sound") HomeAudioService.SoundEnabled = !pref.existed || pref.number != 0;
            if (pref.key == "cat-home.language" && pref.existed)
                GameLanguageService.SetLanguage((GameLanguage)pref.number);
        }
        foreach (var pref in snapshot.preferences)
        {
            if (!pref.existed) PlayerPrefs.DeleteKey(pref.key);
            else if (pref.isString) PlayerPrefs.SetString(pref.key, pref.text);
            else PlayerPrefs.SetInt(pref.key, pref.number);
        }
        PlayerPrefs.Save();
        EditorUtility.audioMasterMute = snapshot.audioMuted;
        UiQaTestSession.End();
        SessionState.EraseString(Key);
        File.WriteAllText(Root + "/manual-preview-restored.json", json);
        CatHomeEditPreview.Refresh();
    }
}
