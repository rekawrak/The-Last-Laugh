using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.Events;

namespace Project.Visuals
{
    /// <summary>
    /// Универсальный контроллер катсцен для всей игры.
    /// 
    /// ЗАПУСК:
    ///   cutsceneController.Play()               — из триггера/скрипта
    ///   cutsceneController.PlayThenPlay(next)   — цепочка катсцен
    /// 
    /// ВОЗВРАТ В ГЕЙМПЛЕЙ:
    ///   onSequenceComplete → ResumeGameplay()
    /// </summary>
    public class CutsceneController : MonoBehaviour
    {
        // ─── ТИПЫ ШАГОВ ──────────────────────────────────────────────────
        public enum StepType
        {
            // Визуал — базовый
            ShowObject,
            HideObject,
            FadeIn,
            FadeOut,
            FadeOverlayIn,
            FadeOverlayOut,
            Wait,
            ZoomIn,

            // Визуал — комикс
            ShowPanel,         // показать панель комикса с анимацией
            HidePanel,         // скрыть панель
            ShakeObject,       // тряска объекта (удар, взрыв)

            // Диалог
            ShowDialogue,      // показать субтитр внизу экрана
            HideDialogue,      // скрыть субтитр
            ShowNPCPhrase,     // фраза над головой NPC
            HideNPCPhrase,

            // Аудио
            PlayMusic,
            StopMusic,
            FadeInMusic,
            FadeOutMusic,
            PlaySound,
            MuteGameAudio,
            UnmuteGameAudio,

            // Камера
            ShakeCamera,       // тряска камеры
            ZoomCamera,        // приближение/отдаление камеры

            // Игра
            PauseGame,         // явная пауза
            ResumeGame,        // явное снятие паузы
            EnablePlayer,      // включить управление
            DisablePlayer,     // выключить управление
            FireUnityEvent,    // вызвать кастомное событие
        }

        // ─── ШАГ ─────────────────────────────────────────────────────────
        [System.Serializable]
        public class CutsceneStep
        {
            [Tooltip("Тип действия")]
            public StepType type;

            [Tooltip("UI объект или GameObject над которым действуем")]
            public GameObject target;

            [Tooltip("Длительность действия")]
            public float duration = 1f;

            [Tooltip("Пауза ПОСЛЕ шага")]
            public float holdAfter = 0f;

            [Header("─ Zoom / Shake ─")]
            public float zoomFrom  = 1.15f;
            public float zoomTo    = 1.0f;
            public float shakeStrength = 5f;

            [Header("─ Диалог ─")]
            [Tooltip("Текст субтитра или фразы NPC")]
            [TextArea(2, 4)]
            public string dialogueText = "";
            [Tooltip("Имя персонажа (опционально)")]
            public string speakerName  = "";
            [Tooltip("Портрет персонажа (опционально)")]
            public Sprite speakerPortrait;
            [Tooltip("Скорость печатания текста (симв/сек). 0 = мгновенно")]
            public float typeSpeed = 30f;

            [Header("─ Аудио ─")]
            public AudioClip audioClip;
            [Range(0f, 1f)]
            public float volume = 1f;
            public bool  loop   = false;

            [Header("─ Камера ─")]
            public float cameraZoomTarget   = 5f;
            public float cameraZoomDuration = 1f;

            [Header("─ Кастомное событие ─")]
            public UnityEvent customEvent;
        }

        // ─── CONFIGURATION ────────────────────────────────────────────────
        [Header("[ ШАГИ ]")]
        public List<CutsceneStep> steps = new List<CutsceneStep>();

        [Header("[ СЛЕДУЮЩАЯ КАТСЦЕНА ]")]
        [Tooltip("Если назначена — запустится сразу после этой")]
        [SerializeField] private CutsceneController nextCutscene;

        [Header("[ UI — БАЗОВЫЕ ]")]
        [SerializeField] private Image      fadeOverlayImage;
        [SerializeField] private GameObject skipTextGO;
        [SerializeField] private float      skipShowDelay    = 2f;
        [SerializeField] private float      skipFadeDuration = 0.6f;

        [Header("[ UI — ДИАЛОГ ]")]
        [Tooltip("Панель субтитров внизу экрана")]
        [SerializeField] private GameObject   dialoguePanel;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private Image           speakerPortraitImage;

