using SiWoC.Stackopolis.Globals;
using UnityEngine;

namespace SiWoC.Stackopolis.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string buildTowerSceneName = "BuildTower";

        public void OnInfinite()
        {
            if (GameFlow.Instance == null)
            {
                Debug.LogError("GameFlow is missing. Play from the Boot scene.");
                return;
            }

            GameFlow.Instance.Forward(buildTowerSceneName);
        }

        public void OnExit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
