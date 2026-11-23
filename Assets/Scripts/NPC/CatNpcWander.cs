using System;
using UnityEngine;
using UnityEngine.AI;

namespace HoAndTheBells.NPC
{
    [Serializable]
    public class CatStairRoute
    {
        public Transform bottom;
        public Transform top;
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CatNpcWander : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visual;
        [SerializeField] private Transform[] destinations = Array.Empty<Transform>();
        [SerializeField] private CatStairRoute[] stairs = Array.Empty<CatStairRoute>();
        [SerializeField] private float walkSpeed = 1.2f;
        [SerializeField] private float runSpeed = 2.8f;
        [SerializeField, Range(0, 1)] private float runChance = 0.25f;
        [SerializeField] private Vector2 pauseSeconds = new Vector2(2, 5);
        [SerializeField] private int identity;
        [SerializeField] private string currentState;
        [SerializeField] private int[] ascents = new int[2];
        [SerializeField] private int[] descents = new int[2];

        private static readonly int Speed = Animator.StringToHash("Speed");
        private NavMeshAgent agent;
        private NavMeshPath path;
        private System.Random random;
        private float waitUntil;
        private float progressAt;
        private float bestDistance;
        private float visualBaseY;
        private int ordinaryTrips;
        private int nextStair;
        private int activeStair = -1;
        private int stairStage;
        private bool moving;

        public string CurrentState => currentState;
        public int[] Ascents => ascents;
        public int[] Descents => descents;
        public bool SawIdle { get; private set; }
        public bool SawWalk { get; private set; }
        public bool SawRun { get; private set; }
        public int FailedPaths { get; private set; }

        public void Configure(Animator animation, Transform model, Transform[] points, CatStairRoute[] routes, int index)
        {
            animator = animation;
            visual = model;
            destinations = points;
            stairs = routes;
            identity = index;
            ascents = new int[routes.Length];
            descents = new int[routes.Length];
        }

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            path = new NavMeshPath();
            random = new System.Random(713 + identity * 101);
            nextStair = stairs.Length > 0 ? identity % stairs.Length : 0;
            ordinaryTrips = identity % 3;
            ascents = new int[stairs.Length];
            descents = new int[stairs.Length];
            visualBaseY = visual.localPosition.y;
            if (!agent.isOnNavMesh)
            {
                currentState = "Outside NavMesh";
                enabled = false;
                return;
            }
            Wait();
        }

        private void Update()
        {
            if (!agent.isOnNavMesh) return;
            float speed = Vector3.ProjectOnPlane(agent.velocity, Vector3.up).magnitude;
            animator.SetFloat(Speed, speed, 0.12f, Time.deltaTime);
            SawIdle |= speed < 0.05f;
            SawWalk |= speed > 0.5f && speed < 1.5f;
            SawRun |= speed > 2f;
            if (!moving)
            {
                if (Time.time >= waitUntil) ChooseDestination();
                return;
            }
            if (agent.pathPending) return;
            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                AbandonPath();
                return;
            }
            if (agent.remainingDistance <= agent.stoppingDistance + 0.04f)
            {
                Arrive();
                return;
            }
            if (agent.remainingDistance < bestDistance - 0.1f)
            {
                bestDistance = agent.remainingDistance;
                progressAt = Time.time;
            }
            if (Time.time - progressAt > 8f) AbandonPath();
        }

        private void LateUpdate()
        {
            // HeightMesh describes the path; the collider keeps the visual on the actual treads.
            if (Physics.Raycast(transform.position + Vector3.up * 0.4f, Vector3.down,
                out var hit, 0.9f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var local = visual.localPosition;
                // The source locomotion clips dip below their Idle sole height.
                float speed = animator.GetFloat(Speed);
                float clearance = speed <= walkSpeed
                    ? Mathf.Lerp(0, 0.022f, speed / walkSpeed)
                    : Mathf.Lerp(0.022f, 0.052f, Mathf.InverseLerp(walkSpeed, runSpeed, speed));
                local.y = visualBaseY + hit.point.y - transform.position.y + clearance;
                visual.localPosition = local;
            }
        }

        private void ChooseDestination()
        {
            if (stairs.Length > 0 && ordinaryTrips >= 3)
            {
                activeStair = nextStair;
                nextStair = (nextStair + 1) % stairs.Length;
                stairStage = 0;
                ordinaryTrips = 0;
                if (Travel(stairs[activeStair].bottom.position)) return;
                activeStair = -1;
            }
            for (int attempt = 0; attempt < destinations.Length; attempt++)
            {
                var point = destinations[random.Next(destinations.Length)];
                if (Vector3.Distance(transform.position, point.position) < 1.5f) continue;
                if (Travel(point.position))
                {
                    ordinaryTrips++;
                    return;
                }
            }
            Wait();
        }

        private bool Travel(Vector3 destination)
        {
            var filter = new NavMeshQueryFilter {agentTypeID = agent.agentTypeID, areaMask = agent.areaMask};
            if (!NavMesh.SamplePosition(destination, out var hit, 0.4f, filter)
                || !agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                FailedPaths++;
                return false;
            }
            agent.speed = random.NextDouble() < runChance ? runSpeed : walkSpeed;
            agent.isStopped = false;
            if (!agent.SetPath(path))
            {
                FailedPaths++;
                return false;
            }
            moving = true;
            currentState = activeStair >= 0 ? $"Stairs {activeStair + 1}: {stairStage}" : agent.speed == runSpeed ? "Running" : "Walking";
            bestDistance = float.PositiveInfinity;
            progressAt = Time.time;
            return true;
        }

        private void Arrive()
        {
            if (activeStair >= 0)
            {
                if (stairStage == 0)
                {
                    stairStage = 1;
                    if (Travel(stairs[activeStair].top.position)) return;
                }
                else if (stairStage == 1)
                {
                    ascents[activeStair]++;
                    stairStage = 2;
                    if (Travel(stairs[activeStair].bottom.position)) return;
                }
                else descents[activeStair]++;
                activeStair = -1;
            }
            Wait();
        }

        private void Wait()
        {
            moving = false;
            currentState = "Idle";
            agent.isStopped = true;
            agent.ResetPath();
            waitUntil = Time.time + pauseSeconds.x + (float)random.NextDouble() * (pauseSeconds.y - pauseSeconds.x);
        }

        private void AbandonPath()
        {
            FailedPaths++;
            activeStair = -1;
            Wait();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (var point in destinations)
                if (point) Gizmos.DrawWireSphere(point.position, 0.2f);
            Gizmos.color = Color.yellow;
            foreach (var route in stairs)
                if (route.bottom && route.top) Gizmos.DrawLine(route.bottom.position, route.top.position);
            if (agent && moving)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, agent.destination);
            }
        }
    }
}
