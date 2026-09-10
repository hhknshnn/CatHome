using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class CatCatchLauncher : MonoBehaviour
{
    public const string CatchScenePath = "Assets/Scenes/Catch/CatCatch.unity";
    public const string CatchSceneName = "CatCatch";

    private bool loading;

    public void Launch()
    {
        if (!isActiveAndEnabled || loading ||
            !CatRunnerSessionContext.TryBeginLaunch(CatchSceneName))
            return;
        loading = true;
        try
        {
            if (StartCoroutine(LaunchRoutine()) == null)
            {
                loading = false;
                CatRunnerSessionContext.CompleteLaunch(CatchSceneName);
            }
        }
        catch
        {
            loading = false;
            CatRunnerSessionContext.CompleteLaunch(CatchSceneName);
            throw;
        }
    }

    private IEnumerator LaunchRoutine()
    {
        try
        {
            CatActionState.CancelForTransition(
                FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include));
            CatRunnerSessionContext.CaptureFromHome();
            AsyncOperation operation = SceneManager.LoadSceneAsync(
                CatchScenePath, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError("Cat Catch scene could not be queued for loading.", this);
                yield break;
            }
            while (!operation.isDone)
                yield return null;
        }
        finally
        {
            loading = false;
            CatRunnerSessionContext.CompleteLaunch(CatchSceneName);
        }
    }

    private void OnDestroy()
    {
        if (loading)
            CatRunnerSessionContext.CompleteLaunch(CatchSceneName);
    }
}
