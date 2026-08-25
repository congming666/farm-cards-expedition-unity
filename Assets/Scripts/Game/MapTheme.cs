using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 阶段5：每张地图的主题（光照/雾/后处理/天空/地面/光柱）。主题化驱动，各图氛围独立。
public class Theme { public Color key, fog, bg, ambient, ground, shaft; public float intensity, density, bloom, bloomThresh, sat, contrast, vignette; public Vector3 rot; public string name; }
public static class MapTheme
{
    static Theme[] T;
    static void Init(){
        T = new Theme[4];
        // 1 春林：清晨绿意，暖光，绿雾
        T[0]=new Theme{ name="春林", key=new Color(1f,0.96f,0.86f), fog=new Color(0.40f,0.58f,0.42f), bg=new Color(0.55f,0.72f,0.55f), ambient=new Color(0.6f,0.65f,0.6f), ground=new Color(1.05f,1.05f,0.98f), shaft=new Color(0.9f,1f,0.7f), intensity=1.25f, density=0.009f, bloom=0.35f, bloomThresh=0.9f, sat=22f, contrast=10f, vignette=0.30f, rot=new Vector3(48f,-28f,0) };
        // 2 黄金城：金色黄昏，琥珀光，金雾
        T[1]=new Theme{ name="黄金城", key=new Color(1f,0.82f,0.55f), fog=new Color(0.55f,0.47f,0.30f), bg=new Color(0.55f,0.42f,0.24f), ambient=new Color(0.6f,0.5f,0.35f), ground=new Color(1.05f,1.0f,0.9f), shaft=new Color(1f,0.8f,0.4f), intensity=1.35f, density=0.012f, bloom=0.5f, bloomThresh=0.8f, sat=30f, contrast=12f, vignette=0.34f, rot=new Vector3(52f,-35f,0) };
        // 3 冰雪：冷蓝，冷光，蓝白雾
        T[2]=new Theme{ name="冰雪", key=new Color(0.72f,0.86f,1f), fog=new Color(0.62f,0.72f,0.86f), bg=new Color(0.68f,0.78f,0.9f), ambient=new Color(0.62f,0.68f,0.8f), ground=new Color(1.0f,1.0f,1.05f), shaft=new Color(0.7f,0.9f,1f), intensity=1.1f, density=0.010f, bloom=0.3f, bloomThresh=0.9f, sat=10f, contrast=10f, vignette=0.28f, rot=new Vector3(50f,-20f,0) };
        // 4 星空：夜晚紫蓝，冷月光，深蓝雾
        T[3]=new Theme{ name="星空", key=new Color(0.62f,0.7f,1f), fog=new Color(0.08f,0.1f,0.22f), bg=new Color(0.04f,0.05f,0.13f), ambient=new Color(0.16f,0.18f,0.3f), ground=new Color(1.0f,1.0f,1.1f), shaft=new Color(0.7f,0.6f,1f), intensity=0.7f, density=0.014f, bloom=0.45f, bloomThresh=0.75f, sat=8f, contrast=14f, vignette=0.42f, rot=new Vector3(45f,-10f,0) };
    }

    // 应用主题到光照/雾/后处理/背景
    public static void Apply(Light key, Volume vol, Camera cam, int tier){
        if(T==null) Init();
        int i=Mathf.Clamp(tier,1,4)-1; var th=T[i];
        if(key!=null){ key.color=th.key; key.intensity=th.intensity; key.transform.rotation=Quaternion.Euler(th.rot); }
        RenderSettings.fog=true; RenderSettings.fogColor=th.fog; RenderSettings.fogDensity=th.density;
        RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=th.ambient;
        if(cam!=null) cam.backgroundColor=th.bg;
        if(vol!=null && vol.profile!=null){
            Bloom b; if(vol.profile.TryGet<Bloom>(out b)){ b.intensity.value=th.bloom; b.threshold.value=th.bloomThresh; }
            ColorAdjustments ca; if(vol.profile.TryGet<ColorAdjustments>(out ca)){ ca.saturation.value=th.sat; ca.contrast.value=th.contrast; }
            Vignette vg; if(vol.profile.TryGet<Vignette>(out vg)){ vg.intensity.value=th.vignette; }
        }
    }
    public static Theme Get(int tier){ if(T==null) Init(); return T[Mathf.Clamp(tier,1,4)-1]; }
}
