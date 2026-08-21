using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum GameLanguage
{
    English = 0,
    Turkish = 1
}

/// <summary>
/// Small, package-free localization seam for Cat Home. The selected language
/// lives outside the gameplay save so NEW GAME never changes it.
/// </summary>
public static class GameLanguageService
{
    public const string PlayerPrefsKey = "cat-home.language";

    private static readonly Dictionary<string, string> English = new()
    {
        ["title.greeting.empty"] = "YOUR COZY CAT AWAITS",
        ["title.greeting.named"] = "{0} IS WAITING FOR YOU",
        ["title.home.empty"] = "WELCOME HOME",
        ["title.home.named"] = "A COZY HOME FOR {0}",
        ["title.play"] = "PLAY",
        ["title.continue"] = "CONTINUE",
        ["title.home_level"] = "HOME LV. {0}",
        ["title.collection"] = "{0} OF {1} • COLLECTED",
        ["title.settings"] = "SETTINGS",
        ["title.credits"] = "CREDITS",
        ["title.quit"] = "QUIT",
        ["title.new_game"] = "NEW GAME",
        ["title.promise"] = "CARE  •  DECORATE  •  PLAY",
        ["title.shortcuts"] = "CHOOSE YOUR HAPPY PLACE",
        ["title.shop"] = "SHOP",
        ["title.rooms"] = "ROOMS",
        ["title.games"] = "GAMES",
        ["title.after_tour"] = "AFTER TOUR",
        ["credits.title"] = "CREDITS",
        ["credits.body"] = "CAT HOME\nA COZY CAT CARE GAME\nMADE WITH CARE FOR CATS AND KIDS",
        ["common.close"] = "CLOSE",
        ["settings.badge"] = "GEAR",
        ["settings.title"] = "SETTINGS",
        ["settings.subtitle"] = "SOUND, MUSIC, MOTION & LANGUAGE",
        ["settings.row.music"] = "MUSIC",
        ["settings.row.sound"] = "SOUND EFFECTS",
        ["settings.row.game_sound"] = "GAME SOUND",
        ["settings.row.haptics"] = "VIBRATION",
        ["settings.row.reduced_motion"] = "REDUCED MOTION",
        ["settings.row.language"] = "LANGUAGE",
        ["settings.on"] = "ON",
        ["settings.off"] = "OFF",
        ["language.english"] = "ENGLISH",
        ["language.turkish"] = "TÜRKÇE",
        ["new_game.title"] = "START A NEW GAME?",
        ["new_game.body"] = "COINS, ROOMS, ITEMS, LEVELS AND SCORES WILL RESET.\nDIAMONDS, PURCHASE RIGHTS AND SETTINGS WILL STAY.",
        ["new_game.cancel"] = "CANCEL",
        ["new_game.confirm"] = "YES, NEW GAME",
        ["new_game.failed"] = "COULD NOT START A NEW GAME. YOUR CURRENT SAVE IS SAFE."
    };

    private static readonly Dictionary<string, string> Turkish = new()
    {
        ["title.greeting.empty"] = "SICAK YUVANDA KEDİN SENİ BEKLİYOR",
        ["title.greeting.named"] = "{0} SENİ BEKLİYOR",
        ["title.home.empty"] = "YUVANA HOŞ GELDİN",
        ["title.home.named"] = "{0} İÇİN SICAK BİR YUVA",
        ["title.play"] = "OYNA",
        ["title.continue"] = "DEVAM ET",
        ["title.home_level"] = "YUVA SV. {0}",
        ["title.collection"] = "{0} / {1} • TOPLANDI",
        ["title.settings"] = "AYARLAR",
        ["title.credits"] = "YAPIMCILAR",
        ["title.quit"] = "ÇIKIŞ",
        ["title.new_game"] = "YENİ OYUN",
        ["title.promise"] = "İLGİLEN  •  DEKORE ET  •  OYNA",
        ["title.shortcuts"] = "MUTLU KÖŞENİ SEÇ",
        ["title.shop"] = "MAĞAZA",
        ["title.rooms"] = "ODALAR",
        ["title.games"] = "OYUNLAR",
        ["title.after_tour"] = "TURDAN SONRA",
        ["credits.title"] = "YAPIMCILAR",
        ["credits.body"] = "CAT HOME\nSICAK BİR KEDİ BAKIM OYUNU\nKEDİLER VE ÇOCUKLAR İÇİN SEVGİYLE YAPILDI",
        ["common.close"] = "KAPAT",
        ["settings.badge"] = "AYAR",
        ["settings.title"] = "AYARLAR",
        ["settings.subtitle"] = "SES, MÜZİK, HAREKET VE DİL",
        ["settings.row.music"] = "MÜZİK",
        ["settings.row.sound"] = "SES EFEKTLERİ",
        ["settings.row.game_sound"] = "OYUN SESİ",
        ["settings.row.haptics"] = "TİTREŞİM",
        ["settings.row.reduced_motion"] = "AZALTILMIŞ HAREKET",
        ["settings.row.language"] = "DİL",
        ["settings.on"] = "AÇIK",
        ["settings.off"] = "KAPALI",
        ["language.english"] = "ENGLISH",
        ["language.turkish"] = "TÜRKÇE",
        ["new_game.title"] = "YENİ OYUN BAŞLATILSIN MI?",
        ["new_game.body"] = "JETONLAR, ODALAR, EŞYALAR, SEVİYELER VE SKORLAR SIFIRLANIR.\nELMASLAR, SATIN ALMA HAKLARI VE AYARLAR KORUNUR.",
        ["new_game.cancel"] = "VAZGEÇ",
        ["new_game.confirm"] = "EVET, YENİ OYUN",
        ["new_game.failed"] = "YENİ OYUN BAŞLATILAMADI. MEVCUT KAYDIN GÜVENDE."
    };

    private static bool initialized;
    private static GameLanguage current;

    public static event Action Changed;

    public static GameLanguage Current
    {
        get
        {
            EnsureInitialized();
            return current;
        }
    }

    public static GameLanguage ResolveInitialLanguage(SystemLanguage systemLanguage) =>
        systemLanguage == SystemLanguage.Turkish
            ? GameLanguage.Turkish
            : GameLanguage.English;

    public static void SetLanguage(GameLanguage language)
    {
        EnsureInitialized();
        if (current == language && PlayerPrefs.HasKey(PlayerPrefsKey))
            return;
        current = language;
        PlayerPrefs.SetInt(PlayerPrefsKey, (int)language);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void Toggle() => SetLanguage(
        Current == GameLanguage.English ? GameLanguage.Turkish : GameLanguage.English);

    public static string Text(string key)
    {
        EnsureInitialized();
        Dictionary<string, string> table = current == GameLanguage.Turkish ? Turkish : English;
        if (table.TryGetValue(key, out string value))
            return value;
        return English.TryGetValue(key, out value) ? value : key;
    }

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, Text(key), args);

    private static void EnsureInitialized()
    {
        if (initialized)
            return;
        int fallback = (int)ResolveInitialLanguage(Application.systemLanguage);
        current = (GameLanguage)Mathf.Clamp(
            PlayerPrefs.GetInt(PlayerPrefsKey, fallback),
            (int)GameLanguage.English,
            (int)GameLanguage.Turkish);
        initialized = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        initialized = false;
        current = GameLanguage.English;
        Changed = null;
    }
}
