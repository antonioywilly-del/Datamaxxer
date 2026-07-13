using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Component attached to the WallObstacle.
    /// Creates a pulsating electric effect by oscillating emission intensity.
    /// This obstacle covers 3 lanes at both inner and outer radii,
    /// forcing the player to dodge by moving to the other half of the tube.
    /// </summary>
    public class WallObstacle : MonoBehaviour
    {
        [Header("Pulse Settings")]
        [Tooltip("Speed of the emission pulse effect.")]
        [SerializeField] private float pulseSpeed = 3f;

        [Tooltip("Minimum emission multiplier.")]
        [SerializeField] private float pulseMin = 4f;

        [Tooltip("Maximum emission multiplier.")]
        [SerializeField] private float pulseMax = 10f;

        private MeshRenderer[] renderers;
        private Color baseEmissionColor = new Color(0.3f, 0.6f, 1f); // Electric blue

        private void Awake()
        {
            renderers = GetComponentsInChildren<MeshRenderer>();
        }

        private void Update()
        {
            // Pulsating emission intensity
            float pulse = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);

            foreach (var rend in renderers)
            {
                if (rend != null && rend.material != null)
                {
                    rend.material.SetColor("_EmissionColor", baseEmissionColor * pulse);
                }
            }
        }

        /// <summary>
        /// Sets the base emission color for the pulse effect.
        /// </summary>
        public void SetEmissionColor(Color color)
        {
            baseEmissionColor = color;
        }
    }
}
