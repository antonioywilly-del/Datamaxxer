using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Component attached to obstacle GameObjects inside the fiber optic tube.
    /// Handles lifecycle (recycle when behind player) and scoring detection.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        [Header("Obstacle Settings")]
        [Tooltip("How many MB this obstacle is worth when dodged.")]
        [SerializeField] private int mbValue = 1;

        [Tooltip("Distance behind the player at which this obstacle is recycled.")]
        [SerializeField] private float recycleBehindDistance = 15f;

        private bool hasBeenPassed = false;
        private Transform playerTransform;

        /// <summary>
        /// Whether this obstacle has already been scored (passed by the player).
        /// </summary>
        public bool HasBeenPassed => hasBeenPassed;

        /// <summary>
        /// MB value awarded when this obstacle is dodged.
        /// </summary>
        public int MBValue => mbValue;

        public void Initialize(Transform player)
        {
            playerTransform = player;
            hasBeenPassed = false;
        }

        private void Update()
        {
            if (playerTransform == null) return;

            float playerZ = playerTransform.position.z;
            float obstacleZ = transform.position.z;

            // Check if the player has passed this obstacle (score it)
            if (!hasBeenPassed && playerZ > obstacleZ + 1f)
            {
                hasBeenPassed = true;
                if (GameManager.Instance != null)
                {
                    float distance = Vector2.Distance(
                        new Vector2(playerTransform.position.x, playerTransform.position.y),
                        new Vector2(transform.position.x, transform.position.y)
                    );
                    GameManager.Instance.OnObstacleDodged(mbValue, distance);
                }
            }

            // Recycle when far behind the player
            if (playerZ > obstacleZ + recycleBehindDistance)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Reset state when pulled from the pool.
        /// </summary>
        public void ResetObstacle()
        {
            hasBeenPassed = false;
        }
    }
}
