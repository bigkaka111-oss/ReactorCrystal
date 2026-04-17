using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Компонент физической 3D-кнопки на столе учёного.
/// Реализует интерфейс IButtonInteractable.
///
/// НАСТРОЙКА В INSPECTOR:
///   1. Добавь этот скрипт на 3D объект кнопки.
///   2. Назначь тег "Button" объекту.
///   3. Убедись, что на объекте есть Collider.
///   4. В поле "On Pressed" добавь нужный метод CrystalDrive
///      (например: CrystalDrive → AdjustPistons(5)).
/// </summary>
/// 

public class PhysicalButton : MonoBehaviour

{
    private void OnMouseDown()
    {
        OnButtonPressed();
    }
    // ============================================================
    // НАСТРОЙКИ
    // ============================================================

    [Header("Действие при нажатии")]
    [SerializeField] private UnityEvent onPressed;   // Событие с вызовом нужного метода

    [Header("Анимация нажатия")]
    [SerializeField] private float pressDepth = 0.04f;  // Насколько кнопка "проваливается" вниз (в юнитах)
    [SerializeField] private float pressReturnSpeed = 0.08f;  // Время возврата в секундах

    [Header("Визуальный отклик (подсветка)")]
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.3f);  // Цвет при наведении
    [SerializeField] private Color normalColor = Color.white;                  // Обычный цвет

    // ============================================================
    // ВНУТРЕННИЕ ПЕРЕМЕННЫЕ
    // ============================================================

    private Vector3 originalLocalPosition;          // Исходная локальная позиция (до нажатия)
    private Renderer buttonRenderer;                 // Renderer для смены цвета
    private MaterialPropertyBlock materialPropBlock; // Блок свойств (не создаёт новый Material)
    private bool isAnimating = false;                // Флаг: анимация нажатия в процессе

    // ============================================================
    // UNITY МЕТОДЫ
    // ============================================================

    private void Awake()
    {
        // Запоминаем исходное положение кнопки
        originalLocalPosition = transform.localPosition;

        // Ищем Renderer на этом или дочернем объекте
        buttonRenderer = GetComponentInChildren<Renderer>();

        // MaterialPropertyBlock позволяет менять цвет без создания нового экземпляра материала
        materialPropBlock = new MaterialPropertyBlock();
    }

    // ============================================================
    // РЕАЛИЗАЦИЯ ИНТЕРФЕЙСА IButtonInteractable
    // ============================================================

    /// <summary>Вызывается при нажатии ЛКМ.</summary>
    public void OnButtonPressed()
    {
        // Запускаем анимацию нажатия (если ещё не в процессе)
        if (!isAnimating)
            StartCoroutine(PressAnimation());

        // Вызываем привязанное UnityEvent действие
        onPressed?.Invoke();

        Debug.Log($"[PhysicalButton] Нажата: {gameObject.name}");
    }

    /// <summary>Вызывается, когда курсор наводится на кнопку.</summary>
    public void OnButtonHover()
    {
        SetButtonColor(hoverColor);
    }

    /// <summary>Вызывается, когда курсор уходит с кнопки.</summary>
    public void OnButtonExit()
    {
        SetButtonColor(normalColor);
    }

    // ============================================================
    // ПРИВАТНЫЕ МЕТОДЫ
    // ============================================================

    /// <summary>Меняет цвет кнопки через MaterialPropertyBlock (без создания нового материала).</summary>
    private void SetButtonColor(Color color)
    {
        if (buttonRenderer == null) return;

        buttonRenderer.GetPropertyBlock(materialPropBlock);
        materialPropBlock.SetColor("_Color", color);
        buttonRenderer.SetPropertyBlock(materialPropBlock);
    }

    /// <summary>
    /// Корутина анимации нажатия:
    /// кнопка опускается вниз на pressDepth и плавно возвращается обратно.
    /// </summary>
    private IEnumerator PressAnimation()
    {
        isAnimating = true;

        // Нажать — переместить вниз
        transform.localPosition = originalLocalPosition + Vector3.down * pressDepth;

        // Подождать
        yield return new WaitForSeconds(pressReturnSpeed);

        // Плавно вернуть на место
        float elapsed = 0f;
        Vector3 pressedPosition = transform.localPosition;

        while (elapsed < pressReturnSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressReturnSpeed;
            transform.localPosition = Vector3.Lerp(pressedPosition, originalLocalPosition, t);
            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        isAnimating = false;
    }
}
