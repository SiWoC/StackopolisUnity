using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    /// <summary>
    /// Moves along a climb matching the baked plane art (default 159 right, 123 up).
    /// Flip with scale.x; no runtime rotation. Spawns and despawns using full sprite bounds.
    /// </summary>
    public class PlaneFlyer : MonoBehaviour
    {
        [Tooltip("World delta for a rightward flight. Leftward uses (-x, y).")]
        [SerializeField] Vector2 climb = new(159f, 123f);
        [SerializeField] float edgeMargin = 0.25f;
        [SerializeField] AudioSource planeSource;
        [SerializeField] AudioClip planeFlyBy;

        SpriteRenderer spriteRenderer;
        float speed;
        int directionSign;
        Vector3 velocity;
        Camera viewCamera;
        bool flying;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <param name="directionSign">1 = left→right, -1 = right→left.</param>
        /// <param name="spawnY">World Y for the transform; X is placed so the whole sprite is off-screen.</param>
        public void Launch(int directionSign, float speed, Camera viewCamera, float spawnY)
        {
            this.directionSign = directionSign >= 0 ? 1 : -1;
            this.speed = Mathf.Max(0.01f, speed);
            this.viewCamera = viewCamera ? viewCamera : Camera.main;
            flying = this.viewCamera;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * this.directionSign;
            transform.localScale = scale;

            Vector2 dir = new(climb.x * this.directionSign, climb.y);
            if (dir.sqrMagnitude < 0.0001f)
                dir = new Vector2(this.directionSign, 0f);
            velocity = ((Vector3)dir.normalized) * this.speed;

            PlaceFullyOffScreen(spawnY);
            planeSource.PlayOneShot(planeFlyBy);
        }

        void Update()
        {
            if (!flying)
                return;

            transform.position += velocity * Time.deltaTime;

            if (HasExitedView() && !planeSource.isPlaying)
                Destroy(gameObject);
        }

        void PlaceFullyOffScreen(float spawnY)
        {
            float z = transform.position.z;
            float halfW = viewCamera.orthographicSize * viewCamera.aspect;
            float camX = viewCamera.transform.position.x;
            float viewLeft = camX - halfW;
            float viewRight = camX + halfW;

            // Rough place, then nudge using real bounds (trail included).
            float roughX = directionSign > 0 ? viewLeft - 1f : viewRight + 1f;
            transform.position = new Vector3(roughX, spawnY, z);

            Bounds b = spriteRenderer.bounds;
            if (directionSign > 0)
                transform.position += Vector3.right * ((viewLeft - edgeMargin) - b.max.x);
            else
                transform.position += Vector3.right * ((viewRight + edgeMargin) - b.min.x);
        }

        /// <summary>
        /// True once the whole sprite has left past the exit side (not the spawn side).
        /// </summary>
        bool HasExitedView()
        {
            Bounds b = spriteRenderer.bounds;
            float halfW = viewCamera.orthographicSize * viewCamera.aspect;
            float halfH = viewCamera.orthographicSize;
            Vector3 cam = viewCamera.transform.position;

            float left = cam.x - halfW;
            float right = cam.x + halfW;
            float top = cam.y + halfH;

            bool pastExitX = directionSign > 0 ? b.min.x > right : b.max.x < left;
            bool pastExitY = b.min.y > top;
            return pastExitX || pastExitY;
        }
    }
}
