using System;
using UnityEngine;

/// <summary>
/// Persisted sound preferences for the home (rooms, garden, shop, menus). Mirrors
/// the static-service shape used elsewhere: no scene presence, PlayerPrefs-backed,
/// wiped nothing on domain reload (preferences are meant to survive). The mini
/// games keep their own <c>CatRunnerProgressService</c> switches; this governs the
/// persistent home soundscape and UI feedback that <see cref="HomeAudioController"/>
/// plays.
/// </summary>
public static class HomeAudioService
{
    private const string SoundKey = "home.audio.sound";
    private const string MusicKey = "home.audio.music";

    private static bool loaded;
    private static bool soundEnabled = true;
    private static bool musicEnabled = true;

    /// <summary>Raised whenever a preference changes so live audio can react.</summary>
    public static event Action Changed;

    public static bool SoundEnabled
    {
        get { EnsureLoaded(); return soundEnabled; }
        set
        {
            EnsureLoaded();
            if (soundEnabled == value)
                return;
            soundEnabled = value;
            PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    /// <summary>Background music toggle. Sound effects stay under <see cref="SoundEnabled"/>.</summary>
    public static bool MusicEnabled
    {
        get { EnsureLoaded(); return musicEnabled; }
        set
        {
            EnsureLoaded();
            if (musicEnabled == value)
                return;
            musicEnabled = value;
            PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static void ToggleSound() => SoundEnabled = !SoundEnabled;

    public static void ToggleMusic() => MusicEnabled = !MusicEnabled;

    private static void EnsureLoaded()
    {
        if (loaded)
            return;
        loaded = true;
        soundEnabled = PlayerPrefs.GetInt(SoundKey, 1) != 0;
        musicEnabled = PlayerPrefs.GetInt(MusicKey, 1) != 0;
    }
}
