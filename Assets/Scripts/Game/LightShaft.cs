using UnityEngine;

// 丁达尔体积光柱：加性混合，朝向相机并沿光向倾斜（配合 Bloom 发光）
public class LightShaft : MonoBehaviour
{
    public Material mat;
    public Vector3 lightDir = new Vector3(0.3f,-1f,0.2f);
    public float width = 2.2f, height = 16f;
    MeshRenderer mr;
    Transform target;

    public void Setup(Material m){ mat=m; }

    void Awake(){
        if(mat==null) mat = new Material(Shader.Find("Game/LightShaft"));
        var mf = gameObject.AddComponent<MeshFilter>(); mf.mesh = MeshFactory.Quad();
        mr = gameObject.AddComponent<MeshRenderer>(); mr.material = mat; mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows=false;
    }

    void LateUpdate(){
        var cam = Camera.main; if(cam==null) return;
        // 围绕竖直轴朝向相机（billboard），再沿光向倾斜
        Vector3 toCam = cam.transform.position - transform.position; toCam.y=0; if(toCam.sqrMagnitude<0.01f) toCam=Vector3.forward; toCam.Normalize();
        Vector3 fwd = Vector3.Lerp(toCam, -lightDir, 0.35f).normalized;
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}
