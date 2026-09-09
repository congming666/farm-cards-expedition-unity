using System;
using UnityEngine;
using UnityEngine.UI;

// ================= 作物图鉴面板（UGUI 运行时弹窗） =================
public class CollectionUI : MonoBehaviour
{
    static CollectionUI _i;
    GameObject _content;

    public static void Toggle()
    {
        if (_i == null) Create();
        if (_i != null) _i.gameObject.SetActive(!_i.gameObject.activeSelf);
    }

    static void Create()
    {
        var root = UIRoot.Ensure();
        var rt = UIFactory.Rect("CollectionUI", root.Root);
        UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<CollectionUI>();
        _i.Build();
        _i.gameObject.SetActive(false);
    }

    void Build()
    {
        var mask = UIFactory.PlacedPanel(transform, "Mask", 0, 0, 1280, 720, new Color(0f, 0f, 0f, 0.7f));
        mask.raycastTarget = true;

        var panel = UIFactory.PlacedPanel(transform, "Panel", 240, 80, 800, 560, new Color(0.08f, 0.10f, 0.14f, 0.98f));
        var prt = panel.rectTransform;

        UIFactory.PlacedLabel(transform, "📖 作物图鉴", 24, G.ParseColor("#7be5c4"), 260, 600, 400, 40, TextAnchor.MiddleCenter);

        var close = UIFactory.Button(transform, "✕", () => gameObject.SetActive(false), 20, new Color(0.3f, 0.15f, 0.15f, 0.98f));
        UIFactory.Place((RectTransform)close.transform, 1000, 600, 40, 40);

        // 收集进度
        int total = GameData.Crops.Length;
        int collected = GameState.cropCollection.Count;
        UIFactory.PlacedLabel(transform, "收集进度：" + collected + "/" + total + "  全局生长+" + ((FarmCollectionSystem.CollectionBonus() - 1f) * 100).ToString("F0") + "%", 14, G.ParseColor("#aaccaa"), 260, 560, 500, 28, TextAnchor.MiddleLeft);

        _content = UIFactory.Rect("Content", prt).gameObject;
        UIFactory.Place((RectTransform)_content.transform, 260, 140, 760, 400);
        var grid = _content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(240, 80);
        grid.spacing = new Vector2(10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        Refresh();
    }

    void Refresh()
    {
        for (int i = _content.transform.childCount - 1; i >= 0; i--)
            Destroy(_content.transform.GetChild(i).gameObject);

        foreach (var crop in GameData.Crops)
        {
            var card = UIFactory.Rect("Crop_" + crop.id, _content.transform);
            var bg = card.gameObject.AddComponent<Image>();

            bool found = GameState.cropCollection.ContainsKey(crop.id);
            string bestQ = found ? GameState.cropCollection[crop.id] : "none";

            if (found)
            {
                bg.color = new Color(0.12f, 0.18f, 0.14f, 0.95f);
                var lab = UIFactory.Label(card, crop.icon + " " + crop.name + "\n<size=11>品质：" + FarmCollectionSystem.QualityName(bestQ) + "</size>\n<size=10>" + FarmTraitSystem.TraitDesc(crop.trait) + "</size>", 13, FarmCollectionSystem.QualityColor(bestQ), TextAnchor.MiddleLeft);
                lab.supportRichText = true;
                var lrt = (RectTransform)lab.transform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = new Vector2(8, 4); lrt.offsetMax = new Vector2(-8, -4);
            }
            else
            {
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                var lab = UIFactory.Label(card, "❓ ???\n<size=11>未发现</size>", 14, G.ParseColor("#555"), TextAnchor.MiddleCenter);
                lab.supportRichText = true;
                var lrt = (RectTransform)lab.transform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            }
        }
    }

    void OnEnable() { if (_content != null) Refresh(); }
}
