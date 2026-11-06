using SiWoC.Stackopolis.Globals;
using UnityEngine;

namespace SiWoC.Stackopolis.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string buildTowerSceneName = "BuildTower";

        void Awake()
        {
            if (GameFlow.Instance == null || BuildSession.Instance == null)
                Debug.LogError("GameFlow/BuildSession is missing. Play from the Boot scene.");
        }

        public void OnDifficultyEasy(bool on)
        {
            if (on)
                BuildSession.Instance.Difficulty = Difficulty.Easy;
        }

        public void OnDifficultyMedium(bool on)
        {
            if (on)
                BuildSession.Instance.Difficulty = Difficulty.Medium;
        }

        public void OnDifficultyHard(bool on)
        {
            if (on)
                BuildSession.Instance.Difficulty = Difficulty.Hard;
        }

        public void OnInfinite()
        {
            BuildSession.Instance.GameMode = GameMode.Infinite;
            BuildSession.Instance.TowerTypeToBuild = TowerType.Residential;
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
