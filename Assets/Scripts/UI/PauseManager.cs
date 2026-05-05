using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class PauseManager : MonoBehaviour
    {
        // ─── CONFIGURATION ────────────────────────────────────────────────
        [Header("[ КЛАВИШИ ]")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        [Header("[ НАЗВАНИЯ СЦЕН ]")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string settingsSceneName = "SettingsScene";

        [Header("[ ОВЕРЛЕЙ ]")]
        [Tooltip("Скорость появления и исчезновения фоновой картинки")]
        [SerializeField] private float overlayFadeDuration = 0.3f;
        // ─────────────────────────────────────────────────────────────────

        [Header("[ UI ЭЛЕМЕНТЫ ]")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Image      overlayImage;
        [SerializeField] private Button     resumeButton;
        [SerializeField] private Button     settingsButton;
        [SerializeField] private Button     mainMenuButton;

        public static PauseManager Instance { get; private set; }
        public bool IsPaused { get; private set; }

        private PauseAnimator pauseAnimator;
        private Coroutine overlayCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (pausePanel != null)
            {
                pauseAnimator = pausePanel.GetComponent<PauseAnimator>();
                pausePanel.SetActive(false);
            }

            // Прячем оверлей при старте
            if (overlayImage != null)
                SetOverlayAlpha(0f);

            BindButtons();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
                TogglePause();
        }

        private void BindButtons()
        {
            resumeButton?.onClick.AddListener(Resume);
            settingsButton?.onClick.AddListener(OpenSettings);
            mainMenuButton?.onClick.AddListener(GoToMainMenu);
        }

        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            IsPaused       = true;
            Time.timeScale = 0f;

            if (pausePanel != null)
                pausePanel.SetActive(true);

            FadeOverlay(0f, 1f);
            pauseAnimator?.Show();
        }

        public void Resume()
        {
            IsPaused = false;

            FadeOverlay(1f, 0f);

            if (pauseAnimator != null)
            {
                pauseAnimator.Hide(() =>
                {
                    Time.timeScale = 1f;
                    if (pausePanel != null)
                        pausePanel.SetActive(false);
                });
            }
            else
            {
                Time.timeScale = 1f;
                if (pausePanel != null)
                    pausePanel.SetActive(false);
            }
        }

        public void OpenSettings()
        {
             // Запоминаем что пришли из паузы
    PlayerPrefs.SetString("PreviousScene", "GameScene");
    PlayerPrefs.Save();
    Time.timeScale = 1f;
    SceneTransitionManager.Instance?.LoadScene(settingsSceneName);
        }

        public void GoToMainMenu()
        {
            IsPaused       = false;
            Time.timeScale = 1f;
            SceneTransitionManager.Instance?.LoadScene(mainMenuSceneName);
        }

        // ─── Анимация оверлея ─────────────────────────────────────────────

        private void FadeOverlay(float from, float to)
        {
            if (overlayImage == null) return;

            if (overlayCoroutine != null)
                StopCoroutine(overlayCoroutine);

            overlayCoroutine = StartCoroutine(FadeRoutine(from, to));
        }

        private System.Collections.IEnumerator FadeRoutine(float from, float to)
        {
            float t = 0f;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / overlayFadeDuration;
                SetOverlayAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t)));
                yield return null;
            }

            SetOverlayAlpha(to);
        }

        private void SetOverlayAlpha(float alpha)
        {
            Color c = overlayImage.color;
            c.a = alpha;
            overlayImage.color = c;
        }

        private void OnDestroy()
        {
            resumeButton?.onClick.RemoveAllListeners();
            settingsButton?.onClick.RemoveAllListeners();
            mainMenuButton?.onClick.RemoveAllListeners();
            Time.timeScale = 1f;
        }
    }
}