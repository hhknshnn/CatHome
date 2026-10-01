using UnityEngine;

public enum CatIdleMood
{
    Happy = 0,
    Content = 1,
    Restless = 2,
    Sleepy = 3,
    Needy = 4
}

public enum CatIdleBeat
{
    LookAround = 0,
    Groom = 1,
    ToyGlance = 2,
    Stretch = 3,
    Play = 4,
    Attention = 5
}

/// <summary>
/// Pure idle-personality rules: mood from Bond + needs, beat weights, speech
/// lines and clip durations. Kept free of MonoBehaviour so EditMode tests can
/// lock the contract without a scene or Animator.
/// </summary>
public static class CatIdlePersonality
{
    public const long HappyBondXp = 80L;
    public const string IdleState = "Idle";

    public static CatIdleMood Evaluate(
        float hunger,
        float thirst,
        float energy,
        long bondXp)
    {
        hunger = Mathf.Clamp(hunger, 0f, 100f);
        thirst = Mathf.Clamp(thirst, 0f, 100f);
        energy = Mathf.Clamp(energy, 0f, 100f);
        float floor = Mathf.Min(hunger, Mathf.Min(thirst, energy));

        if (floor < 28f)
            return CatIdleMood.Needy;
        if (energy < 32f)
            return CatIdleMood.Sleepy;
        if (floor >= 72f && bondXp >= HappyBondXp)
            return CatIdleMood.Happy;
        if (floor >= 48f)
            return CatIdleMood.Content;
        return CatIdleMood.Restless;
    }

    public static float QuietDelay(CatIdleMood mood)
    {
        switch (mood)
        {
            case CatIdleMood.Needy: return 6f;
            case CatIdleMood.Restless: return 8f;
            case CatIdleMood.Sleepy: return 10f;
            case CatIdleMood.Content: return 12f;
            default: return 11f;
        }
    }

    public static float AttentionDelay(CatIdleMood mood)
    {
        switch (mood)
        {
            case CatIdleMood.Needy: return 18f;
            case CatIdleMood.Restless: return 22f;
            case CatIdleMood.Sleepy: return 28f;
            default: return 32f;
        }
    }

    /// <param name="roll0to99">Deterministic 0–99 roll so tests can pin weights.</param>
    public static CatIdleBeat PickBeat(CatIdleMood mood, int roll0to99)
    {
        int roll = Mathf.Clamp(roll0to99, 0, 99);
        switch (mood)
        {
            case CatIdleMood.Happy:
                if (roll < 25) return CatIdleBeat.LookAround;
                if (roll < 45) return CatIdleBeat.Groom;
                if (roll < 70) return CatIdleBeat.ToyGlance;
                if (roll < 80) return CatIdleBeat.Stretch;
                return CatIdleBeat.Play;
            case CatIdleMood.Content:
                if (roll < 30) return CatIdleBeat.LookAround;
                if (roll < 65) return CatIdleBeat.Groom;
                if (roll < 85) return CatIdleBeat.ToyGlance;
                return CatIdleBeat.Stretch;
            case CatIdleMood.Restless:
                if (roll < 25) return CatIdleBeat.LookAround;
                if (roll < 50) return CatIdleBeat.Groom;
                if (roll < 80) return CatIdleBeat.ToyGlance;
                return CatIdleBeat.Stretch;
            case CatIdleMood.Sleepy:
                if (roll < 20) return CatIdleBeat.LookAround;
                if (roll < 35) return CatIdleBeat.Groom;
                if (roll < 45) return CatIdleBeat.ToyGlance;
                return CatIdleBeat.Stretch;
            default:
                if (roll < 40) return CatIdleBeat.LookAround;
                if (roll < 60) return CatIdleBeat.Groom;
                if (roll < 80) return CatIdleBeat.ToyGlance;
                return CatIdleBeat.Stretch;
        }
    }

    public static string AnimatorState(CatIdleBeat beat)
    {
        switch (beat)
        {
            case CatIdleBeat.Groom: return "ActivityScratch";
            case CatIdleBeat.ToyGlance: return "ActivityPawSwat";
            case CatIdleBeat.Stretch: return "LieDown";
            case CatIdleBeat.Play: return "ActivityPounce";
            default: return string.Empty;
        }
    }

    public static float BeatDuration(CatIdleBeat beat)
    {
        switch (beat)
        {
            case CatIdleBeat.LookAround: return 2.2f;
            case CatIdleBeat.Groom: return 1.1f;
            case CatIdleBeat.ToyGlance: return 0.58f;
            case CatIdleBeat.Stretch: return 1.6f;
            case CatIdleBeat.Play: return 0.72f;
            default: return 1.85f;
        }
    }

    public static string AttentionLine(
        CatIdleMood mood,
        string catName,
        float hunger,
        float thirst,
        float energy)
    {
        if (mood == CatIdleMood.Needy)
        {
            if (hunger <= thirst && hunger <= energy)
                return GameLanguageService.Text("idle.hungry");
            if (thirst <= energy)
                return GameLanguageService.Text("idle.thirsty");
            return GameLanguageService.Text("idle.tired");
        }

        if (mood == CatIdleMood.Sleepy)
            return GameLanguageService.Text("idle.sleepy");
        if (mood == CatIdleMood.Restless)
            return GameLanguageService.Text("idle.bored");

        string name = string.IsNullOrWhiteSpace(catName) ? "Melo" : catName;
        return GameLanguageService.Format("idle.pet", name);
    }
}
