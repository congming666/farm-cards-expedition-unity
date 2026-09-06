using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================= 育种温室（UGUI overlay，替代 UIHost.DrawGreenhouse） =================
public class GreenhouseUI : MonoBehaviour
{
    static GreenhouseUI _i;

    class PlantSel { public RectTransform box; public Text name, info; public string id; }
    readonly List<PlantSel> _plants = new List<PlantSel>();

    class PlotCell { public Image box; public Text icon, name, state; public RectTransform fill; public Text pct; }
    readonly PlotCell[] _plots = new PlotCell[16];

    RectTransform _dropHost;
    string _dropSig = "";

    public static void Sync(bool open)
    {
        if (open) { if (_i == null) Create(); if (_i != null && !_i.gameObject.activeSelf) _i.gameObject.SetActive(true); }
        else if (_i != null && _i.gameObject.activeSelf) _i.gameObject.SetActive(false);
    }
    static void Create()
    {
        var root = UIRoot.Ensure();
        var rt = UIFactory.Rect("GreenhouseUI", root.Root); UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<GreenhouseUI>(); _i.Build();
    }

    Text Lab(float x, float y, float w, float h, string t, int ls, Color c, TextAnchor a = TextAnchor.MiddleLeft)
        => UIFactory.PlacedLabel(transform, t, ls, c, x, y, w, h, a);

    void Build()
    {
        UIFactory.PlacedPanel(transform, "Bg", 0, 0, 1280, 720, new Color(0f, 0f, 0f, 0.75f));
        UIFactory.PlacedPanel(transform, "Panel", 140, 30, 1000, 660, new Color(0.06f, 0.09f, 0.10f, 0.95f));
        Lab(160, 46, 600, 32, "🏡 育种温室", 24, G.ParseColor("#8ac8d8"));
        Lab(160, 86, 200, 20, "选择稀有植物：", 15, Color.white);

        // 植物选择器
        int pcol = 0;
        foreach (var plant in GreenhouseData.Plants)
        {
            string pid = plant.id;
            float px = 160 + pcol * 145;
            var box = UIFactory.PlacedPanel(transform, "Plant", px, 110, 135, 60, new Color(0.10f, 0.13f, 0.15f, 0.95f));
            var name = Lab(px + 4, 114, 127, 24, "", 13, Color.white, TextAnchor.MiddleCenter);
            var info = Lab(px + 4, 138, 127, 16, plant.growTime + "秒 · 💰" + plant.seedPrice, 10, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter);
            var btn = UIFactory.Button(transform, "", () => { if (GameState.unlockedGreenhousePlants.Contains(pid)) GameState.selectedGreenhousePlant = pid; }, 10, new Color(0, 0, 0, 0));
            UIFactory.Place((RectTransform)btn.transform, px, 110, 135, 60);
            _plants.Add(new PlantSel { box = (RectTransform)box.transform, name = name, info = info, id = pid });
            pcol++;
        }

        Lab(160, 190, 400, 20, "温室 " + GameState.greenhouseUnlockedPlots + "/16（点击空地播种，点击成熟植物收获）", 15, Color.white);
        // 4x4 格子
        const int gs = 110; float gx = 160, gy = 216;
        for (int i = 0; i < 16; i++)
        {
            int idx = i;
            float cx = gx + (i % 4) * gs, cy = gy + (i / 4) * gs;
            var box = UIFactory.PlacedPanel(transform, "Plot", cx, cy, gs - 6, gs - 6, new Color(0.10f, 0.13f, 0.15f, 0.95f));
            var icon = Lab(cx, cy + 8, gs - 6, 28, "", 24, Color.white, TextAnchor.MiddleCenter);
            var pname = Lab(cx, cy + 36, gs - 6, 16, "", 11, Color.white, TextAnchor.MiddleCenter);
            var state = Lab(cx, cy + 54, gs - 6, 16, "", 11, G.ParseColor("#ffd700"), TextAnchor.MiddleCenter);
            var fillRt = UIFactory.Rect("Fill", transform);
            var fi = fillRt.gameObject.AddComponent<Image>(); fi.color = G.ParseColor("#5a9a5a"); fi.raycastTarget = false;
            var pct = Lab(cx, cy + 66, gs - 6, 12, "", 9, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter);
            var btn = UIFactory.Button(transform, "", () => OnPlotClick(idx), 10, new Color(0, 0, 0, 0));
            UIFactory.Place((RectTransform)btn.transform, cx, cy, gs - 6, gs - 6);
            _plots[i] = new PlotCell { box = box, icon = icon, name = pname, state = state, fill = fillRt, pct = pct };
        }

        Lab(640, 190, 200, 20, "🧪 温室道具：", 15, Color.white);
        _dropHost = UIFactory.Rect("Drops", transform);
        Lab(640, 500, 340, 120,
            "💡 温室提示\n\n· 种植稀有植物消耗金币购买种子\n· 稀有植物成熟后可收获各种道具\n· 传说植物有概率解锁新的稀有植物\n· 温室格子需要金币和材料解锁\n· 作物转化卡可将普通作物变为稀有作物",
            11, new Color(0.7f, 0.7f, 0.7f), TextAnchor.UpperLeft);
        var close = UIFactory.Button(transform, "关闭", () => { GreenhouseSystem.greenhouseOpen = false; }, Mathf.RoundToInt(14 * 1.5f));
        UIFactory.Place((RectTransform)close.transform, 560, 650, 160, 40);
    }

