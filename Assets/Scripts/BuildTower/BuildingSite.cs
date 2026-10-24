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
        [SerializeField] Collider2D dropZone;
        [SerializeField] float collapseQuietWindow = 0.3f;

        readonly List<Block> tower = new List<Block>();
        readonly HashSet<Block> pendingFallen = new HashSet<Block>();
        int fallenFloors;
        bool gameOver;
        bool collapsePending;
        float collapseQuietUntil;

        public bool GameOver => gameOver;
        public TowerStatus Status => BuildStatus();
        public Block Top => tower.Count > 0 ? tower[tower.Count - 1] : null;
        public event Action<TowerStatus> StatusChanged;
        public event Action CollapseSettled;

        public void Reset()
        {
            for (int i = 0; i < tower.Count; i++)
            {
                Block block = tower[i];
                if (block == null)
                    continue;

                block.FellBelowValidY -= OnBlockFellBelowValidY;
                block.Disarm();
            }

            tower.Clear();
            pendingFallen.Clear();
            fallenFloors = 0;
            gameOver = false;
            collapsePending = false;

            // Cleanup stray fallen blocks
            Block[] blocks = FindObjectsByType<Block>();
            for (int i = 0; i < blocks.Length; i++)
                Destroy(blocks[i].gameObject);

            StatusChanged?.Invoke(BuildStatus());
        }

        public void Receive(Block block)
        {
            if (gameOver)
                return;

            PruneFallen();

            // Prune can exhaust lives before this landing is applied — don't stack on a dead tower.
            if (fallenFloors >= fallenLimit)
            {
                gameOver = true;
                Debug.Log($"Game over fallen={fallenFloors}");
                block.ArmDebris(dropZone.bounds.min.y);
                ApplySwayWindow();
                StatusChanged?.Invoke(BuildStatus());
                return;
            }

            if (tower.Count == 0 || IsSupported(block, tower[tower.Count - 1]))
            {
                if (tower.Count > 0)
                {
                    Block under = tower[tower.Count - 1];
                    block.perfect = IsPerfect(block, under);
                    if (block.perfect)
                        SnapPerfectX(block, under);
                }

                Debug.Log($"Tower add {block.name} {block.type} perfect={block.perfect} count={tower.Count + 1} fallen={fallenFloors}", block);
                tower.Add(block);
                ArmInTower(block);
            }
            else
            {
                fallenFloors++;
                Debug.Log($"Tower miss {block.name} fallen={fallenFloors}", block);
                block.ArmDebris(dropZone.bounds.min.y);
            }

            if (fallenFloors >= fallenLimit)
            {
                gameOver = true;
                Debug.Log($"Game over fallen={fallenFloors}");
            }

            ApplySwayWindow();
            StatusChanged?.Invoke(BuildStatus());
        }

        void FixedUpdate()
        {
            if (!collapsePending || gameOver)
                return;

            if (Time.time < collapseQuietUntil)
                return;

            if (!RemainingStackOk())
                return;

            CommitCollapse();
        }

        void ArmInTower(Block block)
        {
            float validY = block.GetComponent<Collider2D>().bounds.min.y;
            block.FellBelowValidY += OnBlockFellBelowValidY;
            block.ArmInTower(validY, dropZone.bounds.min.y, supportOverlap);
        }

        void OnBlockFellBelowValidY(Block block)
        {
            if (gameOver || !tower.Contains(block))
                return;

            pendingFallen.Add(block);
            collapsePending = true;
            collapseQuietUntil = Time.time + collapseQuietWindow;
            Debug.Log($"Tower breach {block.name} quietUntil={collapseQuietUntil:0.00}", block);
        }

        bool RemainingStackOk()
        {
            for (int i = 0; i < tower.Count; i++)
            {
                Block block = tower[i];
                if (block == null || pendingFallen.Contains(block))
                    continue;

                if (!block.IsWithinValidY())
                    return false;
            }

            return true;
        }

        void CommitCollapse()
        {
            int first = tower.Count;
            for (int i = 0; i < tower.Count; i++)
            {
                Block block = tower[i];
                if (block == null || pendingFallen.Contains(block))
                {
                    first = i;
                    break;
                }
            }

            for (int i = first; i < tower.Count; i++)
            {
                Block block = tower[i];
                fallenFloors++;
                if (block == null)
                    continue;

                Debug.Log($"Tower fall {block.name} fallen={fallenFloors}", block);
                block.FellBelowValidY -= OnBlockFellBelowValidY;
                // Keep destroy-watch so debris below DropZone is cleaned up; stop valid-Y events.
                block.ArmDebris(dropZone.bounds.min.y);
            }

            if (first < tower.Count)
                tower.RemoveRange(first, tower.Count - first);

            pendingFallen.Clear();
            collapsePending = false;

            if (fallenFloors >= fallenLimit)
            {
                gameOver = true;
                Debug.Log($"Game over fallen={fallenFloors}");
            }

            ApplySwayWindow();
            StatusChanged?.Invoke(BuildStatus());
            CollapseSettled?.Invoke();
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
                tower[i].FellBelowValidY -= OnBlockFellBelowValidY;
                tower[i].ArmDebris(dropZone.bounds.min.y);
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

        bool IsPerfect(Block block, Block under)
        {
            Collider2D dropped = block.GetComponent<Collider2D>();
            Collider2D below = under.GetComponent<Collider2D>();
            float delta = Mathf.Abs(dropped.bounds.center.x - below.bounds.center.x);
            bool perfect = delta <= perfectSlack;
            Debug.Log($"IsPerfect {block.name} delta={delta:0.000} slack={perfectSlack} {perfect}", block);
            return perfect;
        }

        void SnapPerfectX(Block block, Block under)
        {
            Rigidbody2D body = block.GetComponent<Rigidbody2D>();
            Collider2D dropped = block.GetComponent<Collider2D>();
            Collider2D below = under.GetComponent<Collider2D>();
            float deltaX = below.bounds.center.x - dropped.bounds.center.x;

            Vector2 position = body.position;
            position.x += deltaX;
            body.position = position;

            Vector2 velocity = body.linearVelocity;
            velocity.x = 0f;
            body.linearVelocity = velocity;
        }
    }
}
