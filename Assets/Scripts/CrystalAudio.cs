using UnityEngine;
[System.Serializable]
public class AudioLayer
{
    public AudioSource source;
    [Range(0f,1f)] public float targetVolume = 0.25f;
    public float fadeSpeed = 1f;
    [HideInInspector] public float currentVolume;
}
public class CrystalAudio : MonoBehaviour
{
    public CrystalDrive drive;
    [Range(0f,1f)] public float warningThreshold = 0.65f, warningResetThreshold = 0.5f;
    public AudioLayer idealLayer = new AudioLayer(), warningLayer = new AudioLayer(), mpsLayer = new AudioLayer();
    public AudioSource lockSound;
    public float minPitch = 0.6f, maxPitch = 1.6f, maxRPM = 1200f;
    private AudioSource oneShots;
    private bool warning;
    private float mpsPitchOverride = -1f;
    private void Awake()
    {
        oneShots = gameObject.AddComponent<AudioSource>();
        oneShots.playOnAwake = false; oneShots.spatialBlend = 0f;
    }
    private void OnEnable() { if (drive != null) drive.LockChanged += OnLock; }
    private void OnDisable() { if (drive != null) drive.LockChanged -= OnLock; }
    private void OnLock(bool locked) { if (lockSound != null && drive.ControlsEnabled) lockSound.Play(); }
    private void Start() { Init(idealLayer); Init(warningLayer); Init(mpsLayer); }
    private void Init(AudioLayer layer)
    {
        if (layer?.source == null) return;
        layer.source.loop = true; layer.source.volume = layer.currentVolume = 0f;
        if (layer.source.clip != null) layer.source.Play();
    }
    private void Update()
    {
        if (drive == null) return;
        if (drive.instability >= warningThreshold * 100f || drive.IsPistonOverPressure()) warning = true;
        else if (drive.instability <= warningResetThreshold * 100f && !drive.IsPistonOverPressure()) warning = false;
        bool operating = drive.ControlsEnabled;
        Fade(idealLayer, operating && drive.crystalActive && !warning);
        Fade(warningLayer, operating && warning);
        Fade(mpsLayer, operating && drive.crystalActive && drive.mpsRPM > 0f);
        if (mpsLayer?.source != null)
            mpsLayer.source.pitch = mpsPitchOverride >= 0f ? mpsPitchOverride : Mathf.Lerp(minPitch,maxPitch,Mathf.Clamp01(drive.mpsRPM/Mathf.Max(1f,maxRPM)));
    }
    private void Fade(AudioLayer layer, bool active)
    {
        if (layer?.source == null) return;
        layer.currentVolume = Mathf.MoveTowards(layer.currentVolume, active ? Mathf.Clamp01(layer.targetVolume) : 0f, Mathf.Max(0f,layer.fadeSpeed)*Time.unscaledDeltaTime);
        layer.source.volume = layer.currentVolume;
    }
    public void PlayOneShot(AudioClip clip,float volume=1f) { if (clip != null && oneShots != null) oneShots.PlayOneShot(clip,Mathf.Clamp01(volume)); }
    public void SetLayerVolume(string layerName,float volume)
    {
        var layer = layerName?.ToLowerInvariant() switch { "ideal"=>idealLayer, "warning"=>warningLayer, "mps"=>mpsLayer, _=>null };
        if (layer != null) layer.targetVolume = Mathf.Clamp01(volume);
    }
    public void SetMpsPitch(float pitch) => mpsPitchOverride = Mathf.Clamp(pitch,0.1f,3f);
    public void ResetMpsPitch() { mpsPitchOverride=-1f; warning=false; }
    private void OnValidate() { warningResetThreshold=Mathf.Clamp(warningResetThreshold,0f,warningThreshold); maxRPM=Mathf.Max(1f,maxRPM); }
}

