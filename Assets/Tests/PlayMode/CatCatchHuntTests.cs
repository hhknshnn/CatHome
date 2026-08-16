using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Plays the hunt for real: the cat must earn its catches by chasing, and must
/// never collect a mouse by standing still next to one.
/// </summary>
public sealed class CatCatchHuntTests
{
    private CatCatchGameController game;
    private CatCatchPlayer player;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        CatchLivesService.ApplySavedState(null, System.DateTime.UtcNow);
        CatchLivesService.CompleteTutorial();
        yield return SceneManager.LoadSceneAsync(
            CatCatchLauncher.CatchSceneName, LoadSceneMode.Additive);
        game = Object.FindAnyObjectByType<CatCatchGameController>();
        player = Object.FindAnyObjectByType<CatCatchPlayer>();
        Assert.That(game, Is.Not.Null, "Cat Catch scene did not provide a hunt controller.");
        Assert.That(player, Is.Not.Null, "Cat Catch scene did not provide a hunter.");
        game.StartHunt();
        Assert.That(game.IsHunting, Is.True, "The hunt refused to start.");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (game != null)
            game.ExitToHome();
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetSceneByName(CatCatchLauncher.CatchSceneName);
        if (scene.IsValid() && scene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(scene);
    }

    [UnityTest]
    public IEnumerator IdleCat_NeverVacuumsMice()
    {
        float until = Time.time + 3f;
        while (Time.time < until)
            yield return null;

        Assert.That(game.Catches, Is.Zero, "Standing still still collected mice.");
        Assert.That(game.Score, Is.Zero);
        Assert.That(game.StrikesResolved, Is.Zero, "A strike fired without a pounce.");
    }

    [UnityTest]
    public IEnumerator ChasingCat_RunsDownAndCatchesMice()
    {
        float until = Time.time + 12f;
        while (Time.time < until && game.Catches < 2)
        {
            CatCatchMouse prey = NearestCatchableMouse();
            if (prey != null && !player.IsBusy && player.Prey == null)
                player.ChasePrey(prey);
            yield return null;
        }

        Assert.That(game.Catches, Is.GreaterThanOrEqualTo(2),
            "The cat could not run down two mice in twelve seconds.");
        Assert.That(game.Score, Is.GreaterThan(0));
        Assert.That(game.StrikesResolved, Is.GreaterThanOrEqualTo(game.Catches),
            "Catches appeared without a pounce landing behind them.");
    }

    [UnityTest]
    public IEnumerator CatStaysOnTheFloorWhileHunting()
    {
        float floorTop = float.NegativeInfinity;
        GameObject floor = GameObject.Find("Floor");
        if (floor != null)
        {
            Collider collider = floor.GetComponent<Collider>();
            if (collider != null)
                floorTop = collider.bounds.max.y;
        }

        float until = Time.time + 5f;
        float lowest = float.PositiveInfinity;
        float highest = float.NegativeInfinity;
        while (Time.time < until)
        {
            CatCatchMouse prey = NearestCatchableMouse();
            if (prey != null && !player.IsBusy && player.Prey == null)
                player.ChasePrey(prey);
            lowest = Mathf.Min(lowest, player.Position.y);
            highest = Mathf.Max(highest, player.Position.y);
            yield return null;
        }

        Assert.That(lowest, Is.GreaterThan(floorTop - 0.25f), "The cat sank through the floor.");
        Assert.That(highest, Is.LessThan(floorTop + 1.2f), "The cat flew above the arena.");
    }

    private CatCatchMouse NearestCatchableMouse()
    {
        CatCatchMouse[] mice = Object.FindObjectsByType<CatCatchMouse>(FindObjectsInactive.Include);
        CatCatchMouse best = null;
        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < mice.Length; i++)
        {
            if (mice[i] == null || !mice[i].IsCatchable)
                continue;
            Vector3 delta = mice[i].transform.position - player.Position;
            delta.y = 0f;
            float distance = delta.sqrMagnitude;
            if (distance >= bestDistance)
                continue;
            bestDistance = distance;
            best = mice[i];
        }
        return best;
    }
}
