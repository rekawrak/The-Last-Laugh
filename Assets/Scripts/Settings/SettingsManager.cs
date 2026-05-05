using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using TMPro;

namespace Project.UI
{
    /// <summary>
    /// Менеджер настроек. Звук, графика, полноэкранный режим, разрешение.
    /// Возвращает игрока туда откуда он пришёл.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        // ─── CONFIGURATION ────────────────────────────────────────────────
        [Header("[ КЛЮЧИ PLAYERPREFS ]")]
        [SerializeField] private string musicVolumeKey    = "MusicVolume";
        [SerializeField] private string sfxVolumeKey      = "SFXVolume";
        [SerializeField] private string qualityKey        = "QualityLevel";
        [SerializeField] private string fullscreenKey     = "Fullscreen";
        [SerializeField] private string resolutionKey     = "Resolution";
        [SerializeField] private string previousSceneKey  = "PreviousScene";

        [Header("[ АУДИО МИКСЕР ]")]
        [Tooltip("Назначь AudioMixer из папки Audio")]
        [SerializeField] private AudioMixer audioMixer;
        [Tooltip("Название параметра музыки в AudioMixer")]
        [SerializeField] private string musicMixerParam = "MusicVolume";
        [Tooltip("Название параметра звуков в AudioMixer")]
        [SerializeField] private string sfxMixerParam   = "SFXVolume";

