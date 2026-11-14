using UnityEngine;

namespace HoAndTheBells.Player
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController), typeof(AdventurePlayerInput))]
    public sealed class AdventurePlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float sprintSpeed = 6.5f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField, Min(0.1f)] private float gravity = 20f;
        private CharacterController controller;
        private AdventurePlayerInput input;
        private float verticalSpeed;
        public bool IsGrounded => controller != null && controller.isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<AdventurePlayerInput>();
        }

        private void Update() => Simulate(input.Move, input.Sprint, input.JumpPressed, Time.deltaTime);

        // Explicit intent keeps locomotion independent from devices and camera mode.
        public void Simulate(Vector2 move, bool sprint, bool jumpPressed, float deltaTime)
        {
            if (controller == null || !controller.enabled || deltaTime <= 0f) return;
            move = Vector2.ClampMagnitude(move, 1f);
            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            if (jumpPressed && controller.isGrounded)
                verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
            var horizontal = (transform.right * move.x + transform.forward * move.y)
                * (sprint ? sprintSpeed : walkSpeed);
            float verticalDisplacement = verticalSpeed * deltaTime - 0.5f * gravity * deltaTime * deltaTime;
            verticalSpeed = Mathf.Max(verticalSpeed - gravity * deltaTime, -50f);
            var flags = controller.Move(horizontal * deltaTime + Vector3.up * verticalDisplacement);
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            if ((flags & CollisionFlags.Below) != 0 && verticalSpeed < 0f) verticalSpeed = -2f;
        }

        public void SetControlEnabled(bool enabled) => input.SetControlEnabled(enabled);
        private void OnDisable() => verticalSpeed = 0f;
    }
}
