using System.Collections;
using UnityEngine;

/// <summary>
/// Shove the dish cart and watch it roll.
///
/// The whole cart rolls, so the routine drives the VisualContent transform.
/// After the cat leaves its footprint, the cart is left exactly
/// where it started — a product that drifted a little further every time the cat
/// played with it would eventually be somewhere the catalog never placed it.
///
/// The cat stays planted beside the actual shelf. A measured paw contact
/// starts each roll; the cart never rebounds through the cat.
/// </summary>
[DisallowMultipleComponent]
public sealed class CartNudgeActivity : CatActivity
{
    [Header("Cart nudge")]
    [SerializeField] private Transform shovePoint;
    [SerializeField] private Transform cartVisual;
    [SerializeField] private Vector3 rollDirection = Vector3.right;
    [SerializeField, Min(0.02f)] private float rollDistance = 0.32f;
    [SerializeField, Min(1)] private int shoveCount = 2;

    private CharacterController characterController;
    private Vector3 visualHome;
    private bool visualHomeCaptured;
    private CatToyContactMotion contact;
    public bool IsPushing {get;private set;}
    public int ContactStrokes {get;private set;}
    public float MinimumPawDistance {get;private set;}
    public Vector3 LastStrikePosition {get;private set;}
    public Vector3 ContactPoint {get;private set;}

    public override string ProgressLabel => IsRunning ? "PUSHING..." : string.Empty;

    public float RollDistance => Mathf.Max(0.02f, rollDistance);
    public int ShoveCount => Mathf.Max(1, shoveCount);
    public Transform CartVisual => cartVisual;
    public Vector3 WorldRollDirection => transform.TransformDirection(rollDirection.sqrMagnitude > .0001f ? rollDirection.normalized : Vector3.right).normalized;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (shovePoint == null || cartVisual == null)
        {
            failureReason = "THE CART IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        if (!visualHomeCaptured)
        {
            visualHome = cartVisual.localPosition;
            visualHomeCaptured = true;
        }
        ContactStrokes=0;MinimumPawDistance=float.PositiveInfinity;
        StartCoroutine(NudgeRoutine());
        return true;
    }

    private IEnumerator NudgeRoutine()
    {
        Cat.SetMovementLocked(this,true);if(characterController!=null)characterController.enabled=false;
        Vector3 origin=Cat.transform.position;Quaternion facing=LookTowards(WorldRollDirection,Cat.transform.rotation);
        contact=Cat.GetComponent<CatToyContactMotion>()??Cat.gameObject.AddComponent<CatToyContactMotion>();
        var animation=Cat.GetComponent<CatActivityAnimation>();
        for(int beat=0;beat<ShoveCount;beat++)
        {
            if(!PlanPush(out Vector3 stand,out Vector3 target,out var path)){CancelForTransition();yield break;}
            facing=PushingFacing(stand,target);ContactPoint=target;
            PlayCatPose(CatActivityPose.GentleKnead);yield return CatActivityFacing.Turn(Cat,Quaternion.LookRotation(Vector3.Cross(WorldRollDirection,Vector3.up)),.20f);
            for(int step=0;step<path.Count;step++)
            {
                if(step==path.Count-1)
                {
                    // Turn through the open aisle, never sweep the head into the stove.
                    PlayCatPose(CatActivityPose.GentleKnead);
                    yield return CatActivityFacing.Turn(Cat,Quaternion.LookRotation(Vector3.Cross(WorldRollDirection,Vector3.up)),.20f);
                    yield return CatActivityFacing.Turn(Cat,facing,.25f);
                }
                var arrival=step==path.Count-1?facing:LookTowards(path[step]-Cat.transform.position,Cat.transform.rotation);
                yield return CatActivityMotion.WalkAuthoredStep(Cat,path[step],arrival,.18f);
            }
            bool left=Vector3.Dot(target-stand,facing*Vector3.right)<0;
            bool touched=false;float elapsed=0;IsPushing=true;
            while(elapsed<.80f)
            {
                float t=elapsed/.80f;Cat.transform.SetPositionAndRotation(stand,facing);
                animation.SetTimedPose(left?CatActivityPose.BatLeft:CatActivityPose.BatRight,t);
                float phase=t<.32f?.42f*t/.32f:t<.62f?.42f:Mathf.Lerp(.42f,1,(t-.62f)/.38f);
                contact.Reach(target,left,phase,16);
                if(t>.28f&&t<.72f){MinimumPawDistance=Mathf.Min(MinimumPawDistance,contact.Distance);
                    if(!touched&&contact.Distance<.028f){LastStrikePosition=contact.LastContactPosition;touched=true;GameAudio.Play(AudioCue.WoodTap,.7f);}}
                yield return null;elapsed+=Time.deltaTime;
            }
            IsPushing=false;contact.Clear();
            if(!touched){CancelForTransition();yield break;}
            ContactStrokes++;
            GameAudio.Play(AudioCue.WheelRoll,.8f);
            Vector3 start=cartVisual.position;
            var away=cartVisual.GetComponentInChildren<Renderer>().bounds.center-LastStrikePosition;
            // Wheels constrain a diagonal shove to their rolling axis.
            var roll=Vector3.Dot(away,WorldRollDirection)>=0?WorldRollDirection:-WorldRollDirection;
            Vector3 end=start+roll*(RollDistance/ShoveCount);
            elapsed=0;
            while(elapsed<.55f)
            {
                PlayCatPose(CatActivityPose.GentleKnead);
                float t=Mathf.Clamp01(elapsed/.55f);cartVisual.position=Vector3.Lerp(start,end,1-(1-t)*(1-t));
                yield return null;elapsed+=Time.deltaTime;
            }
            cartVisual.position=end;yield return new WaitForSeconds(.15f);
        }
        // Leave the cart's reset footprint before restoring it and physics.
        PlayCatPose(CatActivityPose.GentleKnead);yield return CatActivityFacing.Turn(Cat,Quaternion.LookRotation(Vector3.Cross(WorldRollDirection,Vector3.up)),.20f);
        if(CatActivityMotion.TryFloorPath(Cat.transform.position,origin,out var exit))
            foreach(var destination in exit)yield return CatActivityMotion.WalkAuthoredStep(Cat,destination,
                LookTowards(destination-Cat.transform.position,Cat.transform.rotation),.18f);
        RestoreCat();CompleteActivity("IT MOVES!");
    }

