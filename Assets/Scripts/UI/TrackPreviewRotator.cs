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
        private float displayedYaw;
        private bool isDragging;
        public Transform Target => target;

        public void Configure(Transform previewTarget)
        {
            target = previewTarget;
            SyncRotation();
        }

        private void Awake()
        {
            SyncRotation();
        }

        private void OnEnable() => SyncRotation();

        private void OnDisable() => isDragging = false;

        public void SyncRotation()
        {
            targetYaw = displayedYaw = target != null ? target.localEulerAngles.y : 0f;
            isDragging = false;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            // Advance both angles equally: smoothing applies only to pointer input,
            // never to the continuous automatic rotation.
            float step = isDragging ? 0f : autoRotationSpeed * Time.unscaledDeltaTime;
            targetYaw = Mathf.Repeat(targetYaw + step, 360f);
            displayedYaw = Mathf.Repeat(displayedYaw + step, 360f);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.unscaledDeltaTime);
            displayedYaw = Mathf.LerpAngle(displayedYaw, targetYaw, blend);
            target.localRotation = Quaternion.Euler(0f, displayedYaw, 0f);
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
