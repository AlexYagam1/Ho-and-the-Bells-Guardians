using UnityEngine;

namespace HoAndTheBells.Player
{
    [DefaultExecutionOrder(-150)]
    [RequireComponent(typeof(AdventurePlayerInput))]
    public sealed class AdventurePlayerLook : MonoBehaviour
    {
        [SerializeField] private Transform eyeTarget;
        [SerializeField] private Transform bodyTarget;
        [SerializeField, Min(0f)] private float sensitivity = 0.12f;
        [SerializeField, Range(1f, 89f)] private float pitchLimit = 80f;
        private AdventurePlayerInput input;
        private float pitch;
        public Transform EyeTarget => eyeTarget;
        public Transform BodyTarget => bodyTarget;

        private void Awake() => input = GetComponent<AdventurePlayerInput>();
        private void Update()
        {
            if (eyeTarget == null || !input.HasControl || Time.timeScale <= 0f) return;
            ApplyLook(input.Look);
        }

        public void ApplyLook(Vector2 delta)
        {
            if (eyeTarget == null) return;
            transform.Rotate(0f, delta.x * sensitivity, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -pitchLimit, pitchLimit);
            eyeTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
