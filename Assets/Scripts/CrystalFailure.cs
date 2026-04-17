using UnityEngine;
using UnityEngine.SceneManagement; // Для перезагрузки
using System.Collections;

public class CrystalFailure : MonoBehaviour
{
    public CrystalDrive drive;

    [Header("Параметры проигрыша")]
    // Сделано <=100 чтобы соответствовать Clamp в Drive (0..100).
    public float failureThreshold = 100f;

    [Header("Эффекты")]
    public GameObject explosionEffect; // Сюда можно кинуть Particle System

    private bool isDead = false;

    void Start()
    {
        if (drive == null)
        {
            Debug.LogWarning($"[CrystalFailure] Поле drive не назначено на '{name}'. Отключаю компонент.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (isDead || drive == null) return;

        if (drive.instability >= failureThreshold)
        {
            TriggerExplosion();
        }
    }

    void TriggerExplosion()
    {
        if (isDead) return;
        isDead = true;

        Debug.LogError("КРИТИЧЕСКАЯ НЕСТАБИЛЬНОСТЬ! ВЗРЫВ!");

        if (explosionEffect != null && drive != null)
        {
            Instantiate(explosionEffect, drive.transform.position, Quaternion.identity);
        }

        // Отключаем компонент, чтобы дальнейшие Update/Invoke не сработали
        enabled = false;

        // Используем Coroutine вместо Invoke — более контролируемо
        StartCoroutine(RestartLevelAfterDelay(5f));
    }

    IEnumerator RestartLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}