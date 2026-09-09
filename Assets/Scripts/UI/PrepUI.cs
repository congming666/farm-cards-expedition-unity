using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================= 远征准备大厅（UGUI 运行时版，替代 UIHost.DrawPrep 的 OnGUI） =================
// 旧 1280x720 坐标由 UIFactory.Place ×1.5 映射；控件建一次、动态值每帧刷新；
// 地图选择/消耗品配载/强化卡装备/出发的行为与旧 DrawPrep 完全一致，逻辑层不改。
public class PrepUI : MonoBehaviour
{
    static PrepUI _i;

    Text _gold, _cardPanelTitle, _cardHint, _areaTip;

    class MapRow { public string id; public Text lab; public Button btn; }
    readonly List<MapRow> _maps = new List<MapRow>();

    class SkillRow { public Text name, info; }
    readonly List<SkillRow> _skills = new List<SkillRow>();

    class ConsumeRow { public string id; public Text lab; }
    readonly List<ConsumeRow> _consumes = new List<ConsumeRow>();

    RectTransform _cardHost;
    string _cardSig = "";
    readonly List<Button> _cardBtns = new List<Button>();
    readonly List<Text> _cardLabs = new List<Text>();
    readonly List<string> _cardIds = new List<string>();

    public static void Sync(string screen)
    {
        bool show = screen == "prep";
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
        var rt = UIFactory.Rect("PrepUI", root.Root);
        UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<PrepUI>();
        _i.Build();
    }

    Button Btn(float x, float y, float w, float h, string text, int legacySize, Action onClick, Color? bg = null, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var b = UIFactory.Button(transform, text, onClick, Mathf.RoundToInt(legacySize * 1.5f), bg);
        UIFactory.Place((RectTransform)b.transform, x, y, w, h);
        var t = b.GetComponentInChildren<Text>();
        t.alignment = align;
        if (align == TextAnchor.MiddleLeft)
        {
            var lrt = (RectTransform)t.transform;
            lrt.offsetMin = new Vector2(18, 0); lrt.offsetMax = new Vector2(-10, 0);
        }
        return b;
    }
    Text Lab(float x, float y, float w, float h, string text, int legacySize, Color c, TextAnchor a = TextAnchor.MiddleLeft)
    {
        return UIFactory.PlacedLabel(transform, text, legacySize, c, x, y, w, h, a);
    }
    Image Panel(float x, float y, float w, float h, Color c, string name = "Panel")
    {
        return UIFactory.PlacedPanel(transform, name, x, y, w, h, c);
    }
    Text TitledPanel(float x, float y, float w, float h, string title)
    {
        Panel(x, y, w, h, new Color(0f, 0f, 0f, 0.5f), "Panel");
        return Lab(x + 12, y + 8, w - 24, 26, title, 16, G.ParseColor("#f0d58f"));
    }

