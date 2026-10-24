using SiWoC.Stackopolis.Globals;
using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    public class BlockFactory : MonoBehaviour
    {
        [SerializeField] GameObject floorPrefab;

        BuildSession buildSession;

        void Awake()
        {
            buildSession = BuildSession.Instance;
        }

        public Block CreateBottom()
        {
            BuildingTheme theme = buildSession.CurrentTheme;
            Sprite sprite = UseTowerTypeSprite()
                ? theme.GetBottom(buildSession.TowerTypeToBuild)
                : theme.GetRandomBottom();
            return Create(sprite, BlockType.Bottom);
        }

        public Block CreateMiddle()
        {
            BuildingTheme theme = buildSession.CurrentTheme;
            Sprite sprite = UseTowerTypeSprite()
                ? theme.GetMiddle(buildSession.TowerTypeToBuild)
                : theme.GetRandomMiddle();
            return Create(sprite, BlockType.Middle);
        }

        public Block CreateRoof()
        {
            BuildingTheme theme = buildSession.CurrentTheme;
            Sprite sprite = UseTowerTypeSprite()
                ? theme.GetRoof(buildSession.TowerTypeToBuild)
                : theme.GetRandomRoof();
            return Create(sprite, BlockType.Roof);
        }

        bool UseTowerTypeSprite()
        {
            GameMode mode = buildSession.GameMode;
            return mode == GameMode.Infinite || mode == GameMode.Classic;
        }

        Block Create(Sprite sprite, BlockType type)
        {
            GameObject blockObject = Instantiate(floorPrefab);
            blockObject.name = sprite.name;
            blockObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            SpriteRenderer spriteRenderer = blockObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;

            BoxCollider2D box = blockObject.GetComponent<BoxCollider2D>();
            box.size = sprite.bounds.size;
            box.offset = sprite.bounds.center;

            Transform blockHang = blockObject.transform.Find("Hang");
            blockHang.localPosition = new Vector3(0f, sprite.bounds.max.y, 0f);

            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            Block block = blockObject.GetComponent<Block>();
            block.type = type;
            return block;
        }
    }
}
