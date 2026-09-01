using SuperRacing.Contracts;
using SuperRacing.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using SuperRacing.Audio;

namespace SuperRacing.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour, IVehicleController, IVehicleAudioTelemetrySource
    {
        [Header("Wheel Colliders")]
        [SerializeField] private WheelCollider frontLeftWheel;
        [SerializeField] private WheelCollider frontRightWheel;
        [SerializeField] private WheelCollider rearLeftWheel;
        [SerializeField] private WheelCollider rearRightWheel;

        [Header("Prototype Tuning")]
        [SerializeField, Min(0f)] private float motorTorque = 1000f;
        [SerializeField, Min(0f)] private float brakeTorque = 3000f;
        [SerializeField, Min(1f)] private float maxSpeedKmh = 120f;
        [SerializeField, Min(0f)] private float driveAssistAcceleration = 1f;
        [SerializeField, Range(0f, 60f)] private float maxSteerAngle = 30f;
        [SerializeField, Range(0f, 60f)] private float minSteerAngleAtTopSpeed = 10f;
        [SerializeField, Range(0.1f, 3f)] private float lowSpeedSidewaysGrip = 1.45f;
        [SerializeField, Range(0.1f, 3f)] private float highSpeedSidewaysGrip = 0.85f;

        [Header("Drift / Handbrake")]
        [SerializeField] private bool driftEnabled = true;
        [SerializeField, Min(0f)] private float handbrakeTorque = 4000f;
        [SerializeField, Range(0.05f, 2f)] private float rearDriftSidewaysGrip = 0.5f;
        [SerializeField, Min(0.1f)] private float driftGripResponse = 16f;
        [SerializeField, Min(0f)] private float minimumDriftSpeedKmh = 18f;
        [SerializeField, Min(0f)] private float driftYawAssist = 0.5f;

        [Header("Respawn")]
        [SerializeField, Min(0.1f)] private float flippedRespawnDelay = 2f;
        [SerializeField, Range(0f, 1f)] private float flippedUpDotThreshold = 0.25f;
        [SerializeField] private float fallRespawnY = -10f;

        [Header("Optional Shared Input Actions")]
        [SerializeField] private InputActionReference driveActionReference;
        [SerializeField] private InputActionReference brakeActionReference;

        [Header("Debug")]
        [SerializeField] private bool showInputDebug;

        public float SpeedKmh => vehicleBody != null ? vehicleBody.linearVelocity.magnitude * 3.6f : 0f;
        public bool CanDrive { get; set; } = true;
        public bool IsDrifting { get; private set; }
        public VehicleAudioTelemetry AudioTelemetry => BuildAudioTelemetry();

        private Rigidbody vehicleBody;
        private InputAction fallbackDriveAction;
        private InputAction fallbackBrakeAction;
        private Vector2 driveInput;
        private bool brakeInput;
        private bool enabledDriveReference;
        private bool enabledBrakeReference;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float flippedTimer;
        private bool hasCachedFriction;
        private string lastInputSource = "None";
        private WheelFrictionCurve frontLeftSidewaysFriction;
        private WheelFrictionCurve frontRightSidewaysFriction;
        private WheelFrictionCurve rearLeftSidewaysFriction;
        private WheelFrictionCurve rearRightSidewaysFriction;

        private void Awake()
        {
            vehicleBody = GetComponent<Rigidbody>();
            ResolveWheelReferences();
            CacheWheelFriction();
        }

        private void Start()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void OnEnable()
        {
            SetupInputActions();
        }

        private void OnDisable()
        {
            TearDownInputActions();
            driveInput = Vector2.zero;
            brakeInput = false;
        }

        private void Update()
        {
            InputAction driveAction = driveActionReference != null
                ? driveActionReference.action
                : fallbackDriveAction;
            InputAction brakeAction = brakeActionReference != null
                ? brakeActionReference.action
                : fallbackBrakeAction;

            driveInput = driveAction != null ? driveAction.ReadValue<Vector2>() : Vector2.zero;
            brakeInput = brakeAction != null && brakeAction.IsPressed();
            lastInputSource = driveInput.sqrMagnitude > 0f || brakeInput ? "InputAction" : "None";

            ApplyDirectKeyboardFallback();
            ApplyLegacyInputFallback();
        }

        private void FixedUpdate()
        {
            if (!HasAllWheels())
            {
                return;
            }

            UpdateRespawnState();
            IsDrifting = driftEnabled && CanDrive && brakeInput && SpeedKmh >= minimumDriftSpeedKmh;
            UpdateGripForSpeed(IsDrifting);

            float speed01 = Mathf.Clamp01(SpeedKmh / Mathf.Max(1f, maxSpeedKmh));
            float steerLimit = Mathf.Lerp(maxSteerAngle, minSteerAngleAtTopSpeed, speed01);
            float steerAngle = CanDrive ? driveInput.x * steerLimit : 0f;
            frontLeftWheel.steerAngle = steerAngle;
            frontRightWheel.steerAngle = steerAngle;

            bool overForwardSpeedLimit = SpeedKmh >= maxSpeedKmh && Vector3.Dot(vehicleBody.linearVelocity, transform.forward) > 0f && driveInput.y > 0f;
            float appliedMotorTorque = CanDrive && !brakeInput && !overForwardSpeedLimit ? driveInput.y * motorTorque : 0f;
            rearLeftWheel.motorTorque = appliedMotorTorque;
            rearRightWheel.motorTorque = appliedMotorTorque;

            float frontBrakeTorque = !CanDrive ? brakeTorque : 0f;
            float rearBrakeTorque = !CanDrive
                ? brakeTorque
                : brakeInput ? handbrakeTorque : 0f;
            frontLeftWheel.brakeTorque = frontBrakeTorque;
            frontRightWheel.brakeTorque = frontBrakeTorque;
            rearLeftWheel.brakeTorque = rearBrakeTorque;
            rearRightWheel.brakeTorque = rearBrakeTorque;

            ApplyDriveAssist(appliedMotorTorque, overForwardSpeedLimit);
            ApplyDriftYawAssist();
        }

        public void ResetVehicle(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            vehicleBody.linearVelocity = Vector3.zero;
            vehicleBody.angularVelocity = Vector3.zero;
            flippedTimer = 0f;
        }

        public void ApplyStats(CarDefinition stats)
        {
            if (stats == null)
            {
                return;
            }

            maxSpeedKmh = stats.MaxSpeedKmh;
            motorTorque = stats.MotorTorque;
            brakeTorque = stats.BrakeTorque;
            maxSteerAngle = stats.SteeringAngle;
            driveAssistAcceleration = Mathf.Clamp(stats.MotorTorque * 0.01f, 8f, 28f);

            float grip = Mathf.Max(0.1f, stats.Grip);
            lowSpeedSidewaysGrip = Mathf.Clamp(grip * 1.45f, 0.1f, 3f);
            highSpeedSidewaysGrip = Mathf.Clamp(grip * 0.85f, 0.1f, 3f);
            minSteerAngleAtTopSpeed = Mathf.Clamp(stats.SteeringAngle * 0.4f, 1f, stats.SteeringAngle);
            CacheWheelFriction();
        }

        private void SetupInputActions()
        {
            if (driveActionReference != null)
            {
                enabledDriveReference = EnableIfNeeded(driveActionReference.action);
            }
            else
            {
                fallbackDriveAction = new InputAction("VehicleDrive", InputActionType.Value);
                fallbackDriveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/s")
                    .With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/a")
                    .With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/d")
                    .With("Right", "<Keyboard>/rightArrow");
                fallbackDriveAction.AddBinding("<Gamepad>/leftStick");
                fallbackDriveAction.Enable();
            }

            if (brakeActionReference != null)
            {
                enabledBrakeReference = EnableIfNeeded(brakeActionReference.action);
            }
            else
            {
                fallbackBrakeAction = new InputAction("VehicleBrake", InputActionType.Button);
                fallbackBrakeAction.AddBinding("<Keyboard>/space");
                fallbackBrakeAction.AddBinding("<Gamepad>/buttonSouth");
                fallbackBrakeAction.Enable();
            }
        }

        private void TearDownInputActions()
        {
            if (enabledDriveReference && driveActionReference != null)
            {
                driveActionReference.action.Disable();
            }

            if (enabledBrakeReference && brakeActionReference != null)
            {
                brakeActionReference.action.Disable();
            }

            fallbackDriveAction?.Dispose();
            fallbackBrakeAction?.Dispose();
            fallbackDriveAction = null;
            fallbackBrakeAction = null;
            enabledDriveReference = false;
            enabledBrakeReference = false;
        }

        private static bool EnableIfNeeded(InputAction action)
        {
            if (action == null || action.enabled)
            {
                return false;
            }

            action.Enable();
            return true;
        }

        private void ApplyDirectKeyboardFallback()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            float horizontal = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            float vertical = 0f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                vertical += 1f;
            }

            Vector2 keyboardInput = new Vector2(horizontal, vertical);
            if (keyboardInput.sqrMagnitude > driveInput.sqrMagnitude)
            {
                driveInput = Vector2.ClampMagnitude(keyboardInput, 1f);
                lastInputSource = "Keyboard.current";
            }

            if (keyboard.spaceKey.isPressed)
            {
                brakeInput = true;
                lastInputSource = "Keyboard.current";
            }
        }

        private void ApplyLegacyInputFallback()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Vector2 legacyInput = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical"));

            if (legacyInput.sqrMagnitude > driveInput.sqrMagnitude)
            {
                driveInput = Vector2.ClampMagnitude(legacyInput, 1f);
                lastInputSource = "Legacy Input";
            }

            if (UnityEngine.Input.GetKey(KeyCode.Space))
            {
                brakeInput = true;
                lastInputSource = "Legacy Input";
            }
