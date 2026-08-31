using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class GaragePreviewRotator : MonoBehaviour, IDragHandler
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.01f)] private float dragSensitivity = 0.35f;
        [SerializeField, Min(0.01f)] private float rotationSharpness = 18f;

        private float targetYaw;

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

            Quaternion desiredRotation = Quaternion.Euler(0f, targetYaw, 0f);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.unscaledDeltaTime);
            target.rotation = Quaternion.Slerp(target.rotation, desiredRotation, blend);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null)
            {
                return;
            }

            targetYaw -= eventData.delta.x * dragSensitivity;
            target.localRotation = Quaternion.Euler(0f, targetYaw, 0f);
        }
    }
}
