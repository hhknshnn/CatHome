using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RunnerPavingContactTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    [UnityTest]
    public IEnumerator AllBreeds_RunAndCrouchRecoveryClearPavingAtBaseAndMaximumSpeed()
    {
        string previous=CatBreedService.SelectedBreedId;
        float previousTimeScale=Time.timeScale,previousCaptureDelta=Time.captureDeltaTime;
        yield return SceneManager.LoadSceneAsync("CatRunner",LoadSceneMode.Single);
        var game=Object.FindAnyObjectByType<CatRunnerGameController>();game.enabled=false;
        var track=Object.FindAnyObjectByType<CatRunnerTrackManager>();track.enabled=false;
        var player=Object.FindAnyObjectByType<CatRunnerPlayer>();
        var items=(List<CatRunnerTrackObject>)typeof(CatRunnerTrackManager).GetField("activeObjects",Flags).GetValue(track);
        var template=player.transform.root.GetComponentsInChildren<CatRunnerTrackObject>(true).First(t=>t.Kind==CatRunnerTrackObjectKind.Platform);
        var ramp=Object.Instantiate(template,template.transform.parent);ramp.gameObject.SetActive(true);items.Add(ramp);
        var mesh=new Mesh();var vertices=new List<Vector3>();
        var speedField=typeof(CatRunnerGameController).GetField("baseSpeed",Flags);
        float baseSpeed=(float)speedField.GetValue(game);
        float maximumSpeed=baseSpeed*(float)typeof(CatRunnerGameController).GetField("maximumSpeedMultiplier",Flags).GetValue(game);
        var laneField=typeof(CatRunnerPlayer).GetField("targetLane",Flags);
        var slide=typeof(CatRunnerPlayer).GetMethod("Slide",Flags);
        Time.timeScale=1;Time.captureFramerate=60;
        try
        {
            foreach(var entry in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(entry.Id);yield return null;yield return null;
                var skin=player.GetComponentInChildren<SkinnedMeshRenderer>();
                foreach(float speed in new[]{baseSpeed,maximumSpeed})
                foreach(float rampZ in new[]{30f,5f,-5f})
                {
                    speedField.SetValue(game,speed);
                    typeof(CatRunnerGameController).GetField("elapsed",Flags).SetValue(game,0f);
                    ramp.transform.localPosition=new Vector3(0,0,rampZ);
                    player.ResetRun();player.SetRunning(true);
                    Vector3 originalScale=player.VisualRoot.localScale;
                    float neutralY=player.VisualRoot.localPosition.y;
                    bool sawRun=false,sawSlide=false,sawRecovery=false;
                    // Native Animator + both LateUpdates at 60fps: ordinary run,
                    // full .72s crouch, then at least .25s of its real recovery.
                    for(int frame=0;frame<80;frame++)
                    {
                        if(frame==18)slide.Invoke(player,null);
                        float laneAmplitude=rampZ>20?.8f:.25f;
                        laneField.SetValue(player,Mathf.Sin(frame*.13f)*laneAmplitude);
                        player.SetTrackSurfaceHeight(track.SampleSurfaceHeightAt(player.LanePosition,0));
                        yield return new WaitForEndOfFrame();
                        bool recovering=sawSlide&&!player.IsSliding;
                        sawRun|=!sawSlide&&!player.IsSliding;
                        sawSlide|=player.IsSliding;sawRecovery|=recovering;
                        skin.BakeMesh(mesh,true);mesh.GetVertices(vertices);
                        float baseY=player.transform.position.y-player.Height;
                        float gap=float.PositiveInfinity;
                        foreach(var v in vertices)
                        {
                            var p=skin.transform.TransformPoint(v);
                            // Check the true curved paving/platform at each
                            // vertex, independently of the contact interpolation.
                            float lane=player.LanePosition+p.x-player.transform.position.x;
                            float floor=baseY+track.SampleSurfaceHeightAt(lane,p.z-player.transform.position.z);
                            gap=Mathf.Min(gap,p.y-floor);
                        }
                        string context=entry.Id+" "+speed+"m/s ramp="+rampZ+" frame="+frame+
                            (player.IsSliding?" crouch":recovering?" recovery":" run");
                        Assert.That(gap,Is.GreaterThan(-.006f),context+" visible paw/fur penetration "+gap);
                        Assert.That(player.VisualRoot.localPosition.y,Is.GreaterThanOrEqualTo(neutralY-.0001f),
                            context+" must not pull the airborne part of a gallop down");
                        Assert.That(player.VisualRoot.localScale,Is.EqualTo(originalScale),context);
                    }
                    Assert.That(sawRun&&sawSlide&&sawRecovery,Is.True,entry.Id+" missing native movement phase");
                }
            }
        }
        finally
        {
            player.SetRunning(false);CatBreedService.Select(previous);speedField.SetValue(game,baseSpeed);
            items.Remove(ramp);Object.Destroy(ramp.gameObject);Object.Destroy(mesh);
            Time.timeScale=previousTimeScale;Time.captureDeltaTime=previousCaptureDelta;
        }
    }

    [UnityTest]
    public IEnumerator AllBreeds_DuckAboveVisiblePavingDuringLaneChangesAndRamps()
    {
        string previous=CatBreedService.SelectedBreedId;
        yield return SceneManager.LoadSceneAsync("CatRunner",LoadSceneMode.Single);
        var game=Object.FindAnyObjectByType<CatRunnerGameController>();game.enabled=false;
        var track=Object.FindAnyObjectByType<CatRunnerTrackManager>();track.enabled=false;
        var player=Object.FindAnyObjectByType<CatRunnerPlayer>();
        var items=(List<CatRunnerTrackObject>)typeof(CatRunnerTrackManager).GetField("activeObjects",Flags).GetValue(track);
        var template=player.transform.root.GetComponentsInChildren<CatRunnerTrackObject>(true).First(t=>t.Kind==CatRunnerTrackObjectKind.Platform);
        var ramp=Object.Instantiate(template,template.transform.parent);ramp.gameObject.SetActive(true);items.Add(ramp);
        var mesh=new Mesh();var vertices=new List<Vector3>();
        try
        {
            foreach(var entry in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(entry.Id);yield return null;yield return null;
                player.ResetRun();player.SetRunning(true);
                Vector3 originalScale=player.VisualRoot.localScale;
                foreach(float rampZ in new[]{30f,5f,-5f})
                {
                    ramp.transform.localPosition=new Vector3(0,0,rampZ);
                    typeof(CatRunnerPlayer).GetField("targetLane",Flags).SetValue(player,0f);
                    typeof(CatRunnerPlayer).GetMethod("Slide",Flags).Invoke(player,null);
                    float until=Time.time+.5f;
                    while(Time.time<until)
                    {
                        if(rampZ>20)typeof(CatRunnerPlayer).GetField("targetLane",Flags).SetValue(player,Mathf.Sin(Time.time*12));
                        player.SetTrackSurfaceHeight(track.SampleSurfaceHeightAt(player.LanePosition,0));
                        yield return new WaitForEndOfFrame();
                        var skin=player.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(mesh,true);mesh.GetVertices(vertices);
                        float baseY=player.transform.position.y-player.Height;
                        float gap=float.PositiveInfinity;
                        foreach(var v in vertices)
                        {
                            var p=skin.transform.TransformPoint(v);
                            // Every visible vertex is compared with the same actual ramp profile.
                            float floor=baseY+track.SampleSurfaceHeightAt(player.LanePosition,p.z-player.transform.position.z);
                            gap=Mathf.Min(gap,p.y-floor);
                        }
                        Assert.That(gap,Is.GreaterThan(-.008f),entry.Id+" road/ramp "+rampZ+" clearance "+gap);
                        Assert.That(player.VisualRoot.localScale,Is.EqualTo(originalScale));
                    }
                }
            }
        }
        finally{player.SetRunning(false);CatBreedService.Select(previous);Object.Destroy(mesh);}
    }
}
