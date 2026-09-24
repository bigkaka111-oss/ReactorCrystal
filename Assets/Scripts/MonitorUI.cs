using UnityEngine;
using TMPro;
public class MonitorUI : MonoBehaviour
{
    [SerializeField] private CrystalDrive crystalDrive;
    [SerializeField] private TextMeshProUGUI pistonText,rpmText,mpsDepthText,mzsDepthText,amperesText,statusText,instabilityText;
    [SerializeField] private Color safeColor=new Color(0.3f,1f,0.75f),warningColor=new Color(1f,0.75f,0.25f),dangerColor=new Color(1f,0.3f,0.25f),inactiveColor=new Color(0.55f,0.65f,0.65f);
    [SerializeField] private float updateInterval=0.1f;
    private float timer;
    private QuestSystem quest;
    private void Start() { quest=FindFirstObjectByType<QuestSystem>(); RefreshDisplay(); }
    private void Update() { timer+=Time.unscaledDeltaTime; if(timer>=Mathf.Clamp(updateInterval,0.03f,0.2f)){timer=0f;RefreshDisplay();} }
    private void Set(TextMeshProUGUI field,string value,Color color) { if(field!=null){field.text=value;field.color=color;} }
    private void RefreshDisplay()
    {
        if(crystalDrive==null)return;
        var d=crystalDrive;
        Set(pistonText,$"ПОРШНИ: {d.pistonPressure:0}%"+(d.pistonLocked?" [LOCK]":""),d.IsPistonInIdealZone()?safeColor:warningColor);
        Set(rpmText,$"МПС RPM: {d.mpsRPM:0}",d.mpsRPM>d.SafeRPM?warningColor:safeColor);
        Set(mpsDepthText,$"МПС ГЛУБ: {d.mpsDepth:0}%",safeColor);
        Set(mzsDepthText,$"МЗС ГЛУБ: {d.mzsDepth:0}%",inactiveColor);
        Set(amperesText,$"ТОК: {d.currentAmperes:0.00} А",d.IsCurrentStable()?safeColor:warningColor);
        Set(instabilityText,$"РИСК: {d.instability:0}% {(d.InstabilityRate>0?"+":"")}{d.InstabilityRate:0.0}/с",d.instability>=70?dangerColor:d.InstabilityRate>0?warningColor:safeColor);
        string status=quest!=null&&quest.IsCompleted?"ЦИКЛ ЗАВЕРШЁН":quest!=null&&quest.IsFailed?"АВАРИЯ · ОТКЛЮЧЕНО":!d.crystalActive?"КРИСТАЛЛ НЕАКТИВЕН":d.instability>=70?"ОПАСНО · ОХЛАДИТЕ":d.InstabilityRate>0?"НЕСТАБИЛЬНОСТЬ РАСТЁТ":d.IsCurrentStable()?"ТОК В ЦЕЛИ":"НАСТРОЙТЕ ТОК";
        Set(statusText,status,d.instability>=70?dangerColor:d.InstabilityRate>0?warningColor:safeColor);
    }
}

