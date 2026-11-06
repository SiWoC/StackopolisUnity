using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    /// <summary>
    /// Periodically spawns a seagull or plane flyby across the camera view.
    /// Spawn Y is at or above the starting camera center, within the current view.
    /// Flyers place themselves fully outside the view on X.
    /// </summary>
    public class AmbientFlyerSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] SeagullFlyer seagullPrefab;
        [SerializeField] PlaneFlyer planePrefab;

        [Header("Timing")]
        [SerializeField] float minInterval = 8f;
        [SerializeField] float maxInterval = 18f;
        [Tooltip("0 = only seagulls, 1 = only planes.")]
        [Range(0f, 1f)]
        [SerializeField] float planeChance = 0.35f;

        [Header("Motion")]
        [SerializeField] float seagullSpeedMin = 1.5f;
        [SerializeField] float seagullSpeedMax = 3f;
        [SerializeField] float planeSpeedMin = 4f;
        [SerializeField] float planeSpeedMax = 7f;

        [Header("Refs")]
        [SerializeField] Camera viewCamera;

        float nextSpawnAt;
        bool spawning = true;
        float minSpawnY;

        void Awake()
        {
            if (!viewCamera)
                viewCamera = Camera.main;

            CaptureMinSpawnY();
        }

        void OnEnable()
        {
            ScheduleNext();
        }

        void Update()
        {
            if (!spawning || !viewCamera)
                return;

            if (Time.time < nextSpawnAt)
                return;

            SpawnOne();
            ScheduleNext();
        }

        public void SetSpawning(bool enabled)
        {
            spawning = enabled;
            if (enabled)
                ScheduleNext();
        }

        /// <summary>
        /// Clears active flyers, re-captures min Y from the (reset) camera, and reschedules.
        /// Call after the camera is reset each round.
        /// </summary>
        public void ResetSpawn()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            CaptureMinSpawnY();
            ScheduleNext();
        }

        void CaptureMinSpawnY()
        {
            if (viewCamera)
                minSpawnY = viewCamera.transform.position.y;
        }

        void ScheduleNext()
        {
            float min = Mathf.Max(0.1f, minInterval);
            float max = Mathf.Max(min, maxInterval);
            nextSpawnAt = Time.time + Random.Range(min, max);
        }

        void SpawnOne()
        {
            bool spawnPlane = planePrefab && (!seagullPrefab || Random.value < planeChance);
            if (spawnPlane)
                SpawnPlane();
            else if (seagullPrefab)
                SpawnSeagull();
        }

        void SpawnSeagull()
        {
            int sign = Random.value < 0.5f ? 1 : -1;
            float speed = Random.Range(seagullSpeedMin, seagullSpeedMax);
            float y = PickSpawnY();

            SeagullFlyer flyer = Instantiate(seagullPrefab, transform);
            flyer.Launch(sign, speed, viewCamera, y);
        }

        void SpawnPlane()
        {
            int sign = Random.value < 0.5f ? 1 : -1;
            float speed = Random.Range(planeSpeedMin, planeSpeedMax);
            float y = PickSpawnY();

            PlaneFlyer flyer = Instantiate(planePrefab, transform);
            flyer.Launch(sign, speed, viewCamera, y);
        }

        float PickSpawnY()
        {
            float halfH = viewCamera.orthographicSize;
            float camY = viewCamera.transform.position.y;
            float viewBottom = camY - halfH;
            float viewTop = camY + halfH;
            float spawnMin = Mathf.Max(minSpawnY, viewBottom);
            float spawnMax = viewTop;
            if (spawnMax < spawnMin)
                spawnMax = spawnMin;
            return Random.Range(spawnMin, spawnMax);
        }
    }
}
