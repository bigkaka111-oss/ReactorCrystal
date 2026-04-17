using UnityEngine;
using UnityEngine.EventSystems; // Обязательно для отслеживания мыши

[RequireComponent(typeof(CanvasGroup))]
public class UIHoverFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Настройки прозрачности")]
    [SerializeField] private float idleAlpha = 0f;      // Прозрачность в покое
    [SerializeField] private float hoverAlpha = 1f;     // Прозрачность при наведении
    [SerializeField] private float fadeSpeed = 5f;      // Скорость появления/исчезновения

    private CanvasGroup canvasGroup;
    private float targetAlpha;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        targetAlpha = idleAlpha;
        canvasGroup.alpha = idleAlpha;
    }

    void Update()
    {
        // Плавно меняем текущую прозрачность к целевой
        if (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }

    // Вызывается Unity, когда мышь заходит в зону кнопки
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetAlpha = hoverAlpha;
    }

    // Вызывается Unity, когда мышь уходит с кнопки
    public void OnPointerExit(PointerEventData eventData)
    {
        targetAlpha = idleAlpha;
    }
}