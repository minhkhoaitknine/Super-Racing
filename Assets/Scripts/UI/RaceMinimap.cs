using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class RaceMinimap : MonoBehaviour
    {
        private const int TextureSize = 256;
        private Transform target;
        private Camera minimapCamera;
        private RenderTexture renderTexture;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;

            GameObject cameraObject = new("Runtime Minimap Camera");
            minimapCamera = cameraObject.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 55f;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.01f, 0.025f, 0.04f, 1f);
            minimapCamera.depth = -20f;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            renderTexture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "Runtime Minimap"
            };
            renderTexture.Create();
            minimapCamera.targetTexture = renderTexture;
            GetComponent<RawImage>().texture = renderTexture;
        }

        private void LateUpdate()
        {
            if (target == null || minimapCamera == null)
            {
                return;
            }

            Vector3 position = target.position;
            minimapCamera.transform.position = new Vector3(position.x, position.y + 100f, position.z);
            minimapCamera.transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
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
