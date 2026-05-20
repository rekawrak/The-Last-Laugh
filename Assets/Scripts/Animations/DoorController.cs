using UnityEngine;

public class DoorController : MonoBehaviour
{
    private Animator anim;
    private BoxCollider2D solidCollider;
    private Transform playerTransform;
    private SpriteRenderer playerRenderer;

    [Header("Настройки слоев")]
    [Tooltip("Order in Layer, когда игрок в коридоре (перед дверью)")]
    [SerializeField] private int corridorOrder = 10;
    
    [Tooltip("Order in Layer, когда игрок зашел внутрь комнаты (за косяк двери)")]
    [SerializeField] private int roomOrder = 3;

    private bool isPlayerInsideTrigger = false;

    void Start()
    {
        anim = GetComponent<Animator>();

        // Надежный поиск твердого коллайдера (стены)
        BoxCollider2D[] colliders = GetComponents<BoxCollider2D>();
        foreach (var c in colliders)
        {
            if (!c.isTrigger)
            {
                solidCollider = c;
                break;
            }
        }
    }

    void Update()
    {
        // Если игрок находится в зоне двери, динамически управляем его слоем
        if (isPlayerInsideTrigger && playerTransform != null && playerRenderer != null)
        {
            // Сравниваем позицию ног игрока с позицией двери по оси Y
            // Если игрок выше (ушел вглубь комнаты) -> прячем за косяк. Если ниже -> выводим вперед.
            if (playerTransform.position.y > transform.position.y)
            {
                playerRenderer.sortingOrder = roomOrder;
            }
            else
            {
                playerRenderer.sortingOrder = corridorOrder;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInsideTrigger = true;
            playerTransform = other.transform;
            playerRenderer = other.GetComponent<SpriteRenderer>();
            
            OpenDoor();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInsideTrigger = false;
            
            // Когда игрок полностью покинул зону, гарантированно возвращаем его в коридор
            if (playerRenderer != null)
            {
                playerRenderer.sortingOrder = corridorOrder;
            }

            playerTransform = null;
            playerRenderer = null;
            
            CloseDoor();
        }
    }

    void OpenDoor()
    {
        if (anim != null)
        {
            anim.SetBool("isOpen", true);
        }
        
        if (solidCollider != null)
        {
            solidCollider.enabled = false; 
        }
    }

    void CloseDoor()
    {
        if (anim != null)
        {
            anim.SetBool("isOpen", false);
        }
        
        if (solidCollider != null)
        {
            solidCollider.enabled = true; 
        }
    }
}