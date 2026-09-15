using UnityEngine;

/// <summary>Observes the accepted motion and contact counters; never moves a paw or prop.</summary>
[DefaultExecutionOrder(800)]
[DisallowMultipleComponent]
public sealed class CatFoley : MonoBehaviour
{
    private CatMovement cat;
    private CatActivityAnimation animation;
    private CatActivity owner;
    private CatSurfaceTurnMotion turn;
    private CatLitterRoutineMotion litter;
    private CatGentleKneadMotion knead;
    private readonly Transform[] paws=new Transform[4];
    private readonly float[] low=new float[4],lastY=new float[4];
    private readonly bool[] lifted=new bool[4];
    private Vector3 previousRoot;
    private float nextBind,nextGesture,lastJumpPhase,nextCreak;
    private int strokes,turnSteps;
    private bool wasJump,wasHiding,leftPress,rightPress;
    private bool wasTreeScratch;
    private float lastScratchPhase;
    private CatActivityPose lastPose;
    private AudioSource water;
    private CatShowerWaterFx.Phase waterPhase;

    private void Awake()
    {
        cat=GetComponent<CatMovement>();previousRoot=transform.position;
        water=gameObject.AddComponent<AudioSource>();GameAudio.Configure(water,true,130);water.clip=GameAudio.Clip(AudioCue.Shower);
        BindPaws();
    }
    private void BindPaws()
    {
        string[] names={"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"};
        foreach(var bone in GetComponentsInChildren<Transform>())
            for(int i=0;i<4;i++)if(bone.name==names[i]){paws[i]=bone;low[i]=lastY[i]=bone.position.y;lifted[i]=false;}
    }
    private void LateUpdate()
    {
        if(Time.unscaledTime>=nextBind)
        {
            nextBind=Time.unscaledTime+1;
            if(animation==null)animation=GetComponent<CatActivityAnimation>();
            if(turn==null)turn=GetComponent<CatSurfaceTurnMotion>();
            if(litter==null)litter=GetComponent<CatLitterRoutineMotion>();
            if(knead==null)knead=GetComponent<CatGentleKneadMotion>();
            if(paws[0]==null)BindPaws();
        }
        Vector3 delta=transform.position-previousRoot;previousRoot=transform.position;
        var active=CatActivity.Active;
        if(active!=owner)
        {
            owner=active;strokes=turnSteps=0;nextGesture=0;wasHiding=wasTreeScratch=false;leftPress=rightPress=false;
            animation=GetComponent<CatActivityAnimation>();
        }
        // Motion helpers can be created midway through the first routine.
        if(owner!=null)
        {
            if(turn==null)turn=GetComponent<CatSurfaceTurnMotion>();
            if(owner is LitterDigActivity&&litter==null)litter=GetComponent<CatLitterRoutineMotion>();
            if((owner is LitterDigActivity||owner is MatKneadActivity)&&knead==null)knead=GetComponent<CatGentleKneadMotion>();
        }
        if(!GameAudio.Allowed(AudioBus.World)||cat==null||!cat.isActiveAndEnabled)
        {water.Stop();water.volume=0;wasJump=false;return;}
        bool jumping=animation!=null&&animation.IsNativeJump;
        if(jumping)
        {
            float phase=animation.NativeJumpPhase;
            if(!wasJump||phase<lastJumpPhase-.1f)lastJumpPhase=0;
            if(lastJumpPhase<CatJumpMotion.Takeoff&&phase>=CatJumpMotion.Takeoff)GameAudio.Play(AudioCue.Jump,.65f);
            if(lastJumpPhase<CatJumpMotion.Touchdown&&phase>=CatJumpMotion.Touchdown)
                GameAudio.Play(LandingCue(owner,transform.position.y),.85f);
            lastJumpPhase=phase;
        }
        wasJump=jumping;
        bool walking=!jumping&&delta.sqrMagnitude>.000002f&&delta.sqrMagnitude<.16f&&
            (animation==null||!animation.IsActive||animation.CurrentPose==CatActivityPose.Walk||animation.CurrentPose==CatActivityPose.Crawl||animation.CurrentPose==CatActivityPose.Stalk);
        Footsteps(walking,delta.y);
        if(owner==null||!owner.IsRunning){water.Stop();waterPhase=CatShowerWaterFx.Phase.Off;return;}
        PollContacts();PollGestures();PollWater();
    }
    private void Footsteps(bool moving,float vertical)
    {
        for(int i=0;i<4;i++)
        {
            if(paws[i]==null)continue;
            float y=paws[i].position.y;
            if(!moving||Mathf.Abs(vertical)>.025f){low[i]=lastY[i]=y;lifted[i]=false;continue;}
            low[i]=Mathf.Min(y,low[i]+Time.deltaTime*.025f);
            if(y>low[i]+.018f)lifted[i]=true;
            if(lifted[i]&&y<low[i]+.012f&&y<=lastY[i])
            {GameAudio.Play(animation!=null&&animation.ContactSurface!=null&&owner!=null&&owner.SupportsContinuousRest?
                AudioCue.PawFabric:StepCue(),cat.IsRunning?1.05f:.7f);lifted[i]=false;}
            lastY[i]=y;
        }
    }
    public static AudioCue StepCue()
    {
        string room=HomeRoomService.CurrentRoomId;
        if(room==HomeRoomService.BathroomId||room==HomeRoomService.KitchenId||room==HomeRoomService.PatioId)return AudioCue.PawTile;
        if(room==HomeRoomService.GardenId)return AudioCue.PawGrass;
        return AudioCue.PawWood;
    }
    public static AudioCue LandingCue(CatActivity activity,float height)
    {
        if(height>.15f&&activity!=null&&(activity.SupportsContinuousRest||activity is HamperDiveActivity||activity is MatKneadActivity))return AudioCue.LandSoft;
        return StepCue()==AudioCue.PawTile?AudioCue.LandTile:StepCue()==AudioCue.PawGrass?AudioCue.LandSoft:AudioCue.LandWood;
    }
    private void PollContacts()
    {
        int count=0;AudioCue cue=ContactCue(owner);
        if(owner is ScratchPostActivity scratch)count=scratch.LeftStrokes+scratch.RightStrokes;
        else if(owner is CatEnrichmentActivity toy)count=toy.ContactCount;
        else if(owner is PaperSpinActivity paper)count=paper.PaperContactCount+paper.RecordContactCount;
        else if(owner is SitLookActivity look&&look.ReactionKind!=SitLookReaction.Sit)
        {count=look.GestureBeats;cue=look.Kind==CatActivityKind.BirdWatch?AudioCue.Bird:AudioCue.Ribbon;}
        else if(owner is LitterDigActivity&&litter!=null&&litter.IsActive)count=litter.ContactStrokes;
        // The sapling's irregular bark does not expose the post's planar
        // contact counter. Its authored downward strokes still have exact phases.
        bool treeStroke=owner.Kind==CatActivityKind.TreeScratch&&animation!=null&&animation.IsActive&&animation.CurrentPose==CatActivityPose.Scratch;
        if(treeStroke)
        {
            float phase=animation.NativeJumpPhase;
            if(!wasTreeScratch||phase<lastScratchPhase)lastScratchPhase=0;
            if(lastScratchPhase<.4f&&phase>=.4f||lastScratchPhase<.9f&&phase>=.9f)GameAudio.Play(AudioCue.ScratchWood,.75f);
            lastScratchPhase=phase;
        }
        else if(count>strokes)
        {
            GameAudio.Play(cue,1);
            if(owner.Kind==CatActivityKind.PaperSpin)GameAudio.Play(AudioCue.PaperRoll,.35f);
        }
        wasTreeScratch=treeStroke;strokes=count;
        if(turn!=null&&turn.IsTurning)
        {
            if(turn.CompletedSteps>turnSteps)GameAudio.Play(owner.SupportsContinuousRest?AudioCue.PawFabric:StepCue(),.40f);
            turnSteps=turn.CompletedSteps;
        }
        else turnSteps=0;
        if(knead!=null&&knead.IsActive)
        {
            bool left=knead.LeftLift>.006f,right=knead.RightLift>.006f;
            if(leftPress&&!left||rightPress&&!right)GameAudio.Play(knead.IsScraping?AudioCue.DigSoil:AudioCue.Cloth,.6f);
            leftPress=left;rightPress=right;
        }
    }
    public static AudioCue ContactCue(CatActivity activity)
    {
        if(activity==null)return AudioCue.WoodTap;
        if(activity is ScratchPostActivity)return activity.Kind==CatActivityKind.ScratchPost?AudioCue.ScratchRope:AudioCue.ScratchWood;
        if(activity is LitterDigActivity)return activity.Kind==CatActivityKind.LitterDig?AudioCue.DigSand:AudioCue.DigSoil;
        switch(activity.Kind)
        {
            case CatActivityKind.PaperSpin:return AudioCue.PaperTear;
            case CatActivityKind.RecordSpin:return AudioCue.Record;
            case CatActivityKind.ToyMousePlay:return AudioCue.ToySqueak;
            case CatActivityKind.BellRoller:return AudioCue.Bell;
            case CatActivityKind.RibbonPlay:return AudioCue.Ribbon;
            case CatActivityKind.CatGrassPlay:return AudioCue.Leaves;
            case CatActivityKind.BallTrack:return AudioCue.BallRoll;
            case CatActivityKind.TreatPuzzle:case CatActivityKind.FoodDispenser:return AudioCue.WoodTap;
            case CatActivityKind.KnockOff:return AudioCue.GlassTap;
            case CatActivityKind.TableKnockOff:return AudioCue.CeramicTap;
            case CatActivityKind.BookKnockOff:return AudioCue.BookLand;
            case CatActivityKind.FruitSwat:return AudioCue.Cloth;
            case CatActivityKind.DiningScatter:return AudioCue.CeramicTap;
            default:return AudioCue.BallTap;
        }
    }
    private void PollGestures()
    {
        if(animation==null||!animation.IsActive)return;
        var pose=animation.CurrentPose;
        if(pose!=lastPose){nextGesture=Time.time+.12f;lastPose=pose;}
        if(Time.time>=nextGesture)
        {
            if(pose==CatActivityPose.Sniff){GameAudio.Play(AudioCue.Sniff,.6f);nextGesture=Time.time+2.5f;}
            else if(pose==CatActivityPose.Groom){GameAudio.Play(AudioCue.Groom,.7f);nextGesture=Time.time+.75f;}
            else if(pose==CatActivityPose.Crawl){GameAudio.Play(AudioCue.Cloth,.6f);nextGesture=Time.time+.8f;}
            else if(pose==CatActivityPose.Stretch||pose==CatActivityPose.TowelSettle){GameAudio.Play(AudioCue.Cloth,.5f);nextGesture=Time.time+2f;}
        }
        if(owner is HamperDiveActivity hamper)
        {if(hamper.IsHiding&&!wasHiding)GameAudio.Play(AudioCue.Cloth,1.1f);wasHiding=hamper.IsHiding;}
        if(owner is SwingRideActivity&&owner.IsWaitingForRestStop&&Time.time>=nextCreak)
        {GameAudio.Play(AudioCue.Creak,.7f);nextCreak=Time.time+3.6f;}
    }
    private void PollWater()
    {
        var fx=owner is ShowerRinseActivity?owner.GetComponent<CatShowerWaterFx>():null;
        var phase=fx==null?CatShowerWaterFx.Phase.Off:fx.CurrentPhase;
        if(phase==CatShowerWaterFx.Phase.Rinsing&&waterPhase!=phase)GameAudio.Play(AudioCue.WaterDrop,.4f);
        if(phase==CatShowerWaterFx.Phase.ShakeOff&&waterPhase!=phase)GameAudio.Play(AudioCue.Shake,.9f);
        waterPhase=phase;
        AudioClip wanted=null;float level=.16f;
        if(phase==CatShowerWaterFx.Phase.Rinsing)wanted=GameAudio.Clip(AudioCue.Shower);
        else if(animation!=null&&animation.IsActive)
        {
            if(owner is SinkSipActivity&&animation.CurrentPose==CatActivityPose.Drink){wanted=GameAudio.Clip(AudioCue.Fountain);level=.075f;}
            else if(owner.Kind==CatActivityKind.TelevisionWatch&&animation.CurrentPose==CatActivityPose.Sit){wanted=GameAudio.Clip(AudioCue.Television);level=.045f;}
            else if((owner.Kind==CatActivityKind.OvenWarmth||owner.Kind==CatActivityKind.FirePitBask)&&owner.IsWaitingForRestStop)
            {wanted=GameAudio.Clip(AudioCue.FireCrackle);level=.06f;}
        }
        if(wanted==null){water.Stop();water.volume=0;return;}
        if(water.clip!=wanted){water.Stop();water.volume=0;water.clip=wanted;}
        water.volume=Mathf.MoveTowards(water.volume,level,Time.deltaTime*.5f);if(!water.isPlaying)water.Play();
    }
    private void OnDisable(){if(water!=null)water.Stop();}
}
