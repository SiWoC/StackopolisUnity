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

        [Header("Optional Props")]
        public Sprite[] propSprites;

        public Sprite GetRandomBottom() => GetRandom(bottomSprites);
        public Sprite GetRandomMiddle() => GetRandom(middleSprites);
        public Sprite GetRandomRoof() => GetRandom(roofSprites);

        public Sprite GetBottom(int index) => GetAt(bottomSprites, index);
        public Sprite GetMiddle(int index) => GetAt(middleSprites, index);
        public Sprite GetRoof(int index) => GetAt(roofSprites, index);

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
