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

public sealed class PhoneWorldInteractionRegressionTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    CareAlignmentPolishTests home;
    GameObject wall, corner;
    Mesh baked;
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Docs/QA/OVERNIGHT_FIX_POLISH_2026-09-27");
    static T Read<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Private).Invoke(owner, args);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
    CatMovement Cat => Read<CatMovement>(home, "cat");
    [SetUp] public void Before() { home = new CareAlignmentPolishTests(); home.Before(); baked = new Mesh(); }
    [TearDown] public void After()
    {
        var stick = Object.FindAnyObjectByType<MobileJoystick>(); if (stick != null) stick.CancelInput();
        if (corner != null) Object.DestroyImmediate(corner); if (wall != null) Object.DestroyImmediate(wall); if (baked != null) Object.DestroyImmediate(baked);
        home?.After();
    }
    static void Input(MobileJoystick stick, Vector2 direction) => typeof(MobileJoystick).GetProperty("Direction").SetValue(stick, direction);

    [UnityTest, Timeout(240000)]
    public IEnumerator SideInputAfterWallContact_AllRatesCanTurnAndLeaveWithoutBodyClipping()
    {
        yield return (IEnumerator)Call(home, "Home");
        wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "QA tangent escape wall";
        wall.transform.position = new Vector3(0, .5f, 0); wall.transform.localScale = new Vector3(3f, 1f, .1f);
        var collider = wall.GetComponent<BoxCollider>();
        var stick = Object.FindAnyObjectByType<MobileJoystick>(); Set(Cat, "cameraTransform", null);
        var rows = new List<string> { "breed,fps,input,travelAlongWall,rootTravel,maxBodyDepth,finalYaw" };
        var failures = new List<string>();
        foreach (string breed in new[] { "russian-blue", "persian", "maine-coon" })
        {
            yield return (IEnumerator)Call(home, "Breed", breed);
            foreach (int fps in new[] { 20, 30, 60 })
            foreach (float side in new[] { -1f, 1f })
            {
                Time.captureFramerate = fps; Call(home, "ReadyNeeds");
                Read<EnergySystem>(home, "energy").ApplySavedValue(100);
                Input(stick, Vector2.zero); Call(home, "Place", new Vector3(0, .05f, -1.3f), Quaternion.identity);
                Set(Cat, "verticalVelocity", 0f); Physics.SyncTransforms(); yield return null;
                Input(stick, Vector2.up); for (int i = 0; i < fps * 2; i++) yield return new WaitForEndOfFrame();
                Vector3 stopped = Cat.transform.position; float depth = 0;
                Input(stick, new Vector2(side, 0));
                for (int i = 0; i < fps * 2; i++)
                {
                    yield return new WaitForEndOfFrame();
                    if (i % Math.Max(1, fps / 20) == 0) depth = Mathf.Max(depth, Depth(collider));
                }
                Input(stick, Vector2.zero);
                Vector3 moved = Cat.transform.position - stopped;
                rows.Add(Csv(breed, fps, side, moved.x * side, moved.magnitude, depth, Cat.transform.eulerAngles.y));
                File.WriteAllLines(Path.Combine(Output, "wall-side-escape.csv"), rows);
                if (moved.x * side < .25f || depth > .015f) failures.Add(rows[rows.Count - 1]);
            }
        }
        Assert.That(failures, Is.Empty, "A tangent joystick command must leave the wall: " + string.Join("\n", failures));
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator PerpendicularInputInsideCorner_MustBackOutThenFollowTheRequestedDirection()
    {
        yield return (IEnumerator)Call(home, "Home");
        wall=GameObject.CreatePrimitive(PrimitiveType.Cube); corner=GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position=new Vector3(-.7f,.5f,.05f);wall.transform.localScale=new Vector3(1.5f,1f,.1f);
        corner.transform.position=new Vector3(.05f,.5f,-.7f);corner.transform.localScale=new Vector3(.1f,1f,1.5f);
        var stick=Object.FindAnyObjectByType<MobileJoystick>();Set(Cat,"cameraTransform",null);
        var rows=new List<string>{"breed,fps,side,travel,alongRequested,depth,blockedTurnFrames"};var failures=new List<string>();
        foreach(string breed in new[]{"russian-blue","persian","maine-coon"})
        {
            yield return (IEnumerator)Call(home,"Breed",breed);
            foreach(int fps in new[]{20,30,60}) foreach(float side in new[]{-1f,1f})
            {
                Time.captureFramerate=fps;Call(home,"ReadyNeeds");Read<EnergySystem>(home,"energy").ApplySavedValue(100);
                Input(stick,Vector2.zero);Call(home,"Place",new Vector3(-.85f,.05f,-.85f),Quaternion.Euler(0,45,0));Set(Cat,"verticalVelocity",0f);yield return null;
                Input(stick,Vector2.one.normalized);for(int i=0;i<fps*2;i++)yield return new WaitForEndOfFrame();
                Vector3 stopped=Cat.transform.position;Vector2 desired=new Vector2(side,-side).normalized;
                Input(stick,desired);float depth=0;int blocked=0;
                for(int i=0;i<fps*3;i++)
                {
                    yield return new WaitForEndOfFrame();if(Read<bool>(Cat,"collisionLimitedTurn"))blocked++;
                    if(i%Math.Max(1,fps/20)==0)depth=Mathf.Max(depth,Depth(wall.GetComponent<BoxCollider>()),Depth(corner.GetComponent<BoxCollider>()));
                }
                Input(stick,Vector2.zero);Vector3 moved=Cat.transform.position-stopped;
                float along=Vector3.Dot(moved,new Vector3(desired.x,0,desired.y));rows.Add(Csv(breed,fps,side,moved.magnitude,along,depth,blocked));
                if(moved.magnitude<.25f||along<.15f||depth>.015f)failures.Add(rows[rows.Count-1]);
            }
        }
        File.WriteAllLines(Path.Combine(Output,"corner-perpendicular-escape.csv"),rows);
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }

    float Depth(BoxCollider box)
    {
        var skin = Cat.GetComponentInChildren<SkinnedMeshRenderer>(); skin.BakeMesh(baked, true);
        var vertices = baked.vertices; var indices = CatBreedCatalog.Load().Find(CatBreedService.SelectedBreedId).ContactVertexIndices;
        float depth = 0;
        foreach (int index in indices)
        {
            Vector3 point = box.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index])) - box.center;
            Vector3 inside = box.size * .5f - new Vector3(Mathf.Abs(point.x), Mathf.Abs(point.y), Mathf.Abs(point.z));
            if (inside.x < 0 || inside.y < 0 || inside.z < 0) continue;
            Vector3 scale = box.transform.lossyScale;
            depth = Mathf.Max(depth, Mathf.Min(inside.x * Mathf.Abs(scale.x), Mathf.Min(inside.y * Mathf.Abs(scale.y), inside.z * Mathf.Abs(scale.z))));
        }
        return depth;
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator Sofa_FirstApproachShowsNapAtAnyHeading_ButStaleOrUnsafeClickCannotMoveCat()
    {
        yield return (IEnumerator)Call(home, "Home");
        var sofa = CatActivity.Registered.OfType<LivingFurnitureActivity>().Single(a => a.Kind == CatActivityKind.SofaLounge && a.gameObject.scene == Cat.gameObject.scene);
        var prompt = Object.FindAnyObjectByType<ActivityPromptController>();
        Vector3 outward = sofa.RoutineEntryPoint.position - sofa.Perch.position; outward.y = 0; outward.Normalize();
        Vector3 near = sofa.RoutineEntryPoint.position + outward * .45f;
        foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
        {
            Call(home, "Place", near, Quaternion.LookRotation(-outward) * Quaternion.Euler(0, yaw, 0));
            yield return null; yield return null;
            bool offered=sofa.TryGetPromptDistance(Cat,out float offeredDistance);
            Assert.That(Read<CatActivity>(prompt, "candidate"), Is.SameAs(sofa), "Nearby discoverable first approach, yaw " + yaw+
                " offered="+offered+" distance="+offeredDistance+" radius="+sofa.InteractionRadius+" visible="+CareInteractionTarget.IsVisibleInRoom(sofa.SelectionVisual,Cat.transform)+
                " square="+CareInteractionTarget.NearbyDistanceSquared(sofa.RoutineEntryPoint,Cat.transform,.75f)+" cat="+Cat.transform.position+" entry="+sofa.RoutineEntryPoint.position);
            var button = Read<Button>(prompt, "actionButton");
            Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.True);
            Vector3 before = Cat.transform.position; Quaternion heading = Cat.transform.rotation;
            // Check the click atomically. This deliberately unsafe heading
            // may be depenetrated by the controller on a later normal frame;
            // that movement must not be attributed to the refused action.
            button.onClick.Invoke();
            Assert.That(sofa.IsRunning, Is.False, "The nearby offer does not bypass the precise launch check.");
            Assert.That(Vector3.Distance(before, Cat.transform.position), Is.LessThan(.002f));
            Assert.That(Quaternion.Angle(heading, Cat.transform.rotation), Is.LessThan(.01f));
        }
        Call(home, "Place", sofa.RoutineEntryPoint.position + outward * 2f, Quaternion.identity);
        yield return null; yield return null;
        Assert.That(Read<CatActivity>(prompt, "candidate"), Is.Not.SameAs(sofa));
    }
    static string Csv(params object[] values) => string.Join(",", values.Select(value => value is IFormattable n ? n.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? ""));
}
#endif
