using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Actual living-room care at 24 fps on the isolated QA save.</summary>
public static class LivingCareVisualCapture
{
    static string Root=>UiQaTestSession.ResultDirectory;
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public static string Status {get;private set;}="Idle";
    static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags).GetValue(o);
    public static void Begin()
    {
        if(!Application.isPlaying || !EditorQaSession.IsActive)throw new InvalidOperationException("Isolated Play required");
        var loader=Object.FindAnyObjectByType<LevelLoader>();
        if(loader==null || !loader.IsReady || loader.CurrentRoom.Id!=HomeRoomService.LivingRoomId)throw new InvalidOperationException("Ready living room required");
        if(Status=="Recording")throw new InvalidOperationException("Already recording");
        Status="Recording";loader.StartCoroutine(Run());
    }
    static void Place(CatMovement cat,Vector3 p,Quaternion q)
    {var cc=cat.GetComponent<CharacterController>();cc.enabled=false;cat.transform.SetPositionAndRotation(p,q);cc.enabled=true;Physics.SyncTransforms();}
    static void Still(string name)
    {var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+"/screens/"+name+".png",texture.EncodeToPNG());Object.Destroy(texture);}
    static IEnumerator Run()
    {
        Directory.CreateDirectory(Root+"/screens");
        var cat=Object.FindAnyObjectByType<CatMovement>();var bowls=cat.GetComponent<BowlInteraction>();
        var idle=cat.GetComponent<CatIdleBehavior>();bool idleState=idle.enabled;
        var camera=Camera.main;var viewport=camera.GetComponent<HomeWorldViewport>();bool viewportState=viewport!=null&&viewport.enabled;
        var cameraPosition=camera.transform.position;var cameraRotation=camera.transform.rotation;float fov=camera.fieldOfView;var rect=camera.rect;
        var rootPosition=cat.transform.position;var rootRotation=cat.transform.rotation;
        int fps=Time.captureFramerate;float scale=Time.timeScale;
        var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));var sizeProperty=view.GetType().GetProperty("selectedSizeIndex",Flags);int size=(int)sizeProperty.GetValue(view);
        var hunger=Object.FindAnyObjectByType<HungerSystem>();var thirst=Object.FindAnyObjectByType<ThirstSystem>();var energy=Object.FindAnyObjectByType<EnergySystem>();
        float h=hunger.CurrentHunger,t=thirst.CurrentThirst,e=energy.CurrentEnergy;
        try
        {
            UiQaVisualTour.Clear();Object.FindAnyObjectByType<CatCompanionPanel>()?.Close();
            idle.enabled=false;Time.timeScale=1;energy.ApplySavedValue(70);
            yield return new WaitForSeconds(.6f);
            if(HomeUiFlow.IsHomeControlBlocked)throw new InvalidOperationException("Modal remains open");
            Place(cat,new Vector3(.15f,.05f,.8f),Quaternion.Euler(0,150,0));
            foreach(int width in new[]{1920,1440})
            {UiQaVisualTour.Resolution(width,1080);yield return null;yield return new WaitForEndOfFrame();Still("Salon-"+width+"x1080");}
            var pad=System.Linq.Enumerable.Single(CatActivity.Registered,a=>a.StoreProductId==HomeStoreService.NapPillowId);
            Place(cat,pad.RoutineEntryPoint.position+Vector3.up*.05f,Quaternion.Euler(0,0,0));yield return null;
            if(!pad.TryStart(cat))throw new InvalidOperationException("Wall pad must be usable");
            float padDeadline=Time.realtimeSinceStartup+12;
            while(pad.IsRunning&&!pad.IsWaitingForRestStop&&Time.realtimeSinceStartup<padDeadline)yield return null;
            if(!pad.IsWaitingForRestStop)throw new InvalidOperationException("Pad failed to reach supported rest");
            yield return new WaitForEndOfFrame();Still("minder-sleep");
            if(!pad.RequestRestStop())throw new InvalidOperationException("Pad stop failed");
            while(pad.IsRunning&&Time.realtimeSinceStartup<padDeadline+8)yield return null;
            if(pad.IsRunning||cat.IsMovementPhysicallyLocked)throw new InvalidOperationException("Pad did not release control");
            File.WriteAllText(Root+"/pad-live.txt","Requested wall pose: enter, held rest, stop and return passed.");
            UiQaVisualTour.Resolution(1280,720);yield return null;
            Vector3 subject=new Vector3(-3.16f,.12f,1.88f),offset=(cameraPosition-subject).normalized*2.60f;
            if(viewport!=null)viewport.enabled=false;
            camera.transform.SetPositionAndRotation(subject+offset,Quaternion.LookRotation(-offset));camera.fieldOfView=38;camera.rect=rect;
            Time.captureFramerate=24;
            foreach(string kind in new[]{"food","water"})
            {
                hunger.ApplySavedValue(30);thirst.ApplySavedValue(30);var setup=Get<BowlInteraction.BowlSetup>(bowls,kind);setup.Fill();
                var entry=setup.InteractionPoint.position;entry.y=.05f;Place(cat,entry,Quaternion.Euler(0,90,0));
                yield return null;yield return new WaitForEndOfFrame();Still(kind+"-before");
                int completed=0;UnityEngine.Events.UnityAction onComplete=()=>completed++;setup.OnInteractionCompleted.AddListener(onComplete);
                string directory="Library/OriginalFeedingMotion/"+kind;Directory.CreateDirectory(directory);
                int frame=0,consuming=0;float mouthMax=0;
                try
                {
                    Get<Button>(bowls,"interactionButton").onClick.Invoke();
                    if(!bowls.IsInteracting)throw new InvalidOperationException(kind+" rejected");
                    do
                    {
                        yield return new WaitForEndOfFrame();
                        var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(directory+"/"+frame.ToString("D4")+".jpg",image.EncodeToJPG(94));
                        if(!string.IsNullOrEmpty(bowls.ActiveCareSound))
                        {
                            var mouthProfile=CatSipMouthCatalog.Load().Find(CatBreedService.SelectedBreedId);
                            var liveAnimator=cat.GetComponentInChildren<Animator>();float distance=float.PositiveInfinity;
                            foreach(var vertex in mouthProfile.vertices)
                            {
                                Vector3 point=Vector3.zero;foreach(var influence in vertex.influences)point+=liveAnimator.transform.Find(influence.bonePath).TransformPoint(influence.bindPosition)*influence.weight;
                                distance=Mathf.Min(distance,Vector3.Distance(point,setup.ContactPoint.position));
                            }
                            if(distance>.12f)throw new InvalidOperationException(kind+" lost real mouth contact: "+distance);
                            mouthMax=Mathf.Max(mouthMax,distance);
                            if(++consuming==24)File.WriteAllBytes(Root+"/screens/"+kind+"-contact.png",image.EncodeToPNG());
                        }
                        Object.Destroy(image);frame++;
                    }while(bowls.IsInteracting && frame<480);
                    if(bowls.IsInteracting || completed!=1 || consuming<24)throw new InvalidOperationException(kind+" failed completion/contact");
                    File.WriteAllText(Root+"/"+kind+"-capture.json", "{\"fps\":24,\"frames\":"+frame+",\"completionCount\":"+completed+",\"consumingFrames\":"+consuming+",\"mouthMaxMetres\":"+mouthMax.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
                    yield return new WaitForEndOfFrame();Still(kind+"-returned");
                }
                finally{setup.OnInteractionCompleted.RemoveListener(onComplete);bowls.CancelInteraction();}
            }
            Status="Complete";
        }
        finally
        {
            bowls.CancelInteraction();idle.enabled=idleState;Place(cat,rootPosition,rootRotation);
            Time.captureFramerate=fps;Time.timeScale=scale;
            if(viewport!=null)viewport.enabled=viewportState;
            camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);camera.fieldOfView=fov;camera.rect=rect;
            sizeProperty.SetValue(view,size);hunger.ApplySavedValue(h);thirst.ApplySavedValue(t);energy.ApplySavedValue(e);
            if(Status!="Complete")Status="Failed; inspect console";
        }
    }
}
