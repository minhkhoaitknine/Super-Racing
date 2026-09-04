using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class GaragePreviewRotator : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.01f)] private float dragSensitivity = 0.35f;
        [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.055f;
        [SerializeField, Min(0f)] private float autoRotationSpeed = 12f;
        [SerializeField, Min(0f)] private float inertiaDamping = 7f;
        [SerializeField, Min(0f)] private float autoResumeDelay = 0.8f;

        private float targetYaw;
        private float smoothVelocity;
        private float dragVelocity;
        private float lastInteractionTime = float.NegativeInfinity;
        private bool isDragging;

        public void SetTarget(Transform previewTarget)
        {
            target = previewTarget;
            targetYaw = target == null ? 0f : target.eulerAngles.y;
        }

        private void Awake()
        {
            targetYaw = target == null ? 0f : target.eulerAngles.y;
        }

        private void Update()
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

            float currentYaw = Mathf.SmoothDampAngle(
                target.localEulerAngles.y,
                targetYaw,
                ref smoothVelocity,
                rotationSmoothTime,
                Mathf.Infinity,
                Mathf.Max(0.0001f, Time.unscaledDeltaTime));
            target.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            targetYaw = target == null ? targetYaw : target.localEulerAngles.y;
            smoothVelocity = 0f;
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
    }
}
