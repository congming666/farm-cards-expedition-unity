using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ================= 游戏设置（分辨率 / 全屏 / 垂直同步 / 画质 / 帧率 / 音量 / 辅助开关） =================
// 数据用 JsonUtility 持久化到 settings.json；Apply() 把设置真正作用到引擎。
// 键位重绑定的数据结构已预留（KeyBind），实际输入映射在输入层增量接入，当前先提供默认表。
[Serializable]
public class KeyBind { public string action; public string display; public KeyBind(){} public KeyBind(string a,string d){ action=a; display=d; } }

[Serializable]
public class GameSettingsData
{
    public int version = 1;
    public int width = 1920;
    public int height = 1080;
    public int fullScreenMode = (int)FullScreenMode.FullScreenWindow; // 0独占 1全屏窗口 2最大化窗口 3窗口
    public bool vSync = true;
    public int qualityLevel = -1;     // -1 = 用工程默认画质
    public int targetFrameRate = 60;  // 0 / -1 = 不限制（开 vSync 时以显示器为准）
    [Range(0f,1f)] public float masterVolume = 0.9f;
    [Range(0f,1f)] public float musicVolume = 0.8f;
    [Range(0f,1f)] public float sfxVolume = 0.9f;
    public bool showDamageNumbers = true;
    public bool screenShake = true;
    public string language = "zh-CN";
    public List<KeyBind> keybinds = new List<KeyBind>();
}

public static class GameSettings
{
    static GameSettingsData _current;
    public static GameSettingsData Current { get { if (_current == null) Load(); return _current; } }

    static string FilePath { get { return Path.Combine(Application.persistentDataPath, "settings.json"); } }

    // 默认键位表（与 GameFlow.PollInput 当前硬编码保持一致，供设置界面展示 / 后续重绑定）
    static List<KeyBind> DefaultKeybinds()
    {
        return new List<KeyBind>{
            new KeyBind("move",      "W A S D / 方向键"),
            new KeyBind("sprint",    "Left Shift"),
            new KeyBind("attack",    "鼠标左键"),
            new KeyBind("pickup",    "鼠标右键"),
            new KeyBind("switchWeapon","Tab / 滚轮"),
            new KeyBind("skill1",    "1"), new KeyBind("skill2","2"),
            new KeyBind("skill3",    "3"), new KeyBind("skill4","4"),
            new KeyBind("item1",     "Q"), new KeyBind("item2","R"), new KeyBind("item3","E"),
            new KeyBind("pause",     "Esc"),
        };
    }

    public static void Load()
    {
        GameSettingsData d = null;
        string raw;
        if (AtomicFile.ReadAllTextRobust(FilePath, out raw))
        {
            try { d = JsonUtility.FromJson<GameSettingsData>(raw); }
            catch (Exception e) { Debug.LogWarning("[Settings] 解析失败，使用默认：" + e.Message); }
        }
        if (d == null) d = new GameSettingsData();
        Migrate(d);
        _current = d;
    }

    // 版本迁移占位：未来 settings 结构升级时在这里把旧版本补默认值
    static void Migrate(GameSettingsData d)
    {
        if (d.keybinds == null || d.keybinds.Count == 0) d.keybinds = DefaultKeybinds();
        d.width = Mathf.Max(640, d.width);
        d.height = Mathf.Max(480, d.height);
        d.masterVolume = Mathf.Clamp01(d.masterVolume);
        d.musicVolume = Mathf.Clamp01(d.musicVolume);
        d.sfxVolume = Mathf.Clamp01(d.sfxVolume);
    }

    public static void Save()
    {
        try { AtomicFile.WriteAllTextAtomic(FilePath, JsonUtility.ToJson(Current, true)); }
        catch (Exception e) { Debug.LogWarning("[Settings] 保存失败：" + e.Message); }
    }

    public static void ResetToDefault()
    {
        _current = new GameSettingsData();
        Migrate(_current);
        Apply();
        Save();
    }

    // 把当前设置作用到引擎（启动时、以及在设置界面点“应用”时调用）
    public static void Apply()
    {
        var d = Current;
        try
        {
            Application.runInBackground = true; // 桌面标配：切出窗口不暂停
            var mode = (FullScreenMode)d.fullScreenMode;
            // 窗口模式且分辨率异常时回退到当前桌面分辨率，避免开出不可见的小窗
            int w = d.width, h = d.height;
            if (mode == FullScreenMode.FullScreenWindow)
            {
                var main = Screen.mainWindowDisplayInfo;
                if (main.width > 0 && main.height > 0) { w = main.width; h = main.height; }
            }
            Screen.SetResolution(w, h, mode);
            QualitySettings.vSyncCount = d.vSync ? 1 : 0;
            Application.targetFrameRate = d.vSync ? -1 : (d.targetFrameRate > 0 ? d.targetFrameRate : -1);
            if (d.qualityLevel >= 0 && d.qualityLevel < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(d.qualityLevel, true);
            // 主音量（音乐 / 音效分通道由 AudioManager 读取 musicVolume/sfxVolume 增量接入）
            AudioListener.volume = d.masterVolume;
        }
        catch (Exception e) { Debug.LogWarning("[Settings] Apply 部分失败：" + e.Message); }
    }

    // 供下拉框使用的可选分辨率（去重、升序）
    public static List<Resolution> CommonResolutions()
    {
        var result = new List<Resolution>();
        var seen = new HashSet<string>();
        var all = Screen.resolutions;
        for (int i = 0; i < all.Length; i++)
        {
            var r = all[i];
            string key = r.width + "x" + r.height;
            if (seen.Contains(key)) continue;
            // 只保留主流 16:9/16:10 且不小于 1280x720
            if (r.width < 1280 || r.height < 720) continue;
            seen.Add(key); result.Add(r);
        }
        return result;
    }
}
