using System;
using UnityEngine;

// Preserve both the player's preference and the service's current language.
internal sealed class TestLanguageScope : IDisposable
{
    private readonly bool existed=PlayerPrefs.HasKey(GameLanguageService.PlayerPrefsKey);
    private readonly int preference=PlayerPrefs.GetInt(GameLanguageService.PlayerPrefsKey);
    private readonly GameLanguage language=GameLanguageService.Current;
    public TestLanguageScope(GameLanguage value){GameLanguageService.SetLanguage(value);}
    public void Dispose()
    {
        GameLanguageService.SetLanguage(language);
        if(existed)PlayerPrefs.SetInt(GameLanguageService.PlayerPrefsKey,preference);
        else PlayerPrefs.DeleteKey(GameLanguageService.PlayerPrefsKey);
        PlayerPrefs.Save();
    }
}
