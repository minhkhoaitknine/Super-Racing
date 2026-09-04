using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class RaceMinimap : MonoBehaviour
    {
        private const int TextureWidth = 384;
        private const int TextureHeight = 256;
        private const float RenderInterval = 0.2f;
        private const float PositionSmoothTime = 0.055f;
        private const float RotationSmoothTime = 0.07f;
        private Transform target;
        private Camera minimapCamera;
        private RenderTexture renderTexture;
        private Vector3 cameraVelocity;
        private float yawVelocity;
        private float nextRenderTime;

        private void Start()
        {
            ResolveTarget();

            GameObject cameraObject = new("Runtime Minimap Camera");
            minimapCamera = cameraObject.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 55f;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.01f, 0.025f, 0.04f, 1f);
            minimapCamera.depth = -20f;
            minimapCamera.allowHDR = false;
            minimapCamera.allowMSAA = false;
            minimapCamera.useOcclusionCulling = false;
            minimapCamera.nearClipPlane = 0.1f;
            minimapCamera.farClipPlane = 180f;
            if (target != null)
            {
                Vector3 initialPosition = target.position;
                minimapCamera.transform.position = new Vector3(initialPosition.x, initialPosition.y + 100f, initialPosition.z);
                minimapCamera.transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
            }
            else
            {
                minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            minimapCamera.enabled = false;

            renderTexture = new RenderTexture(TextureWidth, TextureHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "Runtime Minimap",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear
            };
            renderTexture.Create();
            minimapCamera.targetTexture = renderTexture;
            GetComponent<RawImage>().texture = renderTexture;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                ResolveTarget();
            }

            if (target == null || minimapCamera == null)
            {
                return;
            }

            float deltaTime = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            Vector3 targetPosition = target.position + Vector3.up * 100f;
            minimapCamera.transform.position = Vector3.SmoothDamp(
                minimapCamera.transform.position,
                targetPosition,
                ref cameraVelocity,
                PositionSmoothTime,
                Mathf.Infinity,
                deltaTime);

            float smoothYaw = Mathf.SmoothDampAngle(
                minimapCamera.transform.eulerAngles.y,
                target.eulerAngles.y,
                ref yawVelocity,
                RotationSmoothTime,
                Mathf.Infinity,
                deltaTime);
            minimapCamera.transform.rotation = Quaternion.Euler(90f, smoothYaw, 0f);

            if (Time.unscaledTime < nextRenderTime)
            {
                return;
            }

            minimapCamera.Render();
            nextRenderTime = Time.unscaledTime + RenderInterval;
        }

        private void ResolveTarget()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }

        private void OnDestroy()
        {
            if (minimapCamera != null)
            {
                Destroy(minimapCamera.gameObject);
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }
    }
}
