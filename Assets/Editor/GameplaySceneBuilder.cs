#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Datamaxxer.Gameplay;

namespace Datamaxxer.Editor
{
    public class GameplaySceneBuilder : EditorWindow
    {
        [MenuItem("Datamaxxer/Build Gameplay Test Scene")]
        public static void BuildTestScene()
        {
            // ──────────────────────────────────────────────
            // LIMPIEZA: Eliminar instancias previas
            // ──────────────────────────────────────────────

            DestroyExisting("FiberOpticTube");
            DestroyExisting("DataPacket_Player");
            DestroyExisting("GameplayCamera");
            DestroyExisting("Directional Light - Gameplay");
            DestroyExisting("AmbientLight_Core");

            DestroyExisting("GameplayAudioManager");

            // ──────────────────────────────────────────────
            // PARÁMETROS COMPARTIDOS
            // ──────────────────────────────────────────────

            float tubeRadius = 5f;
            float tubeLength = 300f;

            // Configurar el ObstacleSpawner para sincronizar el radio
            ObstacleSpawner spawner = FindFirstObjectByType<ObstacleSpawner>();
            if (spawner != null)
            {
                SerializedObject spawnerSO = new SerializedObject(spawner);
                spawnerSO.FindProperty("tubeRadius").floatValue = tubeRadius;
                spawnerSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // ──────────────────────────────────────────────
            // 1. TUBO DE FIBRA ÓPTICA
            // ──────────────────────────────────────────────

            GameObject tubeGO = new GameObject("FiberOpticTube");
            tubeGO.transform.position = Vector3.zero;

            // Añadir componentes de malla
            MeshFilter tubeMF = tubeGO.AddComponent<MeshFilter>();
            MeshRenderer tubeMR = tubeGO.AddComponent<MeshRenderer>();
            TubeGenerator tubeGen = tubeGO.AddComponent<TubeGenerator>();

            // Configurar el TubeGenerator vía SerializedObject para respetar los campos privados
            SerializedObject tubeSO = new SerializedObject(tubeGen);
            tubeSO.FindProperty("radius").floatValue = tubeRadius;
            tubeSO.FindProperty("segmentLength").floatValue = tubeLength;
            tubeSO.FindProperty("radialSegments").intValue = 32;
            tubeSO.FindProperty("lengthSegments").intValue = 80;
            tubeSO.ApplyModifiedPropertiesWithoutUndo();

            // Mesh generation now happens automatically in Awake()

            // Crear material del tubo: estética de fibra óptica cyberpunk
            Material tubeMaterial = CreateTubeMaterial();
            tubeMR.material = tubeMaterial;

            // Guardar el material como asset para persistencia
            string matPath = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(matPath))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            string tubeMatAssetPath = matPath + "/FiberOpticTube_Mat.mat";
            Material existingTubeMat = AssetDatabase.LoadAssetAtPath<Material>(tubeMatAssetPath);
            if (existingTubeMat != null)
            {
                // Actualizar el material existente
                EditorUtility.CopySerialized(tubeMaterial, existingTubeMat);
                tubeMR.material = existingTubeMat;
            }
            else
            {
                AssetDatabase.CreateAsset(tubeMaterial, tubeMatAssetPath);
                tubeMR.material = AssetDatabase.LoadAssetAtPath<Material>(tubeMatAssetPath);
            }

            // Añadir un collider al tubo (MeshCollider con la malla invertida no funciona bien,
            // así que usamos un enfoque sin collider en el tubo; las colisiones se manejan por radio)

            Undo.RegisterCreatedObjectUndo(tubeGO, "Create FiberOpticTube");

            // ──────────────────────────────────────────────
            // 2. JUGADOR: ESFERA ROJA (PAQUETE DE DATOS)
            // ──────────────────────────────────────────────

            GameObject playerGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerGO.name = "DataPacket_Player";
            playerGO.tag = "Player";
            playerGO.transform.position = new Vector3(tubeRadius, 0f, 5f); // Carril 0 (ángulo 0°, en la pared derecha)
            playerGO.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            // Material del jugador: rojo neón emisivo
            Material playerMaterial = CreatePlayerMaterial();

            string playerMatAssetPath = matPath + "/DataPacket_Player_Mat.mat";
            Material existingPlayerMat = AssetDatabase.LoadAssetAtPath<Material>(playerMatAssetPath);
            if (existingPlayerMat != null)
            {
                EditorUtility.CopySerialized(playerMaterial, existingPlayerMat);
                playerGO.GetComponent<MeshRenderer>().material = existingPlayerMat;
            }
            else
            {
                AssetDatabase.CreateAsset(playerMaterial, playerMatAssetPath);
                playerGO.GetComponent<MeshRenderer>().material = AssetDatabase.LoadAssetAtPath<Material>(playerMatAssetPath);
            }

            // Eliminar el collider por defecto de la esfera primitiva y añadir uno de tipo esfera
            Object.DestroyImmediate(playerGO.GetComponent<Collider>());
            SphereCollider playerCollider = playerGO.AddComponent<SphereCollider>();
            playerCollider.isTrigger = true;
            playerCollider.radius = 0.5f;

            // Añadir Rigidbody cinemático (necesario para detección de triggers)
            Rigidbody playerRB = playerGO.AddComponent<Rigidbody>();
            playerRB.useGravity = false;
            playerRB.isKinematic = true;

            // Añadir el controlador cilíndrico
            CylindricalPlayerController controller = playerGO.AddComponent<CylindricalPlayerController>();
            SerializedObject controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("tubeRadius").floatValue = tubeRadius;
            controllerSO.FindProperty("forwardSpeed").floatValue = 15f;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            // Añadir una estela luminosa (Trail Renderer) al paquete de datos
            TrailRenderer trail = playerGO.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.5f;
            trail.endWidth = 0.05f;
            trail.startColor = new Color(1f, 0.2f, 0.1f, 0.9f);  // Rojo neón
            trail.endColor = new Color(1f, 0.6f, 0f, 0f);         // Naranja que se desvanece
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.minVertexDistance = 0.1f;

            Undo.RegisterCreatedObjectUndo(playerGO, "Create DataPacket Player");

            // ──────────────────────────────────────────────
            // 3. CÁMARA DE SEGUIMIENTO
            // ──────────────────────────────────────────────

            // Desactivar la cámara principal existente si hay una
            Camera existingCam = Camera.main;
            if (existingCam != null)
            {
                existingCam.gameObject.SetActive(false);
            }

            GameObject cameraGO = new GameObject("GameplayCamera");
            Camera cam = cameraGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.03f, 1f); // Negro azulado profundo
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.tag = "MainCamera";

