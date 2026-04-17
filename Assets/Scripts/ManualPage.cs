using UnityEngine;

public class ManualPage : MonoBehaviour
{
    [Header("Настройки камеры")]
    public Camera mainCamera;
    public Collider pageCollider;

    [Header("Позиция на столе (когда лежит)")]
    public Vector3 restPosition;
    public Vector3 restRotation;

    [Header("Позиция перед глазами (когда нажали)")]
    public Vector3 viewPosition;
    public Vector3 viewRotation;

    [Header("Скорость движения")]
    public float speed = 10f;

    private bool isHeld = false;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Сразу кладем листок в начальную позицию
        transform.position = restPosition;
        transform.eulerAngles = restRotation;
    }

    void Update()
    {
        // 1. Проверка клика
        if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseOver()) isHeld = true;
        }

        // 2. Проверка отпускания кнопки
        if (Input.GetMouseButtonUp(0))
        {
            isHeld = false;
        }

        // 3. Движение
        if (isHeld)
        {
            // Летим к позиции перед глазами
            MoveTowards(viewPosition, viewRotation);
        }
        else
        {
            // Возвращаемся на стол
            MoveTowards(restPosition, restRotation);
        }
    }

    // Метод для плавного перемещения и вращения
    void MoveTowards(Vector3 targetPos, Vector3 targetRot)
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);

        // Плавный поворот через углы Эйлера
        Quaternion targetQuaternion = Quaternion.Euler(targetRot);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetQuaternion, Time.deltaTime * speed);
    }

    // Проверка: наведена ли мышь на листок
    bool IsMouseOver()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider == pageCollider;
        }
        return false;
    }
}