using SiWoC.Stackopolis.BuildTower;
using UnityEngine;

namespace SiWoC.Stackopolis.Globals
{
    /// <summary>
    /// Run settings for a Build Tower session. Lives next to GameFlow (Boot / DontDestroyOnLoad).
    /// </summary>
    public class BuildSession : MonoBehaviour
    {
        public static BuildSession Instance { get; private set; }

        [SerializeField] GameMode gameMode = GameMode.Infinite;
        [SerializeField] TowerType towerTypeToBuild = TowerType.Residential;
        [SerializeField] Difficulty difficulty = Difficulty.Hard;
        [Tooltip("Default theme for Build Tower. Assign on Boot; always available after Awake.")]
        [SerializeField] BuildingTheme currentTheme;

        public GameMode GameMode
        {
            get => gameMode;
            set => gameMode = value;
        }

        public TowerType TowerTypeToBuild
        {
            get => towerTypeToBuild;
            set => towerTypeToBuild = value;
        }

        public Difficulty Difficulty
        {
            get => difficulty;
            set => difficulty = value;
        }

        public BuildingTheme CurrentTheme
        {
            get => currentTheme;
            set
            {
                if (value)
                    currentTheme = value;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
