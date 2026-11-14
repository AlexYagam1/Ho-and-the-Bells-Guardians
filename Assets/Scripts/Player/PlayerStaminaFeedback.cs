using UnityEngine;
using Unity.Cinemachine;

namespace HoAndTheBells.Player
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(PlayerStamina), typeof(AdventurePlayerMotor), typeof(AdventurePlayerInput))]
    public sealed class PlayerStaminaFeedback : MonoBehaviour
    {
        [SerializeField] private StaminaAuraGraphic aura;
        [SerializeField] private CinemachineBasicMultiChannelPerlin noise;
        [SerializeField, Min(151f)] private float visualMaximum = 200f;
        [SerializeField, Range(0f, 1f)] private float auraOpacity = 0.42f;
        [SerializeField, Range(0f, 1f)] private float shakeIntensity = 1f;
        [SerializeField, Range(0f, 1f)] private float pulseIntensity = 0.55f;
        [SerializeField, Min(0f)] private float pulseFrequency = 1.5f;
        [SerializeField, Min(0.01f)] private float orangeThreshold = 50f;
        [SerializeField, Min(0.01f)] private float redThreshold = 15f;
        [SerializeField] private Color green = new Color(0.2f, 1f, 0.3f);
        [SerializeField] private Color orange = new Color(1f, 0.4f, 0.03f);
        [SerializeField] private Color red = new Color(1f, 0.04f, 0.03f);
        private PlayerStamina stamina;
        private AdventurePlayerMotor motor;
        private AdventurePlayerInput input;
        private float visibility, extent, extentFrom, extentTo, extentTime, pulseTime, exhaustionRemaining;
        public float VisualMaximum => visualMaximum;
        private void Awake()
        {
            stamina = GetComponent<PlayerStamina>(); motor = GetComponent<AdventurePlayerMotor>(); input = GetComponent<AdventurePlayerInput>();
            extent = extentFrom = extentTo = Mathf.Clamp01(stamina.Maximum / visualMaximum);
            extentTime = 0.25f;
        }
        private void OnEnable() { if (stamina != null) stamina.Exhausted += OnExhausted; }
        private void OnExhausted()
        {
            if (motor.State == AdventurePlayerMotor.MovementState.Climbing)
            { exhaustionRemaining = 0.35f; visibility = 1f; }
        }
        public Color EvaluateColor(float reserve)
        {
            if (reserve >= visualMaximum) return Color.white;
            if (reserve >= 150f) return Color.Lerp(green, Color.white, Mathf.InverseLerp(150f, visualMaximum, reserve));
            if (reserve >= 100f) return green;
            if (reserve >= orangeThreshold) return Color.Lerp(orange, green, Mathf.InverseLerp(orangeThreshold, 100f, reserve));
            return Color.Lerp(red, orange, Mathf.InverseLerp(redThreshold, orangeThreshold, reserve));
        }
        private void Update()
        {
            if (!input.HasControl) { ClearFeedback(); return; }
            AdvanceVisuals(Time.deltaTime);
        }
        public void AdvanceVisuals(float dt)
        {
            if (dt <= 0f) return;
            float targetExtent = Mathf.Clamp01(stamina.Maximum / visualMaximum);
            if (!Mathf.Approximately(targetExtent, extentTo))
            { extentFrom = extent; extentTo = targetExtent; extentTime = 0f; }
            extentTime = Mathf.Min(0.25f, extentTime + dt);
            extent = Mathf.Lerp(extentFrom, extentTo, Mathf.SmoothStep(0f, 1f, extentTime / 0.25f));
            bool climbing = motor.State == AdventurePlayerMotor.MovementState.Climbing;
            bool exhausted = exhaustionRemaining > 0f;
            if (exhausted)
            {
                exhaustionRemaining = Mathf.Max(0f, exhaustionRemaining - dt);
                visibility = exhaustionRemaining / 0.35f;
            }
            else visibility = Mathf.MoveTowards(visibility, climbing ? 1f : 0f, dt / (climbing ? 0.15f : 0.2f));
            pulseTime += dt;
            float pulse = stamina.Current <= redThreshold
                ? 1f - pulseIntensity * (0.5f + 0.5f * Mathf.Cos(pulseTime * pulseFrequency * Mathf.PI * 2f)) : 1f;
            var tint = EvaluateColor(stamina.Current);
            tint.a = auraOpacity * visibility * pulse;
            if (aura != null) aura.SetAppearance(extent, tint);
            float fatigue = 1f - Mathf.Clamp01(stamina.Current / orangeThreshold);
            if (noise != null) noise.AmplitudeGain = fatigue * fatigue * shakeIntensity * visibility;
        }
        public void ClearFeedback()
        {
            visibility = exhaustionRemaining = 0f;
            if (noise != null) noise.AmplitudeGain = 0f;
            if (aura != null) aura.SetAppearance(extent, Color.clear);
        }
        private void OnDisable()
        {
            if (stamina != null) stamina.Exhausted -= OnExhausted;
            ClearFeedback();
        }
    }
}