        [Header("[ АУДИО ]")]
        [SerializeField] private AudioSource cutsceneMusicSource;
        [SerializeField] private AudioSource cutsceneSFXSource;
        [SerializeField] private AudioMixer  audioMixer;
        [SerializeField] private string      gameMixerVolumeParam  = "GameVolume";
        [SerializeField] private float       gameAudioFadeDuration = 0.5f;

        [Header("[ КАМЕРА ]")]
        [SerializeField] private Camera gameCamera;
        private float originalCameraSize;

        [Header("[ ИГРОК ]")]
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Имя скрипта управления игроком — будет выключаться во время катсцены")]
        [SerializeField] private string playerControllerName = "PlayerController";

        [Header("[ НАСТРОЙКИ ]")]
        [SerializeField] private bool  pauseGameDuringCutscene = true;
        [SerializeField] private bool  playOnStart             = false;
        [SerializeField] private float startDelay              = 0f;

        [Header("[ СОБЫТИЯ ]")]
        public UnityEvent onSequenceStart;
        public UnityEvent onSequenceComplete;
        // ─────────────────────────────────────────────────────────────────

        private bool         skipPressed;
        private bool         isPlaying;
        private MonoBehaviour playerController;

        private void Awake()
        {
            SetFadeAlpha(1f);
            SetGraphicAlpha(skipTextGO, 0f);

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (gameCamera != null)
                originalCameraSize = gameCamera.orthographicSize;

            FindPlayerController();
        }

        private void Start()
        {
            if (playOnStart)
                StartCoroutine(RunWithDelay());
        }

        // ─── ПУБЛИЧНЫЕ МЕТОДЫ ─────────────────────────────────────────────

        /// <summary>
        /// Запустить катсцену
        /// </summary>
        public void Play()
        {
            if (!isPlaying)
                StartCoroutine(RunWithDelay());
        }

        /// <summary>
        /// Запустить и после завершения запустить следующую
        /// </summary>
        public void PlayThenPlay(CutsceneController next)
        {
            nextCutscene = next;
            Play();
        }

        /// <summary>
        /// Пропустить. Вешай на кнопку Skip → OnClick
        /// </summary>
        public void OnSkipClicked() => skipPressed = true;

        /// <summary>
        /// Вернуть управление игроку. Назначай в onSequenceComplete.
        /// </summary>
        public void ResumeGameplay()
        {
            SetPlayerControl(true);
            if (pauseGameDuringCutscene)
                Time.timeScale = 1f;
            StartCoroutine(UnmuteGameAudioRoutine());
        }

        // ─── ПОСЛЕДОВАТЕЛЬНОСТЬ ──────────────────────────────────────────

        private IEnumerator RunWithDelay()
        {
            if (startDelay > 0f)
                yield return WaitUnscaled(startDelay);

            yield return StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            isPlaying   = true;
            skipPressed = false;

            SetPlayerControl(false);
            yield return StartCoroutine(MuteGameAudioRoutine());

            if (pauseGameDuringCutscene)
                Time.timeScale = 0f;

            onSequenceStart?.Invoke();
            StartCoroutine(ShowSkipTextDelayed());

            foreach (CutsceneStep step in steps)
            {
                if (skipPressed) break;
                yield return StartCoroutine(ExecuteStep(step));

                if (step.holdAfter > 0f && !skipPressed)
                    yield return WaitUnscaled(step.holdAfter);
            }

            if (skipPressed)
                yield return StartCoroutine(FastForward());
            else
                yield return StartCoroutine(Finish());
        }

        // ─── ВЫПОЛНЕНИЕ ШАГА ─────────────────────────────────────────────

