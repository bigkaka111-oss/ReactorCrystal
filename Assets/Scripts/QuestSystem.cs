using UnityEngine;

public class QuestSystem : MonoBehaviour
{
    [SerializeField] private CrystalDrive crystalDrive;
    [SerializeField] private Quest[] allQuests = new Quest[5];

    private int currentQuestIndex = 0;
    private float questTimer = 0f;
    private bool questActive = false;

    public delegate void QuestEventHandler(Quest quest);
    public event QuestEventHandler OnQuestStarted;
    public event QuestEventHandler OnQuestCompleted;
    public event QuestEventHandler OnQuestFailed;

    private void Start()
    {
        if (crystalDrive == null)
            Debug.LogError("[QuestSystem] CrystalDrive не назначен!");
    }

    private void Update()
    {
        if (!questActive) return;

        Quest currentQuest = allQuests[currentQuestIndex];
        questTimer += Time.deltaTime;

        if (CheckQuestCondition(currentQuest))
        {
            CompleteQuest();
        }
    }

    public void StartQuest(int questIndex)
    {
        if (questIndex < 0 || questIndex >= allQuests.Length)
        {
            Debug.LogError("[QuestSystem] Неверный индекс квеста!");
            return;
        }

        currentQuestIndex = questIndex;
        Quest quest = allQuests[questIndex];
        questActive = true;
        questTimer = 0f;

        if (quest.modifyPhysics)
        {
            Debug.Log($"[QuestSystem] ФИЗИКА ИЗМЕНЕНА для квеста: {quest.questName}");
        }

        OnQuestStarted?.Invoke(quest);
        Debug.Log($"[QuestSystem] Квест начат: {quest.questName}");
    }

    private bool CheckQuestCondition(Quest quest)
    {
        switch (quest.conditionType)
        {
            case QuestConditionType.CurrentStable:
                if (crystalDrive.IsCurrentStable())
                    return questTimer >= quest.conditionValue;
                return false;

            case QuestConditionType.ReachInstability:
                return crystalDrive.instability >= quest.conditionValue;

            case QuestConditionType.CorrectPressure:
                return crystalDrive.IsPistonInIdealZone() && questTimer >= 2f;

            case QuestConditionType.CombinedStability:
                return crystalDrive.IsPistonInIdealZone()
                    && crystalDrive.IsCurrentStable()
                    && questTimer >= quest.conditionValue;

            case QuestConditionType.MaintainBalance:
                return crystalDrive.IsPistonInIdealZone()
                    && crystalDrive.IsCurrentStable()
                    && crystalDrive.instability < 30f
                    && questTimer >= quest.conditionValue;

            default:
                return false;
        }
    }

    private void CompleteQuest()
    {
        questActive = false;
        OnQuestCompleted?.Invoke(allQuests[currentQuestIndex]);
        Debug.Log($"[QuestSystem] Квест выполнен!");
    }

    public void ResetQuest()
    {
        questActive = false;
        questTimer = 0f;
    }

    public Quest GetCurrentQuest() => questActive ? allQuests[currentQuestIndex] : null;
    public int GetCurrentQuestIndex() => currentQuestIndex;
    public float GetQuestProgress() => questActive ? questTimer : 0f;
}