using UnityEngine;

// Editor-only QA can exercise real save flows in a separate local directory.
// This has no override or configuration in player builds.
public static class EditorQaSession
{
    public static string SaveDirectory
    {
        get
        {
#if UNITY_EDITOR
            string path=UnityEditor.SessionState.GetString("CatHome.QA.SaveDirectory",string.Empty);
            if(!string.IsNullOrEmpty(path))return path;
#endif
            return Application.persistentDataPath;
        }
    }
    public static bool IsActive
    {
        get
        {
#if UNITY_EDITOR
            return !string.IsNullOrEmpty(UnityEditor.SessionState.GetString("CatHome.QA.SaveDirectory",string.Empty));
#else
            return false;
#endif
        }
    }
}
