using UnityEngine;

namespace HoAndTheBells.Player
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController), typeof(AdventurePlayerInput))]
    [RequireComponent(typeof(PlayerStamina), typeof(PlayerTraversalProbe))]
    public sealed class AdventurePlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float sprintSpeed = 6.5f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField, Min(0.1f)] private float gravity = 20f;
        [SerializeField, Min(0f)] private float airJumpHeight = 1.2f;
        [SerializeField, Min(0f)] private float climbSpeed = 1.2f;
        [SerializeField, Min(0f)] private float lateralClimbSpeed = 1f;
        [SerializeField, Min(0f)] private float descendSpeed = 1.5f;
        [SerializeField, Min(0f)] private float climbDrain = 25f;
        [SerializeField, Min(0f)] private float holdDrain = 10f;
        [SerializeField, Min(0f)] private float wallJumpCost = 15f;
        [SerializeField, Min(0f)] private float wallJumpPush = 3f;
        [SerializeField, Min(0.01f)] private float pushDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float regrabDelay = 0.25f;
        [SerializeField, Min(0.05f)] private float mantleDuration = 0.3f;
        public enum MovementState { Locomotion, Climbing, Mantling }
        public MovementState State { get; private set; }
        public bool AirJumpAvailable { get; private set; } = true;
        public bool ExhaustionLocked { get; private set; }
        public event System.Action<MovementState> StateChanged;
        private CharacterController controller;
        private AdventurePlayerInput input;
        private PlayerStamina stamina;
        private PlayerTraversalProbe probe;
        private float verticalSpeed;
        private Vector3 wallNormal, mantleStart, mantleLift, mantleEnd, push;
        private float mantleTime, regrabTimer, pushTime;
        private bool requireForwardRelease;
        public bool IsGrounded => controller != null && controller.isGrounded && verticalSpeed <= 0f && State == MovementState.Locomotion;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<AdventurePlayerInput>();
            stamina = GetComponent<PlayerStamina>();
            probe = GetComponent<PlayerTraversalProbe>();
        }

        private void Update() => Simulate(input.Move, input.Sprint, input.JumpPressed, Time.deltaTime, input.DropPressed, input.HasControl);

        // Explicit intent keeps locomotion independent from devices and camera mode.
        public void Simulate(Vector2 move, bool sprint, bool jumpPressed, float deltaTime, bool dropPressed = false, bool controlEnabled = true)
        {
            if (controller == null || !controller.enabled || deltaTime <= 0f) return;
            move = Vector2.ClampMagnitude(move, 1f);
            regrabTimer = Mathf.Max(0f, regrabTimer - deltaTime);
            if (move.y <= 0f) requireForwardRelease = false;
            if (!controlEnabled)
            {
                move = Vector2.zero; jumpPressed = false;
                if (State != MovementState.Locomotion) Detach();
            }
            bool grounded = IsGrounded;
            if (grounded) { AirJumpAvailable = true; ExhaustionLocked = false; verticalSpeed = -2f; }
            stamina.TickRecovery(grounded, deltaTime);
            if (State == MovementState.Mantling) { AdvanceMantle(deltaTime); return; }

            if (State == MovementState.Climbing)
            {
                if (dropPressed) { Detach(); MoveNormally(Vector2.zero, false, deltaTime); return; }
                if (jumpPressed)
                {
                    bool canJump = stamina.TrySpend(wallJumpCost);
                    var away = wallNormal;
                    Detach();
                    if (stamina.Current <= 0f) ExhaustionLocked = true;
                    if (canJump)
                    {
                        verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
                        push = away * wallJumpPush; pushTime = pushDuration;
                    }
                    MoveNormally(Vector2.zero, false, deltaTime); return;
                }
                if (move.y > 0f && TryMantle(-wallNormal)) { AdvanceMantle(deltaTime); return; }
                if (stamina.Current <= 0f) { ExhaustionLocked = true; Detach(); MoveNormally(Vector2.zero, false, deltaTime); return; }
                if (!probe.Wall(-wallNormal, false, out var contact) || Vector3.Angle(contact.normal, wallNormal) > 30f)
                { Detach(); MoveNormally(Vector2.zero, false, deltaTime); return; }
                wallNormal = contact.normal;
                stamina.Consume((move.y > 0f ? climbDrain : holdDrain) * deltaTime);
                var tangent = Vector3.Cross(Vector3.up, wallNormal);
                var climbUp = Vector3.ProjectOnPlane(Vector3.up, wallNormal).normalized;
                var velocity = -tangent.normalized * (move.x * lateralClimbSpeed)
                    + climbUp * (move.y * (move.y >= 0f ? climbSpeed : descendSpeed));
                controller.Move((velocity - wallNormal * 0.5f) * deltaTime);
                if (stamina.Current <= 0f)
                {
                    ExhaustionLocked = true;
                    if (move.y > 0f && TryMantle(-wallNormal)) { AdvanceMantle(deltaTime); return; }
                    Detach(); MoveNormally(Vector2.zero, false, deltaTime); return;
                }
                if (move.y < 0f && controller.isGrounded) Detach();
                return;
            }

            if (dropPressed) { regrabTimer = regrabDelay; requireForwardRelease = true; }
            bool canGrab = controlEnabled && move.y > 0f && !requireForwardRelease && regrabTimer <= 0f;
            var approach = transform.right * move.x + transform.forward * move.y;
            if (jumpPressed)
            {
                if (grounded) verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
                else if (AirJumpAvailable)
                { verticalSpeed = Mathf.Sqrt(2f * gravity * airJumpHeight); AirJumpAvailable = false; }
            }
            else if (canGrab)
            {
                if (TryMantle(approach)) { AdvanceMantle(deltaTime); return; }
                if (TryGrab(approach, deltaTime))
                {
                    if (ExhaustionLocked) MoveNormally(Vector2.zero, false, deltaTime);
                    return;
                }
            }
            MoveNormally(move, sprint, deltaTime);
            if (canGrab && !IsGrounded) TryGrab(approach, deltaTime);
        }

        private bool TryGrab(Vector3 approach, float deltaTime)
        {
            if (ExhaustionLocked || stamina.Current <= 0f || !probe.Wall(approach, false, out var wall))
                return false;

            wallNormal = wall.normal;
            verticalSpeed = 0f;
            SetState(MovementState.Climbing);
            stamina.Consume(climbDrain * deltaTime);
            if (stamina.Current <= 0f)
            {
                ExhaustionLocked = true;
                Detach();
            }
            return true;
        }

        private void MoveNormally(Vector2 move, bool sprint, float deltaTime)
        {
            var horizontal = (transform.right * move.x + transform.forward * move.y)
                * (sprint ? sprintSpeed : walkSpeed);
            if (pushTime > 0f)
            {
                horizontal += push * (pushTime / pushDuration);
                pushTime = Mathf.Max(0f, pushTime - deltaTime);
            }
            float verticalDisplacement = verticalSpeed * deltaTime - 0.5f * gravity * deltaTime * deltaTime;
            verticalSpeed = Mathf.Max(verticalSpeed - gravity * deltaTime, -50f);
            var flags = controller.Move(horizontal * deltaTime + Vector3.up * verticalDisplacement);
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            if ((flags & CollisionFlags.Below) != 0 && verticalSpeed < 0f) verticalSpeed = -2f;
        }

        private bool TryMantle(Vector3 direction)
        {
            if (!probe.Mantle(direction, out mantleLift, out mantleEnd)) return false;
            mantleStart = transform.position; mantleTime = 0f; verticalSpeed = 0f;
            SetState(MovementState.Mantling); return true;
        }
        private void AdvanceMantle(float dt)
        {
            float halfway = mantleDuration * 0.5f;
            float previousTime = mantleTime;
            mantleTime += dt;
            if (previousTime < halfway && mantleTime > halfway && !MoveMantleSegment(mantleLift)) return;

            float t = Mathf.Clamp01(mantleTime / mantleDuration);
            Vector3 target = t < 0.5f
                ? Vector3.Lerp(mantleStart, mantleLift, Mathf.SmoothStep(0f, 1f, t * 2f))
                : Vector3.Lerp(mantleLift, mantleEnd, Mathf.SmoothStep(0f, 1f, (t - 0.5f) * 2f));
            if (!MoveMantleSegment(target)) return;
            if (t >= 1f)
            {
                var flags = controller.Move(Vector3.down * (probe.Skin + 0.05f));
                if ((flags & CollisionFlags.Below) == 0)
                {
                    Detach();
                    return;
                }
                AirJumpAvailable = true; verticalSpeed = -2f;
                SetState(MovementState.Locomotion);
            }
        }

        private bool MoveMantleSegment(Vector3 target)
        {
            if (probe.ClearPath(transform.position, target))
            {
                controller.Move(target - transform.position);
                if (Vector3.Distance(target, transform.position) <= probe.Skin + 0.025f) return true;
            }
            Detach();
            return false;
        }
        private void SetState(MovementState value)
        {
            if (State == value) return;
            State = value; StateChanged?.Invoke(value);
        }
        private void Detach()
        {
            verticalSpeed = 0f; regrabTimer = regrabDelay; requireForwardRelease = true;
            SetState(MovementState.Locomotion);
        }
        public void SetControlEnabled(bool enabled)
        {
            input.SetControlEnabled(enabled);
            if (!enabled && State != MovementState.Locomotion) Detach();
        }
        private void OnDisable()
        {
            verticalSpeed = 0f; pushTime = 0f;
            SetState(MovementState.Locomotion);
        }
    }
}
