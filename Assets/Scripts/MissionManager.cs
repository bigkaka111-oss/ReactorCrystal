using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public CrystalDrive drive;

    [Header("Настройки задачи (используются только если drive == null)")]
    public float targetAmpsMin = 12f;
    public float targetAmpsMax = 14f;
    public float requiredHoldTime = 20f; // Сколько секунд надо удержать ток

    private float currentHoldTime = 0f;

    void Start()
    {
        if (drive == null)
        {
            Debug.LogWarning($"[MissionManager] Поле drive не назначено на '{name}'. Буду использовать локальные пороги.");
        }
    }

    void Update()
    {
        bool inStableZone;

        // Если есть drive — используем его IsCurrentStable() (централизованная логика)
        if (drive != null)
        {
            inStableZone = drive.IsCurrentStable();
        }
        else
        {
            // fallback: сравниваем локально (как было раньше)
            inStableZone = (GetCurrentAmperes() >= targetAmpsMin && GetCurrentAmperes() <= targetAmpsMax);
        }

        if (inStableZone)
        {
            currentHoldTime += Time.deltaTime;

            if (currentHoldTime >= requiredHoldTime)
            {
                Debug.Log("ЗАДАЧА ВЫПОЛНЕНА! Кристалл дал нужный заряд.");
                // TODO: переход к следующему этапу/оповещение/событие
                // чтобы не спамить лог, можно обрабатывать это единожды
                enabled = false;
            }
        }
        else
        {
            // Если игрок упустил Амперы, прогресс быстро сгорает
            currentHoldTime = Mathf.Max(0f, currentHoldTime - Time.deltaTime * 2f);
        }
    }

    // Fallback-метод, если drive отсутствует (обычно не будет вызван при корректной сцене)
    private float GetCurrentAmperes()
    {
        if (drive != null) return drive.currentAmperes;
        return 0f;
    }
}