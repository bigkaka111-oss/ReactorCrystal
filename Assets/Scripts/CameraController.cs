using UnityEngine;

/// <summary>
/// Управление камерой в стиле FNAF с функцией возврата в центр при повторном клике.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ============================================================
    // НАСТРОЙКИ
    // ============================================================

    [Header("Углы поворота (в градусах)")]
    [SerializeField] private float horizontalAngle = 50f;
    [SerializeField] private float verticalAngle = 30f;

    [Header("Скорость поворота")]
    [SerializeField] private float rotationSpeed = 6f;

    [Header("Эффект живой головы")]
    [SerializeField] private float headBobIntensity = 0.4f;
    [SerializeField] private float headBobSmoothness = 4f;

    // ============================================================
    // ВНУТРЕННИЕ ПЕРЕМЕННЫЕ
    // ============================================================

    public enum CameraPosition { Center, Left, Right, Up, Down }
    private CameraPosition currentPosition = CameraPosition.Center;

    private Quaternion baseRotation;      // Базовое вращение (центр)
    private Quaternion targetRotation;    // Целевое вращение
    private Vector2 smoothMouseDelta;     // Сглаженная дельта мыши

    // ============================================================
    // UNITY МЕТОДЫ
    // ============================================================

    private void Start()
    {
        baseRotation = transform.rotation;
        targetRotation = baseRotation;
    }

    private void Update()
    {
        ApplyHeadBobEffect();
        ApplyRotation();
    }

    // ============================================================
    // ЛОГИКА ПОВОРОТА (ПРИВАТНАЯ)
    // ============================================================

    private void ApplyHeadBobEffect()
    {
        Vector2 rawDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        smoothMouseDelta = Vector2.Lerp(smoothMouseDelta, rawDelta, Time.deltaTime * headBobSmoothness);
    }

    private void ApplyRotation()
    {
        Quaternion headOffset = Quaternion.Euler(
            -smoothMouseDelta.y * headBobIntensity,
             smoothMouseDelta.x * headBobIntensity,
            0f
        );

        Quaternion finalRotation = targetRotation * headOffset;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            finalRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    // ============================================================
    // ПУБЛИЧНЫЕ МЕТОДЫ (ДЛЯ КНОПОК)
    // ============================================================

    /// <summary>
    /// Главный метод управления. 
    /// Если вызываем позицию, в которой уже находимся — сбрасываем в Center.
    /// </summary>
    public void SetCameraPosition(CameraPosition position)
    {
        // ЛОГИКА ПЕРЕКЛЮЧЕНИЯ (TOGGLE):
        // Если мы уже смотрим туда, куда нажали, и это не центр — возвращаемся в центр.
        if (currentPosition == position && position != CameraPosition.Center)
        {
            currentPosition = CameraPosition.Center;
        }
        else
        {
            currentPosition = position;
        }

        // Вычисляем новый targetRotation на основе итогового currentPosition
        switch (currentPosition)
        {
            case CameraPosition.Center:
                targetRotation = baseRotation;
                break;

            case CameraPosition.Left:
                targetRotation = baseRotation * Quaternion.Euler(0f, -horizontalAngle, 0f);
                break;

            case CameraPosition.Right:
                targetRotation = baseRotation * Quaternion.Euler(0f, horizontalAngle, 0f);
                break;

            case CameraPosition.Up:
                targetRotation = baseRotation * Quaternion.Euler(-verticalAngle, 0f, 0f);
                break;

            case CameraPosition.Down:
                targetRotation = baseRotation * Quaternion.Euler(verticalAngle, 0f, 0f);
                break;
        }
    }

    // Эти методы ты вешаешь на OnClick в Unity:
    public void LookCenter() => SetCameraPosition(CameraPosition.Center);
    public void LookLeft() => SetCameraPosition(CameraPosition.Left);
    public void LookRight() => SetCameraPosition(CameraPosition.Right);
    public void LookUp() => SetCameraPosition(CameraPosition.Up);
    public void LookDown() => SetCameraPosition(CameraPosition.Down);

    public CameraPosition GetCurrentPosition() => currentPosition;
}