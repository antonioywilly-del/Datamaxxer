using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Manages gameplay music and collision SFX.
    /// On first play: loops BallCrashMP3. On retry: loops BallCrashMusic.
    /// On collision: plays BallCrashEffect and stops all music.
    /// </summary>
    public class GameplayAudioManager : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // AUDIO CLIPS (assign via Inspector)
        // ──────────────────────────────────────────────

        [Header("Music")]
        [Tooltip("Music track for the first play session.")]
        [SerializeField] private AudioClip firstPlayMusic;   // BallCrashMP3.mp3

        [Tooltip("Music track for retry sessions (after first death).")]
        [SerializeField] private AudioClip retryMusic;       // BallCrashMusic.mp3

        [Header("SFX")]
        [Tooltip("Sound effect played on collision with an obstacle.")]
        [SerializeField] private AudioClip crashSFX;         // BallCrashEffect.mp3

        [Header("Volume")]
        [Tooltip("Volume for music playback (0-1).")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.15f;

        [Tooltip("Volume for crash SFX (0-1).")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.5f;

        // ──────────────────────────────────────────────
        // AUDIO SOURCES
        // ──────────────────────────────────────────────

        private AudioSource musicSource;
        private AudioSource sfxSource;

        // ──────────────────────────────────────────────
        // STATIC STATE (persists across scene reloads)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Tracks whether the player has died at least once during
        /// this application session. Persists across scene reloads
        /// so we know to switch tracks on retry.
        /// Reset when returning to the main menu.
        /// </summary>
        private static bool hasPlayedBefore = false;

        // ──────────────────────────────────────────────
        // LIFECYCLE
        // ──────────────────────────────────────────────

        private void Awake()
        {
            // Cargar volumen desde PlayerPrefs (configurado en el menú de opciones)
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", sfxVolume);

            // Create dedicated AudioSources
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.spatialBlend = 0f; // 2D sound

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;
            sfxSource.spatialBlend = 0f;

            // Ensure there is an active AudioListener in the scene.
            // The scene builder disables the original Main Camera (which had the AudioListener),
            // and the GameplayCamera may not have one. Without it, no audio plays.
            AudioListener existingListener = FindFirstObjectByType<AudioListener>();
            if (existingListener == null)
            {
                gameObject.AddComponent<AudioListener>();
                Debug.Log("[DataMaxxer Audio] No AudioListener found — added one to GameplayAudioManager.");
            }
        }

        private void Start()
        {
            // Pick the correct music track
            AudioClip trackToPlay = hasPlayedBefore ? retryMusic : firstPlayMusic;

            if (trackToPlay != null)
            {
                musicSource.clip = trackToPlay;
                musicSource.Play();
                Debug.Log($"[DataMaxxer Audio] Playing: {trackToPlay.name} (hasPlayedBefore={hasPlayedBefore})");
            }
            else
            {
                Debug.LogWarning("[DataMaxxer Audio] No music clip assigned! Check Inspector references.");
            }

            // Subscribe to game over event
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        // ──────────────────────────────────────────────
        // EVENT SUBSCRIPTIONS
        // ──────────────────────────────────────────────

        private void SubscribeToEvents()
        {
            Invoke(nameof(LateSubscribe), 0.1f);
        }

        private void LateSubscribe()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += HandleGameOver;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= HandleGameOver;
            }
        }

        // ──────────────────────────────────────────────
        // GAME OVER HANDLER
        // ──────────────────────────────────────────────

        private void HandleGameOver(int score, int highScore, bool isNewRecord)
        {
            // Stop music immediately
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            // Play crash SFX
            if (sfxSource != null && crashSFX != null)
            {
                sfxSource.PlayOneShot(crashSFX, sfxVolume);
            }

            // Mark that the player has played (next scene load will use retry music)
            hasPlayedBefore = true;
        }

        // ──────────────────────────────────────────────
        // PUBLIC API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Resets the static flag so the first-play music is used again.
        /// Call this when returning to the main menu.
        /// </summary>
        public static void ResetToFirstPlay()
        {
            hasPlayedBefore = false;
        }
    }
}
