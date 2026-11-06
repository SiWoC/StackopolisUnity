using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    /// <summary>
    /// Crosses the view horizontally while the Animator plays the flap clip.
    /// Spawns and despawns using full sprite bounds.
    /// Plays <see cref="cry"/> once, at a random moment from first visibility until the sprite center has crossed <see cref="cryAcross"/> of the view width.
    /// </summary>
    public class SeagullFlyer : MonoBehaviour
    {
        [SerializeField] float edgeMargin = 0.25f;
        [Header("Cry")]
        [Tooltip("2D source on this bird (Spatial Blend 0). Play On Awake stays off; the cry starts from code.")]
        [SerializeField] AudioSource crySource;
        [SerializeField] AudioClip cry;
        [Tooltip("Cry is scheduled somewhere from first visibility until the sprite center has crossed this fraction of the view width.")]
        [Range(0f, 1f)]
        [SerializeField] float cryAcross = 0.8f;

        SpriteRenderer spriteRenderer;
        float speed;
        int directionSign;
        Camera viewCamera;
        bool flying;
        bool cryScheduled;
        bool cryPlayed;
        float cryAt;

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
            cryScheduled = false;
            cryPlayed = false;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * this.directionSign;
            transform.localScale = scale;

            PlaceFullyOffScreen(spawnY);
        }

        void Update()
        {
            if (!flying)
                return;

            transform.position += Vector3.right * (directionSign * speed * Time.deltaTime);
            WatchCry();

            if (HasExitedView())
                Destroy(gameObject);
        }

        /// <summary>
        /// One cry per flight. The moment is uniform from the first frame the sprite overlaps the view
        /// until its center has crossed <see cref="cryAcross"/> of the view width from the entry side.
        /// </summary>
        void WatchCry()
        {
            if (cryPlayed || !OverlapsView())
                return;

            if (!cryScheduled)
            {
                cryScheduled = true;
                float halfW = viewCamera.orthographicSize * viewCamera.aspect;
                float entryX = viewCamera.transform.position.x - directionSign * halfW;
                float targetX = entryX + directionSign * (cryAcross * halfW * 2f);
                float ahead = (targetX - spriteRenderer.bounds.center.x) * directionSign;
                float untilTarget = ahead > 0f ? ahead / speed : 0f;
                cryAt = Time.time + Random.Range(0f, untilTarget);
            }

            if (Time.time < cryAt)
                return;

            cryPlayed = true;
            crySource.PlayOneShot(cry);
        }

        bool OverlapsView()
        {
            Bounds b = spriteRenderer.bounds;
            float halfW = viewCamera.orthographicSize * viewCamera.aspect;
            float camX = viewCamera.transform.position.x;
            return b.max.x > camX - halfW && b.min.x < camX + halfW;
        }

        void PlaceFullyOffScreen(float spawnY)
        {
            float z = transform.position.z;
            float halfW = viewCamera.orthographicSize * viewCamera.aspect;
            float camX = viewCamera.transform.position.x;
            float viewLeft = camX - halfW;
            float viewRight = camX + halfW;

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
            Vector3 cam = viewCamera.transform.position;

            float left = cam.x - halfW;
            float right = cam.x + halfW;

            return directionSign > 0 ? b.min.x > right : b.max.x < left;
        }
    }
}
