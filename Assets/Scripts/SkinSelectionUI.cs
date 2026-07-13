using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Datamaxxer.UI
{
    /// <summary>
    /// UI controller for the skin selection panel in the main menu.
    /// Dynamically generates a grid of skin preview buttons and handles selection.
    /// </summary>
    public class SkinSelectionUI : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // CONFIGURATION
        // ──────────────────────────────────────────────

        [Header("Panel References")]
        [SerializeField] private GameObject skinsPanel;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Prefab Template")]
        [SerializeField] private GameObject skinButtonTemplate;

        [Header("Visual Settings")]
        [SerializeField] private Color normalBorderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color selectedBorderColor = new Color(0f, 1f, 1f, 1f); // Cyan
        [SerializeField] private Color hoverBorderColor = new Color(1f, 0f, 1f, 1f); // Magenta
        [SerializeField] private int borderWidth = 4;

        // ──────────────────────────────────────────────
        // STATE
        // ──────────────────────────────────────────────

        private Button[] skinButtons;
        private Image[] skinBorders;
        private Image[] skinPreviews;
        private int currentSelectedIndex = 0;
        private bool isInitialized = false;

        // ──────────────────────────────────────────────
        // LIFECYCLE
        // ──────────────────────────────────────────────

        private void OnEnable()
        {
            if (!isInitialized)
            {
                InitializeGrid();
                isInitialized = true;
            }
            RefreshSelection();
        }

        // ──────────────────────────────────────────────
        // INITIALIZATION
        // ──────────────────────────────────────────────

        /// <summary>
        /// Builds the skin grid dynamically from PlayerSkinManager's catalog.
        /// </summary>
        private void InitializeGrid()
        {
            if (Datamaxxer.Gameplay.PlayerSkinManager.Instance == null)
            {
                Debug.LogError("[SkinSelectionUI] PlayerSkinManager instance not found!");
                return;
            }

            int skinCount = Datamaxxer.Gameplay.PlayerSkinManager.Instance.SkinCount;
            skinButtons = new Button[skinCount];
            skinBorders = new Image[skinCount];
            skinPreviews = new Image[skinCount];

            // Hide template
            if (skinButtonTemplate != null)
            {
                skinButtonTemplate.SetActive(false);
            }

            for (int i = 0; i < skinCount; i++)
            {
                GameObject buttonObj = CreateSkinButton(i);
                skinButtons[i] = buttonObj.GetComponent<Button>();
                skinBorders[i] = buttonObj.GetComponent<Image>();

                // Find the preview image (child named "Preview")
                Transform previewTransform = buttonObj.transform.Find("Preview");
                if (previewTransform != null)
                {
                    skinPreviews[i] = previewTransform.GetComponent<Image>();
                }

                // Set up click event
                int index = i; // Capture for closure
                skinButtons[i].onClick.AddListener(() => OnSkinSelected(index));

                // Set up hover events via TerminalSkinButton helper
                AddHoverEffect(buttonObj, i);
            }

            // Load the current selection
            currentSelectedIndex = Datamaxxer.Gameplay.PlayerSkinManager.Instance.SelectedSkinIndex;
        }

        /// <summary>
        /// Creates a single skin button with border and preview image.
        /// </summary>
        private GameObject CreateSkinButton(int index)
        {
            GameObject buttonObj;

            if (skinButtonTemplate != null)
            {
                buttonObj = Instantiate(skinButtonTemplate, gridContainer);
                buttonObj.SetActive(true);
            }
            else
            {
                // Create button from scratch
                buttonObj = new GameObject($"SkinButton_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObj.transform.SetParent(gridContainer, false);
            }

            buttonObj.name = $"SkinButton_{index}";

            // Configure border image
            Image borderImage = buttonObj.GetComponent<Image>();
            if (borderImage == null) borderImage = buttonObj.AddComponent<Image>();
            borderImage.color = normalBorderColor;

            // Configure button
            Button button = buttonObj.GetComponent<Button>();
            if (button == null) button = buttonObj.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            // Create preview image child
            Transform existingPreview = buttonObj.transform.Find("Preview");
            GameObject previewObj;
            if (existingPreview != null)
            {
                previewObj = existingPreview.gameObject;
            }
            else
            {
                previewObj = new GameObject("Preview", typeof(RectTransform), typeof(Image));
                previewObj.transform.SetParent(buttonObj.transform, false);
            }

            RectTransform previewRect = previewObj.GetComponent<RectTransform>();
            previewRect.anchorMin = Vector2.zero;
            previewRect.anchorMax = Vector2.one;
            previewRect.offsetMin = new Vector2(borderWidth, borderWidth);
            previewRect.offsetMax = new Vector2(-borderWidth, -borderWidth);

            Image previewImage = previewObj.GetComponent<Image>();
            if (previewImage == null) previewImage = previewObj.AddComponent<Image>();
            previewImage.color = new Color(0.05f, 0.05f, 0.1f, 1f); // Dark background

            // Try to generate a preview from the prefab
            GeneratePreview(previewImage, index);

            // Create label
            Transform existingLabel = buttonObj.transform.Find("Label");
            GameObject labelObj;
            if (existingLabel != null)
            {
                labelObj = existingLabel.gameObject;
            }
            else
            {
                labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObj.transform.SetParent(buttonObj.transform, false);
            }

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0.25f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
            if (labelText == null) labelText = labelObj.AddComponent<TextMeshProUGUI>();

            // Extract a short display name from the skin name
            string skinName = Datamaxxer.Gameplay.PlayerSkinManager.Instance.GetSkinName(index);
            string displayName = skinName.Replace("RollingBalls_Sci-fi_", "");
            labelText.text = displayName;
            labelText.fontSize = 14;
            labelText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 8;
            labelText.fontSizeMax = 14;

            return buttonObj;
        }

        /// <summary>
        /// Generates a preview for the skin by loading the prefab and extracting its texture.
        /// </summary>
        private void GeneratePreview(Image previewImage, int index)
        {
            string skinName = Datamaxxer.Gameplay.PlayerSkinManager.Instance.GetSkinName(index);
            Sprite previewSprite = Resources.Load<Sprite>($"PlayerSkins/Previews/{skinName}_Preview");
            if (previewSprite != null)
            {
                previewImage.sprite = previewSprite;
                previewImage.color = Color.white;
                previewImage.preserveAspect = true;
                return;
            }

            GameObject prefab = Datamaxxer.Gameplay.PlayerSkinManager.Instance.GetSkinPrefab(index);
            if (prefab == null) return;

            // Try to get the main texture from the prefab's renderer
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials != null)
                {
                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        if (mat != null && mat.mainTexture != null)
                        {
                            // Use the material's main texture as preview
                            Texture2D tex = mat.mainTexture as Texture2D;
                            if (tex != null)
                            {
                                previewImage.sprite = Sprite.Create(
                                    tex,
                                    new Rect(0, 0, tex.width, tex.height),
                                    new Vector2(0.5f, 0.5f)
                                );
                                previewImage.color = Color.white;
                                previewImage.preserveAspect = true;
                                return;
                            }
                        }
                    }
                }
            }

            // Fallback: use the material color
            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterial != null)
                {
                    Color matColor = renderer.sharedMaterial.color;
                    previewImage.color = matColor;
                    return;
                }
            }
        }

        // ──────────────────────────────────────────────
        // HOVER EFFECTS
        // ──────────────────────────────────────────────

        private void AddHoverEffect(GameObject buttonObj, int index)
        {
            SkinButtonHover hover = buttonObj.GetComponent<SkinButtonHover>();
            if (hover == null) hover = buttonObj.AddComponent<SkinButtonHover>();
            hover.Initialize(this, index);
        }

        /// <summary>Called by SkinButtonHover when the pointer enters.</summary>
        public void OnSkinHoverEnter(int index)
        {
            if (index != currentSelectedIndex && skinBorders[index] != null)
            {
                skinBorders[index].color = hoverBorderColor;
            }
        }

        /// <summary>Called by SkinButtonHover when the pointer exits.</summary>
        public void OnSkinHoverExit(int index)
        {
            if (index != currentSelectedIndex && skinBorders[index] != null)
            {
                skinBorders[index].color = normalBorderColor;
            }
        }

        // ──────────────────────────────────────────────
        // SELECTION
        // ──────────────────────────────────────────────

        private void OnSkinSelected(int index)
        {
            if (Datamaxxer.Gameplay.PlayerSkinManager.Instance == null) return;

            // Deselect previous
            if (currentSelectedIndex >= 0 && currentSelectedIndex < skinBorders.Length && skinBorders[currentSelectedIndex] != null)
            {
                skinBorders[currentSelectedIndex].color = normalBorderColor;
            }

            // Select new
            currentSelectedIndex = index;
            Datamaxxer.Gameplay.PlayerSkinManager.Instance.SelectSkin(index);

            // Highlight new selection
            if (skinBorders[currentSelectedIndex] != null)
            {
                skinBorders[currentSelectedIndex].color = selectedBorderColor;
            }

            Debug.Log($"[SkinSelectionUI] Skin {index} selected: {Datamaxxer.Gameplay.PlayerSkinManager.Instance.GetSkinName(index)}");
        }

        /// <summary>
        /// Refreshes the visual state to match the current selection.
        /// </summary>
        private void RefreshSelection()
        {
            if (Datamaxxer.Gameplay.PlayerSkinManager.Instance != null)
            {
                currentSelectedIndex = Datamaxxer.Gameplay.PlayerSkinManager.Instance.SelectedSkinIndex;
            }

            if (skinBorders == null) return;

            for (int i = 0; i < skinBorders.Length; i++)
            {
                if (skinBorders[i] != null)
                {
                    skinBorders[i].color = (i == currentSelectedIndex) ? selectedBorderColor : normalBorderColor;
                }
            }
        }

        // ──────────────────────────────────────────────
        // PUBLIC PANEL CONTROL
        // ──────────────────────────────────────────────

        public void ShowPanel()
        {
            if (skinsPanel != null) skinsPanel.SetActive(true);
        }

        public void HidePanel()
        {
            if (skinsPanel != null) skinsPanel.SetActive(false);
        }
    }

    // ──────────────────────────────────────────────
    // HOVER HELPER (Separate component for EventTrigger)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Helper component added to each skin button to handle hover events.
    /// </summary>
    public class SkinButtonHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        private SkinSelectionUI owner;
        private int skinIndex;

        public void Initialize(SkinSelectionUI ui, int index)
        {
            owner = ui;
            skinIndex = index;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (owner != null) owner.OnSkinHoverEnter(skinIndex);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (owner != null) owner.OnSkinHoverExit(skinIndex);
        }
    }
}
