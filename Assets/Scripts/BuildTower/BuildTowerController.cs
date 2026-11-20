using SiWoC.Stackopolis.Globals;
using TMPro;
using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    public class BuildTowerController : MonoBehaviour
    {
        [SerializeField] CraneDriver craneDriver;
        [SerializeField] BuildingSite buildingSite;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] BackgroundSkyStack backgroundSkyStack;
        [SerializeField] AmbientFlyerSpawner ambientFlyerSpawner;
        [SerializeField] GameObject startButton;
        [SerializeField] GameObject gameOverImage;
        [SerializeField] GameObject finishActionButton;
        [SerializeField] TMP_Text finishActionButtonText;
        [SerializeField] GameObject bottomPanel;
        [SerializeField] GameObject highScorePanel;
        [SerializeField] TMP_Text highScoreText;
        [SerializeField] TMP_Text heightText;
        [SerializeField] TMP_Text difficultyText;
        [SerializeField] GameObject[] backupBlocks;

        string heightFormat;
        string highScoreFormat;

        void Awake()
        {
            if (GameFlow.Instance == null || BuildSession.Instance == null)
                Debug.LogError("GameFlow/BuildSession is missing. Play from the Boot scene.");

            heightFormat = heightText.text;
            highScoreFormat = highScoreText.text;
            difficultyText.text = BuildSession.Instance.EffectiveDifficulty.ToString();
            RefreshHighScoreText();
        }

        void OnEnable()
        {
            buildingSite.StatusChanged += OnTowerStatusChanged;
        }

        void OnDisable()
        {
            if (buildingSite)
                buildingSite.StatusChanged -= OnTowerStatusChanged;
        }

        void Start()
        {
            startButton.SetActive(true);
            gameOverImage.SetActive(false);
            finishActionButton.SetActive(false);
            bottomPanel.SetActive(true);
            if (BuildSession.Instance.GameMode == GameMode.Infinite)
            {
                finishActionButtonText.text = "Again";
                highScorePanel.SetActive(true);
            }
            else
            {
                finishActionButtonText.text = "Place Tower";
                highScorePanel.SetActive(false);
            }
            OnTowerStatusChanged(buildingSite.Status);
        }

        public void OnStart()
        {
            startButton.SetActive(false);
            gameOverImage.SetActive(false);
            finishActionButton.SetActive(false);
            bottomPanel.SetActive(false);
            craneDriver.ResetRound();
            buildingSite.Reset();
            cameraFollow.ResetFollow();
            backgroundSkyStack.ResetStack();
            ambientFlyerSpawner.ResetSpawn();
            craneDriver.Begin();
        }

        public void OnBack()
        {
            GameFlow.Instance.Back();
        }

        void OnTowerStatusChanged(TowerStatus status)
        {
            heightText.text = string.Format(heightFormat, status.Floors, Mathf.RoundToInt(status.Meters), status.Population);
            for (int i = 0; i < backupBlocks.Length; i++)
                backupBlocks[i].SetActive(i < status.LivesRemaining);

            if (status.Finished)
            {
                TryStoreClassicPending(status);
                gameOverImage.SetActive(false);
                finishActionButton.SetActive(true);
                bottomPanel.SetActive(true);
                return;
            }

            if (status.GameOver)
            {
                if (BuildSession.Instance.GameMode == GameMode.Infinite)
                {
                    Settings.TrySetHighScoreFloors(status.Floors, status.Population, BuildSession.Instance.EffectiveDifficulty);
                    RefreshHighScoreText();
                }
                else
                {
                    TryStoreClassicPending(status);
                }

                gameOverImage.SetActive(true);
                finishActionButton.SetActive(true);
                bottomPanel.SetActive(true);
            }
        }

        void TryStoreClassicPending(TowerStatus status)
        {
            if (BuildSession.Instance.GameMode != GameMode.Classic || status.Floors <= 0)
                return;

            bool hasRoof = buildingSite.Top && buildingSite.Top.type == BlockType.Roof;
            BuildSession.Instance.SetPendingTower(
                BuildSession.Instance.TowerTypeToBuild,
                status.Floors,
                status.Population,
                hasRoof);
        }

        public void OnFinishAction()
        {
            if (BuildSession.Instance.GameMode == GameMode.Classic)
            {
                GameFlow.Instance.Back();
            }
            else
            {
                OnStart();
            }
        }

        void RefreshHighScoreText()
        {
            Difficulty difficulty = BuildSession.Instance.EffectiveDifficulty;
            int floors = Settings.GetHighScoreFloors(difficulty);
            int meters = Mathf.RoundToInt(floors * buildingSite.MetersPerFloor);
            int population = Settings.GetHighScorePopulation(difficulty);
            highScoreText.text = string.Format(highScoreFormat, floors, meters, population);
        }
    }
}
