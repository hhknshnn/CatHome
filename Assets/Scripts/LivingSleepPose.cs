using UnityEngine;

// A relaxed side-sleep overlay on the existing Polyperfect Sleeping loop.
// Joint rotations only; original model, limbs, scale and breathing clip remain.
[DefaultExecutionOrder(525)]
public sealed class LivingSleepPose : MonoBehaviour
{
    SleepInteraction sleep;
    Animator animator;
    Transform hips,neck,head;
    Quaternion hipSource,neckSource,headSource;
    bool adjusted,waking,ownsIndicator;
    float weight;
    public float Weight=>weight;
    public void PrepareWake(){waking=true;}
    void Update(){Restore();}
    void LateUpdate()
    {
        if(sleep==null)sleep=GetComponent<SleepInteraction>();
        if(sleep==null)return;
        var current=GetComponentInChildren<Animator>();
        if(animator!=current)
        {
            Restore();animator=current;weight=0;
            hips=CatBreedVisualFactory.FindDescendant(transform,"DEF-spine");
            neck=CatBreedVisualFactory.FindDescendant(transform,"DEF-spine.004");
            head=CatBreedVisualFactory.FindDescendant(transform,"DEF-spine.006");
        }
        if(animator==null||hips==null||neck==null||head==null)return;
        var activity=CatActivity.Active;
        bool furniture=activity!=null&&activity.BelongsTo(GetComponent<CatMovement>())&&activity.SupportsContinuousRest&&activity.IsRestingOnFurniture&&GetComponent<CatActivityAnimation>().CurrentPose==CatActivityPose.Sleep;
        if(!sleep.IsSleeping&&!furniture)waking=false;
        bool resting=LivingRoomSpeech.IsLiving&&!waking&&((sleep.IsSettledOnBed&&animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Sleep"))||furniture);
        var indicator=GetComponent<CatSleepZzzEffect>();
        if(furniture&&resting&&!ownsIndicator&&indicator!=null){indicator.Begin();ownsIndicator=true;}
        if(ownsIndicator&&(!furniture||!resting)){if(indicator!=null)indicator.Stop();ownsIndicator=false;}
        weight=Mathf.MoveTowards(weight,resting?1:0,Time.deltaTime/(waking?.35f:.80f));
        if(weight<=0)return;
        hipSource=hips.localRotation;neckSource=neck.localRotation;headSource=head.localRotation;adjusted=true;
        // Rest on a flank, with the chin softly towards the tucked paws. The
        // existing contact fitter places the real skin back on the mattress.
        hips.rotation=Quaternion.AngleAxis(62f*weight,transform.forward)*hips.rotation;
        neck.rotation=Quaternion.AngleAxis(18f*weight,transform.up)*neck.rotation;
        head.rotation=Quaternion.AngleAxis(14f*weight,transform.right)*head.rotation;
    }
    void Restore(){if(!adjusted)return;if(hips!=null)hips.localRotation=hipSource;if(neck!=null)neck.localRotation=neckSource;if(head!=null)head.localRotation=headSource;adjusted=false;}
    void OnDisable(){Restore();weight=0;waking=false;if(ownsIndicator)GetComponent<CatSleepZzzEffect>()?.Stop();ownsIndicator=false;}
}
