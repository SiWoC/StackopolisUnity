using System;
using SiWoC.Stackopolis.Globals;
using UnityEngine;

namespace SiWoC.Stackopolis.ClassicGrid
{
    /// <summary>
    /// Per-<see cref="TowerType"/> art and copy for the Classic grid UI.
    /// </summary>
    [Serializable]
    public class TowerTypeVisuals
    {
        [Serializable]
        public class Entry
        {
            public Sprite gridSquare;
            public Sprite miniature;
            [TextArea(2, 6)] public string description;
        }

        [SerializeField] Entry residential;
        [SerializeField] Entry commercial;
        [SerializeField] Entry office;
        [SerializeField] Entry luxury;

        public Entry Get(TowerType towerType) => towerType switch
        {
            TowerType.Residential => residential,
            TowerType.Commercial => commercial,
            TowerType.Office => office,
            TowerType.Luxury => luxury,
            _ => residential
        };

        public Sprite GridSquare(TowerType towerType) => Get(towerType).gridSquare;

        public Sprite Miniature(TowerType towerType) => Get(towerType).miniature;

        public string Description(TowerType towerType) => Get(towerType).description;
    }
}
