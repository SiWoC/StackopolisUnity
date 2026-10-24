using System.Collections.Generic;
using SiWoC.Stackopolis.Globals;
using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    /// <summary>
    /// Stacks theme sky tiles above a base city background.
    /// Tile 2 and 4 counts come from BuildSession.CurrentTheme; tile 7 extends with the camera.
    /// </summary>
    public class BackgroundSkyStack : MonoBehaviour
    {
        [Tooltip("Scene city background. Sprite comes from the theme when theme.background is set.")]
        [SerializeField] SpriteRenderer baseBackground;

        [Header("Tile 7 (infinite)")]
        [SerializeField] int tile7PoolSize = 2;
        [SerializeField] Camera viewCamera;

        readonly List<SpriteRenderer> tile7Pool = new();
        Transform segmentsRoot;
        float stackTopY;
        float tile7Height;
        Vector3 placeScale = Vector3.one;
        float placeX;
        float placeZ;
        int sortingLayerId;
        int sortingOrder;
        Sprite tile7;

        void Awake()
        {
            if (!viewCamera)
                viewCamera = Camera.main;

            Rebuild();
        }

        void LateUpdate()
        {
            ExtendTile7IfNeeded();
        }

        [ContextMenu("Rebuild Stack")]
        public void Rebuild()
        {
            EnsureRoot();
            ClearSegments();
            tile7Pool.Clear();
            tile7 = null;
            tile7Height = 0f;

            BuildingTheme theme = BuildSession.Instance.CurrentTheme;

            if (theme.background)
                baseBackground.sprite = theme.background;

            placeScale = baseBackground.transform.lossyScale;
            placeX = baseBackground.transform.position.x;
            placeZ = baseBackground.transform.position.z;
            stackTopY = baseBackground.bounds.max.y;
            sortingLayerId = baseBackground.sortingLayerID;
            sortingOrder = baseBackground.sortingOrder;

            Place(theme.gradient1, "1_gradient");
            for (int i = 0; i < Mathf.Max(0, theme.tile2Count); i++)
                Place(theme.tile2, $"2_tile_{i + 1}");
            Place(theme.gradient3, "3_gradient");
            for (int i = 0; i < Mathf.Max(0, theme.tile4Count); i++)
                Place(theme.tile4, $"4_tile_{i + 1}");
            Place(theme.gradient5, "5_gradient");
            Place(theme.moon6, "6_moon");

            tile7 = theme.tile7;
            tile7Height = WorldHeight(tile7);
            int pool = Mathf.Max(1, tile7PoolSize);
            for (int i = 0; i < pool; i++)
            {
                SpriteRenderer sr = Place(tile7, $"7_tile_{i + 1}");
                if (sr)
                    tile7Pool.Add(sr);
            }

            ExtendTile7IfNeeded();
        }

        /// <summary>
        /// Rebuilds the fixed stack and resets the tile 7 pool (e.g. on new round).
        /// </summary>
        public void ResetStack()
        {
            Rebuild();
        }

        void ExtendTile7IfNeeded()
        {
            if (!viewCamera || !tile7 || tile7Pool.Count == 0 || tile7Height <= 0f)
                return;

            // Keep coverage one tile above the camera view.
            float viewTop = viewCamera.transform.position.y + OrthoHalfHeight(viewCamera) + tile7Height;

            while (stackTopY < viewTop)
            {
                SpriteRenderer lowest = tile7Pool[0];
                float lowestBottom = lowest.bounds.min.y;
                for (int i = 1; i < tile7Pool.Count; i++)
                {
                    float bottom = tile7Pool[i].bounds.min.y;
                    if (bottom < lowestBottom)
                    {
                        lowestBottom = bottom;
                        lowest = tile7Pool[i];
                    }
                }

                // Only recycle a tile that is fully below the camera view.
                float viewBottom = viewCamera.transform.position.y - OrthoHalfHeight(viewCamera);
                if (lowest.bounds.max.y > viewBottom)
                {
                    // Need a new instance instead of recycling.
                    SpriteRenderer extra = Place(tile7, $"7_tile_{tile7Pool.Count + 1}");
                    if (extra)
                        tile7Pool.Add(extra);
                    else
                        break;
                    continue;
                }

                float newBottom = stackTopY;
                float newCenterY = newBottom + tile7Height * 0.5f;
                lowest.transform.position = new Vector3(placeX, newCenterY, placeZ);
                stackTopY = newBottom + tile7Height;
            }
        }

        SpriteRenderer Place(Sprite sprite, string label)
        {
            if (!sprite)
                return null;

            float height = WorldHeight(sprite);
            float centerY = stackTopY + height * 0.5f;

            var go = new GameObject(label);
            go.transform.SetParent(segmentsRoot, false);
            go.transform.position = new Vector3(placeX, centerY, placeZ);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(
                placeScale.x / segmentsRoot.lossyScale.x,
                placeScale.y / segmentsRoot.lossyScale.y,
                placeScale.z / segmentsRoot.lossyScale.z);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerID = sortingLayerId;
            sr.sortingOrder = sortingOrder;

            stackTopY += height;
            return sr;
        }

        float WorldHeight(Sprite sprite)
        {
            if (!sprite)
                return 0f;
            return sprite.bounds.size.y * placeScale.y;
        }

        static float OrthoHalfHeight(Camera cam)
        {
            if (cam.orthographic)
                return cam.orthographicSize;
            return cam.orthographicSize > 0f ? cam.orthographicSize : 5f;
        }

        void EnsureRoot()
        {
            if (segmentsRoot)
                return;

            Transform existing = transform.Find("SkySegments");
            if (existing)
            {
                segmentsRoot = existing;
                return;
            }

            var go = new GameObject("SkySegments");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            segmentsRoot = go.transform;
        }

        void ClearSegments()
        {
            if (!segmentsRoot)
                return;

            for (int i = segmentsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = segmentsRoot.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            stackTopY = 0f;
        }
    }
}
