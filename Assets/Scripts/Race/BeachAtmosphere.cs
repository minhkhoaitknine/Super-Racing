using UnityEngine;
using UnityEngine.Rendering;

namespace SuperRacing.Race
{
    public sealed class BeachAtmosphere : MonoBehaviour
    {
        [SerializeField] private Color hazeColor = new Color(0.64f, 0.73f, 0.76f);
        [SerializeField] private float hazeStart = 65f, hazeEnd = 240f;
        [SerializeField] private Color sunlightColor = new Color(1f, 0.94f, 0.83f);
        [SerializeField] private float sunlightIntensity = 1.3f;
        [SerializeField] private Vector3 sunlightAngles = new Vector3(42f, -35f, 0f);
        private bool applied;
        private bool fog;
        private Color fogColor;
        private FogMode fogMode;
        private float fogStart, fogEnd;
        private Material previousSky, sky;
        private Light sun;
        private Color sunColor;
        private float sunIntensity;
        private Quaternion sunRotation;

        private void Start()
        {
            // Track-selection previews have their own lighting and must not change the scene.
            if (gameObject.scene.name != "Race" && gameObject.scene.name != "Test_Race") return;
            applied = true;
            fog = RenderSettings.fog;
            fogColor = RenderSettings.fogColor;
            fogMode = RenderSettings.fogMode;
            fogStart = RenderSettings.fogStartDistance;
            fogEnd = RenderSettings.fogEndDistance;
            previousSky = RenderSettings.skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = hazeColor;
            RenderSettings.fogStartDistance = hazeStart;
            RenderSettings.fogEndDistance = hazeEnd;
            if (previousSky != null)
            {
                sky = new Material(previousSky);
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.15f);
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.05f);
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.42f, 0.48f, 0.5f));
                RenderSettings.skybox = sky;
            }
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional || light.gameObject.scene != gameObject.scene) continue;
                sun = light;
                break;
            }
            if (sun != null)
            {
                sunColor = sun.color;
                sunIntensity = sun.intensity;
                sunRotation = sun.transform.rotation;
                sun.color = sunlightColor;
                sun.intensity = sunlightIntensity;
                sun.transform.rotation = Quaternion.Euler(sunlightAngles);
            }
        }

        private void OnDisable()
        {
            if (!applied) return;
            applied = false;
            RenderSettings.fog = fog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
            RenderSettings.skybox = previousSky;
            if (sun != null)
            {
                sun.color = sunColor;
                sun.intensity = sunIntensity;
                sun.transform.rotation = sunRotation;
            }
            if (sky != null) Destroy(sky);
        }
    }
}
