using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

namespace Datamaxxer.Core
{
    public class SettingsManager : MonoBehaviour
    {
        [Header("Audio settings")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string musicVolumeParameter = "MusicVol";
        [SerializeField] private string sfxVolumeParameter = "SFXVol";

        [Header("UI Sliders")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider sensitivitySlider;

        [Header("UI Text Indicators")]
        [SerializeField] private TextMeshProUGUI musicValueText;
        [SerializeField] private TextMeshProUGUI sfxValueText;
        [SerializeField] private TextMeshProUGUI sensitivityValueText;

        [Header("Default Values")]
        [SerializeField] private float defaultMusicVolume = 0.15f;
        [SerializeField] private float defaultSFXVolume = 0.50f;
        [SerializeField] private float defaultSensitivity = 0.6f;

        private const string MusicVolumeKey = "MusicVolume";
        private const string SFXVolumeKey = "SFXVolume";
        private const string SensitivityKey = "Sensitivity";

        private void Start()
        {
            LoadAndApplySettings();
            
            // Suscribir eventos de sliders
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }

        public void LoadAndApplySettings()
        {
            float musicVol = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
            float sfxVol = PlayerPrefs.GetFloat(SFXVolumeKey, defaultSFXVolume);
            float sensitivity = PlayerPrefs.GetFloat(SensitivityKey, defaultSensitivity);

            if (musicSlider != null) musicSlider.value = musicVol;
            if (sfxSlider != null) sfxSlider.value = sfxVol;
            if (sensitivitySlider != null) sensitivitySlider.value = sensitivity;

            ApplyMusicVolume(musicVol);
            ApplySFXVolume(sfxVol);
            ApplySensitivity(sensitivity);
        }

        public void SetMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, value);
            ApplyMusicVolume(value);
        }

        public void SetSFXVolume(float value)
        {
            PlayerPrefs.SetFloat(SFXVolumeKey, value);
            ApplySFXVolume(value);
        }

        public void SetSensitivity(float value)
        {
            PlayerPrefs.SetFloat(SensitivityKey, value);
            ApplySensitivity(value);
        }

        private void ApplyMusicVolume(float value)
        {
            if (musicValueText != null)
            {
                musicValueText.text = $"[MUS] {(value * 100f):0}%";
            }

            if (audioMixer != null && !string.IsNullOrEmpty(musicVolumeParameter))
            {
                float db = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
                audioMixer.SetFloat(musicVolumeParameter, db);
            }
        }

        private void ApplySFXVolume(float value)
        {
            if (sfxValueText != null)
            {
                sfxValueText.text = $"[SFX] {(value * 100f):0}%";
            }

            if (audioMixer != null && !string.IsNullOrEmpty(sfxVolumeParameter))
            {
                float db = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
                audioMixer.SetFloat(sfxVolumeParameter, db);
            }
        }

        private void ApplySensitivity(float value)
        {
            if (sensitivityValueText != null)
            {
                sensitivityValueText.text = $"[SENS] {value:F1}x";
            }
        }

        public void SaveSettings()
        {
            PlayerPrefs.Save();
            Debug.Log("Configuraciones del protocolo guardadas correctamente en caché.");
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        // Método de configuración pública para el constructor de UI (Editor)
        public void SetupUI(Slider musicSld, Slider sfxSld, Slider sensSld, TextMeshProUGUI musicLbl, TextMeshProUGUI sfxLbl, TextMeshProUGUI sensLbl)
        {
            musicSlider = musicSld;
            sfxSlider = sfxSld;
            sensitivitySlider = sensSld;
            musicValueText = musicLbl;
            sfxValueText = sfxLbl;
            sensitivityValueText = sensLbl;
        }
    }
}
