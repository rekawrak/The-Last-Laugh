using UnityEngine;

namespace Project.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // ─── CONFIGURATION ────────────────────────────────────────────────
        [Header("[ ДВИЖЕНИЕ ]")]
        [SerializeField] private float moveSpeed         = 4f;
        [SerializeField] private float movementSmoothing = 0.1f;

        [Header("[ ВВОД ]")]
        [SerializeField] private KeyCode upKey    = KeyCode.W;
        [SerializeField] private KeyCode downKey  = KeyCode.S;
        [SerializeField] private KeyCode leftKey  = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.D;

        [Header("[ ПОВОРОТ К МЫШИ ]")]
        [Tooltip("Персонаж смотрит в сторону мыши")]
        [SerializeField] private bool rotateToMouse = true;
        [SerializeField] private Camera gameCamera;
        // ─────────────────────────────────────────────────────────────────

        private Rigidbody2D rb;
        private Vector2     moveInput;
        private Vector2     currentVelocity;
        private Vector2     smoothVelocity;
        private Vector2     facingDirection;

        private Animator animator;
        private static readonly int DirXHash    = Animator.StringToHash("DirX");
        private static readonly int DirYHash    = Animator.StringToHash("DirY");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        private void Awake()
        {
            rb       = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();

            rb.gravityScale   = 0f;
            rb.freezeRotation = true;

            facingDirection = Vector2.down;

            if (gameCamera == null)
                gameCamera = Camera.main;
        }

        private void Update()
        {
            ReadInput();
            UpdateFacingDirection();
        }

        private void FixedUpdate()
        {
            Move();
        }

        // ─── ВВОД ────────────────────────────────────────────────────────

        private void ReadInput()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(rightKey)) x += 1f;
            if (Input.GetKey(leftKey))  x -= 1f;
            if (Input.GetKey(upKey))    y += 1f;
            if (Input.GetKey(downKey))  y -= 1f;

            moveInput = new Vector2(x, y);

            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();
        }

        // ─── НАПРАВЛЕНИЕ ВЗГЛЯДА ─────────────────────────────────────────

        private void UpdateFacingDirection()
        {
            if (!rotateToMouse || gameCamera == null) return;

            // Переводим позицию мыши в мировые координаты
            Vector3 mouseWorld = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            Vector2 direction = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;

            // Определяем основное направление (4 стороны)
            facingDirection = GetCardinalDirection(direction);
        }

        /// <summary>
        /// Переводит произвольное направление в одно из 4 основных
        /// </summary>
        private Vector2 GetCardinalDirection(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (angle >= -45f && angle < 45f)
                return Vector2.right;
            else if (angle >= 45f && angle < 135f)
                return Vector2.up;
            else if (angle >= -135f && angle < -45f)
                return Vector2.down;
            else
                return Vector2.left;
        }

        // ─── ДВИЖЕНИЕ ────────────────────────────────────────────────────

        private void Move()
        {
            Vector2 targetVelocity = moveInput * moveSpeed;

            currentVelocity = Vector2.SmoothDamp(
                currentVelocity,
                targetVelocity,
                ref smoothVelocity,
                movementSmoothing
            );

            rb.linearVelocity = currentVelocity;

            UpdateAnimation();
        }

        // ─── АНИМАЦИЯ ────────────────────────────────────────────────────

        private void UpdateAnimation()
        {
            if (animator == null) return;

            // Проверяем реальное движение через ввод, чтобы избежать микро-смещений физики
            bool isMoving = moveInput.sqrMagnitude > 0.01f;

            // Направление для анимации:
            // если двигаемся — берём направление движения
            // если стоим — берём направление взгляда (к мыши)
            Vector2 animDir = isMoving
                ? GetCardinalDirection(moveInput)
                : facingDirection;

            // Передаем чистые значения 1, 0, -1 прямо в Blend Tree без задержек.
            // Теперь за смену кадров отвечает исключительно правильно настроенный Animator.
            animator.SetFloat(DirXHash,    animDir.x);
            animator.SetFloat(DirYHash,    animDir.y);
            animator.SetBool(IsMovingHash, isMoving);
        }

        // ─── ПУБЛИЧНЫЕ МЕТОДЫ ─────────────────────────────────────────────

        public void SetMovementEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
                rb.linearVelocity = Vector2.zero;
        }

        public void SetFacingDirection(Vector2 direction)
        {
            facingDirection = direction.normalized;
            if (animator == null) return;
            animator.SetFloat(DirXHash, facingDirection.x);
            animator.SetFloat(DirYHash, facingDirection.y);
        }
    }
}