        private IEnumerator ExecuteStep(CutsceneStep step)
        {
            switch (step.type)
            {
                // ── Базовый визуал ────────────────────────────────────────
                case StepType.ShowObject:
                    step.target?.SetActive(true);
                    break;

                case StepType.HideObject:
                    step.target?.SetActive(false);
                    break;

                case StepType.FadeIn:
                    if (step.target != null) step.target.SetActive(true);
                    yield return StartCoroutine(FadeGraphic(step.target, 0f, 1f, step.duration));
                    break;

                case StepType.FadeOut:
                    yield return StartCoroutine(FadeGraphic(step.target, 1f, 0f, step.duration));
                    step.target?.SetActive(false);
                    break;

                case StepType.FadeOverlayIn:
                    yield return StartCoroutine(FadeOverlay(0f, 1f, step.duration));
                    break;

                case StepType.FadeOverlayOut:
                    if (pauseGameDuringCutscene) Time.timeScale = 1f;
                    yield return StartCoroutine(FadeOverlay(1f, 0f, step.duration));
                    break;

                case StepType.Wait:
                    yield return WaitUnscaled(step.duration);
                    break;

                case StepType.ZoomIn:
                    if (step.target != null) step.target.SetActive(true);
                    yield return StartCoroutine(ZoomAndFade(
                        step.target, step.zoomFrom, step.zoomTo, step.duration
                    ));
                    break;

                // ── Комикс-панели ─────────────────────────────────────────
                case StepType.ShowPanel:
                    if (step.target != null) step.target.SetActive(true);
                    yield return StartCoroutine(FadeGraphic(step.target, 0f, 1f, step.duration));
                    break;

                case StepType.HidePanel:
                    yield return StartCoroutine(FadeGraphic(step.target, 1f, 0f, step.duration));
                    step.target?.SetActive(false);
                    break;

                case StepType.ShakeObject:
                    yield return StartCoroutine(ShakeObject(step.target, step.duration, step.shakeStrength));
                    break;

                // ── Диалог ────────────────────────────────────────────────
                case StepType.ShowDialogue:
                    yield return StartCoroutine(ShowDialogueRoutine(step));
                    break;

                case StepType.HideDialogue:
                    if (dialoguePanel != null)
                        yield return StartCoroutine(FadeGraphic(dialoguePanel, 1f, 0f, step.duration));
                    dialoguePanel?.SetActive(false);
                    break;

                case StepType.ShowNPCPhrase:
                    if (step.target != null)
                    {
                        TextMeshProUGUI npcText = step.target.GetComponent<TextMeshProUGUI>();
                        if (npcText != null) npcText.text = step.dialogueText;
                        step.target.SetActive(true);
                        yield return StartCoroutine(FadeGraphic(step.target, 0f, 1f, 0.3f));
                    }
                    break;

                case StepType.HideNPCPhrase:
                    if (step.target != null)
                    {
                        yield return StartCoroutine(FadeGraphic(step.target, 1f, 0f, 0.3f));
                        step.target.SetActive(false);
                    }
                    break;

                // ── Аудио ─────────────────────────────────────────────────
                case StepType.PlayMusic:
                    PlayCutsceneMusic(step.audioClip, step.volume, step.loop);
                    break;

                case StepType.StopMusic:
                    cutsceneMusicSource?.Stop();
                    break;

                case StepType.FadeInMusic:
                    PlayCutsceneMusic(step.audioClip, 0f, step.loop);
                    yield return StartCoroutine(FadeMusicVolume(0f, step.volume, step.duration));
                    break;

                case StepType.FadeOutMusic:
                    float fromVol = cutsceneMusicSource != null ? cutsceneMusicSource.volume : 1f;
                    yield return StartCoroutine(FadeMusicVolume(fromVol, 0f, step.duration));
                    cutsceneMusicSource?.Stop();
                    break;

                case StepType.PlaySound:
                    if (cutsceneSFXSource != null && step.audioClip != null)
                        cutsceneSFXSource.PlayOneShot(step.audioClip, step.volume);
                    break;

                case StepType.MuteGameAudio:
                    yield return StartCoroutine(MuteGameAudioRoutine());
                    break;

                case StepType.UnmuteGameAudio:
                    yield return StartCoroutine(UnmuteGameAudioRoutine());
                    break;

                // ── Камера ────────────────────────────────────────────────
                case StepType.ShakeCamera:
                    yield return StartCoroutine(ShakeCameraRoutine(step.duration, step.shakeStrength));
                    break;

                case StepType.ZoomCamera:
                    yield return StartCoroutine(ZoomCameraRoutine(
                        step.cameraZoomTarget, step.cameraZoomDuration
                    ));
                    break;

                // ── Игра ──────────────────────────────────────────────────
                case StepType.PauseGame:
                    Time.timeScale = 0f;
                    break;

                case StepType.ResumeGame:
                    Time.timeScale = 1f;
                    break;

                case StepType.EnablePlayer:
                    SetPlayerControl(true);
                    break;

                case StepType.DisablePlayer:
                    SetPlayerControl(false);
                    break;

                case StepType.FireUnityEvent:
                    step.customEvent?.Invoke();
                    break;
            }
        }

        // ─── ДИАЛОГ ──────────────────────────────────────────────────────

