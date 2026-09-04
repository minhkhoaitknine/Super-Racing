using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class GaragePreviewRotator : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.01f)] private float dragSensitivity = 0.35f;
        [SerializeField, Min(0.001f)] private float dragSmoothTime = 0.035f;
        [SerializeField, Min(0.001f)] private float autoSmoothTime = 0.08f;
        [SerializeField, Min(0f)] private float autoRotationSpeed = 12f;

        private float targetYaw;
        private float currentYaw;
        private float yawVelocity;
        private bool isDragging;

        public void SetTarget(Transform previewTarget)
        {
            target = previewTarget;
            ResetRotationState();
        }

        private void Awake()
        {
            ResetRotationState();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (!isDragging)
            {
                targetYaw += autoRotationSpeed * Time.unscaledDeltaTime;
            }

            float smoothTime = isDragging ? dragSmoothTime : autoSmoothTime;
            currentYaw = Mathf.SmoothDampAngle(
                currentYaw,
                targetYaw,
                ref yawVelocity,
                smoothTime,
                Mathf.Infinity,
                Mathf.Max(Time.unscaledDeltaTime, 0.0001f));
            target.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            if (target != null)
            {
                currentYaw = target.localEulerAngles.y;
                targetYaw = currentYaw;
                yawVelocity = 0f;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null)
            {
                return;
            }

            targetYaw -= eventData.delta.x * dragSensitivity;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        private void ResetRotationState()
        {
            currentYaw = target == null ? 0f : target.localEulerAngles.y;
            targetYaw = currentYaw;
            yawVelocity = 0f;
        }
    }
}
