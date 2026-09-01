using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class LobbyVehicleInteractor : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Transform vehicleRoot;
        [SerializeField] private MainMenuUI menu;
        [SerializeField, Min(0.01f)] private float dragSensitivity = 0.35f;
        [SerializeField, Min(0f)] private float autoRotationSpeed = 12f;

        private float yaw;
        private bool dragged;
        private bool isDragging;

        public void Configure(Transform target, MainMenuUI targetMenu)
        {
            vehicleRoot = target;
            menu = targetMenu;
            yaw = target != null ? target.localEulerAngles.y : 0f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragged = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
        }

        private void Update()
        {
            if (vehicleRoot == null || isDragging)
            {
                return;
            }

            yaw += autoRotationSpeed * Time.unscaledDeltaTime;
            vehicleRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (vehicleRoot == null)
            {
                return;
            }

            if (eventData.delta.sqrMagnitude > 0.25f)
            {
                dragged = true;
            }

            yaw -= eventData.delta.x * dragSensitivity;
            vehicleRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!dragged && menu != null)
            {
                menu.OpenGarage();
            }
        }
    }
}
