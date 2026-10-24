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
        [Tooltip("Default theme for Build Tower. Assign on Boot; always available after Awake.")]
        [SerializeField] BuildingTheme currentTheme;

        public GameMode GameMode => gameMode;
        public TowerType TowerTypeToBuild => towerTypeToBuild;
        public BuildingTheme CurrentTheme => currentTheme;

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

        public void Configure(GameMode mode, TowerType towerType)
        {
            gameMode = mode;
            towerTypeToBuild = towerType;
        }

        public void Configure(GameMode mode, TowerType towerType, BuildingTheme theme)
        {
            gameMode = mode;
            towerTypeToBuild = towerType;
            if (theme)
                currentTheme = theme;
        }
    }
}