#endif
        }

        private void ResolveWheelReferences()
        {
            if (frontLeftWheel == null)
            {
                frontLeftWheel = FindWheel("WheelColliders/WheelCollider_FL");
            }

            if (frontRightWheel == null)
            {
                frontRightWheel = FindWheel("WheelColliders/WheelCollider_FR");
            }

            if (rearLeftWheel == null)
            {
                rearLeftWheel = FindWheel("WheelColliders/WheelCollider_RL");
            }

            if (rearRightWheel == null)
            {
                rearRightWheel = FindWheel("WheelColliders/WheelCollider_RR");
            }
        }

        private WheelCollider FindWheel(string relativePath)
        {
            Transform wheelTransform = transform.Find(relativePath);
            return wheelTransform != null ? wheelTransform.GetComponent<WheelCollider>() : null;
        }

        private bool HasAllWheels()
        {
            ResolveWheelReferences();

            return frontLeftWheel != null &&
                   frontRightWheel != null &&
                   rearLeftWheel != null &&
                   rearRightWheel != null;
        }

        private void UpdateRespawnState()
        {
            if (transform.position.y < fallRespawnY)
            {
                ResetVehicle(initialPosition, initialRotation);
                return;
            }

            bool isFlipped = Vector3.Dot(transform.up, Vector3.up) < flippedUpDotThreshold;
            flippedTimer = isFlipped ? flippedTimer + Time.fixedDeltaTime : 0f;

            if (flippedTimer >= flippedRespawnDelay)
            {
                ResetVehicle(initialPosition, initialRotation);
            }
        }

        private void CacheWheelFriction()
        {
            if (!HasAllWheels())
            {
                return;
            }

            frontLeftSidewaysFriction = frontLeftWheel.sidewaysFriction;
            frontRightSidewaysFriction = frontRightWheel.sidewaysFriction;
            rearLeftSidewaysFriction = rearLeftWheel.sidewaysFriction;
            rearRightSidewaysFriction = rearRightWheel.sidewaysFriction;
            hasCachedFriction = true;
        }

        private void UpdateGripForSpeed(bool drifting)
        {
            if (!hasCachedFriction)
            {
                CacheWheelFriction();
            }

            float speed01 = Mathf.Clamp01(SpeedKmh / Mathf.Max(1f, maxSpeedKmh));
            float stiffness = Mathf.Lerp(lowSpeedSidewaysGrip, highSpeedSidewaysGrip, speed01);
            ApplySidewaysGrip(frontLeftWheel, frontLeftSidewaysFriction, stiffness);
            ApplySidewaysGrip(frontRightWheel, frontRightSidewaysFriction, stiffness);
            float targetRearGrip = drifting ? rearDriftSidewaysGrip : stiffness;
            float rearLeftGrip = Mathf.MoveTowards(rearLeftWheel.sidewaysFriction.stiffness, targetRearGrip, driftGripResponse * Time.fixedDeltaTime);
            float rearRightGrip = Mathf.MoveTowards(rearRightWheel.sidewaysFriction.stiffness, targetRearGrip, driftGripResponse * Time.fixedDeltaTime);
            ApplySidewaysGrip(rearLeftWheel, rearLeftSidewaysFriction, rearLeftGrip);
            ApplySidewaysGrip(rearRightWheel, rearRightSidewaysFriction, rearRightGrip);
        }

        private void ApplyDriftYawAssist()
        {
            if (!IsDrifting || vehicleBody == null || Mathf.Abs(driveInput.x) < 0.01f)
            {
                return;
            }

            vehicleBody.AddTorque(Vector3.up * (driveInput.x * driftYawAssist), ForceMode.Acceleration);
        }

        private void ApplyDriveAssist(float appliedMotorTorque, bool overForwardSpeedLimit)
        {
            if (vehicleBody == null || driveAssistAcceleration <= 0f || !CanDrive)
            {
                return;
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up);

            if (brakeInput)
            {
                if (driftEnabled)
                {
                    return;
                }

                Vector3 dampedPlanarVelocity = Vector3.MoveTowards(
                    planarVelocity,
                    Vector3.zero,
                    driveAssistAcceleration * 1.8f * Time.fixedDeltaTime);
                vehicleBody.linearVelocity = dampedPlanarVelocity + Vector3.Project(vehicleBody.linearVelocity, Vector3.up);
                return;
            }

            bool hasThrottleInput = Mathf.Abs(driveInput.y) > 0.01f;
            if (!hasThrottleInput || Mathf.Approximately(appliedMotorTorque, 0f) || overForwardSpeedLimit)
            {
                return;
            }

            float maxSpeedMetersPerSecond = maxSpeedKmh / 3.6f;
            Vector3 targetPlanarVelocity = transform.forward * (driveInput.y * maxSpeedMetersPerSecond);
            Vector3 assistedPlanarVelocity = Vector3.MoveTowards(
                planarVelocity,
                targetPlanarVelocity,
                driveAssistAcceleration * Time.fixedDeltaTime);

            vehicleBody.linearVelocity = assistedPlanarVelocity + Vector3.Project(vehicleBody.linearVelocity, Vector3.up);

            float speedForSteer = planarVelocity.magnitude;
            float steerResponse = Mathf.Clamp01(speedForSteer / 8f);
            float yawDegrees = driveInput.x * maxSteerAngle * steerResponse * Time.fixedDeltaTime;
            if (Mathf.Abs(yawDegrees) > 0.001f)
            {
                vehicleBody.MoveRotation(vehicleBody.rotation * Quaternion.Euler(0f, yawDegrees, 0f));
            }
        }

        private void OnGUI()
        {
            if (!showInputDebug)
            {
                return;
            }

            string text = $"Vehicle: {name}\n" +
                          $"Input: {driveInput} Brake: {brakeInput} Source: {lastInputSource}\n" +
                          $"CanDrive: {CanDrive} Speed: {SpeedKmh:0.0} km/h";
            GUI.Label(new Rect(12f, 12f, 420f, 72f), text);
        }

        private static void ApplySidewaysGrip(WheelCollider wheel, WheelFrictionCurve baseFriction, float stiffness)
        {
            baseFriction.stiffness = stiffness;
            wheel.sidewaysFriction = baseFriction;
        }

        private VehicleAudioTelemetry BuildAudioTelemetry()
        {
            WheelCollider[] wheels = { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel };
            int groundedCount = 0;
            float forwardSlip = 0f;
            float sidewaysSlip = 0f;
            SurfaceType surface = SurfaceType.Asphalt;
            foreach (WheelCollider wheel in wheels)
            {
                if (wheel == null || !wheel.GetGroundHit(out WheelHit hit)) continue;
                groundedCount++;
                forwardSlip = Mathf.Max(forwardSlip, Mathf.Abs(hit.forwardSlip));
                sidewaysSlip = Mathf.Max(sidewaysSlip, Mathf.Abs(hit.sidewaysSlip));
                string surfaceName = hit.collider != null ? (hit.collider.tag + hit.collider.name).ToLowerInvariant() : "";
                if (surfaceName.Contains("sand")) surface = SurfaceType.Sand;
                else if (surfaceName.Contains("grass")) surface = SurfaceType.Grass;
            }

            return new VehicleAudioTelemetry
            {
                SpeedKmh = SpeedKmh,
                Throttle = Mathf.Abs(driveInput.y),
                Brake = brakeInput ? 1f : 0f,
                IsGrounded = groundedCount > 0,
                ForwardSlip = forwardSlip,
                SidewaysSlip = sidewaysSlip,
                CurrentSurface = surface
            };
        }
    }
}
