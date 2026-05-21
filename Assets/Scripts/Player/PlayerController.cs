using UnityEngine;

namespace Project.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // --- CONFIGURATION ------------------------------------------------
        [Header("[ ДВИЖЕНИЕ ]")]
        [SerializeField] private float moveSpeed         = 4f;
        [SerializeField] private float movementSmoothing = 0.1f;

        [Header("[ ВВОД ]")]
        [SerializeField] private KeyCode upKey    = KeyCode.W;
        [SerializeField] private KeyCode downKey  = KeyCode.S;
        [SerializeField] private KeyCode leftKey  = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.D;
        // -----------------------------------------------------------------

        private Rigidbody2D rb;
        private Vector2     moveInput;
        private Vector2     currentVelocity;
        private Vector2     smoothVelocity;
        private Vector2     facingDirection;

        private Animator       animator;
        private SpriteRenderer spriteRenderer;

        private static readonly int DirXHash     = Animator.StringToHash("DirX");
        private static readonly int DirYHash     = Animator.StringToHash("DirY");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        private void Awake()
        {
            rb             = GetComponent<Rigidbody2D>();
            animator       = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            rb.gravityScale   = 0f;
            rb.freezeRotation = true;

            facingDirection = Vector2.down;
        }

        private void Update()
        {
            ReadInput();
        }

        private void FixedUpdate()
        {
            Move();
        }

        // --- ВВОД --------------------------------------------------------

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

        // --- НАПРАВЛЕНИЕ ВЗГЛЯДА -----------------------------------------

        private Vector2 GetDirection8Way(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01f) return facingDirection;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if (angle >= 22.5f && angle < 67.5f)
                return new Vector2(1f, 1f);   // СВ
            else if (angle >= 67.5f && angle < 112.5f)
                return new Vector2(0f, 1f);   // Вверх
            else if (angle >= 112.5f && angle < 157.5f)
                return new Vector2(-1f, 1f);  // СЗ
            else if (angle >= 157.5f && angle < 202.5f)
                return new Vector2(-1f, 0f);  // Влево
            else if (angle >= 202.5f && angle < 247.5f)
                return new Vector2(-1f, -1f); // ЮЗ
            else if (angle >= 247.5f && angle < 292.5f)
                return new Vector2(0f, -1f);  // Вниз
            else if (angle >= 292.5f && angle < 337.5f)
                return new Vector2(1f, -1f);  // ЮВ
            else
                return new Vector2(1f, 0f);   // Вправо
        }

        // --- ДВИЖЕНИЕ ----------------------------------------------------

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
            UpdateAnimation(currentVelocity);
        }

        // --- АНИМАЦИЯ ----------------------------------------------------

        private void UpdateAnimation(Vector2 velocity)
        {
            if (animator == null) return;

            // Фактическое движение определяем по физике
            bool isMoving = velocity.sqrMagnitude > 0.05f;
            
            // Направление определяем строго по нажатым кнопкам
            bool hasInput = moveInput.sqrMagnitude > 0.01f;

            if (hasInput)
            {
                facingDirection = GetDirection8Way(moveInput);
            }

            ProcessFlipAndAnimation(facingDirection, isMoving);
        }

        private void ProcessFlipAndAnimation(Vector2 direction, bool isMoving)
        {
            float animX = direction.x;
            float animY = direction.y;

            if (direction.x > 0f)
            {
                if (spriteRenderer != null) spriteRenderer.flipX = true;
                animX = -direction.x; 
            }
            else if (direction.x < 0f)
            {
                if (spriteRenderer != null) spriteRenderer.flipX = false;
            }
            else
            {
                // При движении строго вверх/вниз
                if (spriteRenderer != null) spriteRenderer.flipX = false;
            }

            animator.SetFloat(DirXHash,    animX);
            
            // ЕСЛИ АНИМАЦИИ ВЕРХА И НИЗА ПЕРЕПУТАНЫ В UNITY BLEND TREE:
            // Замените animY в строке ниже на -animY
            animator.SetFloat(DirYHash,    animY); 
            
            animator.SetBool(IsMovingHash, isMoving);
        }

        // --- ПУБЛИЧНЫЕ МЕТОДЫ ---------------------------------------------

        public void SetMovementEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                rb.linearVelocity = Vector2.zero;
                if (animator != null) animator.SetBool(IsMovingHash, false);
            }
        }

        public void SetFacingDirection(Vector2 direction)
        {
            facingDirection = direction.normalized;
            ProcessFlipAndAnimation(GetDirection8Way(facingDirection), false);
        }
    }
}