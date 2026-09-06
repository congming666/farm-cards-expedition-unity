using System;
using System.IO;
using System.Text;
using UnityEngine;

// ================= 崩溃 / 异常日志 =================
// 挂钩 Unity 日志回调与托管层未处理异常，把 Error/Exception 落盘到 persistentDataPath/logs，
// 便于 Steam 玩家回传问题定位（替代“闪退但没有任何线索”）。
public static class CrashLogger
{
    static string _logPath;
    static readonly StringBuilder _buffer = new StringBuilder();
    static bool _started;
    const int MAX_FLUSH_CHARS = 1 << 20; // 单局日志最多缓存 ~1MB，避免异常刷屏刷爆磁盘

    public static string LogDir { get { return Path.Combine(Application.persistentDataPath, "logs"); } }

    public static void Start()
    {
        if (_started) return;
        _started = true;
        try
        {
            if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
            _logPath = Path.Combine(LogDir, "session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log");
            WriteHeader();
            Application.logMessageReceivedThreaded += OnUnityLog;
            AppDomain.CurrentDomain.UnhandledException += OnDomainException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTask;
            Application.quitting += Flush;
        }
        catch (Exception e) { Debug.LogWarning("[CrashLogger] 初始化失败：" + e.Message); }
    }

    static void WriteHeader()
    {
        Append("===== 会话开始 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " =====");
        Append("Unity " + Application.unityVersion + " | 游戏版本 " + Application.version);
        Append("OS: " + SystemInfo.operatingSystem + " | 设备 " + SystemInfo.deviceModel);
        Append("CPU " + SystemInfo.processorType + " | 逻辑核心 " + SystemInfo.processorCount + " | 内存 " + SystemInfo.systemMemorySize + "MB");
        Append("GPU " + SystemInfo.graphicsDeviceName + " | 显存 " + SystemInfo.graphicsMemorySize + "MB | API " + SystemInfo.graphicsDeviceType + " | ShaderModel " + SystemInfo.graphicsShaderLevel);
        Append("分辨率 " + Screen.currentResolution + " | 全屏 " + Screen.fullScreenMode);
    }

    // 后台线程也可能触发，因此这里只用线程安全的 StringBuilder + 轻量落盘
    static void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert && type != LogType.Warning) return;
        Append("[" + type + "] " + condition);
        if (type == LogType.Exception && !string.IsNullOrEmpty(stackTrace)) Append(stackTrace);
    }

    static void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        Append("[FATAL] 未处理异常 (终止=" + e.IsTerminating + ")：" + e.ExceptionObject);
        Flush();
    }

    static void OnUnobservedTask(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        Append("[TASK] 未观察任务异常：" + e.Exception);
    }

    static void Append(string line)
    {
        lock (_buffer)
        {
            if (_buffer.Length > MAX_FLUSH_CHARS) { _buffer.Clear(); _buffer.AppendLine("(日志过长已截断)"); }
            _buffer.AppendLine(line);
        }
    }

    public static void Flush()
    {
        try
        {
            if (string.IsNullOrEmpty(_logPath)) return;
            string text;
            lock (_buffer) { if (_buffer.Length == 0) return; text = _buffer.ToString(); _buffer.Clear(); }
            File.AppendAllText(_logPath, text, Encoding.UTF8);
        }
        catch { /* 日志失败绝不能反过来影响游戏 */ }
    }
}
