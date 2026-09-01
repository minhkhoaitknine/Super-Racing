using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class TrackPreviewRotator : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float autoRotationSpeed = 8f;
        [SerializeField, Min(0.01f)] private float dragSensitivity = 0.3f;
        [SerializeField, Min(0.01f)] private float rotationSharpness = 18f;

        private float targetYaw;
        private bool isDragging;

        public void Configure(Transform previewTarget)
        {
            target = previewTarget;
            targetYaw = target != null ? target.localEulerAngles.y : 0f;
        }

        private void Awake()
        {
            targetYaw = target != null ? target.localEulerAngles.y : 0f;
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            if (!isDragging)
            {
                targetYaw += autoRotationSpeed * Time.unscaledDeltaTime;
            }

            Quaternion desired = Quaternion.Euler(0f, targetYaw, 0f);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.unscaledDeltaTime);
            target.localRotation = Quaternion.Slerp(target.localRotation, desired, blend);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            targetYaw -= eventData.delta.x * dragSensitivity;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }
    }
}
