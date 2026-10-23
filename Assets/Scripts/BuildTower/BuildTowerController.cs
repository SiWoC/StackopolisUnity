using TMPro;
using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    public class BuildTowerController : MonoBehaviour
    {
        [SerializeField] CraneDriver craneDriver;
        [SerializeField] BuildingSite buildingSite;
        [SerializeField] GameObject startButton;
        [SerializeField] GameObject gameOverImage;
        [SerializeField] TMP_Text heightText;
        [SerializeField] GameObject[] backupBlocks;

        string heightFormat;

        void Awake()
        {
            heightFormat = heightText.text;
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
            OnTowerStatusChanged(buildingSite.Status);
        }

        public void OnStart()
        {
            startButton.SetActive(false);
            craneDriver.Begin();
        }

        void OnTowerStatusChanged(TowerStatus status)
        {
            heightText.text = string.Format(heightFormat, status.Floors, Mathf.RoundToInt(status.Meters));
            for (int i = 0; i < backupBlocks.Length; i++)
                backupBlocks[i].SetActive(i < status.LivesRemaining);

            if (status.GameOver)
            {
                gameOverImage.SetActive(true);
            }
        }
    }
}
