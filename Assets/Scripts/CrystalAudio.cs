using UnityEngine;

[System.Serializable]
public class AudioLayer
{
    public AudioSource source;
    public float targetVolume = 1f;
    public float fadeSpeed = 2f;

    [HideInInspector] public float currentVolume = 0f;
}

public class CrystalAudio : MonoBehaviour
{
    [Header("References")]
    public CrystalDrive drive;

    [Header("Thresholds")]
    [Range(0f, 1f)] public float warningThreshold = 0.85f;
    [Range(0f, 1f)] public float warningResetThreshold = 0.8f; // hysteresis

    [Header("Audio Layers")]
    public AudioLayer idealLayer;
    public AudioLayer warningLayer;
    public AudioLayer mpsLayer;

    [Header("One-shot Sounds")]
    public AudioSource lockSound;

    [Header("MPS Settings")]
    public float minPitch = 0.5f;
    public float maxPitch = 2f;
    public float maxRPM = 1500f;

    private bool previousCrystalActive;
    private bool isWarningActive;

    void Start()
    {
        InitLayer(idealLayer);
        InitLayer(warningLayer);
        InitLayer(mpsLayer);
    }

    void Update()
    {
        if (drive == null) return;

        HandleLock();
        UpdateStates();
        UpdateAudio();
    }

    void InitLayer(AudioLayer layer)
    {
        if (layer.source == null) return;
        layer.source.loop = true;
        layer.source.volume = 0f;
        layer.currentVolume = 0f;
        layer.source.Play();
    }

    void HandleLock()
    {
        if (drive.crystalActive && !previousCrystalActive)
        {
            if (lockSound != null) lockSound.Play();
        }
        previousCrystalActive = drive.crystalActive;
    }

    void UpdateStates()
{
    float piston = drive.pistonPressure; // 0..100

    float warningStart = drive.GetMaxSafePressure();   // = pistonMaxActive
    float warningEnd = warningStart - 5f;              // гистерезис

    if (!isWarningActive && piston >= warningStart)
        isWarningActive = true;
    else if (isWarningActive && piston <= warningEnd)
        isWarningActive = false;
}

    void UpdateAudio()
    {
        bool ideal = drive.crystalActive && drive.IsPistonInIdealZone() && !isWarningActive;
        bool warning = isWarningActive;
        bool mps = drive.crystalActive && drive.mpsRPM > 0;

        // PRIORITY: warning > ideal
        if (warning)
        {
            SetLayer(warningLayer, true);
            SetLayer(idealLayer, false);
        }
        else
        {
            SetLayer(warningLayer, false);
            SetLayer(idealLayer, ideal);
        }

        SetLayer(mpsLayer, mps);

        UpdateMpsPitch();

        // Apply smooth fade
        ApplyFade(idealLayer);
        ApplyFade(warningLayer);
        ApplyFade(mpsLayer);
    }

    void SetLayer(AudioLayer layer, bool active)
    {
        if (layer.source == null) return;
        layer.targetVolume = active ? 1f : 0f;
    }

    void ApplyFade(AudioLayer layer)
    {
        if (layer.source == null) return;

        layer.currentVolume = Mathf.MoveTowards(
            layer.currentVolume,
            layer.targetVolume,
            layer.fadeSpeed * Time.deltaTime
        );

        layer.source.volume = layer.currentVolume;
    }

    void UpdateMpsPitch()
    {
        if (mpsLayer.source == null) return;

        float t = Mathf.Clamp01(drive.mpsRPM / maxRPM);
        float pitch = Mathf.Lerp(minPitch, maxPitch, t);

        mpsLayer.source.pitch = pitch;
    }
}
