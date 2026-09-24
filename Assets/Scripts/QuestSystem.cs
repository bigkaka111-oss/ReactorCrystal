using UnityEngine;

[System.Serializable]
public class DifficultyStep
{
    public float minAmps = 8f, maxAmps = 12f, holdSeconds = 10f;
    [Tooltip("Independent multiplier of the base rate, not cumulative.")]
    public float instabilityMultiplier = 1f;
    public Color crystalColor = Color.cyan;
    public float colorTransitionDuration = 1.5f;
}
[DefaultExecutionOrder(-100)]
public class QuestSystem : MonoBehaviour
{
    public enum SessionState { Ready, Running, Stopped, Completed, Failed }
    [SerializeField] private CrystalDrive crystalDrive;
    [SerializeField] private CrystalVisuals crystalVisuals;
    [SerializeField] private CrystalAudio crystalAudio;
    public float initialInstabilityGrowthRate = 0.008f;
    public DifficultyStep[] steps = {
        new DifficultyStep { minAmps=8, maxAmps=12, holdSeconds=10 },
        new DifficultyStep { minAmps=14, maxAmps=18, holdSeconds=12, instabilityMultiplier=1.15f, crystalColor=new Color(0.3f,1f,0.65f) },
        new DifficultyStep { minAmps=20, maxAmps=24, holdSeconds=14, instabilityMultiplier=1.3f, crystalColor=new Color(1f,0.85f,0.3f) },
        new DifficultyStep { minAmps=26, maxAmps=30, holdSeconds=16, instabilityMultiplier=1.5f, crystalColor=new Color(1f,0.55f,0.25f) },
        new DifficultyStep { minAmps=32, maxAmps=36, holdSeconds=18, instabilityMultiplier=1.7f, crystalColor=new Color(1f,0.3f,0.35f) }
    };
    public AudioClip stepCompleteSound, questCompleteSound, failSound;
    public SessionState State { get; private set; } = SessionState.Ready;
    private int currentStep;
    private float holdTimer;
    public int CurrentStep => currentStep;
    public int TotalSteps => steps?.Length ?? 0;
    public DifficultyStep ActiveStep => TotalSteps > 0 ? steps[Mathf.Clamp(currentStep, 0, TotalSteps - 1)] : null;
    public float HoldProgress => IsCompleted ? 1f : ActiveStep == null ? 0f : Mathf.Clamp01(holdTimer / Mathf.Max(0.1f, ActiveStep.holdSeconds));
    public bool IsRunning => State == SessionState.Running;
    public bool IsCompleted => State == SessionState.Completed;
    public bool IsFailed => State == SessionState.Failed;
    public bool IsTerminal => IsCompleted || IsFailed;
    public float CurrentHoldTimer => holdTimer;
    public float ElapsedSeconds { get; private set; }
    public event System.Action<int> OnStepChanged;
    public event System.Action OnQuestCompleted, OnQuestFailed;
    public event System.Action<SessionState> OnStateChanged;
    private void Start() => ResetQuest();
    private void Update() => Tick(Time.deltaTime);
    public void Tick(float dt)
    {
        if (dt <= 0f || crystalDrive == null || ActiveStep == null || IsTerminal || State == SessionState.Stopped) return;
        // Failure wins simultaneous outcomes regardless of Update order.
        if (crystalDrive.instability >= 100f) { FailQuest(); return; }
        if (State == SessionState.Ready)
        {
            if (crystalDrive.IsCurrentStable()) StartQuest(); else return;
        }
        ElapsedSeconds += dt;
        holdTimer = crystalDrive.IsCurrentStable() ? holdTimer + dt : Mathf.Max(0f, holdTimer - dt * 2f);
        if (holdTimer < ActiveStep.holdSeconds) return;
        if (currentStep + 1 >= TotalSteps)
        {
            State = SessionState.Completed;
            crystalDrive.SetControlsEnabled(false);
            crystalAudio?.PlayOneShot(questCompleteSound, 0.5f);
            OnStateChanged?.Invoke(State);
            OnQuestCompleted?.Invoke();
            return;
        }
        currentStep++;
        holdTimer = 0f;
        SetupStep();
        crystalAudio?.PlayOneShot(stepCompleteSound, 0.4f);
        OnStepChanged?.Invoke(currentStep);
    }
    private void SetupStep()
    {
        var step = ActiveStep;
        if (step == null || crystalDrive == null) return;
        crystalDrive.SetTargetCurrentRange(step.minAmps, step.maxAmps);
        crystalDrive.SetInstabilityGrowthRate(initialInstabilityGrowthRate * step.instabilityMultiplier);
        crystalVisuals?.LerpToColor(step.crystalColor, step.colorTransitionDuration);
    }
    public void StartQuest()
    {
        if (IsTerminal || State == SessionState.Running || crystalDrive == null || ActiveStep == null) return;
        State = SessionState.Running;
        crystalDrive.SetControlsEnabled(true);
        SetupStep();
        OnStateChanged?.Invoke(State);
    }
    public void StopQuest()
    {
        if (IsTerminal) return;
        State = SessionState.Stopped;
        crystalDrive?.SetControlsEnabled(false);
        OnStateChanged?.Invoke(State);
    }
    public bool FailQuest()
    {
        if (IsTerminal || State == SessionState.Stopped) return false;
        State = SessionState.Failed;
        crystalDrive?.SetControlsEnabled(false);
        crystalAudio?.PlayOneShot(failSound, 0.5f);
        OnStateChanged?.Invoke(State);
        OnQuestFailed?.Invoke();
        return true;
    }
    public void ResetQuest()
    {
        currentStep = 0; holdTimer = ElapsedSeconds = 0f;
        State = SessionState.Ready;
        crystalDrive?.ResetDrive();
        foreach (var lever in FindObjectsByType<CrystalLever>(FindObjectsSortMode.None)) if (lever.drive == crystalDrive) lever.SyncFromDrive();
        crystalAudio?.ResetMpsPitch();
        SetupStep();
        OnStateChanged?.Invoke(State);
        OnStepChanged?.Invoke(0);
    }
    private void OnValidate()
    {
        initialInstabilityGrowthRate = Mathf.Max(0f, initialInstabilityGrowthRate);
        if (steps == null) return;
        for (int i=0; i<steps.Length; i++)
        {
            if (steps[i] == null) steps[i] = new DifficultyStep();
            var s = steps[i];
            s.minAmps = Mathf.Max(0f, s.minAmps); s.maxAmps = Mathf.Max(s.minAmps + 0.1f, s.maxAmps);
            s.holdSeconds = Mathf.Max(0.1f, s.holdSeconds); s.instabilityMultiplier = Mathf.Max(0f, s.instabilityMultiplier);
            s.colorTransitionDuration = Mathf.Max(0f, s.colorTransitionDuration);
        }
    }
}

