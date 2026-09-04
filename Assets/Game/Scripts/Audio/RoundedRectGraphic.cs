using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.Audio
{
    /// <summary>A lightweight, texture-free rounded rectangle used by the audio-owned UI.</summary>
    [DisallowMultipleComponent]
    public sealed class RoundedRectGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float radius = 18f;
        [SerializeField, Range(2, 16)] private int cornerSegments = 6;

        public float Radius
        {
            get => radius;
            set { radius = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;

            float safeRadius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * .5f);
            int segments = Mathf.Clamp(cornerSegments, 2, 16);
            Vector2 center = rect.center;
            AddVertex(vertexHelper, center, rect);

            Vector2[] corners =
            {
                new(rect.xMax - safeRadius, rect.yMin + safeRadius),
                new(rect.xMax - safeRadius, rect.yMax - safeRadius),
                new(rect.xMin + safeRadius, rect.yMax - safeRadius),
                new(rect.xMin + safeRadius, rect.yMin + safeRadius)
            };
            float[] starts = { -90f, 0f, 90f, 180f };
            for (int corner = 0; corner < 4; corner++)
            {
                for (int step = 0; step <= segments; step++)
                {
                    float radians = (starts[corner] + 90f * step / segments) * Mathf.Deg2Rad;
                    Vector2 point = corners[corner] + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * safeRadius;
                    AddVertex(vertexHelper, point, rect);
                }
            }

            int perimeter = 4 * (segments + 1);
            for (int i = 0; i < perimeter; i++)
                vertexHelper.AddTriangle(0, i + 1, (i + 1) % perimeter + 1);
        }

        private void AddVertex(VertexHelper helper, Vector2 position, Rect rect)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = new Vector2(
                Mathf.InverseLerp(rect.xMin, rect.xMax, position.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, position.y));
            helper.AddVert(vertex);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            radius = Mathf.Max(0f, radius);
            cornerSegments = Mathf.Clamp(cornerSegments, 2, 16);
            SetVerticesDirty();
        }
#endif
    }
}
