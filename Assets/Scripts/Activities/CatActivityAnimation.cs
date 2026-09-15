using UnityEngine;
using System.Collections.Generic;

public enum CatActivityPose { Walk, Sit, Sleep, Eat, Drink, Paw, Scratch, Hop, Crawl, Groom, Stalk, Pounce, Sniff, BatLeft, BatRight, Push, Tug, Stretch, Loaf, Meow, StandUp, SitDown, GentleKnead, TowelJumpUp, TowelJumpDown, TowelSettle, TowelWake }

/// <summary>
/// One animation owner for a scripted furniture routine. The shared controller
/// binds to every breed through CatBreedVisualFactory's normalized armature.
/// Furniture motion moves CatRoot; skeletal poses never squash that root.
/// </summary>
[DefaultExecutionOrder(500)]
[DisallowMultipleComponent]
public sealed class CatActivityAnimation : MonoBehaviour
{
    private CatActivity owner;
    private Animator animator;
    private Transform visual;
    private SkinnedMeshRenderer skin;
    private Mesh sampledMesh;
    private Vector3 rootScale;
    private Vector3 visualPosition;
    private Vector3 visualScale;
    private Quaternion visualRotation;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> bodyVertices = new List<int>();
    private CatActivityPose pose;
    private Transform support;
    private int stateHash;
    private bool needsState;
    private CatMovement movement;
    private bool scriptedPhase;
    private float phase;
    private float walkMetresPerSecond = -1f;
    private CatHomeLocomotionCatalog locomotion;
    private float horizontalSupportBlend = 1f;
    private bool nativeJump;
    private Vector3 jumpStartOffset, jumpEndOffset;
    private float jumpOffsetBlend;
    private bool ownsAnimatorSpeed;
    private float previousAnimatorSpeed;

    public bool IsActive => owner != null && owner.IsRunning;
    public CatActivityPose CurrentPose => pose;
    public Transform ContactSurface => support;
    public bool IsNativeJump => IsActive && nativeJump;
    public float NativeJumpPhase => phase;

    public void Begin(CatActivity activity)
    {
        End();
        owner = activity;
        movement = GetComponent<CatMovement>();
        rootScale = transform.localScale;
        SetPose(CatActivityPose.Walk);
    }

    public void SetPose(CatActivityPose value, Transform contactSurface = null)
    {
        nativeJump = false;
        scriptedPhase = false;
        horizontalSupportBlend = 1f;
        walkMetresPerSecond = -1f;
        var area = contactSurface != null ? contactSurface.GetComponent<CatActivitySurface>() : null;
        if (area != null) value = area.ResolvePose(value);
        needsState |= pose != value || stateHash == 0;
        pose = value;
        support = contactSurface;
    }

    public void SetTimedPose(CatActivityPose value, float normalizedTime, Transform contactSurface = null)
    {
        SetPose(value,contactSurface); scriptedPhase=true; phase=Mathf.Clamp01(normalizedTime);
    }

    public void SetWalkSpeed(float metresPerSecond, Transform contactSurface)
    {
        SetPose(CatActivityPose.Walk, contactSurface);
        walkMetresPerSecond = Mathf.Max(0f, metresPerSecond);
    }

    public void SetHorizontalSupportBlend(float blend) => horizontalSupportBlend = Mathf.Clamp01(blend);

    // A jump has one translation owner. Keep planted paws at their authored
    // horizontal position; transfer seat centering only while airborne.
    public void BeginNativeJump(bool landOnSupport)
    {
        GetComponent<CatSurfaceTurnMotion>()?.Clear();
        ResolveVisual();
        if (animator == null || visual == null || skin == null) return;
        jumpStartOffset = transform.InverseTransformVector(visual.position - visual.parent.TransformPoint(visualPosition));
        jumpStartOffset.y = 0f;
        RestoreVisual();
        animator.Play("Base Layer.NativeJump", 0, 0f);
        animator.Update(0f);
        if (sampledMesh == null) sampledMesh = new Mesh { name = "Cat contact sample" };
        skin.BakeMesh(sampledMesh, true); sampledMesh.GetVertices(vertices);
        Bounds bounds = new Bounds(); bool first = true;
        foreach (int i in bodyVertices)
        {
            Vector3 point = transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
            if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
            else bounds.Encapsulate(point);
        }
        jumpEndOffset = landOnSupport && !first ? new Vector3(-bounds.center.x, 0f, -bounds.center.z) : Vector3.zero;
    }

