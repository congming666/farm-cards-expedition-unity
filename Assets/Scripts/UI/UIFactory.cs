using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
// 方法名与 UGUI 类型同名，用别名避免内部解析到方法
using UIButton = UnityEngine.UI.Button;
using UISlider = UnityEngine.UI.Slider;
using UIToggle = UnityEngine.UI.Toggle;

// ================= 运行时 UGUI 控件工厂（Legacy uGUI，统一字体/配色/尺寸） =================
public static class UIFactory
{
    static Font Font { get { return FontLoader.Main; } }
    public static readonly Color Accent = new Color(0.545f, 0.784f, 0.918f, 1f);
    public static readonly Color PanelBg = new Color(0.15f, 0.17f, 0.21f, 1f);
    public static readonly Color TextMain = new Color(0.93f, 0.94f, 0.96f, 1f);
    public static readonly Color TextDim = new Color(0.62f, 0.66f, 0.72f, 1f);

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    // 旧 IMGUI 以 1280x720、左上角为原点；UGUI 参考分辨率 1920x1080（=1.5 倍）。
    // 直接喂旧坐标即可 1:1 还原布局，内部锚定左下角。
    public static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f); rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(x * 1.5f, (720f - y - h) * 1.5f);
        rt.sizeDelta = new Vector2(w * 1.5f, h * 1.5f);
    }

    // 锚定左下角的纯色面板（旧坐标）
    public static Image PlacedPanel(Transform parent, string name, float x, float y, float w, float h, Color bg)
    {
        var img = Panel(parent, name, bg);
        Place((RectTransform)img.transform, x, y, w, h);
        return img;
    }

    // 锚定左下角的文本（旧坐标）
    public static Text PlacedLabel(Transform parent, string text, int legacySize, Color color, float x, float y, float w, float h, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var t = Label(parent, text, Mathf.RoundToInt(legacySize * 1.5f), color, align);
        Place((RectTransform)t.transform, x, y, w, h);
        return t;
    }

    public static Image Panel(Transform parent, string name, Color bg)
    {
        var rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = bg;
        return img;
    }

    public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var rt = Rect("Label", parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false; t.supportRichText = false;
        return t;
    }

    public static UIButton Button(Transform parent, string text, Action onClick, int fontSize = 26, Color? bg = null)
    {
        var rt = Rect("Btn_" + text, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = bg ?? new Color(0.24f, 0.28f, 0.35f, 1f);
        var btn = rt.gameObject.AddComponent<UIButton>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.highlightedColor = new Color(1.05f, 1.08f, 1.12f, 1f);
        c.pressedColor = new Color(0.75f, 0.85f, 0.95f, 1f);
        c.fadeDuration = 0.06f;
        btn.colors = c;
        var lab = Label(rt, text, fontSize, TextMain, TextAnchor.MiddleCenter);
        Stretch((RectTransform)lab.transform);
        if (onClick != null) btn.onClick.AddListener(() => onClick());
        return btn;
    }

    public static UISlider Slider(Transform parent, float min, float max, bool whole, float value, UnityAction<float> onChanged)
    {
        var rt = Rect("Slider", parent);
        var bg = Panel(rt, "Background", new Color(0.10f, 0.12f, 0.15f, 1f));
        Stretch((RectTransform)bg.transform);
        var fillArea = Rect("Fill Area", rt);
        fillArea.anchorMin = Vector2.zero; fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(10, 10); fillArea.offsetMax = new Vector2(-10, -10);
        var fill = (RectTransform)Panel(fillArea, "Fill", Accent).transform;
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero; fill.offsetMax = Vector2.zero;
        var handleArea = Rect("Handle Slide Area", rt); Stretch(handleArea);
        var handle = (RectTransform)Panel(handleArea, "Handle", Color.white).transform;
        handle.sizeDelta = new Vector2(22, 0);
        var slider = rt.gameObject.AddComponent<UISlider>();
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fill; slider.handleRect = handle;
        slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = whole;
        slider.direction = UISlider.Direction.LeftToRight;
        slider.value = Mathf.Clamp(value, min, max);
        if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
        return slider;
    }

    public static UIToggle Toggle(Transform parent, string labelText, bool isOn, UnityAction<bool> onChanged)
    {
        var rt = Rect("Toggle", parent);
        var box = (RectTransform)Panel(rt, "Box", new Color(0.14f, 0.16f, 0.20f, 1f)).transform;
        box.anchorMin = new Vector2(0, 0.5f); box.anchorMax = new Vector2(0, 0.5f); box.pivot = new Vector2(0, 0.5f);
        box.sizeDelta = new Vector2(36, 36); box.anchoredPosition = new Vector2(2, 0);
        var check = (RectTransform)Panel(box, "Check", Accent).transform;
        Stretch(check); check.offsetMin = new Vector2(6, 6); check.offsetMax = new Vector2(-6, -6);
        var lab = Label(rt, labelText, 24, TextMain);
        var lrt = (RectTransform)lab.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(50, 0); lrt.offsetMax = Vector2.zero;
        var tog = rt.gameObject.AddComponent<UIToggle>();
        tog.targetGraphic = box.GetComponent<Image>(); tog.graphic = check.GetComponent<Image>();
        tog.isOn = isOn;
        if (onChanged != null) tog.onValueChanged.AddListener(onChanged);
        return tog;
    }

    // 一行设置项：左侧固定标题，右侧弹性控件区
    public static RectTransform Row(Transform parent, string title, float height, out RectTransform control)
    {
        var row = Rect("Row", parent);
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.minHeight = height; le.preferredHeight = height;
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 14; h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = true; h.childForceExpandHeight = false;

        var titleText = Label(row, title, 25, TextDim);
        var tle = titleText.gameObject.AddComponent<LayoutElement>();
        tle.preferredWidth = 250; tle.flexibleWidth = 0; tle.minHeight = height;

        control = Rect("Control", row);
        var cle = control.gameObject.AddComponent<LayoutElement>();
        cle.flexibleWidth = 1; cle.minHeight = height;
        return row;
    }

    // 左右步进选择器（比 legacy Dropdown 模板更稳、也更适合手柄）
    public static Text Stepper(Transform parent, string[] options, int index, UnityAction<int> onSelect)
    {
        var holder = Rect("Stepper", parent);
        var hg = holder.gameObject.AddComponent<HorizontalLayoutGroup>();
        hg.spacing = 10; hg.childAlignment = TextAnchor.MiddleCenter;
        hg.childControlWidth = true; hg.childControlHeight = true;
        hg.childForceExpandWidth = true; hg.childForceExpandHeight = true;
        int cur = Mathf.Clamp(index, 0, options.Length - 1);

        var left = Button(holder, "<", null, 30);
        var value = Label(holder, options[cur], 24, TextMain, TextAnchor.MiddleCenter);
        var ve = value.gameObject.AddComponent<LayoutElement>(); ve.flexibleWidth = 1.4f;
        var right = Button(holder, ">", null, 30);
        foreach (var b in new[] { left, right })
        {
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 56; le.flexibleWidth = 0;
        }
        void Refresh() { value.text = options[Mathf.Clamp(cur, 0, options.Length - 1)]; }
        left.onClick.AddListener(() => { cur = (cur - 1 + options.Length) % options.Length; Refresh(); onSelect(cur); });
        right.onClick.AddListener(() => { cur = (cur + 1) % options.Length; Refresh(); onSelect(cur); });
        Refresh();
        return value;
    }
}
