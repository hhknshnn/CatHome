using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Real normal-speed frames; runs only against the isolated QA save.</summary>
public static class ComfortMotionCapture
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public static string Status {get;private set;}="Idle";
    public static void Begin(string kind)
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new InvalidOperationException("Use isolated Play QA.");
        if(Status.StartsWith("Recording"))throw new InvalidOperationException("A recording is already active.");
        var loader=Object.FindAnyObjectByType<LevelLoader>();
        if(loader==null||!loader.IsReady)throw new InvalidOperationException("The home is not ready.");
        Status="Recording "+kind;loader.StartCoroutine(Run(kind,loader));
    }
    static T Read<T>(object value,string field)=>(T)value.GetType().GetField(field,Flags).GetValue(value);
    static void Set(object value,string field,object content)=>value.GetType().GetField(field,Flags).SetValue(value,content);
    static void Invoke(object value,string method)=>value.GetType().GetMethod(method,Flags).Invoke(value,null);
    static void Place(CatMovement cat,Vector3 position,Quaternion rotation)
    {
        CatActionState.CancelForTransition(cat);
        var controller=cat.GetComponent<CharacterController>();controller.enabled=false;
        cat.transform.SetPositionAndRotation(position,rotation);controller.enabled=true;Physics.SyncTransforms();
    }
    static IEnumerator Run(string kind,LevelLoader loader)
    {
        int rate=Time.captureFramerate;float scale=Time.timeScale;
        MobileJoystick joystick=null;CatMovement cat=null;PetInteraction pet=null;
        CatIdleBehavior idle=null;bool idleEnabled=false;
        GameObject hiddenJoystick=null;bool joystickWasActive=false;
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.Mouse qaMouse=null;
        UnityEngine.InputSystem.InputSettings originalInputSettings=null,qaInputSettings=null;
#endif
        try
        {
            UiQaVisualTour.Clear();Object.FindAnyObjectByType<CatCompanionPanel>()?.Close();
            string room=kind=="garden"?HomeRoomService.GardenId:HomeRoomService.LivingRoomId;
            if(loader.CurrentRoom.Id!=room)loader.LoadRoom(room);
            while(loader.IsTransitioning)yield return null;
            yield return null;yield return null;
            cat=Object.FindAnyObjectByType<CatMovement>();
            idle=cat.GetComponent<CatIdleBehavior>();if(idle!=null){idleEnabled=idle.enabled;idle.enabled=false;}
            Object.FindAnyObjectByType<HungerSystem>().ApplySavedValue(100);
            Object.FindAnyObjectByType<ThirstSystem>().ApplySavedValue(100);
            Object.FindAnyObjectByType<EnergySystem>().ApplySavedValue(70);
            Time.timeScale=1;Time.captureFramerate=24;
            if(kind=="locomotion")
            {
                joystick=Read<MobileJoystick>(cat,"mobileJoystick");
                if(joystick==null)throw new InvalidOperationException("Missing actual home joystick.");
                foreach(string state in new[]{"Full","Hungry","LowEnergy"})
                {
                    Object.FindAnyObjectByType<HungerSystem>().ApplySavedValue(state=="Hungry"?0:100);
                    Object.FindAnyObjectByType<ThirstSystem>().ApplySavedValue(state=="Hungry"?34:100);
                    Object.FindAnyObjectByType<EnergySystem>().ApplySavedValue(state=="LowEnergy"?0:99);
                    Place(cat,new Vector3(-.38f,.05f,-1.7f),Quaternion.identity);
                    yield return null;yield return null;
                    typeof(MobileJoystick).GetProperty("Direction").SetValue(joystick,Vector2.up);
                    yield return Frames("Locomotion-"+state,48);
                    typeof(MobileJoystick).GetProperty("Direction").SetValue(joystick,Vector2.zero);
                    yield return new WaitForSeconds(.3f);
                }
            }
            else if(kind=="sleep")
            {
                var sleep=cat.GetComponent<SleepInteraction>();
                var point=Read<Transform>(sleep,"bedInteractionPoint");
                Place(cat,point.position,point.rotation);
                Object.FindAnyObjectByType<EnergySystem>().ApplySavedValue(40);
                yield return null;yield return null;
                if(!sleep.TryHandleActionButton())throw new InvalidOperationException("Sleep was not accepted.");
                yield return Frames("Sleep",168);sleep.CancelForTransition();
            }
            else if(kind=="pet")
            {
                Place(cat,new Vector3(.4f,.05f,-1.4f),Quaternion.Euler(0,145,0));
                yield return null;yield return null;
                pet=cat.GetComponent<PetInteraction>();
#if ENABLE_INPUT_SYSTEM
                originalInputSettings=UnityEngine.InputSystem.InputSystem.settings;
                qaInputSettings=Object.Instantiate(originalInputSettings);
                qaInputSettings.hideFlags=HideFlags.DontSave;
                UnityEngine.InputSystem.InputSystem.settings=qaInputSettings;
                qaInputSettings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
                qaInputSettings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                qaMouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("ComfortQaMouse");
                var camera=Read<Camera>(pet,"gameplayCamera");if(camera==null)camera=Camera.main;
                var screen=(Vector2)camera.WorldToScreenPoint(cat.transform.position+Vector3.up*.25f);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(qaMouse,
                    new UnityEngine.InputSystem.LowLevel.MouseState {position=screen}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return new WaitForSeconds(.4f);
#else
                throw new InvalidOperationException("Motion QA requires the project's Input System pointer.");
#endif
                if(!pet.IsPetting)throw new InvalidOperationException("Petting was not accepted.");
                yield return Frames("Pet",120);
                if(!pet.IsPetting)throw new InvalidOperationException("Pet input was interrupted during capture.");
                Invoke(pet,"EndPetting");
            }
            else if(kind=="garden")
            {
                // The fixed joystick covers the front-left grill contact in a
                // room overview. Hide only this QA control for the motion proof.
                var control=Read<MobileJoystick>(cat,"mobileJoystick");
                if(control!=null){hiddenJoystick=control.gameObject;joystickWasActive=hiddenJoystick.activeSelf;hiddenJoystick.SetActive(false);}
                foreach(var activityKind in new[]{CatActivityKind.DaisyRoll,CatActivityKind.GrillWatch})
                {
                    var activity=CatActivity.Registered.First(a=>a.Kind==activityKind);
                    var position=activity.RoutineEntryPoint.position;position.y=.05f;
                    Place(cat,position,Quaternion.identity);
                    yield return null;yield return null;
                    if(!activity.TryStart(cat))throw new InvalidOperationException("Could not start "+activityKind);
                    yield return Frames(activityKind.ToString(),192);CatActionState.CancelForTransition(cat);
                    yield return new WaitForSeconds(.25f);
                }
            }
            Status="Complete "+kind;
        }
        finally
        {
            if(joystick!=null)typeof(MobileJoystick).GetProperty("Direction").SetValue(joystick,Vector2.zero);
            if(pet!=null)Invoke(pet,"CancelPointer");
            if(idle!=null)idle.enabled=idleEnabled;
            if(hiddenJoystick!=null)hiddenJoystick.SetActive(joystickWasActive);
#if ENABLE_INPUT_SYSTEM
            if(qaMouse!=null)UnityEngine.InputSystem.InputSystem.RemoveDevice(qaMouse);
            if(originalInputSettings!=null)UnityEngine.InputSystem.InputSystem.settings=originalInputSettings;
            if(qaInputSettings!=null)Object.Destroy(qaInputSettings);
#endif
            if(cat!=null)CatActionState.CancelForTransition(cat);
            Time.captureFramerate=rate;Time.timeScale=scale;
            if(Status.StartsWith("Recording"))Status="Interrupted "+kind+"; inspect the console";
        }
    }
    static IEnumerator Frames(string name,int count)
    {
        string directory="Library/ComfortMotion/"+name;Directory.CreateDirectory(directory);
        for(int frame=0;frame<count;frame++)
        {
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(directory+"/"+frame.ToString("D4")+".jpg",texture.EncodeToJPG(94));
            if(frame==(name=="GrillWatch"?36:count/2))
            {
                Directory.CreateDirectory(ComfortPolishQa.Root+"/screens");
                File.WriteAllBytes(ComfortPolishQa.Root+"/screens/Motion-"+name+".png",texture.EncodeToPNG());
            }
            Object.Destroy(texture);Status="Recording "+name+" "+(frame+1)+"/"+count;
        }
    }
}
