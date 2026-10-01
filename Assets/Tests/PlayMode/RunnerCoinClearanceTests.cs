using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RunnerCoinClearanceTests
{
    static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
    [UnityTest] public IEnumerator EightHundredGeneratedRows_KeepEveryCoinOutsideBuiltHazards()
    {
        yield return SceneManager.LoadSceneAsync("CatRunner",LoadSceneMode.Single);
        var track=Object.FindAnyObjectByType<CatRunnerTrackManager>();track.enabled=false;
        var field=typeof(CatRunnerTrackManager).GetField("activeObjects",BindingFlags.Instance|BindingFlags.NonPublic);
        var items=(System.Collections.Generic.List<CatRunnerTrackObject>)field.GetValue(track);
        for(int i=0;i<800;i++)
        {
            Call(track,"ScrollSegments",1.73f);Call(track,"ScrollObjects",1.73f);
            int before=items.Count(x=>x.Kind==CatRunnerTrackObjectKind.Coin);
            Call(track,"SpawnCoinPickup");
            Assert.That(items.Count(x=>x.Kind==CatRunnerTrackObjectKind.Coin),Is.EqualTo(before+1),"Do not lose scheduled coins at row "+i);
            if(i%2==0)Call(track,"SpawnObstacleRow");
            if(i%47==0)Call(track,"SpawnElevationRoute");
            if(i%13==0)Call(track,"SpawnPowerUp");
            foreach(var coin in items.Where(x=>x.Kind==CatRunnerTrackObjectKind.Coin))
            {
            foreach(var hazard in items.Where(x=>x.IsHazard))
                Assert.That(coin.VisualBoundsAt(coin.transform.localPosition).Intersects(hazard.VisualBoundsAt(hazard.transform.localPosition)),Is.False,"row "+i+" "+coin.transform.localPosition+" / "+hazard.name+" "+hazard.transform.localPosition);
            foreach(var platform in items.Where(x=>x.Kind==CatRunnerTrackObjectKind.Platform))
                if(platform.TrySamplePlatformRelativeHeightAt(coin.transform.localPosition.x,coin.transform.localPosition.z,out float surface))
                    Assert.That(coin.RuntimeBaseHeight,Is.GreaterThan(surface+.25f),"Coin route must remain above the boardwalk");
            }
            foreach(var pickup in items.Where(x=>x.IsPickup))
            foreach(var other in items.Where(x=>x!=pickup&&(x.IsPickup||x.IsHazard)))
                Assert.That(pickup.VisualBoundsAt(pickup.transform.localPosition).Intersects(other.VisualBoundsAt(other.transform.localPosition)),Is.False,
                    "Pickup clearance row "+i+" "+pickup.name+" / "+other.name);
            if(i%80==0)
            {
                var coin=items.FirstOrDefault(x=>x.Kind==CatRunnerTrackObjectKind.Coin);
                var hazard=items.FirstOrDefault(x=>x.IsHazard);
                if(coin!=null&&hazard!=null)
                {
                    var center=hazard.VisualBoundsAt(hazard.transform.localPosition).center;
                    Assert.That((bool)Call(track,"IsPickupPathClear",coin,center+Vector3.left*2,center+Vector3.right*2),Is.False,"Magnet must not drag a medal through furniture");
                }
            }
            if(i%40==0)yield return null;
        }
        track.ResetRun();yield return null;
    }
}
