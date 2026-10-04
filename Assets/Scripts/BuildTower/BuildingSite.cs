using System;
using System.Collections.Generic;
using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    public readonly struct TowerStatus
    {
        public readonly int Floors;
        public readonly float Meters;
        public readonly int FallenFloors;
        public readonly int LivesRemaining;
        public readonly bool GameOver;

        public TowerStatus(int floors, float meters, int fallenFloors, int livesRemaining, bool gameOver)
        {
            Floors = floors;
            Meters = meters;
            FallenFloors = fallenFloors;
            LivesRemaining = livesRemaining;
            GameOver = gameOver;
        }
    }

    public class BuildingSite : MonoBehaviour
    {
        [SerializeField] float perfectSlack = 0.1f;
        [SerializeField] float supportOverlap = 0.4f;
        [SerializeField] int fallenLimit = 3;
        [SerializeField] float metersPerFloor = 3f;
        [SerializeField] int swayTopCount = 5;
        [SerializeField] int swayGrowEvery = 7;

        readonly List<Block> tower = new List<Block>();
        int fallenFloors;
        bool gameOver;

        public bool GameOver => gameOver;
        public TowerStatus Status => BuildStatus();
        public Block Top => tower.Count > 0 ? tower[tower.Count - 1] : null;
        public event Action<TowerStatus> StatusChanged;

        public void Receive(Block block)
        {
            if (gameOver)
                return;

            PruneFallen();

            if (tower.Count == 0 || IsSupported(block, tower[tower.Count - 1]))
            {
                if (tower.Count > 0)
                    block.perfect = IsPerfect(block);

                Debug.Log($"Tower add {block.name} {block.type} perfect={block.perfect} count={tower.Count + 1} fallen={fallenFloors}", block);
                tower.Add(block);
            }
            else
            {
                fallenFloors++;
                Debug.Log($"Tower miss {block.name} fallen={fallenFloors}", block);
            }

            if (fallenFloors >= fallenLimit)
            {
                gameOver = true;
                Debug.Log($"Game over fallen={fallenFloors}");
            }

            ApplySwayWindow();
            StatusChanged?.Invoke(BuildStatus());
        }

        /// <summary>
        /// Keeps the top sway window of tower blocks Dynamic; freezes lower floors as Kinematic.
        /// Window starts at <see cref="swayTopCount"/> and grows by 1 at floor
        /// <c>swayTopCount + swayGrowEvery</c>, then every <see cref="swayGrowEvery"/> floors after that.
        /// </summary>
        void ApplySwayWindow()
        {
            int sway = EffectiveSwayTopCount();
            int firstSway = Mathf.Max(0, tower.Count - Mathf.Max(0, sway));
            for (int i = 0; i < tower.Count; i++)
            {
                Rigidbody2D body = tower[i].GetComponent<Rigidbody2D>();
                if (i < firstSway)
                {
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                    body.bodyType = RigidbodyType2D.Kinematic;
                }
                else
                {
                    body.bodyType = RigidbodyType2D.Dynamic;
                }
            }
        }

        int EffectiveSwayTopCount()
        {
            int every = Mathf.Max(1, swayGrowEvery);
            int startFloor = swayTopCount + every;
            int bonus = tower.Count < startFloor ? 0 : 1 + (tower.Count - startFloor) / every;
            return swayTopCount + bonus;
        }

        TowerStatus BuildStatus()
        {
            int floors = tower.Count;
            int lives = Mathf.Max(0, fallenLimit - fallenFloors);
            return new TowerStatus(floors, floors * metersPerFloor, fallenFloors, lives, gameOver);
        }

        void PruneFallen()
        {
            int keep = tower.Count > 0 ? 1 : 0;
            for (int i = 1; i < tower.Count; i++)
            {
                if (!IsSupported(tower[i], tower[i - 1]))
                    break;

                keep = i + 1;
            }

            for (int i = keep; i < tower.Count; i++)
            {
                fallenFloors++;
                Debug.Log($"Tower fall {tower[i].name} fallen={fallenFloors}", tower[i]);
            }

            if (keep < tower.Count)
                tower.RemoveRange(keep, tower.Count - keep);
        }

        bool IsSupported(Block block, Block under)
        {
            Collider2D above = block.GetComponent<Collider2D>();
            Collider2D below = under.GetComponent<Collider2D>();
            float bottom = above.bounds.min.y;
            float top = below.bounds.max.y;
            bool supported = bottom > top - supportOverlap;
            Debug.Log($"IsSupported {block.name} on {under.name} bottom={bottom:0.000} top={top:0.000} overlap={supportOverlap} {supported}", block);
            return supported;
        }

        bool IsPerfect(Block block)
        {
            Collider2D dropped = block.GetComponent<Collider2D>();
            Collider2D under = tower[tower.Count - 1].GetComponent<Collider2D>();
            float delta = Mathf.Abs(dropped.bounds.center.x - under.bounds.center.x);
            bool perfect = delta <= perfectSlack;
            Debug.Log($"IsPerfect {block.name} delta={delta:0.000} slack={perfectSlack} {perfect}", block);
            return perfect;
        }
    }
}
