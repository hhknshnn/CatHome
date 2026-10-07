using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CozyGameLauncher:MonoBehaviour
{
    private string loading;
    public void Launch(CozyGameKind kind)
    {
        string scene=kind==CozyGameKind.Yarn?CozyMiniGame.YarnScene:CozyMiniGame.PondScene;
        if(!isActiveAndEnabled||loading!=null||!CatRunnerSessionContext.TryBeginLaunch(scene))return;
        loading=scene;StartCoroutine(Load(scene));
    }
    private IEnumerator Load(string scene)
    {
        try
        {
            CatActionState.CancelForTransition(FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include));
            CatRunnerSessionContext.CaptureFromHome();var op=SceneManager.LoadSceneAsync(scene,LoadSceneMode.Additive);
            while(op!=null&&!op.isDone)yield return null;
        }
        finally {CatRunnerSessionContext.CompleteLaunch(scene);loading=null;}
    }
    private void OnDestroy(){if(loading!=null)CatRunnerSessionContext.CompleteLaunch(loading);}
}
