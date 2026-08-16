using UnityEngine;

using QuestState = CatHome.Quests.QuestState;

/// <summary>
/// Minimal progression view for verifying the quest system, plus the only claim
/// trigger that exists until the real Quest Panel lands.
/// Editor / development builds only: it spawns its own runtime GameObject,
/// never touches the scene file, and compiles to an empty component in
/// release builds. It will be replaced by the real progression UI later.
/// </summary>
public sealed class ProgressionDebugOverlay : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const float Margin = 8f;
    private const float Width = 300f;

    [SerializeField]
    [Tooltip("Editor-only debug toggle. Hidden by default; tick this on the " +
             "runtime \"ProgressionDebugOverlay (dev only)\" object in the " +
             "Inspector to show the panel in the Game View.")]
    private bool showOverlay = false;

    private GUIStyle boxStyle;
    private GUIStyle textStyle;

    // The claim is deferred to the end of OnGUI so no progression state can
    // change while the quest list is still being drawn.
    private string pendingClaimQuestId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SpawnOverlay()
    {
        if (FindAnyObjectByType<ProgressionDebugOverlay>(FindObjectsInactive.Include) != null)
            return;

        var host = new GameObject("ProgressionDebugOverlay (dev only)");
        host.AddComponent<ProgressionDebugOverlay>();
        DontDestroyOnLoad(host);
    }

    private void OnGUI()
    {
        // Hidden by default from the normal Game View; enable via the Inspector
        // toggle above. No progression/quest/level/XP state is affected.
        if (!showOverlay)
            return;

        EnsureStyles();

        float scale = Screen.dpi > 0f ? Mathf.Max(1f, Screen.dpi / 160f) : 1f;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        float scaledScreenWidth = Screen.width / scale;
        GUILayout.BeginArea(
            new Rect(scaledScreenWidth - Width - Margin, Margin, Width, 600f)
        );
        GUILayout.BeginVertical(boxStyle);

        GUILayout.Label("PROGRESSION (dev)", textStyle);

        if (!ProgressionService.IsInitialized)
        {
            GUILayout.Label("Service not initialized.", textStyle);
        }
        else if (ProgressionService.IsProgressionComplete)
        {
            GUILayout.Label("All quest chapters completed!", textStyle);
        }
        else
        {
            LevelDefinition level = ProgressionService.GetActiveChapterDefinition();
            GUILayout.Label(
                level != null && !string.IsNullOrEmpty(level.LevelName)
                    ? $"Chapter {ProgressionService.CurrentChapterNumber}: {level.LevelName}"
                    : $"Chapter {ProgressionService.CurrentChapterNumber}",
                textStyle
            );

            if (level != null)
            {
                for (int i = 0; i < level.Quests.Count; i++)
                {
                    QuestDefinition quest = level.Quests[i];
                    if (quest == null || string.IsNullOrEmpty(quest.QuestId))
                        continue;

                    // The lifecycle state, not the legacy completed mirror,
                    // decides what is drawn: it separates Completed (reward still
                    // waiting) from Claimed (reward paid).
                    ProgressionService.TryGetQuestProgress(
                        quest.QuestId,
                        out int count,
                        out _
                    );
                    ProgressionService.TryGetQuestState(
                        quest.QuestId,
                        out QuestState state
                    );

                    string mark = state == QuestState.Claimed
                        ? "[x]"
                        : state == QuestState.Completed ? "[!]" : "[ ]";

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(
                        $"{mark} {quest.Description} ({count}/{quest.RequiredCount})",
                        textStyle
                    );

                    // The only claim trigger in the project right now. It calls
                    // the same public API the future Quest Panel will use, so it
                    // verifies the real path rather than a debug shortcut.
                    if (state == QuestState.Completed &&
                        GUILayout.Button("Claim", GUILayout.Width(56f)))
                    {
                        pendingClaimQuestId = quest.QuestId;
                    }

                    GUILayout.EndHorizontal();
                }
            }
        }

        GUILayout.Space(4f);
        GUILayout.Label(
            $"Coins {ProgressionService.Coins}   " +
            $"Diamonds {ProgressionService.Diamonds}   " +
            $"Bond XP {ProgressionService.BondXp}",
            textStyle
        );

        GUILayout.EndVertical();
        GUILayout.EndArea();
        GUI.matrix = previousMatrix;

        ProcessPendingClaim();
    }

    /// <summary>
    /// Runs the requested claim after the layout is finished. TryClaimQuest is
    /// the single reward path: it pays only a Completed quest, exactly once, and
    /// evaluates level advancement itself.
    /// </summary>
    private void ProcessPendingClaim()
    {
        if (string.IsNullOrEmpty(pendingClaimQuestId))
            return;

        string questId = pendingClaimQuestId;
        pendingClaimQuestId = null;

        if (!ProgressionService.TryClaimQuest(questId))
        {
            Debug.Log(
                $"ProgressionDebugOverlay: quest '{questId}' was not claimable. Nothing changed."
            );
        }
    }

    private void EnsureStyles()
    {
        if (boxStyle != null)
            return;

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(8, 8, 6, 6)
        };
        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            richText = false,
            wordWrap = true
        };
        textStyle.normal.textColor = Color.white;
    }
#endif
}
