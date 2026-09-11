using System;
using System.Collections.Generic;
using System.IO;
using SuperRacing.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SuperRacing.Editor
{
    // Explicit source meshes keep road markings, props, water and collision geometry intact.
    public static class AdditionalMapVisualUpgrade
    {
        private const string Folder = "Assets/Game/Art/Maps/SurfacePolish";
        private const string Beach = "Assets/Game/Art/Maps/Beach/Realistic";
        private static readonly HashSet<string> RockPlanes = new HashSet<string>
        { "Plane.017", "Plane.021", "Plane.054", "Plane.055", "Plane.057", "Plane.058" };

        [MenuItem("Super Racing/Polish Desert and Town Square")]
        public static void Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            BuildDesert();
            BuildTown();
            AssetDatabase.SaveAssets();
        }

        private static Material Surface(string name, string diffuse, string normal, Color tint, float bump)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ConfigureTexture(diffuse, false);
            ConfigureTexture(normal, true);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_BumpScale", bump);
            material.SetFloat("_Smoothness", .12f);
            material.EnableKeyword("_NORMALMAP");
            return Save(material, name);
        }

        private static void ConfigureTexture(string path, bool normal, bool fromHeight = false)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.convertToNormalmap = fromHeight;
            if (fromHeight) importer.heightmapScale = .035f;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.SaveAndReimport();
        }

        private static Material Save(Material value, string name)
        {
            string path = Folder + "/" + name + ".mat";
            value.name = name;
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null) { AssetDatabase.CreateAsset(value, path); return value; }
            EditorUtility.CopySerialized(value, existing);
            Object.DestroyImmediate(value);
            return existing;
        }

        private static void BuildDesert()
        {
            var rock = Surface("Desert Sandstone", Folder + "/sandstone_cracks_diff_2k.jpg",
                Folder + "/sandstone_cracks_nor_gl_2k.jpg", new Color(1f, .76f, .57f), .6f);
            var sand = Surface("Desert Sand", Beach + "/coast_sand_01_diff_2k.jpg",
                Beach + "/coast_sand_01_nor_gl_2k.jpg", new Color(1f, .81f, .62f), .4f);
            var road = Save(new Material(AssetDatabase.LoadAssetAtPath<Material>(Beach + "/Weathered Asphalt.mat")), "Desert Asphalt");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Art/Maps/Desert/Imported/Desert_RaceGameDesertV2_Source.fbx");
            var sources = new Dictionary<string, Mesh>();
            foreach (var f in source.GetComponentsInChildren<MeshFilter>(true)) sources[f.sharedMesh.name] = f.sharedMesh;
            string path = "Assets/Game/Prefabs/Maps/DesertMap.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var palette = new Texture2D(2, 2);
            palette.LoadImage(File.ReadAllBytes("Assets/Game/Art/Maps/Beach/Source/textures/Sprite-0001-export-export.png"));
            try
            {
                foreach (var f in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    bool isRock = f.name.StartsWith("Cliff", StringComparison.Ordinal) || RockPlanes.Contains(f.name);
                    bool isSand = f.name == "Plane.006";
                    bool isRoad = f.name == "Plane.005";
                    if (!isRock && !isSand && !isRoad) continue;
                    if (!sources.TryGetValue(f.name, out var mesh)) throw new InvalidOperationException("Missing original mesh: " + f.name);
                    var renderer = f.GetComponent<MeshRenderer>();
                    var markings = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Desert Markings.mat");
                    if (isRoad && markings == null) markings = Save(new Material(renderer.sharedMaterial), "Desert Markings");
                    f.sharedMesh = Project(mesh, f.transform, isRoad, isSand ? 5f : isRoad ? 5f : 7f, palette);
                    renderer.sharedMaterials = isRoad ? new[] { road, markings } : new[] { isSand ? sand : rock };
                }
                Atmosphere(root, true);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { Object.DestroyImmediate(palette); PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Mesh Project(Mesh source, Transform transform, bool road, float metres, Texture2D palette)
        {
            var positions = source.vertices;
            var normals = source.normals;
            var originalUV = source.uv;
            var indices = source.triangles;
            var vertices = new List<Vector3>();
            var projectedNormals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var surface = new List<int>();
            var markings = new List<int>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 a = transform.TransformPoint(positions[indices[i]]);
                Vector3 b = transform.TransformPoint(positions[indices[i + 1]]);
                Vector3 c = transform.TransformPoint(positions[indices[i + 2]]);
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                Vector2 centroid = (originalUV[indices[i]] + originalUV[indices[i + 1]] + originalUV[indices[i + 2]]) / 3f;
                bool marking = road && palette.GetPixelBilinear(centroid.x, centroid.y).maxColorComponent > .6f;
                for (int j = 0; j < 3; j++)
                {
                    int index = indices[i + j];
                    Vector3 p = transform.TransformPoint(positions[index]);
                    // Dominant-axis projection prevents vertical cliffs collapsing into stripes.
                    Vector2 uv = Mathf.Abs(n.y) >= Mathf.Max(Mathf.Abs(n.x), Mathf.Abs(n.z))
                        ? new Vector2(p.x, p.z) : Mathf.Abs(n.x) > Mathf.Abs(n.z)
                        ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
                    (marking ? markings : surface).Add(vertices.Count);
                    vertices.Add(positions[index]);
                    projectedNormals.Add(normals[index]);
                    uvs.Add(marking ? originalUV[index] : uv / metres);
                }
            }
            var result = new Mesh { name = source.name + " Sandstone", indexFormat = IndexFormat.UInt32 };
            result.SetVertices(vertices);
            result.SetNormals(projectedNormals);
            result.SetUVs(0, uvs);
            result.subMeshCount = road ? 2 : 1;
            result.SetTriangles(surface, 0);
            if (road) result.SetTriangles(markings, 1);
            result.RecalculateTangents();
            result.RecalculateBounds();
            string path = Folder + "/Desert_" + source.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(result, path); return result; }
            EditorUtility.CopySerialized(result, existing);
            Object.DestroyImmediate(result);
            return existing;
        }

        private static void BuildTown()
        {
            ConfigureTexture(Folder + "/leafy_grass_diff_2k.jpg", false);
            ConfigureTexture(Folder + "/leafy_grass_nor_gl_2k.jpg", true);
            string path = "Assets/Game/Prefabs/Maps/TownSquareMap.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.name != "Track5_TownSquare" || !renderer.enabled) continue;
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        string atlasPath = "Assets/Game/Art/Maps/TownSquare/Source/source/Track5_TownSquare_0" + (i + 1) + "_Day_Map.png";
                        string normalPath = Folder + "/Town_0" + (i + 1) + "_Relief.png";
                        if (!File.Exists(normalPath)) File.Copy(atlasPath, normalPath);
                        AssetDatabase.ImportAsset(normalPath);
                        // Gentle relief from the matching atlas preserves stone/wood/roof alignment.
                        ConfigureTexture(normalPath, true, true);
                        var mat = new Material(materials[i]);
                        mat.shader = Shader.Find("SuperRacing/Town Atlas Detail");
                        mat.SetTexture("_GrassMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/leafy_grass_diff_2k.jpg"));
                        mat.SetTexture("_GrassNormal", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/leafy_grass_nor_gl_2k.jpg"));
                        mat.SetFloat("_GrassScale", 3f);
                        mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                        mat.SetFloat("_BumpScale", .15f);
                        mat.SetFloat("_Smoothness", .1f);
                        mat.SetFloat("_Metallic", 0f);
                        mat.SetColor("_BaseColor", new Color(.93f, .94f, .95f));
                        mat.EnableKeyword("_NORMALMAP");
                        materials[i] = Save(mat, "Town Masonry " + (i + 1));
                    }
                    renderer.sharedMaterials = materials;
                }
                Atmosphere(root, false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Atmosphere(GameObject root, bool desert)
        {
            var component = root.GetComponent<BeachAtmosphere>();
            if (component == null) component = root.AddComponent<BeachAtmosphere>();
            var settings = new SerializedObject(component);
            settings.FindProperty("hazeColor").colorValue = desert ? new Color(.73f, .68f, .59f) : new Color(.65f, .72f, .76f);
            settings.FindProperty("hazeStart").floatValue = desert ? 85f : 110f;
            settings.FindProperty("hazeEnd").floatValue = desert ? 310f : 340f;
            settings.FindProperty("sunlightColor").colorValue = desert ? new Color(1f, .93f, .82f) : new Color(1f, .97f, .9f);
            settings.FindProperty("sunlightIntensity").floatValue = desert ? 1.15f : 1.1f;
            settings.FindProperty("sunlightAngles").vector3Value = desert ? new Vector3(46f, -35f, 0f) : new Vector3(48f, -25f, 0f);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