            // AudioListener para que el audio funcione en esta cámara
            cameraGO.AddComponent<AudioListener>();

            // Posicionar la cámara detrás y centrada en el eje del tubo
            cameraGO.transform.position = new Vector3(0f, 0f, -8f);
            cameraGO.transform.rotation = Quaternion.identity;

            // Añadir script de seguimiento sencillo
            CameraFollowPlayer camFollow = cameraGO.AddComponent<CameraFollowPlayer>();
            SerializedObject camSO = new SerializedObject(camFollow);
            camSO.FindProperty("target").objectReferenceValue = playerGO.transform;
            camSO.FindProperty("followOffset").vector3Value = new Vector3(0f, 0f, -8f);
            camSO.FindProperty("smoothSpeed").floatValue = 8f;
            camSO.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(cameraGO, "Create Gameplay Camera");

            // ──────────────────────────────────────────────
            // 4. ILUMINACIÓN
            // ──────────────────────────────────────────────

            // Luz direccional suave
            GameObject dirLightGO = new GameObject("Directional Light - Gameplay");
            Light dirLight = dirLightGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.4f, 0.5f, 0.7f, 1f); // Azul frío
            dirLight.intensity = 0.4f;
            dirLightGO.transform.rotation = Quaternion.Euler(30f, 45f, 0f);

            Undo.RegisterCreatedObjectUndo(dirLightGO, "Create Directional Light");

