using SiWoC.Stackopolis.Globals;
using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    [CreateAssetMenu(fileName = "NewBuildingTheme", menuName = "Stackopolis/Building Theme")]
    public class BuildingTheme : ScriptableObject
    {
        public string themeName;

        [Header("Block Sprites")]
        public Sprite[] bottomSprites;
        public Sprite[] middleSprites;
        public Sprite[] roofSprites;

        [Header("Type Sprite indexes")]
        public int residentialBottomIndex = 0;
        public int commercialBottomIndex = 1;
        public int officeBottomIndex = 2;
        public int luxuryBottomIndex = 3;
        public int residentialMiddleIndex = 0;
        public int commercialMiddleIndex = 1;
        public int officeMiddleIndex = 2;
        public int luxuryMiddleIndex = 3;
        public int residentialRoofIndex = 0;
        public int commercialRoofIndex = 1;
        public int officeRoofIndex = 2;
        public int luxuryRoofIndex = 3;

        [Header("Optional Props")]
        public Sprite[] propSprites;

        [Header("Background (bottom → top)")]
        [Tooltip("City / ground background. Leave empty if the scene SpriteRenderer already has the art.")]
        public Sprite background;
        public Sprite gradient1;
        public Sprite tile2;
        public int tile2Count = 1;
        public Sprite gradient3;
        public Sprite tile4;
        public int tile4Count = 1;
        public Sprite gradient5;
        public Sprite moon6;
        public Sprite tile7;

        public Sprite GetRandomBottom() => GetRandom(bottomSprites);
        public Sprite GetRandomMiddle() => GetRandom(middleSprites);
        public Sprite GetRandomRoof() => GetRandom(roofSprites);

        public Sprite GetBottom(int index) => GetAt(bottomSprites, index);
        public Sprite GetMiddle(int index) => GetAt(middleSprites, index);
        public Sprite GetRoof(int index) => GetAt(roofSprites, index);

        public Sprite GetBottom(TowerType towerType) => GetBottom(BottomIndex(towerType));
        public Sprite GetMiddle(TowerType towerType) => GetMiddle(MiddleIndex(towerType));
        public Sprite GetRoof(TowerType towerType) => GetRoof(RoofIndex(towerType));

        int BottomIndex(TowerType towerType) => towerType switch
        {
            TowerType.Residential => residentialBottomIndex,
            TowerType.Commercial => commercialBottomIndex,
            TowerType.Office => officeBottomIndex,
            TowerType.Luxury => luxuryBottomIndex,
            _ => residentialBottomIndex
        };

        int MiddleIndex(TowerType towerType) => towerType switch
        {
            TowerType.Residential => residentialMiddleIndex,
            TowerType.Commercial => commercialMiddleIndex,
            TowerType.Office => officeMiddleIndex,
            TowerType.Luxury => luxuryMiddleIndex,
            _ => residentialMiddleIndex
        };

        int RoofIndex(TowerType towerType) => towerType switch
        {
            TowerType.Residential => residentialRoofIndex,
            TowerType.Commercial => commercialRoofIndex,
            TowerType.Office => officeRoofIndex,
            TowerType.Luxury => luxuryRoofIndex,
            _ => residentialRoofIndex
        };

        Sprite GetRandom(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
                return null;

            return sprites[Random.Range(0, sprites.Length)];
        }

        Sprite GetAt(Sprite[] sprites, int index)
        {
            if (sprites == null || index < 0 || index >= sprites.Length)
                return null;

            return sprites[index];
        }
    }
}
