using System.Collections;
using UnityEngine;

public enum CatCompanionCommand { Meow, Sit, Loaf }

/// <summary>Player-requested companionship at the cat's current clear floor position.</summary>
[DisallowMultipleComponent]
public sealed class CatCommandActivity : CatActivity
{
    private CatCompanionCommand command;
    public CatCompanionCommand Command=>command;
    public const float RestEnergyPerSecond = .35f;
    public bool IsRecoveringEnergy => SupportsContinuousRest && IsWaitingForRestStop;
    public override CatActivityKind Kind=>CatActivityKind.CompanionCommand;
    public override float EnergyCost=>0;
    public override bool SupportsContinuousRest=>command!=CatCompanionCommand.Meow;
    protected override bool RecordsQuestProgress=>false;
    protected override bool UsesFloorApproach=>false;
    protected override bool UsesPreparedStart=>true;
    protected override bool TryPrepareStart(CatMovement actor,out CatActivityStart start)
    {
        start=default;
        return actor!=null && CatActivityStartResolver.Current(actor,actor.transform.position,0f,out start);
    }
    public override string DisplayName=>command==CatCompanionCommand.Loaf?GameLanguageService.Text("companion.loaf_progress"):command==CatCompanionCommand.Sit?GameContentCopy.Text("Birlikte oturalım","Sit with me"):GameContentCopy.Text("Miyav!","Meow!");
    public override string ProgressLabel=>DisplayName;
    public override bool TryGetPromptDistance(CatMovement cat,out float distance){distance=float.PositiveInfinity;return false;}
    public bool Issue(CatCompanionCommand value)
    {
        var cat=GetComponent<CatMovement>();
        if(cat==null||cat.IsMovementLocked||cat.AreWorldActionsBlocked||CatActionState.IsBusy(cat))return false;
        if(!cat.IsBodyPoseClear(cat.transform.position,cat.transform.rotation))
        {GetComponent<CatSpeechBubble>()?.Show(GameContentCopy.Text("Biraz açık alana geçelim.","Let's find a little open space."));return false;}
        command=value;return TryStart(cat);
    }
    protected override bool BeginActivity(){StartCoroutine(Routine());return true;}
    private IEnumerator Routine()
    {
        Cat.SetMovementLocked(this,true);
        if(command==CatCompanionCommand.Meow)
        {
            var voice=CatVoice.EnsureOn(Cat);
            PlayCatPose(CatActivityPose.Meow);voice.Meow();
            yield return new WaitForSeconds(Mathf.Max(3f,voice.MeowDuration));
        }
        else
        {
            // The player's heading is already the accepted resting heading.
            float entryStarted = Time.time;
            PlayCatPose(CatActivityPose.SitDown);
            while (Time.time - entryStarted < .7f) yield return null;
            PlayCatPose(command==CatCompanionCommand.Loaf?CatActivityPose.Loaf:CatActivityPose.Sit);
            while(KeepResting)yield return null;
            if(command==CatCompanionCommand.Loaf){PlayCatPose(CatActivityPose.Sit);yield return new WaitForSeconds(.4f);}
            PlayCatPose(CatActivityPose.StandUp);yield return new WaitForSeconds(.7f);
        }
        Cat.SetMovementLocked(this,false);CompleteActivity(GameContentCopy.Text("Yanında olmak güzel.","I like being with you."));
    }
    protected override void CancelActivity()
    {StopAllCoroutines();if(Cat!=null)Cat.SetMovementLocked(this,false);base.CancelActivity();}
}
