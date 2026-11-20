using System;
using SiWoC.Stackopolis.Globals;
using UnityEngine;

namespace SiWoC.Stackopolis.ClassicGrid
{
    /// <summary>
    /// Classic unlock thresholds and adjacency placement rules.
    /// </summary>
    [Serializable]
    public class ClassicRules
    {
        [SerializeField] int residentialUnlockPopulation = 0;
        [SerializeField] int commercialUnlockPopulation = 1000;
        [SerializeField] int officeUnlockPopulation = 2000;
        [SerializeField] int luxuryUnlockPopulation = 3000;

        public bool IsUnlocked(TowerType towerType, int cityPopulation)
        {
            return cityPopulation >= UnlockPopulation(towerType);
        }

        public int UnlockPopulation(TowerType towerType) => towerType switch
        {
            TowerType.Residential => residentialUnlockPopulation,
            TowerType.Commercial => commercialUnlockPopulation,
            TowerType.Office => officeUnlockPopulation,
            TowerType.Luxury => luxuryUnlockPopulation,
            _ => residentialUnlockPopulation
        };

        /// <summary>
        /// True when the type is unlocked for this city and the lot may receive it.
        /// Empty lots only. Orthogonal neighbors supply adjacency.
        /// </summary>
        public bool CanPlace(TowerType towerType, int x, int y, CityState city)
        {
            Debug.Log($"CanPlace: {towerType}, {x}, {y}, {city.Population}");
            Debug.Log($"InBounds: {city.InBounds(x, y)}");
            if (!city.InBounds(x, y))
                return false;
            Debug.Log($"IsUnlocked: {IsUnlocked(towerType, city.Population)}");
            if (!IsUnlocked(towerType, city.Population))
                return false;
            Debug.Log($"Occupied: {city.GetLot(x, y).Occupied}");
            //if (city.GetLot(x, y).Occupied)
            //    return false;
            Debug.Log($"HasRequiredNeighbors: {HasRequiredNeighbors(towerType, x, y, city)}");
            return HasRequiredNeighbors(towerType, x, y, city);
        }

        bool HasRequiredNeighbors(TowerType towerType, int x, int y, CityState city)
        {
            switch (towerType)
            {
                case TowerType.Residential:
                    return true;
                case TowerType.Commercial:
                    return HasNeighborOfType(x, y, city, TowerType.Residential);
                case TowerType.Office:
                    return HasNeighborOfType(x, y, city, TowerType.Residential)
                        && HasNeighborOfType(x, y, city, TowerType.Commercial);
                case TowerType.Luxury:
                    return HasNeighborOfType(x, y, city, TowerType.Residential)
                        && HasNeighborOfType(x, y, city, TowerType.Commercial)
                        && HasNeighborOfType(x, y, city, TowerType.Office);
                default:
                    return false;
            }
        }

        static bool HasNeighborOfType(int x, int y, CityState city, TowerType required)
        {
            if (IsType(x - 1, y, city, required))
                return true;
            if (IsType(x + 1, y, city, required))
                return true;
            if (IsType(x, y - 1, city, required))
                return true;
            if (IsType(x, y + 1, city, required))
                return true;
            return false;
        }

        static bool IsType(int x, int y, CityState city, TowerType required) =>
            city.TryGetOccupied(x, y, out TowerType type) && type == required;
    }
}
