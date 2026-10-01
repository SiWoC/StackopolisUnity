using UnityEngine;
using UnityEngine.InputSystem;

namespace SiWoC.Stackopolis.BuildTower
{
    public class CraneDriver : MonoBehaviour
    {
        enum Phase
        {
            Idle,
            Lowering,
            Swinging,
            Coasting,
            Raising
        }

        [SerializeField] BlockFactory blockFactory;
        [SerializeField] BuildingSite buildingSite;
        [SerializeField] Transform craneAndCart;
        [SerializeField] float craneLowestPosition = 1.5f;
        [SerializeField] float craneLowerDuration = 1.2f;
        [SerializeField] float craneRaiseDuration = 0.6f;
        [SerializeField] Transform hang;
        [SerializeField] Transform floorHangPoint;
        [SerializeField] Transform roofHangPoint;
        [SerializeField] GameObject cableHookAndSpreaders;
        [SerializeField] GameObject cableAndHook;
        [SerializeField] float amplitudeDegrees = 20f;
        [SerializeField] float periodSeconds = 5f;
        [SerializeField] float coastDamping = 0.1f;

        Phase phase;
        float coastAngle;
        float coastAngularVelocity;
        float startTime;
        float craneLowerSpeed;
        float craneRaiseSpeed;
        float craneRaiseTargetY;
        Block heldBlock;
        Rigidbody2D fallingBody;
        bool fallingWasAwake;

        public void Begin()
        {
            float distance = Mathf.Abs(craneAndCart.localPosition.y - craneLowestPosition);
            craneLowerSpeed = craneLowerDuration > 0f ? distance / craneLowerDuration : distance;
            phase = Phase.Lowering;
        }

        void Update()
        {
            switch (phase)
            {
                case Phase.Lowering:
                    LowerCrane();
                    break;
                case Phase.Swinging:
                    Swing();
                    break;
                case Phase.Coasting:
                    Coast();
                    break;
                case Phase.Raising:
                    RaiseCrane();
                    break;
            }
        }

        void LowerCrane()
        {
            Vector3 position = craneAndCart.localPosition;
            position.y = Mathf.MoveTowards(position.y, craneLowestPosition, craneLowerSpeed * Time.deltaTime);
            craneAndCart.localPosition = position;

            if (!Mathf.Approximately(position.y, craneLowestPosition))
                return;

            startTime = Time.time;
            Attach(blockFactory.CreateBottom());
            phase = Phase.Swinging;
        }

        void RaiseCrane()
        {
            UpdateHangSwing();

            Vector3 position = craneAndCart.localPosition;
            position.y = Mathf.MoveTowards(position.y, craneRaiseTargetY, craneRaiseSpeed * Time.deltaTime);
            craneAndCart.localPosition = position;

            if (!Mathf.Approximately(position.y, craneRaiseTargetY))
                return;

            Attach(blockFactory.CreateMiddle());
            phase = Phase.Swinging;
        }

        void BeginRaise(Block placed)
        {
            float step = placed.GetComponent<Collider2D>().bounds.size.y;
            craneRaiseTargetY = craneAndCart.localPosition.y + step;
            craneRaiseSpeed = craneRaiseDuration > 0f ? step / craneRaiseDuration : step;
            phase = Phase.Raising;
        }

        void Swing()
        {
            float angularVelocity = UpdateHangSwing();

            if (fallingBody != null)
            {
                if (!fallingBody.IsSleeping())
                    fallingWasAwake = true;
                else if (fallingWasAwake)
                    ResolveLanding();

                return;
            }

            if (heldBlock != null && PressedThisFrame())
                Release(angularVelocity);
        }

        float UpdateHangSwing()
        {
            float elapsed = Time.time - startTime;
            float angularVelocity = SwingAngularVelocity(elapsed);
            float angle = amplitudeDegrees * Mathf.Sin((Mathf.PI * 2f) * elapsed / periodSeconds);
            hang.localRotation = Quaternion.Euler(0f, 0f, angle);
            return angularVelocity;
        }

        float SwingAngularVelocity(float elapsed)
        {
            float omega = (Mathf.PI * 2f) / periodSeconds;
            return amplitudeDegrees * omega * Mathf.Cos(omega * elapsed);
        }

        void BeginCoast()
        {
            float elapsed = Time.time - startTime;
            coastAngle = amplitudeDegrees * Mathf.Sin((Mathf.PI * 2f) * elapsed / periodSeconds);
            coastAngularVelocity = SwingAngularVelocity(elapsed);
            phase = Phase.Coasting;
        }

        void Coast()
        {
            float omega = (Mathf.PI * 2f) / periodSeconds;
            float drag = 2f * Mathf.Max(0f, coastDamping) * omega;
            float dt = Time.deltaTime;
            coastAngularVelocity += (-omega * omega * coastAngle - drag * coastAngularVelocity) * dt;
            coastAngle += coastAngularVelocity * dt;

            if (Mathf.Abs(coastAngle) < 0.5f && Mathf.Abs(coastAngularVelocity) < 1f)
            {
                coastAngle = 0f;
                coastAngularVelocity = 0f;
                phase = Phase.Idle;
            }

            hang.localRotation = Quaternion.Euler(0f, 0f, coastAngle);
        }

        void Release(float angularVelocity)
        {
            Rigidbody2D body = heldBlock.GetComponent<Rigidbody2D>();
            heldBlock.transform.SetParent(null, true);

            body.bodyType = RigidbodyType2D.Dynamic;
            float omega = angularVelocity * Mathf.Deg2Rad;
            Vector2 radius = body.worldCenterOfMass - (Vector2)hang.position;
            body.linearVelocity = omega * new Vector2(-radius.y, radius.x);
            body.angularVelocity = angularVelocity;

            heldBlock = null;
            fallingBody = body;
            fallingWasAwake = false;
            ShowCable();
        }

        void ResolveLanding()
        {
            Block block = fallingBody.GetComponent<Block>();
            fallingBody = null;
            fallingWasAwake = false;

            int floorsBefore = buildingSite.Status.Floors;
            buildingSite.Receive(block);
            if (buildingSite.GameOver)
            {
                BeginCoast();
                return;
            }

            if (buildingSite.Status.Floors > floorsBefore)
                BeginRaise(block);
            else
                Attach(blockFactory.CreateMiddle());
        }

        void Attach(Block block)
        {
            heldBlock = block;
            ShowCable();

            Transform hangPoint = block.type == BlockType.Roof ? roofHangPoint : floorHangPoint;
            Transform blockHang = block.transform.Find("Hang");
            block.transform.SetParent(hangPoint, false);
            block.transform.localRotation = Quaternion.identity;
            block.transform.localPosition = -Vector3.Scale(block.transform.localScale, blockHang.localPosition);
        }

        void ShowCable()
        {
            bool roof = heldBlock == null || heldBlock.type == BlockType.Roof;
            cableHookAndSpreaders.SetActive(!roof);
            cableAndHook.SetActive(roof);
        }

        bool PressedThisFrame()
        {
            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.wasPressedThisFrame)
                        return true;
                }
            }

            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }
    }
}