        [Header("[ ФОН ]")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite noirBackgroundSprite;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Image overlayImage;
        // ─────────────────────────────────────────────────────────────────

        [Header("[ ЗВУК ]")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicValueText;
        [SerializeField] private TextMeshProUGUI sfxValueText;

        [Header("[ ГРАФИКА ]")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("[ КНОПКИ ]")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button backButton;

        private Resolution[] availableResolutions;
        private int selectedResolutionIndex;
        private int selectedQualityIndex;

        private void Start()
        {
            Time.timeScale = 1f;

            SetupBackground();
            SetupResolutions();
            SetupQuality();
            LoadSettings();
            BindControls();
        }

        // ─── Фон ─────────────────────────────────────────────────────────

        private void SetupBackground()
        {
            if (backgroundImage != null && noirBackgroundSprite != null)
                backgroundImage.sprite = noirBackgroundSprite;

            if (overlayImage != null)
                overlayImage.color = overlayColor;
        }

        // ─── Разрешения ──────────────────────────────────────────────────

        private void SetupResolutions()
        {
            if (resolutionDropdown == null) return;

            availableResolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            int currentIndex = 0;
            var options = new System.Collections.Generic.List<string>();

            for (int i = 0; i < availableResolutions.Length; i++)
            {
                Resolution r = availableResolutions[i];
                options.Add($"{r.width} x {r.height} @ {r.refreshRateRatio.numerator}Hz");

                if (r.width  == Screen.currentResolution.width &&
                    r.height == Screen.currentResolution.height)
                    currentIndex = i;
            }

            resolutionDropdown.AddOptions(options);

            selectedResolutionIndex = PlayerPrefs.GetInt(resolutionKey, currentIndex);
            resolutionDropdown.value = selectedResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        // ─── Качество графики ─────────────────────────────────────────────

        private void SetupQuality()
        {
            if (qualityDropdown == null) return;

            qualityDropdown.ClearOptions();

            // Берём названия пресетов прямо из Unity
            var options = new System.Collections.Generic.List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(options);

            selectedQualityIndex     = PlayerPrefs.GetInt(qualityKey, QualitySettings.GetQualityLevel());
            qualityDropdown.value    = selectedQualityIndex;
            qualityDropdown.RefreshShownValue();
        }

        // ─── Загрузка сохранённых настроек ───────────────────────────────

        private void LoadSettings()
        {
            float music = PlayerPrefs.GetFloat(musicVolumeKey, 0.8f);
            float sfx   = PlayerPrefs.GetFloat(sfxVolumeKey,   1f);

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = music;
                UpdateMusicText(music);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = sfx;
                UpdateSFXText(sfx);
            }

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = PlayerPrefs.GetInt(fullscreenKey, 1) == 1;

            ApplyAudioMixer(musicMixerParam, music);
            ApplyAudioMixer(sfxMixerParam,   sfx);
        }

        // ─── Привязка элементов ───────────────────────────────────────────

        private void BindControls()
        {
            musicVolumeSlider?.onValueChanged.AddListener(OnMusicChanged);
            sfxVolumeSlider?.onValueChanged.AddListener(OnSFXChanged);
            qualityDropdown?.onValueChanged.AddListener(OnQualityChanged);
            resolutionDropdown?.onValueChanged.AddListener(OnResolutionChanged);
            fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
            applyButton?.onClick.AddListener(ApplyAndSave);
            backButton?.onClick.AddListener(GoBack);
        }

        // ─── Обработчики изменений ────────────────────────────────────────

        private void OnMusicChanged(float value)
        {
            ApplyAudioMixer(musicMixerParam, value);
            UpdateMusicText(value);
        }

        private void OnSFXChanged(float value)
        {
            ApplyAudioMixer(sfxMixerParam, value);
            UpdateSFXText(value);
        }

        private void OnQualityChanged(int index)
        {
            selectedQualityIndex = index;
        }

        private void OnResolutionChanged(int index)
        {
            selectedResolutionIndex = index;
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        // ─── Применение AudioMixer ────────────────────────────────────────

        private void ApplyAudioMixer(string parameter, float value)
        {
            if (audioMixer == null) return;

            // Переводим линейное значение слайдера в децибелы
            float db = value > 0.0001f
                ? Mathf.Log10(value) * 20f
                : -80f;

            audioMixer.SetFloat(parameter, db);
        }

        // ─── Текст значений слайдеров ─────────────────────────────────────

        private void UpdateMusicText(float value)
        {
            if (musicValueText != null)
                musicValueText.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        private void UpdateSFXText(float value)
        {
            if (sfxValueText != null)
                sfxValueText.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        // ─── Сохранить и применить всё ────────────────────────────────────

        private void ApplyAndSave()
        {
            // Звук
            float music = musicVolumeSlider != null ? musicVolumeSlider.value : 0.8f;
            float sfx   = sfxVolumeSlider   != null ? sfxVolumeSlider.value   : 1f;

            PlayerPrefs.SetFloat(musicVolumeKey, music);
            PlayerPrefs.SetFloat(sfxVolumeKey,   sfx);

            // Качество графики
            QualitySettings.SetQualityLevel(selectedQualityIndex, true);
            PlayerPrefs.SetInt(qualityKey, selectedQualityIndex);

            // Разрешение
            if (availableResolutions != null && availableResolutions.Length > 0)
            {
                Resolution r = availableResolutions[selectedResolutionIndex];
                Screen.SetResolution(r.width, r.height, Screen.fullScreen);
                PlayerPrefs.SetInt(resolutionKey, selectedResolutionIndex);
            }

            // Полный экран
            if (fullscreenToggle != null)
                PlayerPrefs.SetInt(fullscreenKey, fullscreenToggle.isOn ? 1 : 0);

            PlayerPrefs.Save();
            Debug.Log("[SettingsManager] Настройки сохранены.");
        }

        // ─── Назад ───────────────────────────────────────────────────────

        private void GoBack()
        {
            // Автоматически сохраняем перед выходом
            ApplyAndSave();

            string previousScene = PlayerPrefs.GetString(previousSceneKey, "MainMenu");
            SceneTransitionManager.Instance?.LoadScene(previousScene);
        }

        private void OnDestroy()
        {
            musicVolumeSlider?.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider?.onValueChanged.RemoveAllListeners();
            qualityDropdown?.onValueChanged.RemoveAllListeners();
            resolutionDropdown?.onValueChanged.RemoveAllListeners();
            fullscreenToggle?.onValueChanged.RemoveAllListeners();
            applyButton?.onClick.RemoveAllListeners();
            backButton?.onClick.RemoveAllListeners();
        }
    }
}