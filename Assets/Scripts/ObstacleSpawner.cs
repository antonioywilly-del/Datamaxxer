using UnityEngine;
using System.Collections.Generic;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Spawns obstacles ahead of the player inside the fiber optic tube.
    /// Uses object pooling and places obstacles at lane positions on the tube wall.
    /// Difficulty ramps aggressively: tighter spacing, more complex patterns, faster pace.
    /// Now includes highly visible obstacles and a massive spinning Gear Obstacle.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // CONFIGURATION
        // ──────────────────────────────────────────────

        [Header("Tube Configuration")]
        [Tooltip("Radius of the tube (must match TubeGenerator and PlayerController).")]
        [SerializeField] private float tubeRadius = 5.0f;

        [Tooltip("Number of lanes (must match PlayerController).")]
        [SerializeField] private int laneCount = 6;

        [Header("Spawn Configuration")]
        [Tooltip("How far ahead of the player to spawn obstacles.")]
        [SerializeField] private float spawnAheadDistance = 90f;

        [Tooltip("Minimum distance between consecutive obstacles along Z.")]
        [SerializeField] private float minObstacleSpacing = 15f;

        [Tooltip("Maximum distance between consecutive obstacles along Z.")]
        [SerializeField] private float maxObstacleSpacing = 22f;

        [Tooltip("Initial grace period (Z distance) before first obstacle.")]
        [SerializeField] private float initialGracePeriod = 25f;

        [Header("Difficulty Scaling")]
        [Tooltip("Minimum spacing decreases over time. This is the floor.")]
        [SerializeField] private float hardMinSpacing = 6f;

        [Tooltip("How quickly spacing tightens (units per second).")]
        [SerializeField] private float difficultyRampRate = 0.08f;

        [Tooltip("Time (seconds) at which the hardest difficulty is reached.")]
        [SerializeField] private float maxDifficultyTime = 120f;

        [Header("Pool Settings")]
        [Tooltip("Max number of obstacles in the pool.")]
        [SerializeField] private int poolSize = 60;

        [Header("Barrier Materials (Single / Double / Triple / Ring)")]
        [Tooltip("Material applied to barrier obstacles.")]
        [SerializeField] private Material barrierMaterial;
        [Tooltip("Base color for barrier obstacles.")]
        [SerializeField] private Color barrierColor = new Color(1f, 0f, 0.6f);
        [Tooltip("Emission intensity multiplier for barriers.")]
        [SerializeField] private float barrierEmission = 8f;

        [Header("Spike Materials")]
        [Tooltip("Material applied to surface spike obstacles.")]
        [SerializeField] private Material spikeMaterial;
        [Tooltip("Base color for spike obstacles.")]
        [SerializeField] private Color spikeColor = new Color(0.2f, 1f, 0.3f);
        [Tooltip("Emission intensity multiplier for spikes.")]
        [SerializeField] private float spikeEmission = 8f;

        [Header("Gear Obstacle Materials")]
        [Tooltip("Material applied to the gear rim segments.")]
        [SerializeField] private Material gearRimMaterial;
        [Tooltip("Color for the gear rim.")]
        [SerializeField] private Color gearRimColor = new Color(1f, 0.45f, 0f);
        [Tooltip("Emission intensity multiplier for gear rim.")]
        [SerializeField] private float gearRimEmission = 6f;
        [Tooltip("Material applied to the gear teeth.")]
        [SerializeField] private Material gearToothMaterial;
        [Tooltip("Color for the gear teeth.")]
        [SerializeField] private Color gearToothColor = new Color(1f, 0.15f, 0f);
        [Tooltip("Emission intensity multiplier for gear teeth.")]
        [SerializeField] private float gearToothEmission = 6f;
        [Tooltip("Color for the gear center glow light.")]
        [SerializeField] private Color gearGlowColor = new Color(1f, 0.45f, 0f);

        [Header("Wall Obstacle Materials")]
        [Tooltip("Material applied to the outer wall segments.")]
        [SerializeField] private Material wallOuterMaterial;
        [Tooltip("Material applied to the inner wall segments.")]
        [SerializeField] private Material wallInnerMaterial;
        [Tooltip("Material applied to the radial connector beams.")]
        [SerializeField] private Material wallConnectorMaterial;
        [Tooltip("Base color for wall obstacle.")]
        [SerializeField] private Color wallColor = new Color(0.3f, 0.6f, 1f);
        [Tooltip("Emission intensity multiplier for wall outer segments.")]
        [SerializeField] private float wallOuterEmission = 6f;
        [Tooltip("Emission intensity multiplier for wall inner segments.")]
        [SerializeField] private float wallInnerEmission = 4f;
        [Tooltip("Emission intensity multiplier for wall connectors.")]
        [SerializeField] private float wallConnectorEmission = 3f;

        // ──────────────────────────────────────────────
        // INTERNAL STATE
        // ──────────────────────────────────────────────

        private Transform playerTransform;
        private float nextSpawnZ;
        private float laneAngleStep;
        private float elapsedTime = 0f;

        // Standard obstacle pool
        private List<GameObject> obstaclePool = new List<GameObject>();
        private int poolIndex = 0;

        // Gear obstacle pool
        private List<GameObject> gearPool = new List<GameObject>();
        private int gearPoolIndex = 0;
        private int gearPoolSize = 5;

        // Wall obstacle pool
        private List<GameObject> wallPool = new List<GameObject>();
        private int wallPoolIndex = 0;
        private int wallPoolSize = 5;

        // Player jump min radius (must match CylindricalPlayerController)
        private float jumpMinRadius = 1.5f;

        // ──────────────────────────────────────────────
        // LIFECYCLE
        // ──────────────────────────────────────────────

        private void Start()
        {
            laneAngleStep = 360f / laneCount;

            // Find the player
            var player = FindFirstObjectByType<CylindricalPlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }

            nextSpawnZ = initialGracePeriod;

            // Pre-create the pools
            CreatePool();
            CreateGearPool();
            CreateWallPool();
        }

        private void Update()
        {
            if (playerTransform == null) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            elapsedTime += Time.deltaTime;

            // Normalized difficulty (0 = start, 1 = max difficulty)
            float difficultyT = Mathf.Clamp01(elapsedTime / maxDifficultyTime);

            float playerZ = playerTransform.position.z;

            // Spawn obstacles ahead of the player
            while (nextSpawnZ < playerZ + spawnAheadDistance)
            {
                SpawnObstacleCluster(nextSpawnZ, difficultyT);

                // Calculate spacing with difficulty scaling — spacing shrinks over time
                float currentMinSpacing = Mathf.Lerp(minObstacleSpacing, hardMinSpacing, difficultyT);
                float currentMaxSpacing = Mathf.Lerp(maxObstacleSpacing, hardMinSpacing + 3f, difficultyT);
                float spacing = Random.Range(currentMinSpacing, currentMaxSpacing);
                nextSpawnZ += spacing;
            }
        }

        // ──────────────────────────────────────────────
        // POOL MANAGEMENT
        // ──────────────────────────────────────────────

        private void CreatePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obs = CreateObstaclePrefab();
                obs.SetActive(false);
                obs.transform.SetParent(transform);
                obstaclePool.Add(obs);
            }
        }

        private void CreateGearPool()
        {
            for (int i = 0; i < gearPoolSize; i++)
            {
                GameObject gear = CreateGearObstaclePrefab();
                gear.SetActive(false);
                gear.transform.SetParent(transform);
                gearPool.Add(gear);
            }
        }

        private void CreateWallPool()
        {
            for (int i = 0; i < wallPoolSize; i++)
            {
                GameObject wall = CreateWallObstaclePrefab();
                wall.SetActive(false);
                wall.transform.SetParent(transform);
                wallPool.Add(wall);
            }
        }

        private GameObject GetFromPool()
        {
            // Find an inactive obstacle
            for (int i = 0; i < obstaclePool.Count; i++)
            {
                int idx = (poolIndex + i) % obstaclePool.Count;
                if (!obstaclePool[idx].activeInHierarchy)
                {
                    poolIndex = (idx + 1) % obstaclePool.Count;
                    return obstaclePool[idx];
                }
            }

            // Pool exhausted — create a new one
            GameObject newObs = CreateObstaclePrefab();
            newObs.SetActive(false);
            newObs.transform.SetParent(transform);
            obstaclePool.Add(newObs);
            return newObs;
        }

        private GameObject GetGearFromPool()
        {
            for (int i = 0; i < gearPool.Count; i++)
            {
                int idx = (gearPoolIndex + i) % gearPool.Count;
                if (!gearPool[idx].activeInHierarchy)
                {
                    gearPoolIndex = (idx + 1) % gearPool.Count;
                    return gearPool[idx];
                }
            }

            GameObject newGear = CreateGearObstaclePrefab();
            newGear.SetActive(false);
            newGear.transform.SetParent(transform);
            gearPool.Add(newGear);
            return newGear;
        }

        private GameObject GetWallFromPool()
        {
            for (int i = 0; i < wallPool.Count; i++)
            {
                int idx = (wallPoolIndex + i) % wallPool.Count;
                if (!wallPool[idx].activeInHierarchy)
                {
                    wallPoolIndex = (idx + 1) % wallPool.Count;
                    return wallPool[idx];
                }
            }

            GameObject newWall = CreateWallObstaclePrefab();
            newWall.SetActive(false);
            newWall.transform.SetParent(transform);
            wallPool.Add(newWall);
            return newWall;
        }

        private GameObject CreateObstaclePrefab()
        {
            GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obs.name = "Obstacle";
            obs.tag = "Obstacle";
            obs.layer = 0;

            // Set up collider as trigger
            var collider = obs.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            // Add Obstacle component
            obs.AddComponent<Obstacle>();

            // Add a vibrant glow via a point light child
            GameObject glowLight = new GameObject("GlowLight");
            glowLight.transform.SetParent(obs.transform);
            glowLight.transform.localPosition = Vector3.zero;
            var light = glowLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 3.5f;
            light.color = barrierColor;
            light.shadows = LightShadows.None;

            return obs;
        }

        private GameObject CreateGearObstaclePrefab()
        {
            GameObject parent = new GameObject("GearObstacle");
            
            // Add Obstacle component (controls Z recycling & scores 5 MB)
            var obstacleComp = parent.AddComponent<Obstacle>();
            var mbField = typeof(Obstacle).GetField("mbValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mbField != null)
            {
                mbField.SetValue(obstacleComp, 5);
            }

            // Add GearObstacle component (controls Z-axis spinning)
            parent.AddComponent<GearObstacle>();

            // Dimensions: inner hole is radius 2.6f, outer edge is 5.0f
            float rWall = tubeRadius;
            float thickness = 2.4f;
            float rCenter = rWall - thickness / 2f; // 3.8f

            for (int i = 0; i < laneCount; i++)
            {
                float angle = i * laneAngleStep * Mathf.Deg2Rad;

                // 1. Rim Box (Outer Ring Segment)
                GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rim.name = $"GearRim_{i}";
                rim.tag = "Obstacle";
                rim.transform.SetParent(parent.transform);

                float x = Mathf.Cos(angle) * rCenter;
                float y = Mathf.Sin(angle) * rCenter;
                rim.transform.localPosition = new Vector3(x, y, 0f);
                rim.transform.localScale = new Vector3(3.2f, thickness, 0.8f);

                Vector3 dirToCenter = -new Vector3(x, y, 0f).normalized;
                rim.transform.localRotation = Quaternion.LookRotation(Vector3.forward, dirToCenter);

                var col = rim.GetComponent<BoxCollider>();
                if (col != null) col.isTrigger = true;

                var rend = rim.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    if (gearRimMaterial != null)
                    {
                        rend.material = gearRimMaterial;
                    }
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_BaseColor", gearRimColor);
                    rend.material.SetColor("_EmissionColor", gearRimColor * gearRimEmission);
                }

                // 2. Gear Tooth (Protrudes outwards/between lanes to look like a mechanical gear)
                GameObject tooth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tooth.name = $"GearTooth_{i}";
                tooth.tag = "Obstacle";
                tooth.transform.SetParent(parent.transform);

                float toothAngle = (i * laneAngleStep + laneAngleStep / 2f) * Mathf.Deg2Rad;
                float tx = Mathf.Cos(toothAngle) * (rWall - 0.2f);
                float ty = Mathf.Sin(toothAngle) * (rWall - 0.2f);
                tooth.transform.localPosition = new Vector3(tx, ty, 0f);
                tooth.transform.localScale = new Vector3(1.2f, 1.2f, 1.0f);
                tooth.transform.localRotation = Quaternion.LookRotation(Vector3.forward, -new Vector3(tx, ty, 0f).normalized);

                var tCol = tooth.GetComponent<BoxCollider>();
                if (tCol != null) tCol.isTrigger = true;

                var tRend = tooth.GetComponent<MeshRenderer>();
                if (tRend != null)
                {
                    if (gearToothMaterial != null)
                    {
                        tRend.material = gearToothMaterial;
                    }
                    tRend.material.EnableKeyword("_EMISSION");
                    tRend.material.SetColor("_BaseColor", gearToothColor);
                    tRend.material.SetColor("_EmissionColor", gearToothColor * gearToothEmission);
                }
            }

            // Glow point light at the center
            GameObject glowLight = new GameObject("GlowLight");
            glowLight.transform.SetParent(parent.transform);
            glowLight.transform.localPosition = Vector3.zero;
            var light = glowLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 4.5f;
            light.color = gearGlowColor;
            light.shadows = LightShadows.None;

            return parent;
        }

        private GameObject CreateWallObstaclePrefab()
        {
            GameObject parent = new GameObject("WallObstacle");

            // Add Obstacle component (scores 3 MB when dodged)
            var obstacleComp = parent.AddComponent<Obstacle>();
            var mbField = typeof(Obstacle).GetField("mbValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mbField != null)
            {
                mbField.SetValue(obstacleComp, 3);
            }

            // Add WallObstacle component (pulsating emission effect)
            parent.AddComponent<WallObstacle>();

            // Use configurable wall color

            // Build wall segments covering 3 consecutive lanes at BOTH radii
            // Outer segments (at tube wall) and Inner segments (at jump min radius)
            for (int i = 0; i < 3; i++)
            {
                // -- OUTER SEGMENT (at tube wall) --
                GameObject outerSeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                outerSeg.name = $"WallOuter_{i}";
                outerSeg.tag = "Obstacle";
                outerSeg.transform.SetParent(parent.transform);

                // Position will be set at spawn time; just configure shape
                float outerThickness = 2.4f;
                outerSeg.transform.localScale = new Vector3(3.2f, outerThickness, 0.8f);

                var oCol = outerSeg.GetComponent<BoxCollider>();
                if (oCol != null) oCol.isTrigger = true;

                var oRend = outerSeg.GetComponent<MeshRenderer>();
                if (oRend != null)
                {
                    if (wallOuterMaterial != null) oRend.material = wallOuterMaterial;
                    oRend.material.EnableKeyword("_EMISSION");
                    oRend.material.SetColor("_BaseColor", wallColor);
                    oRend.material.SetColor("_EmissionColor", wallColor * wallOuterEmission);
                }

                // -- INNER SEGMENT (at jump radius) --
                GameObject innerSeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                innerSeg.name = $"WallInner_{i}";
                innerSeg.tag = "Obstacle";
                innerSeg.transform.SetParent(parent.transform);

                float innerThickness = 1.6f;
                innerSeg.transform.localScale = new Vector3(2.0f, innerThickness, 0.8f);

                var iCol = innerSeg.GetComponent<BoxCollider>();
                if (iCol != null) iCol.isTrigger = true;

                var iRend = innerSeg.GetComponent<MeshRenderer>();
                if (iRend != null)
                {
                    if (wallInnerMaterial != null) iRend.material = wallInnerMaterial;
                    iRend.material.EnableKeyword("_EMISSION");
                    iRend.material.SetColor("_BaseColor", wallColor * 0.7f);
                    iRend.material.SetColor("_EmissionColor", wallColor * wallInnerEmission);
                }

                // -- RADIAL CONNECTOR (beam between outer and inner) --
                GameObject connector = GameObject.CreatePrimitive(PrimitiveType.Cube);
                connector.name = $"WallConnector_{i}";
                connector.tag = "Obstacle";
                connector.transform.SetParent(parent.transform);

                connector.transform.localScale = new Vector3(0.6f, tubeRadius - jumpMinRadius, 0.5f);

                var cCol = connector.GetComponent<BoxCollider>();
                if (cCol != null) cCol.isTrigger = true;

                var cRend = connector.GetComponent<MeshRenderer>();
                if (cRend != null)
                {
                    if (wallConnectorMaterial != null) cRend.material = wallConnectorMaterial;
                    cRend.material.EnableKeyword("_EMISSION");
                    cRend.material.SetColor("_BaseColor", wallColor * 0.5f);
                    cRend.material.SetColor("_EmissionColor", wallColor * wallConnectorEmission);
                }
            }

            // Glow point light at the center
            GameObject glowLight = new GameObject("GlowLight");
            glowLight.transform.SetParent(parent.transform);
            glowLight.transform.localPosition = Vector3.zero;
            var light = glowLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 10f;
            light.intensity = 5f;
            light.color = wallColor;
            light.shadows = LightShadows.None;

            return parent;
        }

        // ──────────────────────────────────────────────
        // OBSTACLE SPAWNING
        // ──────────────────────────────────────────────

        /// <summary>
        /// Spawn an obstacle cluster at a Z position. Pattern selection
        /// shifts towards harder patterns as difficultyT increases.
        /// </summary>
        private void SpawnObstacleCluster(float zPosition, float difficultyT)
        {
            float rand = Random.value;

            if (difficultyT < 0.2f)
            {
                // Easy: mostly singles and spikes, no gears
                if (rand < 0.6f)
                    SpawnSingleLaneBarrier(zPosition);
                else if (rand < 0.85f)
                    SpawnDoubleLaneBarrier(zPosition);
                else
                    SpawnSurfaceSpike(zPosition);
            }
            else if (difficultyT < 0.5f)
            {
                // Medium: more complex lanes + 10% gear obstacles + 8% wall obstacles
                if (rand < 0.08f)
                    SpawnWallObstacle(zPosition);
                else if (rand < 0.18f)
                    SpawnGearObstacle(zPosition);
                else if (rand < 0.40f)
                    SpawnSingleLaneBarrier(zPosition);
                else if (rand < 0.62f)
                    SpawnDoubleLaneBarrier(zPosition);
                else if (rand < 0.82f)
                    SpawnTripleLaneBarrier(zPosition);
                else
                    SpawnSurfaceSpike(zPosition);
            }
            else
            {
                // Hard: 20% gear obstacles, 12% wall obstacles, ring gates, triples
                if (rand < 0.12f)
                    SpawnWallObstacle(zPosition);
                else if (rand < 0.30f)
                    SpawnGearObstacle(zPosition);
                else if (rand < 0.43f)
                    SpawnTripleLaneBarrier(zPosition);
                else if (rand < 0.56f)
                    SpawnDoubleLaneBarrier(zPosition);
                else if (rand < 0.73f)
                    SpawnRingGate(zPosition, 2);
                else if (rand < 0.87f)
                    SpawnRingGate(zPosition, 1); // Only 1 lane gap
                else
                    SpawnSurfaceSpike(zPosition);
            }
        }

        private void SpawnSingleLaneBarrier(float zPos)
        {
            int lane = Random.Range(0, laneCount);
            SpawnBarrierAtLane(lane, zPos, new Vector3(3.0f, 2.2f, 0.8f));
        }

        private void SpawnDoubleLaneBarrier(float zPos)
        {
            int lane1 = Random.Range(0, laneCount);
            int lane2 = (lane1 + 1) % laneCount;
            SpawnBarrierAtLane(lane1, zPos, new Vector3(3.0f, 2.0f, 0.8f));
            SpawnBarrierAtLane(lane2, zPos, new Vector3(3.0f, 2.0f, 0.8f));
        }

        private void SpawnTripleLaneBarrier(float zPos)
        {
            int lane1 = Random.Range(0, laneCount);
            int lane2 = (lane1 + 1) % laneCount;
            int lane3 = (lane1 + 2) % laneCount;
            SpawnBarrierAtLane(lane1, zPos, new Vector3(3.2f, 2.2f, 0.8f));
            SpawnBarrierAtLane(lane2, zPos, new Vector3(3.2f, 2.2f, 0.8f));
            SpawnBarrierAtLane(lane3, zPos, new Vector3(3.2f, 2.2f, 0.8f));
        }

        private void SpawnSurfaceSpike(float zPos)
        {
            int lane = Random.Range(0, laneCount);
            float angle = lane * laneAngleStep * Mathf.Deg2Rad;

            // Scale config: Y is radial height protruding inward
            float scaleY = 3.2f;
            float rCenter = tubeRadius - scaleY / 2f;

            float x = Mathf.Cos(angle) * rCenter;
            float y = Mathf.Sin(angle) * rCenter;

            GameObject obs = GetFromPool();
            obs.transform.position = new Vector3(x, y, zPos);
            obs.transform.localScale = new Vector3(0.8f, scaleY, 0.8f);

            // Orient pointing toward center
            Vector3 dirToCenter = -new Vector3(x, y, 0f).normalized;
            obs.transform.rotation = Quaternion.LookRotation(Vector3.forward, dirToCenter);

            // Apply spike material and custom glow
            var renderer = obs.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (spikeMaterial != null)
                {
                    renderer.material = spikeMaterial;
                }
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_BaseColor", spikeColor);
                renderer.material.SetColor("_EmissionColor", spikeColor * spikeEmission);
            }

            // Update glow light color and intensity
            var light = obs.GetComponentInChildren<Light>();
            if (light != null)
            {
                light.color = spikeColor;
                light.range = 5f;
                light.intensity = 3.5f;
            }

            var obstacleComp = obs.GetComponent<Obstacle>();
            obstacleComp?.Initialize(playerTransform);
            obstacleComp?.ResetObstacle();

            obs.SetActive(true);
        }

        private void SpawnRingGate(float zPos, int gapSize)
        {
            int gapStart = Random.Range(0, laneCount);

            HashSet<int> openLanes = new HashSet<int>();
            for (int g = 0; g < gapSize; g++)
            {
                openLanes.Add((gapStart + g) % laneCount);
            }

            for (int i = 0; i < laneCount; i++)
            {
                if (openLanes.Contains(i)) continue;
                SpawnBarrierAtLane(i, zPos, new Vector3(3.4f, 2.2f, 0.8f));
            }
        }

        private void SpawnBarrierAtLane(int lane, float zPos, Vector3 scale)
        {
            float angle = lane * laneAngleStep * Mathf.Deg2Rad;

            // Center position based on radial height to sit flush on wall
            float rCenter = tubeRadius - scale.y / 2f;
            float x = Mathf.Cos(angle) * rCenter;
            float y = Mathf.Sin(angle) * rCenter;

            GameObject obs = GetFromPool();
            obs.transform.position = new Vector3(x, y, zPos);
            obs.transform.localScale = scale;

            Vector3 dirToCenter = -new Vector3(x, y, 0f).normalized;
            obs.transform.rotation = Quaternion.LookRotation(Vector3.forward, dirToCenter);

            // Apply barrier material and custom glow
            var renderer = obs.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (barrierMaterial != null)
                {
                    renderer.material = barrierMaterial;
                }
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_BaseColor", barrierColor);
                renderer.material.SetColor("_EmissionColor", barrierColor * barrierEmission);
            }

            var light = obs.GetComponentInChildren<Light>();
            if (light != null)
            {
                light.color = barrierColor;
                light.range = 5f;
                light.intensity = 3.5f;
            }

            var obstacleComp = obs.GetComponent<Obstacle>();
            obstacleComp?.Initialize(playerTransform);
            obstacleComp?.ResetObstacle();

            obs.SetActive(true);
        }

        private void SpawnGearObstacle(float zPos)
        {
            GameObject gear = GetGearFromPool();
            gear.transform.position = new Vector3(0f, 0f, zPos);
            gear.transform.rotation = Quaternion.identity;

            var obstacleComp = gear.GetComponent<Obstacle>();
            obstacleComp?.Initialize(playerTransform);
            obstacleComp?.ResetObstacle();

            gear.SetActive(true);
        }

        /// <summary>
        /// Spawns a wall obstacle covering 3 consecutive lanes at both outer and inner radii.
        /// The player can only dodge by being on the other 3 lanes.
        /// </summary>
        private void SpawnWallObstacle(float zPos)
        {
            GameObject wall = GetWallFromPool();
            wall.transform.position = new Vector3(0f, 0f, zPos);
            wall.transform.rotation = Quaternion.identity;

            // Pick a random starting lane for the 3-lane block
            int startLane = Random.Range(0, laneCount);

            // Position the child segments at the correct lane positions
            int segIndex = 0;
            for (int i = 0; i < 3; i++)
            {
                int lane = (startLane + i) % laneCount;
                float angle = lane * laneAngleStep * Mathf.Deg2Rad;

                // -- OUTER SEGMENT --
                float outerThickness = 2.4f;
                float outerRCenter = tubeRadius - outerThickness / 2f;
                float ox = Mathf.Cos(angle) * outerRCenter;
                float oy = Mathf.Sin(angle) * outerRCenter;

                Transform outerT = wall.transform.GetChild(segIndex * 3); // WallOuter_i
                outerT.localPosition = new Vector3(ox, oy, 0f);
                Vector3 outerDir = -new Vector3(ox, oy, 0f).normalized;
                outerT.localRotation = Quaternion.LookRotation(Vector3.forward, outerDir);

                // -- INNER SEGMENT --
                float innerThickness = 1.6f;
                float innerRCenter = jumpMinRadius + innerThickness / 2f;
                float ix = Mathf.Cos(angle) * innerRCenter;
                float iy = Mathf.Sin(angle) * innerRCenter;

                Transform innerT = wall.transform.GetChild(segIndex * 3 + 1); // WallInner_i
                innerT.localPosition = new Vector3(ix, iy, 0f);
                Vector3 innerDir = -new Vector3(ix, iy, 0f).normalized;
                innerT.localRotation = Quaternion.LookRotation(Vector3.forward, innerDir);

                // -- RADIAL CONNECTOR (between outer and inner) --
                float connectorRCenter = (outerRCenter + innerRCenter) / 2f;
                float cx = Mathf.Cos(angle) * connectorRCenter;
                float cy = Mathf.Sin(angle) * connectorRCenter;

                Transform connT = wall.transform.GetChild(segIndex * 3 + 2); // WallConnector_i
                connT.localPosition = new Vector3(cx, cy, 0f);
                Vector3 connDir = -new Vector3(cx, cy, 0f).normalized;
                connT.localRotation = Quaternion.LookRotation(Vector3.forward, connDir);

                segIndex++;
            }

            // Update the pulse effect color to match configuration
            var wallComp = wall.GetComponent<WallObstacle>();
            if (wallComp != null) wallComp.SetEmissionColor(wallColor);

            var obstacleComp = wall.GetComponent<Obstacle>();
            obstacleComp?.Initialize(playerTransform);
            obstacleComp?.ResetObstacle();

            wall.SetActive(true);
        }
    }
}
