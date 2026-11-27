using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    /// <summary>
    /// Spawns people that float diagonally into a placed floor, then despawn once they tuck behind it.
    /// Approach steepness comes from <see cref="maxHeightOffset"/> over the run from view edge to the floor.
    /// </summary>
    public class PopulationSpawner : MonoBehaviour
    {
        [SerializeField] ParasolPerson parasolPersonPrefab;
        [Tooltip("One Animator Controller per parasol look. Spawn picks at random.")]
        [SerializeField] RuntimeAnimatorController[] parasolLooks;
        [Tooltip("Population points per spawned person. +10 population with 2 → 5 people.")]
        [SerializeField] int populationPerPerson = 2;
        [SerializeField] float minSpeed = 1.2f;
        [SerializeField] float maxSpeed = 2.2f;
        [Tooltip("Max world-Y above the floor bottom for a spawn. Larger = steeper float-in.")]
        [SerializeField] float maxHeightOffset = 1.5f;
        [SerializeField] Camera viewCamera;
        [Tooltip("Extra gap past the view edge so the whole sprite starts off-screen.")]
        [SerializeField] float edgeMargin = 0.25f;

        /// <summary>
        /// Spawns people for a population gain on <paramref name="floor"/> (left and right).
        /// Person count is <paramref name="populationGained"/> / <see cref="populationPerPerson"/>.
        /// </summary>
        public void Spawn(Block floor, int populationGained)
        {
            int count = Mathf.Max(1, populationGained / populationPerPerson);
            int leftCount = count / 2;
            int rightCount = count - leftCount;
            if (leftCount != rightCount && Random.value < 0.5f)
                (leftCount, rightCount) = (rightCount, leftCount);

            Bounds floorBounds = floor.GetComponent<Collider2D>().bounds;
            SpawnSide(leftCount, floorBounds, -1);
            SpawnSide(rightCount, floorBounds, 1);
        }

        /// <summary>
        /// Destroys all in-flight people (e.g. on round reset).
        /// </summary>
        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        void SpawnSide(int sideCount, Bounds floorBounds, int sideSign)
        {
            // sideSign: -1 = arrive from the left, +1 = from the right.
            for (int i = 0; i < sideCount; i++)
            {
                float floorBottom = floorBounds.min.y;
                float height = Random.Range(0f, Mathf.Max(0f, maxHeightOffset));
                float speed = Random.Range(minSpeed, maxSpeed);

                RuntimeAnimatorController look = parasolLooks[Random.Range(0, parasolLooks.Length)];
                ParasolPerson person = Instantiate(parasolPersonPrefab, transform);
                person.Launch(floorBounds, sideSign, speed, viewCamera, edgeMargin, floorBottom + height, look);
            }
        }
    }
}
