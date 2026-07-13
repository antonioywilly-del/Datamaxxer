#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Datamaxxer.UI;
using Datamaxxer.Core;

namespace Datamaxxer.Editor
{
    public class MainMenuUIBuilder : EditorWindow
    {
        [MenuItem("Datamaxxer/Build Main Menu UI")]
        public static void BuildUI()
        {
            // Abrir la escena de forma explícita para asegurar que los cambios se realizan en ella
            string menuScenePath = "Assets/Scenes/Scene_0_MainMenu.unity";
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEngine.SceneManagement.Scene scene;
            if (activeScene.path == menuScenePath)
            {
                scene = activeScene;
            }
            else
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(menuScenePath);
            }

            // 1. Buscar o crear Canvas de forma segura
            Canvas canvas = FindObjectOfType<Canvas>();
            GameObject canvasGO;
            if (canvas == null)
            {
                canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            }
            else
            {
                canvasGO = canvas.gameObject;
            }

            // 2. Buscar o crear EventSystem y actualizar su InputModule
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            GameObject esGO;
            if (eventSystem == null)
            {
                esGO = new GameObject("EventSystem", typeof(EventSystem));
                ConfigureEventSystem(esGO);
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            }
            else
            {
                esGO = eventSystem.gameObject;
                ConfigureEventSystem(esGO);
            }