        private IEnumerator ShowDialogueRoutine(CutsceneStep step)
        {
            if (dialoguePanel == null) yield break;

            // Заполняем данные
            if (dialogueText      != null) dialogueText.text      = "";
            if (speakerNameText   != null) speakerNameText.text   = step.speakerName;
            if (speakerPortraitImage != null)
            {
                speakerPortraitImage.sprite  = step.speakerPortrait;
                speakerPortraitImage.enabled = step.speakerPortrait != null;
            }

            dialoguePanel.SetActive(true);
            yield return StartCoroutine(FadeGraphic(dialoguePanel, 0f, 1f, 0.3f));

            // Печатаем текст
            if (step.typeSpeed > 0f)
                yield return StartCoroutine(TypeText(dialogueText, step.dialogueText, step.typeSpeed));
            else if (dialogueText != null)
                dialogueText.text = step.dialogueText;
        }

        private IEnumerator TypeText(TextMeshProUGUI tmp, string text, float speed)
        {
            if (tmp == null) yield break;

            tmp.text = "";
            float delay = 1f / speed;

            foreach (char c in text)
            {
                if (skipPressed) { tmp.text = text; yield break; }
                tmp.text += c;
                yield return new WaitForSecondsRealtime(delay);
            }
        }

        // ─── ФИНАЛ И ПРОПУСК ─────────────────────────────────────────────

        private IEnumerator Finish()
        {
            yield return StartCoroutine(FadeGraphic(skipTextGO, 1f, 0f, skipFadeDuration));

            if (cutsceneMusicSource != null && cutsceneMusicSource.isPlaying)
                yield return StartCoroutine(FadeMusicVolume(cutsceneMusicSource.volume, 0f, 0.5f));

            isPlaying = false;

            // Запускаем следующую катсцену если есть
            if (nextCutscene != null)
                nextCutscene.Play();
            else
                onSequenceComplete?.Invoke();
        }

        private IEnumerator FastForward()
        {
            foreach (CutsceneStep step in steps)
                if (step.target != null) SetGraphicAlpha(step.target, 0f);

            SetGraphicAlpha(skipTextGO, 0f);

            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (cutsceneMusicSource != null) cutsceneMusicSource.Stop();

            Time.timeScale = 1f;
            SetFadeAlpha(1f);

            yield return new WaitForSecondsRealtime(0.3f);
            yield return StartCoroutine(FadeOverlay(1f, 0f, 0.8f));

            isPlaying = false;

            if (nextCutscene != null)
                nextCutscene.Play();
            else
                onSequenceComplete?.Invoke();
        }

        // ─── КНОПКА ПРОПУСКА ─────────────────────────────────────────────

        private IEnumerator ShowSkipTextDelayed()
        {
            yield return WaitUnscaled(skipShowDelay);
            if (!skipPressed && skipTextGO != null)
            {
                skipTextGO.SetActive(true);
                yield return StartCoroutine(FadeGraphic(skipTextGO, 0f, 1f, skipFadeDuration));
            }
        }

        // ─── ИГРОК ───────────────────────────────────────────────────────

        private void FindPlayerController()
        {
            GameObject player = GameObject.FindWithTag(playerTag);
            if (player == null) return;

            foreach (MonoBehaviour mb in player.GetComponents<MonoBehaviour>())
            {
                if (mb.GetType().Name == playerControllerName)
                {
                    playerController = mb;
                    break;
                }
            }
        }

        private void SetPlayerControl(bool enabled)
        {
            if (playerController != null)
                playerController.enabled = enabled;
        }

        // ─── АУДИО ───────────────────────────────────────────────────────

        private void PlayCutsceneMusic(AudioClip clip, float volume, bool loop)
        {
            if (cutsceneMusicSource == null || clip == null) return;
            cutsceneMusicSource.clip   = clip;
            cutsceneMusicSource.volume = volume;
            cutsceneMusicSource.loop   = loop;
            cutsceneMusicSource.Play();
        }

        private IEnumerator FadeMusicVolume(float from, float to, float duration)
        {
            if (cutsceneMusicSource == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                cutsceneMusicSource.volume = Mathf.Lerp(from, to, EaseInOut(Mathf.Clamp01(t)));
                yield return null;
            }
            cutsceneMusicSource.volume = to;
        }

