using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================= 远征 HUD（UGUI 运行时版，替代 UIHost.DrawHUD 的 OnGUI） =================
// 透明叠加在 3D 世界之上（不铺背景）；旧 1280x720 坐标由 UIFactory.Place ×1.5 映射；
// 血/能量条、计时、技能冷却、消耗品数量、任务/兽潮/事件每帧刷新；纯展示无按钮，逻辑层不改。
public class ExpeditionHUD : MonoBehaviour
{
    static ExpeditionHUD _i;

    RectTransform _hpFill, _enFill;
    Text _hpText, _enText, _timer, _mapInfo, _weapon;
    Text[] _skill = new Text[4];
    Image[] _skillBox = new Image[4];
    class Consume { public Text lab; public string id; }
    readonly List<Consume> _consumes = new List<Consume>();
    Text _bag, _objective, _progress, _beast, _evt;
    GameObject _pauseLayer;
    RawImage _mm;

    public static void Sync(string screen)
    {
        bool show = screen == "expedition";
        if (show)
        {
            if (_i == null) Create();
            if (_i != null && !_i.gameObject.activeSelf) _i.gameObject.SetActive(true);
        }
        else if (_i != null && _i.gameObject.activeSelf) _i.gameObject.SetActive(false);
    }

    static void Create()
    {
        var root = UIRoot.Ensure();
        var rt = UIFactory.Rect("ExpeditionHUD", root.Root);
        UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<ExpeditionHUD>();
        _i.Build();
    }

    Text Lab(float x, float y, float w, float h, string text, int legacySize, Color c, TextAnchor a = TextAnchor.MiddleLeft)
    {
        return UIFactory.PlacedLabel(transform, text, legacySize, c, x, y, w, h, a);
    }
    Image Panel(float x, float y, float w, float h, Color c, string name = "Panel")
    {
        // HUD 纯展示：一律不吃射线，避免挡住 3D 世界的鼠标攻击/交互
        var img = UIFactory.PlacedPanel(transform, name, x, y, w, h, c);
        img.raycastTarget = false;
        return img;
    }

    // 横条：暗槽 + 彩色填充 + 居中文本，返回填充 RectTransform 与文本
    void Bar(float x, float y, float w, float h, Color fill, out RectTransform fillRt, out Text txt)
    {
        var bg = Panel(x, y, w, h, new Color(0.13f, 0.13f, 0.13f, 1f), "BarBg"); bg.raycastTarget = false;
        var frt = UIFactory.Rect("BarFill", transform);
        var fimg = frt.gameObject.AddComponent<Image>(); fimg.color = fill; fimg.raycastTarget = false;
        fillRt = frt;
        txt = Lab(x, y, w, h, "", h <= 12 ? 11 : 12, Color.white, TextAnchor.MiddleCenter);
    }

