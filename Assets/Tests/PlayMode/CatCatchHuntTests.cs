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
    public IEnumerator Turnaround_PivotsBeforeRunningAndNeverSteersInFlight()
    {
        player.transform.rotation=Quaternion.identity;
        var start=player.Position;
        player.MoveTo(start+Vector3.back*1.5f);
        yield return null;
        Assert.That(Vector3.Distance(start,player.Position),Is.LessThan(.03f),"No backwards glide before turning");
        float until=Time.time+4f;
        bool sawFlight=false;
        Quaternion launch=Quaternion.identity;
        float launchedAt=0;
        while(Time.time<until)
        {
            if(!player.IsBusy)player.ChasePrey(NearestCatchableMouse());
            if(player.IsPouncing)
            {
                if(!sawFlight){sawFlight=true;launchedAt=Time.time;}
                if(Time.time-launchedAt<.13f)launch=player.transform.rotation;
                else Assert.That(Quaternion.Angle(launch,player.transform.rotation),Is.LessThan(.1f),"Airborne heading stays committed");
            }
            else if(sawFlight)break;
            yield return null;
        }
        Assert.That(sawFlight,Is.True);
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
        // Resolve the arena's OWN floor, never a GameObject called "Floor"
        // somewhere in the session. The Cat Catch scene loads additively on top
        // of whatever is already open, `CatCatchRoot` is parked at world
        // (40, 1000, 0) so it never sits inside the home, and the Living Room
        // authors a floor object called exactly "Floor" at y 0. A global
        // GameObject.Find therefore measured the wrong floor whenever the Living
        // Room happened to be the room left loaded by the previous fixture, and
        // the test failed with "the cat flew above the arena" at y 1000.47 -
        // which is simply the cat standing correctly on the parked arena.
        float floorTop = float.NegativeInfinity;
        Transform floor = player.transform.root.Find("CatchArena/Floor");
        Assert.That(floor, Is.Not.Null, "The Cat Catch arena has no floor.");
        Collider collider = floor.GetComponent<Collider>();
        if (collider != null)
            floorTop = collider.bounds.max.y;

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
