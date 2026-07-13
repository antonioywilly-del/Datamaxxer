using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Genera múltiples segmentos de tubo cilíndrico procedural con normales invertidas
    /// y los recicla infinitamente conforme el jugador avanza.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TubeGenerator : MonoBehaviour
    {
        [Header("Dimensiones del Tubo")]
        [Tooltip("Radio interior del tubo (debe coincidir con el tubeRadius del CylindricalPlayerController).")]
        [SerializeField] private float radius = 5f;

        [Tooltip("Longitud de cada segmento de tubo a lo largo del eje Z.")]
        [SerializeField] private float segmentLength = 100f;

        [Tooltip("Número de segmentos de tubo activos (se reciclan circularmente).")]
        [SerializeField] private int segmentCount = 4;

        [Header("Resolución de la Malla")]
        [Tooltip("Número de segmentos radiales (más = tubo más suave).")]
        [SerializeField] private int radialSegments = 32;

        [Tooltip("Número de segmentos a lo largo de cada segmento de tubo.")]
        [SerializeField] private int lengthSegments = 20;

        [Header("Visual")]
        [Tooltip("Si es true, regenera la malla cada vez que se modifican los parámetros en el Inspector.")]
        [SerializeField] private bool autoRegenerate = true;

        [Tooltip("Material to use for tube segments.")]
        [SerializeField] private Material tubeMaterial;

        // ──────────────────────────────────────────────
        // INTERNAL
        // ──────────────────────────────────────────────

        private GameObject[] segments;
        private Mesh sharedTubeMesh;
        private Transform playerTransform;
        private float furthestSegmentEndZ;

        private void Awake()
        {
            // Create the shared mesh once
            sharedTubeMesh = GenerateTubeMesh();

            // Get material from this object's renderer if not assigned
            if (tubeMaterial == null)
            {
                var renderer = GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    tubeMaterial = renderer.sharedMaterial;
                }
            }

            // Hide the original object's mesh (we'll use child segments)
            var mf = GetComponent<MeshFilter>();
            if (mf != null) mf.mesh = null;
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            // Create segments
            CreateSegments();
        }

        private void Start()
        {
            // Find player
            var player = FindFirstObjectByType<CylindricalPlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            // Check if we need to recycle segments
            RecycleSegments();
        }

        private void OnValidate()
        {
            if (autoRegenerate && Application.isPlaying && sharedTubeMesh != null)
            {
                sharedTubeMesh = GenerateTubeMesh();
                UpdateAllSegmentMeshes();
            }
        }

        // ──────────────────────────────────────────────
        // SEGMENT MANAGEMENT
        // ──────────────────────────────────────────────

        private void CreateSegments()
        {
            segments = new GameObject[segmentCount];

            for (int i = 0; i < segmentCount; i++)
            {
                GameObject seg = new GameObject($"TubeSegment_{i}");
                seg.transform.SetParent(transform);
                seg.transform.localPosition = new Vector3(0f, 0f, i * segmentLength);

                var mf = seg.AddComponent<MeshFilter>();
                mf.sharedMesh = sharedTubeMesh;

                var mr = seg.AddComponent<MeshRenderer>();
                if (tubeMaterial != null)
                {
                    mr.sharedMaterial = tubeMaterial;
                }

                // Disable shadows for interior tube (performance)
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                segments[i] = seg;
            }

            furthestSegmentEndZ = segmentCount * segmentLength;
        }

        /// <summary>
        /// Recycles segments that the player has passed, moving them to the front.
        /// </summary>
        private void RecycleSegments()
        {
            float playerZ = playerTransform.position.z;

            // Check each segment; if the player is past its end, move it ahead
            for (int i = 0; i < segmentCount; i++)
            {
                float segEndZ = segments[i].transform.position.z + segmentLength;

                // If the player is well past this segment (buffer so we don't see it disappear)
                if (playerZ > segEndZ + 10f)
                {
                    // Move this segment to the front
                    segments[i].transform.position = new Vector3(0f, 0f, furthestSegmentEndZ);
                    furthestSegmentEndZ += segmentLength;
                }
            }
        }

        private void UpdateAllSegmentMeshes()
        {
            if (segments == null) return;
            foreach (var seg in segments)
            {
                if (seg != null)
                {
                    var mf = seg.GetComponent<MeshFilter>();
                    if (mf != null) mf.sharedMesh = sharedTubeMesh;
                }
            }
        }

        // ──────────────────────────────────────────────
        // MESH GENERATION
        // ──────────────────────────────────────────────

        /// <summary>
        /// Genera la malla de un segmento de tubo con normales invertidas.
        /// </summary>
        private Mesh GenerateTubeMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "FiberOpticTubeSegment";

            int vertexCount = (radialSegments + 1) * (lengthSegments + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[radialSegments * lengthSegments * 6];

            // Generar vértices
            for (int z = 0; z <= lengthSegments; z++)
            {
                float zPos = ((float)z / lengthSegments) * segmentLength;
                float vCoord = (float)z / lengthSegments;

                for (int r = 0; r <= radialSegments; r++)
                {
                    float angle = ((float)r / radialSegments) * Mathf.PI * 2f;
                    float uCoord = (float)r / radialSegments;

                    int index = z * (radialSegments + 1) + r;

                    float x = Mathf.Cos(angle) * radius;
                    float y = Mathf.Sin(angle) * radius;

                    vertices[index] = new Vector3(x, y, zPos);

                    // Normales invertidas: apuntando hacia el centro del tubo
                    normals[index] = new Vector3(-Mathf.Cos(angle), -Mathf.Sin(angle), 0f);

                    // UVs: U recorre la circunferencia, V recorre la longitud
                    uvs[index] = new Vector2(uCoord * 4f, vCoord * (segmentLength / 10f));
                }
            }

            // Generar triángulos (orden invertido para caras interiores)
            int triIndex = 0;
            for (int z = 0; z < lengthSegments; z++)
            {
                for (int r = 0; r < radialSegments; r++)
                {
                    int current = z * (radialSegments + 1) + r;
                    int next = current + radialSegments + 1;

                    // Triángulo 1
                    triangles[triIndex++] = current;
                    triangles[triIndex++] = current + 1;
                    triangles[triIndex++] = next;

                    // Triángulo 2
                    triangles[triIndex++] = current + 1;
                    triangles[triIndex++] = next + 1;
                    triangles[triIndex++] = next;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }

        // ──────────────────────────────────────────────
        // PUBLIC PROPERTIES
        // ──────────────────────────────────────────────

        /// <summary>
        /// Devuelve el radio del tubo para que otros scripts puedan consultarlo.
        /// </summary>
        public float Radius => radius;

        /// <summary>
        /// Devuelve la longitud de un segmento del tubo.
        /// </summary>
        public float Length => segmentLength;

        // ──────────────────────────────────────────────
        // GIZMOS
        // ──────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            float totalLength = segmentLength * segmentCount;

            // Visualizar el inicio y fin del tubo
            Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
            Vector3 start = transform.position;
            Vector3 end = transform.position + Vector3.forward * totalLength;

            DrawGizmoCircle(start, radius, 36);
            DrawGizmoCircle(end, radius, 36);

            // Líneas longitudinales
            Gizmos.color = new Color(0f, 1f, 1f, 0.08f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                Gizmos.DrawLine(start + offset, end + offset);
            }

            // Segment boundaries
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            for (int i = 1; i < segmentCount; i++)
            {
                Vector3 segStart = start + Vector3.forward * (i * segmentLength);
                DrawGizmoCircle(segStart, radius, 36);
            }
        }

        private void DrawGizmoCircle(Vector3 center, float r, int circleSegments)
        {
            for (int i = 0; i < circleSegments; i++)
            {
                float a1 = ((float)i / circleSegments) * Mathf.PI * 2f;
                float a2 = ((float)(i + 1) / circleSegments) * Mathf.PI * 2f;
                Vector3 p1 = center + new Vector3(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0f);
                Vector3 p2 = center + new Vector3(Mathf.Cos(a2) * r, Mathf.Sin(a2) * r, 0f);
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}
