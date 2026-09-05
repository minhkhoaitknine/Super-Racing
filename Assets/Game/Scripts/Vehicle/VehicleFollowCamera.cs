using UnityEngine;

namespace SuperRacing.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class VehicleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 localOffset = new(0f, 4f, -8f);
        [SerializeField, Min(0.01f)] private float positionDamping = 8f;
        [SerializeField, Min(0.01f)] private float rotationDamping = 10f;
        [SerializeField, Min(1f)] private float lookAheadDistance = 6f;

        private void Awake()
        {
            ResolveDefaultTarget();
        }

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                ResolveDefaultTarget();
                SnapToTarget();
            }

            if (target == null)
            {
                return;
            }

            Quaternion flatRotation = GetFlatTargetRotation();
            Vector3 desiredPosition = target.position + flatRotation * localOffset;
            float positionT = 1f - Mathf.Exp(-positionDamping * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionT);

            Vector3 lookPoint = target.position + flatRotation * Vector3.forward * lookAheadDistance;
            Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            float rotationT = 1f - Mathf.Exp(-rotationDamping * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
        }

        private void ResolveDefaultTarget()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            Quaternion flatRotation = GetFlatTargetRotation();
            transform.position = target.position + flatRotation * localOffset;
            Vector3 lookPoint = target.position + flatRotation * Vector3.forward * lookAheadDistance;
            transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        }

        private Quaternion GetFlatTargetRotation()
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            }

            return Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        }
    }
}
