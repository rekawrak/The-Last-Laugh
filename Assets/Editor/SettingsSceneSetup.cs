#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Project.Editor
{
    public class SettingsSceneSetup : EditorWindow
    {
        [MenuItem("Tools/Setup Settings Scene")]
        public static void Run()
        {
            SetupCamera();
            SetupCanvas();
            SetupBackground();
            SetupSettingsPanel();
            SetupTitle();
            SetupSoundSection();
            SetupGraphicsSection();
            SetupButtons();

            // Сохраняем сцену
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );

            Debug.Log("[SettingsSceneSetup] Готово.");
        }

        // ─── Камера ──────────────────────────────────────────────────────

        private static void SetupCamera()
        {
            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null) return;
            cam.backgroundColor = Color.black;
            cam.clearFlags      = CameraClearFlags.SolidColor;
            EditorUtility.SetDirty(cam);
        }

        // ─── Canvas ──────────────────────────────────────────────────────

        private static void SetupCanvas()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;

            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        // ─── Фон и оверлей ───────────────────────────────────────────────

        private static void SetupBackground()
        {
            // Загружаем спрайт
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/pause_overlay.png"
            );

            // BackgroundImage — растянуть на весь экран
            GameObject bgGO = GameObject.Find("BackgroundImage");
            if (bgGO != null)
            {
                RectTransform rect = bgGO.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image img = bgGO.GetComponent<Image>();
                if (img == null) img = bgGO.AddComponent<Image>();

                if (bgSprite != null)
                    img.sprite = bgSprite;

                img.color         = Color.white;
                img.type          = Image.Type.Simple;
                img.preserveAspect = false;
                EditorUtility.SetDirty(bgGO);
            }

            // Overlay — затемнение поверх фона
            GameObject overlayGO = GameObject.Find("Overlay");
            if (overlayGO != null)
            {
                RectTransform rect = overlayGO.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image img = overlayGO.GetComponent<Image>();
                if (img == null) img = overlayGO.AddComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.6f);
                EditorUtility.SetDirty(overlayGO);
            }
        }

        // ─── SettingsPanel ───────────────────────────────────────────────

        private static void SetupSettingsPanel()
        {
            GameObject panel = GameObject.Find("SettingsPanel");
            if (panel == null) return;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = panel.GetComponent<Image>();
            if (img != null) img.color = new Color(0f, 0f, 0f, 0f);

            VerticalLayoutGroup vlg = GetOrAdd<VerticalLayoutGroup>(panel);
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.spacing                = 24f;
            vlg.padding                = new RectOffset(0, 0, 80, 40);
            vlg.childControlWidth      = false;
            vlg.childControlHeight     = false;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;
            EditorUtility.SetDirty(panel);
        }

        // ─── Title ───────────────────────────────────────────────────────

        private static void SetupTitle()
        {
            GameObject titleGO = GameObject.Find("Title");
            if (titleGO == null) return;

            // Убираем Image если нет спрайта — показываем текст
            Image img = titleGO.GetComponent<Image>();
            if (img != null && img.sprite == null)
            {
                Object.DestroyImmediate(img);

                TextMeshProUGUI tmp = titleGO.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = titleGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = "НАСТРОЙКИ";
                tmp.fontSize  = 64f;
                tmp.color     = new Color(0.86f, 0.78f, 0.63f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
            }

            LayoutElement le = GetOrAdd<LayoutElement>(titleGO);
            le.preferredWidth  = 600f;
            le.preferredHeight = 120f;
            EditorUtility.SetDirty(titleGO);
        }

        // ─── SoundSection ────────────────────────────────────────────────

        private static void SetupSoundSection()
        {
            GameObject section = GameObject.Find("SoundSection");
            if (section == null) return;

            LayoutElement le = GetOrAdd<LayoutElement>(section);
            le.preferredWidth  = 700f;
            le.preferredHeight = 160f;

            VerticalLayoutGroup vlg = GetOrAdd<VerticalLayoutGroup>(section);
            vlg.childAlignment         = TextAnchor.MiddleCenter;
            vlg.spacing                = 20f;
            vlg.childControlWidth      = false;
            vlg.childControlHeight     = false;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;

            SetupSliderRow("MusicSlider", "Музыка");
            SetupSliderRow("SFXSlider",   "Звуки");
            EditorUtility.SetDirty(section);
        }

        private static void SetupSliderRow(string rowName, string labelText)
        {
            GameObject row = GameObject.Find(rowName);
            if (row == null) return;

            LayoutElement le = GetOrAdd<LayoutElement>(row);
            le.preferredWidth  = 700f;
            le.preferredHeight = 60f;

            HorizontalLayoutGroup hlg = GetOrAdd<HorizontalLayoutGroup>(row);
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.spacing                = 20f;
            hlg.padding                = new RectOffset(80, 80, 0, 0);
            hlg.childControlWidth      = false;
            hlg.childControlHeight     = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            // Label
            GameObject labelGO = FindChild(row, "Label");
            if (labelGO != null)
            {
                LayoutElement labelLE = GetOrAdd<LayoutElement>(labelGO);
                labelLE.preferredWidth  = 160f;
                labelLE.preferredHeight = 50f;

                TextMeshProUGUI tmp = labelGO.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text      = labelText;
                    tmp.fontSize  = 26f;
                    tmp.color     = new Color(0.86f, 0.78f, 0.63f, 1f);
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                }
                EditorUtility.SetDirty(labelGO);
            }

            // Slider
            GameObject sliderGO = FindChild(row, "Slider");
            if (sliderGO != null)
            {
                LayoutElement sliderLE = GetOrAdd<LayoutElement>(sliderGO);
                sliderLE.preferredWidth  = 380f;
                sliderLE.preferredHeight = 50f;

                Slider slider = sliderGO.GetComponent<Slider>();
                if (slider != null)
                {
                    slider.minValue     = 0f;
                    slider.maxValue     = 1f;
                    slider.value        = 0.8f;
                    slider.wholeNumbers = false;
                }
                EditorUtility.SetDirty(sliderGO);
            }

            // ValueText
            GameObject valueGO = FindChild(row, "ValueText");
            if (valueGO != null)
            {
                LayoutElement valueLE = GetOrAdd<LayoutElement>(valueGO);
                valueLE.preferredWidth  = 80f;
                valueLE.preferredHeight = 50f;

                TextMeshProUGUI tmp = valueGO.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text      = "80%";
                    tmp.fontSize  = 26f;
                    tmp.color     = new Color(0.86f, 0.78f, 0.63f, 1f);
                    tmp.alignment = TextAlignmentOptions.MidlineRight;
                }
                EditorUtility.SetDirty(valueGO);
            }

            EditorUtility.SetDirty(row);
        }

        // ─── GraphicsSection ─────────────────────────────────────────────

        private static void SetupGraphicsSection()
        {
            GameObject section = GameObject.Find("GraphicsSection");
            if (section == null) return;

            LayoutElement le = GetOrAdd<LayoutElement>(section);
            le.preferredWidth  = 700f;
            le.preferredHeight = 220f;

            VerticalLayoutGroup vlg = GetOrAdd<VerticalLayoutGroup>(section);
            vlg.childAlignment         = TextAnchor.MiddleCenter;
            vlg.spacing                = 20f;
            vlg.childControlWidth      = false;
            vlg.childControlHeight     = false;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;

            SetupDropdown("QualityDropdown",    "Качество графики");
            SetupDropdown("ResolutionDropdown", "Разрешение");
            SetupFullscreenToggle();

            EditorUtility.SetDirty(section);
        }

        private static void SetupDropdown(string name, string placeholder)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) return;

            LayoutElement le = GetOrAdd<LayoutElement>(go);
            le.preferredWidth  = 700f;
            le.preferredHeight = 60f;

            // Цвет текста дропдауна
            TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();
            if (dropdown != null)
            {
                dropdown.captionText.color = new Color(0.86f, 0.78f, 0.63f, 1f);
                dropdown.captionText.fontSize = 24f;
            }

            EditorUtility.SetDirty(go);
        }

        private static void SetupFullscreenToggle()
        {
            GameObject go = GameObject.Find("FullscreenToggle");
            if (go == null) return;

            LayoutElement le = GetOrAdd<LayoutElement>(go);
            le.preferredWidth  = 700f;
            le.preferredHeight = 60f;

            // Текст тогла
            GameObject labelGO = FindChild(go, "Label");
            if (labelGO != null)
            {
                TextMeshProUGUI tmp = labelGO.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text      = "Полный экран";
                    tmp.fontSize  = 26f;
                    tmp.color     = new Color(0.86f, 0.78f, 0.63f, 1f);
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                }
                EditorUtility.SetDirty(labelGO);
            }

            // Размер самого чекбокса
            GameObject bgGO = FindChild(go, "Background");
            if (bgGO != null)
            {
                RectTransform rect = bgGO.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(40f, 40f);
                EditorUtility.SetDirty(bgGO);
            }

            EditorUtility.SetDirty(go);
        }

        // ─── Кнопки ──────────────────────────────────────────────────────

        private static void SetupButtons()
        {
            SetupButton("Button_Apply", "Применить");
            SetupButton("Button_Back",  "Назад");
        }

        private static void SetupButton(string name, string label)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) return;

            LayoutElement le = GetOrAdd<LayoutElement>(go);
            le.preferredWidth  = 320f;
            le.preferredHeight = 80f;

            Button btn = go.GetComponent<Button>();
            if (btn != null)
                btn.transition = Selectable.Transition.None;

            TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text      = label;
                tmp.fontSize  = 28f;
                tmp.color     = new Color(0.86f, 0.78f, 0.63f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
            }

            EditorUtility.SetDirty(go);
        }

        // ─── Вспомогательные ─────────────────────────────────────────────

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        private static GameObject FindChild(GameObject parent, string childName)
        {
            foreach (Transform child in parent.transform)
                if (child.name == childName) return child.gameObject;
            return null;
        }
    }
}
#endif