    void Build()
    {
        // 全屏背景（从 OnGUI 迁移，避免 IMGUI 层遮住 UGUI 控件）
        var bg = gameObject.AddComponent<Image>();
        bg.sprite = Sprite.Create(UIHost.menuBackdrop, new Rect(0,0,UIHost.menuBackdrop.width,UIHost.menuBackdrop.height), new Vector2(0.5f,0.5f), 100f);
        bg.type = Image.Type.Simple; bg.raycastTarget = false;
        // ---------- 顶部 ----------
        Panel(0, 0, 1280, 64, new Color(0.025f, 0.105f, 0.075f, 0.94f), "TopBar");
        Lab(34, 14, 470, 36, "荒野远征准备大厅", 26, Color.white);
        _gold = MakeGoldBadge(980, 120);
        Btn(1132, 13, 120, 38, "返回家园", 18, () => { if (GameFlow.I != null) GameFlow.I.ClosePrep(); },
            new Color(0.12f, 0.25f, 0.18f, 0.98f));

        // ---------- 左上：选择远征区域 ----------
        TitledPanel(28, 80, 374, 286, "选择远征区域");
        float yy = 104;
        foreach (var mp in GameData.Maps)
        {
            string mid = mp.id;
            string txt = "T" + mp.tier + "  " + mp.name + "  [" + mp.danger + "]\n入场 " + mp.entryFee + " 金 · 怪物 " + mp.monsterCount + " · 宝箱 " + mp.chestCount;
            var b = Btn(38, yy, 354, 54, txt, 14, () =>
            {
                var m = SaveSystem.MapById(mid) ?? FindMap(mid);
                if (m != null && !(GameState.gold < m.entryFee)) { GameState.selectedMap = mid; SaveSystem.Save(); }
            }, new Color(0.13f, 0.20f, 0.16f, 0.98f), TextAnchor.MiddleLeft);
            _maps.Add(new MapRow { id = mid, btn = b, lab = b.GetComponentInChildren<Text>() });
            yy += 60;
        }

        // ---------- 左下：本次常驻技能（纯展示，每帧随强化卡刷新） ----------
        TitledPanel(28, 378, 374, 314, "本次常驻技能");
        float sy = 410;
        foreach (var sk in GameData.Skills)
        {
            var name = Lab(42, sy, 340, 40, "", 15, Color.white);
            var info = Lab(42, sy + 20, 340, 18, "", 12, G.ParseColor("#aeb8ae"));
            _skills.Add(new SkillRow { name = name, info = info });
            sy += 46;
        }

        // ---------- 右上：携带消耗品 ----------
        TitledPanel(420, 80, 410, 138, "携带消耗品");
        float cxx = 428;
        foreach (var item in GameData.Consumables)
        {
            string cid = item.id;
            var b = Btn(cxx, 104, 124, 64, "", 13, () => ToggleConsumable(cid),
                new Color(0.14f, 0.18f, 0.24f, 0.98f));
            _consumes.Add(new ConsumeRow { id = cid, lab = b.GetComponentInChildren<Text>() });
            cxx += 132;
        }

        // ---------- 中：携带强化卡（卡牌数量会变化，按签名重建） ----------
        _cardPanelTitle = TitledPanel(420, 230, 410, 220, "携带强化卡  0/3");
        _cardHint = Lab(432, 250, 376, 20, "", 12, G.ParseColor("#aeb8ae"));
        _cardHost = UIFactory.Rect("CardHost", transform);

        // ---------- 区域提示 + 出发 ----------
        TitledPanel(420, 462, 410, 118, "区域提示");
        _areaTip = Lab(432, 480, 376, 80, "", 12, G.ParseColor("#cfd6c8"));
        Btn(420, 596, 410, 56, "确认配置并出发", 20, () => { if (GameFlow.I != null) GameFlow.I.StartExpedition(); },
            new Color(0.14f, 0.32f, 0.20f, 1f));
    }

    Text MakeGoldBadge(float x, float w)
    {
        Panel(x, 13, w, 38, new Color(0.04f, 0.12f, 0.085f, 0.96f), "Badge");
        return Lab(x + 8, 19, w - 16, 26, "金币", 15, G.ParseColor("#f5c84c"), TextAnchor.MiddleCenter);
    }

    void ToggleConsumable(string id)
    {
        int cnt = GameState.loadout.ContainsKey(id) ? GameState.loadout[id] : 0;
        GameState.loadout[id] = cnt > 0 ? cnt - 1 : Math.Min(5, cnt + 1);
        SaveSystem.Save();
    }

    static MapDef FindMap(string id)
    {
        foreach (var m in GameData.Maps) if (m.id == id) return m;
        return null;
    }

    void Update()
    {
        if (!gameObject.activeSelf || GameFlow.I == null || GameFlow.I.screen != "prep") return;
        Refresh();
    }

