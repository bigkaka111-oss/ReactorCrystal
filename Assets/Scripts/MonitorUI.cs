using UnityEngine;
using TMPro;

/// <summary>
/// Скрипт монитора.
/// Считывает данные из CrystalDrive и выводит их на экран
/// через TextMeshPro компоненты.
///
/// НАСТРОЙКА В INSPECTOR:
///   1. Назначь ссылку на объект CrystalDrive.
///   2. Назначь TextMeshPro поля для каждого параметра.
///   3. Монитор обновляется каждые updateInterval секунд (не каждый кадр —
///      это экономит производительность и создаёт эффект "медленного дисплея").
/// </summary>
public class MonitorUI : MonoBehaviour
{
    // ============================================================
    // НАСТРОЙКИ
    // ============================================================

    [Header("Источник данных")]
    [SerializeField] private CrystalDrive crystalDrive;

    [Header("Поля монитора (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI pistonText;     // Давление поршней
    [SerializeField] private TextMeshProUGUI rpmText;        // Обороты МПС
    [SerializeField] private TextMeshProUGUI mpsDepthText;   // Глубина МПС
    [SerializeField] private TextMeshProUGUI mzsDepthText;   // Глубина МЗС
    [SerializeField] private TextMeshProUGUI amperesText;    // Сила тока (А)
    [SerializeField] private TextMeshProUGUI statusText;     // Общий статус системы
    [SerializeField] private TextMeshProUGUI instabilityText; // Уровень нестабильности

    [Header("Цвета индикаторов")]
    [SerializeField] private Color safeColor = new Color(0.2f, 1f, 0.4f);    // Зелёный — норма
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.1f);   // Жёлтый — предупреждение
    [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.2f);    // Красный — опасность
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f); // Серый — неактивно

    [Header("Частота обновления")]
    [SerializeField] private float updateInterval = 0.1f;  // Обновление каждые 100 мс

    // ============================================================
    // ВНУТРЕННИЕ ПЕРЕМЕННЫЕ
    // ============================================================

    private float updateTimer = 0f;

    // ============================================================
    // UNITY МЕТОДЫ
    // ============================================================

    private void Update()
    {
        // Считаем время и обновляем монитор с заданной частотой
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            RefreshDisplay();
        }
    }

    // ============================================================
    // ПРИВАТНЫЕ МЕТОДЫ
    // ============================================================

    /// <summary>
    /// Считывает все данные из CrystalDrive и обновляет текстовые поля.
    /// Вызывается с частотой updateInterval.
    /// </summary>
    private void RefreshDisplay()
    {
        if (crystalDrive == null)
        {
            Debug.LogWarning("[MonitorUI] Не назначен CrystalDrive!");
            return;
        }

        UpdatePistonField();
        UpdateRPMField();
        UpdateMPSDepthField();
        UpdateMZSDepthField();
        UpdateAmperesField();
        UpdateInstabilityField();
        UpdateStatusField();
    }

    /// <summary>Давление поршней с цветом: зелёный в диапазоне 70–85%.</summary>
    private void UpdatePistonField()
    {
        if (pistonText == null) return;

        float p = crystalDrive.pistonPressure;
        pistonText.text = $"ПОРШНИ:  {p:F0}%";

        if (p >= 70f && p <= 85f)
            pistonText.color = safeColor;
        else if (p > 85f || (p > 60f && p < 70f))
            pistonText.color = warningColor;
        else
            pistonText.color = dangerColor;
    }

    /// <summary>Обороты МПС.</summary>
    private void UpdateRPMField()
    {
        if (rpmText == null) return;

        rpmText.text = $"МПС RPM:  {crystalDrive.mpsRPM:F0}";
        rpmText.color = crystalDrive.mpsRPM > 0f ? safeColor : inactiveColor;
    }

    /// <summary>Глубина погружения МПС.</summary>
    private void UpdateMPSDepthField()
    {
        if (mpsDepthText == null) return;

        mpsDepthText.text = $"МПС ГЛУБ: {crystalDrive.mpsDepth:F0}%";
        mpsDepthText.color = crystalDrive.mpsDepth > 0f ? safeColor : inactiveColor;
    }

    /// <summary>Глубина погружения МЗС.</summary>
    private void UpdateMZSDepthField()
    {
        if (mzsDepthText == null) return;

        mzsDepthText.text = $"МЗС ГЛУБ: {crystalDrive.mzsDepth:F0}%";
        mzsDepthText.color = crystalDrive.mzsDepth > 0f ? warningColor : inactiveColor;
    }

    /// <summary>
    /// Сила тока с цветовой индикацией:
    ///   Зелёный = 12–14А (цель)
    ///   Жёлтый  = 10–12А или 14–16А (близко к границе)
    ///   Красный  = ниже 10А или выше 16А (критично)
    /// </summary>
    private void UpdateAmperesField()
    {
        if (amperesText == null) return;

        float a = crystalDrive.currentAmperes;
        amperesText.text = $"ТОК:     {a:F2} А";

        if (crystalDrive.IsCurrentStable())
            amperesText.color = safeColor;
        else if (a < 10f || a > 65f)
            amperesText.color = dangerColor;
        else
            amperesText.color = warningColor;
    }

    /// <summary>Уровень нестабильности в %.</summary>
    private void UpdateInstabilityField()
    {
        if (instabilityText == null) return;

        float inst = crystalDrive.instability;
        instabilityText.text = $"НЕСТАБ:  {inst:F0}%";

        if (inst < 30f)
            instabilityText.color = safeColor;
        else if (inst < 70f)
            instabilityText.color = warningColor;
        else
            instabilityText.color = dangerColor;
    }

    /// <summary>
    /// Общий статус системы — одна строка, описывающая текущее состояние.
    /// </summary>
    private void UpdateStatusField()
    {
        if (statusText == null) return;

        if (!crystalDrive.crystalActive)
        {
            // Кристалл не поднят из воды
            statusText.text = "КРИСТАЛЛ: НЕАКТИВЕН";
            statusText.color = inactiveColor;
        }
        else if (crystalDrive.instability > 70f)
        {
            // Критическая нестабильность
            statusText.text = "!! НЕСТАБИЛЬНОСТЬ КРИТИЧНА";
            statusText.color = dangerColor;
        }
        else if (!crystalDrive.IsCurrentStable())
        {
            // Ток вне нормы
            statusText.text = "~ ТОК ВНЕ НОРМЫ";
            statusText.color = warningColor;
        }
        else
        {
            // Всё в порядке
            statusText.text = ">> СИСТЕМА СТАБИЛЬНА";
            statusText.color = safeColor;
        }
    }
}