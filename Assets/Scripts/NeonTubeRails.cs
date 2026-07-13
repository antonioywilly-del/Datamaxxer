using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Creates neon glowing rail lines along the inside of the fiber optic tube.
    /// These run parallel to the tube axis, adding vibrant color and depth.
    /// </summary>
    public class NeonTubeRails : MonoBehaviour
    {
        [Header("Shader Configuration")]
        [Tooltip("Shader to use for the rails. If null, will fallback to searching.")]
        [SerializeField] private Shader railShader;

        [Header("Configuration")]
        [Tooltip("Radius of the tube (must match TubeGenerator).")]
        [SerializeField] private float tubeRadius = 4.95f;

        [Tooltip("Number of neon rails around the tube circumference.")]
        [SerializeField] private int railCount = 8;

        [Tooltip("Length of each rail segment.")]
        [SerializeField] private float railLength = 200f;

        [Tooltip("Number of rail segment groups (recycled like tube segments).")]
        [SerializeField] private int segmentCount = 3;

        [Header("Visual")]
        [Tooltip("Width of each rail line.")]
        [SerializeField] private float railWidth = 0.06f;

        [Tooltip("Colors for the rails (cycled).")]
        [SerializeField] private Color[] railColors = new Color[]
        {
            new Color(0f, 0.5f, 1f, 1f),    // Azul Eléctrico
            new Color(0.6f, 0f, 1f, 1f),    // Morado Neón
            new Color(0f, 1f, 0.4f, 1f)     // Verde Eléctrico
        };

        private void Awake()
        {
            // Forzar la paleta de colores: Azul, Morado y Verde
            railColors = new Color[]
            {
                new Color(0f, 0.5f, 1f, 1f),    // Azul Eléctrico
                new Color(0.6f, 0f, 1f, 1f),    // Morado Neón
                new Color(0f, 1f, 0.4f, 1f)     // Verde Eléctrico
            };
        }

        [Tooltip("HDR emission intensity multiplier for rail glow.")]
        [SerializeField] private float emissionIntensity = 4f;

        // Internal
        private Transform playerTransform;
        private LineRenderer[][] railSegments; // [segment][rail]
        private float[] segmentStartZ;
        private float furthestSegmentEndZ;
        private Material[] railMaterials;

        private void Start()
        {
            var player = FindFirstObjectByType<CylindricalPlayerController>();
            if (player != null)
                playerTransform = player.transform;

            CreateRailMaterials();
            CreateRailSegments();
        }

        private void Update()
        {
            if (playerTransform == null) return;
            RecycleRailSegments();
        }

        private void CreateRailMaterials()
        {
            // Create one shared material per rail color
            railMaterials = new Material[railCount];
            var shader = railShader != null ? railShader : Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            for (int i = 0; i < railCount; i++)
            {
                Color col = railColors[i % railColors.Length];

                var mat = new Material(shader);
                mat.name = $"NeonRail_Mat_{i}";
                mat.color = col;

                // Enable emission for bloom to pick up
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", col * emissionIntensity);

                // Make it additive-like
                mat.SetFloat("_Surface", 0); // Opaque
                mat.renderQueue = 3000;

                railMaterials[i] = mat;
            }
        }

        private void CreateRailSegments()
        {
            railSegments = new LineRenderer[segmentCount][];
            segmentStartZ = new float[segmentCount];

            for (int s = 0; s < segmentCount; s++)
            {
                float startZ = s * railLength;
                segmentStartZ[s] = startZ;

                railSegments[s] = new LineRenderer[railCount];

                GameObject segGroup = new GameObject($"RailSegment_{s}");
                segGroup.transform.SetParent(transform);
                segGroup.transform.localPosition = Vector3.zero;

                for (int r = 0; r < railCount; r++)
                {
                    float angle = ((float)r / railCount) * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * tubeRadius;
                    float y = Mathf.Sin(angle) * tubeRadius;

                    GameObject railObj = new GameObject($"Rail_{r}");
                    railObj.transform.SetParent(segGroup.transform);

                    var lr = railObj.AddComponent<LineRenderer>();
                    lr.material = railMaterials[r % railMaterials.Length];
                    lr.startWidth = railWidth;
                    lr.endWidth = railWidth;
                    lr.positionCount = 2;
                    lr.useWorldSpace = true;
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lr.receiveShadows = false;

                    // Set start and end points
                    lr.SetPosition(0, new Vector3(x, y, startZ));
                    lr.SetPosition(1, new Vector3(x, y, startZ + railLength));

                    // Add subtle glow color variation
                    Color col = railColors[r % railColors.Length];
                    lr.startColor = col;
                    lr.endColor = col;

                    railSegments[s][r] = lr;
                }
            }

            furthestSegmentEndZ = segmentCount * railLength;
        }

        private void RecycleRailSegments()
        {
            float playerZ = playerTransform.position.z;

            for (int s = 0; s < segmentCount; s++)
            {
                float segEnd = segmentStartZ[s] + railLength;

                if (playerZ > segEnd + 20f)
                {
                    // Move this segment to the front
                    float newStartZ = furthestSegmentEndZ;
                    segmentStartZ[s] = newStartZ;
                    furthestSegmentEndZ += railLength;

                    // Update line positions
                    for (int r = 0; r < railCount; r++)
                    {
                        float angle = ((float)r / railCount) * Mathf.PI * 2f;
                        float x = Mathf.Cos(angle) * tubeRadius;
                        float y = Mathf.Sin(angle) * tubeRadius;

                        railSegments[s][r].SetPosition(0, new Vector3(x, y, newStartZ));
                        railSegments[s][r].SetPosition(1, new Vector3(x, y, newStartZ + railLength));
                    }
                }
            }
        }
    }
}
