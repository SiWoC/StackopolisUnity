using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    public enum BlockType
    {
        Bottom,
        Middle,
        Roof
    }

    public class Block : MonoBehaviour
    {
        public BlockType type;
        public bool perfect;
    }
}
