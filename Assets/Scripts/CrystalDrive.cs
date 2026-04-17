using UnityEngine;

public class CrystalDrive : MonoBehaviour
{
    [Header("Поршни")]
    [SerializeField] private float pistonMinActive = 70f;
    [SerializeField] private float pistonMaxActive = 85f;

    public float GetMaxSafePressure() => pistonMaxActive;
    public float GetMinSafePressure() => pistonMinActive;

    [Header("Физика тока")]
    [SerializeField] private float baseRPMNorm = 600f;
    [SerializeField] private float currentScale = 20f;
    [SerializeField] private float mzsReduction = 8f;

    [SerializeField] private float currentLerpSpeed = 0.3f;

    [Header("Целевой диапазон тока")]
    [SerializeField] private float targetCurrentMin = 12f;
    [SerializeField] private float targetCurrentMax = 14f;

    [Header("Нестабильность")]
    [SerializeField] private float instabilityGrowthRate = 0.4f;
    [SerializeField] private float instabilityDecayRate = 0.25f;

    [HideInInspector] public float pistonPressure = 0f;
    [HideInInspector] public float mpsDepth = 0f;
    [HideInInspector] public float mpsRPM = 0f;
    [HideInInspector] public float mzsDepth = 0f;
    [HideInInspector] public float currentAmperes = 0f;
    [HideInInspector] public float instability = 0f;
    public bool crystalActive = false;

    // ── НОВОЕ: система фиксации поршней ──
    [HideInInspector] public bool pistonLocked = false;
    private float lockedPistonValue = 0f;

    private void FixedUpdate()
    {
        CalculateCrystalState();
        CalculateCurrent();
        CalculateInstability();
    }

    private void CalculateCrystalState()
    {
        crystalActive = pistonPressure >= pistonMinActive
                     && pistonPressure <= pistonMaxActive
                     && pistonLocked; // ── Кристалл активен ТОЛЬКО если поршни зафиксированы ──
    }

    private void CalculateCurrent()
    {
        if (!crystalActive)
        {
            currentAmperes = Mathf.MoveTowards(currentAmperes, 0f, Time.fixedDeltaTime * 2f);
            return;
        }

        float normalizedRPM = mpsRPM / baseRPMNorm;
        float normalizedDepth = mpsDepth / 100f;
        float rawCurrent = normalizedRPM * normalizedDepth * currentScale;
        float mzsEffect = (mzsDepth / 100f) * mzsReduction;
        float targetCurrent = Mathf.Max(0f, rawCurrent - mzsEffect);

        currentAmperes = Mathf.Lerp(
            currentAmperes,
            targetCurrent,
            Time.fixedDeltaTime * currentLerpSpeed
        );
    }

    private void CalculateInstability()
    {
        if (!crystalActive || mpsDepth <= 0f)
        {
            instability = Mathf.MoveTowards(
                instability, 0f,
                Time.fixedDeltaTime * instabilityDecayRate * 15f
            );
            return;
        }

        float idealRPM = mpsDepth * 8f;
        float rpmExcess = Mathf.Max(0f, mpsRPM - idealRPM);

        if (rpmExcess > 0f)
            instability += rpmExcess * instabilityGrowthRate * Time.fixedDeltaTime;
        else
            instability -= instabilityDecayRate * Time.fixedDeltaTime * 10f;

        instability = Mathf.Clamp(instability, 0f, 100f);
    }

    // ── публичные методы ──
    public void ChangePistons(float value)
    {
        // ── Если поршни заблокированы, не даём их двигать ──
        if (pistonLocked) return;

        pistonPressure = Mathf.Clamp(value, 0f, 100f);
    }


    public void AdjustPistons(float delta) => ChangePistons(pistonPressure + delta);

    public void ChangeRPM(float value)
    {
        mpsRPM = Mathf.Clamp(Mathf.Round(value / 100f) * 100f, 0f, 1200f);
    }
    public void AdjustRPM(float delta) => ChangeRPM(mpsRPM + delta);

    public void ChangeMPSDepth(float value) => mpsDepth = Mathf.Clamp(value, 0f, 100f);
    public void AdjustMPSDepth(float delta) => ChangeMPSDepth(mpsDepth + delta);

    public void ChangeMZSDepth(float value) => mzsDepth = Mathf.Clamp(value, 0f, 100f);
    public void AdjustMZSDepth(float delta) => ChangeMZSDepth(mzsDepth + delta);

    public bool IsCurrentStable() =>
        currentAmperes >= targetCurrentMin && currentAmperes <= targetCurrentMax;

    // ── НОВОЕ: метод фиксации поршней ──
    public void LockPistons()
    {
        // Можно фиксировать только если в правильном диапазоне
        if (pistonPressure >= pistonMinActive && pistonPressure <= pistonMaxActive)
        {
            pistonLocked = !pistonLocked; // Toggle
            lockedPistonValue = pistonPressure;

            Debug.Log(pistonLocked ?
                $"[CrystalDrive] Поршни ЗАФИКСИРОВАНЫ на {pistonPressure:F0}%" :
                $"[CrystalDrive] Поршни РАЗБЛОКИРОВАНЫ");
        }
        else
        {
            Debug.LogWarning($"[CrystalDrive] Поршни вне нужного диапазона! ({pistonPressure:F0}%)");
        }
    }

    // ── Публичный метод проверки, находятся ли поршни в идеальной зоне ──
    public bool IsPistonInIdealZone() =>
        pistonPressure >= pistonMinActive && pistonPressure <= pistonMaxActive;

    // ── Для проверки критического давления ──
    public bool IsPistonOverPressure() => pistonPressure > pistonMaxActive;

    // ── НОВОЕ ──
    public float GetPistonNormalized()
    {
        return pistonPressure / 100f;
    }
}