            // 3. Detectar y eliminar instancias previas para evitar duplicidades
            Transform existingRoot = canvasGO.transform.Find("MainMenu_Root");
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot.gameObject);
            }

            // Crear el nuevo contenedor raíz del menú bajo el Canvas
            GameObject menuRoot = new GameObject("MainMenu_Root", typeof(RectTransform));
            menuRoot.transform.SetParent(canvasGO.transform, false);
            StretchRectTransform(menuRoot.GetComponent<RectTransform>());

            // Añadir el controlador al contenedor raíz del menú
            MainMenuController menuController = menuRoot.AddComponent<MainMenuController>();

            // Colores ciberpunk/terminal
            Color panelBgColor = new Color(0.02f, 0.04f, 0.09f, 0.95f); // Azul cobalto muy oscuro y opaco
            Color neonCyan = new Color(0.0f, 1.0f, 1.0f, 1.0f);
            Color neonMagenta = new Color(1.0f, 0.0f, 1.0f, 1.0f);
            Color textGray = new Color(0.7f, 0.7f, 0.7f, 1.0f);

            // Cargar la fuente Orbitron para todo el menú principal
            TMP_FontAsset orbitronFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Orbitron-VariableFont_wght.asset");

            // 4. Crear Panel de Fondo General
            GameObject backgroundGO = CreateUIElement("BackgroundPanel", menuRoot.transform);
            Image bgImage = backgroundGO.AddComponent<Image>();
            
            // Asegurar que la textura está configurada como Sprite (2D y UI)
            string bgPath = "Assets/UI/TitleBackGround.png";
            TextureImporter importer = AssetImporter.GetAtPath(bgPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
            if (bgSprite == null)
            {
                // Si por alguna razón la caché del AssetDatabase no ha cargado el sprite, forzamos reimportación síncrona
                AssetDatabase.ImportAsset(bgPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
            }
            
            var assets = AssetDatabase.LoadAllAssetsAtPath(bgPath);
            foreach (var asset in assets)
            {
                Debug.Log($"[MainMenuUIBuilder] Found asset: {asset.name} ({asset.GetType()})");
            }

            Debug.Log($"[MainMenuUIBuilder] bgSprite loaded from {bgPath}: {(bgSprite != null ? bgSprite.name : "null")}");
            if (bgSprite != null)
            {
                bgImage.sprite = bgSprite;
                bgImage.color = Color.white; // Sin tinte para conservar los colores neón originales
            }
            else
            {
                bgImage.color = panelBgColor;
            }
            StretchRectTransform(backgroundGO.GetComponent<RectTransform>());

            // Añadir un panel de oscurecimiento sutil para mejorar el contraste de la interfaz
            GameObject backgroundOverlayGO = CreateUIElement("BackgroundOverlay", menuRoot.transform);
            Image bgOverlayImage = backgroundOverlayGO.AddComponent<Image>();
            bgOverlayImage.color = new Color(0f, 0f, 0f, 0.4f); // 40% de opacidad
            StretchRectTransform(backgroundOverlayGO.GetComponent<RectTransform>());

            // 5. Crear Paneles Principales bajo el Root
            GameObject menuPanel = CreateUIElement("MenuPanel", menuRoot.transform);
            StretchRectTransform(menuPanel.GetComponent<RectTransform>());

            GameObject settingsPanel = CreateUIElement("SettingsPanel", menuRoot.transform);
            StretchRectTransform(settingsPanel.GetComponent<RectTransform>());
            Image settingsBg = settingsPanel.AddComponent<Image>();
            settingsBg.color = new Color(0.01f, 0.02f, 0.05f, 0.98f);
            settingsPanel.SetActive(false);

            GameObject creditsPanel = CreateUIElement("CreditsPanel", menuRoot.transform);
            StretchRectTransform(creditsPanel.GetComponent<RectTransform>());
            Image creditsBg = creditsPanel.AddComponent<Image>();
            creditsBg.color = new Color(0.01f, 0.02f, 0.05f, 0.98f);
            creditsPanel.SetActive(false);

            GameObject loadingPanel = CreateUIElement("LoadingScreenPanel", menuRoot.transform);
            StretchRectTransform(loadingPanel.GetComponent<RectTransform>());
            Image loadingBg = loadingPanel.AddComponent<Image>();
            loadingBg.color = new Color(0f, 0f, 0.02f, 1f); 
            loadingPanel.SetActive(false);

            // ----------------------------------------------------
            // CONSTRUIR CONTENIDO: MENU PANEL
            // ----------------------------------------------------
            
            // Título Logo - Sombra (Para mejorar legibilidad y darle un look retro-cyberpunk 3D)
            GameObject logoShadowGO = CreateUIElement("LogoText_Shadow", menuPanel.transform);
            RectTransform logoShadowRect = logoShadowGO.GetComponent<RectTransform>();
            logoShadowRect.anchoredPosition = new Vector2(6f, 314f); // Desplazado ligeramente abajo y a la derecha
            logoShadowRect.sizeDelta = new Vector2(1000f, 160f);
            TextMeshProUGUI logoShadowText = logoShadowGO.AddComponent<TextMeshProUGUI>();
            logoShadowText.text = "DATAMAXXER";
            logoShadowText.fontSize = 100;
            logoShadowText.alignment = TextAlignmentOptions.Center;
            logoShadowText.color = new Color(0.85f, 0f, 0.85f, 0.8f); // Sombra magenta neón oscura

            // Título Logo - Texto Principal
            GameObject logoGO = CreateUIElement("LogoText", menuPanel.transform);
            RectTransform logoRect = logoGO.GetComponent<RectTransform>();
            logoRect.anchoredPosition = new Vector2(0f, 320f);
            logoRect.sizeDelta = new Vector2(1000f, 160f);
            TextMeshProUGUI logoText = logoGO.AddComponent<TextMeshProUGUI>();
            logoText.text = "DATAMAXXER";
            logoText.fontSize = 100;
            logoText.alignment = TextAlignmentOptions.Center;
            logoText.color = neonCyan;

            // Cambiar tipo de letra (fuente) del título principal y de la sombra al Orbitron
            if (orbitronFont != null)
            {
                logoText.font = orbitronFont;
                logoShadowText.font = orbitronFont;
            }
            else
            {
                logoText.fontStyle = FontStyles.Bold;
                logoShadowText.fontStyle = FontStyles.Bold;
            }

            // Subtítulo
            GameObject subtitleGO = CreateUIElement("SubtitleText", menuPanel.transform);
            RectTransform subRect = subtitleGO.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0f, 220f);
            subRect.sizeDelta = new Vector2(1000f, 60f);
            TextMeshProUGUI subText = subtitleGO.AddComponent<TextMeshProUGUI>();
            if (orbitronFont != null) subText.font = orbitronFont;
            subText.text = "BOMBA_STUDIOS // INFRASTRUCTURE_LINK_v1.0";
            subText.fontSize = 24;
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = neonMagenta;

            // Botones Contenedor
            GameObject buttonsContainer = CreateUIElement("ButtonsContainer", menuPanel.transform);
            RectTransform containerRect = buttonsContainer.GetComponent<RectTransform>();
            containerRect.anchoredPosition = new Vector2(0f, -80f);
            containerRect.sizeDelta = new Vector2(600f, 450f);
            
            VerticalLayoutGroup vLayout = buttonsContainer.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.spacing = 30f;
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = false;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = false;

            // Crear Botones y configurarlos con TerminalButton
            GameObject btnPlay = CreateTerminalButton("Btn_Play", "TRANSMITIR PAQUETE", buttonsContainer.transform, textGray, neonCyan);
            GameObject btnSettings = CreateTerminalButton("Btn_Settings", "CALIBRAR PROTOCOLO", buttonsContainer.transform, textGray, neonCyan);
            GameObject btnCredits = CreateTerminalButton("Btn_Credits", "REGISTROS DE RED", buttonsContainer.transform, textGray, neonCyan);
            GameObject btnExit = CreateTerminalButton("Btn_Exit", "DESCONECTAR", buttonsContainer.transform, textGray, neonMagenta);

            // ----------------------------------------------------
            // CONSTRUIR CONTENIDO: PANEL DE MEJOR PUNTUACIÓN (HIGH SCORE)
            // ----------------------------------------------------
            GameObject highScorePanel = CreateUIElement("HighScorePanel", menuPanel.transform);
            RectTransform hsRect = highScorePanel.GetComponent<RectTransform>();
            hsRect.anchorMin = new Vector2(1f, 1f); // Esquina superior derecha
            hsRect.anchorMax = new Vector2(1f, 1f);
            hsRect.pivot = new Vector2(1f, 1f);
            hsRect.anchoredPosition = new Vector2(-40f, -40f);
            hsRect.sizeDelta = new Vector2(360f, 140f);

            // Fondo oscuro semitransparente ciberpunk
            Image hsBg = highScorePanel.AddComponent<Image>();
            hsBg.color = new Color(0.01f, 0.02f, 0.05f, 0.85f);

            // Línea decorativa vertical neón magenta en el borde izquierdo
            GameObject hsDecorLine = CreateUIElement("DecorLine", highScorePanel.transform);
            RectTransform decorRect = hsDecorLine.GetComponent<RectTransform>();
            decorRect.anchorMin = new Vector2(0f, 0f);
            decorRect.anchorMax = new Vector2(0f, 1f);
            decorRect.pivot = new Vector2(0f, 0.5f);
            decorRect.anchoredPosition = Vector2.zero;
            decorRect.sizeDelta = new Vector2(5f, 0f);
            Image decorImg = hsDecorLine.AddComponent<Image>();
            decorImg.color = neonMagenta;

            // Texto de Puntuación
            GameObject hsTextGO = CreateUIElement("Text", highScorePanel.transform);
            RectTransform hsTextRect = hsTextGO.GetComponent<RectTransform>();
            hsTextRect.anchorMin = Vector2.zero;
            hsTextRect.anchorMax = Vector2.one;
            hsTextRect.offsetMin = new Vector2(25f, 10f); // Evitar la línea decorativa de 5px
            hsTextRect.offsetMax = new Vector2(-15f, -10f);

            TextMeshProUGUI hsText = hsTextGO.AddComponent<TextMeshProUGUI>();
            hsText.fontSize = 24;
            hsText.alignment = TextAlignmentOptions.MidlineLeft;
            hsText.lineSpacing = 10f;
            hsText.color = textGray;
            if (orbitronFont != null) hsText.font = orbitronFont;

            // Añadir y configurar el script de visualización de high score
            Datamaxxer.Gameplay.HighScoreDisplay hsDisplay = highScorePanel.AddComponent<Datamaxxer.Gameplay.HighScoreDisplay>();
            hsDisplay.Setup(hsText);

            // ----------------------------------------------------
            // CONSTRUIR CONTENIDO: SETTINGS PANEL
            // ----------------------------------------------------
            
            // Título de Configuración (Tamaño aumentado de 50 a 60)
            GameObject settingsTitle = CreateUIElement("SettingsTitle", settingsPanel.transform);
            RectTransform setTIdRect = settingsTitle.GetComponent<RectTransform>();
            setTIdRect.anchoredPosition = new Vector2(0f, 360f);
            setTIdRect.sizeDelta = new Vector2(1100f, 90f);
            TextMeshProUGUI setTText = settingsTitle.AddComponent<TextMeshProUGUI>();
            if (orbitronFont != null) setTText.font = orbitronFont;
            setTText.text = "CALIBRAR PROTOCOLO DE TRANSMISIÓN";
            setTText.fontSize = 60;
            setTText.alignment = TextAlignmentOptions.Center;
            setTText.color = neonCyan;

            // Contenedor de Sliders (Ensanchado y ampliado para acomodar los nuevos elementos de opciones)
            GameObject slidersContainer = CreateUIElement("SlidersContainer", settingsPanel.transform);
            RectTransform slidersRect = slidersContainer.GetComponent<RectTransform>();
            slidersRect.anchoredPosition = new Vector2(0f, -10f);
            slidersRect.sizeDelta = new Vector2(1050f, 500f);
            VerticalLayoutGroup sLayout = slidersContainer.AddComponent<VerticalLayoutGroup>();
            sLayout.childAlignment = TextAnchor.UpperCenter;
            sLayout.spacing = 40f;

            // Sliders para Música, SFX, Sensibilidad (Tamaño aumentado a 32 internamente)
            GameObject musicSliderGO = CreateSliderElement("MusicSlider", slidersContainer.transform, "VOLUMEN MÚSICA", 0.15f, out TextMeshProUGUI musicValueTxt);
            GameObject sfxSliderGO = CreateSliderElement("SFXSlider", slidersContainer.transform, "VOLUMEN EFECTOS", 0.50f, out TextMeshProUGUI sfxValueTxt);
            GameObject sensSliderGO = CreateSliderElement("SensSlider", slidersContainer.transform, "SENSIBILIDAD GIRO", 0.6f, out TextMeshProUGUI sensValueTxt);

            // Botón Volver de Configuración
            GameObject btnBackSettings = CreateTerminalButton("Btn_BackSettings", "GUARDAR Y CERRAR", settingsPanel.transform, textGray, neonCyan);
            RectTransform btnBSRect = btnBackSettings.GetComponent<RectTransform>();
            btnBSRect.anchoredPosition = new Vector2(0f, -380f);
            btnBSRect.sizeDelta = new Vector2(450f, 75f);

            // Configurar e inicializar SettingsManager directamente
            SettingsManager settingsManager = settingsPanel.AddComponent<SettingsManager>();
            settingsManager.SetupUI(
                musicSliderGO.GetComponent<Slider>(),
                sfxSliderGO.GetComponent<Slider>(),
                sensSliderGO.GetComponent<Slider>(),
                musicValueTxt,
                sfxValueTxt,
                sensValueTxt
            );

            // ----------------------------------------------------
            // CONSTRUIR CONTENIDO: CREDITS PANEL
            // ----------------------------------------------------
            
            // Título de Registro de Red
            GameObject creditsTitle = CreateUIElement("CreditsTitle", creditsPanel.transform);
            RectTransform credTIdRect = creditsTitle.GetComponent<RectTransform>();
            credTIdRect.anchoredPosition = new Vector2(0f, 360f);
            credTIdRect.sizeDelta = new Vector2(1000f, 90f);
            TextMeshProUGUI credTText = creditsTitle.AddComponent<TextMeshProUGUI>();
            if (orbitronFont != null) credTText.font = orbitronFont;
            credTText.text = "REGISTROS DE RED - ACCESO CONCEDIDO";
            credTText.fontSize = 50;
            credTText.alignment = TextAlignmentOptions.Center;
            credTText.color = neonMagenta;

            // Contenido de Registro de Red (Tamaño 32)
            GameObject creditsContent = CreateUIElement("CreditsContent", creditsPanel.transform);
            RectTransform credCRect = creditsContent.GetComponent<RectTransform>();
            credCRect.anchoredPosition = new Vector2(0f, -10f);
            credCRect.sizeDelta = new Vector2(1100f, 450f);
            TextMeshProUGUI credCText = creditsContent.AddComponent<TextMeshProUGUI>();
            if (orbitronFont != null) credCText.font = orbitronFont;
            credCText.text = "PROYECTO: DATAMAXXER\nDESARROLLADO POR: BOMBA STUDIOS\n\n- ADMINISTRADOR DE RED / PROGRAMACIÓN: ANTONIO\n- INGENIERO DE ENLACE / DISEÑO: ABDULLAH\n\nTRANSMISIÓN DE DATOS EN FIBRA DE SÍLICE ESTABLE.";
            credCText.fontSize = 32;
            credCText.alignment = TextAlignmentOptions.Center;
            credCText.color = textGray;

            // Botón Volver de Registro
            GameObject btnBackCredits = CreateTerminalButton("Btn_BackCredits", "CERRAR REGISTRO", creditsPanel.transform, textGray, neonCyan);
            RectTransform btnBCRect = btnBackCredits.GetComponent<RectTransform>();
            btnBCRect.anchoredPosition = new Vector2(0f, -370f);
            btnBCRect.sizeDelta = new Vector2(450f, 75f);

            // ----------------------------------------------------
            // CONSTRUIR CONTENIDO: LOADING PANEL
            // ----------------------------------------------------
            
            GameObject loadingTextGO = CreateUIElement("LoadingText", loadingPanel.transform);
            RectTransform loadTextRect = loadingTextGO.GetComponent<RectTransform>();
            loadTextRect.anchoredPosition = new Vector2(0f, 60f);
            loadTextRect.sizeDelta = new Vector2(900f, 90f);
            TextMeshProUGUI loadingTextComp = loadingTextGO.AddComponent<TextMeshProUGUI>();
            loadingTextComp.text = "CONECTANDO_CON_NODO... 0%";
            loadingTextComp.fontSize = 36;
            loadingTextComp.alignment = TextAlignmentOptions.Center;
            loadingTextComp.color = neonCyan;

            // Aplicar fuente Orbitron al texto de carga
            TMP_FontAsset loadingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Orbitron-VariableFont_wght.asset");
            if (loadingFont != null)
            {
                loadingTextComp.font = loadingFont;
            }

            // Barra de carga
            GameObject loadingBarGO = CreateUIElement("LoadingBar", loadingPanel.transform);
            RectTransform barRect = loadingBarGO.GetComponent<RectTransform>();
            barRect.anchoredPosition = new Vector2(0f, -60f);
            barRect.sizeDelta = new Vector2(800f, 40f); 
            Image barBgImage = loadingBarGO.AddComponent<Image>();
            barBgImage.color = new Color(0.1f, 0.1f, 0.2f, 1f);

            GameObject fillArea = CreateUIElement("FillArea", loadingBarGO.transform);
            StretchRectTransform(fillArea.GetComponent<RectTransform>());
            
            GameObject fill = CreateUIElement("Fill", fillArea.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = neonCyan;

            Slider slider = loadingBarGO.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;

            // ----------------------------------------------------
            // CONFIGURAR MAIN MENU CONTROLLER DIRECTAMENTE
            // ----------------------------------------------------
            
            menuController.SetupPanels(menuPanel, settingsPanel, creditsPanel, loadingPanel);
            menuController.SetupLoadingElements(slider, loadingTextComp);
            menuController.SetupButtons(
                btnPlay.GetComponent<Button>(),
                btnSettings.GetComponent<Button>(),
                btnCredits.GetComponent<Button>(),
                btnExit.GetComponent<Button>(),
                btnBackSettings.GetComponent<Button>(),
                btnBackCredits.GetComponent<Button>()
            );

            // 6. Registrar la creación de toda la jerarquía de UI en el Undo de Unity
            Undo.RegisterCreatedObjectUndo(menuRoot, "Build Main Menu UI");

            // Marcar la escena como modificada y guardarla
            EditorUtility.SetDirty(menuRoot);
            if (menuRoot.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(menuRoot.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(menuRoot.scene);
            }

            Debug.Log("¡UI del Menú Principal generada y configurada con éxito en la escena!");
        }


        private static void ConfigureEventSystem(GameObject esGO)
        {
            System.Type newModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newModuleType != null)
            {
                StandaloneInputModule oldModule = esGO.GetComponent<StandaloneInputModule>();
                if (oldModule != null)
                {
                    DestroyImmediate(oldModule);
                }

                if (esGO.GetComponent(newModuleType) == null)
                {
                    esGO.AddComponent(newModuleType);
                }
            }
            else
            {
                if (esGO.GetComponent<StandaloneInputModule>() == null)
                {
                    esGO.AddComponent<StandaloneInputModule>();
                }
            }
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchRectTransform(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateTerminalButton(string name, string text, Transform parent, Color textNormal, Color textHover)
        {
            GameObject btnGO = new GameObject(name, typeof(RectTransform));
            btnGO.transform.SetParent(parent, false);
            RectTransform rect = btnGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(550f, 75f); 

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.05f);

            Button button = btnGO.AddComponent<Button>();
            button.transition = Selectable.Transition.None; 

            GameObject textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(btnGO.transform, false);
            StretchRectTransform(textGO.GetComponent<RectTransform>());
            
            TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 32; 
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = textNormal;

            TMP_FontAsset btnFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Orbitron-VariableFont_wght.asset");
            if (btnFont != null)
            {
                tmpText.font = btnFont;
            }

            TerminalButton tBtn = btnGO.AddComponent<TerminalButton>();
            tBtn.SetupButton(tmpText, textNormal, textHover);

            return btnGO;
        }

        private static GameObject CreateSliderElement(string name, Transform parent, string labelText, float defaultValue, out TextMeshProUGUI valueText)
        {
            GameObject container = CreateUIElement(name + "_Container", parent);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(1000f, 100f); // Tamaño del contenedor del slider aumentado a 1000x100

            // Label
            GameObject labelGO = CreateUIElement("Label", container.transform);
            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0.4f, 0.5f);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(10f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 60f); // Altura de etiqueta aumentada
            TextMeshProUGUI labelComp = labelGO.AddComponent<TextMeshProUGUI>();
            labelComp.text = labelText;
            labelComp.fontSize = 32; // Fuente de etiqueta en menú de opciones aumentada a 32
            labelComp.alignment = TextAlignmentOptions.MidlineLeft;
            labelComp.color = new Color(0.7f, 0.7f, 0.7f);

            TMP_FontAsset sliderFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Orbitron-VariableFont_wght.asset");
            if (sliderFont != null)
            {
                labelComp.font = sliderFont;
            }

            // Slider Component GO
            GameObject sliderGO = CreateUIElement("Slider", container.transform);
            RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.4f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.85f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = new Vector2(0f, 30f); // Grosor del slider aumentado a 30

            // Background
            GameObject bg = CreateUIElement("Background", sliderGO.transform);
            StretchRectTransform(bg.GetComponent<RectTransform>());
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.15f, 1f);

            // Fill Area
            GameObject fillArea = CreateUIElement("FillArea", sliderGO.transform);
            StretchRectTransform(fillArea.GetComponent<RectTransform>());
            GameObject fill = CreateUIElement("Fill", fillArea.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.0f, 1.0f, 1.0f, 1f);

            // Handle Slide Area
            GameObject handleArea = CreateUIElement("HandleSlideArea", sliderGO.transform);
            StretchRectTransform(handleArea.GetComponent<RectTransform>());
            GameObject handle = CreateUIElement("Handle", handleArea.transform);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(30f, 0f); // Handle aumentado a 30
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(1.0f, 0.0f, 1.0f, 1f); 

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultValue;

            // Value indicator text
            GameObject valTextGO = CreateUIElement("ValueText", container.transform);
            RectTransform valRect = valTextGO.GetComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.85f, 0.5f);
            valRect.anchorMax = new Vector2(1.0f, 0.5f);
            valRect.pivot = new Vector2(1f, 0.5f);
            valRect.anchoredPosition = new Vector2(-10f, 0f);
            valRect.sizeDelta = new Vector2(0f, 60f); // Altura aumentada
            valueText = valTextGO.AddComponent<TextMeshProUGUI>();
            valueText.text = "100%";
            valueText.fontSize = 32; // Fuente de valor en menú de opciones aumentada a 32
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            valueText.color = new Color(0.0f, 1.0f, 1.0f);

            TMP_FontAsset sliderValueFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Orbitron-VariableFont_wght.asset");
            if (sliderValueFont != null)
            {
                valueText.font = sliderValueFont;
            }

            return sliderGO;
        }

        [InitializeOnLoadMethod]
        private static void CleanAudioOnLoad()
        {
            if (!EditorPrefs.GetBool("Datamaxxer_MainMenuAudioCleaned", false))
            {
                EditorApplication.delayCall += () =>
                {
                    string menuScenePath = "Assets/Scenes/Scene_0_MainMenu.unity";
                    var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                    
                    // Solo procedemos si la escena del menú principal está abierta o si la abrimos temporalmente
                    bool isMenuOpen = activeScene.path == menuScenePath;
                    UnityEngine.SceneManagement.Scene scene = activeScene;
                    
                    if (!isMenuOpen)
                    {
                        scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(menuScenePath);
                    }

                    GameObject audioManager = GameObject.Find("GameplayAudioManager");
                    if (audioManager != null)
                    {
                        Undo.DestroyObjectImmediate(audioManager);
                        Debug.Log("[DataMaxxer] Automatic Cleanup: Removed GameplayAudioManager from Main Menu scene.");
                        
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                    }

                    if (!isMenuOpen && !string.IsNullOrEmpty(activeScene.path))
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(activeScene.path);
                    }

                    EditorPrefs.SetBool("Datamaxxer_MainMenuAudioCleaned", true);
                };
            }
        }
    }
}
#endif
