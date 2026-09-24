using UnityEngine;
public class ManualPage : MonoBehaviour
{
    public Camera mainCamera;
    public Collider pageCollider;
    public Vector3 restPosition,restRotation,viewPosition,viewRotation;
    public float speed=10f;
    private bool isHeld;
    private Vector3 cameraOffset;
    private Quaternion cameraRotation;
    private void Start()
    {
        if(mainCamera==null)mainCamera=Camera.main;
        transform.SetPositionAndRotation(restPosition,Quaternion.Euler(restRotation));
        if(mainCamera!=null)
        {
            cameraOffset=mainCamera.transform.InverseTransformPoint(viewPosition);
            cameraRotation=Quaternion.Inverse(mainCamera.transform.rotation)*Quaternion.Euler(viewRotation);
        }
    }
    private void Update()
    {
        if(Time.timeScale<=0f || !Input.GetMouseButton(0))isHeld=false;
        if(!WorldInteraction.Blocked&&Input.GetMouseButtonDown(0)&&mainCamera!=null&&pageCollider!=null)
        {
            if(Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition),out var hit)&&hit.collider==pageCollider)isHeld=true;
        }
        var pos=isHeld&&mainCamera!=null?mainCamera.transform.TransformPoint(cameraOffset):restPosition;
        var rot=isHeld&&mainCamera!=null?mainCamera.transform.rotation*cameraRotation:Quaternion.Euler(restRotation);
        float t=1f-Mathf.Exp(-speed*Time.unscaledDeltaTime);
        transform.position=Vector3.Lerp(transform.position,pos,t);
        transform.rotation=Quaternion.Slerp(transform.rotation,rot,t);
    }
    private void OnApplicationFocus(bool focus) { if(!focus)isHeld=false; }
}

