using System;
using UnityEngine;

/// <summary>Consistent TR/EN copy for the shared entry pool.</summary>
public static class MiniGameLivesPresentation
{
    public static string Count() => MiniGameLivesService.IsUnlimited
        ? CozyGameUi.Copy("Ortak canlar · Sınırsız", "Shared lives · Unlimited")
        : CozyGameUi.Copy($"Ortak canlar  {MiniGameLivesService.CurrentLives}/20", $"Shared lives  {MiniGameLivesService.CurrentLives}/20");

    public static string Refill()
    {
        if (MiniGameLivesService.IsUnlimited)
            return CozyGameUi.Copy("Dört oyunda geçerli", "Available in all four games");
        if (MiniGameLivesService.CurrentLives == MiniGameLivesService.MaximumLives)
            return CozyGameUi.Copy("Canlar dolu · Her tur 1 can", "Lives full · 1 life per round");
        TimeSpan remaining=MiniGameLivesService.TimeUntilNextLife();
        int seconds=Mathf.Max(0,Mathf.CeilToInt((float)remaining.TotalSeconds));
        string time=$"{seconds/60:00}:{seconds%60:00}";
        return CozyGameUi.Copy($"+1 can {time} sonra · 10 dk'da 1", $"+1 life in {time} · 1 every 10 min");
    }

    public static string Summary() => Count()+" · "+Refill();
}
