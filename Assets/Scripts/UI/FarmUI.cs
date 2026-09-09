using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================= 农场主页（UGUI 运行时版，替代 UIHost.DrawFarm 的 OnGUI） =================
// 布局坐标沿用旧 IMGUI 的 1280x720，由 UIFactory.Place 统一 ×1.5 映射到 1920x1080；
// 控件只创建一次，动态数值（金币/生长进度/每日奖励/选中态）在 Update 每帧刷新；
// 所有点击行为与旧 DrawFarm 完全一致，逻辑层不改。
public class FarmUI : MonoBehaviour
{
    static FarmUI _i;

    Text _gold, _seeds, _materials, _farmTitle;
    Text _catalyst, _daily, _relief;
    Button _dailyBtn, _reliefBtn;
    Text _dailyBtnText, _reliefBtnText;
    Text _weatherLab, _seasonLab, _beautyLab;
    Button _visitorBtn;

    class Cell { public Button btn; public Text lab; public Image bg; public RectTransform fill; public Image fillImg; string _t; Color _c; }
    readonly Cell[] _cells = new Cell[36];
    class CropBtn { public string id; public Button btn; public Text lab; }
    readonly List<CropBtn> _cropBtns = new List<CropBtn>();

    public static void Sync(string screen)
    {
        bool show = screen == "farm";
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
        var rt = UIFactory.Rect("FarmUI", root.Root);
        UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<FarmUI>();
        _i.Build();
    }

