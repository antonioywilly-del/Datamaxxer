using UnityEngine;
using UnityEngine.UIElements;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Bridge between the GameManager and the UI Toolkit HUD.
    /// Updates score, multiplier, and game over overlay in real-time.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameplayHUD : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // UI REFERENCES
        // ──────────────────────────────────────────────

        private UIDocument uiDocument;
        private VisualElement root;

        // HUD elements
        private Label scoreLabel;
        private Label multiplierLabel;
        private Label highscoreLabel;

        // Game Over elements
        private VisualElement gameoverOverlay;
        private Label gameoverScore;
        private Label gameoverMultiplier;
        private Label gameoverHighscore;
        private Label newRecordLabel;

        // Game Over buttons
        private Button retryButton;
        private Button menuButton;

        // ──────────────────────────────────────────────
        // STATE
        // ──────────────────────────────────────────────

        private bool isGameOver = false;

        // ──────────────────────────────────────────────
        // LIFECYCLE
        // ──────────────────────────────────────────────

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            // Get root and cache references
            root = uiDocument.rootVisualElement;
            if (root == null) return;

            // HUD
            scoreLabel = root.Q<Label>("score-label");
            multiplierLabel = root.Q<Label>("multiplier-label");
            highscoreLabel = root.Q<Label>("highscore-label");

            // Game Over
            gameoverOverlay = root.Q<VisualElement>("gameover-overlay");
            gameoverScore = root.Q<Label>("gameover-score");
            gameoverMultiplier = root.Q<Label>("gameover-multiplier");
            gameoverHighscore = root.Q<Label>("gameover-highscore");
            newRecordLabel = root.Q<Label>("new-record-label");

            // Game Over buttons
            retryButton = root.Q<Button>("retry-button");
            menuButton = root.Q<Button>("menu-button");

            // Force hide game over overlay at start using style.display
            if (gameoverOverlay != null)
            {
                gameoverOverlay.style.display = DisplayStyle.None;
            }

            // Force hide new record label
            if (newRecordLabel != null)
            {
                newRecordLabel.style.display = DisplayStyle.None;
            }

            // Set initial high score
            if (highscoreLabel != null)
            {
                int hs = HighScoreManager.GetHighScore();
                highscoreLabel.text = hs > 0 ? $"BEST: {hs} MB" : "BEST: ---";
            }

            // Wire button callbacks
            if (retryButton != null)
            {
                retryButton.clicked += OnRetryClicked;
            }
            if (menuButton != null)
            {
                menuButton.clicked += OnMenuClicked;
            }

            // Subscribe to GameManager events
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            // Unsubscribe button callbacks
            if (retryButton != null)
            {
                retryButton.clicked -= OnRetryClicked;
            }
            if (menuButton != null)
            {
                menuButton.clicked -= OnMenuClicked;
            }

            UnsubscribeFromEvents();
        }

        // ──────────────────────────────────────────────
        // BUTTON HANDLERS
        // ──────────────────────────────────────────────

        private void OnRetryClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }

        private void OnMenuClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToMainMenu();
            }
        }

        // ──────────────────────────────────────────────
        // EVENT SUBSCRIPTIONS
        // ──────────────────────────────────────────────

        private void SubscribeToEvents()
        {
            // We need to wait for GameManager to be available
            Invoke(nameof(LateSubscribe), 0.1f);
        }

        private void LateSubscribe()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged += HandleScoreChanged;
                GameManager.Instance.OnMultiplierChanged += HandleMultiplierChanged;
                GameManager.Instance.OnGameOver += HandleGameOver;
                GameManager.Instance.OnNearMiss += ShowDodgeAlert;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged -= HandleScoreChanged;
                GameManager.Instance.OnMultiplierChanged -= HandleMultiplierChanged;
                GameManager.Instance.OnGameOver -= HandleGameOver;
                GameManager.Instance.OnNearMiss -= ShowDodgeAlert;
            }
        }

        // ──────────────────────────────────────────────
        // EVENT HANDLERS
        // ──────────────────────────────────────────────

        private void HandleScoreChanged(int scoreMB)
        {
            if (scoreLabel != null)
            {
                scoreLabel.text = $"{scoreMB} MB";
            }
        }

        private void HandleMultiplierChanged(float multiplier)
        {
            if (multiplierLabel != null)
            {
                multiplierLabel.text = $"x{multiplier:F1}";

                // Change color intensity based on multiplier
                if (multiplier >= 4f)
                {
                    multiplierLabel.style.color = new Color(1f, 0.85f, 0f); // Gold
                }
                else if (multiplier >= 2f)
                {
                    multiplierLabel.style.color = new Color(1f, 0.4f, 0f); // Orange
                }
                else
                {
                    multiplierLabel.style.color = new Color(1f, 0f, 0.67f); // Magenta (default)
                }
            }
        }

        private void HandleGameOver(int score, int highScore, bool isNewRecord)
        {
            if (gameoverOverlay == null) return;

            isGameOver = true;

            // Show the game over overlay
            gameoverOverlay.style.display = DisplayStyle.Flex;

            // Populate stats
            if (gameoverScore != null) gameoverScore.text = $"{score} MB";
            if (gameoverMultiplier != null)
            {
                float peakMult = GameManager.Instance != null ? GameManager.Instance.PeakBandwidthMultiplier : 1f;
                gameoverMultiplier.text = $"x{peakMult:F1}";
            }
            if (gameoverHighscore != null) gameoverHighscore.text = $"{highScore} MB";

            // Show/hide new record
            if (newRecordLabel != null)
            {
                newRecordLabel.style.display = isNewRecord ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // ──────────────────────────────────────────────
        // DODGE ALERT POPUPS
        // ──────────────────────────────────────────────

        private void ShowDodgeAlert(string message, Color color)
        {
            if (root == null) return;

            // Create floating label dynamically
            Label alert = new Label(message);
            alert.style.position = Position.Absolute;
            alert.style.alignSelf = Align.Center;
            alert.style.top = Length.Percent(35f);
            alert.style.color = color;
            alert.style.fontSize = 18f;
            alert.style.unityFontStyleAndWeight = FontStyle.Bold;

            if (scoreLabel != null)
            {
                alert.style.unityFontDefinition = scoreLabel.style.unityFontDefinition;
            }

            root.Add(alert);

            // Animate fade-out and float-up
            StartCoroutine(AnimateDodgeAlert(alert));
        }

        private System.Collections.IEnumerator AnimateDodgeAlert(Label label)
        {
            float duration = 1.2f;
            float elapsed = 0f;
            float startTop = 35f;
            float endTop = 25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                label.style.opacity = 1f - t;
                label.style.top = Length.Percent(Mathf.Lerp(startTop, endTop, t));

                yield return null;
            }

            root.Remove(label);
        }
    }
}
