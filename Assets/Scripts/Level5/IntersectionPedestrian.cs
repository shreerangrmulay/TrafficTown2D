using System;
using UnityEngine;

namespace TrafficTown2D.Level5
{
    public enum PedestrianState
    {
        ApproachingCurb,
        WaitingAtCurb,
        Crossing,
        Exiting,
        Finished
    }

    public class IntersectionPedestrian : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 1.6f;
        [SerializeField] private float bobFrequency = 8.5f;
        [SerializeField] private float bobHeight = 0.08f;

        private Vector3 spawnPoint;
        private Vector3 waitPoint;
        private Vector3 crossTargetPoint;
        private Vector3 exitPoint;
        private Transform visualRoot;

        private PedestrianState state = PedestrianState.ApproachingCurb;
        private float waitingTime = 0f;
        private float stepTimer = 0f;

        public PedestrianState State => state;
        public bool IsWaiting => state == PedestrianState.WaitingAtCurb;
        public bool IsCrossing => state == PedestrianState.Crossing;
        public float WaitingTime => waitingTime;

        public event Action<IntersectionPedestrian> CrossedSuccessfully;

        public void InitializePath(Vector3 spawn, Vector3 wait, Vector3 crossTarget, Vector3 exit, Transform visual)
        {
            spawnPoint = spawn;
            waitPoint = wait;
            crossTargetPoint = crossTarget;
            exitPoint = exit;
            visualRoot = visual;

            transform.position = spawnPoint;
            state = PedestrianState.ApproachingCurb;
            waitingTime = 0f;
            stepTimer = 0f;

            OrientTowards(waitPoint);
        }

        // Backward compatibility
        public void Initialize(Vector3 start, Vector3 target, Vector3 exit, Transform visual)
        {
            InitializePath(start, start, target, exit, visual);
            state = PedestrianState.WaitingAtCurb;
        }

        private void Update()
        {
            switch (state)
            {
                case PedestrianState.ApproachingCurb:
                    MoveTowardsPoint(waitPoint);
                    if (Vector3.Distance(transform.position, waitPoint) < 0.25f)
                    {
                        state = PedestrianState.WaitingAtCurb;
                        if (visualRoot != null) visualRoot.localPosition = Vector3.zero;
                        OrientTowards(crossTargetPoint);
                    }
                    break;

                case PedestrianState.WaitingAtCurb:
                    waitingTime += Time.deltaTime;
                    if (TrafficPhaseController.Instance != null && TrafficPhaseController.Instance.IsPedestrianWalkActive())
                    {
                        state = PedestrianState.Crossing;
                        OrientTowards(crossTargetPoint);
                    }
                    break;

                case PedestrianState.Crossing:
                    MoveTowardsPoint(crossTargetPoint);
                    if (Vector3.Distance(transform.position, crossTargetPoint) < 0.25f)
                    {
                        state = PedestrianState.Exiting;
                        CrossedSuccessfully?.Invoke(this);
                        OrientTowards(exitPoint);
                    }
                    break;

                case PedestrianState.Exiting:
                    MoveTowardsPoint(exitPoint);
                    if (Vector3.Distance(transform.position, exitPoint) < 0.35f)
                    {
                        state = PedestrianState.Finished;
                        Destroy(gameObject);
                    }
                    break;
            }
        }

        private void MoveTowardsPoint(Vector3 destination)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, walkSpeed * Time.deltaTime);

            // Walking bob animation
            stepTimer += Time.deltaTime * bobFrequency;
            if (visualRoot != null)
            {
                float offsetY = Mathf.Abs(Mathf.Sin(stepTimer)) * bobHeight;
                visualRoot.localPosition = new Vector3(0f, offsetY, 0f);
            }
        }

        private void OrientTowards(Vector3 target)
        {
            Vector3 dir = (target - transform.position).normalized;
            if (dir.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}
