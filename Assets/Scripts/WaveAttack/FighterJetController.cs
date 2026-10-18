using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies.Fighter
{
    [DisallowMultipleComponent]
    public class FighterJetController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 15f;
        [SerializeField] private float _stoppingDistance = 0.15f;

        [Tooltip("How smoothly the jet turns to face each waypoint. Higher = snappier turns, lower = wider, softer turns. This is the main knob for making direction changes feel smooth instead of sudden.")]
        [SerializeField] private float _rotationSpeed = 3f;

        [Tooltip("How close (as a fraction of SlowdownDistance) speed is allowed to drop while approaching a point. Keep this well above 0 so the jet doesn't crawl to a near-stop before every waypoint.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _minForwardSpeedFraction = 0.35f;

        [Tooltip("For waypoints where the jet turns to face the target: besides being close enough (StoppingDistance), how close (in degrees) the jet's final rotation must be to the waypoint transform's own rotation before it's considered fully arrived. This is used by the align-in-place phase after the jet reaches the point.")]
        [Range(0.5f, 45f)]
        [SerializeField] private float _arrivalAngleThreshold = 8f;

        [Header("Movement Feel")]
        [SerializeField] private float _acceleration = 20f;
        [SerializeField] private float _deceleration = 25f;

        [Tooltip("Distance from a target point at which the jet starts slowing down.")]
        [SerializeField] private float _slowdownDistance = 5f;

        [Header("Banking (optional)")]
        [Tooltip("Child transform holding the visual mesh.")]
        [SerializeField] private Transform _visualRoot;

        [SerializeField] private float _bankFactor = 1.5f;
        [SerializeField] private float _maxBankAngle = 35f;
        [SerializeField] private float _bankSmoothSpeed = 4f;

        public event Action<FighterJetController> OnDespawned;

        private IJetState _currentState;
        private bool _isActive;
        private FlightPath _currentPath;

        // Root مخصوص حرکت واقعی هواپیما
        private Transform _movementRoot;

        public float MoveSpeed => _moveSpeed;
        public float StoppingDistance => _stoppingDistance;
        public float RotationSpeed => _rotationSpeed;

        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float SlowdownDistance => _slowdownDistance;
        public float MinForwardSpeedFraction => _minForwardSpeedFraction;
        public float ArrivalAngleThreshold => _arrivalAngleThreshold;

        public Transform VisualRoot =>
            _visualRoot != null ? _visualRoot : transform;

        public float BankFactor => _bankFactor;
        public float MaxBankAngle => _maxBankAngle;
        public float BankSmoothSpeed => _bankSmoothSpeed;

        public float CurrentSpeed { get; set; }

        public Transform ExitPoint =>
            _currentPath != null ? _currentPath.ExitPoint : null;

        public List<FlightWaypoint> Waypoints =>
            _currentPath != null ? _currentPath.Waypoints : null;

        public int CurrentWaypointIndex { get; set; }
        public float WaitTimer { get; set; }

        // --------------------------------------------------
        // این Transform مسئول حرکت اصلی روی مسیر است.
        // DroneHover به transform خود جت دست می‌زند،
        // ولی Stateها فقط این Root را حرکت می‌دهند.
        // --------------------------------------------------

        public Transform MovementTransform => _movementRoot;

        private void Awake()
        {
            CreateMovementRoot();
        }

        private void CreateMovementRoot()
        {
            if (_movementRoot != null)
                return;

            GameObject root = new GameObject(
                $"{gameObject.name}_MovementRoot"
            );

            _movementRoot = root.transform;

            _movementRoot.position = transform.position;
            _movementRoot.rotation = transform.rotation;

            // جت زیر Root قرار می‌گیرد.
            // world position و rotation حفظ می‌شود.
            transform.SetParent(_movementRoot, true);
        }

        private void Update()
        {
            if (_isActive)
                _currentState?.Update(this);
        }

        public bool Fly(FlightPath path)
        {
            if (_isActive)
                return false;

            if (path == null || path.StartPoint == null)
            {
                Debug.LogError(
                    $"{name}: Invalid FlightPath or StartPoint.",
                    this
                );

                return false;
            }

            if (_movementRoot == null)
                CreateMovementRoot();

            _currentPath = path;

            // مسیر فقط Root را جابه‌جا می‌کند
            _movementRoot.position = path.StartPoint.position;
            _movementRoot.rotation = path.StartPoint.rotation;

            // خود Jet برای DroneHover در مرکز Root قرار می‌گیرد.
            // DroneHover بعداً localPosition خودش را تغییر می‌دهد.
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            CurrentWaypointIndex = 0;
            WaitTimer = 0f;
            CurrentSpeed = 0f;

            _isActive = true;

            if (path.Waypoints != null &&
                path.Waypoints.Count > 0)
            {
                ChangeState(
                    new MoveToWaypointState()
                );
            }
            else
            {
                ChangeState(
                    new MoveToExitState()
                );
            }

            return true;
        }

        public void ChangeState(IJetState newState)
        {
            _currentState?.Exit(this);

            _currentState = newState;

            _currentState?.Enter(this);
        }

        public void Despawn()
        {
            if (!_isActive)
                return;

            _isActive = false;

            OnDespawned?.Invoke(this);

            if (_movementRoot != null)
            {
                Destroy(_movementRoot.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}