            // Luz puntual en el centro del tubo (simula el núcleo de fibra óptica)
            GameObject coreLightGO = new GameObject("AmbientLight_Core");
            Light coreLight = coreLightGO.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.color = new Color(0f, 0.8f, 1f, 1f); // Cian
            coreLight.intensity = 2f;
            coreLight.range = tubeRadius * 3f;
            coreLightGO.transform.position = new Vector3(0f, 0f, 10f);
            // Hacer que la luz siga al jugador
            coreLightGO.transform.SetParent(playerGO.transform);
            coreLightGO.transform.localPosition = new Vector3(0f, 0f, 5f);

            Undo.RegisterCreatedObjectUndo(coreLightGO, "Create Core Light");

            // ──────────────────────────────────────────────
            // 5. AUDIO MANAGER
            // ──────────────────────────────────────────────

            GameObject audioGO = new GameObject("GameplayAudioManager");
            GameplayAudioManager audioMgr = audioGO.AddComponent<GameplayAudioManager>();

            // Auto-assign audio clips from known asset paths
            SerializedObject audioSO = new SerializedObject(audioMgr);

            AudioClip firstPlayClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BallCrashMP3.mp3");
            AudioClip retryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BallCrashMusic.mp3");
            AudioClip crashClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/BallCrashEffect.mp3");

            if (firstPlayClip != null) audioSO.FindProperty("firstPlayMusic").objectReferenceValue = firstPlayClip;
            if (retryClip != null) audioSO.FindProperty("retryMusic").objectReferenceValue = retryClip;
            if (crashClip != null) audioSO.FindProperty("crashSFX").objectReferenceValue = crashClip;

            audioSO.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(audioGO, "Create GameplayAudioManager");

            // ──────────────────────────────────────────────
            // FINALIZAR
            // ──────────────────────────────────────────────

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Marcar la escena como modificada
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Selection.activeGameObject = playerGO;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("¡Escenario de prueba del Gameplay generado con éxito! Pulsa Play para probar el movimiento.");
        }

        // ──────────────────────────────────────────────
        // CREACIÓN DE MATERIALES
        // ──────────────────────────────────────────────

        private static Material CreateTubeMaterial()
        {
            // Intentar usar URP Lit, si no existe usar Standard
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader);
            mat.name = "FiberOpticTube_Mat";

            // Color base: blanco para preservar el color de la textura del circuito
            Color baseColor = Color.white;
            mat.SetColor("_BaseColor", baseColor);
            mat.color = baseColor;

            // Metallic y Smoothness para el aspecto de sílice pulida
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.4f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
            if (mat.HasProperty("_GlossMapScale")) mat.SetFloat("_GlossMapScale", 0.85f);

            // Cargar texturas de circuito de neón del tubo
            Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/NeonGrid_Tube.png");
            Texture2D emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/NeonGrid_Tube_Emission.png");

            if (baseTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseTex);
            }

            if (emissionTex != null)
            {
                if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", emissionTex);
            }

            // Emisión: activar emisión con un multiplicador de intensidad HDR para el brillo neón
            mat.EnableKeyword("_EMISSION");
            Color emissionColor = Color.white * 3.5f; // Multiplicador de intensidad de emisión
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emissionColor);
            }

            // Configurar para renderizar ambas caras (doble cara)
            if (mat.HasProperty("_Cull"))
            {
                mat.SetFloat("_Cull", (float)CullMode.Off);
            }

            return mat;
        }

        private static Material CreatePlayerMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader);
            mat.name = "DataPacket_Player_Mat";

            // Color base: rojo intenso
            Color baseColor = new Color(0.9f, 0.1f, 0.05f, 1f);
            mat.SetColor("_BaseColor", baseColor);
            mat.color = baseColor;

            // Metallic y Smoothness para efecto brillante de energía
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.6f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.95f);
            if (mat.HasProperty("_GlossMapScale")) mat.SetFloat("_GlossMapScale", 0.95f);

            // Emisión: rojo neón brillante (la esfera brilla con luz propia)
            mat.EnableKeyword("_EMISSION");
            Color emissionColor = new Color(1f, 0.15f, 0.05f, 1f) * 2.5f; // Rojo intenso con HDR
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emissionColor);
            }

            return mat;
        }

        // ──────────────────────────────────────────────
        // UTILIDADES
        // ──────────────────────────────────────────────

        private static void DestroyExisting(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }
        }
    }
}
#endif
