using UnityEngine;

/// <summary>
/// ScriptableObject для хранения одного квеста.
/// Создавай эти файлы через Assets > Create > Quest
/// </summary>
[CreateAssetMenu(fileName = "New Quest", menuName = "Quest")]
public class Quest : ScriptableObject
{
    [Header("Информация")]
    public string questName = "Квест 1";
    public int questStep = 1; // На какой шаг это относится (1-5)

    [TextArea(3, 5)]
    public string description = "Описание квеста...";

    [TextArea(2, 4)]
    public string objectives = "Что нужно сделать...";

    [Header("Спрайты для мануала")]
    public Sprite leftPageSprite;   // Левая страница
    public Sprite rightPageSprite;  // Правая страница
    public Sprite questIcon;

    [Header("Условия завершения")]
    public QuestConditionType conditionType = QuestConditionType.CurrentStable;
    public float conditionValue = 20f;

    [Header("Модификация физики при старте")]
    public bool modifyPhysics = false;
    public float modifiedTargetCurrentMin = 12f;
    public float modifiedTargetCurrentMax = 14f;
    public float modifiedBaseRPM = 600f;
    public float modifiedCurrentScale = 20f;
}

public enum QuestConditionType
{
    CurrentStable,
    ReachInstability,
    CorrectPressure,
    CombinedStability,
    MaintainBalance
}