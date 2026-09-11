using System.Collections.Generic;
using UnityEngine;

namespace SuperRacing.Economy
{
    // Own the generated textures for exactly as long as this vehicle is displayed.
    public sealed class CarPaintTextures : MonoBehaviour
    {
        private readonly Dictionary<Texture, RenderTexture> textures = new();

        public Texture Recolor(Texture source, Color color)
        {
            if (textures.TryGetValue(source, out RenderTexture existing)) return existing;
            Shader shader = Resources.Load<Shader>("CarPaintRecolor");
            if (shader == null || !shader.isSupported) return source;
            var material = new Material(shader);
            material.SetColor("_PaintColor", QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color);
            var texture = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32)
            {
                name = source.name + " Custom Paint",
                useMipMap = true,
                autoGenerateMips = false,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            RenderTexture previous = RenderTexture.active;
            bool previousSrgb = GL.sRGBWrite;
            try
            {
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(source, texture, material);
                texture.GenerateMips();
            }
            finally
            {
                GL.sRGBWrite = previousSrgb;
                RenderTexture.active = previous;
                Destroy(material);
            }
            textures.Add(source, texture);
            return texture;
        }

        private void OnDestroy()
        {
            foreach (RenderTexture texture in textures.Values)
            {
                texture.Release();
                Destroy(texture);
            }
            textures.Clear();
        }
    }
}
