using UnityEngine;
// 2.5D 相机跟随：保持斜俯视角度，平滑跟随目标
public class GameCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 14f, -11f);
    public float smooth = 8f;
    public float tilt = 58f;
    void LateUpdate(){
        if(target==null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth*Time.deltaTime);
        transform.rotation = Quaternion.Euler(tilt, 0, 0);
    }
}
