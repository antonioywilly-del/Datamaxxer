using UnityEngine;
using UnityEngine.InputSystem;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Controlador del paquete de datos dentro del cable de fibra óptica cilíndrico.
    /// Gestiona el movimiento angular entre 6 carriles, la rotación suave y el salto radial.
    /// Compatible con el nuevo Input System de Unity.
    /// </summary>
    public class CylindricalPlayerController : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // CONFIGURACIÓN DEL TUBO
        // ──────────────────────────────────────────────

        [Header("Tubo Cilíndrico")]
        [Tooltip("Radio del tubo de fibra óptica en el que se mueve el jugador.")]
        [SerializeField] private float tubeRadius = 5f;

        [Tooltip("Número de carriles angulares distribuidos uniformemente en el tubo.")]
        [SerializeField] private int laneCount = 6;

        // ──────────────────────────────────────────────
        // MOVIMIENTO ANGULAR (CARRILES)
        // ──────────────────────────────────────────────

        [Header("Rotación entre Carriles")]
        [Tooltip("Velocidad de interpolación angular hacia el carril destino.")]
        [SerializeField] private float laneRotationSpeed = 8f;

        [Tooltip("Umbral angular (en grados) para considerar que el jugador ha llegado al carril destino.")]
        [SerializeField] private float snapThreshold = 0.5f;

        // ──────────────────────────────────────────────
        // AVANCE AUTOMÁTICO (ENDLESS RUNNER)
        // ──────────────────────────────────────────────

        [Header("Avance Automático")]
        [Tooltip("Velocidad base de avance del paquete de datos a lo largo del eje Z del tubo.")]
        [SerializeField] private float forwardSpeed = 18f;

        [Tooltip("Multiplicador de velocidad que se incrementa con el tiempo.")]
        [SerializeField] private float speedMultiplier = 1f;

        [Tooltip("Incremento del multiplicador de velocidad por segundo.")]
        [SerializeField] private float speedAcceleration = 0.05f;

        [Tooltip("Velocidad máxima (cap del multiplicador).")]
        [SerializeField] private float maxSpeedMultiplier = 3f;

        // ──────────────────────────────────────────────
        // SALTO RADIAL
        // ──────────────────────────────────────────────

        [Header("Salto Radial")]
        [Tooltip("Radio mínimo al que se acerca el jugador durante el salto (0 = centro del tubo).")]
        [SerializeField] private float jumpMinRadius = 1.5f;

        [Tooltip("Duración total del salto en segundos.")]
        [SerializeField] private float jumpDuration = 0.6f;

        [Tooltip("Punto normalizado (0-1) de la curva a partir del cual se puede encadenar otro salto. Valores más bajos permiten saltar antes.")]
        [SerializeField] private float earlyJumpThreshold = 0.5f;

        [Tooltip("Curva de animación del salto radial.")]
        [SerializeField] private AnimationCurve jumpCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.35f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -4f, 0f)
        );

        // ──────────────────────────────────────────────
        // INPUT SYSTEM
        // ──────────────────────────────────────────────

        [Header("Input Actions (Nuevo Input System)")]
        [Tooltip("Referencia al asset de Input Actions. Si se deja vacío, se usa Keyboard directamente.")]
        [SerializeField] private InputActionAsset inputActions;

        private InputAction moveAction;
        private InputAction jumpAction;

        // ──────────────────────────────────────────────
        // ESTADO INTERNO
        // ──────────────────────────────────────────────

        private int currentLane = 0;
        private int targetLane = 0;
        private float currentAngle = 0f;
        private float targetAngle = 0f;
        private float currentRadius;

        private bool isJumping = false;
        private float jumpTimer = 0f;
        private bool jumpBuffered = false;

        private float laneAngleStep;
        private float sensitivity = 1f;

        // ──────────────────────────────────────────────
        // GAME STATE
        // ──────────────────────────────────────────────

        private bool isAlive = true;

        // ──────────────────────────────────────────────
        // DEATH VISUAL EFFECTS
        // ──────────────────────────────────────────────

        [Header("Death Effects")]
        [Tooltip("How long the death slow-motion effect lasts.")]
        [SerializeField] private float deathSlowDuration = 0.5f;
        private float deathTimer = 0f;

        // ──────────────────────────────────────────────
        // COOLDOWN DE CAMBIO DE CARRIL
        // ──────────────────────────────────────────────

        [Header("Cooldown de Cambio de Carril")]
        [Tooltip("Tiempo mínimo entre cambios de carril consecutivos.")]
        [SerializeField] private float laneSwitchCooldown = 0.15f;
        private float laneSwitchTimer = 0f;

        // Estado de la entrada lateral (para detectar flancos de pulsación)
        private bool wasMovingLeft = false;
        private bool wasMovingRight = false;

        // ──────────────────────────────────────────────
        // INICIALIZACIÓN
        // ──────────────────────────────────────────────

        private void Awake()
        {
            laneAngleStep = 360f / laneCount;
            currentRadius = tubeRadius;
            sensitivity = PlayerPrefs.GetFloat("Sensitivity", 0.6f);
        }

        private void OnEnable()
        {
            SetupInputActions();
        }

        private void OnDisable()
        {
            CleanupInputActions();
        }

        private void SetupInputActions()
        {
            if (inputActions != null)
            {
                // Si se asignó un asset de Input Actions, buscar las acciones "Move" y "Jump"
                var playerMap = inputActions.FindActionMap("Player");
                if (playerMap != null)
                {
                    moveAction = playerMap.FindAction("Move");
                    jumpAction = playerMap.FindAction("Jump");
                }
            }

            // Fallback: si no se asignó un asset, crear acciones directamente desde código
            if (moveAction == null)
            {
                moveAction = new InputAction("Move", InputActionType.Value, null);
                // Composite WASD
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
                // Composite Flechas
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/rightArrow");
                // Gamepad
                moveAction.AddBinding("<Gamepad>/leftStick");
            }

            if (jumpAction == null)
            {
                jumpAction = new InputAction("Jump", InputActionType.Button, null);
                jumpAction.AddBinding("<Keyboard>/space");
                jumpAction.AddBinding("<Gamepad>/buttonSouth");
            }

            moveAction?.Enable();
            jumpAction?.Enable();
        }

        private void CleanupInputActions()
        {
            // Solo desactivar las acciones que creamos nosotros (no las del asset)
            if (inputActions == null)
            {
                moveAction?.Disable();
                moveAction?.Dispose();
                jumpAction?.Disable();
                jumpAction?.Dispose();
            }

            moveAction = null;
            jumpAction = null;
        }

        private void Start()
        {
            currentLane = 0;
            targetLane = 0;
            currentAngle = GetLaneAngle(currentLane);
            targetAngle = currentAngle;
            isAlive = true;
            ApplyPosition();

            // Apply the selected skin from PlayerSkinManager
            if (PlayerSkinManager.Instance != null)
            {
                PlayerSkinManager.Instance.ApplySkin(gameObject);
            }
        }

        // ──────────────────────────────────────────────
        // BUCLE PRINCIPAL
        // ──────────────────────────────────────────────

        private void Update()
        {
            if (!isAlive)
            {
                // Death animation: slow down and stop
                deathTimer += Time.deltaTime;
                if (deathTimer < deathSlowDuration)
                {
                    float slowFactor = 1f - (deathTimer / deathSlowDuration);
                    transform.position += Vector3.forward * forwardSpeed * slowFactor * 0.3f * Time.deltaTime;
                }
                return;
            }

            HandleLaneInput();
            HandleJumpInput();

            UpdateRotation();
            UpdateJump();
            UpdateForwardMovement();

            ApplyPosition();

            speedMultiplier = Mathf.Min(speedMultiplier + speedAcceleration * Time.deltaTime, maxSpeedMultiplier);

            if (laneSwitchTimer > 0f)
            {
                laneSwitchTimer -= Time.deltaTime;
            }
        }

        // ──────────────────────────────────────────────
        // COLLISION DETECTION
        // ──────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!isAlive) return;

            // Check if we hit an obstacle
            if (other.CompareTag("Obstacle"))
            {
                Die();
            }
        }

        /// <summary>
        /// Triggers death: packet loss.
        /// </summary>
        private void Die()
        {
            if (!isAlive) return;

            isAlive = false;
            deathTimer = 0f;

            // Disable trail
            var trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.emitting = false;
            }

            // Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerPacketLoss();
            }

            Debug.Log("[DataMaxxer] Player hit obstacle — PACKET LOST!");
        }

        // ──────────────────────────────────────────────
        // ENTRADA DE CARRILES (Nuevo Input System)
        // ──────────────────────────────────────────────

        private void HandleLaneInput()
        {
            if (moveAction == null || laneSwitchTimer > 0f) return;

            Vector2 moveInput = moveAction.ReadValue<Vector2>();

            // Detectar flancos: solo cambiar de carril cuando la tecla pasa de no-pulsada a pulsada
            bool isMovingRight = moveInput.x > 0.5f;
            bool isMovingLeft = moveInput.x < -0.5f;

            if (isMovingRight && !wasMovingRight)
            {
                targetLane = (targetLane + 1) % laneCount;
                targetAngle = GetLaneAngle(targetLane);
                laneSwitchTimer = laneSwitchCooldown;
            }
            else if (isMovingLeft && !wasMovingLeft)
            {
                targetLane = (targetLane - 1 + laneCount) % laneCount;
                targetAngle = GetLaneAngle(targetLane);
                laneSwitchTimer = laneSwitchCooldown;
            }

            wasMovingRight = isMovingRight;
            wasMovingLeft = isMovingLeft;
        }

        // ──────────────────────────────────────────────
        // ENTRADA DE SALTO (Nuevo Input System)
        // ──────────────────────────────────────────────

        private void HandleJumpInput()
        {
            if (jumpAction == null) return;

            if (jumpAction.WasPressedThisFrame())
            {
                if (!isJumping)
                {
                    // Not jumping — start immediately
                    isJumping = true;
                    jumpTimer = 0f;
                    jumpBuffered = false;
                }
                else
                {
                    // Mid-jump — buffer the input
                    jumpBuffered = true;
                }
            }
        }

        // ──────────────────────────────────────────────
        // ACTUALIZACIÓN DE ROTACIÓN SUAVE
        // ──────────────────────────────────────────────

        private void UpdateRotation()
        {
            if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) > snapThreshold)
            {
                currentAngle = Mathf.LerpAngle(
                    currentAngle,
                    targetAngle,
                    Time.deltaTime * laneRotationSpeed * sensitivity
                );
            }
            else
            {
                currentAngle = targetAngle;
                currentLane = targetLane;
            }
        }

        // ──────────────────────────────────────────────
        // ACTUALIZACIÓN DEL SALTO RADIAL
        // ──────────────────────────────────────────────

        private void UpdateJump()
        {
            if (!isJumping) return;

            jumpTimer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(jumpTimer / jumpDuration);
            float jumpFactor = jumpCurve.Evaluate(normalizedTime);
            currentRadius = Mathf.Lerp(tubeRadius, jumpMinRadius, jumpFactor);

            // Allow chaining: if past the early threshold and a jump is buffered, restart
            if (jumpBuffered && normalizedTime >= earlyJumpThreshold)
            {
                jumpTimer = 0f;
                jumpBuffered = false;
                // isJumping stays true — seamless chain
                return;
            }

            if (normalizedTime >= 1f)
            {
                isJumping = false;
                currentRadius = tubeRadius;
                jumpTimer = 0f;
                jumpBuffered = false;
            }
        }

        // ──────────────────────────────────────────────
        // AVANCE AUTOMÁTICO POR EL TUBO
        // ──────────────────────────────────────────────

        private void UpdateForwardMovement()
        {
            transform.position += Vector3.forward * forwardSpeed * speedMultiplier * Time.deltaTime;
        }

        // ──────────────────────────────────────────────
        // POSICIONAMIENTO EN COORDENADAS CILÍNDRICAS
        // ──────────────────────────────────────────────

        private void ApplyPosition()
        {
            float angleRad = currentAngle * Mathf.Deg2Rad;

            float localX = currentRadius * Mathf.Cos(angleRad);
            float localY = currentRadius * Mathf.Sin(angleRad);

            Vector3 pos = transform.position;
            pos.x = localX;
            pos.y = localY;
            transform.position = pos;

            Vector3 radialDirection = new Vector3(localX, localY, 0f).normalized;
            transform.rotation = Quaternion.LookRotation(Vector3.forward, -radialDirection);
        }

        // ──────────────────────────────────────────────
        // UTILIDADES Y PROPIEDADES PÚBLICAS
        // ──────────────────────────────────────────────

        private float GetLaneAngle(int lane)
        {
            return lane * laneAngleStep;
        }

        public bool IsJumping => isJumping;
        public bool IsAlive => isAlive;
        public int CurrentLane => currentLane;
        public float CurrentSpeedMultiplier => speedMultiplier;
        public float CurrentRadius => currentRadius;

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
        }

        // ──────────────────────────────────────────────
        // GIZMOS (Visualización en el Editor)
        // ──────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            float step = 360f / (laneCount > 0 ? laneCount : 6);
            float r = tubeRadius;
            float z = transform.position.z;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            for (int i = 0; i < 36; i++)
            {
                float a1 = i * 10f * Mathf.Deg2Rad;
                float a2 = (i + 1) * 10f * Mathf.Deg2Rad;
                Gizmos.DrawLine(
                    new Vector3(r * Mathf.Cos(a1), r * Mathf.Sin(a1), z),
                    new Vector3(r * Mathf.Cos(a2), r * Mathf.Sin(a2), z)
                );
            }

            Gizmos.color = Color.yellow;
            for (int i = 0; i < (laneCount > 0 ? laneCount : 6); i++)
            {
                float a = i * step * Mathf.Deg2Rad;
                Vector3 lanePos = new Vector3(r * Mathf.Cos(a), r * Mathf.Sin(a), z);
                Gizmos.DrawLine(new Vector3(0, 0, z), lanePos);
                Gizmos.DrawWireSphere(lanePos, 0.3f);
            }

            Gizmos.color = new Color(1f, 0f, 1f, 0.2f);
            for (int i = 0; i < 36; i++)
            {
                float a1 = i * 10f * Mathf.Deg2Rad;
                float a2 = (i + 1) * 10f * Mathf.Deg2Rad;
                Gizmos.DrawLine(
                    new Vector3(jumpMinRadius * Mathf.Cos(a1), jumpMinRadius * Mathf.Sin(a1), z),
                    new Vector3(jumpMinRadius * Mathf.Cos(a2), jumpMinRadius * Mathf.Sin(a2), z)
                );
            }
        }
    }
}