    // 本地按钮：吃旧坐标/旧字号，内部做 1.5 倍换算
    Button Btn(float x, float y, float w, float h, string text, int legacySize, Action onClick, Color? bg = null)
    {
        var b = UIFactory.Button(transform, text, onClick, Mathf.RoundToInt(legacySize * 1.5f), bg);
        UIFactory.Place((RectTransform)b.transform, x, y, w, h);
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

    void Build()
    {
        // 全屏背景（从 OnGUI 迁移，避免 IMGUI 层遮住 UGUI 控件）
        var bg = gameObject.AddComponent<Image>();
        bg.sprite = Sprite.Create(UIHost.farmBackdrop, new Rect(0,0,UIHost.farmBackdrop.width,UIHost.farmBackdrop.height), new Vector2(0.5f,0.5f), 100f);
        bg.type = Image.Type.Simple; bg.raycastTarget = false;
        // ---------- 顶部资源条 ----------
        Panel(0, 0, 1280, 64, new Color(0.025f, 0.105f, 0.075f, 0.94f), "TopBar");
        Lab(34, 14, 400, 34, "我的家园农场", 25, Color.white);
        _gold = MakeBadge(610, 120, "金币", G.ParseColor("#f5c84c"));
        _seeds = MakeBadge(740, 110, "种子", G.ParseColor("#78e48c"));
        _materials = MakeBadge(860, 110, "材料", G.ParseColor("#ee9540"));
        Btn(1132, 13, 120, 38, "返回菜单", 18, () => { if (GameFlow.I != null) GameFlow.I.BackToMenu(); },
            new Color(0.12f, 0.25f, 0.18f, 0.98f));

        // ---------- 左侧农田 ----------
        Panel(26, 80, 704, 612, new Color(0.045f, 0.11f, 0.072f, 0.90f), "FarmGridPanel");
        _farmTitle = Lab(54, 98, 620, 30, "", 16, G.ParseColor("#efd78d"));
        const int cs = 84; float gx = 54, gy = 142;
        for (int i = 0; i < 36; i++)
        {
            int idx = i;
            int row = i / 6, col = i % 6;
            float x = gx + col * cs, y = gy + row * cs;
            var b = Btn(x, y, cs - 7, cs - 7, "", 13, () => OnPlotClick(idx), new Color(0.2f, 0.105f, 0.035f, 0.96f));
            var cell = new Cell
            {
                btn = b,
                bg = b.GetComponent<Image>(),
                lab = b.GetComponentInChildren<Text>()
            };
            cell.lab.alignment = TextAnchor.MiddleCenter; cell.lab.fontStyle = FontStyle.Bold;
            // 进度槽 + 进度填充（相对格子底部）
            var barBg = Panel(x + 5, y + cs - 16, cs - 17, 6, new Color(0.12f, 0.12f, 0.09f, 1f), "BarBg");
            barBg.raycastTarget = false;
            var fillRt = UIFactory.Rect("BarFill", transform);
            var fillImg = fillRt.gameObject.AddComponent<Image>();
            fillImg.color = G.ParseColor("#70ef72"); fillImg.raycastTarget = false;
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0, 0); fillRt.pivot = new Vector2(0, 0);
            cell.fill = fillRt; cell.fillImg = fillImg;
            _cells[i] = cell;
        }

        // ---------- 右上：家园设施 ----------
        const float px = 760;
        TitledPanel(px, 80, 492, 126, "家园设施");
        Facility(px + 12, 120, 148, 72, "🔨", "卡牌工坊", () => { if (GameFlow.I != null) GameFlow.I.OpenWorkshop(); });
        Facility(px + 172, 120, 148, 72, "📦", "物资仓库", () => { GreenhouseSystem.warehouseOpen = true; });
        Facility(px + 332, 120, 148, 72, "🏡", "育种温室", () => { GreenhouseSystem.greenhouseOpen = true; GreenhouseSystem.Init(); });

        // ---------- 家园补给站 ----------
        TitledPanel(px, 218, 492, 132, "家园补给站");
        _catalyst = Lab(px + 12, 252, 170, 24, "", 12, G.ParseColor("#e8d8a8"));
        Btn(px + 174, 248, 122, 32, "使用催化剂", 13, () => { if (GameFlow.I != null) GameFlow.I.UseCatalyst(); });
        _daily = Lab(px + 12, 290, 150, 24, "", 12, G.ParseColor("#e8d8a8"));
        _dailyBtn = Btn(px + 150, 288, 100, 30, "领取奖励", 13, () => { if (GameFlow.I != null) GameFlow.I.ClaimDaily(); });
        _dailyBtnText = _dailyBtn.GetComponentInChildren<Text>();
        _relief = Lab(px + 300, 290, 150, 24, "", 12, G.ParseColor("#e8d8a8"));
        _reliefBtn = Btn(px + 382, 288, 98, 30, "领取保障", 13, () => { if (GameFlow.I != null) GameFlow.I.ClaimRelief(); });
        _reliefBtnText = _reliefBtn.GetComponentInChildren<Text>();

        // ---------- 荒野远征站 ----------
        TitledPanel(px, 364, 492, 124, "荒野远征站");
        Lab(px + 14, 400, 456, 32, "选择地图、技能与携带物进入荒野，获取战利品后撤离。", 12, G.ParseColor("#cfd6c8"));
        Btn(px + 14, 438, 464, 38, "进入远征准备大厅  →", 17, () => { if (GameFlow.I != null) GameFlow.I.OpenPrep(); },
            new Color(0.14f, 0.30f, 0.20f, 1f));

        // ---------- 天气与季节 ----------
        _weatherLab = Lab(px + 14, 500, 200, 28, "", 14, G.ParseColor("#aaddff"));
        _seasonLab = Lab(px + 220, 500, 140, 28, "", 14, G.ParseColor("#ffddaa"));
        _beautyLab = Lab(px + 370, 500, 120, 28, "", 14, G.ParseColor("#ddaaff"));

        // ---------- 农场新功能 ----------
        Btn(px + 14, 536, 110, 32, "🏭 工坊", 13, () => { ProcessingUI.Toggle(); }, new Color(0.16f, 0.20f, 0.28f, 0.98f));
        Btn(px + 132, 536, 110, 32, "📖 图鉴", 13, () => { CollectionUI.Toggle(); }, new Color(0.16f, 0.20f, 0.28f, 0.98f));
        Btn(px + 250, 536, 110, 32, "🎨 装饰", 13, () => { DecorationUI.Toggle(); }, new Color(0.16f, 0.20f, 0.28f, 0.98f));
        _visitorBtn = Btn(px + 368, 536, 110, 32, "🚶 访客", 13, () => { FarmDecorationSystem.InteractVisitor(); }, new Color(0.20f, 0.16f, 0.28f, 0.98f));
        _visitorBtn.gameObject.SetActive(false);

        // ---------- 选择作物 ----------
        TitledPanel(760, 502, 492, 174, "选择作物");
        float cx = 772;
        foreach (var crop in GameData.Crops)
        {
            if (!GameState.unlockedCrops.Contains(crop.id)) continue;
            string cid = crop.id;
            var b = Btn(cx, 544, 88, 60, UIHost.CropGlyph(crop.id) + "\n" + crop.name, 12, () =>
            {
                GameState.selectedCrop = cid; SaveSystem.Save();
            }, new Color(0.16f, 0.22f, 0.16f, 0.98f));
            _cropBtns.Add(new CropBtn { id = cid, btn = b, lab = b.GetComponentInChildren<Text>() });
            cx += 94;
        }
    }

