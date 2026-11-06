using System;
using UnityEngine;

namespace SiWoC.Stackopolis.BuildTower
{
    public enum BlockType
    {
        Bottom,
        Middle,
        Roof
    }

    public class Block : MonoBehaviour
    {
        public BlockType type;
        public bool perfect;

        public event Action<Block> FellBelowValidY;

        Block support;
        float destroyBelowY;
        float fallSlack;
        bool watchSupport;
        bool watchDestroy;
        bool breached;

        /// <summary>
        /// Floor this block landed on; null for the tower base.
        /// </summary>
        public Block Support => support;

        public void ArmInTower(Block support, float destroyBelowY, float fallSlack)
        {
            this.support = support;
            this.destroyBelowY = destroyBelowY;
            this.fallSlack = fallSlack;
            watchSupport = support != null;
            watchDestroy = true;
            breached = false;
        }

        public void ArmDebris(float destroyBelowY)
        {
            this.destroyBelowY = destroyBelowY;
            support = null;
            watchSupport = false;
            watchDestroy = true;
            breached = false;
            FellBelowValidY = null;
        }

        public void Disarm()
        {
            support = null;
            watchSupport = false;
            watchDestroy = false;
            FellBelowValidY = null;
        }

        public bool IsWithinValidY()
        {
            if (!support)
                return true;

            float bottom = GetComponent<Collider2D>().bounds.min.y;
            float top = support.GetComponent<Collider2D>().bounds.max.y;
            return bottom > top - fallSlack;
        }

        void FixedUpdate()
        {
            if (!watchSupport && !watchDestroy)
                return;

            if (watchSupport && !breached && !IsWithinValidY())
            {
                breached = true;
                FellBelowValidY?.Invoke(this);
            }

            if (watchDestroy && GetComponent<Collider2D>().bounds.min.y < destroyBelowY)
            {
                Disarm();
                Destroy(gameObject);
            }
        }
    }
}
