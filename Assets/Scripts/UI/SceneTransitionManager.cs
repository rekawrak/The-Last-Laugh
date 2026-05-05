using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Project.UI
{
    /// <summary>
    /// Глобальный менеджер переходов между сценами.
    /// Добавляется в каждую сцену. Делает плавный фейд при загрузке и выгрузке.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        // ─── CONFIGURATION ────────────────────────────────────────────────
        [Header("[ ФЕЙД ]")]
        [SerializeField] private float fadeInDuration  = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private Color fadeColor = Color.black;
        // ─────────────────────────────────────────────────────────────────

        public static SceneTransitionManager Instance { get; private set; }

        private Image fadeImage;
        private Canvas fadeCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CreateFadeCanvas();
        }

        private void Start()
        {
            StartCoroutine(FadeIn());
        }

        // ─── Создание Canvas для фейда ───────────────────────────────────

        private void CreateFadeCanvas()
        {
            // Создаём отдельный Canvas поверх всего
            GameObject canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform);

            fadeCanvas = canvasGO.AddComponent<Canvas>();
            fadeCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 999;

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Создаём Image которое закрывает весь экран
            GameObject imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(canvasGO.transform, false);

            fadeImage = imageGO.AddComponent<Image>();
            fadeImage.color = fadeColor;
            fadeImage.raycastTarget = true;

            RectTransform rect = imageGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }


        /// <summary>
        /// Плавно перейти в другую сцену.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            StartCoroutine(FadeOutAndLoad(sceneName));
        }

        // ─── Фейд появления (при входе в сцену) ─────────────────────────

        private IEnumerator FadeIn()
{
    // Гарантируем, что начинаем с полного черного экрана
    SetAlpha(1f);
    fadeImage.raycastTarget = true;

    // Ждем один кадр или короткую паузу, чтобы Unity успела стабилизировать FPS
    yield return new WaitForEndOfFrame(); 

    float t = 0f;
    while (t < 1f)
    {
        // Используем deltaTime, но ограничиваем шаг, чтобы при лаге не было скачка
        t += Time.unscaledDeltaTime / fadeInDuration;
        SetAlpha(Mathf.Lerp(1f, 0f, t));
        yield return null;
    }

    SetAlpha(0f);
    fadeImage.raycastTarget = false;
}

        // ─── Фейд исчезновения + загрузка сцены ─────────────────────────

        private IEnumerator FadeOutAndLoad(string sceneName)
        {
            fadeImage.raycastTarget = true;
            SetAlpha(0f);
            float t = 0f;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / fadeOutDuration;
                SetAlpha(Mathf.Lerp(0f, 1f, t));
                yield return null;
            }

            SetAlpha(1f);
            SceneManager.LoadScene(sceneName);
        }

        private void SetAlpha(float alpha)
        {
            if (fadeImage == null) return;
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
        }
    }
}