using System;
using UnityEngine;
using UnityEngine.UI;

// ================= 加工工坊面板（UGUI 运行时弹窗，作物→产品加工链） =================
public class ProcessingUI : MonoBehaviour
{
    static ProcessingUI _i;
    Text _title;
    GameObject _content;
    GameObject _queuePanel;

    public static void Toggle()
    {
        if (_i == null) Create();
        if (_i != null) _i.gameObject.SetActive(!_i.gameObject.activeSelf);
    }

    static void Create()
    {
        var root = UIRoot.Ensure();
        var rt = UIFactory.Rect("ProcessingUI", root.Root);
        UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<ProcessingUI>();
        _i.Build();
        _i.gameObject.SetActive(false);
    }

    void Build()
    {
        var mask = UIFactory.PlacedPanel(transform, "Mask", 0, 0, 1280, 720, new Color(0f, 0f, 0f, 0.7f));
        mask.raycastTarget = true;

        var panel = UIFactory.PlacedPanel(transform, "Panel", 240, 80, 800, 560, new Color(0.08f, 0.10f, 0.14f, 0.98f));
        var prt = panel.rectTransform;

        _title = UIFactory.PlacedLabel(transform, "🏭 加工工坊 Lv." + GameState.workshopLevel, 24, G.ParseColor("#ffd700"), 260, 600, 400, 40, TextAnchor.MiddleCenter);

        var close = UIFactory.Button(transform, "✕", () => gameObject.SetActive(false), 20, new Color(0.3f, 0.15f, 0.15f, 0.98f));
        UIFactory.Place((RectTransform)close.transform, 1000, 600, 40, 40);

        _content = UIFactory.Rect("Content", prt).gameObject;
        UIFactory.Place((RectTransform)_content.transform, 260, 140, 760, 340);
        var vlg = _content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

        RefreshRecipes();

        UIFactory.PlacedLabel(transform, "加工队列：", 14, G.ParseColor("#aaccaa"), 260, 500, 200, 24, TextAnchor.MiddleLeft);
        _queuePanel = UIFactory.Rect("Queue", prt).gameObject;
        UIFactory.Place((RectTransform)_queuePanel.transform, 260, 470, 760, 50);

        var upg = UIFactory.Button(transform, "升级工坊 (" + GameState.workshopLevel * 100 + "金)", () =>
        {
            FarmProcessingSystem.UpgradeWorkshop();
            RefreshRecipes();
            _title.text = "🏭 加工工坊 Lv." + GameState.workshopLevel;
        }, 16, new Color(0.2f, 0.25f, 0.15f, 0.98f));
        UIFactory.Place((RectTransform)upg.transform, 260, 560, 200, 40);
    }

    void RefreshRecipes()
    {
        for (int i = _content.transform.childCount - 1; i >= 0; i--)
            Destroy(_content.transform.GetChild(i).gameObject);

        foreach (var recipe in FarmProcessingSystem.Recipes)
        {
            bool locked = recipe.workshopLevel > GameState.workshopLevel;
            var row = UIFactory.Rect("Row_" + recipe.id, _content.transform);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 42;

            var bg = row.gameObject.AddComponent<Image>();
            bg.color = locked ? new Color(0.15f, 0.15f, 0.15f, 0.8f) : new Color(0.12f, 0.18f, 0.14f, 0.9f);

            var label = UIFactory.Label(row, recipe.icon + " " + recipe.name + " (" + recipe.inputQty + " " + recipe.inputCrop + " → " + recipe.outputQty + " " + recipe.outputName + ") " + recipe.time + "s" + (locked ? " 🔒需Lv." + recipe.workshopLevel : ""), 13, locked ? G.ParseColor("#666") : Color.white, TextAnchor.MiddleLeft);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = new Vector2(10, 0); lrt.offsetMax = new Vector2(-120, 0);

            if (!locked)
            {
                var btn = UIFactory.Button(row, "加工", () => { FarmProcessingSystem.StartProcessing(recipe.id); }, 13, new Color(0.15f, 0.30f, 0.20f, 0.98f));
                var brt = (RectTransform)btn.transform;
                brt.anchorMin = new Vector2(1, 0); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(1, 0.5f);
                brt.sizeDelta = new Vector2(100, 0); brt.anchoredPosition = new Vector2(-5, 0);
            }
        }
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;
        for (int i = _queuePanel.transform.childCount - 1; i >= 0; i--)
            Destroy(_queuePanel.transform.GetChild(i).gameObject);

        float x = 0;
        foreach (var job in GameState.processingQueue)
        {
            var recipe = FarmProcessingSystem.GetRecipe(job.recipeId);
            if (recipe == null) continue;
            var item = UIFactory.Rect("Job", _queuePanel.transform);
            UIFactory.Place(item, x, 5, 140, 40);
            var bg = item.gameObject.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.18f, 0.10f, 0.9f);
            var lab = UIFactory.Label(item, recipe.icon + " " + recipe.name + " " + job.remaining.ToString("F0") + "s", 11, G.ParseColor("#ffd700"), TextAnchor.MiddleCenter);
            var lrt = (RectTransform)lab.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            x += 150;
        }
    }
}
