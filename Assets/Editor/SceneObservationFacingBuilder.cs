using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Bind non-store watching activities to real, current scene surfaces.</summary>
public static class SceneObservationFacingBuilder
{
    public static int Configure(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return 0;
        var nodes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        int configured = 0;
        foreach (var activity in nodes.Select(node => node.GetComponent<SitLookActivity>()).Where(activity =>
                     activity != null && string.IsNullOrEmpty(activity.StoreProductId)))
        {
            Transform surface = null;
            if (activity.Kind == CatActivityKind.WindowWatch)
            {
                var window = nodes.FirstOrDefault(node => node.name == "WindowSystem");
                if (window != null) surface = window.Find("GlassPanel") ?? window.Find("Window");
            }
            else if (activity.Kind == CatActivityKind.BirdWatch)
                surface = nodes.FirstOrDefault(node => node.name == "Courtyard Tree");
            else continue;
            if (surface == null)
                throw new InvalidOperationException(scene.name + "/" + activity.name + ": missing real observation surface.");
            LivingRoomGazeLayoutBuilder.ConfigureWindowEntry(activity);
            SitLookFacingBuilder.ConfigureSceneSurface(activity, surface);
            configured++;
        }
        return configured;
    }
}
