using TMPro;
using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    public class BuildTowerController : MonoBehaviour
    {
        [SerializeField] CraneDriver craneDriver;
        [SerializeField] BuildingSite buildingSite;
        [SerializeField] GameObject startButton;
        [SerializeField] TMP_Text heightText;

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
        }
    }
}
