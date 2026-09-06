using UnityEngine;
using UnityEngine.UI;

// ================= 主菜单（UGUI 运行时版，替代 UIHost.DrawMenu 的 OnGUI） =================
// 文案 / 按钮行为与旧 IMGUI 主菜单一一对应；其余屏幕仍走 OnGUI，后续逐屏迁移。
// 由 UIHost.DrawUI 每帧调用 Sync(screen) 决定显隐，幂等。
public class MainMenuUI : MonoBehaviour
{
    static MainMenuUI _i;

    public static void Sync(string screen)
    {
        bool show = screen == "menu";
        if (show)
        {
            if (_i == null) Create();
            if (_i != null && !_i.gameObject.activeSelf) _i.gameObject.SetActive(true);
        }
        else if (_i != null && _i.gameObject.activeSelf)
        {
            _i.gameObject.SetActive(false);
        }
    }

    static void Create()
    {
        var root = UIRoot.Ensure();
        var go = UIFactory.Rect("MainMenu", root.Root).gameObject;
        _i = go.AddComponent<MainMenuUI>();
        _i.Build();
        go.SetActive(true);
    }

    void Build()
    {
        var rt = (RectTransform)transform;
        UIFactory.Stretch(rt);

        // 全屏背景（还原旧 menuBackdrop：顶部青墨绿 -> 底部蓝灰的竖向渐变）
        var bg = gameObject.AddComponent<Image>();
        bg.sprite = MakeBackdropSprite();
        bg.type = Image.Type.Simple;

        BuildCenter();
        BuildFooter();
        BuildSettingsButton();
    }

    // 居中文案与按钮
    void BuildCenter()
    {
        var content = UIFactory.Rect("Content", transform);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(1180, 920);
        content.anchoredPosition = Vector2.zero;
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16; vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;

        AddSpacer(content, 40);
        AddLine(content, 560);
        AddText(content, "🌾 农庄牌", 64, G.ParseColor("#7fff7f"), 96, 1180, FontStyle.Bold);
        AddText(content, "荒 野 远 征", 30, G.ParseColor("#aaccaa"), 48, 1180);
        AddLine(content, 560);
        AddText(content, "农场经营 × 卡牌构筑 × 搜打撤撤离", 22, G.ParseColor("#aaccaa"), 40, 1180);
        AddSpacer(content, 34);

        AddMenuButton(content, "🌱 开始游戏", 32, new Color(0.12f, 0.30f, 0.20f, 1f), 78, () =>
        {
            if (GameFlow.I != null) GameFlow.I.StartGame();
        });
        AddMenuButton(content, "📖 游戏说明", 26, new Color(0.16f, 0.22f, 0.30f, 1f), 66, () =>
        {
            if (GameFlow.I != null) GameFlow.I.AddToast("WASD移动，左键攻击/交互，1-4技能，Q/R/E消耗品", "success");
        });
        AddMenuButton(content, "🚪 退出游戏", 22, new Color(0.30f, 0.16f, 0.16f, 1f), 58, QuitGame);
        AddSpacer(content, 26);
        AddText(content, "✨ 物资仓库  ·  🏡 育种温室  ·  ⚔️ T1-T4远征  ·  🚁 双撤离机制", 20, G.ParseColor("#8ac8d8"), 40, 1180);
    }

    void BuildFooter()
    {
        var ver = UIFactory.Label(transform, "v1.9 · 农场卡牌 · 荒野远征", 16, G.ParseColor("#aeb8ae"), TextAnchor.MiddleCenter);
        var r = (RectTransform)ver.transform;
        r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0);
        r.sizeDelta = new Vector2(0, 34); r.anchoredPosition = new Vector2(0, 18);
    }

    // 桌面习惯：主菜单右上角给一个显式设置入口（F10 也可呼出）
    void BuildSettingsButton()
    {
        var btn = UIFactory.Button(transform, "⚙ 设置 (F10)", () => SettingsPanel.ToggleInstance(), 22,
            new Color(0.14f, 0.18f, 0.22f, 0.92f));
        var r = (RectTransform)btn.transform;
        r.anchorMin = r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(1, 1);
        r.sizeDelta = new Vector2(220, 54); r.anchoredPosition = new Vector2(-30, -28);
    }

    // ---------- 布局小工具 ----------
    void AddSpacer(Transform parent, float h)
    {
        var s = UIFactory.Rect("Spacer", parent);
        s.gameObject.AddComponent<LayoutElement>().preferredHeight = h;
    }
    void AddLine(Transform parent, float w)
    {
        var line = UIFactory.Panel(parent, "Line", G.ParseColor("#7fff7f")).rectTransform;
        var le = line.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.preferredHeight = 3;
    }
    void AddText(Transform parent, string text, int size, Color color, float h, float w, FontStyle style = FontStyle.Normal)
    {
        var t = UIFactory.Label(parent, text, size, color, TextAnchor.MiddleCenter);
        t.fontStyle = style;
        var le = t.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = w; le.preferredHeight = h;
    }
    void AddMenuButton(Transform parent, string text, int fontSize, Color bg, float h, System.Action onClick)
    {
        var b = UIFactory.Button(parent, text, onClick, fontSize, bg);
        var le = b.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 480; le.preferredHeight = h;
    }

    // 退出游戏：发布包直接退出进程；Unity 编辑器内则停止播放
    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 旧 UIHost.MakeBackdrop 的菜单版本（无田垄），竖向双色渐变 + 极轻中景微光
    static Sprite MakeBackdropSprite()
    {
        const int w = 8, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color top = new Color(0.035f, 0.10f, 0.10f);
        Color bottom = new Color(0.06f, 0.08f, 0.12f);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            Color c = Color.Lerp(bottom, top, v);
            for (int x = 0; x < w; x++) px[y * w + x] = c;
        }
        tex.SetPixels(px); tex.Apply(false);
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}
