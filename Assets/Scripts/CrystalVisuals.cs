using UnityEngine;

public class CrystalVisuals : MonoBehaviour
{
    [Header("Ссылки")]
    public CrystalDrive drive;
    public Light bunkerLight;

    [Header("Настройка Цвета")]
    [Tooltip("Позволяет настроить изменение цвета на всём промежутке 0-100%")]
    public Gradient colorOverInstability;

    [Header("Пороги Состояний (%)")]
    [Range(0, 100)] public float flickerThreshold = 50f;   // Когда начинается мигание
    [Range(0, 100)] public float criticalThreshold = 80f;  // Когда начинается резкий рост яркости

    [Header("Интенсивность")]
    public float baseIntensity = 3f;
    public float maxIntensity = 100f;
    public float smoothingSpeed = 1.5f; // Скорость реакции света на изменения

    [Header("Настройки Мигания (Зона 50-80%)")]
    public float minFlickerFreq = 5f;
    public float maxFlickerFreq = 20f;
    public float minFlickerAmp = 0.3f;
    public float maxFlickerAmp = 1.5f;

    [Header("Настройки Критической Зоны (80-100%)")]
    public float criticalFlickerFreq = 25f;
    public float criticalFlickerAmp = 2f;

    private float currentIntensity;

    void Start()
    {
        if (bunkerLight != null)
            currentIntensity = bunkerLight.intensity;

        // Если градиент не настроен, создадим простой сине-желтый по умолчанию
        if (colorOverInstability == null)
            CreateDefaultGradient();
    }

    void Update()
    {
        if (drive == null || bunkerLight == null) return;

        float inst = drive.instability; // 0–100
        float instNormalized = inst / 100f;

        // 1. Цвет берем напрямую из градиента
        bunkerLight.color = colorOverInstability.Evaluate(instNormalized);

        // 2. Логика интенсивности
        float targetIntensity = baseIntensity;

        if (inst < flickerThreshold)
        {
            // Стабильное состояние
            targetIntensity = baseIntensity;
        }
        else if (inst < criticalThreshold)
        {
            // Зона нарастающего мигания
            // Вычисляем t (0..1) внутри этого диапазона
            float t = (inst - flickerThreshold) / (criticalThreshold - flickerThreshold);

            float flickerFreq = Mathf.Lerp(minFlickerFreq, maxFlickerFreq, t);
            float flickerAmp = Mathf.Lerp(minFlickerAmp, maxFlickerAmp, t);
            float flicker = Mathf.Abs(Mathf.Sin(Time.time * flickerFreq)) * flickerAmp;

            targetIntensity = baseIntensity + flicker;
        }
        else
        {
            // Критическая зона
            // Вычисляем t (0..1) от criticalThreshold до 100
            float t = (inst - criticalThreshold) / (100f - criticalThreshold);

            float floodTarget = Mathf.Lerp(baseIntensity, maxIntensity, t);
            float flicker = Mathf.Abs(Mathf.Sin(Time.time * criticalFlickerFreq)) * criticalFlickerAmp;

            targetIntensity = floodTarget + flicker;
        }

        // Плавное применение изменений
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothingSpeed);
        bunkerLight.intensity = currentIntensity;
    }

    private void CreateDefaultGradient()
    {
        colorOverInstability = new Gradient();
        var colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(Color.blue, 0.0f);
        colorKeys[1] = new GradientColorKey(Color.yellow, 1.0f);
        var alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
        colorOverInstability.SetKeys(colorKeys, alphaKeys);
    }
}