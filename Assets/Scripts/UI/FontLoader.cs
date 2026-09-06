using UnityEngine;

// ================= 字体加载 =================
// 发布前把【可商用】的思源黑体(Source Han Sans / Noto Sans CJK)字体资产放到
// Assets/Resources/Fonts/SourceHanSans（.ttf 导入为 Font），这里会优先加载并随包分发。
// 找不到时回退到“系统动态字体”仅供开发期使用——微软雅黑/苹方的版权不属于你，禁止随商业包分发。
public static class FontLoader
{
    static Font _font;
    public static Font Main { get { if (_font == null) _font = Load(); return _font; } }

    static Font Load()
    {
        var bundled = Resources.Load<Font>("Fonts/SourceHanSans");
        if (bundled != null) return bundled;

        string[] preferred;
        switch (Application.systemLanguage)
        {
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional:
                preferred = new[] { "Microsoft YaHei", "SimHei", "PingFang SC", "Noto Sans CJK SC", "Arial Unicode MS", "Arial" };
                break;
            case SystemLanguage.Japanese:
                preferred = new[] { "Yu Gothic UI", "Hiragino Sans", "Noto Sans CJK JP", "Arial Unicode MS", "Arial" };
                break;
            default:
                preferred = new[] { "Segoe UI", "Arial" };
                break;
        }
        var os = Font.CreateDynamicFontFromOSFont(preferred, 32);
        if (os != null) return os;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 最终兜底（Arial 等价内置）
    }
}
