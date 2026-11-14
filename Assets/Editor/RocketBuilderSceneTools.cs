using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor-Helfer rund um die Builder-Szene.
/// </summary>
public static class RocketBuilderSceneTools
{
    private const string ScenePath = "Assets/Scenes/RocketBuilder.unity";
    private const string MenuRoot = "SpaceAgency/Rocket Builder/";

    [MenuItem(MenuRoot + "Builder-Szene öffnen", priority = 20)]
    public static void OpenBuilderScene()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Builder-Szene fehlt",
                $"Die Szene '{ScenePath}' wurde nicht gefunden.", "OK");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem(MenuRoot + "Szene in Build Settings eintragen", priority = 21)]
    public static void AddSceneToBuildSettings()
    {
        bool added = EnsureSceneInBuildSettings();
        EditorUtility.DisplayDialog("Build Settings",
            added ? "Die Builder-Szene wurde zu den Build Settings hinzugefügt."
                  : "Die Builder-Szene ist bereits in den Build Settings eingetragen.",
            "OK");
    }

    [MenuItem(MenuRoot + "Builder-Szene starten", priority = 22)]
    public static void PlayBuilderScene()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Builder-Szene fehlt",
                $"Die Szene '{ScenePath}' wurde nicht gefunden.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    [MenuItem(MenuRoot + "Part-Registry Builder öffnen", priority = 40)]
    public static void OpenRegistryBuilder()
    {
        PartRegistryBuilder.ShowWindow();
    }

    [InitializeOnLoadMethod]
    private static void EnsureSceneIsRegistered()
    {
        EditorApplication.delayCall += () => EnsureSceneInBuildSettings();
    }

    private static bool EnsureSceneInBuildSettings()
    {
        if (!File.Exists(ScenePath))
            return false;

        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        if (scenes.Exists(scene => scene.path == ScenePath))
            return false;

        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[RocketBuilder] Builder-Szene zu den Build Settings hinzugefügt.");
        return true;
    }
}
