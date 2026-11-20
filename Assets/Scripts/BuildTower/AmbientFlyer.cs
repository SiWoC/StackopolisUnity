using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    /// <summary>
    /// One flyby the spawner can place across the camera view.
    /// </summary>
    public abstract class AmbientFlyer : MonoBehaviour
    {
        /// <param name="directionSign">1 = left→right, -1 = right→left.</param>
        /// <param name="spawnY">World Y for the transform; X is placed so the whole sprite is off-screen.</param>
        public abstract void Launch(int directionSign, float speed, Camera viewCamera, float spawnY);
    }
}