    Text MakeBadge(float x, float w, string name, Color c)
    {
        var bg = Panel(x, 13, w, 38, new Color(0.04f, 0.12f, 0.085f, 0.96f), "Badge");
        var outline = UIFactory.Rect("Outline", transform);
        var ol = outline.gameObject.AddComponent<Image>();
        ol.color = new Color(c.r, c.g, c.b, 0.52f); ol.raycastTarget = false;
        UIFactory.Place(outline, x, 13, w, 1); // 顶线（简化描边）
        return Lab(x + 8, 19, w - 16, 26, name, 15, c, TextAnchor.MiddleCenter);
    }

    void TitledPanel(float x, float y, float w, float h, string title)
    {
        Panel(x, y, w, h, new Color(0f, 0f, 0f, 0.5f), "Panel");
        Lab(x + 12, y + 8, w - 24, 26, title, 16, G.ParseColor("#f0d58f"));
    }

    void Facility(float x, float y, float w, float h, string icon, string name, Action onClick)
    {
        var b = Btn(x, y, w, h, icon + "\n" + name, 14, onClick, new Color(0.10f, 0.18f, 0.13f, 0.95f));
        var t = b.GetComponentInChildren<Text>();
        t.color = G.ParseColor("#e8d8a8");
    }

    void Update()
    {
        if (!gameObject.activeSelf || GameFlow.I == null || GameFlow.I.screen != "farm") return;
        Refresh();
    }

    void Refresh()
    {
        SetText(_gold, "金币  " + GameState.gold);
        SetText(_seeds, "种子  " + GreenhouseSystem.GetWarehouseCount("seeds"));
        SetText(_materials, "材料  " + GreenhouseSystem.GetWarehouseCount("materials"));
        SetText(_farmTitle, "农田 " + GameState.unlockedPlots + "/36    点击空地播种 · 点击成熟作物收获");
        SetText(_catalyst, "生长催化剂 ×" + GreenhouseSystem.GetWarehouseCount("growth_catalyst"));

        bool claimed = RewardSystem.DailyClaimedToday();
        int day = RewardSystem.GetNextDailyDay();
        SetText(_daily, claimed ? ("已领 第" + RewardSystem.TodayDay() + "天") : ("每日 第" + day + "天"));
        SetText(_dailyBtnText, claimed ? "今日已领" : "领取奖励");
        SetText(_relief, RewardSystem.IsReliefEligible() ? "可领保障" : "保障暂不可领");

        // 天气/季节/美观
        SetText(_weatherLab, FarmCareSystem.WeatherIcon(GameState.weather) + " " + FarmCareSystem.WeatherName(GameState.weather));
        SetText(_seasonLab, FarmCareSystem.SeasonIcon(GameState.season) + " " + FarmCareSystem.SeasonName(GameState.season) + " D" + GameState.seasonDay);
        SetText(_beautyLab, "✨ 美观 " + GameState.farmBeauty);
        // 访客
        if (_visitorBtn != null)
        {
            bool visiting = GameState.visitorState == "visiting";
            _visitorBtn.gameObject.SetActive(visiting);
            if (visiting)
            {
                var vt = _visitorBtn.GetComponentInChildren<Text>();
                if (vt != null) vt.text = "🚶 " + GameState.visitorName;
            }
        }

        for (int i = 0; i < 36; i++) RefreshCell(i);
        foreach (var c in _cropBtns)
        {
            bool sel = GameState.selectedCrop == c.id;
            c.lab.color = sel ? G.ParseColor("#ffe27a") : Color.white;
        }
    }