    void OnPlotClick(int i)
    {
        var plot = GameState.greenhousePlots[i];
        if (i >= GameState.greenhouseUnlockedPlots)
        {
            if (i == GameState.greenhouseUnlockedPlots) GreenhouseSystem.UnlockPlot(i);
        }
        else if (plot.plant != null)
        {
            if (plot.ready) GreenhouseSystem.Harvest(i);
            else if (GameFlow.I != null) GameFlow.I.AddToast("还没成熟呢", "warning");
        }
        else GreenhouseSystem.Plant(i);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;
        GameState.EnsureGreenhousePlots();
        double nowSec = System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds;

        // 植物选择器刷新
        foreach (var ps in _plants)
        {
            var plant = PlantById(ps.id); if (plant == null) continue;
            bool unlocked = GameState.unlockedGreenhousePlants.Contains(plant.id);
            bool selected = GameState.selectedGreenhousePlant == plant.id;
            Color c = plant.rarity == "legendary" ? G.ParseColor("#ee9637") : plant.rarity == "epic" ? G.ParseColor("#a878d8") : G.ParseColor("#78c878");
            ps.name.text = (unlocked ? plant.icon : "🔒") + " " + plant.name;
            ps.name.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            ps.box.GetComponent<Image>().color = selected && unlocked ? new Color(c.r, c.g, c.b, 0.5f) : new Color(0.10f, 0.13f, 0.15f, 0.95f);
        }

        // 16 格子刷新
        const int gs = 110; float gx = 160, gy = 216;
        for (int i = 0; i < 16; i++)
        {
            float cx = gx + (i % 4) * gs, cy = gy + (i / 4) * gs;
            var cell = _plots[i];
            var plot = GameState.greenhousePlots[i];
            if (i >= GameState.greenhouseUnlockedPlots)
            {
                int goldCost = GreenhouseSystem.GetUnlockCost(i), matCost = GreenhouseSystem.GetUnlockMatCost(i);
                cell.icon.text = "🔒"; cell.icon.color = Color.white;
                cell.name.text = ""; cell.state.text = "💰" + goldCost + " 🔧" + matCost;
                cell.state.color = new Color(0.7f, 0.7f, 0.7f);
                cell.fill.gameObject.SetActive(false); cell.pct.text = "";
            }
            else if (plot.plant != null)
            {
                double elapsed = nowSec - plot.plantedAt;
                float progress = Mathf.Clamp01((float)(elapsed / plot.plant.growTime));
                plot.ready = progress >= 1;
                Color pc = plot.plant.rarity == "legendary" ? G.ParseColor("#ee9637") : plot.plant.rarity == "epic" ? G.ParseColor("#a878d8") : G.ParseColor("#78c878");
                cell.icon.text = plot.plant.icon; cell.icon.color = Color.white;
                cell.name.text = plot.plant.name; cell.name.color = pc;
                if (plot.ready) { cell.state.text = "✨可收获"; cell.state.color = G.ParseColor("#ffd700"); cell.fill.gameObject.SetActive(false); cell.pct.text = ""; }
                else
                {
                    cell.state.text = ""; cell.fill.gameObject.SetActive(true);
                    UIFactory.Place(cell.fill, cx + 8, cy + 56, (gs - 22) * progress, 8);
                    cell.pct.text = Mathf.FloorToInt(progress * 100) + "%";
                }
            }
            else
            {
                cell.icon.text = "➕"; cell.icon.color = new Color(0.4f, 0.6f, 0.4f);
                cell.name.text = "播种"; cell.name.color = new Color(0.5f, 0.7f, 0.5f);
                cell.state.text = ""; cell.fill.gameObject.SetActive(false); cell.pct.text = "";
            }
        }

        RebuildDrops();
    }

    void RebuildDrops()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kv in GameState.warehouseItems) if (kv.Value > 0 && GreenhouseData.Drops.ContainsKey(kv.Key)) sb.Append(kv.Key).Append(kv.Value);
        string sig = sb.ToString();
        if (sig == _dropSig) return; _dropSig = sig;
        for (int i = _dropHost.childCount - 1; i >= 0; i--) Destroy(_dropHost.GetChild(i).gameObject);
        float dy = 216; bool has = false;
        foreach (var kv in GameState.warehouseItems)
        {
            if (kv.Value <= 0) continue;
            var def = GreenhouseData.Drops.ContainsKey(kv.Key) ? GreenhouseData.Drops[kv.Key] : null;
            if (def == null) continue;
            has = true;
            UIFactory.PlacedPanel(_dropHost, "Box", 640, dy, 340, 50, new Color(0.10f, 0.13f, 0.15f, 0.95f)).raycastTarget = false;
            UIFactory.PlacedLabel(_dropHost, def.icon, 24, Color.white, 648, dy + 4, 40, 42, TextAnchor.MiddleCenter);
            UIFactory.PlacedLabel(_dropHost, def.name + " ×" + kv.Value, 13, Color.white, 694, dy + 6, 200, 18);
            UIFactory.PlacedLabel(_dropHost, def.desc, 10, new Color(0.7f, 0.7f, 0.7f), 694, dy + 24, 200, 18);
            string id = kv.Key;
            var use = UIFactory.Button(_dropHost, "使用", () => GreenhouseSystem.UseDropItem(id), Mathf.RoundToInt(12 * 1.5f));
            UIFactory.Place((RectTransform)use.transform, 900, dy + 10, 72, 30);
            dy += 56;
        }
        if (!has) UIFactory.PlacedLabel(_dropHost, "收获稀有植物获取道具", 13, new Color(0.6f, 0.6f, 0.6f), 640, 240, 340, 40, TextAnchor.MiddleCenter);
    }

    static GreenhousePlantDef PlantById(string id)
    {
        foreach (var p in GreenhouseData.Plants) if (p.id == id) return p;
        return null;
    }
}
