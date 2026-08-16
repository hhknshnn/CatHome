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
        if (!loading)
            StartCoroutine(LaunchRoutine());
    }

    private IEnumerator LaunchRoutine()
    {
        loading = true;
        CatRunnerSessionContext.CaptureFromHome();

        Scene loaded = SceneManager.GetSceneByName(CatchSceneName);
        if (!loaded.IsValid() || !loaded.isLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(
                CatchScenePath,
                LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError("Cat Catch scene could not be queued for loading.", this);
                loading = false;
                yield break;
            }

            while (!operation.isDone)
                yield return null;
        }

        loading = false;
    }
}
