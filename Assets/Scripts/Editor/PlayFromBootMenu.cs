using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SiWoC.Stackopolis.Editor
{
    public static class PlayFromBootMenu
    {
        const string BootScenePath = "Assets/Scenes/Boot.unity";

        [MenuItem("Stackopolis/Play From Boot %#b")]
        static void PlayFromBoot()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            SceneAsset boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (!boot)
            {
                Debug.LogError($"Boot scene not found at {BootScenePath}");
                return;
            }

            EditorSceneManager.playModeStartScene = boot;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Stackopolis/Clear Play Start Scene")]
        static void ClearPlayStartScene()
        {
            EditorSceneManager.playModeStartScene = null;
            Debug.Log("Play Mode start scene cleared. Play uses the open scene(s).");
        }

        [MenuItem("Stackopolis/Clear Play Start Scene", true)]
        static bool ClearPlayStartSceneValidate()
        {
            return EditorSceneManager.playModeStartScene != null;
        }
    }
}
