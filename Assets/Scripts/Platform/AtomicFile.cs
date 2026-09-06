using System;
using System.IO;
using System.Text;
using UnityEngine;

// ================= 原子文件读写（存档 / 设置共用） =================
// 目标：写入过程中崩溃/断电也不会产生半截损坏文件；旧文件自动留 .bak 备份。
// 流程：先写 *.tmp -> 用 tmp 覆盖正式文件（覆盖前把旧文件复制成 *.bak）。
public static class AtomicFile
{
    static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    // 原子写入：成功返回 true。任何 IO 异常都向上抛，由调用方决定是否吞掉并提示。
    public static void WriteAllTextAtomic(string path, string content)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("path empty");
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string tmp = path + ".tmp";
        File.WriteAllText(tmp, content, Utf8NoBom);

        if (File.Exists(path))
        {
            string bak = path + ".bak";
            try { File.Copy(path, bak, true); }   // 保留上一版可读备份
            catch (Exception e) { Debug.LogWarning("[AtomicFile] 备份失败(可继续)：" + e.Message); }
        }
        // File.Copy 覆盖在各平台 / Mono 下行为最一致，避免依赖 File.Move 的重载差异
        File.Copy(tmp, path, true);
        try { File.Delete(tmp); } catch { }
    }

    // 读取：正式文件损坏时自动回退 .bak；都失败返回 false。
    public static bool ReadAllTextRobust(string path, out string content)
    {
        content = null;
        if (TryRead(path, out content)) return true;
        string bak = path + ".bak";
        if (File.Exists(bak) && TryRead(bak, out content))
        {
            Debug.LogWarning("[AtomicFile] 主文件读取失败，已回退到备份：" + bak);
            return true;
        }
        return false;
    }

    static bool TryRead(string path, out string content)
    {
        content = null;
        try
        {
            if (!File.Exists(path)) return false;
            string raw = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(raw)) return false;
            content = raw;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AtomicFile] 读取失败 " + path + "：" + e.Message);
            return false;
        }
    }
}
