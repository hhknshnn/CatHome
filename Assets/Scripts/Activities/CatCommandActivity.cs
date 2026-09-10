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
    public override string DisplayName=>command==CatCompanionCommand.Loaf?GameContentCopy.Text("Loaf keyfi","Loaf time"):command==CatCompanionCommand.Sit?GameContentCopy.Text("Birlikte oturalım","Sit with me"):GameContentCopy.Text("Miyav!","Meow!");
    public override string ProgressLabel=>DisplayName;
    public override bool TryGetPromptDistance(CatMovement cat,out float distance){distance=float.PositiveInfinity;return false;}
    public bool Issue(CatCompanionCommand value)
    {
        var cat=GetComponent<CatMovement>();
        if(cat==null||cat.IsMovementLocked||cat.AreWorldActionsBlocked||CatActionState.IsBusy(cat))return false;
        if(!CatActivityMotion.IsFloorClear(cat.transform.position,.28f))
        {GetComponent<CatSpeechBubble>()?.Show(GameContentCopy.Text("Biraz açık alana geçelim.","Let's find a little open space."));return false;}
        command=value;return TryStart(cat);
    }
    protected override bool BeginActivity(){StartCoroutine(Routine());return true;}
    private IEnumerator Routine()
    {
        Cat.SetMovementLocked(this,true);
        Quaternion presentation = CatActivityFacing.Resolve(Cat, Cat.transform.position, Cat.transform.rotation);
        if(command==CatCompanionCommand.Meow)
        {
            yield return CatActivityFacing.Turn(Cat, presentation);
            var voice=CatVoice.EnsureOn(Cat);
            PlayCatPose(CatActivityPose.Meow);voice.Meow();
            yield return new WaitForSeconds(Mathf.Max(3f,voice.MeowDuration));
        }
        else
        {
            // Turn during the existing entry window; only the held pose earns
            // rest energy, with the same .7 s entry as before.
            float entryStarted = Time.time;
            PlayCatPose(CatActivityPose.SitDown);
            yield return CatActivityFacing.Turn(Cat, presentation);
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
