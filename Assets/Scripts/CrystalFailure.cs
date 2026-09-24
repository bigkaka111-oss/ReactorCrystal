using UnityEngine;

public class CrystalFailure : MonoBehaviour
{
    public CrystalDrive drive;
    public GameObject explosionEffect;
    public Transform effectOrigin;
    private GameObject activeEffect;
    private QuestSystem quest;
    private void OnEnable()
    {
        quest = FindFirstObjectByType<QuestSystem>();
        if (quest != null) { quest.OnQuestFailed += ShowFailure; quest.OnStateChanged += OnState; }
    }
    private void OnDisable()
    {
        if (quest != null) { quest.OnQuestFailed -= ShowFailure; quest.OnStateChanged -= OnState; }
        if (activeEffect != null) Destroy(activeEffect);
    }
    private void OnState(QuestSystem.SessionState state)
    {
        if (state == QuestSystem.SessionState.Ready && activeEffect != null) Destroy(activeEffect);
    }
    private void ShowFailure()
    {
        if (explosionEffect != null && drive != null)
        {
            activeEffect = Instantiate(explosionEffect, effectOrigin != null ? effectOrigin.position : drive.transform.position, Quaternion.identity);
            Destroy(activeEffect, 6f);
        }
        // The result screen owns restart; a normal loss is not an error.
    }
}

