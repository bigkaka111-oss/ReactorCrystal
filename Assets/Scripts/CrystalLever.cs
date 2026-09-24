using UnityEngine;
public class CrystalLever : MonoBehaviour
{
    public CrystalDrive drive;
    public enum LeverAction { MpsDepth, MzsDepth, Pistons, RPM }
    public LeverAction action;
    public float maxValueForSystem = 100f;
    public Vector3 moveAxis = Vector3.forward;
    public float minValue = -0.1f, maxValue = 0.1f, sensitivity = 0.001f, smoothSpeed = 3f;
    private bool isDragging, initialized;
    private float dragStartMouse, dragStartValue, targetSystemValue, smoothedSystemValue;
    private Vector3 startLocalPos;
    public float TargetValue => targetSystemValue;
    private bool Locked => action == LeverAction.Pistons && drive != null && drive.pistonLocked;
    private void Awake() => Initialize();
    private void Initialize()
    {
        if (initialized) return;
        initialized = true; startLocalPos = transform.localPosition;
        moveAxis = moveAxis.sqrMagnitude > 0f ? moveAxis.normalized : Vector3.forward;
        SyncFromDrive();
    }
    private void OnEnable() { if (drive != null) { drive.LockChanged += OnLock; drive.EmergencyStopped += SyncFromDrive; } }
    private void OnDisable() { isDragging = false; if (drive != null) { drive.LockChanged -= OnLock; drive.EmergencyStopped -= SyncFromDrive; } }
    private void OnLock(bool value) { if (action == LeverAction.Pistons) SyncFromDrive(); }
    public void SyncFromDrive()
    {
        if (!initialized) { Initialize(); return; }
        if (drive == null) return;
        isDragging = false;
        targetSystemValue = smoothedSystemValue = action switch {
            LeverAction.Pistons => drive.pistonPressure, LeverAction.RPM => drive.mpsRPM,
            LeverAction.MpsDepth => drive.mpsDepth, _ => drive.mzsDepth };
        UpdatePosition();
    }
    public void SetTargetValue(float value)
    {
        if (drive == null || !drive.ControlsEnabled || Locked) return;
        targetSystemValue = Mathf.Clamp(value, 0f, maxValueForSystem); UpdatePosition();
    }
    private void UpdatePosition() => transform.localPosition = startLocalPos + moveAxis * Mathf.Lerp(minValue, maxValue, targetSystemValue / Mathf.Max(1f, maxValueForSystem));
    private void OnMouseDown()
    {
        if (WorldInteraction.Blocked || drive == null || !drive.ControlsEnabled || Locked) return;
        isDragging = true; dragStartMouse = Input.mousePosition.y;
        dragStartValue = Mathf.Lerp(minValue, maxValue, targetSystemValue / Mathf.Max(1f, maxValueForSystem));
    }
    private void OnMouseUp() => isDragging = false;
    private void OnApplicationFocus(bool focus) { if (!focus) isDragging = false; }
    private void Update()
    {
        if (drive == null || !drive.ControlsEnabled || Time.timeScale <= 0f) { isDragging = false; return; }
        if (Locked) { SyncFromDrive(); return; }
        if (isDragging && !Input.GetMouseButton(0)) isDragging = false;
        if (isDragging) SetTargetValue(Mathf.InverseLerp(minValue, maxValue, dragStartValue + (Input.mousePosition.y - dragStartMouse) * sensitivity) * maxValueForSystem);
        smoothedSystemValue = Mathf.Lerp(smoothedSystemValue, targetSystemValue, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        if (Mathf.Abs(smoothedSystemValue - targetSystemValue) < 0.001f) smoothedSystemValue = targetSystemValue;
        switch (action)
        {
            case LeverAction.MpsDepth: drive.ChangeMPSDepth(smoothedSystemValue); break;
            case LeverAction.MzsDepth: drive.ChangeMZSDepth(smoothedSystemValue); break;
            case LeverAction.Pistons: drive.ChangePistons(smoothedSystemValue); break;
            case LeverAction.RPM: drive.ChangeRPM(smoothedSystemValue); break;
        }
    }
    private void OnValidate() { maxValueForSystem = action == LeverAction.RPM ? 1200f : 100f; maxValue = Mathf.Max(minValue + 0.0001f, maxValue); smoothSpeed = Mathf.Max(0.1f, smoothSpeed); }
}

