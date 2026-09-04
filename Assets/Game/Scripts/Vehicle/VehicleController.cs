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
        private const float DefaultMotorTorque = 1000f;
        private const float DefaultBrakeTorque = 3000f;
        private const float DefaultMaxSpeedKmh = 120f;
        private const float DefaultDriveAssistAcceleration = 3.5f;
        private const float DefaultMaxSteerAngle = 30f;
        private const float DefaultMinSteerAngleAtTopSpeed = 10f;
        private const float DefaultLowSpeedSidewaysGrip = 1.6f;
        private const float DefaultHighSpeedSidewaysGrip = 1.25f;

        [Header("Wheel Colliders")]
        [SerializeField] private WheelCollider frontLeftWheel;
        [SerializeField] private WheelCollider frontRightWheel;
        [SerializeField] private WheelCollider rearLeftWheel;
        [SerializeField] private WheelCollider rearRightWheel;

        [Header("Prototype Tuning")]
        [SerializeField, Min(0f)] private float motorTorque = 1000f;
        [SerializeField, Min(0f)] private float brakeTorque = 3000f;
        [SerializeField, Min(1f)] private float maxSpeedKmh = 120f;
        [SerializeField, Min(0f)] private float driveAssistAcceleration = 3.5f;
        [SerializeField, Range(0f, 60f)] private float maxSteerAngle = 30f;
        [SerializeField, Range(0f, 60f)] private float minSteerAngleAtTopSpeed = 10f;
        [SerializeField, Range(0.1f, 3f)] private float lowSpeedSidewaysGrip = 1.6f;
        [SerializeField, Range(0.1f, 3f)] private float highSpeedSidewaysGrip = 1.25f;
        [SerializeField, Min(0.1f)] private float steeringInputResponse = 3.5f;
        [SerializeField, Min(0.1f)] private float steeringReturnResponse = 6f;
        [SerializeField, Min(0f)] private float normalLateralGripResponse = 8f;
        [SerializeField, Min(0f)] private float normalYawResponse = 6f;
        [SerializeField, Range(1f, 180f)] private float maxNormalYawRate = 70f;
        [SerializeField, Range(0f, 1f)] private float lowSpeedSteeringAssist = 0.35f;

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

        [Header("Silent Stuck Recovery")]
        [SerializeField, Min(0.1f)] private float safePoseSaveInterval = 0.5f;
        [SerializeField, Min(0f)] private float safePoseMinimumSpeedKmh = 8f;
        [SerializeField, Range(0f, 1f)] private float safePoseUpDotThreshold = 0.75f;
        [SerializeField, Min(0f)] private float stuckSpeedThresholdKmh = 1.5f;
        [SerializeField, Min(0f)] private float stuckMovementThreshold = 0.4f;
        [SerializeField, Min(0.1f)] private float stuckDetectionWindow = 2f;
        [SerializeField, Min(0f)] private float stuckRecoveryVerticalOffset = 0.75f;
        [SerializeField, Min(0f)] private float stuckRecoveryCooldown = 1.25f;
        [SerializeField, Min(0f)] private float minimumRecoveryDistance = 3f;

        [Header("Optional Shared Input Actions")]
        [SerializeField] private InputActionReference driveActionReference;
        [SerializeField] private InputActionReference brakeActionReference;

        [Header("Debug")]
        [SerializeField] private bool showInputDebug;

        public float SpeedKmh => vehicleBody != null ? vehicleBody.linearVelocity.magnitude * 3.6f : 0f;
        public bool CanDrive { get; set; } = true;
        public bool IsDrifting { get; private set; }
        public VehicleAudioTelemetry AudioTelemetry => BuildAudioTelemetry();

        private const int SafePoseCapacity = 8;

        private Rigidbody vehicleBody;
        private InputAction fallbackDriveAction;
        private InputAction fallbackBrakeAction;
        private Vector2 driveInput;
        private float currentSteerInput;
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
        private readonly SafePose[] safePoseHistory = new SafePose[SafePoseCapacity];
        private int safePoseCount;
        private int nextSafePoseIndex;
        private float safePoseTimer;
        private Vector3 stuckWindowStartPosition;
        private float stuckTimer;
        private float recoveryCooldownTimer;
        private bool hasStuckWindowStart;
        private bool stuckForwardAttempted;
        private bool stuckReverseAttempted;
        private bool stuckSteeringAttempted;

        private void Awake()
        {
            vehicleBody = GetComponent<Rigidbody>();
            ConfigureRigidbodyForSmoothFollow();
            ResolveWheelReferences();
            CacheWheelFriction();
        }

        private void Start()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            StoreSafePose(initialPosition, initialRotation);
            ResetStuckDetectionWindow();
        }

        private void OnEnable()
        {
            SetupInputActions();
        }

        private void OnDisable()
        {
            TearDownInputActions();
            driveInput = Vector2.zero;
            currentSteerInput = 0f;
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
            if (UpdateStuckRecoveryState())
            {
                return;
            }

            IsDrifting = driftEnabled && CanDrive && brakeInput && SpeedKmh >= minimumDriftSpeedKmh;
            UpdateGripForSpeed(IsDrifting);

            float speed01 = Mathf.Clamp01(SpeedKmh / Mathf.Max(1f, maxSpeedKmh));
            float steerLimit = Mathf.Lerp(maxSteerAngle, minSteerAngleAtTopSpeed, Mathf.Sqrt(speed01));
            float targetSteerInput = CanDrive ? driveInput.x : 0f;
            float inputResponse = Mathf.Abs(targetSteerInput) > Mathf.Abs(currentSteerInput)
                ? steeringInputResponse
                : steeringReturnResponse;
            currentSteerInput = Mathf.MoveTowards(currentSteerInput, targetSteerInput, inputResponse * Time.fixedDeltaTime);
            float steerAngle = currentSteerInput * steerLimit;
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
            ApplyNormalHandlingAssist();
            ApplyDriftYawAssist();
        }

        public void ResetVehicle(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            vehicleBody.linearVelocity = Vector3.zero;
            vehicleBody.angularVelocity = Vector3.zero;
            currentSteerInput = 0f;
            flippedTimer = 0f;
            recoveryCooldownTimer = stuckRecoveryCooldown;
            ResetStuckDetectionWindow();
        }

        public void ApplyStats(CarDefinition stats)
        {
            if (stats == null)
            {
                return;
            }

            float speedScale = stats.MaxSpeedPercent * 0.01f;
            float accelerationScale = stats.AccelerationPercent * 0.01f;
            float brakingScale = stats.BrakingPercent * 0.01f;
            float steeringScale = stats.SteeringPercent * 0.01f;
            float gripScale = stats.GripPercent * 0.01f;

            maxSpeedKmh = DefaultMaxSpeedKmh * speedScale;
            motorTorque = DefaultMotorTorque * accelerationScale;
            brakeTorque = DefaultBrakeTorque * brakingScale;
            driveAssistAcceleration = DefaultDriveAssistAcceleration * accelerationScale;
            maxSteerAngle = DefaultMaxSteerAngle * steeringScale;
            minSteerAngleAtTopSpeed = DefaultMinSteerAngleAtTopSpeed * steeringScale;
            lowSpeedSidewaysGrip = DefaultLowSpeedSidewaysGrip * gripScale;
            highSpeedSidewaysGrip = DefaultHighSpeedSidewaysGrip * gripScale;
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

        private void ConfigureRigidbodyForSmoothFollow()
        {
            if (vehicleBody == null || vehicleBody.interpolation != RigidbodyInterpolation.None)
            {
                return;
            }

            vehicleBody.interpolation = RigidbodyInterpolation.Interpolate;
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

        private bool UpdateStuckRecoveryState()
        {
            CaptureSafePoseIfStable();

            if (recoveryCooldownTimer > 0f)
            {
                recoveryCooldownTimer = Mathf.Max(0f, recoveryCooldownTimer - Time.fixedDeltaTime);
                ResetStuckDetectionWindow();
                return false;
            }

            if (!CanDrive || brakeInput || CountGroundedWheels() < 2)
            {
                ResetStuckDetectionWindow();
                return false;
            }

            bool hasForwardInput = driveInput.y > 0.5f;
            bool hasReverseInput = driveInput.y < -0.5f;
            bool hasSteeringInput = Mathf.Abs(driveInput.x) > 0.5f;
            bool hasThrottleInput = hasForwardInput || hasReverseInput;

            if (!hasThrottleInput)
            {
                ResetStuckDetectionWindow();
                return false;
            }

            if (!hasStuckWindowStart)
            {
                stuckWindowStartPosition = transform.position;
                hasStuckWindowStart = true;
            }

            stuckForwardAttempted |= hasForwardInput;
            stuckReverseAttempted |= hasReverseInput;
            stuckSteeringAttempted |= hasSteeringInput;

            bool movedEnough = Vector3.Distance(transform.position, stuckWindowStartPosition) >= stuckMovementThreshold;
            bool movingEnough = SpeedKmh > stuckSpeedThresholdKmh;
            if (movedEnough || movingEnough)
            {
                ResetStuckDetectionWindow();
                return false;
            }

            stuckTimer += Time.fixedDeltaTime;
            bool hasTriedEscapeInputs = stuckForwardAttempted && stuckReverseAttempted && stuckSteeringAttempted;
            if (stuckTimer < stuckDetectionWindow || !hasTriedEscapeInputs)
            {
                return false;
            }

            RecoverFromStuck();
            return true;
        }

        private void CaptureSafePoseIfStable()
        {
            safePoseTimer += Time.fixedDeltaTime;
            if (safePoseTimer < safePoseSaveInterval)
            {
                return;
            }

            safePoseTimer = 0f;
            if (!CanDrive || brakeInput || CountGroundedWheels() < 2)
            {
                return;
            }

            bool movingSafely = SpeedKmh >= safePoseMinimumSpeedKmh;
            bool upright = Vector3.Dot(transform.up, Vector3.up) >= safePoseUpDotThreshold;
            if (!movingSafely || !upright)
            {
                return;
            }

            StoreSafePose(transform.position, transform.rotation);
        }

        private void RecoverFromStuck()
        {
            SafePose recoveryPose = FindRecoveryPose();
            ResetVehicle(
                recoveryPose.Position + Vector3.up * stuckRecoveryVerticalOffset,
                recoveryPose.Rotation);
        }

        private SafePose FindRecoveryPose()
        {
            SafePose fallbackPose = safePoseCount > 0
                ? safePoseHistory[(nextSafePoseIndex - 1 + SafePoseCapacity) % SafePoseCapacity]
                : new SafePose(initialPosition, initialRotation);

            for (int offset = 1; offset <= safePoseCount; offset++)
            {
                int index = (nextSafePoseIndex - offset + SafePoseCapacity) % SafePoseCapacity;
                SafePose candidate = safePoseHistory[index];
                if (Vector3.Distance(candidate.Position, transform.position) >= minimumRecoveryDistance)
                {
                    return candidate;
                }
            }

            return fallbackPose;
        }

        private void StoreSafePose(Vector3 position, Quaternion rotation)
        {
            safePoseHistory[nextSafePoseIndex] = new SafePose(position, rotation);
            nextSafePoseIndex = (nextSafePoseIndex + 1) % SafePoseCapacity;
            safePoseCount = Mathf.Min(safePoseCount + 1, SafePoseCapacity);
        }

        private void ResetStuckDetectionWindow()
        {
            hasStuckWindowStart = false;
            stuckWindowStartPosition = transform.position;
            stuckTimer = 0f;
            stuckForwardAttempted = false;
            stuckReverseAttempted = false;
            stuckSteeringAttempted = false;
        }

        private int CountGroundedWheels()
        {
            int groundedWheels = 0;
            if (frontLeftWheel != null && frontLeftWheel.GetGroundHit(out _))
            {
                groundedWheels++;
            }

            if (frontRightWheel != null && frontRightWheel.GetGroundHit(out _))
            {
                groundedWheels++;
            }

            if (rearLeftWheel != null && rearLeftWheel.GetGroundHit(out _))
            {
                groundedWheels++;
            }

            if (rearRightWheel != null && rearRightWheel.GetGroundHit(out _))
            {
                groundedWheels++;
            }

            return groundedWheels;
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
            if (!IsDrifting || vehicleBody == null || Mathf.Abs(currentSteerInput) < 0.01f)
            {
                return;
            }

            vehicleBody.AddTorque(Vector3.up * (currentSteerInput * driftYawAssist), ForceMode.Acceleration);
        }

        private void ApplyNormalHandlingAssist()
        {
            if (IsDrifting || vehicleBody == null || !CanDrive)
            {
                return;
            }

            Vector3 verticalVelocity = Vector3.Project(vehicleBody.linearVelocity, Vector3.up);
            Vector3 localPlanarVelocity = transform.InverseTransformDirection(
                Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up));
            float lateralRetention = Mathf.Exp(-normalLateralGripResponse * Time.fixedDeltaTime);
            localPlanarVelocity.x *= lateralRetention;
            vehicleBody.linearVelocity = transform.TransformDirection(localPlanarVelocity) + verticalVelocity;

            float forwardSpeed = localPlanarVelocity.z;
            float steeringAtSpeed = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 5f);
            float steeringFromThrottle = Mathf.Abs(driveInput.y) > 0.1f
                ? Mathf.Abs(driveInput.y) * lowSpeedSteeringAssist
                : 0f;
            float steeringAssist = Mathf.Max(steeringAtSpeed, steeringFromThrottle);
            float movementDirection = Mathf.Abs(forwardSpeed) > 0.5f
                ? Mathf.Sign(forwardSpeed)
                : Mathf.Sign(Mathf.Approximately(driveInput.y, 0f) ? 1f : driveInput.y);
            float targetYawRate = currentSteerInput * movementDirection * maxNormalYawRate * Mathf.Deg2Rad * steeringAssist;
            Vector3 angularVelocity = vehicleBody.angularVelocity;
            angularVelocity.y = Mathf.MoveTowards(
                angularVelocity.y,
                targetYawRate,
                normalYawResponse * Time.fixedDeltaTime);
            vehicleBody.angularVelocity = angularVelocity;
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

        private readonly struct SafePose
        {
            public SafePose(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
        }
    }
}
