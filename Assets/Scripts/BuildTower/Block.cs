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

        float validY;
        float destroyBelowY;
        float fallSlack;
        bool watchValidY;
        bool watchDestroy;
        bool breached;

        public float ValidY => validY;

        public void ArmInTower(float validY, float destroyBelowY, float fallSlack)
        {
            this.validY = validY;
            this.destroyBelowY = destroyBelowY;
            this.fallSlack = fallSlack;
            watchValidY = true;
            watchDestroy = true;
            breached = false;
        }

        public void ArmDebris(float destroyBelowY)
        {
            this.destroyBelowY = destroyBelowY;
            watchValidY = false;
            watchDestroy = true;
            breached = false;
            FellBelowValidY = null;
        }

        public void Disarm()
        {
            watchValidY = false;
            watchDestroy = false;
            FellBelowValidY = null;
        }

        public bool IsWithinValidY()
        {
            return GetComponent<Collider2D>().bounds.min.y >= validY - fallSlack;
        }

        void FixedUpdate()
        {
            if (!watchValidY && !watchDestroy)
                return;

            float y = GetComponent<Collider2D>().bounds.min.y;

            if (watchValidY && !breached && y < validY - fallSlack)
            {
                breached = true;
                FellBelowValidY?.Invoke(this);
            }

            if (watchDestroy && y < destroyBelowY)
            {
                Disarm();
                Destroy(gameObject);
            }
        }
    }
}
