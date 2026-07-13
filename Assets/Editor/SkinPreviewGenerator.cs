using UnityEngine;
using UnityEditor;
using System.IO;

namespace Datamaxxer.Editor
{
    /// <summary>
    /// Editor utility to generate preview images for all player skin prefabs.
    /// Run from the menu: Datamaxxer > Generate Skin Previews
    /// </summary>
    public class SkinPreviewGenerator : UnityEditor.Editor
    {
        private const string PrefabFolder = "Assets/Resources/PlayerSkins";
        private const string OutputFolder = "Assets/Resources/PlayerSkins/Previews";
        private const int PreviewSize = 256;

        [MenuItem("Datamaxxer/Generate Skin Previews")]
        public static void GeneratePreviews()
        {
            // Ensure output folder exists
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Resources/PlayerSkins/Previews")))
            {
                AssetDatabase.CreateFolder(PrefabFolder, "Previews");
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            int count = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Previews/")) continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                string prefabName = Path.GetFileNameWithoutExtension(path);
                string outputPath = $"{OutputFolder}/{prefabName}_Preview.png";

                Texture2D preview = GeneratePreviewTexture(prefab);
                if (preview != null)
                {
                    byte[] pngData = preview.EncodeToPNG();
                    string fullPath = Path.Combine(Application.dataPath, outputPath.Replace("Assets/", ""));
                    File.WriteAllBytes(fullPath, pngData);
                    DestroyImmediate(preview);
                    count++;
                    Debug.Log($"[SkinPreviewGenerator] Generated preview: {outputPath}");
                }
            }

            AssetDatabase.Refresh();

            // Set texture import settings for all previews
            string[] previewGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { OutputFolder });
            foreach (string guid in previewGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.isReadable = true;
                    importer.maxTextureSize = PreviewSize;
                    importer.SaveAndReimport();
                }
            }

            Debug.Log($"[SkinPreviewGenerator] Generated {count} skin previews in {OutputFolder}");
            // EditorUtility.DisplayDialog("Skin Preview Generator", $"Generated {count} preview images.", "OK");
        }

        private static Texture2D GeneratePreviewTexture(GameObject prefab)
        {
            // Use Unity's built-in asset preview system
            Texture2D assetPreview = AssetPreview.GetAssetPreview(prefab);

            // The asset preview may need time to generate. Try multiple times.
            if (assetPreview == null)
            {
                // Force loading
                AssetPreview.SetPreviewTextureCacheSize(256);
                for (int i = 0; i < 100; i++)
                {
                    assetPreview = AssetPreview.GetAssetPreview(prefab);
                    if (assetPreview != null) break;
                    System.Threading.Thread.Sleep(50);
                    AssetPreview.GetAssetPreview(prefab);
                }
            }

            if (assetPreview != null)
            {
                // Copy the preview texture (it's managed by Unity and may be destroyed)
                Texture2D copy = new Texture2D(assetPreview.width, assetPreview.height, TextureFormat.RGBA32, false);
                copy.SetPixels(assetPreview.GetPixels());
                copy.Apply();
                return copy;
            }

            // Fallback: create a simple colored texture from the material
            Renderer renderer = prefab.GetComponentInChildren<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Color baseColor = Color.gray;
                if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                    baseColor = renderer.sharedMaterial.GetColor("_BaseColor");
                else if (renderer.sharedMaterial.HasProperty("_Color"))
                    baseColor = renderer.sharedMaterial.GetColor("_Color");

                Texture2D tex = new Texture2D(PreviewSize, PreviewSize, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[PreviewSize * PreviewSize];

                // Create a simple sphere-like gradient
                Vector2 center = new Vector2(PreviewSize / 2f, PreviewSize / 2f);
                float maxRadius = PreviewSize / 2f;

                for (int y = 0; y < PreviewSize; y++)
                {
                    for (int x = 0; x < PreviewSize; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                        if (dist <= 1f)
                        {
                            // Simple sphere shading
                            float shade = 1f - dist * 0.5f;
                            pixels[y * PreviewSize + x] = baseColor * shade;
                            pixels[y * PreviewSize + x].a = 1f;
                        }
                        else
                        {
                            pixels[y * PreviewSize + x] = Color.clear;
                        }
                    }
                }

                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            }

            return null;
        }
    }
}
