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

        /// <summary>
        /// When true, the chosen <see cref="Difficulty"/> applies (Infinite).
        /// When false, the run uses Hard (Classic for now).
        /// </summary>
        public bool DifficultyActive { get; private set; } = true;

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
            set
            {
                difficulty = value;
                Settings.SetDifficulty(value);
            }
        }

        /// <summary>
        /// Difficulty used by the run: the chosen value when <see cref="DifficultyActive"/>, otherwise Hard.
        /// </summary>
        public Difficulty EffectiveDifficulty =>
            DifficultyActive ? difficulty : Difficulty.Hard;

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
            difficulty = Settings.GetDifficulty();
        }

        public void PrepareInfinite()
        {
            gameMode = GameMode.Infinite;
            DifficultyActive = true;
            difficulty = Settings.GetDifficulty();
        }

        public void PrepareClassic()
        {
            gameMode = GameMode.Classic;
            DifficultyActive = false;
            difficulty = Difficulty.Hard;
        }
    }
}
