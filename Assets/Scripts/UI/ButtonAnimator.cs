using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Project.UI
{
    /// <summary>
    /// Анимация кнопки
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class NoirButtonAnimator : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        // ─── CONFIGURATION ────────────────────────────────────────────────
        [Header("[ НАВЕДЕНИЕ ]")]
        [Tooltip("Масштаб при наведении (1.08 = увеличение на 8%)")]
        [SerializeField] private float hoverScale = 1.05f;
        [Tooltip("Скорость анимации")]
        [SerializeField] private float animationSpeed = 8f;

        [Header("[ НАЖАТИЕ ]")]
        [Tooltip("Цвет затемнения при нажатии")]
        [SerializeField] private Color pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [Tooltip("Масштаб при нажатии")]
        [SerializeField] private float pressedScale = 0.96f;

        [Header("[ ЗВУК ]")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioSource audioSource;
        // ─────────────────────────────────────────────────────────────────

        private Vector3 originalScale;
        private Vector3 targetScale;
        private Image image;
        private Color originalColor;
        private bool isPressed;

        private void Awake()
        {
            image         = GetComponent<Image>();
            originalScale = transform.localScale;
            targetScale   = originalScale;
            originalColor = image.color;
        }

        private void Update()
        {
            // Плавная анимация масштаба
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.unscaledDeltaTime * animationSpeed
            );
        }

        // ─── Наведение ───────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isPressed)
                targetScale = originalScale * hoverScale;

            PlaySound(hoverSound);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isPressed)
                targetScale = originalScale;

            image.color = originalColor;
        }

        // ─── Нажатие ─────────────────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed       = true;
            targetScale     = originalScale * pressedScale;
            image.color     = pressedColor;

            PlaySound(clickSound);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed   = false;
            targetScale = originalScale * hoverScale;
            image.color = originalColor;
        }

        // ─── Звук ────────────────────────────────────────────────────────

        private void PlaySound(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.PlayOneShot(clip);
        }
    }
}