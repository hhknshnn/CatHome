using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RunnerCrouchTransitionTests
{
    [UnityTest]
    public IEnumerator AllBreeds_EntryStrideAndRecoveryHaveNoInstantJointFlip()
    {
        float previousScale=Time.timeScale,previousCaptureDelta=Time.captureDeltaTime;Time.timeScale=1;
        try
        {
            var catalog=CatBreedCatalog.Load();
            foreach(var entry in catalog.Entries)
            {
                var owner=new GameObject("CrouchTransition_"+entry.Id);
                owner.transform.position=new Vector3(600,1000,0);
                try
                {
                    var visual=CatBreedVisualFactory.Create(entry,catalog.GameplayController,owner.transform);
                    visual.transform.localScale=Vector3.one*.5f;
                    var animator=visual.GetComponentInChildren<Animator>();
                    var motion=animator.gameObject.AddComponent<MiniGameCatAnimation>();
                    var joints=visual.GetComponentsInChildren<Transform>(true).Where(t=>
                        t.name=="DEF-spine.006"||t.name.StartsWith("DEF-upper_arm.")||
                        t.name=="DEF-forearm.L"||t.name=="DEF-forearm.R"||
                        t.name=="DEF-thigh.L"||t.name=="DEF-thigh.R"||
                        t.name=="DEF-shin.L"||t.name=="DEF-shin.R"||
                        t.name=="DEF-hand.L"||t.name=="DEF-hand.R"||
                        t.name=="DEF-foot.L"||t.name=="DEF-foot.R").ToArray();
                    var probe=owner.AddComponent<RunnerTransitionRawPoseProbe>();probe.Configure(joints);
                    // 7m/s base and the actual authored 7*1.7 maximum speed;
                    // tablet/desktop frame steps and three distinct exit phases.
                    foreach(int frameRate in new[]{30,60})
                    foreach(float speed in new[]{7f,11.9f})
                    foreach(float crouchSeconds in new[]{.43f,.6f,.72f})
                    {
                        Time.captureFramerate=frameRate;
                        float warmUntil=Time.time+.14f;
                        while(Time.time<warmUntil){motion.Run(speed);yield return new WaitForEndOfFrame();}
                        var previous=joints.Select(t=>t.localRotation).ToArray();
                        float until=Time.time+crouchSeconds;
                        float lastPhase=0,totalCycles=0,elapsed=0;
                        string context=entry.Id+" "+speed+"m/s "+frameRate+"fps exit="+crouchSeconds;
                        while(Time.time<until)
                        {
                            motion.Crouch(speed);
                            elapsed+=Time.deltaTime;
                            yield return new WaitForEndOfFrame();
                            // The rendered step belongs to this frame's delta,
                            // not the previous frame before the coroutine yield.
                            AssertSmoothFrame(joints,previous,Time.deltaTime,context+" crouch",probe,animator);
                            totalCycles+=Mathf.Repeat(motion.CrouchPhase-lastPhase,1);lastPhase=motion.CrouchPhase;
                        }
                        Assert.That(totalCycles/elapsed,Is.InRange(1.2f,2.65f),entry.Id+" unreadable stride cadence");
                        // Includes completion of the .22s bridge and the normal
                        // gallop afterward, so a delayed release snap cannot hide.
                        float recoverUntil=Time.time+.35f;
                        float recoveryStarted=Time.time;
                        while(Time.time<recoverUntil)
                        {
                            motion.Run(speed);yield return new WaitForEndOfFrame();
                            float recoveryElapsed=Time.time-recoveryStarted;
                            AssertSmoothFrame(joints,previous,Time.deltaTime,context+" recovery t="+
                                recoveryElapsed.ToString("F4"),probe,animator,recoveryElapsed);
                        }
                        var runClip=animator.runtimeAnimatorController.animationClips.First(c=>c.name.EndsWith("|Run",System.StringComparison.Ordinal));
                        float cycles=animator.GetFloat("LocomotionRate")/runClip.length;
                        Assert.That(cycles,Is.InRange(2.4f,4.01f),context+" gallop cadence after recovery");
                        Assert.That(visual.transform.localScale,Is.EqualTo(Vector3.one*.5f));
                    }
                    Time.timeScale=0;yield return null;
                    motion.Crouch(11.9f);float frozenPhase=motion.CrouchPhase;
                    for(int frame=0;frame<3;frame++){motion.Crouch(11.9f);yield return null;}
                    Assert.That(motion.CrouchPhase,Is.EqualTo(frozenPhase),entry.Id+" crouch advances while paused");
                    Time.timeScale=1;
                }
                finally{Object.Destroy(owner);}
                yield return null;
            }
        }
        finally{Time.timeScale=previousScale;Time.captureDeltaTime=previousCaptureDelta;}
    }

    static void AssertSmoothFrame(Transform[] joints,Quaternion[] previous,float dt,string context,
        RunnerTransitionRawPoseProbe probe,Animator animator,float recoveryElapsed=-1)
    {
        for(int i=0;i<joints.Length;i++)
        {
            float angle=Quaternion.Angle(previous[i],joints[i].localRotation);
            float rawStep=Quaternion.Angle(probe.Previous[i],probe.Current[i]);
            float limit=Mathf.Max(38,dt*1800);
            if(recoveryElapsed>=0)
            {
                // A 4Hz gallop advances .133 cycles per 30fps frame. Native
                // instrumentation measured a 63.24-degree forearm step with a
                // 64.53-degree final step: the old absolute 60-degree recovery
                // bound rejected the authored gait, not an introduced snap.
                // Keep the strict crouch bound; recovery must not introduce
                // more than 150deg/s (5deg at 30fps) above the native step.
                limit=Mathf.Max(limit,rawStep+dt*150);
                // Past the .22s bridge, preserve the actual authored Run pose.
                // This also catches a filter that hides a snap by delaying it.
                if(recoveryElapsed>=.25f)
                    Assert.That(Quaternion.Angle(joints[i].localRotation,probe.Current[i]),Is.LessThan(.1f),
                        context+" "+joints[i].name+" must return exactly to the native Run pose");
            }
            if(!(angle<limit))
            {
                Assert.That(angle,Is.LessThan(limit),context+" "+joints[i].name+
                    " snaps "+angle+" degrees; raw Animator step="+rawStep+
                    " normalized="+animator.GetCurrentAnimatorStateInfo(0).normalizedTime+
                    " rate="+animator.GetFloat("LocomotionRate")+" dt="+dt);
            }
            previous[i]=joints[i].localRotation;
        }
    }
}

// Test-only observation before MiniGameCatAnimation's execution order 200.
// Measures the native Animator output before the final transition pose bridge.
[DefaultExecutionOrder(150)]
public sealed class RunnerTransitionRawPoseProbe:MonoBehaviour
{
    private Transform[] joints;
    public Quaternion[] Previous{get;private set;}
    public Quaternion[] Current{get;private set;}
    public void Configure(Transform[] value)
    {
        joints=value;Previous=new Quaternion[value.Length];Current=new Quaternion[value.Length];
        for(int i=0;i<joints.Length;i++)Previous[i]=Current[i]=joints[i].localRotation;
    }
    private void LateUpdate()
    {
        if(joints==null)return;
        for(int i=0;i<joints.Length;i++)
        {Previous[i]=Current[i];Current[i]=joints[i].localRotation;}
    }
}
