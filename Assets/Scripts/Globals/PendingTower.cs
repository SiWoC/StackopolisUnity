namespace SiWoC.Stackopolis.Globals
{
    /// <summary>
    /// Classic tower waiting to be placed or scrapped on the city grid.
    /// </summary>
    public readonly struct PendingTower
    {
        public readonly TowerType TowerType;
        public readonly int Floors;
        public readonly int Population;
        public readonly bool HasRoof;

        public PendingTower(TowerType towerType, int floors, int population, bool hasRoof)
        {
            TowerType = towerType;
            Floors = floors;
            Population = population;
            HasRoof = hasRoof;
        }

        /// <summary>
        /// Population relative to a perfect Classic tower of this type (0..1).
        /// Max = (1 bottom + middle count) × perfect population 10; roof does not score.
        /// </summary>
        public float Efficiency
        {
            get
            {
                int max = MaxPopulation(TowerType);
                return max > 0 ? (float)Population / max : 0f;
            }
        }

        public static int MaxPopulation(TowerType towerType) => towerType switch
        {
            TowerType.Residential => 110,
            TowerType.Commercial => 210,
            TowerType.Office => 310,
            TowerType.Luxury => 410,
            _ => 110
        };
    }
}
