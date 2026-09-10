using System.Collections.Generic;
using UnityEngine;

/// <summary>Movement owns time; skeletal poses follow the actual flight/stride instead of squashing the skin.</summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class MiniGameCatAnimation:MonoBehaviour
{
    private Animator animator;private int current;
    private static readonly int Idle=Animator.StringToHash("Base Layer.Idle");
    private static readonly int Jump=Animator.StringToHash("Base Layer.MiniGameJump");
    private static readonly int Duck=Animator.StringToHash("Base Layer.MiniGameDuck");
    private static readonly int Pounce=Animator.StringToHash("Base Layer.MiniGamePounce");
    private static readonly int Speed=Animator.StringToHash("Speed"), Rate=Animator.StringToHash("LocomotionRate");
    private AnimatorOverrideController overrides;
    private Transform[] transitionBones;
    private Vector3[] transitionPositions;
    private Quaternion[] transitionRotations;
    private float crouchPhase, poseBlendRemaining, poseBlendDuration, runClipSeconds=11f/30f;
    private bool recoveringToRun;
    private const float CrouchBlendSeconds=.09f;
    private const float RecoveryBlendSeconds=.22f;
    public float CrouchPhase=>crouchPhase;
    public static float CrouchCyclesPerSecond(float metresPerSecond)=>
        Mathf.Clamp(Mathf.Max(0,metresPerSecond)/4.2f,1.25f,2.6f);
    public static float RunCyclesPerSecond(float metresPerSecond)=>
        Mathf.Clamp(Mathf.Max(0,metresPerSecond)/2.8f,.8f,4f);
    private void Awake()
    {
        animator=GetComponent<Animator>();
        var tag=GetComponentInParent<CatBreedVisualTag>();
        string breed=tag!=null?tag.BreedId:CatBreedCatalog.DefaultBreedId;
        overrides=new AnimatorOverrideController(animator.runtimeAnimatorController);
        foreach(string kind in new[]{"Duck","Jump","Pounce"})
        {
            var clip=Resources.Load<AnimationClip>("MiniGameAnimations/"+breed+"_"+kind);
            if(clip!=null)overrides["MiniGame"+kind]=clip;
        }
        animator.runtimeAnimatorController=overrides;
        foreach(var clip in animator.runtimeAnimatorController.animationClips)
            if(clip.name.EndsWith("|Run",System.StringComparison.Ordinal)&&clip.length>0)
            {runClipSeconds=clip.length;break;}
        var joints=new List<Transform>();
        foreach(var joint in GetComponentsInChildren<Transform>(true))
            if(joint.name.StartsWith("DEF-",System.StringComparison.Ordinal))joints.Add(joint);
        transitionBones=joints.ToArray();
        transitionPositions=new Vector3[transitionBones.Length];
        transitionRotations=new Quaternion[transitionBones.Length];
    }
    public void Run(float metresPerSecond)
    {
        Ensure();if(animator==null)return;
        if(current!=Idle)
        {
            if(current==Duck)
            {
                // Capture the actual last rendered pose. A controller crossfade
                // would restart the outgoing walk when animator.speed becomes 1,
                // blending two moving gaits and snapping the folded wrists.
                BeginPoseBlend(RecoveryBlendSeconds);recoveringToRun=true;
                animator.Play(Idle,0,0f);
            }
            else{StopPoseBlend();animator.CrossFadeInFixedTime(Idle,.12f);}
            current=Idle;
        }
        animator.speed=1;
        animator.SetFloat(Speed,metresPerSecond>.05f?1:0,.08f,Time.deltaTime);
        // One complete gallop advances approximately 2.8m in the mini-game's scale.
        // Derive playback rate from the actual short source clip, not seconds
        // assumed by a fixed divisor (the 11-frame Run was exceeding 5Hz).
        float rate=metresPerSecond>.05f?RunCyclesPerSecond(metresPerSecond)*runClipSeconds:1;
        if(recoveringToRun)
            rate=Mathf.Lerp(Mathf.Min(.75f,rate),rate,
                Mathf.SmoothStep(0,1,1-poseBlendRemaining/RecoveryBlendSeconds));
        animator.SetFloat(Rate,rate);
    }
    public void Flight(float progress){Pose(Jump,Mathf.Clamp01(progress));}
    public void Crouch(float metresPerSecond)
    {
        Ensure();if(animator==null)return;
        if(current!=Duck)
        {
            crouchPhase=0;
            BeginPoseBlend(CrouchBlendSeconds);recoveringToRun=false;
        }
        // The former travelled/.9 advanced a WALK eight or more times each
        // second. A bounded low step remains readable as the track accelerates.
        crouchPhase=Mathf.Repeat(crouchPhase+CrouchCyclesPerSecond(metresPerSecond)*Time.deltaTime,1);
        Pose(Duck,crouchPhase);
    }
    private void LateUpdate()
    {
        if(poseBlendRemaining<=0||Time.timeScale<=0)return;
        poseBlendRemaining=Mathf.Max(0,poseBlendRemaining-Time.deltaTime);
        float blend=Mathf.SmoothStep(0,1,1-poseBlendRemaining/poseBlendDuration);
        for(int i=0;i<transitionBones.Length;i++)
        {
            var joint=transitionBones[i];
            joint.localPosition=Vector3.Lerp(transitionPositions[i],joint.localPosition,blend);
            joint.localRotation=Quaternion.Slerp(transitionRotations[i],joint.localRotation,blend);
        }
        if(poseBlendRemaining<=0)recoveringToRun=false;
        // RunnerGroundContact executes later (300), after both pose bridges.
    }
    private void BeginPoseBlend(float seconds)
    {
        poseBlendDuration=poseBlendRemaining=seconds;
        for(int i=0;i<transitionBones.Length;i++)
        {
            transitionPositions[i]=transitionBones[i].localPosition;
            transitionRotations[i]=transitionBones[i].localRotation;
        }
    }
    private void StopPoseBlend(){poseBlendRemaining=0;recoveringToRun=false;}
    public void Hunt(float progress){Pose(Pounce,Mathf.Clamp01(progress));}
    private void Pose(int state,float phase)
    {
        Ensure();if(animator==null||!animator.HasState(0,state))return;
        // Explicit normalized time prevents clip duration or breed changes from moving the contact frame.
        if(state!=Duck)StopPoseBlend();
        animator.speed=0;animator.Play(state,0,phase);current=state;
    }
    private void Ensure(){if(animator==null)animator=GetComponent<Animator>();}
    private void OnDisable(){if(animator!=null)animator.speed=1;current=0;StopPoseBlend();crouchPhase=0;}
    private void OnDestroy(){if(overrides!=null)Destroy(overrides);}
}