    public void SetNativeJumpSample(CatActivityPose value, float normalizedTime, float airborneBlend, bool linearOffset = false)
    {
        SetTimedPose(value, normalizedTime);
        nativeJump = true;
        jumpOffsetBlend = linearOffset ? Mathf.Clamp01(airborneBlend) : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(airborneBlend));
    }

    private void RestoreAnimatorSpeed()
    {
        if (ownsAnimatorSpeed && animator != null) animator.speed = previousAnimatorSpeed;
        ownsAnimatorSpeed = false;
    }

    private void Update()
    {
        if (!IsActive || movement == null || !movement.IsMovementPhysicallyLocked) return;
        ResolveVisual();
        if (animator == null || !animator.isActiveAndEnabled) return;
        if (!scriptedPhase) RestoreAnimatorSpeed();
        string state = StateFor(pose);
        int hash = Animator.StringToHash("Base Layer." + state);
        if (scriptedPhase && animator.HasState(0, hash))
        {
            if (!ownsAnimatorSpeed) { previousAnimatorSpeed = animator.speed; ownsAnimatorSpeed = true; }
            animator.speed = 0f;
            animator.Play(hash, 0, phase); stateHash=hash; needsState=false;
        }
        else if (needsState && animator.HasState(0, hash))
        {
            animator.CrossFadeInFixedTime(hash, .12f, 0);
            stateHash = hash;
            needsState = false;
        }
        // Runs after CatMovement, which correctly clears locomotion input while locked.
        animator.SetFloat("Speed", pose == CatActivityPose.Walk ? .5f : 0f);
        if (pose == CatActivityPose.Walk && walkMetresPerSecond >= 0f)
        {
            if (locomotion == null) locomotion = Resources.Load<CatHomeLocomotionCatalog>(CatHomeLocomotionCatalog.ResourceName);
            var tag = animator.GetComponentInParent<CatBreedVisualTag>();
            var entry = locomotion != null ? locomotion.Find(tag != null ? tag.BreedId : CatBreedCatalog.DefaultBreedId) : null;
            float blend = CatHomeLocomotionCatalog.RunBlendForSpeed(walkMetresPerSecond);
            animator.SetFloat("Speed", Mathf.Lerp(.5f, 1f, blend));
            animator.SetFloat("LocomotionRate", entry != null ? entry.PlaybackRate(walkMetresPerSecond, blend, transform.lossyScale.x) : 1f);
        }
    }

    private void LateUpdate()
    {
        if (!IsActive || movement == null || !movement.IsMovementPhysicallyLocked) return;
        // Legacy routines used scale squashes for poses. Preserve the cat's proportions
        // while the actual skeleton animates, including on larger/smaller breeds.
        transform.localScale = rootScale;
        if (visual == null || skin == null) return;
        // Coroutines set this frame's trajectory after Update. Evaluate that
        // exact pose here, before contact correction, without an extra frame.
        if (scriptedPhase && animator.isActiveAndEnabled)
        {
            if (!ownsAnimatorSpeed) { previousAnimatorSpeed = animator.speed; ownsAnimatorSpeed = true; }
            animator.speed = 0f;
            animator.Play(Animator.StringToHash("Base Layer." + StateFor(pose)), 0, phase);
            animator.Update(0f);
        }
        visual.localPosition = visualPosition;
        visual.localScale = visualScale;
        visual.localRotation = visualRotation;
        var area = support != null ? support.GetComponent<CatActivitySurface>() : null;
        Vector3 up = support != null ? support.up : Vector3.up;
        if (pose != CatActivityPose.Hop && pose != CatActivityPose.Walk && pose != CatActivityPose.Crawl)
        {
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up);
            if (forward.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(forward, up);
        }
        if (area != null && area.AlignAlongSurface)
            visual.rotation = CatActivityFacing.AlongAxis(movement, support.position,
                support.rotation * Quaternion.Euler(0f, 90f, 0f)) * visualRotation;
        if (sampledMesh == null) sampledMesh = new Mesh { name = "Cat contact sample" };
        // Account for the breed hierarchy's scale before transforming to world
        // space. The default overload expands this sample again under the 1.5x
        // skin hierarchy, lifting the visible paws above the measured surface.
        skin.BakeMesh(sampledMesh, true);
        sampledMesh.GetVertices(vertices);
        // Missing custom-breed profiles still work without reading an imported mesh.
        if (bodyVertices.Count == 0)
            for (int i = 0; i < vertices.Count; i++) bodyVertices.Add(i);
        if (bodyVertices.Count == 0 || vertices.Count == 0) return;
        Matrix4x4 contactFrame = support != null ? support.worldToLocalMatrix :
            Matrix4x4.TRS(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), Vector3.one).inverse;
        Bounds bounds = new Bounds(contactFrame.MultiplyPoint3x4(
            skin.transform.TransformPoint(vertices[bodyVertices[0]])), Vector3.zero);
        foreach (int i in bodyVertices)
            bounds.Encapsulate(contactFrame.MultiplyPoint3x4(skin.transform.TransformPoint(vertices[i])));

        // Contact areas are measured on the BUILT prefab, never inferred from its box.
        // Keep the breed's natural scale. Tail tips can hang below a seat and
        // must not lift the paws off it or drag the body centre backwards.
        if (nativeJump)
        {
            visual.position += transform.TransformVector(Vector3.Lerp(jumpStartOffset, jumpEndOffset, jumpOffsetBlend));
            visual.position += Vector3.up * (-bounds.min.y + .008f);
        }
        else if (area != null)
        {
            Vector3 correction = new Vector3(-bounds.center.x * horizontalSupportBlend,
                -bounds.min.y + .008f, -bounds.center.z * horizontalSupportBlend);
            visual.position += support.TransformVector(correction);
        }
        else
            visual.position += up * (-bounds.min.y * (support != null ? support.lossyScale.y : 1f) + .008f);
    }

    private void ResolveVisual()
    {
        Animator current = GetComponentInChildren<Animator>();
        if (current == animator) return;
        RestoreVisual();
        animator = current;
        visual = animator != null ? animator.transform : null;
        skin = animator != null ? animator.GetComponentInChildren<SkinnedMeshRenderer>() : null;
        if (visual != null)
        {
            visualPosition = visual.localPosition;
            visualScale = visual.localScale;
            visualRotation = visual.localRotation;
        }
        bodyVertices.Clear();
        if (skin != null && skin.sharedMesh != null)
        {
            // Vendor meshes are not CPU-readable in players. The editor bakes the
            // tail mask into the catalog; BakeMesh supplies only the posed positions.
            var tag = animator.GetComponentInParent<CatBreedVisualTag>();
            var entry = CatBreedCatalog.Load()?.Find(tag != null ? tag.BreedId : CatBreedCatalog.DefaultBreedId);
            if (entry?.ContactVertexIndices != null)
                foreach (int index in entry.ContactVertexIndices)
                    if (index >= 0 && index < skin.sharedMesh.vertexCount) bodyVertices.Add(index);
        }
        needsState = true;
    }

    public void End()
    {
        GetComponent<CatSurfaceTurnMotion>()?.Clear();
        RestoreAnimatorSpeed();
        if (owner != null)
        {
            transform.localScale = rootScale;
            RestoreVisual();
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetFloat("Speed", 0f);
                animator.CrossFadeInFixedTime("Idle", .12f);
            }
        }
        owner = null;
        support = null;
        stateHash = 0;
    }

    private void RestoreVisual()
    {
        if (visual == null) return;
        visual.localPosition = visualPosition;
        visual.localScale = visualScale;
        visual.localRotation = visualRotation;
    }

    public static string StateFor(CatActivityPose pose)
    {
        switch (pose)
        {
            case CatActivityPose.Sit: return "Pet";
            case CatActivityPose.Sleep: return "Sleep";
            case CatActivityPose.Eat: return "Eat";
            case CatActivityPose.Drink: return "Drink";
            case CatActivityPose.Paw: return "ActivityPawSwat";
            case CatActivityPose.Scratch: return "ToyScratch";
            case CatActivityPose.Hop: return "NativeJump";
            case CatActivityPose.Crawl: return "ActivityTunnelCrawl";
            case CatActivityPose.Groom: return "ActivityScratch";
            case CatActivityPose.Stalk: return "ToyStalk";
            case CatActivityPose.Pounce: return "ToyPounce";
            case CatActivityPose.Sniff: return "ToySniff";
            case CatActivityPose.BatLeft: return "ToyBatLeft";
            case CatActivityPose.BatRight: return "ToyBatRight";
            case CatActivityPose.Push: return "ToyPush";
            case CatActivityPose.Tug: return "ToyTug";
            case CatActivityPose.Stretch: return "ToyStretch";
            case CatActivityPose.Loaf: return "CompanionLoaf";
            case CatActivityPose.Meow: return "CompanionMeow";
            case CatActivityPose.StandUp: return "CompanionStandUp";
            case CatActivityPose.SitDown: return "CompanionSitDown";
            case CatActivityPose.TowelJumpUp: return "NativeJump";
            case CatActivityPose.TowelJumpDown: return "NativeJump";
            case CatActivityPose.TowelSettle: return "TowelSettle";
            case CatActivityPose.TowelWake: return "TowelWake";
            default: return "Idle";
        }
    }

    private void OnDisable() => End();
    private void OnDestroy()
    {
        if (sampledMesh != null) Destroy(sampledMesh);
    }
}
