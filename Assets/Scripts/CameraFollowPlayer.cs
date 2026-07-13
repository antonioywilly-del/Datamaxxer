using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Cámara de seguimiento que persigue al paquete de datos a lo largo del eje del tubo,
    /// manteniéndose siempre centrada en el eje Z para dar perspectiva de profundidad.
    /// </summary>
    public class CameraFollowPlayer : MonoBehaviour
    {
        [Header("Seguimiento")]
        [Tooltip("Transform del jugador a seguir.")]
        [SerializeField] private Transform target;

        [Tooltip("Offset de la cámara respecto al jugador (normalmente negativo en Z para estar detrás).")]
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -8f);

        [Tooltip("Velocidad de suavizado del seguimiento.")]
        [SerializeField] private float smoothSpeed = 8f;

        [Header("Rotación")]
        [Tooltip("Si es true, la cámara rota ligeramente para acompañar la rotación angular del jugador.")]
        [SerializeField] private bool followRotation = true;

        [Tooltip("Factor de atenuación de la rotación de seguimiento (0 = sin rotación, 1 = rotación completa).")]
        [SerializeField, Range(0f, 1f)] private float rotationFactor = 0.3f;

        private void LateUpdate()
        {
            if (target == null) return;

            // La cámara siempre se posiciona en el eje del tubo (X=0, Y=0),
            // detrás del jugador en el eje Z
            Vector3 targetPosition = new Vector3(0f, 0f, target.position.z) + followOffset;

            // Interpolación suave de la posición
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

            if (followRotation)
            {
                // Rotar ligeramente acompañando la orientación del jugador para dar dinamismo
                Quaternion targetRotation = Quaternion.Slerp(
                    Quaternion.identity,
                    target.rotation,
                    rotationFactor
                );

                // Solo tomamos la rotación en el eje Z (roll) del jugador
                Vector3 euler = targetRotation.eulerAngles;
                Quaternion rollOnly = Quaternion.Euler(0f, 0f, euler.z * rotationFactor);

                transform.rotation = Quaternion.Slerp(transform.rotation, rollOnly, smoothSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, smoothSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Permite asignar el target desde código (usado por el GameplaySceneBuilder).
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
