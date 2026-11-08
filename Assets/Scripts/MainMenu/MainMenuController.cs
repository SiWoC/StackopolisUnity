using SiWoC.Stackopolis.Globals;
using UnityEngine;
using UnityEngine.UI;

namespace SiWoC.Stackopolis.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string buildTowerSceneName = "BuildTower";
        [SerializeField] GameObject difficultyBlur;
        [SerializeField] Toggle difficultyEasy;
        [SerializeField] Toggle difficultyMedium;
        [SerializeField] Toggle difficultyHard;

        void Start()
        {
            difficultyBlur.SetActive(false);
            SyncDifficultyToggles();
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
            BuildSession.Instance.PrepareInfinite();
            BuildSession.Instance.TowerTypeToBuild = TowerType.Residential;

            SyncDifficultyToggles();
            difficultyBlur.SetActive(true);
        }

        public void OnClassic()
        {
            BuildSession.Instance.PrepareClassic();
            OnGo();
        }

        public void OnGo()
        {
            difficultyBlur.SetActive(false);
            GameFlow.Instance.Forward(buildTowerSceneName);
        }

        public void OnExit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void SyncDifficultyToggles()
        {
            Difficulty difficulty = BuildSession.Instance.Difficulty;
            difficultyEasy.SetIsOnWithoutNotify(difficulty == Difficulty.Easy);
            difficultyMedium.SetIsOnWithoutNotify(difficulty == Difficulty.Medium);
            difficultyHard.SetIsOnWithoutNotify(difficulty == Difficulty.Hard);
        }
    }
}
