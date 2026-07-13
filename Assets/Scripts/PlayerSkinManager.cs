using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Manages player skin selection, persistence, and runtime application.
    /// Uses Resources.Load to load skin prefabs from Resources/PlayerSkins/.
    /// Persists the selected skin index via PlayerPrefs.
    /// </summary>
    public class PlayerSkinManager : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // SINGLETON
        // ──────────────────────────────────────────────

        private static PlayerSkinManager instance;
        public static PlayerSkinManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PlayerSkinManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("PlayerSkinManager");
                        instance = go.AddComponent<PlayerSkinManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        // ──────────────────────────────────────────────
        // SKIN CATALOG
        // ──────────────────────────────────────────────

        /// <summary>
        /// Ordered list of skin prefab names (without path).
        /// Must match filenames inside Resources/PlayerSkins/.
        /// </summary>
        private static readonly string[] SkinNames = new string[]
        {
            "RollingBalls_Sci-fi_1_1",
            "RollingBalls_Sci-fi_1_2",
            "RollingBalls_Sci-fi_1_3",
            "RollingBalls_Sci-fi_1_4",
            "RollingBalls_Sci-fi_1_5",
            "RollingBalls_Sci-fi_2_1",
            "RollingBalls_Sci-fi_2_2",
            "RollingBalls_Sci-fi_2_3",
            "RollingBalls_Sci-fi_2_4",
            "RollingBalls_Sci-fi_2_5",
            "RollingBalls_Sci-fi_3_1",
            "RollingBalls_Sci-fi_3_2",
            "RollingBalls_Sci-fi_3_3",
            "RollingBalls_Sci-fi_3_4",
            "RollingBalls_Sci-fi_3_5",
            "RollingBalls_Sci-fi_4_1",
            "RollingBalls_Sci-fi_4_2",
            "RollingBalls_Sci-fi_4_3",
            "RollingBalls_Sci-fi_4_4",
            "RollingBalls_Sci-fi_4_5",
        };

        // ──────────────────────────────────────────────
        // PERSISTENCE
        // ──────────────────────────────────────────────

        private const string SelectedSkinKey = "SelectedSkin";
        private const int DefaultSkinIndex = 0;

        // ──────────────────────────────────────────────
        // STATE
        // ──────────────────────────────────────────────

        [Header("Skin Instantiation Settings")]
        [SerializeField] private Vector3 skinLocalRotation = new Vector3(15f, 90f, 0f);
        [SerializeField] private float playerColliderRadius = 0.4f;

        private int selectedSkinIndex;
        private GameObject skinVisualInstance;

        // ──────────────────────────────────────────────
        // PUBLIC API
        // ──────────────────────────────────────────────

        /// <summary>Number of available skins.</summary>
        public int SkinCount => SkinNames.Length;

        /// <summary>Currently selected skin index (0-based).</summary>
        public int SelectedSkinIndex => selectedSkinIndex;

        /// <summary>Get the display name for a skin by index.</summary>
        public string GetSkinName(int index)
        {
            if (index < 0 || index >= SkinNames.Length) return "Unknown";
            return SkinNames[index];
        }

        /// <summary>Get the prefab for a skin by index (loaded from Resources).</summary>
        public GameObject GetSkinPrefab(int index)
        {
            if (index < 0 || index >= SkinNames.Length) return null;
            return Resources.Load<GameObject>($"PlayerSkins/{SkinNames[index]}");
        }

        // ──────────────────────────────────────────────
        // LIFECYCLE
        // ──────────────────────────────────────────────

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSelectedSkin();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        // ──────────────────────────────────────────────
        // SELECTION
        // ──────────────────────────────────────────────

        /// <summary>
        /// Select a skin by index. Saves to PlayerPrefs immediately.
        /// </summary>
        public void SelectSkin(int index)
        {
            if (index < 0 || index >= SkinNames.Length)
            {
                Debug.LogWarning($"[PlayerSkinManager] Invalid skin index: {index}");
                return;
            }

            selectedSkinIndex = index;
            PlayerPrefs.SetInt(SelectedSkinKey, selectedSkinIndex);
            PlayerPrefs.Save();

            Debug.Log($"[PlayerSkinManager] Skin selected: {SkinNames[selectedSkinIndex]} (index {selectedSkinIndex})");
        }

        private void LoadSelectedSkin()
        {
            selectedSkinIndex = PlayerPrefs.GetInt(SelectedSkinKey, DefaultSkinIndex);

            // Validate saved index
            if (selectedSkinIndex < 0 || selectedSkinIndex >= SkinNames.Length)
            {
                selectedSkinIndex = DefaultSkinIndex;
            }

            Debug.Log($"[PlayerSkinManager] Loaded skin: {SkinNames[selectedSkinIndex]} (index {selectedSkinIndex})");
        }

        // ──────────────────────────────────────────────
        // APPLY SKIN TO PLAYER
        // ──────────────────────────────────────────────

        /// <summary>
        /// Applies the currently selected skin to the player GameObject.
        /// Disables the original MeshFilter/MeshRenderer and instantiates
        /// the skin prefab as a child.
        /// </summary>
        public void ApplySkin(GameObject playerObject)
        {
            if (playerObject == null)
            {
                Debug.LogError("[PlayerSkinManager] Cannot apply skin: playerObject is null.");
                return;
            }

            // Remove any previous skin visual
            if (skinVisualInstance != null)
            {
                Destroy(skinVisualInstance);
                skinVisualInstance = null;
            }

            // Also remove any previously spawned skin children (tag-based or name-based)
            foreach (Transform child in playerObject.transform)
            {
                if (child.name.StartsWith("SkinVisual_"))
                {
                    Destroy(child.gameObject);
                }
            }

            // Disable original mesh visuals (keep collider and controller)
            MeshFilter mf = playerObject.GetComponent<MeshFilter>();
            MeshRenderer mr = playerObject.GetComponent<MeshRenderer>();

            // MeshFilter doesn't have .enabled — hide original by disabling MeshRenderer
            // and clearing the mesh reference
            Mesh originalMesh = mf != null ? mf.sharedMesh : null;
            if (mf != null) mf.mesh = null;
            if (mr != null) mr.enabled = false;

            // Load and instantiate the skin prefab
            GameObject skinPrefab = GetSkinPrefab(selectedSkinIndex);
            if (skinPrefab == null)
            {
                Debug.LogError($"[PlayerSkinManager] Failed to load skin prefab: {SkinNames[selectedSkinIndex]}");
                // Re-enable original visuals as fallback
                if (mf != null && originalMesh != null) mf.mesh = originalMesh;
                if (mr != null) mr.enabled = true;
                return;
            }

            skinVisualInstance = Instantiate(skinPrefab, playerObject.transform);
            skinVisualInstance.name = $"SkinVisual_{SkinNames[selectedSkinIndex]}";
            skinVisualInstance.transform.localPosition = Vector3.zero;
            skinVisualInstance.transform.localRotation = Quaternion.Euler(skinLocalRotation);

            // Scale the skin to fit the player's SphereCollider (radius 0.5, player scale 0.8)
            // The Rolling_Balls prefabs are typically scale 1, so we adjust to match
            // the visible size of the original sphere (diameter ~1 unit at local scale)
            AdjustSkinScale(skinVisualInstance, playerObject);

            // Adjust SphereCollider radius to be tighter (prevent ghost deaths)
            SphereCollider sc = playerObject.GetComponent<SphereCollider>();
            if (sc != null)
            {
                sc.radius = playerColliderRadius;
            }

            // Adjust TrailRenderer colors based on skin material (predominant color)
            TrailRenderer tr = playerObject.GetComponent<TrailRenderer>();
            if (tr != null)
            {
                Renderer skinRenderer = skinVisualInstance.GetComponentInChildren<Renderer>();
                if (skinRenderer != null && skinRenderer.sharedMaterial != null)
                {
                    Material mat = skinRenderer.sharedMaterial;
                    Color keyColor = Color.cyan; // default fallback

                    if (mat.HasProperty("_BaseColor"))
                    {
                        keyColor = mat.GetColor("_BaseColor");
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        keyColor = mat.color;
                    }

                    // Check if emission is active and bright (glowing parts)
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        Color emissive = mat.GetColor("_EmissionColor");
                        float intensity = emissive.r + emissive.g + emissive.b;
                        if (intensity > 0.1f)
                        {
                            // De-intensify HDR color to normal RGB bounds
                            float maxVal = Mathf.Max(emissive.r, emissive.g, emissive.b);
                            if (maxVal > 1f)
                            {
                                keyColor = emissive / maxVal;
                            }
                            else
                            {
                                keyColor = emissive;
                            }
                        }
                    }

                    // Fallback to neon cyan if the color is too dark
                    if (keyColor.r + keyColor.g + keyColor.b < 0.15f)
                    {
                        keyColor = new Color(0f, 1f, 1f, 1f); // Neon Cyan
                    }

                    keyColor.a = 1f;

                    // Create gradient fading to transparent
                    Gradient gradient = new Gradient();
                    GradientColorKey[] colorKeys = new GradientColorKey[2];
                    colorKeys[0] = new GradientColorKey(keyColor, 0.0f);
                    colorKeys[1] = new GradientColorKey(keyColor, 1.0f);

                    GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                    alphaKeys[0] = new GradientAlphaKey(0.9f, 0.0f);
                    alphaKeys[1] = new GradientAlphaKey(0.0f, 1.0f);

                    gradient.SetKeys(colorKeys, alphaKeys);
                    tr.colorGradient = gradient;

                    Debug.Log($"[PlayerSkinManager] Set TrailRenderer color to {keyColor} for skin {SkinNames[selectedSkinIndex]}");
                }
            }

            // Disable any colliders on the skin (we use the player's SphereCollider)
            Collider[] skinColliders = skinVisualInstance.GetComponentsInChildren<Collider>();
            foreach (Collider col in skinColliders)
            {
                col.enabled = false;
            }

            // Disable any Rigidbodies on the skin
            Rigidbody[] skinRigidbodies = skinVisualInstance.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in skinRigidbodies)
            {
                Destroy(rb);
            }

            Debug.Log($"[PlayerSkinManager] Applied skin: {SkinNames[selectedSkinIndex]}");
        }

        /// <summary>
        /// Adjusts the skin instance scale so it visually matches
        /// the original player sphere size.
        /// </summary>
        private void AdjustSkinScale(GameObject skinInstance, GameObject playerObject)
        {
            // Get the bounds of the skin
            Renderer[] renderers = skinInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            // Target diameter: match the SphereCollider diameter (radius 0.5 * 2 = 1 unit in local space)
            float targetDiameter = 1.0f;
            float currentMaxDimension = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);

            if (currentMaxDimension > 0.001f)
            {
                // Account for the player's scale (0.8)
                float playerScale = playerObject.transform.localScale.x;
                float scaleFactor = (targetDiameter / currentMaxDimension) * playerScale;
                skinInstance.transform.localScale = Vector3.one * scaleFactor;
            }
        }
    }
}
