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

        enum LandingDetectMode
        {
            VelocitySettle,
            ContactTimer
        }

        [SerializeField] BlockFactory blockFactory;
        [SerializeField] BuildingSite buildingSite;
        [SerializeField] Transform craneAndCart;
        [SerializeField] float craneLowestPosition = 1.5f;
        [SerializeField] float craneLowerDuration = 1.2f;
        [SerializeField] float craneRaiseDuration = 0.6f;
        [Tooltip("World Y from FloorHangPoint up to CraneAndCart (scene: ~1.965). 0 = measure at Begin.")]
        [SerializeField] float craneDistance;
        [SerializeField] Collider2D dropZone;
        [SerializeField] Transform hang;
        [SerializeField] Transform floorHangPoint;
        [SerializeField] Transform roofHangPoint;
        [SerializeField] GameObject cableHookAndSpreaders;
        [SerializeField] GameObject cableAndHook;
        [SerializeField] float amplitudeDegrees = 20f;
        [SerializeField] float periodSeconds = 5f;
        [SerializeField] float coastDamping = 0.1f;
        [SerializeField] LandingDetectMode landingDetectMode = LandingDetectMode.VelocitySettle;
        [SerializeField] float settleLinearSpeed = 0.15f;
        [SerializeField] float settleAngularSpeed = 20f;
        [SerializeField] float settleDwellTime = 0.2f;
        [SerializeField] float contactSettleDelay = 0.25f;
        [SerializeField] float settleTimeout = 2f;

        Phase phase;
        float coastAngle;
        float coastAngularVelocity;
        float startTime;
        float craneLowerSpeed;
        float craneRaiseSpeed;
        float craneRaiseTargetY;
        float measuredCraneDistance;
        float dropGap;
        Block heldBlock;
        Rigidbody2D fallingBody;
        float fallStartTime;
        float settleQuietTime;
        bool contactSettling;
        float contactSettleAt;
        ContactFilter2D anyContactFilter;

        void Awake()
        {
            anyContactFilter.NoFilter();
        }

        public void Begin()
        {
            measuredCraneDistance = craneDistance > 0f ? craneDistance : MeasureCraneDistance();
            float distance = Mathf.Abs(craneAndCart.localPosition.y - craneLowestPosition);
            craneLowerSpeed = craneLowerDuration > 0f ? distance / craneLowerDuration : distance;
            phase = Phase.Lowering;
        }

        float MeasureCraneDistance()
        {
            Quaternion rotation = hang.localRotation;
            hang.localRotation = Quaternion.identity;
            float distance = craneAndCart.position.y - floorHangPoint.position.y;
            hang.localRotation = rotation;
            return distance;
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

            // Air gap from FloorHangPoint down to DropZone at the first swing pose.
            dropGap = floorHangPoint.position.y - dropZone.bounds.max.y;
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

        void BeginRaise()
        {
            Transform topHang = buildingSite.Top.transform.Find("Hang");
            float targetWorldY = topHang.position.y + measuredCraneDistance + dropGap;
            if (craneAndCart.parent != null)
                craneRaiseTargetY = craneAndCart.parent.InverseTransformPoint(new Vector3(craneAndCart.position.x, targetWorldY, craneAndCart.position.z)).y;
            else
                craneRaiseTargetY = targetWorldY;

            float distance = Mathf.Abs(craneRaiseTargetY - craneAndCart.localPosition.y);
            craneRaiseSpeed = craneRaiseDuration > 0f ? distance / craneRaiseDuration : distance;
            phase = Phase.Raising;
        }

        void Swing()
        {
            float angularVelocity = UpdateHangSwing();

            if (fallingBody != null)
            {
                WatchFalling();
                return;
            }

            if (heldBlock != null && PressedThisFrame())
                Release(angularVelocity);
        }

        void WatchFalling()
        {
            if (Time.time - fallStartTime >= settleTimeout)
            {
                ResolveLanding();
                return;
            }

            switch (landingDetectMode)
            {
                case LandingDetectMode.VelocitySettle:
                    WatchVelocitySettle();
                    break;
                case LandingDetectMode.ContactTimer:
                    WatchContactTimer();
                    break;
            }
        }

        void WatchVelocitySettle()
        {
            bool quiet = fallingBody.linearVelocity.sqrMagnitude <= settleLinearSpeed * settleLinearSpeed
                && Mathf.Abs(fallingBody.angularVelocity) <= settleAngularSpeed;

            if (quiet)
            {
                settleQuietTime += Time.deltaTime;
                if (settleQuietTime >= settleDwellTime)
                    ResolveLanding();
            }
            else
            {
                settleQuietTime = 0f;
            }
        }

        void WatchContactTimer()
        {
            if (!contactSettling)
            {
                Collider2D falling = fallingBody.GetComponent<Collider2D>();
                if (falling.IsTouching(anyContactFilter))
                {
                    contactSettling = true;
                    contactSettleAt = Time.time + contactSettleDelay;
                }

                return;
            }

            if (Time.time >= contactSettleAt)
                ResolveLanding();
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
            fallStartTime = Time.time;
            settleQuietTime = 0f;
            contactSettling = false;
            ShowCable();
        }

        void ResolveLanding()
        {
            Block block = fallingBody.GetComponent<Block>();
            fallingBody = null;
            settleQuietTime = 0f;
            contactSettling = false;

            int floorsBefore = buildingSite.Status.Floors;
            buildingSite.Receive(block);
            if (buildingSite.GameOver)
            {
                BeginCoast();
                return;
            }

            if (buildingSite.Status.Floors > floorsBefore)
                BeginRaise();
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
