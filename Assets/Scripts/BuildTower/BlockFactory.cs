using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    public class BlockFactory : MonoBehaviour
    {
        [SerializeField] BuildingTheme currentTheme;
        [SerializeField] GameObject floorPrefab;

        void Awake()
        {
            Debug.Assert(currentTheme != null, nameof(currentTheme), this);
            Debug.Assert(floorPrefab != null, nameof(floorPrefab), this);
        }

        public Block CreateBottom(BuildingTheme theme = null, bool randomSprite = true, int spriteIndex = 0)
        {
            BuildingTheme selected = theme != null ? theme : currentTheme;
            Sprite sprite = randomSprite ? selected.GetRandomBottom() : selected.GetBottom(spriteIndex);
            return Create(sprite, BlockType.Bottom);
        }

        public Block CreateMiddle(BuildingTheme theme = null, bool randomSprite = true, int spriteIndex = 0)
        {
            BuildingTheme selected = theme != null ? theme : currentTheme;
            Sprite sprite = randomSprite ? selected.GetRandomMiddle() : selected.GetMiddle(spriteIndex);
            return Create(sprite, BlockType.Middle);
        }

        public Block CreateRoof(BuildingTheme theme = null, bool randomSprite = true, int spriteIndex = 0)
        {
            BuildingTheme selected = theme != null ? theme : currentTheme;
            Sprite sprite = randomSprite ? selected.GetRandomRoof() : selected.GetRoof(spriteIndex);
            return Create(sprite, BlockType.Roof);
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
