using UnityEngine;
public class CrystalVisuals : MonoBehaviour
{
    public CrystalDrive drive;
    public Light bunkerLight;
    public float baseIntensity=100f,maxIntensity=900f,smoothingSpeed=3f;
    [HideInInspector] public float colorLerpTime,colorLerpDuration;
    private Color currentTargetColor=Color.cyan,colorLerpFrom;
    private float currentIntensity;
    private void Awake()
    {
        if(bunkerLight!=null) { currentIntensity=bunkerLight.intensity; currentTargetColor=bunkerLight.color; }
    }
    private void Update()
    {
        if(drive==null||bunkerLight==null)return;
        colorLerpTime+=Time.deltaTime;
        bunkerLight.color=Color.Lerp(colorLerpFrom,currentTargetColor,colorLerpDuration<=0f?1f:Mathf.Clamp01(colorLerpTime/colorLerpDuration));
        float target=Mathf.Lerp(baseIntensity,maxIntensity,drive.instability/100f);
        currentIntensity=Mathf.Lerp(currentIntensity,target,1f-Mathf.Exp(-smoothingSpeed*Time.deltaTime));
        bunkerLight.intensity=currentIntensity;
    }
    public void LerpToColor(Color color,float duration)
    {
        if(bunkerLight==null)return;
        colorLerpFrom=bunkerLight.color; currentTargetColor=color; colorLerpTime=0f; colorLerpDuration=Mathf.Max(0f,duration);
        if(duration<=0f)bunkerLight.color=color;
    }
}

