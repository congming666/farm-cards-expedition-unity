using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 阶段1：搭建 2.5D 斜俯视相机、主光源、URP 全局体积（雾/Bloom/调色/暗角）
public class GameSceneBoot
{
    public Camera Cam;
    public Light Key;
    public Volume Volume;

    public void Build(float orthoSize = 9f){
        // ---- 主相机（2.5D 斜俯视，正交，跟随角色） ----
        var camGo = Camera.main?.gameObject ?? new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Cam = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
        Cam.orthographic = true; Cam.orthographicSize = orthoSize; Cam.clearFlags = CameraClearFlags.SolidColor;
        Cam.nearClipPlane = 0.3f; Cam.farClipPlane = 200f;
        // 轻微俯角：稍微压低相机使地面呈一点透视感（2.5D）
        Cam.transform.rotation = Quaternion.Euler(58f, 0, 0);
        // URP 相机数据（设置抗锯齿 + 后处理）
        var ud = camGo.GetComponent<UniversalAdditionalCameraData>() ?? camGo.AddComponent<UniversalAdditionalCameraData>();
        ud.renderPostProcessing = true; ud.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        // ---- 主光源 ----
        var lightGo = new GameObject("Key Light");
        Key = lightGo.AddComponent<Light>(); Key.type = LightType.Directional;
        Key.color = new Color(1f,0.95f,0.85f); Key.intensity = 1.25f;
        Key.transform.rotation = Quaternion.Euler(50f,-30f,0);
        var ldata = lightGo.GetComponent<UniversalAdditionalLightData>() ?? lightGo.AddComponent<UniversalAdditionalLightData>();
        // 阴影
        ldata.softShadowQuality = SoftShadowQuality.High;
        // ---- 全局体积：雾/Bloom/调色/暗角 ----
        volumeGo = new GameObject("Global Volume");
        Volume = volumeGo.AddComponent<Volume>();
        Volume.isGlobal = true;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        Volume.profile = profile;
        var bloom = profile.Add<Bloom>(true); bloom.intensity.value = 0.45f; bloom.threshold.value = 0.85f; bloom.scatter.value = 0.7f;
        var ca = profile.Add<ColorAdjustments>(true); ca.contrast.value = 12f; ca.saturation.value = 26f;
        var vig = profile.Add<Vignette>(true); vig.intensity.value = 0.32f; vig.smoothness.value = 0.45f;
        // URP 场景雾（RenderSettings），供体积雾回退
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = 0.012f;
    }
    GameObject volumeGo;
}
