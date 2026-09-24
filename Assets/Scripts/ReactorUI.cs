using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>Presentation and pause only. QuestSystem owns all simulation outcomes.</summary>
[DefaultExecutionOrder(100)]
public class ReactorUI : MonoBehaviour
{
    public CrystalDrive drive;
    public QuestSystem quest;
    public CameraController cameraController;
    public TMP_FontAsset font;
    public Canvas navigationCanvas;
    public static bool ModalOpen { get; private set; }
    public bool IsMenuOpen => modal != null && modal.activeSelf;
    private Canvas canvas;
    private GameObject hud, modal;
    private RectTransform card;
    private TMP_Text stageText,targetText,progressText,riskText,hintText,toastText;
    private UnityEngine.UI.Image progressFill,riskFill;
    private TMP_Text[] leverTexts=new TMP_Text[4];
    private UnityEngine.UI.Image[] leverCards=new UnityEngine.UI.Image[4];
    private CrystalLever[] levers=new CrystalLever[4];
    private int selected;
    private float toastUntil,masterVolume=0.7f;
    private bool motion=true,started;
    private string page="title";
    private readonly Color ink=new Color(0.025f,0.055f,0.065f,1f);
    private readonly Color panel=new Color(0.025f,0.06f,0.07f,0.86f);
    private readonly Color mint=new Color(0.4f,0.95f,0.78f);
    private readonly Color amber=new Color(1f,0.73f,0.3f);
    private readonly Color paper=new Color(0.91f,0.94f,0.9f);
    private readonly string[] names={"ПОРШНИ","ГЛУБИНА МПС","ОБОРОТЫ МПС","СТАБИЛИЗАТОР"};
    private void Start()
    {
        if(drive==null)drive=FindFirstObjectByType<CrystalDrive>();
        if(quest==null)quest=FindFirstObjectByType<QuestSystem>();
        if(cameraController==null&&Camera.main!=null)cameraController=Camera.main.GetComponent<CameraController>();
        foreach(var l in FindObjectsByType<CrystalLever>(FindObjectsSortMode.None))
        {
            int i=l.action==CrystalLever.LeverAction.Pistons?0:l.action==CrystalLever.LeverAction.MpsDepth?1:l.action==CrystalLever.LeverAction.RPM?2:3;
            levers[i]=l;
        }
        masterVolume=PlayerPrefs.GetFloat("Contour.Volume",0.7f);
        motion=PlayerPrefs.GetInt("Contour.Motion",0)==1;
        ApplySettings();
        Build();
        quest.OnStateChanged+=OnState;
        quest.OnStepChanged+=OnStep;
        drive.Feedback+=Toast;
        ShowPage("title");
    }
    private RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform)); var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    private UnityEngine.UI.Image Box(Transform parent,string name,float x,float y,float w,float h,Color color,bool raycast=false)
    {
        var r=Rect(parent,name,x,y,w,h);var img=r.gameObject.AddComponent<UnityEngine.UI.Image>();img.color=color;img.raycastTarget=raycast;return img;
    }
    private TMP_Text Text(Transform parent,string name,string value,float x,float y,float w,float h,float size,Color color)
    {
        var r=Rect(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        if(font!=null)t.font=font;t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;
        t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;return t;
    }
    private UnityEngine.UI.Button Button(Transform parent,string title,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,bool primary=false)
    {
        var img=Box(parent,title,x,y,w,h,primary?mint:new Color(0.09f,0.16f,0.17f,1),true);
        var b=img.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=img;
        var colors=b.colors;colors.highlightedColor=new Color(0.75f,1f,0.9f);colors.pressedColor=new Color(0.5f,0.8f,0.7f);b.colors=colors;
        var label=Text(img.transform,"Label",title,16,0,w-32,h,22,primary?ink:paper);label.alignment=TextAlignmentOptions.MidlineLeft;
        b.onClick.AddListener(action);return b;
    }
    private void Build()
    {
        var root=new GameObject("ContourInterface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        root.transform.SetParent(transform,false);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=0.5f;
        hud=Rect(root.transform,"HUD",0,0,1600,900).gameObject;
        var hr=hud.GetComponent<RectTransform>();hr.anchorMin=Vector2.zero;hr.anchorMax=Vector2.one;hr.offsetMin=hr.offsetMax=Vector2.zero;
        var left=Box(hud.transform,"Experiment",32,28,450,152,panel);
        Text(left.transform,"Kicker","КОНТУР  /  ЛАБОРАТОРИЯ 07",20,15,410,24,16,mint);
        stageText=Text(left.transform,"Stage","",20,42,410,30,24,paper);
        targetText=Text(left.transform,"Target","",20,80,410,30,23,amber);
        Box(left.transform,"Track",20,122,280,4,new Color(0.2f,0.3f,0.3f));
        progressFill=Box(left.transform,"Progress",20,122,0,4,mint);
        progressText=Text(left.transform,"Time","",310,109,130,26,16,paper);
        var right=Box(hud.transform,"Risk",0,28,310,140,panel);
        var rr=right.rectTransform;rr.anchorMin=rr.anchorMax=new Vector2(1,1);rr.anchoredPosition=new Vector2(-342,-28);
        Text(right.transform,"Kicker","СОСТОЯНИЕ УСТАНОВКИ",20,15,280,22,16,mint);
        riskText=Text(right.transform,"Value","",20,44,280,34,28,paper);
        Box(right.transform,"Track",20,94,270,5,new Color(0.2f,0.3f,0.3f));riskFill=Box(right.transform,"RiskFill",20,94,0,5,amber);
        Button(right.transform,"ПАУЗА  /  ESC",20,108,270,26,()=>ShowPage("pause"));
        var bottom=Box(hud.transform,"Controls",0,0,1050,112,panel);
        var br=bottom.rectTransform;br.anchorMin=br.anchorMax=new Vector2(0.5f,0);br.anchoredPosition=new Vector2(-525,132);
        for(int i=0;i<4;i++)
        {
            int index=i;var b=Button(bottom.transform,"",12+i*258,12,250,54,()=>selected=index);
            leverCards[i]=b.GetComponent<UnityEngine.UI.Image>();leverTexts[i]=b.GetComponentInChildren<TMP_Text>();
        }
        hintText=Text(bottom.transform,"Hint","1–4: выбор · ↑ ↓ / колесо: настройка · F: фиксация · X: отключить",18,76,1010,26,17,paper);
        toastText=Text(hud.transform,"Notice","",0,0,1050,44,22,amber);
        var tr=toastText.rectTransform;tr.anchorMin=tr.anchorMax=new Vector2(0.5f,0);tr.anchoredPosition=new Vector2(-525,185);toastText.alignment=TextAlignmentOptions.Center;
        modal=Box(root.transform,"Modal",0,0,1600,900,new Color(0.008f,0.02f,0.025f,0.7f),true).gameObject;
        var mr=modal.GetComponent<RectTransform>();mr.anchorMin=Vector2.zero;mr.anchorMax=Vector2.one;mr.offsetMin=mr.offsetMax=Vector2.zero;
        card=Box(modal.transform,"Card",0,0,840,680,ink,true).rectTransform;card.anchorMin=card.anchorMax=new Vector2(0.5f,0.5f);card.anchoredPosition=new Vector2(-420,340);
    }
    public void BeginRun()
    {
        quest.ResetQuest();started=true;cameraController?.LookCenter();ClosePage();
        Toast("Начните с поршней 70–85% и глубины МПС 100%. Затем поднимите обороты.");
    }
    public void ShowPage(string value)
    {
        page=value;ModalOpen=true;modal.SetActive(true);hud.SetActive(false);
        if(navigationCanvas!=null)navigationCanvas.enabled=false;
        Time.timeScale=quest.IsTerminal?1f:0f;AudioListener.pause=!quest.IsTerminal;
        foreach(Transform c in card)Destroy(c.gameObject);
        Box(card,"Accent",0,0,840,4,quest.IsFailed?new Color(1f,0.3f,0.25f):mint);
        Text(card,"Issue","ЛАБОРАТОРИЯ 07     /     ПРОТОКОЛ УПРАВЛЕНИЯ",44,30,760,28,17,mint);
        if(value=="help")
        {
            Text(card,"Title","РУКОВОДСТВО ОПЕРАТОРА",44,82,760,55,36,paper);
            Text(card,"Instructions",
                "01   ПОДГОТОВКА\nПоршни: 70–85%. Нажмите F, чтобы зафиксировать их.\nГлубина МПС: 100%. Начните с 300 RPM.\n\n02   ЭКСПЕРИМЕНТ\nУдерживайте ток в диапазоне на экране. Цель меняется\nпосле каждого этапа. Вне диапазона прогресс убывает.\n\n03   БЕЗОПАСНОСТЬ\nВысокие обороты при малой глубине повышают риск.\nМЗС уменьшает ток и замедляет рост нестабильности.\nПри опасности нажмите X: отключение и охлаждение.\n\nУПРАВЛЕНИЕ\nТяните рычаги мышью или выберите 1–4 и используйте\n↑ ↓ / колесо. A / D / W / S — взгляд, пробел — центр.\nEsc — пауза. H — это руководство.",44,155,750,435,20,paper);
            Button(card,"НАЗАД",44,608,752,46,Back,true);
        }
        else if(value=="settings")
        {
            Text(card,"Title","НАСТРОЙКИ",44,86,740,62,50,paper);
            Text(card,"Body","Комфорт важнее эффекта. Настройки сохраняются.",44,158,740,50,23,paper);
            Button(card,$"ГРОМКОСТЬ: {masterVolume*100:0}%  ·  ИЗМЕНИТЬ",44,248,752,58,()=>{masterVolume=masterVolume>=0.99f?0f:Mathf.Min(1f,masterVolume+0.25f);ApplySettings();ShowPage("settings");});
            Button(card,"ДВИЖЕНИЕ КАМЕРЫ: "+(motion?"ВКЛ":"ВЫКЛ"),44,328,752,58,()=>{motion=!motion;ApplySettings();ShowPage("settings");});
            Button(card,"ЭКРАН: "+(Screen.fullScreen?"ПОЛНЫЙ":"ОКНО"),44,408,752,58,()=>StartCoroutine(ToggleScreen()));
            Button(card,"НАЗАД",44,608,752,46,Back,true);
        }
        else if(quest.IsTerminal&&value=="result")
        {
            Text(card,"Title",quest.IsCompleted?"ЦИКЛ ЗАВЕРШЁН":"АВАРИЙНОЕ ОТКЛЮЧЕНИЕ",44,94,752,130,quest.IsCompleted?56:43,paper);
            Text(card,"Body",quest.IsCompleted
                ?$"Пять этапов пройдены. Контур стабилизирован.\n\nВремя эксперимента: {quest.ElapsedSeconds:0} с\nФинальная нестабильность: {drive.instability:0}%\n\nСпасибо за смену, оператор."
                :$"Нестабильность достигла 100%. Установка отключена.\n\nПройдено этапов: {quest.CurrentStep} из {quest.TotalSteps}\n\nУвеличивайте глубину МПС до повышения оборотов.\nВ следующий раз используйте МЗС или X до аварии,\nчтобы охладить установку и продолжить этап.",44,246,750,250,24,paper);
            Button(card,"НОВАЯ СМЕНА",44,540,752,54,BeginRun,true);
            Button(card,"ВЫЙТИ",44,608,752,46,Quit);
        }
        else
        {
            bool pause=value=="pause";
            Text(card,"Title",pause?"СМЕНА ПРИОСТАНОВЛЕНА":"КОНТУР",44,90,752,110,pause?43:86,paper);
            Text(card,"Body",pause?"Установка на паузе. Параметры сохранены.":"Один оператор. Пять этапов.\nУдержите установку под контролем.",44,207,750,84,27,paper);
            Text(card,"Edition","АТМОСФЕРНАЯ ДЕМОВЕРСИЯ  /  0.1.0",44,318,750,25,16,mint);
            Button(card,pause?"ПРОДОЛЖИТЬ":"НАЧАТЬ СМЕНУ",44,374,752,56,pause?(UnityEngine.Events.UnityAction)ClosePage:BeginRun,true);
            Button(card,"РУКОВОДСТВО",44,448,366,52,()=>ShowPage("help"));
            Button(card,"НАСТРОЙКИ",430,448,366,52,()=>ShowPage("settings"));
            Button(card,pause?"НАЧАТЬ ЗАНОВО":"ВЫЙТИ",44,519,752,48,pause?(UnityEngine.Events.UnityAction)BeginRun:Quit);
            Text(card,"Footer","СЛЕДИТЕ ЗА ТОКОМ. ОСТАВЛЯЙТЕ ЗАПАС БЕЗОПАСНОСТИ.",44,613,752,25,15,amber);
        }
    }
    private System.Collections.IEnumerator ToggleScreen()
    {
        Screen.fullScreen=!Screen.fullScreen;
        yield return null;
        yield return null;
        if(IsMenuOpen&&page=="settings")ShowPage("settings");
    }
    private void Back()=>ShowPage(quest.IsTerminal?"result":started?"pause":"title");
    public void ClosePage()
    {
        if(quest.IsTerminal){ShowPage("result");return;}
        ModalOpen=false;modal.SetActive(false);hud.SetActive(true);Time.timeScale=1f;AudioListener.pause=false;
        if(navigationCanvas!=null)navigationCanvas.enabled=true;
    }
    private void ApplySettings()
    {
        AudioListener.volume=Mathf.Clamp01(masterVolume);
        if(cameraController!=null)cameraController.MotionEnabled=motion;
        PlayerPrefs.SetFloat("Contour.Volume",masterVolume);PlayerPrefs.SetInt("Contour.Motion",motion?1:0);PlayerPrefs.Save();
    }
    private void OnState(QuestSystem.SessionState state) { if(state==QuestSystem.SessionState.Completed||state==QuestSystem.SessionState.Failed)ShowPage("result"); }
    private void OnStep(int index) { if(started)Toast($"ЭТАП {index+1} · ЦЕЛЬ {quest.ActiveStep.minAmps:0}–{quest.ActiveStep.maxAmps:0} А"); }
    public void Toast(string value) { if(toastText==null)return;toastText.text=value;toastUntil=Time.unscaledTime+5f; }
    private void Update()
    {
        if(canvas==null)return;
        if(Input.GetKeyDown(KeyCode.Escape)) { if(IsMenuOpen&&page=="pause")ClosePage();else if(IsMenuOpen)Back();else ShowPage("pause"); }
        if(Input.GetKeyDown(KeyCode.H))ShowPage("help");
        if(IsMenuOpen)return;
        for(int i=0;i<4;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))selected=i;
        var lever=levers[selected];
        if(lever!=null)
        {
            float amount=Input.mouseScrollDelta.y*(selected==2?100f:2f);
            if(Input.GetKey(KeyCode.UpArrow))amount+=(selected==2?300f:25f)*Time.deltaTime;
            if(Input.GetKey(KeyCode.DownArrow))amount-=(selected==2?300f:25f)*Time.deltaTime;
            if(amount!=0f)lever.SetTargetValue(lever.TargetValue+amount);
        }
        if(Input.GetKeyDown(KeyCode.F))drive.LockPistons();
        if(Input.GetKeyDown(KeyCode.X))drive.EmergencyStop();
        stageText.text=$"ЭТАП {quest.CurrentStep+1:00} / {quest.TotalSteps:00}";
        targetText.text=$"ЦЕЛЬ  {drive.TargetCurrentMin:0}–{drive.TargetCurrentMax:0} А   /   {drive.currentAmperes:0.0} А";
        progressText.text=$"{quest.CurrentHoldTimer:0.0} / {quest.ActiveStep?.holdSeconds:0} с";
        progressFill.rectTransform.sizeDelta=new Vector2(280*quest.HoldProgress,4);
        riskText.text=$"{drive.instability:0}%   {(drive.InstabilityRate>0?"РАСТЁТ":"БЕЗОПАСНО")}";
        riskText.color=drive.instability>=70?new Color(1f,0.35f,0.25f):drive.InstabilityRate>0?amber:mint;
        riskFill.color=riskText.color;riskFill.rectTransform.sizeDelta=new Vector2(270*drive.instability/100f,5);
        for(int i=0;i<4;i++)
        {
            float v=i==0?drive.pistonPressure:i==1?drive.mpsDepth:i==2?drive.mpsRPM:drive.mzsDepth;
            leverTexts[i].text=$"{i+1}  {names[i]}\n{v:0}"+(i==2?" RPM":i==0&&drive.pistonLocked?"%  / LOCK":"%");
            leverTexts[i].fontSize=17;leverCards[i].color=i==selected?new Color(0.12f,0.28f,0.26f):new Color(0.07f,0.12f,0.13f);
        }
        if(Time.unscaledTime>toastUntil)toastText.text="";
    }
    private void OnApplicationFocus(bool focus) { if(!focus&&started&&!IsMenuOpen&&!quest.IsTerminal)ShowPage("pause"); }
    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
    private void OnDestroy()
    {
        if(quest!=null){quest.OnStateChanged-=OnState;quest.OnStepChanged-=OnStep;}
        if(drive!=null)drive.Feedback-=Toast;
        Time.timeScale=1f;AudioListener.pause=false;ModalOpen=false;
    }
}

