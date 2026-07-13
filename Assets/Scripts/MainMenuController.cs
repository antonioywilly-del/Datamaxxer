using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Datamaxxer.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject loadingScreenPanel;
        [SerializeField] private GameObject skinsPanel;

        [Header("Loading Screen Elements")]
        [SerializeField] private Slider loadingBar;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private float minimumLoadingTime = 1f;

        [Header("Buttons (Wired at Start)")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button backSettingsButton;
        [SerializeField] private Button backCreditsButton;
        [SerializeField] private Button skinsButton;
        [SerializeField] private Button backSkinsButton;

        [Header("Settings")]
        [SerializeField] private string gameplaySceneName = "Scene_1_Gameplay";

        private void Start()
        {
            // Registrar eventos de clics automáticamente a nivel de ejecución para evitar problemas de persistencia en el Editor
            if (playButton != null) playButton.onClick.AddListener(() => StartGame(gameplaySceneName));
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
            if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
            
            if (backSettingsButton != null) backSettingsButton.onClick.AddListener(ShowMenuPanel);
            if (backCreditsButton != null) backCreditsButton.onClick.AddListener(ShowMenuPanel);
            if (skinsButton != null) skinsButton.onClick.AddListener(OpenSkins);
            if (backSkinsButton != null) backSkinsButton.onClick.AddListener(ShowMenuPanel);

            ShowMenuPanel();
        }

        public void ShowMenuPanel()
        {
            if (menuPanel != null) menuPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (loadingScreenPanel != null) loadingScreenPanel.SetActive(false);
            if (skinsPanel != null) skinsPanel.SetActive(false);
        }

        public void OpenSettings()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void OpenSkins()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (skinsPanel != null) skinsPanel.SetActive(true);
        }

        public void OpenCredits()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        public void ExitGame()
        {
            Debug.Log("Saliendo del juego...");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        public void StartGame(string sceneName)
        {
            StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (loadingScreenPanel != null) loadingScreenPanel.SetActive(true);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"No se pudo cargar la escena: {sceneName}. Asegúrate de añadirla en Build Settings.");
                yield break;
            }

            operation.allowSceneActivation = false;
            float elapsedTime = 0f;

            while (!operation.isDone)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                
                if (loadingBar != null)
                {
                    loadingBar.value = progress;
                }

                if (loadingText != null)
                {
                    loadingText.text = $"CONECTANDO_CON_NODO... {(progress * 100f):0}%";
                }

                if (operation.progress >= 0.9f && elapsedTime >= minimumLoadingTime)
                {
                    if (loadingText != null)
                    {
                        loadingText.text = "ENLACE_ESTABLECIDO. INICIANDO...";
                    }
                    yield return new WaitForSeconds(0.5f);
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }
        }

        // Métodos de inicialización pública para el constructor de UI (Editor)
        public void SetupPanels(GameObject menu, GameObject settings, GameObject credits, GameObject loading, GameObject skins = null)
        {
            menuPanel = menu;
            settingsPanel = settings;
            creditsPanel = credits;
            loadingScreenPanel = loading;
            skinsPanel = skins;
        }

        public void SetupLoadingElements(Slider bar, TextMeshProUGUI text)
        {
            loadingBar = bar;
            loadingText = text;
        }

        public void SetupButtons(Button play, Button settings, Button credits, Button exit, Button backSettings, Button backCredits, Button skins = null, Button backSkins = null)
        {
            playButton = play;
            settingsButton = settings;
            creditsButton = credits;
            exitButton = exit;
            backSettingsButton = backSettings;
            backCreditsButton = backCredits;
            skinsButton = skins;
            backSkinsButton = backSkins;
        }
    }
}
