using System;
using System.Collections.Generic;
using UnityEngine;

public enum CozyGameKind { Yarn, Pond }
[Serializable] public sealed class CozyScoreRecord
{
    public CozyGameKind game; public string runId,utc; public int score;
    public CozyScoreRecord Clone()=>(CozyScoreRecord)MemberwiseClone();
}
[Serializable] public sealed class YarnCheckpoint
{
    public string runId;
    public int level,shots,solved,stars,score,paidCoins,phase;
    public int[] roundStars=new int[CozyGameRules.LevelCount];
    public bool[] pearls=new bool[CozyGameRules.LevelCount];
    public bool bonusTaken,continued,completionRecorded;
    public float seconds,phaseTime,shotTime,ballHeight,stepRemainder;
    public Vector2 ball,velocity;
    public Vector3 cat,captureFrom;
    public Quaternion catRotation,ballRotation;
    public YarnCheckpoint Clone(){var c=(YarnCheckpoint)MemberwiseClone();c.roundStars=roundStars==null?new int[CozyGameRules.LevelCount]:(int[])roundStars.Clone();c.pearls=pearls==null?new bool[CozyGameRules.LevelCount]:(bool[])pearls.Clone();return c;}
}
[Serializable] public sealed class CozyGameSaveState
{
    public int[] yarnStars=new int[CozyGameRules.LevelCount];
    public int pondBest,pondBestScore,yarnBest,fishMask,completedRounds;
    public YarnCheckpoint checkpoint;
    public CozyScoreRecord[] scores=new CozyScoreRecord[0];
    public CozyGameSaveState Clone()
    {
        var c=(CozyGameSaveState)MemberwiseClone();c.yarnStars=yarnStars==null?new int[CozyGameRules.LevelCount]:(int[])yarnStars.Clone();c.checkpoint=checkpoint?.Clone();
        var rows=new List<CozyScoreRecord>();if(scores!=null)foreach(var row in scores)if(row!=null)rows.Add(row.Clone());c.scores=rows.ToArray();return c;
    }
}
public static class CozyGameProgress
{
    private static CozyGameSaveState state=new CozyGameSaveState();
    public static CozyGameSaveState Capture()=>state.Clone();
    public static void Apply(CozyGameSaveState saved)
    {
        state=saved==null?new CozyGameSaveState():saved.Clone();Array.Resize(ref state.yarnStars,CozyGameRules.LevelCount);
        for(int i=0;i<state.yarnStars.Length;i++)state.yarnStars[i]=Mathf.Clamp(state.yarnStars[i],0,3);
        state.pondBest=Mathf.Max(0,state.pondBest);state.pondBestScore=Mathf.Max(0,state.pondBestScore);state.yarnBest=Mathf.Max(0,state.yarnBest);
        state.fishMask&=63;state.completedRounds=Mathf.Max(0,state.completedRounds);
        var rows=new List<CozyScoreRecord>();foreach(var r in state.scores)if(r.score>=0&&Enum.IsDefined(typeof(CozyGameKind),r.game)&&DateTime.TryParse(r.utc,out _))rows.Add(r);
        if(rows.Count>64)rows.RemoveRange(0,rows.Count-64);state.scores=rows.ToArray();state.checkpoint=SanitizeCheckpoint(state.checkpoint);
    }
    static YarnCheckpoint SanitizeCheckpoint(YarnCheckpoint c)
    {
        if(c==null||string.IsNullOrEmpty(c.runId)||c.runId.Length!=32||c.level<0||c.level>=CozyGameRules.LevelCount)return null;
        if(!Finite(c.ball.x)||!Finite(c.ball.y)||!Finite(c.cat.x)||!Finite(c.cat.y)||!Finite(c.cat.z)||!Finite(c.velocity.x)||!Finite(c.velocity.y)||!Finite(c.seconds))return null;
        if(!Finite(c.phaseTime)||!Finite(c.shotTime)||!Finite(c.ballHeight)||!Finite(c.captureFrom.x)||!Finite(c.captureFrom.y)||!Finite(c.captureFrom.z)||!Finite(c.catRotation.x)||!Finite(c.catRotation.y)||!Finite(c.catRotation.z)||!Finite(c.catRotation.w)||!Finite(c.ballRotation.x)||!Finite(c.ballRotation.y)||!Finite(c.ballRotation.z)||!Finite(c.ballRotation.w))return null;
        Array.Resize(ref c.roundStars,CozyGameRules.LevelCount);Array.Resize(ref c.pearls,CozyGameRules.LevelCount);c.score=c.stars=c.solved=0;
        for(int i=0;i<c.roundStars.Length;i++){c.roundStars[i]=Mathf.Clamp(c.roundStars[i],0,3);if(c.roundStars[i]>0){c.solved++;c.stars+=c.roundStars[i];c.score+=CozyGameRules.YarnScore(i,c.roundStars[i],c.pearls[i]);}}
        c.shots=Mathf.Clamp(c.shots,0,3);c.phase=Mathf.Clamp(c.phase,0,5);c.paidCoins=Mathf.Clamp(c.paidCoins,0,CozyGameRules.YarnCoins(c.score));c.seconds=Mathf.Clamp(c.seconds,0,CozyGameRules.ContinueSeconds);
        c.stepRemainder=Finite(c.stepRemainder)?Mathf.Clamp(c.stepRemainder,0,.012f):0;
        c.velocity=Vector2.ClampMagnitude(c.velocity,CozyGameRules.MaxPull*CozyGameRules.LaunchFactor);c.ball=new Vector2(Mathf.Clamp(c.ball.x,-2.65f,2.65f),Mathf.Clamp(c.ball.y,-1.85f,1.85f));
        return c.continued&&c.seconds<=0?null:c;
    }
    static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
    public static YarnCheckpoint Checkpoint=>state.checkpoint?.Clone();
    public static void SetCheckpoint(YarnCheckpoint c)=>state.checkpoint=SanitizeCheckpoint(c?.Clone());
    public static int TotalStars{get{int n=0;foreach(int s in state.yarnStars)n+=s;return n;}}
    public static int FishCount{get{int n=0;for(int i=0;i<6;i++)if((state.fishMask&(1<<i))!=0)n++;return n;}}
    public static bool HasFish(int i)=>i>=0&&i<6&&(state.fishMask&(1<<i))!=0;
    public static int PondBest=>state.pondBestScore;
    public static int YarnBest=>state.yarnBest;
    public static void RecordYarn(int level,int stars){if(level>=0&&level<CozyGameRules.LevelCount)state.yarnStars[level]=Mathf.Max(state.yarnStars[level],Mathf.Clamp(stars,0,3));}
    public static void RecordFish(int kind){if(kind>=0&&kind<6)state.fishMask|=1<<kind;}
    public static void Complete(CozyGameKind kind,int score){state.completedRounds++;UpdateBest(kind,score);}
    static void UpdateBest(CozyGameKind kind,int score){if(kind==CozyGameKind.Pond)state.pondBestScore=Mathf.Max(state.pondBestScore,score);else state.yarnBest=Mathf.Max(state.yarnBest,score);}
    public static void RecordScore(CozyGameKind kind,string runId,int score,DateTime utc)
    {
        UpdateBest(kind,score);var list=new List<CozyScoreRecord>(state.scores);var old=list.Find(x=>x.game==kind&&x.runId==runId);
        if(old!=null){old.score=Mathf.Max(old.score,score);old.utc=utc.ToUniversalTime().ToString("O");}else list.Add(new CozyScoreRecord{game=kind,runId=runId,score=score,utc=utc.ToUniversalTime().ToString("O")});
        if(list.Count>64)list.RemoveAt(0);state.scores=list.ToArray();
    }
    public static int PeriodBest(CozyGameKind kind,CompetitionPeriod period,DateTime utc)
    {
        if(period==CompetitionPeriod.AllTime)return kind==CozyGameKind.Yarn?YarnBest:PondBest;
        var start=utc.ToUniversalTime().Date;if(period==CompetitionPeriod.Weekly)start=start.AddDays(-((int)start.DayOfWeek+6)%7);
        int best=0;foreach(var row in state.scores)if(row.game==kind&&DateTime.TryParse(row.utc,null,System.Globalization.DateTimeStyles.RoundtripKind,out var at)&&at.ToUniversalTime()>=start)best=Mathf.Max(best,row.score);return best;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>state=new CozyGameSaveState();
}
public static class CozyGameRules
{
    public const int LevelCount=24,ShotsPerLevel=3,ContinueDiamonds=2;
    public const float YarnSeconds=180,ContinueSeconds=60,PondSeconds=75,BallRadius=.22f,MaxPull=2.1f,LaunchFactor=3.3f;
    public static readonly Vector2 BoardMin=new Vector2(-2.65f,-1.85f),BoardMax=new Vector2(2.65f,1.85f);
    public static Vector2 Mirror(Vector2 p,int level)=>level>=12?new Vector2(-p.x,-p.y):p;
    public static Vector2 Start(int level)=>Mirror(new Vector2(-2.05f,-1.2f+(level%2)*.2f),level);
    public static Vector2 Goal(int level)=>Mirror(new Vector2(1.95f,1.15f-(level%3)*.25f),level);
    public static Vector2 Launch(Vector2 pull)=>Vector2.ClampMagnitude(pull,MaxPull)*LaunchFactor;
    public static int Stars(int shots,bool bonus)=>Mathf.Clamp(4-shots+(bonus?1:0),1,3);
    public static bool IsInBasket(Vector2 p,Vector2 goal,float speed)=>Vector2.Distance(p,goal)<.30f&&speed<3.8f;
    public static int YarnScore(int level,int stars,bool pearl)=>100+Mathf.Clamp(level,0,LevelCount-1)*25+Mathf.Clamp(stars,1,3)*25+(pearl?25:0);
    public static int YarnCoins(int score)=>Mathf.Clamp(score/25,0,1000);
    public static int YarnReward(int solved)=>Mathf.Clamp(solved,0,LevelCount)*12;
    public static int PondReward(int caught)=>Mathf.Clamp(caught,0,15)*8;
    public static int PondScore(int kind,int combo,bool perfect,bool requested)=>100+Mathf.Clamp(kind,0,5)*15+Mathf.Clamp(combo-1,0,4)*25+(perfect?50:0)+(requested?75:0);
    public static float FishWindow(int kind)=>kind<2?1.2f:kind<4?.95f:.72f;
    public static readonly string[] FishTr={"Mercan yüzgeç","Nane pullu","Bal köpüğü","Gece mavisi","İnci kuyruk","Gün batımı"};
    public static readonly string[] FishEn={"Coral fin","Mint scale","Honey ripple","Midnight blue","Pearl tail","Sunset"};
    public static string FishName(int kind)=>GameContentCopy.Text(FishTr[Mathf.Clamp(kind,0,5)],FishEn[Mathf.Clamp(kind,0,5)]);
    public static List<Rect> Cushions(int level)
    {
        var list=new List<Rect>();int s=level%12;
        if(s==1)Add(list,level,0,-.05f,.55f,1.35f);
        if(s==2){Add(list,level,-.6f,.1f,1.35f,.45f);Add(list,level,.85f,-.75f,.48f,.8f);}
        if(s==3){Add(list,level,-.6f,-.65f,.5f,1.45f);Add(list,level,.7f,.75f,.5f,1.5f);}
        if(s==4){Add(list,level,-.55f,.55f,1.75f,.43f);Add(list,level,.75f,-.7f,1.15f,.43f);}
        if(s==5){Add(list,level,-.6f,.2f,.5f,1.2f);Add(list,level,.65f,-.45f,.5f,1.2f);}
        if(s==6){Add(list,level,-.3f,-.5f,1.6f,.38f);Add(list,level,.6f,.65f,.38f,1.25f);}
        if(s==7){Add(list,level,-.7f,.25f,.42f,1.75f);Add(list,level,.65f,-.8f,1.2f,.38f);}
        if(s==8){Add(list,level,-.8f,-.65f,.40f,.8f);Add(list,level,0,.55f,1.5f,.38f);Add(list,level,1,-.55f,.38f,.9f);}
        if(s==9){Add(list,level,-.8f,.55f,1.4f,.35f);Add(list,level,.65f,-.65f,.4f,1.6f);}
        if(s==10){Add(list,level,-1,.25f,.35f,1.65f);Add(list,level,.25f,-.65f,1.35f,.35f);Add(list,level,.7f,.65f,.38f,.9f);}
        if(s==11){Add(list,level,-.7f,-.65f,.38f,1.4f);Add(list,level,.35f,.6f,.38f,1.3f);Add(list,level,1.25f,-.45f,.55f,.38f);}
        return list;
    }
    static void Add(List<Rect> list,int level,float x,float y,float w,float h){var size=new Vector2(w,h);list.Add(new Rect(Mirror(new Vector2(x,y),level)-size*.5f,size));}
    public static void StepBall(ref Vector2 p,ref Vector2 velocity,List<Rect> cushions,int level,float dt)
    {
        var before=p;p+=velocity*dt;
        for(int a=0;a<2;a++){float min=BoardMin[a]+BallRadius,max=BoardMax[a]-BallRadius;if(p[a]<min||p[a]>max){p[a]=Mathf.Clamp(p[a],min,max);velocity[a]*=-.72f;}}
        foreach(var r in cushions)
        {
            var nearest=new Vector2(Mathf.Clamp(p.x,r.xMin,r.xMax),Mathf.Clamp(p.y,r.yMin,r.yMax));var delta=p-nearest;float d=delta.magnitude;
            if(d>=BallRadius)continue;if(d<.001f){p=before;velocity=-velocity*.7f;continue;}
            var normal=delta/d;p=nearest+normal*(BallRadius+.002f);if(Vector2.Dot(velocity,normal)<0)velocity=Vector2.Reflect(velocity,normal)*.8f;
        }
        velocity=Vector2.MoveTowards(velocity,Vector2.zero,(level%12>=4&&Mirror(p,level).y<0?1.5f:.9f)*dt);
    }
}

