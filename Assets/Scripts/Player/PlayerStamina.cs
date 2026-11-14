using System;
using UnityEngine;

namespace HoAndTheBells.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerStamina : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximum = 100f;
        [SerializeField, Min(0f)] private float recoveryPerSecond = 40f;
        [SerializeField, Min(0f)] private float recoveryDelay = 0.3f;
        private float current;
        private float groundedRecoveryTime;
        public float Current => current;
        public float Maximum => maximum;
        public float Normalized => current / maximum;
        public event Action<float, float> Changed;
        public event Action Exhausted;

        private void Awake() => current = maximum;
        public void SetMaximumStamina(float value)
        {
            if (!float.IsFinite(value) || value <= 0f) throw new ArgumentOutOfRangeException(nameof(value));
            maximum = value;
            SetCurrent(Mathf.Min(current, maximum));
        }
        public void Restore(float amount)
        {
            if (!float.IsFinite(amount) || amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            SetCurrent(Mathf.Min(maximum, current + amount));
        }
        public bool TrySpend(float amount)
        {
            if (!float.IsFinite(amount) || amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            if (current < amount) return false;
            Consume(amount);
            return true;
        }
        public void Consume(float amount)
        {
            if (!float.IsFinite(amount) || amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            groundedRecoveryTime = 0f;
            SetCurrent(Mathf.Max(0f, current - amount));
        }
        public void TickRecovery(bool grounded, float dt)
        {
            if (dt <= 0f) return;
            if (!grounded) { groundedRecoveryTime = 0f; return; }
            float before = groundedRecoveryTime;
            groundedRecoveryTime += dt;
            float recoveryTime = Mathf.Max(0f, groundedRecoveryTime - recoveryDelay)
                - Mathf.Max(0f, before - recoveryDelay);
            if (recoveryTime > 0f && current < maximum) Restore(recoveryTime * recoveryPerSecond);
        }
        private void SetCurrent(float value)
        {
            float previous = current;
            current = value;
            Changed?.Invoke(current, maximum);
            if (previous > 0f && current <= 0f) Exhausted?.Invoke();
        }
    }
}
