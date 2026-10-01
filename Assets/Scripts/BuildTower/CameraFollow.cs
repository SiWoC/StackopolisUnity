using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] BuildingSite buildingSite;
        [SerializeField] Transform followTarget;
        [SerializeField] int startAfterFloors = 2;
        [SerializeField] float smoothTime = 0.25f;

        bool following;
        float yOffset;
        float velocityY;

        void OnEnable()
        {
            buildingSite.StatusChanged += OnTowerStatusChanged;
        }

        void OnDisable()
        {
            if (buildingSite)
                buildingSite.StatusChanged -= OnTowerStatusChanged;
        }

        void LateUpdate()
        {
            if (!following)
                return;

            Vector3 position = transform.position;
            float targetY = followTarget.position.y + yOffset;
            position.y = Mathf.SmoothDamp(position.y, targetY, ref velocityY, smoothTime);
            transform.position = position;
        }

        void OnTowerStatusChanged(TowerStatus status)
        {
            if (following || status.Floors < startAfterFloors)
                return;

            following = true;
            yOffset = transform.position.y - followTarget.position.y;
            velocityY = 0f;
        }
    }
}
