using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Central game state manager for the DataMaxxer gameplay scene.
    /// Tracks score (MB), bandwidth multiplier, and game state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // SINGLETON
        // ──────────────────────────────────────────────

        public static GameManager Instance { get; private set; }

        // ──────────────────────────────────────────────
        // GAME STATE
        // ──────────────────────────────────────────────

        public enum GameState { Playing, GameOver }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.Playing;

        public GameState CurrentState => currentState;
        public bool IsPlaying => currentState == GameState.Playing;

        // ──────────────────────────────────────────────
        // SCORING
        // ──────────────────────────────────────────────

        [Header("Scoring")]
        [Tooltip("Current score in Megabytes.")]
        private int scoreMB = 0;

        [Tooltip("Current bandwidth multiplier.")]
        private float bandwidthMultiplier = 1f;

        [Tooltip("Max bandwidth multiplier achievable.")]
        [SerializeField] private float maxBandwidthMultiplier = 4096f;

        [Tooltip("Multiplier increment per consecutive perfect dodge.")]
        [SerializeField] private float multiplierIncrement = 0.25f;

        [Tooltip("Time window (seconds) after passing an obstacle to count as 'perfect timing'.")]
        [SerializeField] private float perfectTimingWindow = 0.3f;

        private int consecutivePerfectDodges = 0;
        private float lastDodgeTime = -999f;

        // Track peak multiplier for game-over display
        private float peakBandwidthMultiplier = 1f;

        // ──────────────────────────────────────────────
        // HIGH SCORE
        // ──────────────────────────────────────────────

        private int highScore = 0;
        private bool isNewHighScore = false;

        // ──────────────────────────────────────────────
        // DIFFICULTY SCALING
        // ──────────────────────────────────────────────

        [Header("Difficulty")]
        [Tooltip("Time elapsed since game start (used for difficulty scaling).")]
        private float gameTime = 0f;

        /// <summary>Current elapsed game time in seconds.</summary>
        public float GameTime => gameTime;

        // ──────────────────────────────────────────────
        // REFERENCES
        // ──────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private CylindricalPlayerController playerController;

        // ──────────────────────────────────────────────
        // INPUT (New Input System)
        // ──────────────────────────────────────────────

        private InputAction restartAction;

        // ──────────────────────────────────────────────
        // EVENTS (for HUD to listen to)
        // ──────────────────────────────────────────────

        public System.Action<int> OnScoreChanged;
        public System.Action<float> OnMultiplierChanged;
        public System.Action<int, int, bool> OnGameOver; // score, highScore, isNewRecord
        public System.Action<string, Color> OnNearMiss;  // message, color

        // Procedural audio for near misses
        [Header("Audio Configuration")]
        [SerializeField] private AudioClip closeDodgeClip;
        [SerializeField] private AudioClip superCloseDodgeClip;
        private AudioSource nearMissAudioSource;

        // ──────────────────────────────────────────────
        // PUBLIC PROPERTIES
        // ──────────────────────────────────────────────

        public int ScoreMB => scoreMB;
        public float BandwidthMultiplier => bandwidthMultiplier;
        public float PeakBandwidthMultiplier => peakBandwidthMultiplier;
        public int HighScore => highScore;
        public bool IsNewHighScore => isNewHighScore;

        // ──────────────────────────────────────────────
        // LIFECYCLE
        // ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            highScore = HighScoreManager.GetHighScore();

            // Set up restart input using new Input System
            restartAction = new InputAction("Restart", InputActionType.Button);
            restartAction.AddBinding("<Keyboard>/space");
            restartAction.AddBinding("<Keyboard>/enter");
            restartAction.Enable();

            // Initialize procedural chimes
            InitializeNearMissAudio();
        }

        private void Start()
        {
            currentState = GameState.Playing;
            scoreMB = 0;
            bandwidthMultiplier = 1f;
            peakBandwidthMultiplier = 1f;
            consecutivePerfectDodges = 0;
            lastDodgeTime = -999f;
            gameTime = 0f;

            OnScoreChanged?.Invoke(scoreMB);
            OnMultiplierChanged?.Invoke(bandwidthMultiplier);

            // Auto-find player if not assigned
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<CylindricalPlayerController>();
            }
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                gameTime += Time.deltaTime;
            }
            else if (currentState == GameState.GameOver)
            {
                // Allow restart with Space or Enter
                if (restartAction != null && restartAction.triggered)
                {
                    RestartGame();
                }
            }
        }

        private void OnDestroy()
        {
            if (restartAction != null)
            {
                restartAction.Disable();
                restartAction.Dispose();
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ──────────────────────────────────────────────
        // SCORING INTERFACE
        // ──────────────────────────────────────────────

        /// <summary>
        /// Called by Obstacle when the player successfully dodges it,
        /// providing the lateral distance at which they passed.
        /// </summary>
        public void OnObstacleDodged(int mbValue, float dodgeDistance)
        {
            if (currentState != GameState.Playing) return;

            bool isFirstDodge = lastDodgeTime < 0f;
            float timeSinceLastDodge = isFirstDodge ? 0f : (Time.time - lastDodgeTime);
            lastDodgeTime = Time.time;

            // Near miss mechanic
            bool isCloseDodge = dodgeDistance < 4.0f;
            bool isSuperCloseDodge = dodgeDistance < 2.5f;

            int bonusMB = 0;
            string dodgeType = "NORMAL";

            if (isSuperCloseDodge)
            {
                bonusMB = 5; // +5 MB bonus
                dodgeType = "SUPER CLOSE";
                consecutivePerfectDodges += 2; // Fast forward multiplier
                OnNearMiss?.Invoke("¡EVASIÓN CRÍTICA! +5 MB", new Color(0f, 1f, 1f)); // Neon Cyan
                TriggerNearMissAudio(true);
            }
            else if (isCloseDodge)
            {
                bonusMB = 2; // +2 MB bonus
                dodgeType = "CLOSE";
                consecutivePerfectDodges++;
                OnNearMiss?.Invoke("¡EVASIÓN CERCANA! +2 MB", new Color(1f, 0f, 0.67f)); // Neon Magenta
                TriggerNearMissAudio(false);
            }
            else
            {
                // Normal dodge timing check
                if (timeSinceLastDodge < perfectTimingWindow * 3f)
                {
                    consecutivePerfectDodges++;
                }
                else
                {
                    // Soft penalty: halve the streak instead of full reset, but keep at least 1 if we already had a multiplier
                    consecutivePerfectDodges = consecutivePerfectDodges > 0 ? Mathf.Max(1, consecutivePerfectDodges / 2) : 0;
                }
            }

            // Calculate multiplier
            float newMultiplier = 1f + (consecutivePerfectDodges * multiplierIncrement);
            bandwidthMultiplier = Mathf.Min(newMultiplier, maxBandwidthMultiplier);
            OnMultiplierChanged?.Invoke(bandwidthMultiplier);

            // Track peak
            if (bandwidthMultiplier > peakBandwidthMultiplier)
                peakBandwidthMultiplier = bandwidthMultiplier;

            // Apply score with multiplier + near miss bonus (bonus is also multiplied!)
            int earnedMB = Mathf.RoundToInt((mbValue + bonusMB) * bandwidthMultiplier);
            scoreMB += Mathf.Max(1, earnedMB);

            OnScoreChanged?.Invoke(scoreMB);

            if (bonusMB > 0)
            {
                Debug.Log($"[GameManager] {dodgeType} DODGE! Distance: {dodgeDistance:F2} | Bonus: +{bonusMB} MB | Total Earned: {earnedMB} MB");
            }
        }

        // ──────────────────────────────────────────────
        // GAME OVER (Packet Loss)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Called when the player collides with an obstacle.
        /// </summary>
        public void TriggerPacketLoss()
        {
            if (currentState != GameState.Playing) return;

            currentState = GameState.GameOver;

            // Save high score
            isNewHighScore = HighScoreManager.SaveIfHighScore(scoreMB, peakBandwidthMultiplier);
            highScore = HighScoreManager.GetHighScore();

            // Notify listeners
            OnGameOver?.Invoke(scoreMB, highScore, isNewHighScore);

            Debug.Log($"[DataMaxxer] PACKET LOST! Score: {scoreMB} MB | High Score: {highScore} MB | New Record: {isNewHighScore}");
        }

        // ──────────────────────────────────────────────
        // RESTART
        // ──────────────────────────────────────────────

        public void RestartGame()
        {
            if (restartAction != null)
            {
                restartAction.Disable();
                restartAction.Dispose();
                restartAction = null;
            }
            Instance = null;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Returns to the main menu scene (Scene_0_MainMenu).
        /// </summary>
        public void GoToMainMenu()
        {
            Instance = null;
            GameplayAudioManager.ResetToFirstPlay();
            SceneManager.LoadScene("Scene_0_MainMenu");
        }

        // ──────────────────────────────────────────────
        // PROCEDURAL AUDIO HELPERS
        // ──────────────────────────────────────────────

        private void InitializeNearMissAudio()
        {
            if (closeDodgeClip == null)
            {
                closeDodgeClip = CreateProceduralNearMissClip(false);
            }
            if (superCloseDodgeClip == null)
            {
                superCloseDodgeClip = CreateProceduralNearMissClip(true);
            }

            nearMissAudioSource = gameObject.AddComponent<AudioSource>();
            nearMissAudioSource.playOnAwake = false;
            nearMissAudioSource.loop = false;
            nearMissAudioSource.volume = 0.35f;
            nearMissAudioSource.spatialBlend = 0f; // 2D sound
        }

        private void TriggerNearMissAudio(bool isSuperClose)
        {
            if (nearMissAudioSource == null) return;

            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
            nearMissAudioSource.volume = sfxVol * 0.55f;

            AudioClip clip = isSuperClose ? superCloseDodgeClip : closeDodgeClip;
            if (clip != null)
            {
                nearMissAudioSource.PlayOneShot(clip);
            }
        }

        private AudioClip CreateProceduralNearMissClip(bool isSuperClose)
        {
            int sampleRate = 44100;
            float duration = isSuperClose ? 0.35f : 0.18f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 0f;

                if (isSuperClose)
                {
                    // Double pitch slide: 800 -> 1400 -> 1800
                    float p = t / duration;
                    freq = Mathf.Lerp(800f, 1800f, p * p);
                }
                else
                {
                    // Single slide: 600 -> 1000
                    freq = Mathf.Lerp(600f, 1100f, t / duration);
                }

                float val = Mathf.Sin(2f * Mathf.PI * freq * t);
                
                // Exponential decay envelope
                float envelope = Mathf.Pow(1f - ((float)i / sampleCount), 1.5f);
                samples[i] = val * envelope * 0.12f;
            }

            AudioClip clip = AudioClip.Create("NearMissChime", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
