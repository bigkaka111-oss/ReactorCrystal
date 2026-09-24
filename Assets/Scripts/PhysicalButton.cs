using System.Collections;
using UnityEngine;
using UnityEngine.Events;
public class PhysicalButton : MonoBehaviour
{
    [SerializeField] private UnityEvent onPressed = new UnityEvent();
    [SerializeField] private float pressDepth = 0.004f, pressReturnSpeed = 0.08f;
    [SerializeField] private Color hoverColor = new Color(0.35f,1f,0.8f);
    [SerializeField] private Color normalColor = Color.white;
    private Vector3 originalLocalPosition;
    private Renderer buttonRenderer;
    private MaterialPropertyBlock properties;
    private string colorProperty;
    private bool isAnimating;
    private CrystalDrive drive;
    private void Awake()
    {
        originalLocalPosition=transform.localPosition; buttonRenderer=GetComponentInChildren<Renderer>();
        properties=new MaterialPropertyBlock(); drive=FindFirstObjectByType<CrystalDrive>();
        if(buttonRenderer!=null && buttonRenderer.sharedMaterial!=null)
        {
            colorProperty=buttonRenderer.sharedMaterial.HasProperty("_BaseColor")?"_BaseColor":"_Color";
            if(buttonRenderer.sharedMaterial.HasProperty(colorProperty)) normalColor=buttonRenderer.sharedMaterial.GetColor(colorProperty);
        }
    }
    private void OnMouseDown() { if(!WorldInteraction.Blocked) OnButtonPressed(); }
    private void OnMouseEnter() { if(!WorldInteraction.Blocked) OnButtonHover(); }
    private void OnMouseExit() => OnButtonExit();
    public void OnButtonPressed()
    {
        if(isAnimating || Time.timeScale<=0f || (drive!=null&&!drive.ControlsEnabled)) return;
        StartCoroutine(PressAnimation()); onPressed?.Invoke();
    }
    public void OnButtonHover() => SetColor(hoverColor);
    public void OnButtonExit() => SetColor(normalColor);
    private void SetColor(Color c)
    {
        if(buttonRenderer==null||properties==null||colorProperty==null)return;
        buttonRenderer.GetPropertyBlock(properties); properties.SetColor(colorProperty,c); buttonRenderer.SetPropertyBlock(properties);
    }
    private IEnumerator PressAnimation()
    {
        isAnimating=true; transform.localPosition=originalLocalPosition+Vector3.down*pressDepth;
        float elapsed=0f;
        while(elapsed<pressReturnSpeed*2f)
        {
            elapsed+=Time.deltaTime;
            transform.localPosition=Vector3.Lerp(originalLocalPosition+Vector3.down*pressDepth,originalLocalPosition,Mathf.Clamp01(elapsed/Mathf.Max(0.01f,pressReturnSpeed*2f)));
            yield return null;
        }
        transform.localPosition=originalLocalPosition; isAnimating=false;
    }
    private void OnDisable() { StopAllCoroutines(); transform.localPosition=originalLocalPosition; isAnimating=false; OnButtonExit(); }
}