    void Refresh()
    {
        Set(_gold, "金币  " + GameState.gold);

        // 地图：选中绿 / 金币不足灰
        foreach (var r in _maps)
        {
            var mp = FindMap(r.id);
            if (mp == null) continue;
            bool locked = GameState.gold < mp.entryFee;
            bool sel = mp.id == GameState.selectedMap;
            r.lab.color = locked ? new Color(0.5f, 0.5f, 0.5f) : (sel ? G.ParseColor("#7fff7f") : Color.white);
        }

        // 常驻技能数值（随已装备强化卡变化）
        var boosts = CardSystem.GetSelectedBoosts();
        for (int i = 0; i < _skills.Count && i < GameData.Skills.Length; i++)
        {
            var sk = GameData.Skills[i];
            var stats = SkillMath.GetStats(sk, boosts.ContainsKey(sk.id) ? boosts[sk.id] : 0);
            string power = stats.damage > 0 ? ("伤害 " + stats.damage)
                : stats.stunDuration > 0 ? ("控制 " + stats.stunDuration + "s")
                : stats.dashDistance > 0 ? ("位移 " + stats.dashDistance)
                : ("隐身 " + stats.stealthDuration + "s");
            Set(_skills[i].name, UIHost.SkillGlyph(sk.id) + "  " + sk.name + " · Lv." + stats.level + (stats.extraLevels > 0 ? (" (+" + stats.extraLevels + ")") : ""));
            Set(_skills[i].info, power + " · 能量 " + stats.energyCost + " · CD " + stats.cooldown + "s");
        }

        // 消耗品配载
        foreach (var c in _consumes)
        {
            var def = ConsumableById(c.id);
            int cnt = GameState.loadout.ContainsKey(c.id) ? GameState.loadout[c.id] : 0;
            Set(c.lab, UIHost.ConsumableGlyph(c.id) + "  " + (def != null ? def.name : c.id) + "\n×" + cnt + (cnt > 0 ? "\n(点击卸下)" : ""));
            c.lab.color = cnt > 0 ? G.ParseColor("#7fff7f") : Color.white;
        }

        // 强化卡
        Set(_cardPanelTitle, "携带强化卡  " + GameState.selectedBoostCards.Count + "/3");
        Set(_cardHint, GameState.cardInventory.Count == 0
            ? "卡牌工坊暂无强化卡。收获作物后再来配置。"
            : ("拥有 " + GameState.cardInventory.Count + " 张强化卡，点击装备/卸下"));
        RebuildCardsIfChanged();
        for (int i = 0; i < _cardIds.Count; i++)
        {
            var card = GameState.cardInventory.Find(c => c.id == _cardIds[i]);
            bool s = GameState.selectedBoostCards.Contains(_cardIds[i]);
            _cardLabs[i].color = s ? G.ParseColor("#83f2b2") : Color.white;
            if (card != null)
            {
                var sk = SaveSystem.SkillById(card.skillId);
                string skName = sk != null ? sk.name : card.skillId;
                Set(_cardLabs[i], UIHost.SkillGlyph(card.skillId) + " " + skName + " +" + card.power + "\n" + (s ? "[已携带]" : "点击携带") + "\n" + card.rarity + "卡");
            }
        }

        // 区域提示（随选中地图）
        var selMap = SaveSystem.MapById(GameState.selectedMap) ?? GameData.Maps[0];
        Set(_areaTip, "T" + selMap.tier + " · " + selMap.name
            + "\n区域有陷阱、水域/泥地减速与持续伤害区域。"
            + "\n环境威胁：" + (3 + selMap.tier * 2) + "–" + (5 + selMap.tier * 3) + " 个陷阱");
    }

    // cardInventory 前 3 张的 id 签名变化时（工坊装备/获得新卡后）重建按钮
    void RebuildCardsIfChanged()
    {
        var sb = new System.Text.StringBuilder();
        int n = Mathf.Min(3, GameState.cardInventory.Count);
        for (int i = 0; i < n; i++) sb.Append(GameState.cardInventory[i].id).Append('|');
        string sig = n + ":" + sb;
        if (sig == _cardSig) return;
        _cardSig = sig;

        for (int i = _cardHost.childCount - 1; i >= 0; i--) Destroy(_cardHost.GetChild(i).gameObject);
        _cardBtns.Clear(); _cardLabs.Clear(); _cardIds.Clear();

        float bx = 428; int bcnt = 0;
        foreach (var card in GameState.cardInventory)
        {
            if (bcnt >= 3) break;
            string cid = card.id;
            var b = BtnInHost(bx, 276, 124, 72, () => CardSystem.ToggleBoost(cid));
            _cardBtns.Add(b); _cardLabs.Add(b.GetComponentInChildren<Text>()); _cardIds.Add(cid);
            bx += 132; bcnt++;
        }
    }

    // 强化卡按钮建在 _cardHost 下（便于整体重建），坐标仍用全局旧坐标
    Button BtnInHost(float x, float y, float w, float h, Action onClick)
    {
        var b = UIFactory.Button(_cardHost, "", onClick, Mathf.RoundToInt(12 * 1.5f), new Color(0.16f, 0.14f, 0.22f, 0.98f));
        UIFactory.Place((RectTransform)b.transform, x, y, w, h);
        var t = b.GetComponentInChildren<Text>();
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        // 文本在刷新时按卡牌数据填充
        int idx = _cardIds.Count; // 当前即将加入的序号
        var card = GameState.cardInventory.Count > idx ? GameState.cardInventory[idx] : null;
        if (card != null)
        {
            var sk = SaveSystem.SkillById(card.skillId);
            bool s = GameState.selectedBoostCards.Contains(card.id);
            string skName = sk != null ? sk.name : card.skillId;
            t.text = UIHost.SkillGlyph(card.skillId) + " " + skName + " +" + card.power + "\n" + (s ? "[已携带]" : "点击携带") + "\n" + card.rarity + "卡";
        }
        return b;
    }

    static ConsumableDef ConsumableById(string id)
    {
        foreach (var c in GameData.Consumables) if (c.id == id) return c;
        return null;
    }

    static void Set(Text t, string s) { if (t != null && t.text != s) t.text = s; }
}
