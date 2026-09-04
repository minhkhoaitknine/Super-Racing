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
        [SerializeField, Min(0f)] private float inertiaDamping = 7f;
        [SerializeField, Min(0f)] private float autoResumeDelay = 0.8f;

        private float targetYaw;
        private float currentYaw;
        private float yawVelocity;
        private float dragVelocity;
        private float lastInteractionTime = float.NegativeInfinity;
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
                float timeSinceInteraction = Time.unscaledTime - lastInteractionTime;
                if (timeSinceInteraction < autoResumeDelay && Mathf.Abs(dragVelocity) > 0.01f)
                {
                    targetYaw += dragVelocity * Time.unscaledDeltaTime;
                    dragVelocity *= Mathf.Exp(-inertiaDamping * Time.unscaledDeltaTime);
                }
                else if (timeSinceInteraction >= autoResumeDelay)
                {
                    dragVelocity = 0f;
                    targetYaw += autoRotationSpeed * Time.unscaledDeltaTime;
                }
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
            dragVelocity = 0f;
            lastInteractionTime = Time.unscaledTime;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null)
            {
                return;
            }

            float deltaYaw = -eventData.delta.x * dragSensitivity;
            targetYaw += deltaYaw;

            float deltaTime = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float instantaneousVelocity = deltaYaw / deltaTime;
            float velocityBlend = 1f - Mathf.Exp(-20f * deltaTime);
            dragVelocity = Mathf.Lerp(dragVelocity, instantaneousVelocity, velocityBlend);
            lastInteractionTime = Time.unscaledTime;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            lastInteractionTime = Time.unscaledTime;
        }

        private void ResetRotationState()
        {
            currentYaw = target == null ? 0f : target.localEulerAngles.y;
            targetYaw = currentYaw;
            yawVelocity = 0f;
            dragVelocity = 0f;
            lastInteractionTime = float.NegativeInfinity;
        }
    }
}
