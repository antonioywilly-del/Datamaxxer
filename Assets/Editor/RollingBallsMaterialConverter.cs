using UnityEngine;
using UnityEditor;
using System.IO;

namespace Datamaxxer.Editor
{
    /// <summary>
    /// Utility script to convert Rolling Balls materials from Built-in Standard shader to URP Lit.
    /// Run from the menu: Datamaxxer > Convert Rolling Balls Materials
    /// </summary>
    public class RollingBallsMaterialConverter : UnityEditor.Editor
    {
        private const string TargetFolder = "Assets/Rolling_Balls-Sci-fi_Pack";

        [MenuItem("Datamaxxer/Convert Rolling Balls Materials")]
        public static void ConvertMaterials()
        {
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { TargetFolder });
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpLitShader == null)
            {
                Debug.LogError("[RollingBallsMaterialConverter] Universal Render Pipeline/Lit shader not found!");
                return;
            }

            int count = 0;
            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Only convert if it's using the old standard or other built-in shader
                if (mat.shader.name != urpLitShader.name)
                {
                    Undo.RecordObject(mat, "Convert to URP Lit");

                    // Read existing properties
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    Color color = mat.HasProperty("_Color") ? mat.color : Color.white;
                    
                    Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                    Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
                    Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

                    Vector2 baseScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
                    Vector2 baseOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;

                    // Switch shader to URP Lit
                    mat.shader = urpLitShader;

                    // Map values to new URP property names
                    if (mainTex != null)
                    {
                        mat.SetTexture("_BaseMap", mainTex);
                        mat.SetTextureScale("_BaseMap", baseScale);
                        mat.SetTextureOffset("_BaseMap", baseOffset);
                    }
                    mat.SetColor("_BaseColor", color);

                    if (bumpMap != null)
                    {
                        mat.SetTexture("_BumpMap", bumpMap);
                        mat.EnableKeyword("_NORMALMAP");
                    }

                    if (emissionMap != null)
                    {
                        mat.SetTexture("_EmissionMap", emissionMap);
                        mat.SetColor("_EmissionColor", emissionColor);
                        mat.EnableKeyword("_EMISSION");
                    }

                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[RollingBallsMaterialConverter] Successfully converted {count} materials to URP Lit.");
        }
    }
}