    void RefreshCell(int i)
    {
        var cell = _cells[i];
        var plot = GameState.farmPlots[i];
        Color bg; string text; float pct = 0f; bool showBar = false; int fontSize = 20;

        if (i >= GameState.unlockedPlots)
        {
            bool next = i == GameState.unlockedPlots;
            bg = next ? new Color(0.25f, 0.21f, 0.12f, 0.95f) : new Color(0.11f, 0.13f, 0.105f, 0.90f);
            if (next)
            {
                var cost = FarmSystem.GetUnlockCost(i);
                text = "锁定\n" + cost.gold + " 金" + (cost.materials > 0 ? ("\n" + cost.materials + " 材料") : "");
            }
            else text = "锁定";
            fontSize = 17;
        }
        else if (plot.crop != null)
        {
            float progress = UIHost.CropProgress(plot, i);
            bg = new Color(0.19f, 0.115f, 0.045f, 0.98f);
            // 湿度低时背景偏红
            if (plot.moisture < 30f) bg = new Color(0.25f, 0.10f, 0.08f, 0.98f);
            bool hasStatus = !string.IsNullOrEmpty(plot.status);
            string state = hasStatus ? ("\n" + UIHost.StatusIcon(plot.status)) : progress >= 1 ? "\n可收获" : "";
            // 豌豆显示剩余次数
            if (plot.crop.trait == "reharvest" && plot.harvestCount > 0) state += "\n剩" + (3 - plot.harvestCount) + "次";
            text = UIHost.CropGlyph(plot.crop.id) + state;
            pct = progress; showBar = true; fontSize = progress >= 1 ? 29 : 36;
            cell.fillImg.color = progress >= 1 ? G.ParseColor("#e7c946") : (plot.moisture < 30f ? G.ParseColor("#ff6b6b") : G.ParseColor("#70ef72"));
            // 品质文字颜色
            cell.lab.color = FarmCollectionSystem.QualityColor(plot.quality);
        }
        else
        {
            bg = new Color(0.20f, 0.105f, 0.035f, 0.96f);
            text = "播种"; fontSize = 20;
        }

        if (cell.bg.color != bg) cell.bg.color = bg;
        if (cell.lab.text != text) cell.lab.text = text;
        if (Mathf.RoundToInt(cell.lab.fontSize) != Mathf.RoundToInt(fontSize * 1.5f)) cell.lab.fontSize = Mathf.RoundToInt(fontSize * 1.5f);
        // 进度条宽度（旧坐标槽宽 cs-17=67）
        cell.fill.gameObject.SetActive(showBar);
        if (showBar)
        {
            // 与 Build 中槽位同一位置：x=gx+col*cs+5, y=gy+row*cs+cs-16（1280 基准）
            int row = i / 6, col = i % 6;
            float bx = 54 + col * 84 + 5, by = 142 + row * 84 + 84 - 16;
            UIFactory.Place(cell.fill, bx, by, 67 * pct, 6);
        }
    }

    void OnPlotClick(int i)
    {
        var plot = GameState.farmPlots[i];
        if (i >= GameState.unlockedPlots)
        {
            if (i == GameState.unlockedPlots) FarmSystem.UnlockPlot(i);
        }
        else if (plot.crop != null)
        {
            if (!string.IsNullOrEmpty(plot.status)) FarmSystem.Tend(i);
            else if (UIHost.CropProgress(plot) >= 1f) FarmSystem.Harvest(i);
        }
        else FarmSystem.Plant(i);
    }

    static void SetText(Text t, string s) { if (t != null && t.text != s) t.text = s; }
}
