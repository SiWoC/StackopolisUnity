using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    /// <summary>
    /// Short pixel confetti puff at both ends of a perfect joint.
    /// Builds a shared white Point-filter sprite and tints each piece from <see cref="colors"/>.
    /// </summary>
    public class PerfectConfettiBurst : MonoBehaviour
    {
        [SerializeField] Color[] colors =
        {
            new Color(1f, 0.85f, 0.2f),
            new Color(1f, 0.35f, 0.45f),
            new Color(0.35f, 0.85f, 1f),
            new Color(0.45f, 1f, 0.4f),
            new Color(1f, 0.55f, 0.15f),
        };
        [SerializeField] int count = 8;
        [SerializeField] int pixelSize = 2;
        [SerializeField] float duration = 0.45f;
        [SerializeField] float maxSpeed = 2.5f;
        [SerializeField] int sortingOrder = 20;

        const float PixelsPerUnit = 100f;
        const float Gravity = 9f;
        const float Grid = 0.01f;

        Sprite pieceSprite;
        int spritePixelSize = -1;

        /// <summary>
        /// Spawns confetti at the left and right ends of the joint between <paramref name="placed"/> and <paramref name="under"/>.
        /// </summary>
        public void Burst(Block placed, Block under)
        {
            if (count <= 0 || placed == null || under == null)
                return;

            EnsureSprite();

            Collider2D above = placed.GetComponent<Collider2D>();
            Collider2D below = under.GetComponent<Collider2D>();
            float seamY = below.bounds.max.y;
            float leftX = Mathf.Max(above.bounds.min.x, below.bounds.min.x);
            float rightX = Mathf.Min(above.bounds.max.x, below.bounds.max.x);

            int leftCount = count / 2;
            int rightCount = count - leftCount;

            SpawnSide(leftCount, new Vector3(leftX, seamY, 0f), -1f);
            SpawnSide(rightCount, new Vector3(rightX, seamY, 0f), 1f);
        }

        void SpawnSide(int sideCount, Vector3 origin, float outwardSign)
        {
            float life = Mathf.Max(0.05f, duration);
            float speedCap = Mathf.Max(0.01f, maxSpeed);

            for (int i = 0; i < sideCount; i++)
            {
                GameObject go = new GameObject("Confetti");
                go.transform.SetParent(transform, false);
                go.transform.position = Snap(origin);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = pieceSprite;
                sr.sortingOrder = sortingOrder;
                Color tint = PickColor();
                sr.color = tint;

                float speed = Random.Range(speedCap * 0.35f, speedCap);
                float angle = Random.Range(25f, 80f) * Mathf.Deg2Rad;
                Vector2 velocity = new Vector2(
                    outwardSign * Mathf.Cos(angle) * speed,
                    Mathf.Sin(angle) * speed);

                Piece piece = go.AddComponent<Piece>();
                piece.Begin(sr, tint, velocity, life);
            }
        }

        Color PickColor()
        {
            if (colors == null || colors.Length == 0)
                return Color.white;
            return colors[Random.Range(0, colors.Length)];
        }

        void EnsureSprite()
        {
            int size = Mathf.Max(1, pixelSize);
            if (pieceSprite != null && spritePixelSize == size)
                return;

            if (pieceSprite != null)
            {
                if (pieceSprite.texture != null)
                    Destroy(pieceSprite.texture);
                Destroy(pieceSprite);
            }

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply(false, true);

            pieceSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            spritePixelSize = size;
        }

        void OnDestroy()
        {
            if (pieceSprite == null)
                return;
            if (pieceSprite.texture != null)
                Destroy(pieceSprite.texture);
            Destroy(pieceSprite);
            pieceSprite = null;
        }

        static Vector3 Snap(Vector3 p)
        {
            return new Vector3(
                Mathf.Round(p.x / Grid) * Grid,
                Mathf.Round(p.y / Grid) * Grid,
                0f);
        }

        class Piece : MonoBehaviour
        {
            SpriteRenderer spriteRenderer;
            Color baseColor;
            Vector2 velocity;
            float life;
            float duration;

            public void Begin(SpriteRenderer sr, Color color, Vector2 velocity, float duration)
            {
                spriteRenderer = sr;
                baseColor = color;
                this.velocity = velocity;
                this.duration = duration;
                life = 0f;
            }

            void Update()
            {
                float dt = Time.deltaTime;
                velocity.y -= Gravity * dt;

                Vector3 p = transform.position;
                p.x += velocity.x * dt;
                p.y += velocity.y * dt;
                transform.position = Snap(p);

                life += dt;
                float t = Mathf.Clamp01(life / duration);
                Color c = baseColor;
                c.a = baseColor.a * (1f - t);
                spriteRenderer.color = c;

                if (life >= duration)
                    Destroy(gameObject);
            }
        }
    }
}
