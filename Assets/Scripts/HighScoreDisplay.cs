using UnityEngine;
using TMPro;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Displays the best score (high score) and bandwidth multiplier on the main menu UI.
    /// </summary>
    public class HighScoreDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI highScoreText;

        public void Setup(TextMeshProUGUI textComp)
        {
            highScoreText = textComp;
            UpdateText();
        }

        private void Start()
        {
            UpdateText();
        }

        private void UpdateText()
        {
            if (highScoreText != null)
            {
                int score = HighScoreManager.GetHighScore();
                float multiplier = HighScoreManager.GetHighMultiplier();
                
                // Formato premium ciberpunk con colores neón (Cyan y Magenta)
                highScoreText.text = $"<color=#00FFFF>[MEJOR_RENDIMIENTO]</color>\nREGISTRO: <color=#FF00FF>{score} MB</color>\nBANDA: <color=#FF00FF>{multiplier:F1}x</color>";
            }
        }
    }
}
