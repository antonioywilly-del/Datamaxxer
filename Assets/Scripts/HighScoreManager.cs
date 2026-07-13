using UnityEngine;

namespace Datamaxxer.Gameplay
{
    /// <summary>
    /// Manages high score persistence using PlayerPrefs.
    /// Provides static methods for saving and retrieving the best score.
    /// </summary>
    public static class HighScoreManager
    {
        private const string HIGH_SCORE_KEY = "DataMaxxer_HighScore_MB";
        private const string HIGH_MULTIPLIER_KEY = "DataMaxxer_HighMultiplier";

        /// <summary>
        /// Returns the saved high score in MB.
        /// </summary>
        public static int GetHighScore()
        {
            return PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        }

        /// <summary>
        /// Returns the best bandwidth multiplier achieved.
        /// </summary>
        public static float GetHighMultiplier()
        {
            return PlayerPrefs.GetFloat(HIGH_MULTIPLIER_KEY, 1f);
        }

        /// <summary>
        /// Saves the score if it beats the current high score.
        /// Returns true if a new record was set.
        /// </summary>
        public static bool SaveIfHighScore(int scoreMB, float multiplier = 1f)
        {
            int currentHigh = GetHighScore();
            if (scoreMB > currentHigh)
            {
                PlayerPrefs.SetInt(HIGH_SCORE_KEY, scoreMB);
                PlayerPrefs.SetFloat(HIGH_MULTIPLIER_KEY, multiplier);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets the high score (for debugging).
        /// </summary>
        public static void ResetHighScore()
        {
            PlayerPrefs.DeleteKey(HIGH_SCORE_KEY);
            PlayerPrefs.DeleteKey(HIGH_MULTIPLIER_KEY);
            PlayerPrefs.Save();
        }
    }
}
