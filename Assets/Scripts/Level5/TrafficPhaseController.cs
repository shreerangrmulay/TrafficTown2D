using System;
using System.Collections;
using UnityEngine;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level5
{
    public enum IntersectionPhase
    {
        NorthSouthGreen,
        NorthSouthYellow,
        AllRedClearance,
        EastWestGreen,
        EastWestYellow,
        PedestrianWalk,
        PedestrianClearance
    }

    public class TrafficPhaseController : MonoBehaviour
    {
        public static TrafficPhaseController Instance { get; private set; }

        [Header("Traffic Light Components")]
        [SerializeField] private Level3TrafficLight northLight;
        [SerializeField] private Level3TrafficLight southLight;
        [SerializeField] private Level3TrafficLight eastLight;
        [SerializeField] private Level3TrafficLight westLight;

        [Header("Pedestrian Signal Visuals")]
        [SerializeField] private SpriteRenderer[] pedestrianWalkVisuals;
        [SerializeField] private SpriteRenderer[] pedestrianDontWalkVisuals;

        [Header("Phase Timing")]
        [SerializeField] private float yellowDuration = 3.0f;
        [SerializeField] private float allRedDuration = 1.5f;
        [SerializeField] private float pedestrianWalkDuration = 8.0f;
        [SerializeField] private float pedestrianClearanceDuration = 2.5f;

        private IntersectionPhase currentPhase = IntersectionPhase.NorthSouthGreen;
        private IntersectionPhase pendingTargetPhase = IntersectionPhase.EastWestGreen;
        private bool isTransitioning = false;
        private float phaseTimer = 0f;
        private float currentPhaseDuration = 0f;

        public IntersectionPhase CurrentPhase => currentPhase;
        public bool IsTransitioning => isTransitioning;
        public float PhaseTimeRemaining => Mathf.Max(0f, currentPhaseDuration - phaseTimer);
        public float PhaseElapsedTime => phaseTimer;

        public event Action<IntersectionPhase> PhaseChanged;
        public event Action SafePhaseChangeCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Set autonomous = false on all corner lights so they are slave to this phase controller
            if (northLight != null) northLight.SetAutonomous(false);
            if (southLight != null) southLight.SetAutonomous(false);
            if (eastLight != null) eastLight.SetAutonomous(false);
            if (westLight != null) westLight.SetAutonomous(false);

            ApplyPhase(IntersectionPhase.NorthSouthGreen);
        }

        private void Update()
        {
            phaseTimer += Time.deltaTime;

            bool spacePressed = false;
            bool pPressed = false;

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) spacePressed = true;
                if (kb.pKey.wasPressedThisFrame) pPressed = true;
            }

            if (!spacePressed && !pPressed)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.Space)) spacePressed = true;
                    if (Input.GetKeyDown(KeyCode.P)) pPressed = true;
                }
                catch {}
            }

            if (spacePressed)
            {
                RequestPhaseChange();
            }
            else if (pPressed)
            {
                RequestPedestrianPhase();
            }
            else if (!isTransitioning && (currentPhase == IntersectionPhase.NorthSouthGreen || currentPhase == IntersectionPhase.EastWestGreen))
            {
                // Auto-advance if player doesn't manually switch
                if (phaseTimer >= currentPhaseDuration)
                {
                    RequestPhaseChange();
                }
            }
        }

        public bool RequestPhaseChange()
        {
            if (isTransitioning) return false;

            if (currentPhase == IntersectionPhase.NorthSouthGreen)
            {
                StartCoroutine(TransitionRoutine(IntersectionPhase.EastWestGreen));
                return true;
            }
            else if (currentPhase == IntersectionPhase.EastWestGreen)
            {
                StartCoroutine(TransitionRoutine(IntersectionPhase.NorthSouthGreen));
                return true;
            }
            else if (currentPhase == IntersectionPhase.PedestrianWalk)
            {
                // Early advance from pedestrian phase
                StopAllCoroutines();
                StartCoroutine(ClearPedestriansThenVehiclePhase(pendingTargetPhase));
                return true;
            }

            return false;
        }

        public bool RequestPedestrianPhase()
        {
            if (isTransitioning) return false;
            if (currentPhase == IntersectionPhase.PedestrianWalk || currentPhase == IntersectionPhase.PedestrianClearance)
                return false;

            // Remember which vehicle direction to return to afterwards
            pendingTargetPhase = (currentPhase == IntersectionPhase.NorthSouthGreen) 
                ? IntersectionPhase.EastWestGreen 
                : IntersectionPhase.NorthSouthGreen;

            StartCoroutine(TransitionToPedestriansRoutine());
            return true;
        }

        private IEnumerator TransitionRoutine(IntersectionPhase targetGreenPhase)
        {
            isTransitioning = true;

            // 1. Switch active direction to YELLOW
            if (currentPhase == IntersectionPhase.NorthSouthGreen)
            {
                ApplyPhase(IntersectionPhase.NorthSouthYellow);
            }
            else if (currentPhase == IntersectionPhase.EastWestGreen)
            {
                ApplyPhase(IntersectionPhase.EastWestYellow);
            }
            yield return new WaitForSeconds(yellowDuration);

            // 2. ALL RED clearance interval for intersection safety
            ApplyPhase(IntersectionPhase.AllRedClearance);
            yield return new WaitForSeconds(allRedDuration);

            // 3. Grant GREEN to target direction
            ApplyPhase(targetGreenPhase);
            isTransitioning = false;
            SafePhaseChangeCompleted?.Invoke();
        }

        private IEnumerator TransitionToPedestriansRoutine()
        {
            isTransitioning = true;

            // 1. Transition active vehicle phase to YELLOW
            if (currentPhase == IntersectionPhase.NorthSouthGreen)
            {
                ApplyPhase(IntersectionPhase.NorthSouthYellow);
            }
            else if (currentPhase == IntersectionPhase.EastWestGreen)
            {
                ApplyPhase(IntersectionPhase.EastWestYellow);
            }
            yield return new WaitForSeconds(yellowDuration);

            // 2. ALL RED clearance before pedestrians step onto road
            ApplyPhase(IntersectionPhase.AllRedClearance);
            yield return new WaitForSeconds(allRedDuration);

            // 3. PEDESTRIAN WALK
            ApplyPhase(IntersectionPhase.PedestrianWalk);
            isTransitioning = false;

            yield return new WaitForSeconds(pedestrianWalkDuration);

            // 4. Pedestrian clearance buffer (Don't Walk flashing)
            yield return StartCoroutine(ClearPedestriansThenVehiclePhase(pendingTargetPhase));
        }

        private IEnumerator ClearPedestriansThenVehiclePhase(IntersectionPhase targetVehiclePhase)
        {
            isTransitioning = true;
            ApplyPhase(IntersectionPhase.PedestrianClearance);

            // Flash Don't Walk warning
            float flashElapsed = 0f;
            while (flashElapsed < pedestrianClearanceDuration)
            {
                flashElapsed += 0.4f;
                SetPedestrianVisuals(walk: false, dontWalk: (Mathf.FloorToInt(flashElapsed * 3f) % 2 == 0));
                yield return new WaitForSeconds(0.4f);
            }

            // All Red safety buffer
            ApplyPhase(IntersectionPhase.AllRedClearance);
            yield return new WaitForSeconds(allRedDuration);

            // Resume vehicle traffic
            ApplyPhase(targetVehiclePhase);
            isTransitioning = false;
            SafePhaseChangeCompleted?.Invoke();
        }

        private void ApplyPhase(IntersectionPhase phase)
        {
            currentPhase = phase;
            phaseTimer = 0f;

            switch (phase)
            {
                case IntersectionPhase.NorthSouthGreen:
                    currentPhaseDuration = 18.0f;
                    SetNorthSouthSignal(SignalState.Green);
                    SetEastWestSignal(SignalState.Red);
                    SetPedestrianVisuals(walk: false, dontWalk: true);
                    break;

                case IntersectionPhase.NorthSouthYellow:
                    currentPhaseDuration = yellowDuration;
                    SetNorthSouthSignal(SignalState.Yellow);
                    SetEastWestSignal(SignalState.Red);
                    SetPedestrianVisuals(walk: false, dontWalk: true);
                    break;

                case IntersectionPhase.AllRedClearance:
                    currentPhaseDuration = allRedDuration;
                    SetNorthSouthSignal(SignalState.Red);
                    SetEastWestSignal(SignalState.Red);
                    SetPedestrianVisuals(walk: false, dontWalk: true);
                    break;

                case IntersectionPhase.EastWestGreen:
                    currentPhaseDuration = 18.0f;
                    SetNorthSouthSignal(SignalState.Red);
                    SetEastWestSignal(SignalState.Green);
                    SetPedestrianVisuals(walk: false, dontWalk: true);
                    break;

                case IntersectionPhase.EastWestYellow:
                    currentPhaseDuration = yellowDuration;
                    SetNorthSouthSignal(SignalState.Red);
                    SetEastWestSignal(SignalState.Yellow);
                    SetPedestrianVisuals(walk: false, dontWalk: true);
                    break;

                case IntersectionPhase.PedestrianWalk:
                    currentPhaseDuration = pedestrianWalkDuration;
                    SetNorthSouthSignal(SignalState.Red);
                    SetEastWestSignal(SignalState.Red);
                    SetPedestrianVisuals(walk: true, dontWalk: false);
                    break;

                case IntersectionPhase.PedestrianClearance:
                    currentPhaseDuration = pedestrianClearanceDuration;
                    SetNorthSouthSignal(SignalState.Red);
                    SetEastWestSignal(SignalState.Red);
                    SetPedestrianVisuals(walk: false, dontWalk: true);
                    break;
            }

            PhaseChanged?.Invoke(currentPhase);
        }

        private void SetNorthSouthSignal(SignalState state)
        {
            if (northLight != null) northLight.SetState(state);
            if (southLight != null) southLight.SetState(state);
        }

        private void SetEastWestSignal(SignalState state)
        {
            if (eastLight != null) eastLight.SetState(state);
            if (westLight != null) westLight.SetState(state);
        }

        private void SetPedestrianVisuals(bool walk, bool dontWalk)
        {
            Color walkColor = walk ? new Color(0.15f, 0.95f, 0.35f, 1f) : new Color(0.05f, 0.25f, 0.10f, 0.25f);
            Color dontWalkColor = dontWalk ? new Color(1f, 0.20f, 0.20f, 1f) : new Color(0.25f, 0.05f, 0.05f, 0.25f);

            if (pedestrianWalkVisuals != null)
            {
                foreach (var r in pedestrianWalkVisuals)
                {
                    if (r != null) r.color = walkColor;
                }
            }

            if (pedestrianDontWalkVisuals != null)
            {
                foreach (var r in pedestrianDontWalkVisuals)
                {
                    if (r != null) r.color = dontWalkColor;
                }
            }
        }

        public bool IsGreenFor(ApproachDirection dir)
        {
            if (dir == ApproachDirection.North || dir == ApproachDirection.South)
            {
                return currentPhase == IntersectionPhase.NorthSouthGreen;
            }
            else
            {
                return currentPhase == IntersectionPhase.EastWestGreen;
            }
        }

        public bool IsYellowFor(ApproachDirection dir)
        {
            if (dir == ApproachDirection.North || dir == ApproachDirection.South)
            {
                return currentPhase == IntersectionPhase.NorthSouthYellow;
            }
            else
            {
                return currentPhase == IntersectionPhase.EastWestYellow;
            }
        }

        public bool CanVehicleProceed(ApproachDirection dir)
        {
            return IsGreenFor(dir);
        }

        public bool IsPedestrianWalkActive()
        {
            return currentPhase == IntersectionPhase.PedestrianWalk;
        }

        public void ConfigureLights(Level3TrafficLight north, Level3TrafficLight south, Level3TrafficLight east, Level3TrafficLight west)
        {
            northLight = north;
            southLight = south;
            eastLight = east;
            westLight = west;
        }

        public void SetPedestrianVisuals(SpriteRenderer[] walk, SpriteRenderer[] dontWalk)
        {
            pedestrianWalkVisuals = walk;
            pedestrianDontWalkVisuals = dontWalk;
        }
    }
}
