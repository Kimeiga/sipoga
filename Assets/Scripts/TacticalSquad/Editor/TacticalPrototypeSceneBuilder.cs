using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sipoga.Tactics.Editor
{
    public static class TacticalPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TacticalSquadPrototype.unity";

        [MenuItem("Sipoga/Tactical Prototype/Open Glasshouse")]
        public static void OpenPrototype()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureInBuildSettings();
        }

        [MenuItem("Sipoga/Tactical Prototype/Run Glasshouse")]
        public static void RunPrototype()
        {
            OpenPrototype();
            if (SceneManager.GetActiveScene().path == ScenePath)
            {
                EditorApplication.isPlaying = true;
            }
        }

        [MenuItem("Sipoga/Tactical Prototype/Rebuild Empty Launch Scene")]
        public static void RebuildPrototypeScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject marker = new GameObject("Runtime-generated prototype. Press Play.");
            marker.transform.position = Vector3.zero;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureInBuildSettings();
            Selection.activeGameObject = marker;
            AssetDatabase.Refresh();
        }

        private static void EnsureInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == ScenePath)
                {
                    if (!scenes[i].enabled)
                    {
                        scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
                        EditorBuildSettings.scenes = scenes.ToArray();
                    }

                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
