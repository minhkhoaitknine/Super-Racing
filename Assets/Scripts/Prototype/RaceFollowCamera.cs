using UnityEngine;

namespace SuperRacing.Prototype
{
    [DisallowMultipleComponent]
    public sealed class RaceFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 4f, -7f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1f, 3f);
        [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.15f;
        [SerializeField, Min(0f)] private float rotationSharpness = 12f;

        private Vector3 positionVelocity;

        private void Start()
        {
            if (target == null)
            {
                PrototypeVehicleController vehicle = FindFirstObjectByType<PrototypeVehicleController>();
                target = vehicle == null ? null : vehicle.transform;
            }

            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Quaternion flatRotation = GetFlatTargetRotation();
            Vector3 desiredPosition = target.position + flatRotation * followOffset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime);

            Vector3 lookPoint = target.position + flatRotation * lookOffset;
            Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            Quaternion flatRotation = GetFlatTargetRotation();
            transform.position = target.position + flatRotation * followOffset;
            transform.LookAt(target.position + flatRotation * lookOffset, Vector3.up);
            positionVelocity = Vector3.zero;
        }

        private Quaternion GetFlatTargetRotation()
        {
            Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
            return forward.sqrMagnitude < 0.001f
                ? Quaternion.identity
                : Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
