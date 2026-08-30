using UnityEngine;

namespace SuperRacing.Vehicle
{
    [DisallowMultipleComponent]
    public sealed class WheelVisualSync : MonoBehaviour
    {
        [SerializeField] private WheelCollider frontLeftCollider;
        [SerializeField] private Transform frontLeftVisual;
        [SerializeField] private WheelCollider frontRightCollider;
        [SerializeField] private Transform frontRightVisual;
        [SerializeField] private WheelCollider rearLeftCollider;
        [SerializeField] private Transform rearLeftVisual;
        [SerializeField] private WheelCollider rearRightCollider;
        [SerializeField] private Transform rearRightVisual;

        private void Awake()
        {
            ResolveReferences();
        }

        private void LateUpdate()
        {
            SyncWheel(frontLeftCollider, frontLeftVisual);
            SyncWheel(frontRightCollider, frontRightVisual);
            SyncWheel(rearLeftCollider, rearLeftVisual);
            SyncWheel(rearRightCollider, rearRightVisual);
        }

        private void ResolveReferences()
        {
            frontLeftCollider ??= FindWheelCollider("WheelColliders/WheelCollider_FL");
            frontRightCollider ??= FindWheelCollider("WheelColliders/WheelCollider_FR");
            rearLeftCollider ??= FindWheelCollider("WheelColliders/WheelCollider_RL");
            rearRightCollider ??= FindWheelCollider("WheelColliders/WheelCollider_RR");

            frontLeftVisual ??= transform.Find("Visuals/WheelVisual_FL");
            frontRightVisual ??= transform.Find("Visuals/WheelVisual_FR");
            rearLeftVisual ??= transform.Find("Visuals/WheelVisual_RL");
            rearRightVisual ??= transform.Find("Visuals/WheelVisual_RR");
        }

        private WheelCollider FindWheelCollider(string relativePath)
        {
            Transform wheelTransform = transform.Find(relativePath);
            return wheelTransform != null ? wheelTransform.GetComponent<WheelCollider>() : null;
        }

        private static void SyncWheel(WheelCollider wheelCollider, Transform wheelVisual)
        {
            if (wheelCollider == null || wheelVisual == null)
            {
                return;
            }

            wheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);
            wheelVisual.SetPositionAndRotation(position, rotation);
        }
    }
}
