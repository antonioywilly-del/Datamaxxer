using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Creates periodic neon ring markers inside the tube.
    /// These rings give the impression of data transfer checkpoints
    /// and add visual structure to the tunnel.
    /// </summary>
    public class NeonRingMarkers : MonoBehaviour
    {
        [Header("Shader Configuration")]
        [Tooltip("Shader to use for the rings. If null, will fallback to searching.")]
        [SerializeField] private Shader ringShader;

        [Header("Configuration")]
        [Tooltip("Tube radius (must match TubeGenerator).")]
        [SerializeField] private float tubeRadius = 4.9f;

        [Tooltip("Distance between rings along Z.")]
        [SerializeField] private float ringSpacing = 15f;

        [Tooltip("Number of ring segments visible ahead.")]
        [SerializeField] private int visibleRings = 12;

        [Header("Visual")]
        [Tooltip("Number of line segments per ring (higher = smoother).")]
        [SerializeField] private int ringResolution = 48;

        [Tooltip("Width of the ring lines.")]
        [SerializeField] private float ringWidth = 0.04f;

        [Tooltip("Ring glow intensity.")]
        [SerializeField] private float emissionIntensity = 5f;

        // Internal
        private Transform playerTransform;
        private LineRenderer[] rings;
        private float[] ringZ;
        private float furthestRingZ;
        private Material ringMaterial;

        private void Start()
        {
            var player = FindFirstObjectByType<CylindricalPlayerController>();
            if (player != null)
                playerTransform = player.transform;

            CreateRingMaterial();
            CreateRings();
        }

        private void Update()
        {
            if (playerTransform == null) return;

            float playerZ = playerTransform.position.z;

            for (int i = 0; i < rings.Length; i++)
            {
                // Recycle rings that are behind the player
                if (playerZ > ringZ[i] + 10f)
                {
                    ringZ[i] = furthestRingZ;
                    furthestRingZ += ringSpacing;
                    UpdateRingPositions(i);

                    // Randomize color for variety
                    float hue = Random.Range(0f, 1f);
                    // Bias toward cyan, magenta, green range
                    float[] hueOptions = { 0.5f, 0.85f, 0.33f, 0.75f, 0.15f };
                    hue = hueOptions[Random.Range(0, hueOptions.Length)] + Random.Range(-0.05f, 0.05f);
                    Color col = Color.HSVToRGB(hue % 1f, 0.9f, 1f);
                    rings[i].startColor = col;
                    rings[i].endColor = col;
                    rings[i].material.SetColor("_EmissionColor", col * emissionIntensity);
                    rings[i].material.color = col;
                }
            }
        }

        private void CreateRingMaterial()
        {
            var shader = ringShader != null ? ringShader : Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            ringMaterial = new Material(shader);
            ringMaterial.name = "NeonRing_Mat";
            ringMaterial.color = new Color(0f, 1f, 1f);
            ringMaterial.EnableKeyword("_EMISSION");
            ringMaterial.SetColor("_EmissionColor", new Color(0f, 1f, 1f) * emissionIntensity);
        }

        private void CreateRings()
        {
            rings = new LineRenderer[visibleRings];
            ringZ = new float[visibleRings];

            for (int i = 0; i < visibleRings; i++)
            {
                float z = i * ringSpacing;
                ringZ[i] = z;

                GameObject ringObj = new GameObject($"NeonRing_{i}");
                ringObj.transform.SetParent(transform);

                var lr = ringObj.AddComponent<LineRenderer>();
                lr.positionCount = ringResolution + 1;
                lr.loop = true;
                lr.useWorldSpace = true;
                lr.startWidth = ringWidth;
                lr.endWidth = ringWidth;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;

                // Clone material for independent color
                lr.material = new Material(ringMaterial);

                // Randomize initial color
                float hue = Random.Range(0f, 1f);
                Color col = Color.HSVToRGB(hue, 0.9f, 1f);
                lr.startColor = col;
                lr.endColor = col;
                lr.material.color = col;
                lr.material.SetColor("_EmissionColor", col * emissionIntensity);

                rings[i] = lr;
                UpdateRingPositions(i);
            }

            furthestRingZ = visibleRings * ringSpacing;
        }

        private void UpdateRingPositions(int index)
        {
            float z = ringZ[index];
            var lr = rings[index];

            for (int j = 0; j <= ringResolution; j++)
            {
                float angle = ((float)j / ringResolution) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * tubeRadius;
                float y = Mathf.Sin(angle) * tubeRadius;
                lr.SetPosition(j, new Vector3(x, y, z));
            }
        }
    }
}
