using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Finite presentation tour on the isolated QA copy; never invokes a reward, purchase or reset action.</summary>
public static class JoyfulVisualQa
{
    public static string RootDirectory = "Docs/QA/JOYFUL_ARCADE_2026-09-08";
    public static string Status { get; private set; } = "Idle";
    public static bool IsRunning { get; private set; }
    public static int CapturedCount { get; private set; }
    public static int CompletedCount { get; private set; }
    public static int TotalCount { get; private set; }

    private sealed class Shot
    {
        public int Width, Page = -1;
        public string Name, View;
        public bool Bottom;
    }

    private static Queue<Shot> shots;
    private static Shot current;
    private static int phase, previousWidth, previousHeight;
    private static double next, deadline, started;
    private static string previousOutput, screenshot;
    private static float previousTimeScale;
    private static readonly StringBuilder log = new StringBuilder();
    private static PetTutorialHint tutorial;
    private static CollectionCompleteCelebrationView collection;
    private static WhileYouWereAwayPopup away;
    private static CatCompanionPanel companion;
    private static bool tutorialEnabled, collectionEnabled, awayEnabled;

    public static void Begin()
    {
        if (IsRunning) throw new InvalidOperationException("The Joyful UI tour is already running.");
        if (!Application.isPlaying || !EditorQaSession.IsActive)
            throw new InvalidOperationException("Use an isolated editor Play session.");
        if (HomeUiFlow.IsMiniGameVisible || CatActivity.Active != null || HomeEditModeController.IsAnyOpen)
            throw new InvalidOperationException("Return to the home and finish the active activity/edit mode first.");
        if (!PetTutorialHint.IsOnboardingCompleted)
            throw new InvalidOperationException("Use an established-player QA copy for this tour; it does not change onboarding preferences.");

        Required<CatMovement>();
        tutorial = Required<PetTutorialHint>();
        collection = Required<CollectionCompleteCelebrationView>();
        away = Required<WhileYouWereAwayPopup>();
        companion = Required<CatCompanionPanel>();
        Required<TitleScreen>(); Required<MainPanelController>(); Required<ShopPanelController>();
        Required<RoomSelectorPanel>(); Required<GamesHubPanel>(); Required<HomeLevelUpCelebrationView>();
        Required<OnboardingCelebrationView>(); Required<CatDialogueView>();
        Directory.CreateDirectory(RootDirectory);

        previousWidth = Screen.width; previousHeight = Screen.height;
        previousOutput = UiQaVisualTour.OutputDirectory;
        previousTimeScale = Time.timeScale;
        tutorialEnabled = tutorial.enabled; collectionEnabled = collection.enabled; awayEnabled = away.enabled;
        tutorial.enabled = false;
        // Suppress automatic presentation without consuming queued collection or
        // offline rewards; the views are opened directly with example copy only.
        collection.enabled = false;
        away.enabled = false;
        Time.timeScale = 1f;
        shots = new Queue<Shot>();
        foreach (int width in new[] { 1920, 1440 }) AddShots(width);
        TotalCount = shots.Count; CompletedCount = CapturedCount = 0;
        log.Clear(); log.AppendLine("Joyful UI native screenshots; example rewards shown, none claimed.");
        log.AppendLine("Inputs and scrolling are verified separately by native tests; bottom views here use ScrollRect positioning.");
        IsRunning = true; started = EditorApplication.timeSinceStartup;
        SetPhase(0, 0);
        Status = "Starting";
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void AddShots(int width)
    {
        Add(width, "01_Title", "Title"); Add(width, "02_NewGame", "NewGame");
        Add(width, "03_HomeHUD", "HomeHUD"); Add(width, "04_Menu", "Menu");
        Add(width, "05_WhileYouWereAway", "Return"); Add(width, "06_Collection", "Collection");
        Add(width, "07_LevelUp", "LevelUp"); Add(width, "08_Onboarding", "Onboarding");
        Add(width, "09_Name", "Name"); Add(width, "10_Dialogue", "Dialogue");
        Add(width, "11_Rooms", "Rooms"); Add(width, "12_Games", "Games");
        foreach (var item in new[] { ("13_ShopCat", "ShopCat"), ("14_ShopRoom", "ShopRoom"), ("15_ShopHome", "ShopHome") })
        { Add(width, item.Item1, item.Item2); Add(width, item.Item1 + "-Bottom", item.Item2, true); }
        Add(width, "16_Companion", "Companion");
        for (int page = 0; page < HomeGuideContent.Count; page++)
        {
            shots.Enqueue(new Shot { Width = width, Name = "Guide-" + (page + 1), View = "Guide", Page = page });
            shots.Enqueue(new Shot { Width = width, Name = "Guide-" + (page + 1) + "-Bottom", View = "Guide", Page = page, Bottom = true });
        }
    }

    private static void Add(int width, string name, string view, bool bottom = false)
    { shots.Enqueue(new Shot { Width = width, Name = name, View = view, Bottom = bottom }); }

    public static void Stop() { if (IsRunning) Finish("Stopped"); }

    private static void Tick()
    {
        if (!IsRunning) return;
        try
        {
            if (!Application.isPlaying || !EditorQaSession.IsActive) { Finish("Stopped: QA Play ended"); return; }
            double now = EditorApplication.timeSinceStartup;
            if (now - started > 600) throw new TimeoutException("The complete UI tour exceeded ten minutes.");
            if (now > deadline) throw new TimeoutException("Timed out at " + Status);
            if (now < next) return;
            if (phase == 0)
            {
                companion.Close(); UiQaVisualTour.Clear();
                if (shots.Count == 0) { Status = "Closing final view"; SetPhase(5, .65); return; }
                current = shots.Dequeue();
                UiQaVisualTour.OutputDirectory = RootDirectory + (current.Width == 1920 ? "/screens-wide" : "/screens-narrow");
                if (Screen.width != current.Width || Screen.height != 1080) UiQaVisualTour.Resolution(current.Width, 1080);
                Status = $"Preparing {CompletedCount + 1}/{TotalCount}: {current.Name} {current.Width}x1080";
                SetPhase(1, .65); return;
            }
            if (phase == 1)
            {
                if (Screen.width != current.Width || Screen.height != 1080 || HasOpenPopup()) return;
                PrepareCurrent();
                SetPhase(2, current.View == "Title" || current.View == "NewGame" ? 2.2 : 1.3);
                return;
            }
            if (phase == 2)
            {
                EnsureExpectedView();
                if (current.Bottom)
                {
                    var scroll = current.View == "Guide" ? UiQaVisualTour.Get<ScrollRect>(companion, "guideScroll") :
                        UiQaVisualTour.Get<ScrollRect>(Required<ShopPanelController>(), "productScrollRect");
                    Canvas.ForceUpdateCanvases();
                    if (scroll.content.rect.height <= scroll.viewport.rect.height + 1f)
                    {
                        log.AppendLine(current.Width + "\t" + current.Name + "\tNo overflow; top screenshot covers this page.");
                        CompletedCount++; SetPhase(0, 0); return;
                    }
                    scroll.StopMovement(); scroll.verticalNormalizedPosition = 0f;
                }
                SetPhase(3, current.Bottom ? .2 : 0); return;
            }
            if (phase == 3)
            {
                EnsureExpectedView();
                screenshot = Path.Combine(UiQaVisualTour.OutputDirectory, current.Name + ".png");
                Directory.CreateDirectory(UiQaVisualTour.OutputDirectory);
                // Delete only this tour's named old frame, so a stale PNG cannot
                // satisfy the asynchronous screenshot completion check.
                if (File.Exists(screenshot)) File.Delete(screenshot);
                UiQaVisualTour.Capture(current.Name);
                Status = $"Capturing {CompletedCount + 1}/{TotalCount}: {current.Name} {current.Width}x1080";
                SetPhase(4, .2); return;
            }
            if (phase == 4)
            {
                if (!ScreenshotReady(screenshot, current.Width, 1080)) return;
                log.AppendLine(current.Width + "\t" + current.Name + "\t" + screenshot);
                CapturedCount++; CompletedCount++;
                File.WriteAllText(Path.Combine(RootDirectory, "visual-tour.txt"), log.ToString());
                SetPhase(0, .1); return;
            }
            if (phase == 5)
            {
                if (HasOpenPopup()) return;
                Finish("Complete");
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Finish("Failed: " + Status + ": " + error.Message);
        }
    }

    private static void PrepareCurrent()
    {
        if (current.View == "HomeHUD") return;
        if (current.View == "Companion") { companion.Show(false); return; }
        if (current.View == "Guide")
        {
            companion.Show(true); UiQaVisualTour.Set(companion, "page", current.Page);
            UiQaVisualTour.Call(companion, "RefreshGuide"); return;
        }
        UiQaVisualTour.Show(current.View);
    }

    private static void EnsureExpectedView()
    {
        bool visible;
        switch (current.View)
        {
            case "HomeHUD": visible = !HasOpenPopup(); break;
            case "Title": visible = TitleScreen.IsShowing; break;
            case "NewGame": visible = UiQaVisualTour.Get<bool>(Required<TitleScreen>(), "newGameOpen"); break;
            case "Menu": visible = Required<MainPanelController>().IsOpen; break;
            case "Return": visible = away.IsOpen; break;
            case "Collection": visible = CollectionCompleteCelebrationView.IsAnyOpen; break;
            case "LevelUp": visible = HomeLevelUpCelebrationView.IsAnyOpen; break;
            case "Onboarding": visible = OnboardingCelebrationView.IsAnyOpen; break;
            case "Name": case "Dialogue": visible = CatDialogueView.IsAnyVisible; break;
            case "Rooms": visible = RoomSelectorPanel.IsAnyOpen; break;
            case "Games": visible = GamesHubPanel.IsAnyOpen; break;
            case "ShopCat": case "ShopRoom": case "ShopHome": visible = ShopPanelController.IsAnyOpen; break;
            default: visible = CatCompanionPanel.IsAnyOpen; break;
        }
        if (!visible) throw new InvalidOperationException("The requested view did not open: " + current.View);
    }

    private static bool HasOpenPopup() => TitleScreen.IsShowing || HomeUiFlow.IsHomeControlBlocked ||
        Required<MainPanelController>().IsOpen || ShopPanelController.IsAnyOpen || RoomSelectorPanel.IsAnyOpen ||
        CatBreedShopPanel.IsAnyOpen || QuestPanelController.IsAnyOpen || SettingsPanel.IsAnyOpen ||
        PrivacyDataPanel.IsAnyOpen || GamesHubPanel.IsAnyOpen || LeaderboardPanel.IsAnyOpen;

    private static bool ScreenshotReady(string path, int width, int height)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < 24) return false;
        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var header = new byte[24]; if (stream.Read(header, 0, header.Length) != header.Length) return false;
                int actualWidth = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                int actualHeight = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                if (actualWidth != width || actualHeight != height)
                    throw new InvalidOperationException($"Captured {actualWidth}x{actualHeight}; required {width}x{height}.");
                return true;
            }
        }
        catch (IOException) { return false; }
    }

    private static void SetPhase(int value, double delay)
    { phase = value; next = EditorApplication.timeSinceStartup + delay; deadline = next + 12; }

    private static T Required<T>() where T : Component
    { var found = UiQaVisualTour.Find<T>(); if (found == null) throw new InvalidOperationException("Missing view: " + typeof(T).Name); return found; }

    private static void Finish(string result)
    {
        EditorApplication.update -= Tick;
        IsRunning = false;
        try
        {
            if (Application.isPlaying && EditorQaSession.IsActive) { companion?.Close(); UiQaVisualTour.Clear(); }
        }
        catch (Exception error) { result += " | Close failed: " + error.Message; }
        finally
        {
            try
            {
                if (tutorial != null) tutorial.enabled = tutorialEnabled;
                if (collection != null) collection.enabled = collectionEnabled;
                if (away != null) away.enabled = awayEnabled;
                Time.timeScale = previousTimeScale;
                UiQaVisualTour.OutputDirectory = previousOutput;
                if (Application.isPlaying && previousWidth > 0 && previousHeight > 0) UiQaVisualTour.Resolution(previousWidth, previousHeight);
            }
            catch (Exception error) { result += " | Restore failed: " + error.Message; }
            Status = result;
            log.AppendLine(result + $"; captured {CapturedCount}; completed {CompletedCount}/{TotalCount}.");
            try { File.WriteAllText(Path.Combine(RootDirectory, "visual-tour.txt"), log.ToString()); }
            catch (Exception error) { Status += " | Report failed: " + error.Message; }
        }
    }
}
