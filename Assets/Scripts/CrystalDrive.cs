using UnityEngine;

public class CrystalDrive : MonoBehaviour
{
    [SerializeField] private float pistonMinActive = 70f, pistonMaxActive = 85f;
    [SerializeField] private float baseRPMNorm = 600f, currentScale = 20f, mzsReduction = 8f;
    [SerializeField] private float currentLerpSpeed = 0.65f;
    [SerializeField] private float targetCurrentMin = 8f, targetCurrentMax = 12f;
    [SerializeField] private float instabilityGrowthRate = 0.008f;
    [Tooltip("Instability percentage points recovered per second.")]
    [SerializeField] private float instabilityDecayRate = 4f;
    [HideInInspector] public float pistonPressure, mpsDepth, mpsRPM, mzsDepth, currentAmperes, instability;
    public bool crystalActive;
    [HideInInspector] public bool pistonLocked;
    public bool ControlsEnabled { get; private set; } = true;
    public float InstabilityRate { get; private set; }
    public float TargetCurrentMin => targetCurrentMin;
    public float TargetCurrentMax => targetCurrentMax;
    public float SafeRPM => mpsDepth * 8f;
    public event System.Action<bool> LockChanged;
    public event System.Action EmergencyStopped;
    public event System.Action<string> Feedback;
    private void FixedUpdate() => Simulate(Time.fixedDeltaTime);
    public void Simulate(float dt)
    {
        if (!ControlsEnabled || dt <= 0f) return;
        crystalActive = IsPistonInIdealZone();
        if (crystalActive)
        {
            float target = Mathf.Max(0f, mpsRPM / Mathf.Max(1f, baseRPMNorm) * mpsDepth / 100f * currentScale - mzsDepth / 100f * mzsReduction);
            currentAmperes = Mathf.Lerp(currentAmperes, target, 1f - Mathf.Exp(-currentLerpSpeed * dt));
        }
        else currentAmperes = Mathf.MoveTowards(currentAmperes, 0f, dt * 4f);
        float excess = Mathf.Max(0f, mpsRPM - SafeRPM);
        InstabilityRate = !crystalActive || mpsDepth <= 0f ? -instabilityDecayRate * 1.5f
            : excess > 0f ? excess * instabilityGrowthRate - mzsDepth / 100f * 3f : -instabilityDecayRate;
        instability = Mathf.Clamp(instability + InstabilityRate * dt, 0f, 100f);
    }
    public void SetControlsEnabled(bool value) { ControlsEnabled = value; if (!value) InstabilityRate = 0f; }
    public void ResetDrive()
    {
        bool wasLocked = pistonLocked;
        pistonPressure = mpsDepth = mpsRPM = mzsDepth = currentAmperes = instability = InstabilityRate = 0f;
        crystalActive = pistonLocked = false;
        ControlsEnabled = true;
        if (wasLocked) LockChanged?.Invoke(false);
    }
    public void EmergencyStop()
    {
        if (!ControlsEnabled) return;
        bool wasLocked = pistonLocked;
        pistonLocked = false;
        pistonPressure = mpsRPM = 0f;
        crystalActive = false;
        if (wasLocked) LockChanged?.Invoke(false);
        EmergencyStopped?.Invoke();
        Feedback?.Invoke("АВАРИЙНОЕ ОТКЛЮЧЕНИЕ · Установка остывает");
    }
    public void ChangePistons(float value) { if (ControlsEnabled && !pistonLocked) pistonPressure = Mathf.Clamp(value, 0f, 100f); }
    public void AdjustPistons(float delta) => ChangePistons(pistonPressure + delta);
    public void ChangeRPM(float value) { if (ControlsEnabled) mpsRPM = Mathf.Clamp(Mathf.Round(value / 100f) * 100f, 0f, 1200f); }
    public void AdjustRPM(float delta) => ChangeRPM(mpsRPM + delta);
    public void ChangeMPSDepth(float value) { if (ControlsEnabled) mpsDepth = Mathf.Clamp(value, 0f, 100f); }
    public void AdjustMPSDepth(float delta) => ChangeMPSDepth(mpsDepth + delta);
    public void ChangeMZSDepth(float value) { if (ControlsEnabled) mzsDepth = Mathf.Clamp(value, 0f, 100f); }
    public void AdjustMZSDepth(float delta) => ChangeMZSDepth(mzsDepth + delta);
    public void LockPistons()
    {
        if (!ControlsEnabled) return;
        if (!pistonLocked && !IsPistonInIdealZone()) { Feedback?.Invoke($"Для фиксации: давление {pistonMinActive:0}–{pistonMaxActive:0}%"); return; }
        pistonLocked = !pistonLocked;
        LockChanged?.Invoke(pistonLocked);
        Feedback?.Invoke(pistonLocked ? "ПОРШНИ ЗАФИКСИРОВАНЫ" : "ПОРШНИ РАЗБЛОКИРОВАНЫ");
    }
    public bool IsCurrentStable() => crystalActive && currentAmperes >= targetCurrentMin && currentAmperes <= targetCurrentMax;
    public bool IsPistonInIdealZone() => pistonPressure >= pistonMinActive && pistonPressure <= pistonMaxActive;
    public bool IsPistonOverPressure() => pistonPressure > pistonMaxActive;
    public float GetMaxSafePressure() => pistonMaxActive;
    public float GetMinSafePressure() => pistonMinActive;
    public float GetPistonNormalized() => pistonPressure / 100f;
    public void SetTargetCurrentRange(float min, float max) { targetCurrentMin = Mathf.Max(0f, min); targetCurrentMax = Mathf.Max(targetCurrentMin + 0.1f, max); }
    public void SetInstabilityGrowthRate(float rate) => instabilityGrowthRate = Mathf.Max(0f, rate);
    public float GetInstabilityGrowthRate() => instabilityGrowthRate;
    private void OnValidate()
    {
        pistonMinActive = Mathf.Clamp(pistonMinActive, 0f, 99f);
        pistonMaxActive = Mathf.Clamp(pistonMaxActive, pistonMinActive + 1f, 100f);
        baseRPMNorm = Mathf.Max(1f, baseRPMNorm);
        currentLerpSpeed = Mathf.Max(0.01f, currentLerpSpeed);
        instabilityGrowthRate = Mathf.Max(0f, instabilityGrowthRate);
        instabilityDecayRate = Mathf.Max(0.1f, instabilityDecayRate);
    }
}

