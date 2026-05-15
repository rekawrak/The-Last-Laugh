using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Скорость перемещения
    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Start()
    {
        // Получаем компонент физики
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Считываем нажатия клавиш (WASD / стрелки)
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Нормализуем вектор, чтобы по диагонали не бегал быстрее
        moveInput.Normalize();
    }

    void FixedUpdate()
    {
        // Двигаем тело через физику
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
}