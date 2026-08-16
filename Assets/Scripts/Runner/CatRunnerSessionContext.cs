using UnityEngine;

/// <summary>
/// Small scene-transition context. It carries only the positive care bonus;
/// wallets and save data remain owned by their existing services.
/// </summary>
public static class CatRunnerSessionContext
{
    public static int CareBonusPercent { get; private set; }
    public static string ReturnRoomId { get; private set; } =
        HomeRoomService.LivingRoomId;
    public static string ReturnRoomSceneName =>
        HomeRoomService.GetOrLivingRoom(ReturnRoomId).SceneName;

    public static void CaptureFromHome()
    {
        // Snapshot the launch origin. Save migration or another service may
        // normalize HomeRoomService while Runner is open; returning must still
        // target the room the player actually left.
        ReturnRoomId = HomeRoomService.CurrentRoomId;

        HungerSystem hunger = Object.FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);
        ThirstSystem thirst = Object.FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
        EnergySystem energy = Object.FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);

        if (hunger == null || thirst == null || energy == null)
        {
            CareBonusPercent = 0;
            return;
        }

        float average = (hunger.CurrentHunger + thirst.CurrentThirst + energy.CurrentEnergy) / 3f;
        CareBonusPercent = average >= 80f ? 20 : average >= 60f ? 10 : 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        CareBonusPercent = 0;
        ReturnRoomId = HomeRoomService.LivingRoomId;
    }
}
