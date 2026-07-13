using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Creates ambient speed particle effects inside the tube.
    /// Particles stream past the player, giving a sense of velocity and depth.
    /// </summary>
    public class SpeedParticles : MonoBehaviour
    {
        [Header("Shader Configuration")]
        [Tooltip("Shader to use for particles. If null, will fallback to searching.")]
        [SerializeField] private Shader particleShader;

        [Header("Configuration")]
        [Tooltip("Tube radius for particle spawn ring.")]
        [SerializeField] private float tubeRadius = 4.5f;

        [Tooltip("How far ahead to spawn particles.")]
        [SerializeField] private float spawnDistance = 60f;

        private Transform playerTransform;
        private ParticleSystem ps;

        private void Start()
        {
            var player = FindFirstObjectByType<CylindricalPlayerController>();
            if (player != null)
                playerTransform = player.transform;

            CreateParticleSystem();
        }

        private void Update()
        {
            if (playerTransform == null || ps == null) return;

            // Follow the player but offset ahead
            transform.position = playerTransform.position + Vector3.forward * spawnDistance;
        }

        private void CreateParticleSystem()
        {
            ps = gameObject.AddComponent<ParticleSystem>();
            var main = ps.main;

            // Particles appear ahead and stream backward past the player
            main.startLifetime = 3f;
            main.startSpeed = -20f; // Move toward the player
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            // Neon colors
            var startColor = main.startColor;
            startColor.mode = ParticleSystemGradientMode.RandomColor;
            Gradient colorGrad = new Gradient();
            colorGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0f, 1f, 1f), 0f),     // Cyan
                    new GradientColorKey(new Color(1f, 0f, 0.67f), 0.25f), // Magenta
                    new GradientColorKey(new Color(0.3f, 1f, 0.3f), 0.5f), // Green
                    new GradientColorKey(new Color(0.5f, 0.2f, 1f), 0.75f), // Purple
                    new GradientColorKey(new Color(0f, 1f, 1f), 1f),     // Cyan
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.6f, 0f),
                    new GradientAlphaKey(0.6f, 1f),
                }
            );
            main.startColor = colorGrad;

            // Shape: ring around tube interior
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = tubeRadius * 0.85f;
            shape.arc = 360f;
            shape.rotation = new Vector3(0f, 90f, 0f); // Face forward along Z

            // Emission: steady stream
            var emission = ps.emission;
            emission.rateOverTime = 120f;

            // Size over lifetime: fade out
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(0.8f, 0.5f);
            sizeCurve.AddKey(1f, 0f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Color over lifetime: fade alpha
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient lifetimeGrad = new Gradient();
            lifetimeGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.1f),
                    new GradientAlphaKey(0.8f, 0.7f),
                    new GradientAlphaKey(0f, 1f),
                }
            );
            col.color = lifetimeGrad;

            // Use additive particle material
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Create simple additive particle material
            var shader = particleShader != null ? particleShader : Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.name = "SpeedParticle_Mat";
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 1);   // Additive
                mat.SetColor("_BaseColor", Color.white);
                mat.renderQueue = 3100;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_BLENDMODE_ADD");
                renderer.material = mat;
            }
        }
    }
}
