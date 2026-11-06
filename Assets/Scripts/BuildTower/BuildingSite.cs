using System;
using System.Collections.Generic;
using SiWoC.Stackopolis.Globals;
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
        [Header("Landing")]
        [Tooltip("2D source (Spatial Blend 0). Play On Awake off.")]
        [SerializeField] AudioSource landingSource;
        [Tooltip("Place. Flat contact with the tower. The bottom plays this on the drop zone.")]
        [SerializeField] AudioClip successfulLand;
        [Tooltip("Follows place when that landing is perfect.")]
        [SerializeField] AudioClip perfect;
        [Tooltip("Collapse. A fallen block lies flat on the drop zone.")]
        [SerializeField] AudioClip landingAfterFalling;
        [Tooltip("Flat when the absolute angle is under this.")]
        [SerializeField] float levelAngle = 2f;
        [Header("Perfect Confetti")]
        [SerializeField] PerfectConfettiBurst perfectConfetti;

        readonly List<Block> tower = new List<Block>();
        readonly HashSet<Block> pendingFallen = new HashSet<Block>();
        int fallenFloors;
        bool gameOver;
        bool collapsePending;
        float collapseQuietUntil;
        int swayWindow;
        int perfectStreak;
        int nextGrowthFloor;
        Block dropping;
        bool placePlayed;
        readonly List<Block> groundWatch = new List<Block>();
        readonly HashSet<Block> collapsePlayed = new HashSet<Block>();
        float collapseBusyUntil;
        readonly List<ContactPoint2D> contacts = new List<ContactPoint2D>();

        public bool GameOver => gameOver;
        public float MetersPerFloor => metersPerFloor;
        public TowerStatus Status => BuildStatus();
        public Block Top => tower.Count > 0 ? tower[tower.Count - 1] : null;

        Difficulty ActiveDifficulty => BuildSession.Instance.Difficulty;
        public event Action<TowerStatus> StatusChanged;
        public event Action CollapseSettled;

        void Awake()
        {
            ResetSwayProgress();
        }

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
            dropping = null;
            placePlayed = false;
            groundWatch.Clear();
            collapsePlayed.Clear();
            collapseBusyUntil = 0f;
            ResetSwayProgress();

            // Cleanup stray fallen blocks
            Block[] blocks = FindObjectsByType<Block>();
            for (int i = 0; i < blocks.Length; i++)
                Destroy(blocks[i].gameObject);

            StatusChanged?.Invoke(BuildStatus());
        }

        /// <summary>
        /// Follows a released block until <see cref="Receive"/>.
        /// Place plays once, when the block is flat against the tower. The bottom plays on the drop zone.
        /// A later flat landing on the drop zone plays collapse.
        /// </summary>
        public void WatchDrop(Block block)
        {
            dropping = block;
            placePlayed = false;
        }

        public void Receive(Block block)
        {
            if (gameOver)
            {
                dropping = null;
                WatchGround(block);
                return;
            }

            TryPlaceSound(block);
            dropping = null;

            PruneFallen();

            // Prune can exhaust lives before this landing is applied — don't stack on a dead tower.
            if (fallenFloors >= fallenLimit)
            {
                gameOver = true;
                Debug.Log($"Game over fallen={fallenFloors}");
                ReleaseAsDebris(block);
                InterruptPerfectRun();
                ApplySwayWindow();
                StatusChanged?.Invoke(BuildStatus());
                return;
            }

            if (tower.Count == 0 || IsSupported(block, tower[tower.Count - 1]))
            {
                Block under = null;
                if (tower.Count > 0)
                {
                    under = tower[tower.Count - 1];
                    block.perfect = IsPerfect(block, under);
                    if (block.perfect)
                        SnapPerfectX(block, under);
                }

                Debug.Log($"Tower add {block.name} {block.type} perfect={block.perfect} count={tower.Count + 1} fallen={fallenFloors}", block);
                tower.Add(block);
                block.FellBelowValidY += OnBlockFellBelowValidY;
                block.ArmInTower(under, dropZone.bounds.min.y, supportOverlap);
                RecordPlacedBlock(block.perfect);
                if (block.perfect)
                {
                    if (block.type == BlockType.Middle)
                        ApplyPerfectMiddleSprite(block);
                    PlayLanding(perfect);
                    if (perfectConfetti && under)
                        perfectConfetti.Burst(block, under);
                }
            }
            else
            {
                fallenFloors++;
                Debug.Log($"Tower miss {block.name} fallen={fallenFloors}", block);
                ReleaseAsDebris(block);
                InterruptPerfectRun();
            }

            if (fallenFloors >= fallenLimit)
            {
                gameOver = true;
                //Debug.Log($"Game over fallen={fallenFloors}");
            }

            ApplySwayWindow();
            StatusChanged?.Invoke(BuildStatus());
        }

        void FixedUpdate()
        {
            WatchDroppingBlock();
            WatchGroundLandings();

            if (!collapsePending || gameOver)
                return;

            if (Time.time < collapseQuietUntil)
                return;

            if (!RemainingStackOk())
                return;

            CommitCollapse();
        }

        void PlayLanding(AudioClip clip)
        {
            if (!landingSource || !clip)
                return;

            landingSource.PlayOneShot(clip);
        }

        void WatchDroppingBlock()
        {
            if (!dropping)
                return;

            if (!gameOver)
                TryPlaceSound(dropping);

            // The bottom's flat contact with the drop zone is the place, and a block still against the tower has not fallen off yet.
            if (dropping.type == BlockType.Bottom)
                return;
            if (!gameOver && TouchesTower(dropping))
                return;

            TryCollapseSound(dropping);
        }

        void WatchGroundLandings()
        {
            for (int i = groundWatch.Count - 1; i >= 0; i--)
            {
                Block block = groundWatch[i];
                if (!block)
                {
                    groundWatch.RemoveAt(i);
                    continue;
                }

                TryCollapseSound(block);
            }
        }

        /// <summary>
        /// One place per drop: flat against the tower, or the bottom flat on the drop zone.
        /// </summary>
        void TryPlaceSound(Block block)
        {
            if (placePlayed || block != dropping || !IsLevel(block))
                return;

            bool bottomDown = block.type == BlockType.Bottom && IsTouchingDropZone(block);
            if (!bottomDown && !TouchesTower(block))
                return;

            placePlayed = true;
            PlayLanding(successfulLand);
        }

        /// <summary>
        /// One collapse per block. The clip starts only when the previous collapse clip has finished.
        /// </summary>
        void TryCollapseSound(Block block)
        {
            if (!block || collapsePlayed.Contains(block) || !LandedOnGround(block))
                return;

            collapsePlayed.Add(block);
            groundWatch.Remove(block);
            PlayCollapse();
        }

        void PlayCollapse()
        {
            if (!landingSource || !landingAfterFalling || Time.time < collapseBusyUntil)
                return;

            landingSource.PlayOneShot(landingAfterFalling);
            float pitch = Mathf.Max(0.01f, Mathf.Abs(landingSource.pitch));
            collapseBusyUntil = Time.time + landingAfterFalling.length / pitch;
        }

        void WatchGround(Block block)
        {
            if (!block || collapsePlayed.Contains(block))
                return;
            if (!groundWatch.Contains(block))
                groundWatch.Add(block);

            TryCollapseSound(block);
        }

        /// <summary>
        /// Drops the block out of the build and watches for a flat landing on the drop zone.
        /// Keeps destroy-watch so debris below the drop zone is cleaned up.
        /// </summary>
        void ReleaseAsDebris(Block block)
        {
            block.FellBelowValidY -= OnBlockFellBelowValidY;
            block.ArmDebris(dropZone.bounds.min.y);
            WatchGround(block);
        }

        bool IsLevel(Block block)
        {
            float angle = Mathf.Abs(Mathf.DeltaAngle(0f, block.GetComponent<Rigidbody2D>().rotation));
            return angle < levelAngle;
        }

        bool IsTouchingDropZone(Block block)
        {
            return block.GetComponent<Collider2D>().IsTouching(dropZone);
        }

        /// <summary>
        /// Drop zone, or another fallen block that is already on that pile.
        /// </summary>
        bool LandedOnGround(Block block)
        {
            if (IsTouchingDropZone(block))
                return true;

            Collider2D col = block.GetComponent<Collider2D>();
            int count = col.GetContacts(contacts);
            for (int i = 0; i < count; i++)
            {
                Collider2D otherCol = contacts[i].collider;
                if (!otherCol)
                    continue;

                if (otherCol == col)
                    otherCol = contacts[i].otherCollider;
                if (!otherCol)
                    continue;

                Block other = otherCol.GetComponent<Block>();
                if (!other || other == block)
                    continue;

                bool standing = tower.Contains(other) && !pendingFallen.Contains(other);
                if (standing)
                    continue;
                if (collapsePlayed.Contains(other) || IsTouchingDropZone(other))
                    return true;
            }

            return false;
        }

        bool TouchesTower(Block block)
        {
            Collider2D col = block.GetComponent<Collider2D>();
            for (int i = 0; i < tower.Count; i++)
            {
                Block floor = tower[i];
                if (!floor || floor == block)
                    continue;
                if (col.IsTouching(floor.GetComponent<Collider2D>()))
                    return true;
            }

            return false;
        }

        void OnBlockFellBelowValidY(Block block)
        {
            if (gameOver || !tower.Contains(block))
                return;

            pendingFallen.Add(block);
            collapsePending = true;
            collapseQuietUntil = Time.time + collapseQuietWindow;
            WatchGround(block);
            //Debug.Log($"Tower breach {block.name} quietUntil={collapseQuietUntil:0.00}", block);
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

                //Debug.Log($"Tower fall {block.name} fallen={fallenFloors}", block);
                ReleaseAsDebris(block);
            }

            bool shortened = first < tower.Count;
            if (shortened)
                tower.RemoveRange(first, tower.Count - first);

            pendingFallen.Clear();
            collapsePending = false;

            if (shortened)
                InterruptPerfectRun();

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
        /// The window starts at <see cref="swayTopCount"/> and grows by 1 when the tower reaches
        /// <c>swayTopCount + swayGrowEvery</c>, then every <see cref="swayGrowEvery"/> floors.
        /// After two perfect landings in a row, <see cref="Difficulty.Medium"/> lowers the window by 1 on that landing and on each further perfect, down to 0.
        /// <see cref="Difficulty.Easy"/> sets the window to 0 for that whole run.
        /// Growth waits during the run. When the run ends, the window rises to at least <see cref="swayTopCount"/>
        /// and the next +1 is <see cref="swayGrowEvery"/> floors above the tower left standing.
        /// On game over freezes the remaining stack instead (leaves pending/debris Dynamic).
        /// </summary>
        void ApplySwayWindow()
        {
            if (gameOver)
            {
                FreezeRemainingTower();
                return;
            }

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

        void FreezeRemainingTower()
        {
            for (int i = 0; i < tower.Count; i++)
            {
                Block block = tower[i];
                if (block == null || pendingFallen.Contains(block))
                    continue;

                Rigidbody2D body = block.GetComponent<Rigidbody2D>();
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.rotation = 0f;
                body.bodyType = RigidbodyType2D.Kinematic;
                Physics2D.SyncTransforms();

                if (i == 0)
                    continue;

                Block under = tower[i - 1];
                if (under == null || pendingFallen.Contains(under))
                    continue;

                Collider2D aboveCol = block.GetComponent<Collider2D>();
                Collider2D underCol = under.GetComponent<Collider2D>();
                float deltaY = underCol.bounds.max.y - aboveCol.bounds.min.y;
                Vector2 position = body.position;
                position.y += deltaY;
                body.position = position;
                Physics2D.SyncTransforms();
            }
        }

        int EffectiveSwayTopCount()
        {
            if (ActiveDifficulty == Difficulty.Easy || ActiveDifficulty == Difficulty.Medium)
                return Mathf.Max(0, swayWindow);

            int every = GrowInterval();
            int startFloor = swayTopCount + every;
            int bonus = tower.Count < startFloor ? 0 : 1 + (tower.Count - startFloor) / every;
            return swayTopCount + bonus;
        }

        void ResetSwayProgress()
        {
            swayWindow = Mathf.Max(0, swayTopCount);
            perfectStreak = 0;
            nextGrowthFloor = swayWindow + GrowInterval();
        }

        void RecordPlacedBlock(bool perfect)
        {
            if (ActiveDifficulty == Difficulty.Hard)
                return;

            if (perfect)
            {
                perfectStreak++;
                if (perfectStreak >= 2)
                {
                    ApplyStreakHelp();
                    return;
                }
            }
            else if (perfectStreak >= 2)
            {
                BreakSwayStreak();
                return;
            }
            else
            {
                perfectStreak = 0;
            }

            ApplyScheduledGrowth();
        }

        void InterruptPerfectRun()
        {
            if (ActiveDifficulty == Difficulty.Hard)
                return;

            if (perfectStreak >= 2)
                BreakSwayStreak();
            else
                perfectStreak = 0;
        }

        /// <summary>
        /// Medium lowers the window by 1, down to 0. Easy sets the window to 0.
        /// </summary>
        void ApplyStreakHelp()
        {
            if (ActiveDifficulty == Difficulty.Easy)
                swayWindow = 0;
            else if (ActiveDifficulty == Difficulty.Medium)
                swayWindow = Mathf.Max(0, swayWindow - 1);

            LogSway("help");
        }

        /// <summary>
        /// Ends a perfect run. The window returns to at least <see cref="swayTopCount"/>,
        /// and the next growth is <see cref="swayGrowEvery"/> floors above the tower now standing.
        /// </summary>
        void BreakSwayStreak()
        {
            perfectStreak = 0;
            swayWindow = Mathf.Max(swayTopCount, swayWindow);
            nextGrowthFloor = tower.Count + GrowInterval();
            LogSway("break");
        }

        void ApplyScheduledGrowth()
        {
            int every = GrowInterval();
            while (tower.Count >= nextGrowthFloor)
            {
                swayWindow++;
                nextGrowthFloor += every;
                LogSway("grow");
            }
        }

        int GrowInterval()
        {
            return Mathf.Max(1, swayGrowEvery);
        }

        void LogSway(string reason)
        {
            Debug.Log($"Sway {reason} {ActiveDifficulty} streak={perfectStreak} window={swayWindow} next={nextGrowthFloor} floors={tower.Count}", this);
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
                //Debug.Log($"Tower fall {tower[i].name} fallen={fallenFloors}", tower[i]);
                ReleaseAsDebris(tower[i]);
            }

            if (keep < tower.Count)
            {
                tower.RemoveRange(keep, tower.Count - keep);
                InterruptPerfectRun();
            }
        }

        bool IsSupported(Block block, Block under)
        {
            Collider2D above = block.GetComponent<Collider2D>();
            Collider2D below = under.GetComponent<Collider2D>();
            float bottom = above.bounds.min.y;
            float top = below.bounds.max.y;
            bool supported = bottom > top - supportOverlap;
            //Debug.Log($"IsSupported {block.name} on {under.name} bottom={bottom:0.000} top={top:0.000} overlap={supportOverlap} {supported}", block);
            return supported;
        }

        void ApplyPerfectMiddleSprite(Block block)
        {
            BuildSession session = BuildSession.Instance;
            Sprite sprite = session.CurrentTheme.GetPerfectMiddle(session.TowerTypeToBuild);
            if (!sprite)
                return;

            block.GetComponent<SpriteRenderer>().sprite = sprite;
        }

        bool IsPerfect(Block block, Block under)
        {
            Collider2D dropped = block.GetComponent<Collider2D>();
            Collider2D below = under.GetComponent<Collider2D>();
            float delta = Mathf.Abs(dropped.bounds.center.x - below.bounds.center.x);
            bool perfect = delta <= perfectSlack;
            //Debug.Log($"IsPerfect {block.name} delta={delta:0.000} slack={perfectSlack} {perfect}", block);
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
