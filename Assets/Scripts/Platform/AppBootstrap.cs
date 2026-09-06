using UnityEngine;

// ================= 应用启动引导（最早执行，无需场景里挂对象） =================
// BeforeSceneLoad 阶段：崩溃日志 -> 读取并应用设置 -> 初始化 Steam；并挂一个常驻驱动组件。
public static class AppBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        CrashLogger.Start();
        GameSettings.Load();
        GameSettings.Apply();
        SteamPlatform.Init();

        var go = new GameObject("[AppDriver]");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AppDriver>();
    }
}

// 常驻：每帧驱动 Steam 回调、定期落盘日志；退出时统一保存 + 云同步 + 释放。
public class AppDriver : MonoBehaviour
{
    float _logFlushTimer;
    void Update()
    {
        SteamPlatform.Tick();
        _logFlushTimer += Time.unscaledDeltaTime;
        if (_logFlushTimer >= 5f) { _logFlushTimer = 0f; CrashLogger.Flush(); }
    }

    // 失焦时也落一次盘，防止切窗口后强杀进程丢日志/存档
    void OnApplicationPause(bool paused) { if (paused) SafePersist(); }
    void OnApplicationFocus(bool hasFocus) { if (!hasFocus) SafePersist(); }

    void OnApplicationQuit()
    {
        SafePersist();
        SteamPlatform.Shutdown();
        CrashLogger.Flush();
    }

    void SafePersist()
    {
        try { SaveSystem.Save(); } catch (System.Exception e) { Debug.LogWarning("[AppDriver] 退出保存失败：" + e.Message); }
        try { GameSettings.Save(); } catch { }
        try { SteamPlatform.SyncSavesToCloud(); } catch { }
        CrashLogger.Flush();
    }
}
