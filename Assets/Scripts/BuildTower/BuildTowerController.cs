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
        [SerializeField] GameObject startButton;
        [SerializeField] GameObject gameOverImage;
        [SerializeField] GameObject againButton;
        [SerializeField] GameObject bottomPanel;
        [SerializeField] TMP_Text highScoreText;
        [SerializeField] TMP_Text heightText;
        [SerializeField] GameObject[] backupBlocks;

        string heightFormat;
        string highScoreFormat;

        void Awake()
        {
            heightFormat = heightText.text;
            highScoreFormat = highScoreText.text;
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
            againButton.SetActive(false);
            bottomPanel.SetActive(true);
            OnTowerStatusChanged(buildingSite.Status);
        }

        public void OnStart()
        {
            startButton.SetActive(false);
            gameOverImage.SetActive(false);
            againButton.SetActive(false);
            bottomPanel.SetActive(false);
            craneDriver.ResetRound();
            buildingSite.Reset();
            cameraFollow.ResetFollow();
            if (backgroundSkyStack)
                backgroundSkyStack.ResetStack();
            craneDriver.Begin();
        }

        public void OnBack()
        {
            GameFlow.Instance.Back();
        }

        void OnTowerStatusChanged(TowerStatus status)
        {
            heightText.text = string.Format(heightFormat, status.Floors, Mathf.RoundToInt(status.Meters));
            for (int i = 0; i < backupBlocks.Length; i++)
                backupBlocks[i].SetActive(i < status.LivesRemaining);

            if (status.GameOver)
            {
                Settings.TrySetHighScoreFloors(status.Floors);
                RefreshHighScoreText();
                gameOverImage.SetActive(true);
                againButton.SetActive(true);
                bottomPanel.SetActive(true);
            }
        }

        void RefreshHighScoreText()
        {
            int floors = Settings.HighScoreFloors;
            int meters = Mathf.RoundToInt(floors * buildingSite.MetersPerFloor);
            highScoreText.text = string.Format(highScoreFormat, floors, meters);
        }
    }
}
