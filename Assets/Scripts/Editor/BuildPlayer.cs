// ================= 发布构建脚本（编辑器） =================
// 菜单：
//   Build/Windows64 发布构建(IL2CPP)        —— 普通发行包
//   Build/Windows64 发布构建(Steam/STEAMWORKS) —— 带 Steam SDK 编译符号
//   Build/仅编译检查(不打包)               —— 快速验证全部脚本能否编译
// 也支持命令行：Unity -batchmode -quit -executeMethod BuildPlayer.ReleaseCLI [-steam]
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildPlayer
{
    const string ExeRelativePath = "Build/standalone/FarmCards.exe";
    const string CompanyName = "FarmCards Studio";
    const string ProductName = "FarmCards Expedition";
    const string AppIdentifier = "com.farmcards.expedition";

    [MenuItem("Build/Windows64 发布构建(IL2CPP)")]
    public static void ReleaseMenu() { BuildInternal(false); }

    [MenuItem("Build/Windows64 发布构建(Steam/STEAMWORKS)")]
    public static void ReleaseSteamMenu() { BuildInternal(true); }

    [MenuItem("Build/仅编译检查(不打包)")]
    public static void CompileCheckMenu()
    {
        EnsurePlayerSettings(false);
        Debug.Log("[Build] 脚本编译检查通过，未出包。");
    }

    // 命令行入口：参数里带 -steam 则启用 STEAMWORKS
    public static void ReleaseCLI()
    {
        bool steam = Environment.GetCommandLineArgs().Any(a => string.Equals(a, "-steam", StringComparison.OrdinalIgnoreCase));
        var report = BuildInternal(steam);
        // batchmode 下构建结果决定退出码，便于 CI 判定
        EditorApplication.Exit(report != null && report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static BuildReport BuildInternal(bool steam)
    {
        EnsurePlayerSettings(steam);

        // 场景：优先用 Build Settings 里勾选的场景；没有就回退到唯一样例场景
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        if (scenes.Count == 0)
        {
            string fallback = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(fallback)) scenes.Add(fallback);
            else { Debug.LogError("[Build] 找不到任何可构建场景"); return null; }
        }

        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "standalone"));
        Directory.CreateDirectory(outDir);
        string exePath = Path.Combine(outDir, "FarmCards.exe");

        var opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None // 显式非 Development：不含调试面板/日志
        };
        if (steam) opts.extraScriptingDefines = new[] { "STEAMWORKS" };

        Debug.Log("[Build] 开始构建 -> " + exePath + (steam ? " (STEAMWORKS)" : ""));
        var report = BuildPipeline.BuildPlayer(opts);

        PrintReport(report, outDir);
        return report;
    }

    static void EnsurePlayerSettings(bool steam)
    {
        EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneWindows64;

        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppIdentifier);

        // x86_64 架构（值 1 = Intel 64-bit）
        PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, (int)Architecture.X64);

        // IL2CPP：检测模块是否安装，缺失则回退 Mono 并明确告警
        if (IsIl2CppAvailable())
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Minimal);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone, Il2CppCompilerConfiguration.Master); // 发布最高优化
            Debug.Log("[Build] 脚本后端：IL2CPP / x86_64 / Master");
        }
        else
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Debug.LogWarning("[Build] 未检测到 Windows IL2CPP 模块，已回退 Mono。发布前请在 Unity Hub 安装 “Windows Build Support (IL2CPP)”。");
        }

        // 发行构建必须关闭 Development 与脚本调试
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;

        if (steam) Debug.Log("[Build] 本次启用编译符号：STEAMWORKS（需要 Assets/Plugins 下存在 Steamworks.NET.dll）");
    }

    static bool IsIl2CppAvailable()
    {
        try
        {
            string core = Path.Combine(EditorApplication.applicationContentsPath, "il2cpp");
            string variation = Path.Combine(EditorApplication.applicationContentsPath,
                "PlaybackEngines", "windowsstandalonesupport", "Variations", "win64_player_nondevelopment_il2cpp");
            return Directory.Exists(core) && Directory.Exists(variation);
        }
        catch { return false; }
    }

    static void PrintReport(BuildReport report, string outDir)
    {
        if (report == null) { Debug.LogError("[Build] 构建返回空报告"); return; }
        var s = report.summary;
        long bytes = 0; int files = 0;
        try
        {
            foreach (var f in new DirectoryInfo(outDir).GetFiles("*", SearchOption.AllDirectories)) { bytes += f.Length; files++; }
        }
        catch { }

        string[] lines =
        {
            "===== 构建报告 " + DateTime.Now + " =====",
            "结果: " + s.result,
            "输出目录: " + outDir,
            "总大小: " + (bytes / 1024.0 / 1024.0).ToString("F1") + " MB / 文件数 " + files,
            "耗时: " + (s.buildEndedAt - s.buildStartedAt).TotalSeconds.ToString("F1") + "s",
            "错误数: " + s.totalErrors + "  警告数: " + s.totalWarnings,
        };
        foreach (var l in lines) Debug.Log("[Build] " + l);
        try { File.WriteAllLines(Path.Combine(outDir, "..", "last-build-report.txt"), lines); } catch { }
    }

    // 避免直接写魔法数字，语义化架构值（Unity: 0=None/Universal, 1=x86_64, 2=ARM64）
    enum Architecture { X64 = 1, ARM64 = 2 }
}
