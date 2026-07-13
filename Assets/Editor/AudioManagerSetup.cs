#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Datamaxxer.Gameplay;

namespace Datamaxxer.Editor
{
    /// <summary>
    /// Quick utility to add the GameplayAudioManager to the current scene
    /// without rebuilding the entire gameplay scene.
    /// </summary>
    public class AudioManagerSetup : EditorWindow
    {
        [MenuItem("Datamaxxer/Setup Gameplay Audio Manager")]
        public static void SetupAudioManager()
        {
            // Remove existing if present
            GameObject existing = GameObject.Find("GameplayAudioManager");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // Create new
            GameObject audioGO = new GameObject("GameplayAudioManager");
            GameplayAudioManager audioMgr = audioGO.AddComponent<GameplayAudioManager>();

            // Auto-assign audio clips
            SerializedObject audioSO = new SerializedObject(audioMgr);

            AudioClip firstPlayClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BallCrashMP3.mp3");
            AudioClip retryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BallCrashMusic.mp3");
            AudioClip crashClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/BallCrashEffect.mp3");

            int assigned = 0;
            if (firstPlayClip != null) { audioSO.FindProperty("firstPlayMusic").objectReferenceValue = firstPlayClip; assigned++; }
            if (retryClip != null) { audioSO.FindProperty("retryMusic").objectReferenceValue = retryClip; assigned++; }
            if (crashClip != null) { audioSO.FindProperty("crashSFX").objectReferenceValue = crashClip; assigned++; }

            audioSO.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(audioGO, "Create GameplayAudioManager");

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Selection.activeGameObject = audioGO;

            Debug.Log($"[DataMaxxer] GameplayAudioManager created. {assigned}/3 audio clips assigned.");
        }
    }
}
#endif
