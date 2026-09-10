using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class MiniGameMotionTests
{
    [UnityTest]
    public IEnumerator AllBreeds_JumpLandAndDuckWithAnUnscaledBody()
    {
        var isolated=UnityEngine.SceneManagement.SceneManager.CreateScene("MiniGameMotionIsolated");
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(isolated);
        foreach(string sceneName in new[]{"CatRunner","CatCatch","GameScene","CatHome_UI","LivingRoom_Level01"})
        {
            var loaded=UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            if(loaded.IsValid()&&loaded.isLoaded)yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(loaded);
        }
        var catalog=CatBreedCatalog.Load();float old=Time.timeScale;Time.timeScale=1;
        var service=Object.FindAnyObjectByType<CatBreedRuntimeController>();bool serviceEnabled=service!=null&&service.enabled;
        if(service!=null)service.enabled=false;
        try
        {
        foreach(var entry in catalog.Entries)
        {
            var owner=new GameObject("MiniGameMotion_"+entry.Id);owner.transform.position=new Vector3(500,1000,0);
            try
            {
                var visual=CatBreedVisualFactory.Create(entry,catalog.GameplayController,owner.transform);visual.transform.localScale=Vector3.one*.5f;
                var animator=visual.GetComponentInChildren<Animator>();
                var player=owner.AddComponent<CatRunnerPlayer>();player.RebindBreedVisual(animator,visual.transform);player.SetRunning(true);
                Assert.That(player.VisualRoot,Is.EqualTo(visual.transform),entry.Id+" must bind the outer visual root");
                Assert.That(visual.transform.localScale,Is.EqualTo(Vector3.one*.5f),entry.Id+" rebind must not copy the inner 3x model scale");
                yield return null;
                var motion=animator.GetComponent<MiniGameCatAnimation>();Assert.That(motion,Is.Not.Null,entry.Id);
                Vector3 rest=visual.transform.localScale;int landings=0;player.Landed+=()=>landings++;
                typeof(CatRunnerPlayer).GetMethod("Jump",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);
                float peak=0,until=Time.time+1.1f;bool measuredApex=false;
                while(Time.time<until)
                {
                    peak=Mathf.Max(peak,player.JumpHeight);Assert.That(visual.transform.localScale,Is.EqualTo(rest));
                    yield return new WaitForEndOfFrame();
                    if(player.JumpHeight>.98f)
                    {
                        float floor=owner.transform.position.y-player.Height;
                        var bounds=PosedBounds(visual);
                        Assert.That(bounds.min.y-floor,Is.GreaterThan(.88f),entry.Id+" visible paws/body must clear the tallest .83m obstacle at the apex");
                        measuredApex=true;
                    }
                }
                Assert.That(measuredApex,Is.True,entry.Id+" no measured apex");
                Assert.That(peak,Is.GreaterThan(.70f),entry.Id+" never jumped");
                Assert.That(landings,Is.EqualTo(1),entry.Id+" height="+player.JumpHeight+" peak="+peak);Assert.That(player.JumpHeight,Is.Zero);
                typeof(CatRunnerPlayer).GetMethod("Slide",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);
                yield return new WaitForSeconds(.12f);
                Assert.That(player.IsSliding,Is.True);Assert.That(visual.transform.localScale,Is.EqualTo(rest));
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("MiniGameDuck"),Is.True,entry.Id);
                while(player.IsSliding)
                {
                    yield return new WaitForEndOfFrame();if(!player.IsSliding)break;
                    float floor=owner.transform.position.y-player.Height;var bounds=PosedBounds(visual);
                    Assert.That(bounds.min.y-floor,Is.GreaterThan(-.006f),entry.Id+" duck sinks into the paving");
                    Assert.That(bounds.max.y-floor,Is.LessThan(.50f),entry.Id+" visible duck pose hits the canopy");
                }
                Assert.That(player.IsSliding,Is.False);
            }
            finally{Object.Destroy(owner);}
        }
        }
        finally{Time.timeScale=old;if(service!=null)service.enabled=serviceEnabled;}
    }
    static Bounds PosedBounds(GameObject visual)
    {
        var mesh=new Mesh();Bounds bounds=new Bounds();bool first=true;
        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            skin.BakeMesh(mesh,true);
            foreach(var v in mesh.vertices)
            {var p=skin.transform.TransformPoint(v);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
        }
        Object.Destroy(mesh);return bounds;
    }
}