    private bool PlanPush(out Vector3 stand,out Vector3 target,out System.Collections.Generic.List<Vector3> path)
    {
        stand=Cat.transform.position;target=Vector3.zero;path=null;
        var candidates=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Vector3,Vector3>>();
        Vector3 seed=RoutineFloorPosition;seed.y=Cat.transform.position.y;
        for(int x=-6;x<=6;x++)for(int z=-6;z<=6;z++)
        {
            Vector3 p=seed+new Vector3(x*.04f,0,z*.04f);
            if(!CatActivityMotion.IsFloorClear(p))continue;
            var contact=MeasureContact(p);if(float.IsInfinity(contact.x)||Vector3.Distance(p,contact)>.55f||Vector3.ProjectOnPlane(contact-p,Vector3.up).magnitude<.32f)continue;
            candidates.Add(new System.Collections.Generic.KeyValuePair<Vector3,Vector3>(p,contact));
        }
        candidates.Sort((a,b)=>Vector3.Distance(a.Key,a.Value).CompareTo(Vector3.Distance(b.Key,b.Value)));
        foreach(var pair in candidates)
        {
            var candidate=pair.Key;var contact=pair.Value;var forward=PushingFacing(candidate,contact)*Vector3.forward;
            var staging=candidate-forward*.10f;
            if(!CatActivityMotion.ClearSegment(staging,candidate)||!CatActivityMotion.TryFloorPath(Cat.transform.position,staging,out path))continue;
            path.Add(candidate);stand=candidate;target=contact;return true;
        }
        return false;
    }

    private Quaternion PushingFacing(Vector3 stand,Vector3 target)
    {
        var aim=LookTowards(target-stand,Cat.transform.rotation);
        var left=aim*Quaternion.Euler(0,-25,0);var right=aim*Quaternion.Euler(0,25,0);
        var camera=CatActivityFacing.CameraPosition(Cat);
        bool leftClear=HeadClear(stand,left),rightClear=HeadClear(stand,right);
        if(leftClear!=rightClear)return leftClear?left:right;
        return CatActivityFacing.FacingDot(left*Vector3.forward,stand,camera)>CatActivityFacing.FacingDot(right*Vector3.forward,stand,camera)?left:right;
    }

    private bool HeadClear(Vector3 stand,Quaternion facing)
    {
        foreach(var collider in Physics.OverlapSphere(stand+facing*Vector3.forward*.30f+Vector3.up*.38f,.075f,~0,QueryTriggerInteraction.Ignore))
            if(collider.GetComponentInParent<CatMovement>()==null)return false;
        return true;
    }

    private Vector3 MeasureContact(Vector3 stand)
    {
        Bounds bounds=cartVisual.GetComponentInChildren<Renderer>().bounds;
        Vector3 toward=bounds.center-stand;toward.y=0;toward.Normalize();
        Vector3 best=Vector3.positiveInfinity;float cost=float.PositiveInfinity;
        foreach(float height in new[]{.25f,.30f,.20f,.35f,.40f})
        foreach(var mesh in cartVisual.GetComponentsInChildren<MeshCollider>())
        {
            var from=stand;from.y=height;
            RaycastHit hit;if(!mesh.Raycast(new Ray(from,toward),out hit,1.4f))continue;
            float distance=hit.distance+Mathf.Abs(height-.40f);
            if(distance>=cost)continue;cost=distance;best=hit.point+hit.normal*.012f;
        }
        return best;
    }

    private void RestoreCat()
    {
        IsPushing=false;contact?.Clear();
        if (cartVisual != null && visualHomeCaptured)
            cartVisual.localPosition = visualHome;
        if (Cat == null)
            return;

        if (characterController != null)
            characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    private static Quaternion LookTowards(Vector3 direction, Quaternion fallback)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : fallback;
    }

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        RestoreCat();
        base.CancelActivity();
    }

#if UNITY_EDITOR
    public void EditorConfigureNudge(
        Transform shove, Transform visual, Vector3 direction, float distance, int shoves)
    {
        shovePoint = shove;
        cartVisual = visual;
        rollDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        rollDistance = Mathf.Max(0.02f, distance);
        shoveCount = Mathf.Max(1, shoves);
    }
#endif
}
