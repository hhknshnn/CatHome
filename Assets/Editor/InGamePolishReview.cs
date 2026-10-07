using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Finite, read-only presentation cases on a copied player save.</summary>
public static class InGamePolishReview
{
    public static string Root
    {
        get => SessionState.GetString("CatHome.PopupReview.Output", "Docs/QA/IN_GAME_POLISH_2026-10-05");
        set => SessionState.SetString("CatHome.PopupReview.Output", value);
    }
    public static string Status { get; private set; } = "Idle";
    sealed class Shot { public string view, name; public int width; public GameLanguage language; }
    static Queue<Shot> shots;
    static Shot current;
    static int phase, completed;
    static double next;
    static GameLanguage originalLanguage;
    static readonly List<string> reports = new List<string>();
    public static void Start()
    {
        if (!Application.isPlaying || !EditorQaSession.IsActive) throw new InvalidOperationException("Copied-save Play required.");
        originalLanguage = GameLanguageService.Current;
        UiQaVisualTour.Find<PetTutorialHint>().enabled = false;
        UiQaVisualTour.Find<WhileYouWereAwayPopup>().enabled = false;
        UiQaVisualTour.Find<CollectionCompleteCelebrationView>().enabled = false;
        shots = new Queue<Shot>(); reports.Clear(); completed = 0;
        string[] all = { "Title", "Credits", "NewGame", "AccountChoice", "Menu", "ShopRoom", "ShopCat", "ShopHome", "ShopRoomBottom", "ShopCatBottom", "ShopHomeBottom", "MyCat", "MyCatBottom", "Rooms", "RoomsBottom", "Settings", "Privacy", "DeleteConfirmation", "Quests", "Dailies", "Purchase", "Prerequisite", "DiamondConfirmation", "DiamondPacks", "Games", "LeaderboardEmpty", "LeaderboardExample", "ReturnExample", "LevelUpExample", "CollectionExample", "OnboardingExample", "DialogueExample", "NameExample", "Companion", "Guide0", "Guide1", "Guide2", "Guide3", "Guide4", "Guide5" };
        foreach (var lang in new[] { GameLanguage.Turkish, GameLanguage.English })
        {
            foreach (int width in new[] { 1920, 848 }) foreach (string view in all) Add(view, width, lang);
            foreach (int width in new[] { 1440, 2400 }) foreach (string view in new[] { "Menu", "ShopRoom", "MyCat", "Rooms", "Quests", "Settings", "Games", "Companion" }) Add(view, width, lang);
        }
        phase = 0; next = 0; Status = "Starting";
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    static void Add(string view, int width, GameLanguage language)
    { shots.Enqueue(new Shot { view=view, width=width, language=language, name=(language==GameLanguage.Turkish?"TR":"EN")+"-"+width+"-"+view }); }
    static void Tick()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Tick; Status="Stopped"; return; }
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (phase == 0)
            {
                UiQaVisualTour.Find<CatCompanionPanel>()?.Close(); UiQaVisualTour.Clear();
                if (shots.Count == 0)
                {
                    GameLanguageService.SetLanguage(originalLanguage); UiQaVisualTour.Resolution(1920,1080);
                    EditorApplication.update -= Tick; Status="Complete: "+completed; return;
                }
                current=shots.Dequeue(); Status=current.name+" ("+completed+" done)";
                GameLanguageService.SetLanguage(current.language);
                UiQaVisualTour.Resolution(current.width,current.width==848?392:1080);
                phase=1; next=EditorApplication.timeSinceStartup+.5;
            }
            else if (phase == 1)
            {
                if (current.view == "Companion") UiQaVisualTour.Find<CatCompanionPanel>().Show(false);
                else if (current.view.StartsWith("Guide"))
                {
                    var companion=UiQaVisualTour.Find<CatCompanionPanel>();
                    UiQaVisualTour.Set(companion,"page",int.Parse(current.view.Substring(5))); companion.Show(true);
                }
                else UiQaVisualTour.Show(current.view.Replace("Bottom",""));
                phase=2; next=EditorApplication.timeSinceStartup+1.3;
            }
            else if (phase == 2)
            {
                if (current.view.EndsWith("Bottom"))
                {
                    Transform root=current.view.StartsWith("Shop")?UiQaVisualTour.Find<ShopPanelController>().transform:
                        current.view.StartsWith("MyCat")?UiQaVisualTour.Find<CatBreedShopPanel>().transform:UiQaVisualTour.Find<RoomSelectorPanel>().transform;
                    foreach (var scroll in root.GetComponentsInChildren<ScrollRect>()) { scroll.StopMovement(); scroll.verticalNormalizedPosition=0; }
                }
                phase=3; next=EditorApplication.timeSinceStartup+.2;
            }
            else
            {
                UiQaVisualTour.OutputDirectory=Root+"/screens";
                UiQaVisualTour.Capture(current.name);
                var issues=new List<string>();
                foreach (var label in UnityEngine.Object.FindObjectsByType<TMP_Text>())
                {
                    if (!label.isActiveAndEnabled || label.color.a<.05f) continue;
                    float alpha=1; foreach (var g in label.GetComponentsInParent<CanvasGroup>()) alpha*=g.alpha;
                    if (alpha<.05f) continue;
                    var corners=new Vector3[4]; label.rectTransform.GetWorldCorners(corners);
                    var center=(corners[0]+corners[2])*.5f;
                    if (label.GetComponentsInParent<RectMask2D>().Any(mask=>!RectTransformUtility.RectangleContainsScreenPoint(mask.rectTransform,center))) continue;
                    label.ForceMeshUpdate();
                    if (label.isTextTruncated || label.isTextOverflowing) issues.Add(label.name);
                }
                reports.Add(current.name+" text="+string.Join(",",issues.Distinct()));
                File.WriteAllLines(Root+"/visual-review.txt",reports);
                completed++; phase=0; next=EditorApplication.timeSinceStartup+.2;
            }
        }
        catch (Exception e) { Status="Failed: "+e; File.WriteAllText(Root+"/visual-error.txt",Status); EditorApplication.update-=Tick; }
    }
}
