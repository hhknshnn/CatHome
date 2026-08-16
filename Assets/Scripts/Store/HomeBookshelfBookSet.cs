using System.Collections;
using UnityEngine;

/// <summary>
/// Owns the dependent bookshelf book set. The set stays hidden until it is dropped on the
/// purchased tall bookshelf, then each colored book is arranged in sequence. Once installed,
/// the set is parented to the bookshelf so moving the shelf also moves every book.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomeBookshelfBookSet : MonoBehaviour
{
    [SerializeField] private string productId = HomeStoreService.BookSetId;
    [SerializeField] private string bookshelfProductId = HomeStoreService.BookshelfId;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Transform[] books = new Transform[0];
    [SerializeField] private Vector3[] targetLocalPositions = new Vector3[0];
    [SerializeField] private Vector3[] targetLocalEulerAngles = new Vector3[0];
    [SerializeField] private Vector3[] targetLocalScales = new Vector3[0];
    [SerializeField, Min(0.05f)] private float bookMoveDuration = 0.22f;
    [SerializeField, Min(0f)] private float bookStagger = 0.045f;

    private Transform authoredParent;
    private Vector3 authoredLocalPosition;
    private Quaternion authoredLocalRotation;
    private Vector3 authoredLocalScale;
    private Transform bookshelfRoot;
    private Coroutine animationRoutine;
    private bool cachedAuthoringTransform;
    private bool previewing;
    private float previewBookYaw;

    public int BookCount => books != null ? books.Length : 0;
    public float CurrentLocalYaw => previewBookYaw;
    public int ShelfRowCount
    {
        get
        {
            int count = 0;
            var rows = new float[3];
            for (int i = 0; i < GetSafeBookCount(); i++)
            {
                float y = targetLocalPositions[i].y;
                bool found = false;
                for (int row = 0; row < count; row++)
                    found |= Mathf.Abs(rows[row] - y) < 0.03f;
                if (!found && count < rows.Length)
                    rows[count++] = y;
            }
            return count;
        }
    }
    public bool IsInstalled =>
        HomeStoreService.IsOwned(productId) &&
        HomeStoreService.TryGetWorldPlacement(productId, out _, out _);

    private void Awake()
    {
        CacheAuthoringTransform();
        RefreshState(false);
    }

    private void OnEnable()
    {
        CacheAuthoringTransform();
        HomeStoreService.OwnershipChanged += HandleOwnershipChanged;
        HomeStoreService.PlacementChanged += HandlePlacementChanged;
        RefreshState(false);
    }

    private void OnDisable()
    {
        HomeStoreService.OwnershipChanged -= HandleOwnershipChanged;
        HomeStoreService.PlacementChanged -= HandlePlacementChanged;
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }

    public void BeginPlacementPreview(Transform targetBookshelf)
    {
        CacheAuthoringTransform();
        previewing = true;
        bookshelfRoot = targetBookshelf;
        previewBookYaw = HomeStoreService.TryGetWorldPlacement(
            productId, out _, out float savedYaw)
            ? savedYaw
            : 0f;
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }

    public void SetPlacementPreview(Transform targetBookshelf, bool snapped)
    {
        if (!previewing)
            return;

        bookshelfRoot = targetBookshelf;
        if (!snapped || bookshelfRoot == null)
        {
            if (visualRoot != null)
                visualRoot.SetActive(false);
            return;
        }

        AttachToBookshelf(bookshelfRoot);
        ArrangeBooksImmediate();
        if (visualRoot != null)
            visualRoot.SetActive(true);
    }

    public void RotatePreview(float degrees)
    {
        if (!previewing)
            return;
        previewBookYaw = Mathf.Repeat(previewBookYaw + degrees, 360f);
        ArrangeBooksImmediate();
    }

    public void CommitPlacement(Transform targetBookshelf)
    {
        previewing = false;
        bookshelfRoot = targetBookshelf;
        if (bookshelfRoot == null)
        {
            RefreshState(false);
            return;
        }

        AttachToBookshelf(bookshelfRoot);
        if (visualRoot != null)
            visualRoot.SetActive(true);

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = Application.isPlaying
            ? StartCoroutine(ArrangeBooksRoutine())
            : null;
        if (!Application.isPlaying)
            ArrangeBooksImmediate();
    }

    public void CancelPlacementPreview()
    {
        previewing = false;
        RefreshState(false);
    }

    public void SnapToBookshelfImmediate(Transform targetBookshelf)
    {
        SnapToBookshelfImmediate(targetBookshelf, 0f);
    }

    public void SnapToBookshelfImmediate(Transform targetBookshelf, float localYaw)
    {
        bookshelfRoot = targetBookshelf;
        previewBookYaw = Mathf.Repeat(localYaw, 360f);
        if (bookshelfRoot == null)
        {
            HideUninstalledSet();
            return;
        }

        AttachToBookshelf(bookshelfRoot);
        ArrangeBooksImmediate();
        if (visualRoot != null)
            visualRoot.SetActive(true);
    }

    private IEnumerator ArrangeBooksRoutine()
    {
        int count = GetSafeBookCount();
        var starts = new Vector3[count];
        var startRotations = new Quaternion[count];
        var startsScales = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Transform book = books[i];
            Vector3 target = targetLocalPositions[i];
            starts[i] = target + new Vector3(
                Mathf.Lerp(-0.32f, 0.32f, count <= 1 ? 0.5f : i / (float)(count - 1)),
                0.42f + i * 0.012f,
                0.42f + i * 0.008f);
            startRotations[i] = Quaternion.Euler(0f, -28f + i * 6f, -18f + i * 4f);
            startsScales[i] = targetLocalScales[i] * 0.72f;
            book.localPosition = starts[i];
            book.localRotation = startRotations[i];
            book.localScale = startsScales[i];
        }

        for (int i = 0; i < count; i++)
        {
            Transform book = books[i];
            Quaternion targetRotation = GetTargetRotation(i);
            float elapsed = 0f;
            while (elapsed < bookMoveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / bookMoveDuration);
                t = t * t * (3f - 2f * t);
                book.localPosition = Vector3.LerpUnclamped(starts[i], targetLocalPositions[i], t);
                book.localRotation = Quaternion.SlerpUnclamped(startRotations[i], targetRotation, t);
                book.localScale = Vector3.LerpUnclamped(startsScales[i], targetLocalScales[i], t);
                yield return null;
            }

            SetBookAtTarget(i);
            if (bookStagger > 0f)
                yield return new WaitForSecondsRealtime(bookStagger);
        }

        animationRoutine = null;
    }

    private void ArrangeBooksImmediate()
    {
        int count = GetSafeBookCount();
        for (int i = 0; i < count; i++)
            SetBookAtTarget(i);
    }

    private void SetBookAtTarget(int index)
    {
        Transform book = books[index];
        if (book == null)
            return;
        book.localPosition = targetLocalPositions[index];
        book.localRotation = GetTargetRotation(index);
        book.localScale = targetLocalScales[index];
        book.gameObject.SetActive(true);
    }

    private int GetSafeBookCount()
    {
        return Mathf.Min(
            books != null ? books.Length : 0,
            targetLocalPositions != null ? targetLocalPositions.Length : 0,
            targetLocalEulerAngles != null ? targetLocalEulerAngles.Length : 0,
            targetLocalScales != null ? targetLocalScales.Length : 0);
    }

    private Quaternion GetTargetRotation(int index)
    {
        Vector3 euler = targetLocalEulerAngles[index];
        euler.y += previewBookYaw;
        return Quaternion.Euler(euler);
    }

    private void RefreshState(bool animate)
    {
        if (previewing)
            return;

        if (!HomeStoreService.IsOwned(productId) ||
            !HomeStoreService.TryGetWorldPlacement(productId, out _, out float localYaw) ||
            !TryFindBookshelf(out Transform target))
        {
            HideUninstalledSet();
            return;
        }

        if (animate)
        {
            previewBookYaw = Mathf.Repeat(localYaw, 360f);
            CommitPlacement(target);
        }
        else
            SnapToBookshelfImmediate(target, localYaw);
    }

    private bool TryFindBookshelf(out Transform target)
    {
        HomeProductPlacement[] placements =
            FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include);
        for (int i = 0; i < placements.Length; i++)
        {
            if (placements[i] != null && placements[i].ProductId == bookshelfProductId)
            {
                target = placements[i].MovableRoot;
                return target != null;
            }
        }

        target = null;
        return false;
    }

    private void AttachToBookshelf(Transform target)
    {
        transform.SetParent(target, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void HideUninstalledSet()
    {
        RestoreAuthoringTransform();
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }

    private void CacheAuthoringTransform()
    {
        if (cachedAuthoringTransform)
            return;
        authoredParent = transform.parent;
        authoredLocalPosition = transform.localPosition;
        authoredLocalRotation = transform.localRotation;
        authoredLocalScale = transform.localScale;
        cachedAuthoringTransform = true;
    }

    private void RestoreAuthoringTransform()
    {
        if (!cachedAuthoringTransform)
            return;
        transform.SetParent(authoredParent, false);
        transform.localPosition = authoredLocalPosition;
        transform.localRotation = authoredLocalRotation;
        transform.localScale = authoredLocalScale;
    }

    private void HandleOwnershipChanged(string changedProductId)
    {
        if (string.IsNullOrEmpty(changedProductId) ||
            changedProductId == productId || changedProductId == bookshelfProductId)
        {
            RefreshState(false);
        }
    }

    private void HandlePlacementChanged(string changedProductId)
    {
        if (string.IsNullOrEmpty(changedProductId) ||
            changedProductId == productId || changedProductId == bookshelfProductId)
        {
            RefreshState(false);
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        string id,
        string requiredBookshelfId,
        GameObject content,
        Transform[] bookTransforms,
        Vector3[] positions,
        Vector3[] eulerAngles,
        Vector3[] scales)
    {
        productId = id;
        bookshelfProductId = requiredBookshelfId;
        visualRoot = content;
        books = bookTransforms ?? new Transform[0];
        targetLocalPositions = positions ?? new Vector3[0];
        targetLocalEulerAngles = eulerAngles ?? new Vector3[0];
        targetLocalScales = scales ?? new Vector3[0];
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }
#endif
}
