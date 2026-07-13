using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Datamaxxer.UI
{
    public class TerminalButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Text settings")]
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Color normalColor = Color.gray;
        [SerializeField] private Color hoverColor = Color.cyan;
        [SerializeField] private string prefixOnHover = "> ";
        [SerializeField] private string suffixOnHover = " <";

        [Header("Transform settings")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float transitionSpeed = 10f;

        [Header("Audio settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;

        private string originalText;
        private Vector3 originalScale;
        private Vector3 targetScale;
        private Coroutine glitchCoroutine;
        private int clickFrame = -1;

        // Compartir clips procedimentales por defecto entre todos los botones
        private static AudioClip defaultHoverClip;
        private static AudioClip defaultClickClip;

        private void Awake()
        {
            if (buttonText == null)
            {
                buttonText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (buttonText != null)
            {
                originalText = buttonText.text;
                buttonText.color = normalColor;
            }

            originalScale = transform.localScale;
            targetScale = originalScale;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.loop = false;
                }
            }

            // Inicializar clips procedimentales si no hay ninguno asignado en el inspector
            InitializeProceduralClips();
        }

        private void InitializeProceduralClips()
        {
            if (hoverClip == null)
            {
                if (defaultHoverClip == null)
                {
                    defaultHoverClip = CreateProceduralHoverClip();
                }
                hoverClip = defaultHoverClip;
            }

            if (clickClip == null)
            {
                if (defaultClickClip == null)
                {
                    defaultClickClip = CreateProceduralClickClip();
                }
                clickClip = defaultClickClip;
            }
        }

        private static AudioClip CreateProceduralHoverClip()
        {
            int sampleRate = 44100;
            float duration = 0.03f; // Solo 30 milisegundos (muy rápido)
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 800f; // Frecuencia más baja y agradable para el hover
                float val = Mathf.Sin(2f * Mathf.PI * freq * t);
                
                // Envolvente de decaimiento exponencial rápida para mayor suavidad
                float envelope = Mathf.Pow(1f - ((float)i / sampleCount), 2f);
                samples[i] = val * envelope * 0.04f; // Volumen extra suave
            }

            AudioClip clip = AudioClip.Create("HoverProcedural", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateProceduralClickClip()
        {
            int sampleRate = 44100;
            float duration = 0.18f; // 180 milisegundos
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float timeMs = t * 1000f;
                
                // Doble bip ascendente procedimental (chime llamativo)
                float val = 0f;
                float envelope = 0f;
                
                if (timeMs < 70f)
                {
                    // Primer bip: de 700Hz a 900Hz
                    float p = timeMs / 70f;
                    float freq = Mathf.Lerp(700f, 900f, p);
                    val = Mathf.Sin(2f * Mathf.PI * freq * t);
                    envelope = 1f - p;
                }
                else if (timeMs >= 90f && timeMs < 180f)
                {
                    // Segundo bip (más agudo): de 950Hz a 1250Hz
                    float p = (timeMs - 90f) / 90f;
                    float freq = Mathf.Lerp(950f, 1250f, p);
                    val = Mathf.Sin(2f * Mathf.PI * freq * t);
                    envelope = 1f - p;
                }

                // Crujido retro digital leve
                float squareVal = val >= 0f ? 1f : -1f;
                float blendedVal = Mathf.Lerp(val, squareVal, 0.12f);

                samples[i] = blendedVal * envelope * 0.28f; // Volumen más llamativo y audible
            }

            AudioClip clip = AudioClip.Create("ClickProcedural", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * transitionSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            HighlightButton();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetButton();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            clickFrame = Time.frameCount;
            PlaySound(clickClip);
        }

        public void OnSelect(BaseEventData eventData)
        {
            HighlightButton();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            ResetButton();
        }

        private void HighlightButton()
        {
            targetScale = originalScale * hoverScale;

            if (buttonText != null)
            {
                buttonText.color = hoverColor;
                
                // Evitamos iniciar la corrutina si el GameObject se ha desactivado
                if (isActiveAndEnabled)
                {
                    if (glitchCoroutine != null) StopCoroutine(glitchCoroutine);
                    glitchCoroutine = StartCoroutine(GlitchTextCoroutine());
                }
            }

            if (clickFrame != Time.frameCount)
            {
                PlaySound(hoverClip);
            }
        }

        private void ResetButton()
        {
            targetScale = originalScale;

            if (buttonText != null)
            {
                if (glitchCoroutine != null) StopCoroutine(glitchCoroutine);
                buttonText.text = originalText;
                buttonText.color = normalColor;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && audioSource.isActiveAndEnabled && clip != null)
            {
                float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
                audioSource.PlayOneShot(clip, sfxVol);
            }
        }

        private IEnumerator GlitchTextCoroutine()
        {
            string formattedText = $"{prefixOnHover}{originalText}{suffixOnHover}";
            char[] chars = formattedText.ToCharArray();
            
            float glitchDuration = 0.15f;
            float elapsed = 0f;
            string glitchChars = "!@#$%^&*()_+{}|:<>?-=[]\\;',./";

            while (elapsed < glitchDuration)
            {
                char[] tempChars = (char[])chars.Clone();
                int corruptCount = Random.Range(1, 4);
                for (int i = 0; i < corruptCount; i++)
                {
                    int randomIndex = Random.Range(0, tempChars.Length);
                    if (!char.IsWhiteSpace(tempChars[randomIndex]))
                    {
                        tempChars[randomIndex] = glitchChars[Random.Range(0, glitchChars.Length)];
                    }
                }

                buttonText.text = new string(tempChars);
                elapsed += Time.deltaTime;
                yield return new WaitForSeconds(0.03f);
            }

            buttonText.text = formattedText;
        }

        private void OnDisable()
        {
            ResetButton();
        }

        // Método de configuración pública para el constructor de UI (Editor)
        public void SetupButton(TextMeshProUGUI label, Color normalCol, Color hoverCol)
        {
            buttonText = label;
            normalColor = normalCol;
            hoverColor = hoverCol;
        }
    }
}
