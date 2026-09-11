using System.Collections.Generic;
using System.IO;
using SuperRacing.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SuperRacing.Editor
{
    public static class BeachVisualUpgrade
    {
        private const string Folder = "Assets/Game/Art/Maps/Beach/Realistic";
        private const string PrefabPath = "Assets/Game/Prefabs/Maps/BeachMap.prefab";

        [MenuItem("Super Racing/Upgrade Beach Visuals")]
        public static void Build()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play mode before rebuilding Beach.");
            AssetDatabase.Refresh();
            Material sand = Surface("Coastal Sand", "coast_sand_01", 0.12f, new Color(1.15f, 1.12f, 1.02f));
            Material rock = Surface("Coastal Rock", "coast_sand_rocks_02", 0.18f, new Color(0.92f, 0.95f, 0.97f));
            Material road = Surface("Weathered Asphalt", "aerial_asphalt_01", 0.18f, Color.white);
            Material water = SaveMaterial(new Material(Shader.Find("SuperRacing/Coastal Water")), "Coastal Water");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Art/Maps/Beach/Imported/Beach_RaceGameBeach_Source.fbx");
            var sourceMeshes = new Dictionary<string, Mesh>();
            foreach (MeshFilter filter in original.GetComponentsInChildren<MeshFilter>(true))
                sourceMeshes[filter.sharedMesh.name] = filter.sharedMesh;
            Texture2D palette = ReadPalette();
            try
            {
                foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    string name = filter.name;
                    bool isRock = name.StartsWith("Cliff") || name == "Plane.070" || name == "Plane.074" || name == "Plane.080" || name == "Plane.082" || name == "Plane.075" || name == "Plane.081" || name == "Plane.083";
                    bool isSand = name == "Plane.061";
                    bool isRoad = name == "Plane.048" || name == "Plane.062";
                    bool isWater = name == "Plane.069";
                    if (!isRock && !isSand && !isRoad && !isWater) continue;
                    // Always rebuild from the FBX, not from a previously upgraded mesh.
                    if (!sourceMeshes.TryGetValue(name, out Mesh source)) throw new System.InvalidOperationException("Missing source mesh " + name);
                    MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                    Material originalMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Road Markings.mat");
                    if (isRoad && originalMaterial == null)
                        originalMaterial = SaveMaterial(new Material(renderer.sharedMaterial), "Road Markings");
                    Mesh mesh = ProjectMesh(source, filter.transform, isRock, isRoad, isWater, palette);
                    string meshPath = Folder + "/" + name + ".asset";
                    Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
                    else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
                    filter.sharedMesh = mesh;
                    renderer.sharedMaterials = isRoad ? new[] { road, originalMaterial } : new[] { isRock ? rock : isSand ? sand : water };
                    if (isWater) renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                if (root.GetComponent<BeachAtmosphere>() == null) root.AddComponent<BeachAtmosphere>();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(palette);
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }

        private static Material Surface(string name, string id, float smoothness, Color tint)
        {
            string diffusePath = Folder + "/" + id + "_diff_2k.jpg";
            string normalPath = Folder + "/" + id + "_nor_gl_2k.jpg";
            foreach (string path in new[] { diffusePath, normalPath })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = path == normalPath ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 8;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 0.7f);
            material.SetFloat("_Smoothness", smoothness);
            material.SetColor("_BaseColor", tint);
            return SaveMaterial(material, name);
        }

        private static Material SaveMaterial(Material material, string name)
        {
            string path = Folder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            material.name = name;
            if (existing == null) { AssetDatabase.CreateAsset(material, path); return material; }
            EditorUtility.CopySerialized(material, existing);
            Object.DestroyImmediate(material);
            return existing;
        }

        private static Texture2D ReadPalette()
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes("Assets/Game/Art/Maps/Beach/Source/textures/Sprite-0001-export-export.png"));
            return texture;
        }

        private static Mesh ProjectMesh(Mesh source, Transform transform, bool rock, bool road, bool water, Texture2D palette)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var surfaceIndices = new List<int>();
            var markings = new List<int>();
            Vector3[] positions = source.vertices;
            Vector3[] sourceNormals = source.normals;
            Vector2[] sourceUvs = source.uv;
            int[] triangles = source.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = transform.TransformPoint(positions[triangles[i]]);
                Vector3 b = transform.TransformPoint(positions[triangles[i + 1]]);
                Vector3 c = transform.TransformPoint(positions[triangles[i + 2]]);
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                bool marking = false;
                if (road)
                {
                    Vector2 uv = (sourceUvs[triangles[i]] + sourceUvs[triangles[i + 1]] + sourceUvs[triangles[i + 2]]) / 3f;
                    Color color = palette.GetPixelBilinear(uv.x, uv.y);
                    marking = color.maxColorComponent > 0.6f;
                }
                for (int corner = 0; corner < 3; corner++)
                {
                    int index = triangles[i + corner];
                    Vector3 world = transform.TransformPoint(positions[index]);
                    if (water) world.y = -0.35f;
                    Vector2 uv;
                    if (marking) uv = sourceUvs[index];
                    else if (rock && Mathf.Abs(normal.y) < 0.65f)
                        uv = Mathf.Abs(normal.x) > Mathf.Abs(normal.z) ? new Vector2(world.z, world.y) / 8f : new Vector2(world.x, world.y) / 8f;
                    else uv = new Vector2(world.x, world.z) / (rock ? 8f : road ? 5f : 7f);
                    (marking ? markings : surfaceIndices).Add(vertices.Count);
                    vertices.Add(transform.InverseTransformPoint(world));
                    normals.Add(water ? transform.InverseTransformDirection(Vector3.up) : sourceNormals[index]);
                    uvs.Add(uv);
                }
            }
            var mesh = new Mesh { name = source.name + " Coastal", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = road ? 2 : 1;
            mesh.SetTriangles(surfaceIndices, 0);
            if (road) mesh.SetTriangles(markings, 1);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
