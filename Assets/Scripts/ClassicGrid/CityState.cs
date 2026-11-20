using System;
using SiWoC.Stackopolis.Globals;
using UnityEngine;

namespace SiWoC.Stackopolis.ClassicGrid
{
    /// <summary>
    /// Runtime 5×5 city lots and total population.
    /// </summary>
    public class CityState
    {
        public const int Size = 5;

        readonly Lot[] lots = new Lot[Size * Size];

        public int Population { get; private set; }

        public static CityState CreateEmpty() => new CityState();

        CityState()
        {
            for (int i = 0; i < lots.Length; i++)
                lots[i] = Lot.Empty;
        }

        public bool InBounds(int x, int y) =>
            x >= 0 && x < Size && y >= 0 && y < Size;

        public Lot GetLot(int x, int y) => lots[Index(x, y)];

        public bool TryGetOccupied(int x, int y, out TowerType towerType)
        {
            if (!InBounds(x, y))
            {
                towerType = default;
                return false;
            }

            Lot lot = GetLot(x, y);
            if (!lot.Occupied)
            {
                towerType = default;
                return false;
            }

            towerType = lot.TowerType;
            return true;
        }

        public void Place(int x, int y, TowerType towerType, int floors, int population, bool hasRoof)
        {
            int index = Index(x, y);
            Lot previous = lots[index];
            if (previous.Occupied)
                Population -= previous.Population;

            lots[index] = new Lot(towerType, floors, population, hasRoof);
            Population += population;
        }

        public void Clear(int x, int y)
        {
            int index = Index(x, y);
            Lot previous = lots[index];
            if (!previous.Occupied)
                return;

            Population -= previous.Population;
            lots[index] = Lot.Empty;
        }

        public string ToJson()
        {
            var data = new SaveData
            {
                lots = new LotSave[lots.Length]
            };

            for (int i = 0; i < lots.Length; i++)
            {
                Lot lot = lots[i];
                data.lots[i] = new LotSave
                {
                    occupied = lot.Occupied,
                    towerType = (int)lot.TowerType,
                    floors = lot.Floors,
                    population = lot.Population,
                    hasRoof = lot.HasRoof
                };
            }

            return JsonUtility.ToJson(data);
        }

        public static CityState FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return CreateEmpty();

            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data?.lots == null || data.lots.Length != Size * Size)
                return CreateEmpty();

            CityState city = new CityState();
            for (int i = 0; i < data.lots.Length; i++)
            {
                LotSave saved = data.lots[i];
                if (!saved.occupied)
                    continue;

                city.lots[i] = new Lot(
                    (TowerType)saved.towerType,
                    saved.floors,
                    saved.population,
                    saved.hasRoof);
                city.Population += saved.population;
            }

            return city;
        }

        static int Index(int x, int y) => y * Size + x;

        public readonly struct Lot
        {
            public static Lot Empty => default;

            public readonly TowerType TowerType;
            public readonly int Floors;
            public readonly int Population;
            public readonly bool HasRoof;
            public readonly bool Occupied;

            public Lot(TowerType towerType, int floors, int population, bool hasRoof)
            {
                TowerType = towerType;
                Floors = floors;
                Population = population;
                HasRoof = hasRoof;
                Occupied = true;
            }
        }

        [Serializable]
        class SaveData
        {
            public LotSave[] lots;
        }

        [Serializable]
        class LotSave
        {
            public bool occupied;
            public int towerType;
            public int floors;
            public int population;
            public bool hasRoof;
        }
    }
}
