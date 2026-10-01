using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Import only with the Cart mesh draft and baked CatMeshContactSurface catalog.
public sealed class CartMeshContactPolishTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    GroundContactStartTests fixture;
    CatMovement cat;
    CartNudgeActivity cart;
    readonly List<string> rows = new List<string>();
    bool fixtureReady;
    [SetUp] public void Before()
    {
        fixture = new GroundContactStartTests(); fixture.Before(); fixtureReady = true;
        rows.Clear(); rows.Add("stage,attempt,x,y,z,yaw,currentClear,firstPlans,facingPlans,laterPlans,publicReady,milliseconds,detail");
    }
    [TearDown] public void After()
    { try { if (fixtureReady) fixture.After(); } finally { Flush(); } }
    object InvokeFixture(string name, params object[] args) => typeof(GroundContactStartTests).GetMethod(name, Private).Invoke(fixture, args);
    static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
    void Flush()
    {
        Directory.CreateDirectory(Root);
        File.WriteAllLines(Root + "/cart-mesh-contact-diagnostic.csv", rows);
    }
    void Row(string stage, int attempt = 0, bool clear = false, int first = 0, int facing = 0,
        int later = 0, bool ready = false, double ms = 0, string detail = "")
    {
        Vector3 p = cat != null ? cat.transform.position : Vector3.zero;
        rows.Add(FormattableString.Invariant($"{stage},{attempt},{p.x:F5},{p.y:F5},{p.z:F5},{(cat != null ? cat.transform.eulerAngles.y : 0):F2},{clear},{first},{facing},{later},{ready},{ms:F3},\"{detail.Replace("\"", "\"\"")}\""));
        Flush();
    }

    [UnityTest, Timeout(120000)] public IEnumerator RussianBlue_RealMeshBothPushes_PauseAndRootStayFixed()
    {
        yield return (IEnumerator)InvokeFixture("Prepare", "Kitchen_Level01");
        cat = Field<CatMovement>(fixture, "cat");
        CatBreedService.Select("russian-blue"); yield return null; yield return null;
        cart = CatActivity.Registered.OfType<CartNudgeActivity>().Single(a => a.gameObject.scene == cat.gameObject.scene);
        RoomPlayModeSupport.ProvisionNeeds();
        var set = new CatMeshContactSurface.TargetSet(cart.CartVisual);
        var resolver = (CatPawMeshContactResolver)typeof(CartNudgeActivity).GetMethod("EnsureMeshContacts", Private).Invoke(cart, null);
        var centre = Field<Transform>(cart, "shovePoint").position;
        var renderer = cart.CartVisual.GetComponentInChildren<Renderer>();
        Assert.That(set.TryClosest(renderer.bounds.center, out var catalogHit) && catalogHit.IsValid, Is.True,
            "Bake the actual cart mesh into CatMeshContactSurface; do not substitute bounds");
        Row("catalog-ready", detail: "mesh=" + catalogHit.Mesh.name + ";triangle=" + catalogHit.Triangle + ";point=" + catalogHit.Point);
        bool found = false; CatActivityStart accepted = default; int attempts = 0;
        var whole = System.Diagnostics.Stopwatch.StartNew();
        foreach (float radius in new[] { 0f, .12f, .24f, .34f, .42f })
        {
            if (found) break;
            for (int angle = 0; angle < (radius == 0 ? 1 : 12) && !found; angle++)
            {
                Vector3 position = centre + Quaternion.Euler(0, angle * 30f, 0) * Vector3.forward * radius;
                Vector3 inward = renderer.bounds.center - position; inward.y = 0;
                Quaternion aim = inward.sqrMagnitude > .000001f ? Quaternion.LookRotation(inward) : Quaternion.identity;
                foreach (float yaw in new[] { 0f, -35f, 35f, -65f, 65f })
                {
                    if (whole.Elapsed.TotalSeconds > 45)
                    { Row("bounded-timeout", attempts, detail: "No layout, target, collider or threshold changed"); Assert.Fail("Cart diagnostic exceeded 45 seconds"); }
                    InvokeFixture("Place", position, aim * Quaternion.Euler(0, yaw, 0)); attempts++;
                    Vector3 body = cat.transform.position; Quaternion heading = cat.transform.rotation;
                    Vector3 flatCentre = centre; flatCentre.y = body.y;
                    bool clear = CatActivityStartResolver.Current(cat, flatCentre, .44f, out _);
                    int firstPlans = 0, facingPlans = 0, laterPlans = 0;
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    if (clear)
                    {
                        resolver.TryResolve(cat, Vector3.zero, out _, first =>
                        {
                            firstPlans++;
                            if (!CatActivityStartResolver.Facing(cat, flatCentre, .44f, first.Plan.Target, 70f, out _)) return false;
                            facingPlans++;
                            Vector3 away = renderer.bounds.center - first.Plan.Target;
                            Vector3 roll = Vector3.Dot(away, cart.WorldRollDirection) >= 0 ? cart.WorldRollDirection : -cart.WorldRollDirection;
                            for (int beat = 1; beat < cart.ShoveCount; beat++)
                                if (!resolver.TryResolve(cat, roll * (cart.RollDistance * beat / cart.ShoveCount), out _)) return false;
                            laterPlans++; return true;
                        });
                    }
                    bool ready = cart.TryGetStartPose(cat, out accepted); timer.Stop();
                    Row("candidate", attempts, clear, firstPlans, facingPlans, laterPlans, ready, timer.Elapsed.TotalMilliseconds);
                    Assert.That(Vector3.Distance(body, cat.transform.position), Is.LessThan(.00001f));
                    Assert.That(Quaternion.Angle(heading, cat.transform.rotation), Is.LessThan(.001f));
                    if (ready)
                    {
                        yield return new WaitForEndOfFrame();
                        if (cart.TryGetStartPose(cat, out accepted)) { found = true; break; }
                        Row("settled-refused", attempts);
                    }
                    if (attempts % 4 == 0) yield return null;
                }
            }
        }
        Assert.That(found, Is.True, "No current stance supports both actual contacts. CSV separates current, first, facing and later gates.");
        Assert.That(cart.TryGetPromptDistance(cat, out _), Is.True);
        Vector3 stand = cat.transform.position, home = cart.CartVisual.position; Quaternion rotation = cat.transform.rotation;
        int completed = 0; Action<CatActivity> onComplete = a => { if (a == cart) completed++; };
        CatActivity.Completed += onComplete;
        int previousStroke = 0, independentlyMeasured = 0; bool sawPaw = false, paused = false;
        float maximumTravel = 0, maximumRoot = 0, maximumYaw = 0, closest = float.PositiveInfinity;
        try
        {
            Assert.That(cart.TryStart(cat), Is.True);
            float deadline = Time.realtimeSinceStartup + 12;
            while (cart.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                maximumRoot = Mathf.Max(maximumRoot, Vector3.Distance(stand, cat.transform.position));
                maximumYaw = Mathf.Max(maximumYaw, Quaternion.Angle(rotation, cat.transform.rotation));
                maximumTravel = Mathf.Max(maximumTravel, Vector3.Distance(home, cart.CartVisual.position));
                if (cart.IsPushing)
                {
                    var left = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-hand.L");
                    var right = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-hand.R");
                    float distance = Mathf.Min(Vector3.Distance(left.position, cart.ContactPoint), Vector3.Distance(right.position, cart.ContactPoint));
                    Assert.That(set.TryClosest(cart.ContactPoint, out var actual), Is.True);
                    Assert.That(Vector3.Distance(actual.Point, cart.ContactPoint), Is.LessThan(.0005f), "The selected target is on a real triangle");
                    closest = Mathf.Min(closest, distance); sawPaw |= distance < .028f;
                    if (sawPaw && !paused)
                    {
                        paused = true; Time.timeScale = 0;
                        Vector3 pawL = left.position, pawR = right.position, prop = cart.CartVisual.position;
                        int strokes = cart.ContactStrokes;
                        for (int frame = 0; frame < 4; frame++)
                        {
                            yield return new WaitForEndOfFrame();
                            Assert.That(Vector3.Distance(pawL, left.position), Is.LessThan(.001f));
                            Assert.That(Vector3.Distance(pawR, right.position), Is.LessThan(.001f));
                            Assert.That(Vector3.Distance(prop, cart.CartVisual.position), Is.LessThan(.00001f));
                            Assert.That(cart.ContactStrokes, Is.EqualTo(strokes));
                        }
                        Time.timeScale = 1;
                    }
                }
                if (cart.ContactStrokes > previousStroke)
                {
                    Assert.That(cart.ContactStrokes, Is.EqualTo(previousStroke + 1));
                    Assert.That(sawPaw, Is.True, "Each counted stroke needs independently observed real hand-to-mesh contact");
                    independentlyMeasured++; previousStroke = cart.ContactStrokes; sawPaw = false;
                    Row("actual-stroke", detail: "stroke=" + previousStroke + ";closest=" + closest);
                    closest = float.PositiveInfinity;
                }
            }
            Assert.That(cart.IsRunning, Is.False); Assert.That(completed, Is.EqualTo(1));
            Assert.That(cart.ContactStrokes, Is.EqualTo(cart.ShoveCount));
            Assert.That(independentlyMeasured, Is.EqualTo(cart.ShoveCount)); Assert.That(paused, Is.True);
            Assert.That(maximumRoot, Is.LessThan(.001f)); Assert.That(maximumYaw, Is.LessThan(.2f));
            Assert.That(maximumTravel, Is.GreaterThanOrEqualTo(cart.RollDistance - .002f));
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(cat.GetComponent<CharacterController>().enabled && !cat.IsMovementPhysicallyLocked, Is.True);
            Row("completed", ready: true, detail: "contacts=" + independentlyMeasured + ";travel=" + maximumTravel + ";root=" + maximumRoot + ";yaw=" + maximumYaw);
        }
        finally { Time.timeScale = 1; CatActivity.Completed -= onComplete; }
    }
}
