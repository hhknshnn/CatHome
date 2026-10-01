#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Measurement only: freeze an accepted real stool routine at its final supported
// StandUp, then query current collision gates without moving or changing the cat.
public sealed class CounterStoolLandingCandidateTests
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;
    CatActivity activity;
    readonly List<string> rows = new List<string>();
    static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Hidden).Invoke(target, args);
    static T Read<T>(object target, string name) =>
        (T)target.GetType().GetField(name, Hidden).GetValue(target);
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory",
        "Docs/QA/INTERACTION_POLISH_2026-09-16");
    static string Csv(params object[] values) => string.Join(",", values.Select(v => "\"" +
        (v is Vector3 p ? FormattableString.Invariant($"{p.x:R};{p.y:R};{p.z:R}") :
        v is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : v?.ToString() ?? "")
        .Replace("\"", "\"\"") + "\""));
    [SetUp] public void Before()
    {
        fixture = new PreparedInteractionStartTests(); fixture.Before();
        rows.Add("variant,angle,extension,root,target,heading,poseClear,boundaryClear,endsClear,rejectionPhase,rejectionRoot,elapsedMs,standPhase");
    }
    [UnityTearDown] public IEnumerator After()
    {
        if (activity != null && activity.IsRunning) activity.CancelForTransition();
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, "counter-stool-landing-candidates.csv"), rows);
        fixture?.After();
        yield return RoomPlayModeSupport.WaitForPendingContactData();
    }
    [UnityTest, Timeout(45000)]
    public IEnumerator CounterStool_LastStandUp_LandingCandidatesAreMeasuredBeforeDescent()
    {
        float deadline = Time.realtimeSinceStartup + 35f;
        yield return (IEnumerator)Call(fixture, "Prepare", "Kitchen_Level01");
        var cat = Read<CatMovement>(fixture, "cat");
        var stool = CatActivity.Registered.OfType<PerchNapActivity>().Single(a =>
            a.gameObject.scene == cat.gameObject.scene && a.StoreProductId == "kitchen.counter-stool");
        activity = stool;
        object[] args = { activity, default(CatActivityStart), null };
        Assert.That((bool)Call(fixture, "FindReadyPose", args), Is.True, (string)args[2]);
        yield return new WaitForEndOfFrame(); yield return new WaitForEndOfFrame();
        RoomPlayModeSupport.ProvisionNeeds(); Assert.That(activity.TryStart(cat), Is.True);
        var animation = cat.GetComponent<CatActivityAnimation>(); bool captured = false;
        while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            if (activity.IsWaitingForRestStop && activity.RestingSeconds >= .2f) activity.RequestRestStop();
            if (animation.CurrentPose != CatActivityPose.StandUp || animation.IsNativeJump ||
                animation.NativeJumpPhase < .94f) continue;
            captured = true; break;
        }
        Assert.That(captured, Is.True, "No actual final stool StandUp within diagnostic deadline.");
        activity.StopAllCoroutines();
        Assert.That(activity.TryGetPreparedStart(cat, out var accepted), Is.True);
        Vector3 from = cat.transform.position, preferred = stool.FloorPoint.position;
        preferred.y = accepted.Position.y;
        Quaternion originalRotation = cat.transform.rotation;
        Vector3 outward = Vector3.ProjectOnPlane(preferred - from, Vector3.up);
        float distance = outward.magnitude; outward.Normalize();
        Physics.SyncTransforms();
        var boundary = HomeRoomBoundary.FindFor(cat.gameObject.scene);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        bool resolved = CatActivityStartResolver.GroundLanding(cat, from, preferred,
            out var selected, out var selectedHeading);
        rows.Add(Csv("production", 0, 0, from, selected, selectedHeading.eulerAngles.y,
            resolved, resolved, resolved, 0, Vector3.zero, timer.Elapsed.TotalMilliseconds, animation.NativeJumpPhase));
        int clearCurrent = 0, clearAdditional = 0;
        foreach (float angle in new[] { 0f, -15f, 15f, -30f, 30f, -45f, 45f, -60f, 60f,
            -75f, 75f, -90f, 90f, -105f, 105f, -120f, 120f, -135f, 135f, -150f, 150f, -165f, 165f, 180f })
        {
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * outward;
            Quaternion heading = Quaternion.LookRotation(direction);
            for (int extension = 0; extension <= 6; extension++)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Bounded candidate diagnostic exceeded 35 seconds.");
                Vector3 candidate = from + direction * (distance + extension * .06f); candidate.y = preferred.y;
                timer.Restart();
                bool poseClear = cat.IsInteractionPoseClear(candidate, heading);
                bool boundaryClear = boundary == null || (candidate.x >= boundary.MinimumXZ.x &&
                    candidate.x <= boundary.MaximumXZ.x && candidate.z >= boundary.MinimumXZ.y && candidate.z <= boundary.MaximumXZ.y);
                var rejected = default(CatJumpClearanceResolver.Rejection);
                bool endsClear = poseClear && boundaryClear &&
                    CatJumpClearanceResolver.EndsClear(cat, from, candidate, heading, false, false, out rejected);
                rows.Add(Csv(Mathf.Abs(angle) <= 90 ? "current-halfplane" : "additional-halfplane", angle,
                    extension, from, candidate, heading.eulerAngles.y, poseClear, boundaryClear, endsClear,
                    rejected.phase, rejected.root, timer.Elapsed.TotalMilliseconds, animation.NativeJumpPhase));
                if (endsClear)
                {
                    if (Mathf.Abs(angle) <= 90) clearCurrent++; else clearAdditional++;
                    break;
                }
                if (poseClear && boundaryClear && rejected.phase <= CatJumpMotion.Takeoff) break;
            }
        }
        Assert.That(cat.transform.position, Is.EqualTo(from), "Read-only candidate queries moved the cat.");
        Assert.That(Quaternion.Angle(cat.transform.rotation, originalRotation), Is.LessThan(.001f), "Queries rotated the cat.");
        Assert.That(resolved, Is.EqualTo(clearCurrent > 0), "Production search differs from the same current candidate gates.");
        TestContext.WriteLine($"Measurement only; no completed cycle claimed. Production={resolved}; current valid headings={clearCurrent}; additional valid headings={clearAdditional}. Inspect counter-stool-landing-candidates.csv.");
    }
}
#endif
