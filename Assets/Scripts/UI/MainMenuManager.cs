using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Project.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        // ─── CONFIGURATION ───────────────────────────────────────────────────
        [Header("[ НАЗВАНИЯ СЦЕН ]")]
        [SerializeField] private string loadingSceneName  = "LoadingScene";
        [SerializeField] private string settingsSceneName = "SettingsScene";

        [Header("[ КЛЮЧ СОХРАНЕНИЯ ]")]
        [SerializeField] private string saveExistsKey = "SaveExists";

        [Header("[ ФОН ]")]
        [SerializeField] private VideoPlayer backgroundVideoPlayer;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite noirBackgroundSprite;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Image overlayImage;

        [Header("[ АНИМАЦИЯ ]")]
        [SerializeField] private Animator menuAnimator;
        [SerializeField] private string showTrigger = "Show";

        [Header("[ ЗВУК ]")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip menuMusic;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.4f;

        [Header("[ КНОПКИ ]")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            Time.timeScale = 1f;

            SetupBackground();
            SetupMusic();
            SetupContinueButton();
            BindButtons();

            if (menuAnimator != null)
                menuAnimator.SetTrigger(showTrigger);
        }

        private void SetupContinueButton()
        {
            if (continueButton == null) return;
            bool saveExists = PlayerPrefs.GetInt(saveExistsKey, 0) == 1;
            continueButton.gameObject.SetActive(saveExists);
        }

        private void SetupBackground()
        {
            if (backgroundVideoPlayer != null)
            {
                backgroundVideoPlayer.isLooping = true;
                backgroundVideoPlayer.Play();
            }
            else if (backgroundImage != null && noirBackgroundSprite != null)
            {
                backgroundImage.sprite = noirBackgroundSprite;
            }

            if (overlayImage != null)
                overlayImage.color = overlayColor;
        }

        private void SetupMusic()
        {
            if (musicSource == null || menuMusic == null) return;
            musicSource.clip   = menuMusic;
            musicSource.volume = musicVolume;
            musicSource.loop   = true;
            musicSource.Play();
        }

        private void BindButtons()
        {
            newGameButton?.onClick.AddListener(OnNewGameClicked);
            continueButton?.onClick.AddListener(OnContinueClicked);
            settingsButton?.onClick.AddListener(OnSettingsClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);
        }

        // Новая игра и Продолжить — через LoadingScene (там своя анимация)
        private void OnNewGameClicked()
        {
            PlayerPrefs.SetInt(saveExistsKey, 1);
            PlayerPrefs.Save();
            SceneTransitionManager.Instance.LoadScene(loadingSceneName);
        }

        private void OnContinueClicked()
        {
            SceneTransitionManager.Instance.LoadScene(loadingSceneName);
        }

        private void OnSettingsClicked()
        {
                // Запоминаем что пришли из главного меню
    PlayerPrefs.SetString("PreviousScene", "MainMenu");
    PlayerPrefs.Save();
    SceneTransitionManager.Instance?.LoadScene(settingsSceneName);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            newGameButton?.onClick.RemoveAllListeners();
            continueButton?.onClick.RemoveAllListeners();
            settingsButton?.onClick.RemoveAllListeners();
            quitButton?.onClick.RemoveAllListeners();
        }
    }
}