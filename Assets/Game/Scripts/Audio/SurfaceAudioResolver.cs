using UnityEngine;

namespace SuperRacing.Audio
{
    public sealed class SurfaceAudioMarker : MonoBehaviour
    {
        public SurfaceType surface = SurfaceType.Asphalt;
    }

    public static class SurfaceAudioResolver
    {
        public static SurfaceType Resolve(Collider collider, SurfaceType fallback = SurfaceType.Asphalt)
        {
            if (collider == null) return fallback;
            SurfaceAudioMarker marker = collider.GetComponentInParent<SurfaceAudioMarker>();
            if (marker != null) return marker.surface;

            string evidence = collider.name + " " + collider.tag;
            if (collider.sharedMaterial != null) evidence += " " + collider.sharedMaterial.name;
            Renderer renderer = collider.GetComponentInParent<Renderer>();
            if (renderer == null) renderer = collider.GetComponentInChildren<Renderer>();
            if (renderer != null)
                foreach (Material material in renderer.sharedMaterials) if (material != null) evidence += " " + material.name;

            evidence = evidence.ToLowerInvariant();
            if (evidence.Contains("sand") || evidence.Contains("desert") || evidence.Contains("gravel") || evidence.Contains("dirt")) return SurfaceType.Sand;
            if (evidence.Contains("grass") || evidence.Contains("turf")) return SurfaceType.Grass;
            if (evidence.Contains("road") || evidence.Contains("asphalt") || evidence.Contains("track") || evidence.Contains("concrete")) return SurfaceType.Asphalt;
            return fallback;
        }
    }
}
