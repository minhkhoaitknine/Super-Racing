using SuperRacing.Contracts;
using SuperRacing.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperRacing.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PrototypeVehicleController : MonoBehaviour, IVehicleController
    {
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float steeringSpeed = 90f;
        [SerializeField] private float maxSpeedKmh = 80f;

        private Rigidbody body;

        public float SpeedKmh => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public bool IsDrifting => false;
        public bool CanDrive { get; set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.centerOfMass = new Vector3(0f, -0.35f, 0f);
        }

        private void FixedUpdate()
        {
            if (!CanDrive || Keyboard.current == null)
            {
                return;
            }

            float throttle = 0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) throttle += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) throttle -= 1f;

            float steering = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steering -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steering += 1f;

            if (SpeedKmh < maxSpeedKmh || throttle < 0f)
            {
                body.AddForce(transform.forward * (throttle * acceleration), ForceMode.Acceleration);
            }

            if (Mathf.Abs(throttle) > 0.01f || body.linearVelocity.sqrMagnitude > 0.25f)
            {
                Quaternion turn = Quaternion.Euler(0f, steering * steeringSpeed * Time.fixedDeltaTime, 0f);
                body.MoveRotation(body.rotation * turn);
            }
        }

        public void ApplyStats(CarDefinition stats)
        {
            if (stats == null) return;
            maxSpeedKmh = 80f * stats.MaxSpeedPercent * 0.01f;
            acceleration = 20f * stats.AccelerationPercent * 0.01f;
            steeringSpeed = 90f * stats.SteeringPercent * 0.01f;
        }

        public void ResetVehicle(Vector3 position, Quaternion rotation)
        {
            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
