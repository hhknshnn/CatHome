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
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Test-only native measurement. Imports no geometry, changes no production limits.
public sealed class KitchenCareSolverDiagnosticTests
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const int MeasuredMealFrame = 224;
    CareAlignmentPolishTests fixture;
    bool prepared;
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory",
        "Docs/QA/INTERACTION_POLISH_2026-09-16");
    static T Read<T>(object instance, string name) => (T)instance.GetType().GetField(name, Fields).GetValue(instance);
    static object Call(object instance, string name, params object[] args) =>
        instance.GetType().GetMethod(name, Fields).Invoke(instance, args);
    [SetUp] public void Before()
    {
        fixture = new CareAlignmentPolishTests(); fixture.Before(); prepared = true;
    }
    [TearDown] public void After()
    { if (prepared) fixture.After(); prepared = false; }

    [UnityTest, Timeout(120000)]
    public IEnumerator TwoBreeds_ActualMeal_Frame224_All33BoundedShoulderCandidates()
    {
        var candidates = new List<string> {
            "breed,mealFrame,nativeFrame,choice,pitch,productionVisited,planted,goal,food,sourceTorso,sourceNeck0,sourceNeck1,sourceNeck2,sourceMouth,sourceRequiredReach,chainLength,postTorsoNeck0,postTorsoRequiredReach,postTorsoDeficit,goalDistance,lowestMouthFoodDistance,nearestActualMouth,totalNeck,leftPawError,rightPawError"
        };
        var legs = new List<string> {
            "breed,mealFrame,nativeFrame,choice,pitch,side,upper,lower,distance,rawMaximumMargin,maximumMarginWithEpsilon,minimumMarginWithEpsilon,bendSqr,plantResult,reason,pawError,arm,fore,handBeforePlant,pawTarget,sourceElbow"
        };
        var states = new List<string> {
            "breed,event,mealFrame,nativeFrame,root,yaw,food,weight,actualMouth,headDistance,minimumDistance,selectedTorso,selectedNeck,sourceRequiredReach,reachedRequiredReach,chainLength,sourceDeficit,pawError,completions"
        };
        try
        {
            yield return (IEnumerator)Call(fixture, "Home");
            yield return (IEnumerator)Call(fixture, "Room", HomeRoomService.KitchenId);
            foreach (string breed in new[] { "oriental-shorthair", "persian" })
            {
                yield return (IEnumerator)Call(fixture, "Breed", breed);
                Call(fixture, "ReadyNeeds");
                var cat = Read<CatMovement>(fixture, "cat");
                var meal = CatActivity.Registered.OfType<MealTimeActivity>().Single(a => a.gameObject.scene == cat.gameObject.scene);
                var target = meal.BowlPoint;
                Call(fixture, "FindCareStance", target, Read<Transform>(meal, "standPoint"),
                    (Func<bool>)(() => meal.TryGetPromptDistance(cat, out _)));
                var bowls = cat.GetComponent<BowlInteraction>();
                Call(bowls, "Update");
                var prompt = Object.FindAnyObjectByType<ActivityPromptController>();
                Call(prompt, "RefreshImmediate");
                Assert.That(Read<CatActivity>(prompt, "candidate"), Is.SameAs(meal));
                var button = Read<Button>(prompt, "actionButton");
                Assert.That(button != null && button.isActiveAndEnabled && button.interactable, Is.True);
                int completions = 0;
                Action<CatActivity> complete = activity => { if (activity == meal) completions++; };
                CatActivity.Completed += complete;
                bool measured = false;
                try
                {
                    button.onClick.Invoke();
                    Assert.That(meal.IsRunning && meal.IsEating, Is.True);
                    int frame = 0; float deadline = Time.realtimeSinceStartup + 15f;
                    while (meal.IsRunning && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame(); frame++;
                        if (frame != MeasuredMealFrame) continue;
                        var head = cat.GetComponent<CatMealHeadMotion>();
                        Assert.That(head != null && head.IsActive && meal.IsEating, Is.True);
                        Assert.That(Read<float>(head, "weight"), Is.GreaterThanOrEqualTo(.999f));
                        AddState(states, breed, "before", frame, cat, target, head, completions);
                        MeasureCandidates(breed, frame, cat, target, head, candidates, legs);
                        AddState(states, breed, "restored", frame, cat, target, head, completions);
                        measured = true;
                    }
                    Assert.That(measured, Is.True, "The unchanged full meal must reach the observed source frame.");
                    Assert.That(meal.IsRunning, Is.False, "Reach failure still releases the action normally.");
                    AddState(states, breed, "final", frame, cat, target, cat.GetComponent<CatMealHeadMotion>(), completions);
                }
                finally { CatActivity.Completed -= complete; CatActionState.CancelForTransition(cat); }
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            }
            Assert.That(candidates.Count - 1, Is.EqualTo(66));
            Assert.That(legs.Count - 1, Is.EqualTo(132));
        }
        finally
        {
            Directory.CreateDirectory(Output);
            File.WriteAllLines(Path.Combine(Output, "kitchen-care-shoulder-candidates.csv"), candidates);
            File.WriteAllLines(Path.Combine(Output, "kitchen-care-shoulder-legs.csv"), legs);
            File.WriteAllLines(Path.Combine(Output, "kitchen-care-shoulder-states.csv"), states);
        }
    }

    void AddState(List<string> rows, string breed, string stage, int frame,
        CatMovement cat, Transform target, CatMealHeadMotion head, int completions)
    {
        Vector3 outside = cat.transform.position - target.position; outside.y = 0;
        Vector3 goal = target.position + Vector3.up * .025f + outside.normalized * .025f;
        rows.Add(Csv(breed, stage, frame, Time.frameCount, cat.transform.position, cat.transform.eulerAngles.y,
            target.position, head != null ? Read<float>(head, "weight") : 0f,
            (float)Call(fixture, "ActualMouthDistance", target), head != null ? head.Distance : float.NaN,
            head != null ? head.MinimumDistance : float.NaN, head != null ? head.ForequarterDeflection : float.NaN,
            head != null ? head.TotalDeflection : float.NaN, head != null ? head.RequiredReach : float.NaN,
            head != null ? Vector3.Distance(head.ReachedNeckBasePosition, goal) : float.NaN,
            head != null ? head.PhysicalChainLength : float.NaN, head != null ? head.ReachDeficit : float.NaN,
            head != null ? head.PawPlantError : float.NaN, completions));
    }

    void MeasureCandidates(string breed, int mealFrame, CatMovement cat, Transform target,
        CatMealHeadMotion head, List<string> rows, List<string> legRows)
    {
        // Everything runs synchronously in this exact native EndOfFrame. Restore
        // the original winning live pose even if any reflection/assertion fails.
        int nativeFrame = Time.frameCount;
        var transforms = cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        var positions = transforms.Select(t => t.localPosition).ToArray();
        var rotations = transforms.Select(t => t.localRotation).ToArray();
        var scales = transforms.Select(t => t.localScale).ToArray();
        Vector3 root = cat.transform.position; Quaternion yaw = cat.transform.rotation;
        float nativeActual = (float)Call(fixture, "ActualMouthDistance", target);
        var torso = Read<Transform>(head, "torso");
        var joints = Read<Transform[]>(head, "joints");
        var sources = Read<Quaternion[]>(head, "sourcePose");
        var forelegs = Read<Array>(head, "forelegs");
        float weight = Read<float>(head, "weight");
        Vector3 outside = root - target.position; outside.y = 0;
        Vector3 goal = target.position + Vector3.up * .025f + outside.normalized * .025f;
        bool productionContinues = true;
        try
        {
            Call(head, "RestoreFrame");
            object endpoint = Call(head, "LowestMouth");
            Vector3 sourceMouth = (Vector3)Call(endpoint, "World");
            Vector3 sourceTorso = torso.position, source0 = joints[0].position,
                source1 = joints[1].position, source2 = joints[2].position;
            float chain = Vector3.Distance(source0, source1) + Vector3.Distance(source1, source2) + Vector3.Distance(source2, sourceMouth);
            for (int choice = 0; choice <= 32; choice++)
            {
                Call(head, "RestoreFrame");
                int step = (choice + 1) / 2 * (choice % 2 == 0 ? -1 : 1);
                float pitch = CatMealHeadMotion.MaximumForequarterDeflection * weight * step / 16;
                torso.rotation = Quaternion.AngleAxis(pitch, cat.transform.right) * torso.rotation;
                bool planted = true; var pawErrors = new float[2];
                for (int side = 0; side < 2; side++)
                {
                    object limb = forelegs.GetValue(side);
                    var arm = Read<Transform>(limb, "arm"); var fore = Read<Transform>(limb, "fore"); var hand = Read<Transform>(limb, "hand");
                    Vector3 paw = Read<Vector3>(limb, "pawPosition"), elbow = Read<Vector3>(limb, "elbowPosition");
                    Vector3 armBefore = arm.position, foreBefore = fore.position, handBefore = hand.position;
                    Vector3 delta = paw - armBefore; float distance = delta.magnitude;
                    float upper = Vector3.Distance(armBefore, foreBefore), lower = Vector3.Distance(foreBefore, handBefore);
                    float rawMargin = upper + lower - distance;
                    float maxMargin = rawMargin - .00001f, minMargin = distance - Mathf.Abs(upper - lower) - .00001f;
                    Vector3 direction = distance > 0 ? delta / distance : Vector3.zero;
                    Vector3 bend = Vector3.ProjectOnPlane(elbow - armBefore, direction);
                    if (bend.sqrMagnitude < .00000001f) bend = Vector3.ProjectOnPlane(foreBefore - armBefore, direction);
                    bool result = (bool)Call(limb, "Plant"); planted &= result;
                    float error = Vector3.Distance(hand.position, paw); pawErrors[side] = error;
                    string reason = result ? "accepted" : minMargin < 0 ? "inside-minimum-chain" :
                        maxMargin < 0 ? "beyond-maximum-chain" : bend.sqrMagnitude < .00000001f ? "undefined-bend-plane" : "residual-paw-error";
                    legRows.Add(Csv(breed, mealFrame, nativeFrame, choice, pitch, side == 0 ? "L" : "R", upper, lower, distance,
                        rawMargin, maxMargin, minMargin, bend.sqrMagnitude, result, reason, error, armBefore, foreBefore, handBefore, paw, elbow));
                }
                Vector3 postBase = joints[0].position;
                float postRequired = Vector3.Distance(postBase, goal), goalDistance = float.NaN,
                    foodDistance = float.NaN, actual = float.NaN, neckAngle = float.NaN;
                bool visited = productionContinues;
                if (planted)
                {
                    Call(head, "SolveNeck", endpoint, goal);
                    Vector3 lowest = (Vector3)Call(Call(head, "LowestMouth"), "World");
                    goalDistance = Vector3.Distance(lowest, goal); foodDistance = Vector3.Distance(lowest, target.position);
                    actual = (float)Call(fixture, "ActualMouthDistance", target);
                    neckAngle = 0; for (int i = 0; i < joints.Length; i++) neckAngle += Quaternion.Angle(sources[i], joints[i].localRotation);
                    if (goalDistance < .009f) productionContinues = false;
                }
                rows.Add(Csv(breed, mealFrame, nativeFrame, choice, pitch, visited, planted, goal, target.position,
                    sourceTorso, source0, source1, source2, sourceMouth, Vector3.Distance(source0, goal), chain,
                    postBase, postRequired, Mathf.Max(0, postRequired - chain), goalDistance, foodDistance, actual, neckAngle, pawErrors[0], pawErrors[1]));
            }
        }
        finally
        {
            for (int i = 0; i < transforms.Length; i++)
            { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
        }
        Assert.That(Time.frameCount, Is.EqualTo(nativeFrame), "All candidates must use exactly one captured source frame.");
        Assert.That(Vector3.Distance(root, cat.transform.position), Is.LessThan(.000001f));
        Assert.That(Quaternion.Angle(yaw, cat.transform.rotation), Is.LessThan(.001f));
        Assert.That((float)Call(fixture, "ActualMouthDistance", target), Is.EqualTo(nativeActual).Within(.000002f),
            "Candidate inspection must restore the production winning pose before the next native frame.");
    }

    static string Csv(params object[] values) => string.Join(",", values.Select(value =>
    {
        string text = value is Vector3 vector ? FormattableString.Invariant($"{vector.x:F7};{vector.y:F7};{vector.z:F7}") :
            value is IFormattable formatted ? formatted.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? "";
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }));
}
#endif
