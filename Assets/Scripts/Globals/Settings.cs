using UnityEngine;

namespace SiWoC.Stackopolis.Globals
{
    public static class Settings
    {
        const string PrefHighScoreFloors = "HighScoreFloors";
        const string PrefHighScorePopulation = "HighScorePopulation";
        const string PrefDifficulty = "Difficulty";
        const string PrefClassicCity = "ClassicCity";
        const string PrefClassicPending = "ClassicPending";
        const string PrefClassicPendingHas = "ClassicPendingHas";

        public static Difficulty GetDifficulty()
        {
            return (Difficulty)PlayerPrefs.GetInt(PrefDifficulty, (int)Difficulty.Hard);
        }

        public static void SetDifficulty(Difficulty difficulty)
        {
            PlayerPrefs.SetInt(PrefDifficulty, (int)difficulty);
            PlayerPrefs.Save();
        }

        public static int GetHighScoreFloors(Difficulty difficulty)
        {
            return PlayerPrefs.GetInt(PrefHighScoreFloors + difficulty, 0);
        }

        public static int GetHighScorePopulation(Difficulty difficulty)
        {
            return PlayerPrefs.GetInt(PrefHighScorePopulation + difficulty, 0);
        }

        /// <summary>
        /// Writes the high score for <paramref name="difficulty"/> only when
        /// <paramref name="floors"/> is higher than the stored value.
        /// Stores <paramref name="population"/> beside that floors record.
        /// </summary>
        /// <returns>True if a new high score was stored.</returns>
        public static bool TrySetHighScoreFloors(int floors, int population, Difficulty difficulty)
        {
            if (floors <= GetHighScoreFloors(difficulty))
                return false;

            PlayerPrefs.SetInt(PrefHighScoreFloors + difficulty, floors);
            PlayerPrefs.SetInt(PrefHighScorePopulation + difficulty, population);
            PlayerPrefs.Save();
            return true;
        }

        public static void SaveClassicCity(string json)
        {
            PlayerPrefs.SetString(PrefClassicCity, json);
            PlayerPrefs.Save();
        }

        public static string LoadClassicCityJson()
        {
            return PlayerPrefs.GetString(PrefClassicCity, string.Empty);
        }

        public static void SaveClassicPending(TowerType towerType, int floors, int population, bool hasRoof)
        {
            PlayerPrefs.SetInt(PrefClassicPendingHas, 1);
            PlayerPrefs.SetInt(PrefClassicPending + "Type", (int)towerType);
            PlayerPrefs.SetInt(PrefClassicPending + "Floors", floors);
            PlayerPrefs.SetInt(PrefClassicPending + "Population", population);
            PlayerPrefs.SetInt(PrefClassicPending + "HasRoof", hasRoof ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ClearClassicPending()
        {
            PlayerPrefs.SetInt(PrefClassicPendingHas, 0);
            PlayerPrefs.Save();
        }

        public static bool TryLoadClassicPending(out PendingTower pending)
        {
            if (PlayerPrefs.GetInt(PrefClassicPendingHas, 0) == 0)
            {
                pending = default;
                return false;
            }

            pending = new PendingTower(
                (TowerType)PlayerPrefs.GetInt(PrefClassicPending + "Type", 0),
                PlayerPrefs.GetInt(PrefClassicPending + "Floors", 0),
                PlayerPrefs.GetInt(PrefClassicPending + "Population", 0),
                PlayerPrefs.GetInt(PrefClassicPending + "HasRoof", 0) != 0);
            return true;
        }
    }
}
