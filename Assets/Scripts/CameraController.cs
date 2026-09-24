using UnityEngine;
public class CameraController : MonoBehaviour
{
    [SerializeField] private float horizontalAngle=50f,verticalAngle=30f,rotationSpeed=6f;
    [SerializeField] private float headBobIntensity=0.15f,headBobSmoothness=4f;
    public enum CameraPosition { Center,Left,Right,Up,Down }
    private CameraPosition currentPosition;
    private Vector3 baseEuler;
    private Vector2 smoothMouseDelta;
    private Quaternion targetRotation;
    public bool MotionEnabled { get; set; } = true;
    private void Awake() { baseEuler=transform.eulerAngles; targetRotation=transform.rotation; }
    private void Update()
    {
        if(Time.timeScale<=0f || ReactorUI.ModalOpen)return;
        if(Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow))LookLeft();
        if(Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow))LookRight();
        if(Input.GetKeyDown(KeyCode.W))LookUp();
        if(Input.GetKeyDown(KeyCode.S))LookDown();
        if(Input.GetKeyDown(KeyCode.Space))LookCenter();
        var delta=MotionEnabled?new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y")):Vector2.zero;
        smoothMouseDelta=Vector2.Lerp(smoothMouseDelta,Vector2.ClampMagnitude(delta,3f),1f-Mathf.Exp(-headBobSmoothness*Time.deltaTime));
        var offset=Quaternion.Euler(-smoothMouseDelta.y*headBobIntensity,smoothMouseDelta.x*headBobIntensity,0);
        transform.rotation=Quaternion.Slerp(transform.rotation,targetRotation*offset,1f-Mathf.Exp(-rotationSpeed*Time.deltaTime));
    }
    public void SetCameraPosition(CameraPosition position)
    {
        currentPosition=currentPosition==position&&position!=CameraPosition.Center?CameraPosition.Center:position;
        float yaw=currentPosition==CameraPosition.Left?-horizontalAngle:currentPosition==CameraPosition.Right?horizontalAngle:0f;
        float pitch=currentPosition==CameraPosition.Up?-verticalAngle:currentPosition==CameraPosition.Down?verticalAngle:0f;
        targetRotation=Quaternion.Euler(baseEuler.x+pitch,baseEuler.y+yaw,0f);
    }
    public void LookCenter()=>SetCameraPosition(CameraPosition.Center);
    public void LookLeft()=>SetCameraPosition(CameraPosition.Left);
    public void LookRight()=>SetCameraPosition(CameraPosition.Right);
    public void LookUp()=>SetCameraPosition(CameraPosition.Up);
    public void LookDown()=>SetCameraPosition(CameraPosition.Down);
    public CameraPosition GetCurrentPosition()=>currentPosition;
}

