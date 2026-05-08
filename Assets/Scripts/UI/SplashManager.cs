using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

namespace Project.UI
{
    /// <summary>
    /// Управляет двумя заставками при запуске:
    /// 1. Видео студии
    /// 2. Картинка игры с музыкой и кнопкой пропуска
    /// После обеих — переход в главное меню.
    /// </summary>
    public class SplashManager : MonoBehaviour
    {
        // ─── CONFIGURATION ────────────────────────────────────────────────
        [Header("[ НАЗВАНИЯ СЦЕН ]")]
        [SerializeField] private string mainMenuScene = "MainMenu";

        [Header("[ ЗАСТАВКА СТУДИИ ]")]
        [SerializeField] private GameObject studioSplashGO;
        [SerializeField] private VideoPlayer studioVideoPlayer;
        [Tooltip("Максимальное время ожидания видео (секунд)")]
        [SerializeField] private float studioMaxWait = 10f;

        [Header("[ ЗАСТАВКА ИГРЫ ]")]
        [SerializeField] private GameObject gameSplashGO;
        [SerializeField] private Image      artImage;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip   splashMusic;
        [SerializeField] private TextMeshProUGUI skipText;
        [Tooltip("Время появления картинки")]
        [SerializeField] private float artFadeInDuration  = 2f;
        [Tooltip("Как долго показывается заставка игры")]
        [SerializeField] private float splashHoldDuration = 20f;
        [Tooltip("Задержка перед появлением кнопки пропуска")]
        [SerializeField] private float skipTextDelay      = 2.5f;
        [Tooltip("Скорость появления текста пропуска")]
        [SerializeField] private float skipTextFadeDuration = 0.8f;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.7f;

        [Header("[ ОБЩИЙ ФЕЙД ]")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private float globalFadeDuration = 0.8f;
        // ─────────────────────────────────────────────────────────────────

        private bool skipPressed;
        private bool gameSplashActive;

        private void Start()
        {
            Time.timeScale = 1f;

            // Начинаем с чёрного экрана
            SetFadeAlpha(1f);
            SetArtAlpha(0f);
            SetSkipAlpha(0f);

            studioSplashGO?.SetActive(true);
            gameSplashGO?.SetActive(false);

            StartCoroutine(RunSplashSequence());
        }

        private void Update()
        {
            // Пропуск кликом или пробелом — только во время заставки игры
            if (!gameSplashActive) return;

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                skipPressed = true;
        }

        // ─── Основная последовательность ─────────────────────────────────

        private IEnumerator RunSplashSequence()
        {
            yield return StartCoroutine(PlayStudioSplash());
            yield return StartCoroutine(PlayGameSplash());
            yield return StartCoroutine(GoToMainMenu());
        }

        // ─── Заставка студии ─────────────────────────────────────────────

        private IEnumerator PlayStudioSplash()
        {
            if (studioVideoPlayer == null)
            {
                studioSplashGO?.SetActive(false);
                yield break;
            }

            // Фейд появления
            yield return StartCoroutine(Fade(1f, 0f, globalFadeDuration));

            studioVideoPlayer.Play();

            float elapsed = 0f;
            while (studioVideoPlayer.isPlaying && elapsed < studioMaxWait)
            {
                // Пропуск кликом
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                {
                    studioVideoPlayer.Stop();
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Фейд исчезновения
            yield return StartCoroutine(Fade(0f, 1f, globalFadeDuration));
            studioSplashGO?.SetActive(false);

            yield return new WaitForSeconds(0.2f);
        }

        // ─── Заставка игры ───────────────────────────────────────────────

        private IEnumerator PlayGameSplash()
        {
            gameSplashGO?.SetActive(true);
            gameSplashActive = false;
            skipPressed      = false;

            // Запускаем музыку
            if (musicSource != null && splashMusic != null)
            {
                musicSource.clip   = splashMusic;
                musicSource.volume = 0f;
                musicSource.loop   = true;
                musicSource.Play();
            }

            // Фейд появления экрана
            yield return StartCoroutine(Fade(1f, 0f, globalFadeDuration));

            gameSplashActive = true;

            // Плавное появление картинки + нарастание музыки
            yield return StartCoroutine(FadeInArtAndMusic());

            // Ждём появления кнопки пропуска
            yield return new WaitForSeconds(skipTextDelay);
            yield return StartCoroutine(FadeSkipText(0f, 1f));

            // Держим заставку — ждём таймер или нажатие
            float holdTimer = 0f;
            while (holdTimer < splashHoldDuration && !skipPressed)
            {
                holdTimer += Time.deltaTime;
                yield return null;
            }

            // Плавное исчезновение текста пропуска
            yield return StartCoroutine(FadeSkipText(1f, 0f));
        }

        private IEnumerator FadeInArtAndMusic()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / artFadeInDuration;
                float eased = EaseInOut(Mathf.Clamp01(t));

                SetArtAlpha(eased);

                if (musicSource != null)
                    musicSource.volume = Mathf.Lerp(0f, musicVolume, eased);

                yield return null;
            }

            SetArtAlpha(1f);
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }

        // ─── Переход в главное меню ──────────────────────────────────────

        private IEnumerator GoToMainMenu()
        {
            // Затухание музыки и экрана одновременно
            StartCoroutine(FadeOutMusic());
            yield return StartCoroutine(Fade(0f, 1f, globalFadeDuration));

            gameSplashGO?.SetActive(false);

            // Используем SceneTransitionManager если есть, иначе напрямую
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene(mainMenuScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
        }

        private IEnumerator FadeOutMusic()
        {
            if (musicSource == null) yield break;

            float startVolume = musicSource.volume;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / globalFadeDuration;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            musicSource.Stop();
        }

        // ─── Вспомогательные методы ──────────────────────────────────────

        private IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                SetFadeAlpha(Mathf.Lerp(from, to, EaseInOut(Mathf.Clamp01(t))));
                yield return null;
            }
            SetFadeAlpha(to);
        }

        private IEnumerator FadeSkipText(float from, float to)
        {
            if (skipText == null) yield break;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / skipTextFadeDuration;
                SetSkipAlpha(Mathf.Lerp(from, to, EaseInOut(Mathf.Clamp01(t))));
                yield return null;
            }
            SetSkipAlpha(to);
        }

        private void SetFadeAlpha(float a)
        {
            if (fadeOverlay == null) return;
            Color c = fadeOverlay.color;
            c.a = a;
            fadeOverlay.color = c;
        }

        private void SetArtAlpha(float a)
        {
            if (artImage == null) return;
            Color c = artImage.color;
            c.a = a;
            artImage.color = c;
        }

        private void SetSkipAlpha(float a)
        {
            if (skipText == null) return;
            Color c = skipText.color;
            c.a = a;
            skipText.color = c;
        }

        private float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}