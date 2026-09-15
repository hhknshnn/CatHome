using UnityEngine;

/// <summary>Quiet paw contacts observed from each mini-game cat's animated feet.</summary>
[DefaultExecutionOrder(810)]
public sealed class MiniGameFoley : MonoBehaviour
{
    private CatRunnerPlayer runner;
    private CatCatchPlayer hunter;
    private readonly Transform[] paws=new Transform[4];
    private readonly float[] floor=new float[4],previous=new float[4];
    private readonly bool[] lifted=new bool[4];
    private Vector3 lastPosition;
    private float nextBind;
    private void Awake(){runner=GetComponent<CatRunnerPlayer>();hunter=GetComponent<CatCatchPlayer>();lastPosition=transform.position;}
    private void LateUpdate()
    {
        if(paws[0]==null&&Time.unscaledTime>=nextBind)
        {
            nextBind=Time.unscaledTime+1;
            string[] names={"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"};
            foreach(var bone in GetComponentsInChildren<Transform>())for(int i=0;i<4;i++)
                if(bone.name==names[i]){paws[i]=bone;floor[i]=previous[i]=bone.position.y;lifted[i]=false;}
        }
        Vector3 delta=transform.position-lastPosition;lastPosition=transform.position;
        bool moving=GameAudio.Allowed(AudioBus.MiniGame)&&(runner!=null?runner.IsRunning&&!runner.IsAirborne&&!runner.IsSliding:
            hunter!=null&&!hunter.IsBusy&&delta.sqrMagnitude>.000002f&&delta.sqrMagnitude<.16f);
        for(int i=0;i<4;i++)
        {
            if(paws[i]==null)continue;float y=paws[i].position.y;
            if(!moving){floor[i]=previous[i]=y;lifted[i]=false;continue;}
            floor[i]=Mathf.Min(y,floor[i]+Time.deltaTime*.025f);
            if(y>floor[i]+.018f)lifted[i]=true;
            if(lifted[i]&&y<floor[i]+.012f&&y<=previous[i])
            {GameAudio.Play(runner!=null?AudioCue.PawWood:AudioCue.PawGrass,.6f,AudioBus.MiniGame);lifted[i]=false;}
            previous[i]=y;
        }
    }
}
