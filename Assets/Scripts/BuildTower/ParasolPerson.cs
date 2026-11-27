using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    /// <summary>
    /// One person floating into a floor. Animator plays on the prefab; this moves and despawns.
    /// </summary>
    public class ParasolPerson : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;
        Animator animator;
        Vector3 target;
        int sideSign;
        float speed;
        float nearEdgeX;
        bool flying;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        /// <param name="sideSign">-1 = from the left, +1 = from the right.</param>
        /// <param name="spawnBottomY">World Y for the sprite bottom at spawn.</param>
        public void Launch(
            Bounds floorBounds,
            int sideSign,
            float speed,
            Camera viewCamera,
            float edgeMargin,
            float spawnBottomY,
            RuntimeAnimatorController look)
        {
            this.sideSign = sideSign;
            this.speed = speed;
            nearEdgeX = sideSign < 0 ? floorBounds.min.x : floorBounds.max.x;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (sideSign < 0 ? 1f : -1f);
            transform.localScale = scale;

            animator.runtimeAnimatorController = look;
            // No sprite until the Animator samples; force the first frame before measuring.
            animator.Update(0f);

            PlaceFullyOffScreen(viewCamera, edgeMargin);
            SetSpriteBottomY(spawnBottomY);

            float pivotAboveBottom = PivotAboveSpriteBottom();
            target = new Vector3(
                floorBounds.center.x,
                floorBounds.min.y + pivotAboveBottom,
                transform.position.z);
            flying = true;
        }

        void Update()
        {
            if (!flying)
                return;

            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime);

            if (HasTuckedBehindFloor())
                Destroy(gameObject);
        }

        void PlaceFullyOffScreen(Camera viewCamera, float edgeMargin)
        {
            float z = transform.position.z;
            float y = transform.position.y;
            float halfW = viewCamera.orthographicSize * viewCamera.aspect;
            float camX = viewCamera.transform.position.x;
            float viewLeft = camX - halfW;
            float viewRight = camX + halfW;

            float roughX = sideSign < 0 ? viewLeft - 1f : viewRight + 1f;
            transform.position = new Vector3(roughX, y, z);

            Bounds b = spriteRenderer.bounds;
            if (sideSign < 0)
                transform.position += Vector3.right * ((viewLeft - edgeMargin) - b.max.x);
            else
                transform.position += Vector3.right * ((viewRight + edgeMargin) - b.min.x);
        }

        void SetSpriteBottomY(float bottomY)
        {
            float delta = bottomY - (transform.position.y - PivotAboveSpriteBottom());
            transform.position += Vector3.up * delta;
        }

        /// <summary>
        /// World distance from sprite rect bottom to the transform (high umbrella pivot).
        /// </summary>
        float PivotAboveSpriteBottom()
        {
            Sprite sprite = spriteRenderer.sprite;
            return sprite.pivot.y / sprite.pixelsPerUnit * Mathf.Abs(transform.lossyScale.y);
        }

        /// <summary>
        /// True once the whole sprite has crossed the floor's near edge (fully tucked behind).
        /// </summary>
        bool HasTuckedBehindFloor()
        {
            Bounds b = spriteRenderer.bounds;
            if (sideSign < 0)
                return b.min.x >= nearEdgeX;
            return b.max.x <= nearEdgeX;
        }
    }
}