    void Build()
    {
        // 小地图：纹理由 GameFlow 以 10Hz 生成上传，UGUI 只负责显示（右下角，旧 160px → 240px）
        var mmRt = UIFactory.Rect("Minimap", transform);
        mmRt.anchorMin = mmRt.anchorMax = new Vector2(1f, 0f); mmRt.pivot = new Vector2(1f, 0f);
        mmRt.sizeDelta = new Vector2(240f, 240f); mmRt.anchoredPosition = new Vector2(-18f, 18f);
        _mm = mmRt.gameObject.AddComponent<RawImage>(); _mm.raycastTarget = false;

        // 左上：生命 / 能量
        Panel(12, 10, 220, 66, new Color(0f, 0f, 0f, 0.5f), "Vitals");
        Bar(20, 40, 180, 14, G.ParseColor("#ff6666"), out _hpFill, out _hpText);
        Bar(20, 58, 180, 12, G.ParseColor("#66aaff"), out _enFill, out _enText);

        // 顶部居中：计时 / 地图
        _timer = Lab(540, 10, 200, 40, "", 30, Color.white, TextAnchor.MiddleCenter);
        _mapInfo = Lab(540, 52, 200, 20, "", 12, G.ParseColor("#aeb8ae"), TextAnchor.MiddleCenter);
        // 武器
        _weapon = Lab(320, 10, 220, 30, "", 17, Color.white);

        // 底部：4 技能格
        for (int i = 0; i < 4; i++)
        {
            float x = 320 + i * 60;
            var box = Panel(x, 640, 52, 52, new Color(0f, 0f, 0f, 0.55f), "SkillBox"); box.raycastTarget = false;
            _skillBox[i] = box;
            _skill[i] = Lab(x, 640, 52, 52, "", 16, Color.white, TextAnchor.MiddleCenter);
        }
        // 底部：消耗品
        int qi = 0;
        foreach (var item in GameData.Consumables)
        {
            float x = 560 + qi * 60;
            var box = Panel(x, 640, 52, 52, new Color(0f, 0f, 0f, 0.55f), "ConsumeBox"); box.raycastTarget = false;
            var t = Lab(x, 640, 52, 52, "", 14, G.ParseColor("#ffd700"), TextAnchor.MiddleCenter);
            _consumes.Add(new Consume { lab = t, id = item.id });
            qi++;
        }

        // 右上：背包统计
        Panel(1120, 10, 140, 72, new Color(0f, 0f, 0f, 0.5f), "Bag");
        _bag = Lab(1128, 18, 124, 46, "", 15, Color.white);

        // 左中：任务 / 兽潮 / 事件
        Panel(12, 90, 230, 96, new Color(0f, 0f, 0f, 0.5f), "Objective");
        _objective = Lab(20, 96, 214, 20, "", 14, Color.white);
        _progress = Lab(20, 118, 214, 18, "", 12, G.ParseColor("#aeb8ae"));
        _beast = Lab(20, 150, 214, 20, "", 12, G.ParseColor("#f2d078"));
        _evt = Lab(20, 174, 214, 16, "", 12, G.ParseColor("#aeb8ae"));

        // 暂停遮罩
        _pauseLayer = UIFactory.PlacedPanel(transform, "Pause", 0, 0, 1280, 720, new Color(0f, 0f, 0f, 0.72f)).gameObject;
        Lab(0, 270, 1280, 70, "游戏已暂停", 42, Color.white, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
        Lab(0, 342, 1280, 36, "按 Esc 继续游戏", 18, G.ParseColor("#aaccaa"), TextAnchor.MiddleCenter);
        _pauseLayer.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;
        var gf = GameFlow.I;
        if (gf == null || gf.screen != "expedition" || gf.current == null) return;
        Refresh(gf.current);
    }

    void Refresh(Expedition e)
    {
        var p = e.player;
        float hpPct = G.Clamp(p.hp / p.maxHp, 0, 1);
        float enPct = G.Clamp(p.energy / p.maxEnergy, 0, 1);
        UIFactory.Place(_hpFill, 20, 40, 180 * hpPct, 14);
        UIFactory.Place(_enFill, 20, 58, 180 * enPct, 12);
        Set(_hpText, "生命 " + Mathf.CeilToInt(p.hp) + "/" + Mathf.CeilToInt(p.maxHp));
        Set(_enText, "能量 " + Mathf.CeilToInt(p.energy) + "/" + Mathf.CeilToInt(p.maxEnergy));

        int mins = (int)Mathf.Floor(e.timeLeft / 60f), secs = (int)Mathf.Floor(e.timeLeft % 60f);
        Set(_timer, mins + ":" + secs.ToString("00"));
        Set(_mapInfo, "T" + e.map.tier + " · " + e.map.name + " · " + e.map.danger);
        Set(_weapon, e.weapon.icon + " " + e.weapon.name + "  [Tab]");

        for (int i = 0; i < 4; i++)
        {
            var def = GameData.Skills[i];
            var sk = SkillMath.GetStats(def, e.skillBoosts.ContainsKey(def.id) ? e.skillBoosts[def.id] : 0);
            float cd = e.skillCooldowns[i];
            Set(_skill[i], sk.def.icon + "\n" + sk.def.key + (cd > 0 ? ("\n" + cd.ToString("F0")) : ""));
            _skill[i].color = cd <= 0 ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        }
        foreach (var c in _consumes)
        {
            int cnt = e.consumables.ContainsKey(c.id) ? e.consumables[c.id] : 0;
            var def = ConsumableById(c.id);
            Set(c.lab, (def != null ? def.icon : "?") + "\n" + (def != null ? def.key : "") + " ×" + cnt);
            c.lab.color = cnt > 0 ? G.ParseColor("#ffd700") : new Color(0.4f, 0.4f, 0.4f, 1f);
        }

        float gg = 0; int ss = 0;
        foreach (var it in e.bag) { if (it.type == "gold") gg += it.amount; if (it.type == "seed") ss += (int)it.amount; }
        Set(_bag, "金币 " + gg + "\n种子 " + ss);

        if (e.objective != null)
        {
            Set(_objective, e.objective.title + (e.objective.complete ? " · 已完成" : ""));
            Set(_progress, e.objective.progress + "/" + e.objective.target);
        }
        else { Set(_objective, ""); Set(_progress, ""); }
        Set(_beast, e.beastWave.active
            ? ("⚠ 第 " + e.beastWave.wave + " 波兽潮 剩余 " + e.beastWave.remaining)
            : ("兽潮预警 " + Mathf.CeilToInt(e.beastWave.nextIn) + "s"));
        _beast.color = e.beastWave.active ? G.ParseColor("#ff6644") : G.ParseColor("#f2d078");
        Set(_evt, e.activeEvent != null ? ("事件：" + e.activeEvent.name + " · " + Mathf.CeilToInt(e.activeEvent.timeLeft) + "s") : "区域平静");

        if (_pauseLayer.activeSelf != e.paused) _pauseLayer.SetActive(e.paused);
        if (_mm != null && GameFlow.I != null) _mm.texture = GameFlow.I.mmTex;
    }

    static ConsumableDef ConsumableById(string id)
    {
        foreach (var c in GameData.Consumables) if (c.id == id) return c;
        return null;
    }

    static void Set(Text t, string s) { if (t != null && t.text != s) t.text = s; }
}
