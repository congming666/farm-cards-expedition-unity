using System;
using UnityEngine;
using UnityEngine.UI;

// ================= 装饰商店面板（UGUI 运行时弹窗） =================
public class DecorationUI : MonoBehaviour
{
    static DecorationUI _i;
    GameObject _content;
    Text _beautyLabel;

    public static void Toggle()
    {
        if (_i == null) Create();
        if (_i != null) _i.gameObject.SetActive(!_i.gameObject.activeSelf);
    }

    static void Create()
    {
        var root = UIRoot.Ensure();
        var rt = UIFactory.Rect("DecorationUI", root.Root);
        UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<DecorationUI>();
        _i.Build();
        _i.gameObject.SetActive(false);
    }

    void Build()
    {
        var mask = UIFactory.PlacedPanel(transform, "Mask", 0, 0, 1280, 720, new Color(0f, 0f, 0f, 0.7f));
        mask.raycastTarget = true;

        var panel = UIFactory.PlacedPanel(transform, "Panel", 240, 80, 800, 560, new Color(0.08f, 0.10f, 0.14f, 0.98f));
        var prt = panel.rectTransform;

        UIFactory.PlacedLabel(transform, "🎨 装饰商店", 24, G.ParseColor("#ddaaff"), 260, 600, 400, 40, TextAnchor.MiddleCenter);

        var close = UIFactory.Button(transform, "✕", () => gameObject.SetActive(false), 20, new Color(0.3f, 0.15f, 0.15f, 0.98f));
        UIFactory.Place((RectTransform)close.transform, 1000, 600, 40, 40);

        _beautyLabel = UIFactory.PlacedLabel(transform, "✨ 当前美观度：" + GameState.farmBeauty + "  金币加成+" + ((FarmDecorationSystem.BeautyGoldBonus() - 1f) * 100).ToString("F0") + "%", 14, G.ParseColor("#ffd700"), 260, 560, 500, 28, TextAnchor.MiddleLeft);

        _content = UIFactory.Rect("Content", prt).gameObject;
        UIFactory.Place((RectTransform)_content.transform, 260, 140, 760, 400);
        var grid = _content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(240, 100);
        grid.spacing = new Vector2(10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        Refresh();
    }

    void Refresh()
    {
        for (int i = _content.transform.childCount - 1; i >= 0; i--)
            Destroy(_content.transform.GetChild(i).gameObject);

        foreach (var item in FarmDecorationSystem.ShopItems)
        {
            var card = UIFactory.Rect("Deco_" + item.id, _content.transform);
            var bg = card.gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            var lab = UIFactory.Label(card, item.icon + " " + item.name + "\n<size=11>美观+" + item.beauty + " | " + item.desc + "</size>", 13, Color.white, TextAnchor.MiddleLeft);
            lab.supportRichText = true;
            var lrt = (RectTransform)lab.transform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1); lrt.offsetMin = new Vector2(8, 28); lrt.offsetMax = new Vector2(-8, -4);

            var btn = UIFactory.Button(card, "购买 (" + item.cost + "金)", () =>
            {
                FarmDecorationSystem.BuyDecoration(item.id);
                Refresh();
                _beautyLabel.text = "✨ 当前美观度：" + GameState.farmBeauty + "  金币加成+" + ((FarmDecorationSystem.BeautyGoldBonus() - 1f) * 100).ToString("F0") + "%";
            }, 12, new Color(0.20f, 0.18f, 0.28f, 0.98f));
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0); brt.pivot = new Vector2(0.5f, 0);
            brt.sizeDelta = new Vector2(-16, 24); brt.anchoredPosition = new Vector2(0, 4);
        }
    }

    void OnEnable() { if (_content != null) { Refresh(); _beautyLabel.text = "✨ 当前美观度：" + GameState.farmBeauty + "  金币加成+" + ((FarmDecorationSystem.BeautyGoldBonus() - 1f) * 100).ToString("F0") + "%"; } }
}
