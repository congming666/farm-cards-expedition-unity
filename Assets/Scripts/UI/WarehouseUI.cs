using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================= 物资仓库（UGUI overlay，替代 UIHost.DrawWarehouse） =================
public class WarehouseUI : MonoBehaviour
{
    static WarehouseUI _i;
    Text _capText;
    RectTransform _capFill;
    RectTransform _itemHost;
    string _sig = "";

    public static void Sync(bool open)
    {
        if (open) { if (_i == null) Create(); if (_i != null && !_i.gameObject.activeSelf) _i.gameObject.SetActive(true); }
        else if (_i != null && _i.gameObject.activeSelf) _i.gameObject.SetActive(false);
    }
    static void Create()
    {
        var root = UIRoot.Ensure();
        var rt = UIFactory.Rect("WarehouseUI", root.Root); UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<WarehouseUI>(); _i.Build();
    }

    Button Btn(float x, float y, float w, float h, string text, int ls, System.Action onClick, Color? bg = null)
    {
        var b = UIFactory.Button(transform, text, onClick, Mathf.RoundToInt(ls * 1.5f), bg);
        UIFactory.Place((RectTransform)b.transform, x, y, w, h); return b;
    }
    Text Lab(float x, float y, float w, float h, string t, int ls, Color c, TextAnchor a = TextAnchor.MiddleLeft)
        => UIFactory.PlacedLabel(transform, t, ls, c, x, y, w, h, a);

    void Build()
    {
        UIFactory.PlacedPanel(transform, "Bg", 0, 0, 1280, 720, new Color(0f, 0f, 0f, 0.75f));
        UIFactory.PlacedPanel(transform, "Panel", 200, 40, 880, 640, new Color(0.06f, 0.09f, 0.10f, 0.95f));
        Lab(220, 56, 600, 32, "📦 物资仓库", 24, G.ParseColor("#8ac8d8"));
        _capText = Lab(220, 100, 300, 20, "", 15, Color.white);
        UIFactory.PlacedPanel(transform, "CapBg", 220, 124, 400, 14, new Color(0.13f, 0.13f, 0.13f, 1f)).raycastTarget = false;
        _capFill = UIFactory.Rect("CapFill", transform);
        var fi = _capFill.gameObject.AddComponent<Image>(); fi.color = G.ParseColor("#5a9a5a"); fi.raycastTarget = false;
        Btn(660, 100, 180, 40, "一键出售作物", 14, SellAllCrops);
        Btn(860, 100, 200, 40, "扩建仓库 (+25)\n💰" + GreenhouseSystem.GetWarehouseUpgradeCost(), 12,
            () => GreenhouseSystem.UpgradeWarehouse(), new Color(0.20f, 0.16f, 0.08f, 1f));
        _itemHost = UIFactory.Rect("Items", transform);
        var close = UIFactory.Button(transform, "关闭", () => { GreenhouseSystem.warehouseOpen = false; }, Mathf.RoundToInt(14 * 1.5f));
        UIFactory.Place((RectTransform)close.transform, 560, 640, 160, 40);
    }

    void SellAllCrops()
    {
        var toRemove = new List<string>();
        foreach (var kv in GameState.warehouseItems)
        {
            var crop = SaveSystem.CropById(kv.Key);
            if (crop != null) { GameState.gold += (int)crop.sellPrice * kv.Value; toRemove.Add(kv.Key); }
        }
        foreach (var id in toRemove) GameState.warehouseItems.Remove(id);
        if (toRemove.Count > 0) { if (GameFlow.I != null) GameFlow.I.AddToast("一键出售完成", "gold"); SaveSystem.Save(); }
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;
        int used = GreenhouseSystem.GetWarehouseUsed(), cap = GameState.warehouseCapacity;
        Set(_capText, "仓库容量：" + used + " / " + cap);
        UIFactory.Place(_capFill, 220, 124, 400 * Mathf.Clamp01((float)used / cap), 14);
        RebuildItems();
    }

    void RebuildItems()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kv in GameState.warehouseItems) if (kv.Value > 0) sb.Append(kv.Key).Append(kv.Value);
        string sig = sb.ToString();
        if (sig == _sig) return; _sig = sig;
        for (int i = _itemHost.childCount - 1; i >= 0; i--) Destroy(_itemHost.GetChild(i).gameObject);
        int col = 0; float y = 160;
        foreach (var kv in GameState.warehouseItems)
        {
            if (kv.Value <= 0) continue;
            float bx = 220 + (col % 4) * 210, by = y + (col / 4) * 100;
            var def = GreenhouseData.Drops.ContainsKey(kv.Key) ? GreenhouseData.Drops[kv.Key] : null;
            var crop = SaveSystem.CropById(kv.Key);
            string icon = def != null ? def.icon : crop != null ? crop.icon : "📦";
            string name = def != null ? def.name : crop != null ? crop.name : kv.Key;
            int sellPrice = def != null ? def.sellPrice : crop != null ? (int)crop.sellPrice : 0;
            UIFactory.PlacedPanel(_itemHost, "Box", bx, by, 190, 90, new Color(0.10f, 0.13f, 0.15f, 0.95f)).raycastTarget = false;
            UIFactory.PlacedLabel(_itemHost, icon + " " + name, 14, Color.white, bx + 8, by + 6, 174, 24);
            UIFactory.PlacedLabel(_itemHost, "×" + kv.Value, 16, G.ParseColor("#ffd700"), bx + 8, by + 28, 174, 20);
            if (sellPrice > 0)
            {
                string id = kv.Key;
                var s1 = UIFactory.Button(_itemHost, "出售 💰" + sellPrice, () => GreenhouseSystem.SellWarehouseItem(id, 1), Mathf.RoundToInt(11 * 1.5f));
                UIFactory.Place((RectTransform)s1.transform, bx + 8, by + 52, 82, 30);
                var s2 = UIFactory.Button(_itemHost, "全部出售", () => GreenhouseSystem.SellWarehouseItem(id, kv.Value), Mathf.RoundToInt(11 * 1.5f));
                UIFactory.Place((RectTransform)s2.transform, bx + 96, by + 52, 86, 30);
            }
            else UIFactory.PlacedLabel(_itemHost, "不可出售", 12, G.ParseColor("#888888"), bx + 8, by + 56, 174, 20);
            col++;
        }
        if (GameState.warehouseItems.Count == 0)
        {
            UIFactory.PlacedLabel(_itemHost, "📦 仓库空空如也", 20, G.ParseColor("#888888"), 220, 300, 840, 40, TextAnchor.MiddleCenter);
            UIFactory.PlacedLabel(_itemHost, "收获作物和远征战利品会自动存入这里", 12, G.ParseColor("#aeb8ae"), 220, 340, 840, 24, TextAnchor.MiddleCenter);
        }
    }
    static void Set(Text t, string s) { if (t != null && t.text != s) t.text = s; }
}
