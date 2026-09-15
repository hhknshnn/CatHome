using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using U=PremiumUiElements;

/// <summary>Read-only projection of the real player's saved collection into Edit Mode.</summary>
[InitializeOnLoad]
public static class CatHomeEditPreview
{
    static bool applying;
    static CatHomeEditPreview()
    {
        EditorApplication.playModeStateChanged+=state=>
        {if(state==PlayModeStateChange.ExitingEditMode||state==PlayModeStateChange.EnteredPlayMode)Clear();if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Refresh;};
        AssemblyReloadEvents.beforeAssemblyReload+=Clear;
        EditorSceneManager.sceneSaving+=(scene,path)=>Clear();
        EditorSceneManager.sceneSaved+=scene=>EditorApplication.delayCall+=Refresh;
        EditorApplication.delayCall+=Refresh;
    }
    public static void Clear()
    {
        // Fast Play / scene unloading can leave a preview in an invalid scene.
        foreach(var state in Resources.FindObjectsOfTypeAll<EditorHomePreviewState>().Where(s=>!EditorUtility.IsPersistent(s)))
        {state.Restore();UnityEngine.Object.DestroyImmediate(state.gameObject);}
    }
    public static void Refresh()
    {
        if(applying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorQaSession.IsActive||Application.isBatchMode)return;
        var room=SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
        var ui=SceneManager.GetSceneByPath(CatHomeAuthoringWorkspace.UiScenePath);
        if(!room.IsValid()||!room.isLoaded||!ui.IsValid()||!ui.isLoaded)return;
        string path=Path.Combine(Application.persistentDataPath,"cat-home-save.json");if(!File.Exists(path))return;
        applying=true;
        try
        {
            Clear();var save=JsonUtility.FromJson<CatHomeSaveData>(File.ReadAllText(path));if(save?.homeStore==null)return;
            var host=new GameObject("Editor · saved home preview");SceneManager.MoveGameObjectToScene(host,room);
            host.hideFlags=HideFlags.DontSaveInEditor|HideFlags.DontSaveInBuild;
            var state=host.AddComponent<EditorHomePreviewState>();state.CaptureStore();
            // Only service memory is populated. No Load/SaveNow, selection or purchase API is used.
            HomeStoreService.ApplySavedState(save.homeStore);
            foreach(var root in room.GetRootGameObjects())
            {
                foreach(var display in root.GetComponentsInChildren<StoreProductDisplay>(true))
                    state.Show(new SerializedObject(display).FindProperty("visualRoot").objectReferenceValue as GameObject,
                        HomeStoreService.IsOwned(display.ProductId)&&!HomeStoreService.IsStored(display.ProductId));
                foreach(var activity in root.GetComponentsInChildren<CatActivity>(true))
                {
                    var target=new SerializedObject(activity).FindProperty("unlockedContent").objectReferenceValue as GameObject;
                    bool visible=activity.RequiresStoreOwnership?HomeStoreService.IsOwned(activity.StoreProductId)&&!HomeStoreService.IsStored(activity.StoreProductId):save.bondXp>=activity.RequiredBondXp;
                    state.Show(target,visible);
                }
            }
            var planner=host.AddComponent<CatRoomArrangement>();
            var ids=HomeStoreService.Products.Where(p=>CatCollectionPolicy.IsCatItem(p.Id)&&HomeStoreService.IsOwned(p.Id)&&!HomeStoreService.IsStored(p.Id)).Select(p=>p.Id).ToArray();
            if(planner.TryPlan(ids,out var plan))
                foreach(var product in UnityEngine.Object.FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(product.gameObject.scene==room&&plan.TryGetValue(product.ProductId,out var pose))state.Place(product.MovableRoot,pose.position,Quaternion.Euler(0,pose.yaw,0));
            var cat=room.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatMovement>(true)).FirstOrDefault();
            var catalog=CatBreedCatalog.Load();
            if(cat!=null&&catalog!=null)
            {
                foreach(var renderer in cat.GetComponentsInChildren<Renderer>(true))state.HideRenderer(renderer);
                var visual=CatBreedVisualFactory.Create(CatBreedService.SelectedEntry,catalog.GameplayController,host.transform,"Saved cat appearance");
                visual.transform.SetPositionAndRotation(cat.transform.position,cat.transform.rotation);
                visual.transform.localScale=cat.transform.lossyScale;
                if(save.hasCatPose&&save.homeStore.currentRoomId==HomeRoomService.LivingRoomId)
                    visual.transform.SetPositionAndRotation(save.catWorldPosition,save.catWorldRotation);
                var animator=visual.GetComponentInChildren<Animator>();
                var idle=catalog.GameplayController.animationClips.FirstOrDefault(c=>c.name=="Idle_A");
                if(idle==null)idle=catalog.GameplayController.animationClips.FirstOrDefault(c=>c.name.Contains("Idle"));
                if(animator!=null&&idle!=null){idle.SampleAnimation(animator.gameObject,0);animator.enabled=false;}
            }
            Dock(host.transform,ui);
            var rects=ui.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).ToArray();
            var camera=room.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c=>c.enabled);
            state.ConfigureProjection(camera,rects.FirstOrDefault(r=>r.name=="FoodBar"||r.name=="HungerUI"),rects.FirstOrDefault(r=>r.name=="ThirstUI"),rects.FirstOrDefault(r=>r.name=="EnergyUI"));
            foreach(var portrait in ui.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SelectedCatPortrait>(true)))portrait.Refresh();
            Canvas.ForceUpdateCanvases();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
        catch(Exception error){Clear();Debug.LogWarning("Saved home preview: "+error.Message);}
        finally{applying=false;}
    }
    static void Dock(Transform owner,Scene ui)
    {
        var source=ui.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).FirstOrDefault(t=>t.name=="HomeDock");
        if(source==null)return;
        // Same 1080-wide adaptive dock as Play, in a temporary overlay canvas.
        var root=U.Rect("Editor commands dock",owner);var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=135;
        var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var safe=U.Rect("SafeArea",root);U.Fill(safe);safe.gameObject.AddComponent<SafeAreaRect>();
        var button=U.Action("CompanionShortcut",safe,PremiumTypography.Emphasis,null,ModernUiArt.Paper,135,40,248,56,out var label);
        var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);
        ModernUiArt.FlatNavigation(button);JoyfulUiArt.Icon("TogetherPaw",button.targetGraphic.transform,"Paw",-83,0,48);
        U.At(label.rectTransform,23,0,162,42);label.fontSize=21;label.text=GameContentCopy.Text("Kedi komutları","Cat commands");
        owner.GetComponent<EditorHomePreviewState>().ConfigureDock(rect,safe);
        // Preview controls are visual only; runtime installs the real connected button.
        foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
    }
}