        private IEnumerator MuteGameAudioRoutine()
        {
            if (audioMixer == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / gameAudioFadeDuration;
                audioMixer.SetFloat(gameMixerVolumeParam, Mathf.Lerp(0f, -80f, t));
                yield return null;
            }
            audioMixer.SetFloat(gameMixerVolumeParam, -80f);
        }

        private IEnumerator UnmuteGameAudioRoutine()
        {
            if (audioMixer == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / gameAudioFadeDuration;
                audioMixer.SetFloat(gameMixerVolumeParam, Mathf.Lerp(-80f, 0f, t));
                yield return null;
            }
            audioMixer.SetFloat(gameMixerVolumeParam, 0f);
        }

        // ─── КАМЕРА ──────────────────────────────────────────────────────

        private IEnumerator ShakeCameraRoutine(float duration, float strength)
        {
            if (gameCamera == null) yield break;
            Vector3 originalPos = gameCamera.transform.localPosition;
            float   elapsed     = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float fade = 1f - (elapsed / duration);
                gameCamera.transform.localPosition = originalPos + (Vector3)Random.insideUnitCircle * strength * fade;
                yield return null;
            }

            gameCamera.transform.localPosition = originalPos;
        }

        private IEnumerator ZoomCameraRoutine(float targetSize, float duration)
        {
            if (gameCamera == null) yield break;
            float startSize = gameCamera.orthographicSize;
            float t         = 0f;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                gameCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, EaseInOut(Mathf.Clamp01(t)));
                yield return null;
            }

            gameCamera.orthographicSize = targetSize;
        }

        // ─── АНИМАЦИИ ────────────────────────────────────────────────────

        private IEnumerator FadeOverlay(float from, float to, float duration)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                SetFadeAlpha(Mathf.Lerp(from, to, EaseInOut(Mathf.Clamp01(t))));
                yield return null;
            }
            SetFadeAlpha(to);
        }

        private IEnumerator FadeGraphic(GameObject go, float from, float to, float duration)
        {
            if (go == null) yield break;
            float t = 0f;
            while (t < 1f && !skipPressed)
            {
                t += Time.unscaledDeltaTime / duration;
                SetGraphicAlpha(go, Mathf.Lerp(from, to, EaseInOut(Mathf.Clamp01(t))));
                yield return null;
            }
            SetGraphicAlpha(go, to);
        }

        private IEnumerator ZoomAndFade(GameObject go, float zoomFrom, float zoomTo, float duration)
        {
            if (go == null) yield break;
            go.transform.localScale = Vector3.one * zoomFrom;
            SetGraphicAlpha(go, 0f);
            float t = 0f;
            while (t < 1f && !skipPressed)
            {
                t += Time.unscaledDeltaTime / duration;
                float eased = EaseInOut(Mathf.Clamp01(t));
                go.transform.localScale = Vector3.one * Mathf.Lerp(zoomFrom, zoomTo, eased);
                SetGraphicAlpha(go, eased);
                yield return null;
            }
            go.transform.localScale = Vector3.one * zoomTo;
            SetGraphicAlpha(go, 1f);
        }

        private IEnumerator ShakeObject(GameObject go, float duration, float strength)
        {
            if (go == null) yield break;
            Vector3 originalPos = go.transform.localPosition;
            float   elapsed     = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float fade = 1f - (elapsed / duration);
                go.transform.localPosition = originalPos + (Vector3)Random.insideUnitCircle * strength * fade;
                yield return null;
            }
            go.transform.localPosition = originalPos;
        }

        // ─── УТИЛИТЫ ─────────────────────────────────────────────────────

        private IEnumerator WaitUnscaled(float seconds)
        {
            float t = 0f;
            while (t < seconds && !skipPressed)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void SetFadeAlpha(float alpha)
        {
            if (fadeOverlayImage == null) return;
            Color c = fadeOverlayImage.color;
            c.a = alpha;
            fadeOverlayImage.color = c;
        }

        private void SetGraphicAlpha(GameObject go, float alpha)
        {
            if (go == null) return;
            CanvasGroup     cg  = go.GetComponent<CanvasGroup>();
            Image           img = go.GetComponent<Image>();
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (cg  != null) { cg.alpha = alpha; return; }
            if (img != null) { Color c = img.color; c.a = alpha; img.color = c; }
            if (tmp != null) { Color c = tmp.color; c.a = alpha; tmp.color = c; }
        }

        private float EaseInOut(float t) => t * t * (3f - 2f * t);
    }
}