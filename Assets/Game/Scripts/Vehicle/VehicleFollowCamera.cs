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

            Vector3 desiredPosition = target.TransformPoint(localOffset);
            float positionT = 1f - Mathf.Exp(-positionDamping * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionT);

            Vector3 lookPoint = target.position + target.forward * lookAheadDistance;
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

            transform.position = target.TransformPoint(localOffset);
            Vector3 lookPoint = target.position + target.forward * lookAheadDistance;
            transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        }
    }
}
