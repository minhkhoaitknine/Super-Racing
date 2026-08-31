using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperRacing.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        [Header("Wheel Colliders")]
        [SerializeField] private WheelCollider frontLeftWheel;
        [SerializeField] private WheelCollider frontRightWheel;
        [SerializeField] private WheelCollider rearLeftWheel;
        [SerializeField] private WheelCollider rearRightWheel;

        [Header("Prototype Tuning")]
        [SerializeField, Min(0f)] private float motorTorque = 1400f;
        [SerializeField, Min(0f)] private float brakeTorque = 3000f;
        [SerializeField, Min(1f)] private float maxSpeedKmh = 120f;
        [SerializeField, Range(0f, 60f)] private float maxSteerAngle = 25f;
        [SerializeField, Range(0f, 60f)] private float minSteerAngleAtTopSpeed = 10f;
        [SerializeField, Range(0.1f, 3f)] private float lowSpeedSidewaysGrip = 1.45f;
        [SerializeField, Range(0.1f, 3f)] private float highSpeedSidewaysGrip = 0.85f;

        [Header("Respawn")]
        [SerializeField, Min(0.1f)] private float flippedRespawnDelay = 2f;
        [SerializeField, Range(0f, 1f)] private float flippedUpDotThreshold = 0.25f;
        [SerializeField] private float fallRespawnY = -10f;

        [Header("Optional Shared Input Actions")]
        [SerializeField] private InputActionReference driveActionReference;
        [SerializeField] private InputActionReference brakeActionReference;

        public float SpeedKmh => vehicleBody != null ? vehicleBody.linearVelocity.magnitude * 3.6f : 0f;
        public bool CanDrive { get; set; } = true;

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
        }

        private void FixedUpdate()
        {
            if (!HasAllWheels())
            {
                return;
            }

            UpdateRespawnState();
            UpdateGripForSpeed();

            float speed01 = Mathf.Clamp01(SpeedKmh / Mathf.Max(1f, maxSpeedKmh));
            float steerLimit = Mathf.Lerp(maxSteerAngle, minSteerAngleAtTopSpeed, speed01);
            float steerAngle = CanDrive ? driveInput.x * steerLimit : 0f;
            frontLeftWheel.steerAngle = steerAngle;
            frontRightWheel.steerAngle = steerAngle;

            bool overForwardSpeedLimit = SpeedKmh >= maxSpeedKmh && Vector3.Dot(vehicleBody.linearVelocity, transform.forward) > 0f && driveInput.y > 0f;
            float appliedMotorTorque = CanDrive && !brakeInput && !overForwardSpeedLimit ? driveInput.y * motorTorque : 0f;
            rearLeftWheel.motorTorque = appliedMotorTorque;
            rearRightWheel.motorTorque = appliedMotorTorque;

            float appliedBrakeTorque = brakeInput || !CanDrive ? brakeTorque : 0f;
            frontLeftWheel.brakeTorque = appliedBrakeTorque;
            frontRightWheel.brakeTorque = appliedBrakeTorque;
            rearLeftWheel.brakeTorque = appliedBrakeTorque;
            rearRightWheel.brakeTorque = appliedBrakeTorque;
        }

        public void ResetVehicle(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            vehicleBody.linearVelocity = Vector3.zero;
            vehicleBody.angularVelocity = Vector3.zero;
            flippedTimer = 0f;
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

        private void ResolveWheelReferences()
        {
            frontLeftWheel ??= FindWheel("WheelColliders/WheelCollider_FL");
            frontRightWheel ??= FindWheel("WheelColliders/WheelCollider_FR");
            rearLeftWheel ??= FindWheel("WheelColliders/WheelCollider_RL");
            rearRightWheel ??= FindWheel("WheelColliders/WheelCollider_RR");
        }

        private WheelCollider FindWheel(string relativePath)
        {
            Transform wheelTransform = transform.Find(relativePath);
            return wheelTransform != null ? wheelTransform.GetComponent<WheelCollider>() : null;
        }

        private bool HasAllWheels()
        {
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

        private void UpdateGripForSpeed()
        {
            if (!hasCachedFriction)
            {
                CacheWheelFriction();
            }

            float speed01 = Mathf.Clamp01(SpeedKmh / Mathf.Max(1f, maxSpeedKmh));
            float stiffness = Mathf.Lerp(lowSpeedSidewaysGrip, highSpeedSidewaysGrip, speed01);
            ApplySidewaysGrip(frontLeftWheel, frontLeftSidewaysFriction, stiffness);
            ApplySidewaysGrip(frontRightWheel, frontRightSidewaysFriction, stiffness);
            ApplySidewaysGrip(rearLeftWheel, rearLeftSidewaysFriction, stiffness);
            ApplySidewaysGrip(rearRightWheel, rearRightSidewaysFriction, stiffness);
        }

        private static void ApplySidewaysGrip(WheelCollider wheel, WheelFrictionCurve baseFriction, float stiffness)
        {
            baseFriction.stiffness = stiffness;
            wheel.sidewaysFriction = baseFriction;
        }
    }
}
