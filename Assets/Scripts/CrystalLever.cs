using UnityEngine;

public class CrystalLever : MonoBehaviour
{
    public CrystalDrive drive;

    public enum LeverAction { MpsDepth, MzsDepth, Pistons, RPM }
    public LeverAction action;

    [Header("Максимум системы (100 для глубины/поршней, 1200 для RPM)")]
    public float maxValueForSystem = 100f;

    [Header("Ось движения (локальная)")]
    public Vector3 moveAxis = Vector3.forward;

    [Header("Физические границы рычага")]
    public float minValue = -0.1f;
    public float maxValue = 0.1f;

    [Header("Чувствительность перетаскивания")]
    public float sensitivity = 0.001f;

    // ── ПРАВКА: задержка значения.
    // Рычаг сдвинулся — но значение в CrystalDrive ползёт к цели медленно.
    // smoothSpeed = 2f: доползает примерно за 1-2 секунды.
    // Крути в Inspector: 0.5 = очень медленно, 5.0 = почти мгновенно.
    [Header("Задержка передачи значения")]
    public float smoothSpeed = 2f;

    // ──────────────────────────────────────
    private bool isDragging = false;
    private float dragStartMouse;
    private float dragStartValue;
    private Vector3 startLocalPos;

    // Целевое значение (куда сдвинул рычаг) и сглаженное (что реально идёт в Drive)
    private float targetSystemValue = 0f;
    private float smoothedSystemValue = 0f;

    void Start()
    {
        startLocalPos = transform.localPosition;
        moveAxis = moveAxis.normalized;
    }

    void OnMouseDown()
    {
        isDragging = true;
        dragStartMouse = Input.mousePosition.y;
        dragStartValue = GetCurrentPhysicalValue();
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    void Update()
    {
        if (isDragging)
        {
            float mouseDelta = Input.mousePosition.y - dragStartMouse;
            float newValue = Mathf.Clamp(
                dragStartValue + mouseDelta * sensitivity,
                minValue, maxValue
            );

            // Двигаем 3D объект рычага сразу — он реагирует на руку мгновенно
            transform.localPosition = startLocalPos + moveAxis * newValue;

            // Считаем целевое значение для системы (0–maxValueForSystem)
            float t = Mathf.InverseLerp(minValue, maxValue, newValue);
            targetSystemValue = t * maxValueForSystem;
        }

        // Сглаженное значение медленно ползёт к целевому — это и есть "задержка рычага"
        smoothedSystemValue = Mathf.Lerp(
            smoothedSystemValue,
            targetSystemValue,
            Time.deltaTime * smoothSpeed
        );

        // Отправляем в CrystalDrive уже сглаженное значение
        if (drive != null)
        {
            switch (action)
            {
                case LeverAction.MpsDepth: drive.ChangeMPSDepth(smoothedSystemValue); break;
                case LeverAction.MzsDepth: drive.ChangeMZSDepth(smoothedSystemValue); break;
                case LeverAction.Pistons: drive.ChangePistons(smoothedSystemValue); break;
                case LeverAction.RPM: drive.ChangeRPM(smoothedSystemValue); break;
            }
        }
    }

    float GetCurrentPhysicalValue()
    {
        Vector3 offset = transform.localPosition - startLocalPos;
        return Vector3.Dot(offset, moveAxis);
    }
}