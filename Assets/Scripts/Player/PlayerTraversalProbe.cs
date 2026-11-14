using UnityEngine;

namespace HoAndTheBells.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerTraversalProbe : MonoBehaviour
    {
        [SerializeField] private LayerMask solidMask = ~0;
        [SerializeField, Range(0f, 30f)] private float wallTiltLimit = 15f;
        [SerializeField, Range(0.3f, 1f)] private float mantleHeightRatio = 0.85f;
        private CharacterController capsule;
        private void Awake() => capsule = GetComponent<CharacterController>();
        public float Radius => capsule.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        public float Height => Mathf.Max(capsule.height * Mathf.Abs(transform.lossyScale.y), 2f * Radius);
        public float Skin => capsule.skinWidth * Mathf.Max(Mathf.Abs(transform.lossyScale.y), 1f);
        public Vector3 Feet => transform.TransformPoint(capsule.center) - Vector3.up * (Height * 0.5f);
        private bool IsSelf(Collider other) => other == capsule || other.transform.IsChildOf(transform);
        private bool Cast(Vector3 origin, Vector3 direction, float distance, out RaycastHit closest)
        {
            closest = default;
            float nearest = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(origin, direction, distance, solidMask, QueryTriggerInteraction.Ignore))
            {
                if (IsSelf(hit.collider) || hit.distance >= nearest) continue;
                closest = hit; nearest = hit.distance;
            }
            return nearest < float.PositiveInfinity;
        }
        public bool Wall(Vector3 direction, bool markedOnly, out RaycastHit hit)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (!Cast(Feet + Vector3.up * Height * 0.5f, direction, Radius * 2f + Skin, out hit)) return false;
            if (Mathf.Abs(hit.normal.y) > Mathf.Sin(wallTiltLimit * Mathf.Deg2Rad)) return false;
            if (hit.rigidbody != null) return false; // Moving surfaces are deliberately unsupported.
            if (Vector3.Dot(-direction, hit.normal) < 0.7f) return false;
            var marker = hit.collider.GetComponentInParent<ClimbableSurface>();
            return !markedOnly || (marker != null && marker.isActiveAndEnabled);
        }
        public bool Mantle(Vector3 direction, out Vector3 lift, out Vector3 destination)
        {
            lift = destination = default;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            float step = capsule.stepOffset * Mathf.Abs(transform.lossyScale.y);
            float maxRise = Height * mantleHeightRatio;
            if (!Cast(Feet + Vector3.up * (step + Skin + 0.02f), direction, Radius * 2f + Skin, out var wall)) return false;
            if (wall.rigidbody != null || Mathf.Abs(wall.normal.y) > Mathf.Sin(wallTiltLimit * Mathf.Deg2Rad)) return false;
            var sample = Feet + direction * (wall.distance + Radius + Skin * 2f);
            if (!Cast(sample + Vector3.up * (maxRise + Skin), Vector3.down, maxRise - step, out var top)) return false;
            if (top.rigidbody != null || Vector3.Angle(top.normal, Vector3.up) > capsule.slopeLimit) return false;
            float rise = top.point.y - Feet.y;
            if (rise <= step + Skin || rise > maxRise) return false;
            // Require support under the capsule footprint, not just one point on a thin rail.
            var tangent = Vector3.Cross(Vector3.up, direction);
            foreach (var offset in new[] { direction, -direction, tangent, -tangent })
            {
                if (!Cast(top.point + offset * Radius * 0.85f + Vector3.up * 0.12f, Vector3.down, 0.24f, out var support)
                    || support.rigidbody != null || Vector3.Angle(support.normal, Vector3.up) > capsule.slopeLimit) return false;
            }
            destination = transform.position + (top.point - Feet) + Vector3.up * (Skin + 0.015f);
            lift = new Vector3(transform.position.x, destination.y, transform.position.z);
            return ClearAt(destination) && ClearPath(transform.position, lift) && ClearPath(lift, destination);
        }
        private void CapsuleAt(Vector3 root, out Vector3 bottom, out Vector3 top, out float radius)
        {
            radius = Mathf.Max(0.01f, Radius - Skin - 0.005f);
            var center = root + transform.TransformPoint(capsule.center) - transform.position;
            float segment = Height * 0.5f - Radius;
            bottom = center - Vector3.up * segment;
            top = center + Vector3.up * segment;
        }
        public bool ClearAt(Vector3 root)
        {
            CapsuleAt(root, out var bottom, out var top, out var radius);
            foreach (var hit in Physics.OverlapCapsule(bottom, top, radius, solidMask, QueryTriggerInteraction.Ignore))
                if (!IsSelf(hit)) return false;
            return true;
        }
        public bool ClearPath(Vector3 from, Vector3 to)
        {
            CapsuleAt(from, out var bottom, out var top, out var radius);
            var delta = to - from;
            if (delta.sqrMagnitude < 0.000001f) return true;
            foreach (var hit in Physics.CapsuleCastAll(bottom, top, radius, delta.normalized, delta.magnitude, solidMask, QueryTriggerInteraction.Ignore))
                if (!IsSelf(hit.collider)) return false;
            return ClearAt(to);
        }
    }
}
