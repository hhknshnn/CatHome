using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class CozyGamesSaveTests
{
    CozyGameSaveState before;
    [SetUp] public void SetUp()=>before=CozyGameProgress.Capture();
    [TearDown] public void TearDown()=>CozyGameProgress.Apply(before);
    [TestCase(11)][TestCase(12)] public void MigrationPreservesExistingProgress(int version)
    {
        var data=new CatHomeSaveData{version=version,coins=620,diamonds=9,hunger=.62f,thirst=.72f,energy=.82f,bondXp=73,catchBestScore=480,cozyGames=new CozyGameSaveState{yarnStars=new[]{3,2,1,3,0,2},pondBest=12}};
        typeof(CatHomeSaveSystem).GetMethod("MigrateSaveData",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{data});
        Assert.That(data.version,Is.EqualTo(CatHomeSaveSystem.CurrentSaveVersion));Assert.That(data.cozyGames.yarnStars.Length,Is.EqualTo(24));
        Assert.That(data.coins,Is.EqualTo(620));Assert.That(data.diamonds,Is.EqualTo(9));Assert.That(data.bondXp,Is.EqualTo(73));Assert.That(data.catchBestScore,Is.EqualTo(480));
        Assert.That(data.hunger,Is.EqualTo(.62f));Assert.That(data.thirst,Is.EqualTo(.72f));Assert.That(data.energy,Is.EqualTo(.82f));
        if(version==12){CollectionAssert.AreEqual(new[]{3,2,1,3,0,2},data.cozyGames.yarnStars.Take(6));Assert.That(data.cozyGames.pondBest,Is.EqualTo(12));}
    }
    [Test] public void ProgressSanitizesAndOwnsItsArrays()
    {
        var input=new CozyGameSaveState{yarnStars=new[]{-2,9,2},pondBest=-7,fishMask=255,completedRounds=-1};
        CozyGameProgress.Apply(input);input.yarnStars[1]=0;
        var saved=CozyGameProgress.Capture();Assert.That(saved.yarnStars.Length,Is.EqualTo(24));CollectionAssert.AreEqual(new[]{0,3,2},saved.yarnStars.Take(3));
        Assert.That(saved.pondBest,Is.Zero);Assert.That(saved.fishMask,Is.EqualTo(63));Assert.That(saved.completedRounds,Is.Zero);
        saved.yarnStars[1]=0;Assert.That(CozyGameProgress.TotalStars,Is.EqualTo(5));CozyGameProgress.RecordYarn(1,1);Assert.That(CozyGameProgress.TotalStars,Is.EqualTo(5));
    }
    [Test] public void CheckpointAndWalletSurviveDurableRecovery()
    {
        string folder=Path.Combine(Path.GetTempPath(),"CozySave-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            var checkpoint=new YarnCheckpoint{runId=Guid.NewGuid().ToString("N"),level=8,shots=2,phase=3,ball=new Vector2(.3f,-.2f),velocity=new Vector2(.8f,1.2f),seconds=42,continued=true,paidCoins=7,ballHeight=.24f,catRotation=Quaternion.identity,ballRotation=Quaternion.identity};
            checkpoint.roundStars[0]=3;CozyGameProgress.SetCheckpoint(checkpoint);checkpoint.roundStars[0]=0;
            var safe=CozyGameProgress.Checkpoint;Assert.That(safe.roundStars[0],Is.EqualTo(3));Assert.That(safe.score,Is.EqualTo(175));Assert.That(safe.paidCoins,Is.EqualTo(7));
            var data=new CatHomeSaveData{version=13,coins=710,diamonds=7,lastSaveUtc=DateTime.UtcNow.ToString("O"),cozyGames=CozyGameProgress.Capture()};
            string path=Path.Combine(folder,"save.json");var write=typeof(CatHomeSaveSystem).GetMethod("TryWriteSaveDataAtPath",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.That((bool)write.Invoke(null,new object[]{data,path}),Is.True);
            foreach(string file in new[]{path,path+CatHomeSaveSystem.RecoveryFileSuffix})
            {
                var recovered=JsonUtility.FromJson<CatHomeSaveData>(File.ReadAllText(file));CozyGameProgress.Apply(recovered.cozyGames);var c=CozyGameProgress.Checkpoint;
                Assert.That(c.runId,Is.EqualTo(safe.runId));Assert.That(c.ball,Is.EqualTo(safe.ball));Assert.That(c.velocity,Is.EqualTo(safe.velocity));Assert.That(c.phase,Is.EqualTo(3));Assert.That(c.seconds,Is.EqualTo(42));Assert.That(c.continued,Is.True);
                Assert.That(recovered.coins,Is.EqualTo(710));Assert.That(recovered.diamonds,Is.EqualTo(7));
            }
            safe.ball.x=float.NaN;CozyGameProgress.SetCheckpoint(safe);Assert.That(CozyGameProgress.Checkpoint,Is.Null);
        }
        finally{Directory.Delete(folder,true);}
    }
    [Test] public void FourBoardsAndPersonalPeriodsRemainDistinct()
    {
        var ids=Enum.GetValues(typeof(CompetitionGame)).Cast<CompetitionGame>().Select(g=>CompetitionRules.BoardId(g,CompetitionPeriod.Daily)).ToArray();
        Assert.That(ids.Length,Is.EqualTo(4));Assert.That(ids.Distinct().Count(),Is.EqualTo(4));
        CozyGameProgress.Apply(new CozyGameSaveState());var now=new DateTime(2026,10,7,12,0,0,DateTimeKind.Utc);
        CozyGameProgress.RecordScore(CozyGameKind.Yarn,"old",900,now.AddDays(-8));CozyGameProgress.RecordScore(CozyGameKind.Yarn,"week",500,now.AddDays(-1));CozyGameProgress.RecordScore(CozyGameKind.Yarn,"today",200,now);
        Assert.That(CozyGameProgress.PeriodBest(CozyGameKind.Yarn,CompetitionPeriod.Daily,now),Is.EqualTo(200));
        Assert.That(CozyGameProgress.PeriodBest(CozyGameKind.Yarn,CompetitionPeriod.Weekly,now),Is.EqualTo(500));
        Assert.That(CozyGameProgress.PeriodBest(CozyGameKind.Yarn,CompetitionPeriod.AllTime,now),Is.EqualTo(900));
        Assert.That(CozyGameProgress.PeriodBest(CozyGameKind.Pond,CompetitionPeriod.AllTime,now),Is.Zero);
        CozyGameProgress.RecordScore(CozyGameKind.Yarn,"today",300,now);Assert.That(CozyGameProgress.Capture().scores.Length,Is.EqualTo(3));
    }
    [Test] public void AllTwentyFourLayoutsHavePhysicalRoutesAndIncreasingPoints()
    {
        var routes=RoutePulls();
        for(int level=0;level<24;level++)
        {
            var p=CozyGameRules.Start(level);var v=CozyGameRules.Launch(routes[level]);var obstacles=CozyGameRules.Cushions(level);bool found=false;
            for(int i=0;i<1200;i++){CozyGameRules.StepBall(ref p,ref v,obstacles,level,.012f);if(CozyGameRules.IsInBasket(p,CozyGameRules.Goal(level),v.magnitude)){found=true;break;}}
            Assert.That(found,Is.True,"Puzzle "+(level+1));
            if(level>0)Assert.That(CozyGameRules.YarnScore(level,3,false),Is.GreaterThan(CozyGameRules.YarnScore(level-1,3,false)));
        }
    }
    static Vector2[] RoutePulls(){var first=new[]{
            new Vector2(1.60783994f,0.715856493f),
            new Vector2(1.09163368f,1.746979f),
            new Vector2(1.9357667f,0.704561412f),
            new Vector2(0.782927871f,1.93781424f),
            new Vector2(1.73268855f,1.16871309f),
            new Vector2(0.916195691f,1.87847948f),
            new Vector2(-1.73268831f,1.16871333f),
            new Vector2(1.45183587f,-1.50342023f),
            new Vector2(-1.50342f,1.45183611f),
            new Vector2(1.52420473f,0.879999995f),
            new Vector2(1.90211308f,-0.618033886f),
            new Vector2(1.28673244f,1.64694238f)
        };return first.Concat(first.Select(p=>-p)).ToArray();}

}

