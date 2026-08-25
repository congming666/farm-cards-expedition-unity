using UnityEngine;

// 战争迷雾（自定义 Shader）：已探索区域用遮罩RT持久保留(柔边)，由 Game/FogOfWar Shader 驱动呈现。
[RequireComponent(typeof(MeshRenderer))]
public class FogOfWar : MonoBehaviour
{
    public RenderTexture mask;
    public Material paintMat, fogMat;
    public float mapUnits = 60f;
    public float visibleRadius = 7f;
    float lastRevealX = -999, lastRevealZ = -999;
    public float revealThreshold = 0.6f;
    MeshRenderer mr;
    public void EnsureMesh(Mesh mesh){ var mf = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>(); mf.mesh = mesh ?? MeshFactory.Plane(); var mr2 = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>(); }
    public void Setup(float units){
        mapUnits = units;
        // 遮罩（红通道存已探索）
        mask = new RenderTexture(512,512,0,RenderTextureFormat.R8); mask.Create();
        ClearMask();
        paintMat = new Material(Shader.Find("Hidden/FogPaint"));
        fogMat = new Material(Shader.Find("Game/FogOfWar"));
        fogMat.SetTexture("_MaskTex", mask); fogMat.SetFloat("_MapSize", mapUnits); fogMat.SetFloat("_ExploredDim", 0.5f);
        // 迷雾面片：平铺在世界上方
        mr = GetComponent<MeshRenderer>();
        if(mr!=null){ mr.material = fogMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows=false; }
    }

    void ClearMask(){ var old=RenderTexture.active; RenderTexture.active=mask; GL.Clear(true,true,new Color(0,0,0,0)); RenderTexture.active=old; }

    // 每帧：若玩家移动则把已探索柔边圆"最大混合"进遮罩（持久保留）
    public void Reveal(Vector3 worldPos){
        if(mask==null||paintMat==null) return;
        float ux=worldPos.x/mapUnits, uz=worldPos.z/mapUnits;
        if(Mathf.Abs(worldPos.x-lastRevealX)<revealThreshold && Mathf.Abs(worldPos.z-lastRevealZ)<revealThreshold) return;
        lastRevealX=worldPos.x; lastRevealZ=worldPos.z;
        paintMat.SetVector("_Center",new Vector4(ux,uz,0,0));
        paintMat.SetFloat("_Radius", visibleRadius/mapUnits);
        paintMat.SetFloat("_Strength",1f);
        Graphics.Blit(null, mask, paintMat);
    }

    public void Tick(PlayerState p){
        if(fogMat==null) return;
        Reveal(new Vector3(p.x*0.025f,0,p.y*0.025f));
        fogMat.SetVector("_PlayerPos",new Vector4(p.x*0.025f,0,p.y*0.025f,0));
        fogMat.SetFloat("_VisibleRadius", visibleRadius);
    }

    void OnDestroy(){ if(mask!=null) mask.Release(); }
}
