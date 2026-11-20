using System.Collections.Generic;
using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    /// <summary>
    /// One flyby kind with its own speed, weight, and world-Y window.
    /// </summary>
    [System.Serializable]
    public class AltitudeFlyer
    {
        [Tooltip("Prefab component. Any AmbientFlyer.")]
        public AmbientFlyer prefab;

        [Tooltip("Chance weight while this window overlaps the view. Eligible kinds are rolled against each other.")]
        [Min(0f)]
        public float weight = 1f;

        public float speedMin = 2f;
        public float speedMax = 4f;

        [Tooltip("Spawn at or above this world Y.")]
        public float minHeight;

        [Tooltip("Spawn at or below this world Y. Use a large value such as 9999 for no practical ceiling.")]
        public float maxHeight = 9999f;
    }

    /// <summary>
    /// Periodically spawns a flyby across the camera view.
    /// Spawn Y is at or above the starting camera center, within the current view, and inside that kind's height window.
    /// Flyers place themselves fully outside the view on X.
    /// </summary>
    public class AmbientFlyerSpawner : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] float minInterval = 8f;
        [SerializeField] float maxInterval = 18f;

        [Header("Flyers")]
        [Tooltip("Each kind has its own height window and weight.")]
        [SerializeField] AltitudeFlyer[] altitudeFlyers;

        [Header("Refs")]
        [SerializeField] Camera viewCamera;

        readonly List<SpawnChoice> choices = new();
        float nextSpawnAt;
        bool spawning = true;
        float minSpawnY;

        struct SpawnChoice
        {
            public AmbientFlyer prefab;
            public float weight;
            public float speedMin;
            public float speedMax;
            public float yMin;
            public float yMax;
        }

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
            choices.Clear();
            if (altitudeFlyers != null)
            {
                for (int i = 0; i < altitudeFlyers.Length; i++)
                {
                    AltitudeFlyer entry = altitudeFlyers[i];
                    if (entry == null)
                        continue;

                    TryAdd(
                        entry.prefab,
                        entry.weight,
                        entry.speedMin,
                        entry.speedMax,
                        entry.minHeight,
                        entry.maxHeight);
                }
            }

            if (choices.Count == 0)
                return;

            SpawnChoice pick = Roll(choices);
            int sign = Random.value < 0.5f ? 1 : -1;
            float speed = Random.Range(pick.speedMin, pick.speedMax);
            float y = Random.Range(pick.yMin, pick.yMax);

            AmbientFlyer flyer = Instantiate(pick.prefab, transform);
            flyer.Launch(sign, speed, viewCamera, y);
        }

        void TryAdd(
            AmbientFlyer prefab,
            float weight,
            float speedMin,
            float speedMax,
            float minHeight,
            float maxHeight)
        {
            if (!prefab || weight <= 0f)
                return;

            if (!TryWindow(minHeight, maxHeight, out float yMin, out float yMax))
                return;

            choices.Add(new SpawnChoice
            {
                prefab = prefab,
                weight = weight,
                speedMin = speedMin,
                speedMax = speedMax,
                yMin = yMin,
                yMax = yMax
            });
        }

        /// <summary>
        /// Overlap of the current view, the starting camera center, and this kind's height window.
        /// </summary>
        bool TryWindow(float minHeight, float maxHeight, out float yMin, out float yMax)
        {
            float halfH = viewCamera.orthographicSize;
            float camY = viewCamera.transform.position.y;
            yMin = Mathf.Max(minSpawnY, camY - halfH, minHeight);
            yMax = Mathf.Min(camY + halfH, maxHeight);
            return yMax >= yMin;
        }

        static SpawnChoice Roll(List<SpawnChoice> options)
        {
            float total = 0f;
            for (int i = 0; i < options.Count; i++)
                total += options[i].weight;

            float roll = Random.value * total;
            float acc = 0f;
            for (int i = 0; i < options.Count; i++)
            {
                acc += options[i].weight;
                if (roll <= acc)
                    return options[i];
            }

            return options[options.Count - 1];
        }
    }
}
