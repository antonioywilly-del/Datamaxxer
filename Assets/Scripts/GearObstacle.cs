using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Component attached to the parent GearObstacle.
    /// Handles the continuous rotation (spinning) of the gear obstacle around the Z-axis.
    /// </summary>
    public class GearObstacle : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [Tooltip("Speed of rotation in degrees per second around the Z-axis.")]
        [SerializeField] private float spinSpeed = 35f;

        private void Update()
        {
            // Spin the gear obstacle around the Z-axis (forward direction)
            transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);
        }

        /// <summary>
        /// Sets a custom spin speed.
        /// </summary>
        public void SetSpinSpeed(float speed)
        {
            spinSpeed = speed;
        }
    }
}
