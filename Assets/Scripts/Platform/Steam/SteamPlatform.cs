using System.IO;
using UnityEngine;

// ================= Steam 平台封装（Steamworks.NET） =================
// 设计：用编译符号 STEAMWORKS 隔离真实 SDK。
//   - 未安装 Steamworks.NET 时：本文件全部是空实现(NoOp)，工程照常编译、照常出包；
//   - 接入步骤：把 Steamworks.NET 的 Steamworks.NET.dll 放进 Assets/Plugins/，
//     并在 Player Settings -> Scripting Define Symbols 加上 STEAMWORKS（或用带该符号的构建菜单）。
// 这样游戏业务代码只依赖本封装，不直接 using Steamworks，切换平台 / 去 SDK 都不用改玩法逻辑。
public static class SteamPlatform
{
    public static bool Available { get; private set; }
    public static string UserName { get { return Available ? GetUserNameInternal() : ""; } }

    // ---------- 生命周期 ----------
    public static void Init()
    {
#if STEAMWORKS
        try
        {
            if (!Steamworks.SteamAPI.Init())
            {
                Debug.LogWarning("[Steam] SteamAPI.Init 失败（非 Steam 启动会这样，开发环境可忽略）");
                Available = false;
                return;
            }
            Available = true;
            Debug.Log("[Steam] 已初始化，玩家：" + Steamworks.SteamFriends.GetPersonaName());
            // 通知 Steam 玩家正在游戏中（Rich Presence 基础状态）
            Steamworks.SteamFriends.SetRichPresence("steam_display", "#StatusFull");
        }
        catch (System.Exception e) { Debug.LogWarning("[Steam] 初始化异常：" + e.Message); Available = false; }
#else
        Available = false; // 未集成 SDK：安全空转
#endif
    }

    public static void Tick()
    {
#if STEAMWORKS
        if (Available) { try { Steamworks.SteamAPI.RunCallbacks(); } catch { } }
#endif
    }

    public static void Shutdown()
    {
#if STEAMWORKS
        if (Available) { try { Steamworks.SteamAPI.Shutdown(); } catch { } Available = false; }
#endif
    }

    // ---------- 成就 ----------
    public static void UnlockAchievement(string apiName)
    {
        if (string.IsNullOrEmpty(apiName)) return;
#if STEAMWORKS
        if (!Available) return;
        try
        {
            var stats = Steamworks.SteamUserStats;
            if (stats.GetAchievement(apiName, out bool already) && !already)
            {
                stats.SetAchievement(apiName);
                stats.StoreStats();
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[Steam] 成就失败 " + apiName + "：" + e.Message); }
#else
        Debug.Log("[Steam:NoOp] 解锁成就 " + apiName);
#endif
    }

    // ---------- 统计 ----------
    public static void SetStat(string apiName, int value)
    {
#if STEAMWORKS
        if (!Available) return;
        try { Steamworks.SteamUserStats.SetStat(apiName, value); Steamworks.SteamUserStats.StoreStats(); } catch { }
#endif
    }
    public static void SetStat(string apiName, float value)
    {
#if STEAMWORKS
        if (!Available) return;
        try { Steamworks.SteamUserStats.SetStat(apiName, value); Steamworks.SteamUserStats.StoreStats(); } catch { }
#endif
    }

    // ---------- Rich Presence（好友列表里显示“正在 T2 废弃小农庄远征”） ----------
    // key 常用 "currentmap"/"state"，steam_display 配合 Steamworks 后台本地化令牌。
    public static void SetRichPresence(string key, string value)
    {
#if STEAMWORKS
        if (!Available) return;
        try { Steamworks.SteamFriends.SetRichPresence(key, value); } catch { }
#endif
    }
    public static void SetPlayState(string friendlyText)
    {
#if STEAMWORKS
        if (!Available) return;
        try
        {
            Steamworks.SteamFriends.SetRichPresence("state", friendlyText);
            Steamworks.SteamFriends.SetRichPresence("steam_display", "#StatusFull");
        } catch { }
#endif
    }

    // ---------- 云存档 ----------
    // 方式：把本地存档文件写入 Steam Remote Storage，由 Steam 自动在多机间同步。
    public static bool CloudEnabled {
        get {
#if STEAMWORKS
            try { return Available && Steamworks.SteamRemoteStorage.IsCloudEnabledForApp(); } catch { return false; }
#else
            return false;
#endif
        }
    }

    public static void UploadFileToCloud(string localPath, string cloudName)
    {
#if STEAMWORKS
        if (!Available || !File.Exists(localPath)) return;
        try
        {
            byte[] bytes = File.ReadAllBytes(localPath);
            Steamworks.SteamRemoteStorage.FileWrite(cloudName, bytes, bytes.Length);
        }
        catch (System.Exception e) { Debug.LogWarning("[Steam] 云存档上传失败：" + e.Message); }
#endif
    }

    public static bool DownloadFileFromCloud(string cloudName, string localPath)
    {
#if STEAMWORKS
        if (!Available) return false;
        try
        {
            var rs = Steamworks.SteamRemoteStorage;
            if (!rs.FileExists(cloudName)) return false;
            int size = rs.GetFileSize(cloudName);
            if (size <= 0) return false;
            byte[] bytes = new byte[size];
            rs.FileRead(cloudName, bytes, size);
            AtomicFile.WriteAllTextAtomic(localPath, System.Text.Encoding.UTF8.GetString(bytes));
            return true;
        }
        catch (System.Exception e) { Debug.LogWarning("[Steam] 云存档下载失败：" + e.Message); return false; }
#else
        return false;
#endif
    }

    // 把本地全部存档槽同步上云（退出 / 切场景时调用）
    public static void SyncSavesToCloud()
    {
#if STEAMWORKS
        if (!Available || !CloudEnabled) return;
        try
        {
            for (int i = 0; i < SaveSystem.SLOT_COUNT; i++)
            {
                string local = SaveSystem.SlotPath(i);
                if (File.Exists(local)) UploadFileToCloud(local, "save_slot_" + i + ".json");
            }
        } catch { }
#endif
    }

#if STEAMWORKS
    static string GetUserNameInternal() { try { return Steamworks.SteamFriends.GetPersonaName(); } catch { return ""; } }
#else
    static string GetUserNameInternal() { return ""; }
#endif
}
