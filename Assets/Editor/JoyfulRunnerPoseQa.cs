using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Native post-LateUpdate diagnostic footage; no sampled clips or additional camera.</summary>
public static class JoyfulRunnerPoseQa
{
    public const string Folder="Docs/QA/JOYFUL_ARCADE_2026-09-08/runner-pose";
    public const int FramesPerSecond=60;
    public static string Status{get;private set;}="Idle";
    public static string LastDirectory{get;private set;}
    private static bool running,cancel;
    private static readonly int[] SheetFrames={8,20,23,36,58,65,73,105};

    public static void Begin(bool record=true)
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new InvalidOperationException("Use isolated QA Play from the home scene.");
        if(running)throw new InvalidOperationException("Runner pose capture already active.");
        if(UiQaVisualTour.Find<CatRunnerGameController>()!=null)throw new InvalidOperationException("Return from the existing Runner before this tour.");
        var host=UiQaVisualTour.Find<CatMovement>();
        if(host==null)throw new InvalidOperationException("Home cat is required to host the QA coroutine.");
        cancel=false;running=true;host.StartCoroutine(Tour(record));
    }
    public static void Cancel()=>cancel=true;

    private static IEnumerator Tour(bool record)
    {
        int oldRate=Time.captureFramerate,oldWidth=Screen.width,oldHeight=Screen.height;
        float oldTimeScale=Time.timeScale;
        string oldBreed=CatBreedService.SelectedBreedId;
        var oldEnergy=RunnerEnergyService.CaptureState(DateTime.UtcNow);
        CatRunnerGameController game=null;Camera camera=null;CatRunnerCameraRig rig=null;Canvas canvas=null;
        Vector3 cameraPosition=Vector3.zero;Quaternion cameraRotation=Quaternion.identity;
        float oldFov=50,oldNear=.1f,oldBaseSpeed=7,oldMaximum=1.7f;
        bool rigEnabled=false,canvasEnabled=false,completed=false;
        var mesh=new Mesh();var vertices=new List<Vector3>();
        var takes=new List<string>();
        LastDirectory=Folder+"/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(LastDirectory);
        var csv=new StringBuilder("take,frame,seconds,sliding,phase,minimum_clearance_m,maximum_height_m,max_joint_step_degrees\n");
        try
        {
            Status="Opening isolated Runner";Time.timeScale=1;Time.captureFramerate=FramesPerSecond;
            UiQaVisualTour.Resolution(1920,1080);UiQaVisualTour.Clear();
            var now=DateTime.UtcNow;
            RunnerEnergyService.ApplySavedState(new RunnerEnergySaveState{energy=5,regenerationAnchorUtc=now.ToString("O"),unlimitedUntilUtc=string.Empty,rewardedAdsDayUtc=now.ToString("yyyy-MM-dd")},now);
            UiQaVisualTour.Find<CatRunnerLauncher>().Launch();
            for(int frame=0;frame<48;frame++)yield return null;
            game=UiQaVisualTour.Find<CatRunnerGameController>();
            game.StartFromWelcome();game.SkipTutorial();
            for(int frame=0;frame<180&&!game.IsRunning;frame++)yield return null;
            if(!game.IsRunning)throw new InvalidOperationException("Runner did not begin a real attempt.");
            var player=UiQaVisualTour.Find<CatRunnerPlayer>();var track=UiQaVisualTour.Find<CatRunnerTrackManager>();
            camera=UiQaVisualTour.Get<Camera>(game,"runnerCamera");rig=camera.GetComponent<CatRunnerCameraRig>();
            canvas=UiQaVisualTour.Get<Canvas>(game,"runnerCanvas");
            cameraPosition=camera.transform.position;cameraRotation=camera.transform.rotation;oldFov=camera.fieldOfView;oldNear=camera.nearClipPlane;
            rigEnabled=rig!=null&&rig.enabled;canvasEnabled=canvas!=null&&canvas.enabled;
            if(rig!=null)rig.enabled=false;if(canvas!=null)canvas.enabled=false;
            oldBaseSpeed=UiQaVisualTour.Get<float>(game,"baseSpeed");oldMaximum=UiQaVisualTour.Get<float>(game,"maximumSpeedMultiplier");
            // Use the game's existing hazard-free practice setting. Track movement,
            // player Update, Animator, final LateUpdate and ground correction remain native.
            track.SetTutorialSafety(true);
            UiQaVisualTour.Set(game,"maximumSpeedMultiplier",1f);
            foreach(string breed in new[]{"domestic-shorthair-orange","maine-coon"})
            {
                CatBreedService.Select(breed);for(int frame=0;frame<12;frame++)yield return null;
                if(CatBreedService.SelectedBreedId!=breed)throw new InvalidOperationException("QA breed is unavailable: "+breed);
                foreach(float speed in new[]{oldBaseSpeed,oldBaseSpeed*oldMaximum})
                foreach(string view in new[]{"Side","Rear"})
                {
                    if(cancel)yield break;
                    UiQaVisualTour.Set(game,"baseSpeed",speed);UiQaVisualTour.Set(game,"elapsed",0f);
                    track.ResetRun();track.SetTutorialSafety(true);player.ResetRun();player.SetRunning(true);
                    var animator=player.GetComponentInChildren<Animator>();
                    var motion=animator.GetComponent<MiniGameCatAnimation>();
                    var skins=player.GetComponentsInChildren<SkinnedMeshRenderer>();
                    var joints=animator.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("DEF-")).ToArray();
                    var previous=joints.Select(t=>t.localRotation).ToArray();
                    string take=breed+"-"+view+"-"+speed.ToString("0.0",CultureInfo.InvariantCulture)+"mps";
                    string directory=LastDirectory+"/"+take;Directory.CreateDirectory(directory);takes.Add(take);
                    for(int frame=0;frame<120;frame++)
                    {
                        if(cancel)yield break;
                        if(frame==20)UiQaVisualTour.Call(player,"Slide");
                        Vector3 focus=player.transform.position+Vector3.up*.25f;
                        camera.transform.position=focus+(view=="Side"?new Vector3(1.8f,.38f,-.06f):new Vector3(.66f,.47f,-1.62f));
                        camera.transform.LookAt(focus);camera.fieldOfView=36;camera.nearClipPlane=.02f;
                        yield return new WaitForEndOfFrame();
                        if(!game.IsRunning)throw new InvalidOperationException("The controlled Runner stopped during pose capture.");
                        float low=float.PositiveInfinity,high=float.NegativeInfinity;
                        float baseY=player.transform.position.y-player.Height;
                        foreach(var skin in skins)
                        {
                            skin.BakeMesh(mesh,true);mesh.GetVertices(vertices);
                            foreach(var vertex in vertices)
                            {
                                var p=skin.transform.TransformPoint(vertex);
                                float floor=baseY+track.SampleSurfaceHeightAt(player.LanePosition,p.z-player.transform.position.z);
                                low=Mathf.Min(low,p.y-floor);high=Mathf.Max(high,p.y-baseY);
                            }
                        }
                        float maxStep=0;
                        for(int j=0;j<joints.Length;j++){maxStep=Mathf.Max(maxStep,Quaternion.Angle(previous[j],joints[j].localRotation));previous[j]=joints[j].localRotation;}
                        csv.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2:F4},{3},{4:F4},{5:F5},{6:F5},{7:F3}\n",take,frame,frame/(float)FramesPerSecond,player.IsSliding,motion.CrouchPhase,low,high,maxStep);
                        bool selected=SheetFrames.Contains(frame);
                        if(record||selected)
                        {
                            var image=ScreenCapture.CaptureScreenshotAsTexture();
                            if(record)File.WriteAllBytes(directory+"/"+frame.ToString("D4")+".jpg",image.EncodeToJPG(94));
                            if(selected)File.WriteAllBytes(directory+"/pose-"+frame.ToString("D3")+".png",image.EncodeToPNG());
                            Object.Destroy(image);
                        }
                        Status=take+" frame "+frame+"/119";
                    }
                }
            }
            File.WriteAllText(LastDirectory+"/native-frames.csv",csv.ToString());
            WriteIndex(takes,record);completed=true;Status="Complete: "+LastDirectory;
        }
        finally
        {
            Object.Destroy(mesh);
            if(camera!=null){camera.transform.position=cameraPosition;camera.transform.rotation=cameraRotation;camera.fieldOfView=oldFov;camera.nearClipPlane=oldNear;}
            if(rig!=null)rig.enabled=rigEnabled;if(canvas!=null)canvas.enabled=canvasEnabled;
            if(game!=null)
            {
                UiQaVisualTour.Set(game,"baseSpeed",oldBaseSpeed);UiQaVisualTour.Set(game,"maximumSpeedMultiplier",oldMaximum);
                // Exit without claiming diagnostic coins or a result reward.
                game.ExitFromPause();
            }
            CatBreedService.Select(oldBreed);RunnerEnergyService.ApplySavedState(oldEnergy,DateTime.UtcNow);
            Time.captureFramerate=oldRate;Time.timeScale=oldTimeScale;
            UiQaVisualTour.Resolution(oldWidth,oldHeight);running=false;
            if(!completed){File.WriteAllText(LastDirectory+"/native-frames-partial.csv",csv.ToString());Status=cancel?"Cancelled":"Failed; see Unity console";}
        }
    }

    private static void WriteIndex(List<string> takes,bool record)
    {
        var html=new StringBuilder("<!doctype html><meta charset='utf-8'><title>Runner native crouch</title><style>body{background:#132d31;color:#fff;font:16px system-ui;margin:32px}h2{margin-top:40px}.sheet{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}figure{margin:0}img{width:100%;border-radius:10px}figcaption{padding:8px;color:#c7dcd7}a{color:#9ee9d2}</style><h1>Runner: actual entry, crouch and recovery</h1><p>Orange Shorthair and Maine Coon; native 1920×1080 frames after Animator and all LateUpdate passes. Existing Runner camera moved for anatomy inspection. Existing tutorial safety removes hazards; the road, cat movement and ground contact keep running. These editor captures are not device FPS evidence.</p><p><a href='native-frames.csv'>Per-frame measurements</a> · 60 simulation frames/second</p>");
        string[] labels={"Run","Entry","Entry blend","Low step","Low step","Recovery","Recovery","Run again"};
        foreach(string take in takes)
        {
            html.Append("<h2>").Append(take).Append("</h2><div class='sheet'>");
            for(int i=0;i<SheetFrames.Length;i++)
                html.Append("<figure><img src='").Append(take).Append("/pose-").Append(SheetFrames[i].ToString("D3")).Append(".png'><figcaption>").Append(labels[i]).Append(" · ").Append((SheetFrames[i]/(float)FramesPerSecond).ToString("0.000",CultureInfo.InvariantCulture)).Append("s</figcaption></figure>");
            html.Append("</div>");
            if(record)html.Append("<p>Video source: this folder's 0000.jpg–0119.jpg, 60 fps.</p>");
        }
        File.WriteAllText(LastDirectory+"/index.html",html.ToString());
    }
}
