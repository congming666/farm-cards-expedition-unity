using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================= 设置菜单（UGUI，运行时生成） =================
// 分辨率 / 全屏 / 垂直同步 / 画质 / 帧率上限 / 主·音乐·音效音量 / 辅助开关。
// 控件改动即时写入 GameSettings 并 Apply；F10 全局呼出/关闭（避开游戏内 Esc 暂停键）。
public class SettingsPanel : MonoBehaviour
{
    static SettingsPanel _instance;
    GameObject _content;
    float _prevTimeScale = 1f;
    bool _open;
    public bool IsOpen { get { return _open; } }
    public static bool IsOpenAny { get { return _instance != null && _instance._open; } }

    public static void ToggleInstance()
    {
        if (_instance == null)
        {
            var ui = UIRoot.Ensure();
            var go = UIFactory.Rect("SettingsPanel", ui.Root).gameObject;
            _instance = go.AddComponent<SettingsPanel>();
            _instance.Build();
        }
        _instance.Toggle();
    }

    public static void CloseInstance(){ if(_instance!=null && _instance._open) _instance.Close(); }

    void Build()
    {
        // 全屏遮罩（吃掉背后点击）
        var dim = gameObject.AddComponent<Image>();
        dim.color = new Color(0, 0, 0, 0.55f);
        UIFactory.Stretch((RectTransform)transform);

        // 居中面板
        var panel = (RectTransform)UIFactory.Panel(transform, "Panel", new Color(0.115f, 0.13f, 0.165f, 0.985f)).transform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(900, 0);
        panel.anchoredPosition = Vector2.zero;
        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 26, 26);
        vlg.spacing = 12; vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var title = UIFactory.Label(panel, "设置  (F10 关闭)", 38, UIFactory.TextMain, TextAnchor.MiddleCenter);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;

        _content = UIFactory.Rect("Content", panel).gameObject;
        var cvlg = _content.AddComponent<VerticalLayoutGroup>();
        cvlg.spacing = 10; cvlg.childAlignment = TextAnchor.UpperCenter;
        cvlg.childControlWidth = true; cvlg.childControlHeight = true;
        cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false;
        var cle = _content.AddComponent<ContentSizeFitter>(); cle.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _content.AddComponent<LayoutElement>().flexibleWidth = 1;

        BuildContent(_content.transform);

        // 底部按钮行
        var footer = UIFactory.Rect("Footer", panel);
        footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
        var fh = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        fh.spacing = 14; fh.childControlWidth = true; fh.childControlHeight = true;
        fh.childForceExpandWidth = true; fh.childForceExpandHeight = true;
        UIFactory.Button(footer, "恢复默认", Rebuild, 26, new Color(0.35f, 0.27f, 0.22f, 1f));
        UIFactory.Button(footer, "应用并保存", () => { GameSettings.Apply(); GameSettings.Save(); }, 26, new Color(0.22f, 0.38f, 0.30f, 1f));
        UIFactory.Button(footer, "关闭", Close, 26);
        UIFactory.Button(footer, "退出游戏", QuitGame, 26, new Color(0.40f, 0.20f, 0.20f, 1f));

        gameObject.SetActive(false);
    }

    void BuildContent(Transform t)
    {
        var d = GameSettings.Current;

        // —— 分辨率 ——
        var resList = GameSettings.CommonResolutions();
        var labels = new List<string>(); var ws = new List<int>(); var hs = new List<int>();
        labels.Add("桌面原生"); ws.Add(0); hs.Add(0);
        int sel = 0;
        for (int i = 0; i < resList.Count; i++)
        {
            var r = resList[i];
            labels.Add(r.width + " × " + r.height);
            ws.Add(r.width); hs.Add(r.height);
            if (r.width == d.width && r.height == d.height) sel = i + 1;
        }
        var resCtrl = UIFactory.Row(t, "分辨率", 54, out var rc);
        UIFactory.Stepper(rc, labels.ToArray(), sel, i => {
            d.width = ws[i]; d.height = hs[i]; GameSettings.Apply();
        });
        UIFactory.Stretch(rc);

        // —— 全屏模式 ——
        string[] fsLabels = { "独占全屏", "全屏窗口", "最大化窗口", "窗口化" };
        var fsCtrl = UIFactory.Row(t, "显示模式", 54, out var fc);
        UIFactory.Stepper(fc, fsLabels, d.fullScreenMode, i => { d.fullScreenMode = i; GameSettings.Apply(); });
        UIFactory.Stretch(fc);

        // —— 垂直同步 ——
        var vsCtrl = UIFactory.Row(t, "垂直同步 VSync", 54, out var vc);
        UIFactory.Toggle(vc, "开启（关闭后可锁帧）", d.vSync, v => { d.vSync = v; GameSettings.Apply(); });
        UIFactory.Stretch(vc);

        // —— 帧率上限 ——
        string[] fpsLabels = { "30", "60", "120", "144", "不限" };
        int[] fpsVals = { 30, 60, 120, 144, -1 };
        int fpsSel = 1; for (int i = 0; i < fpsVals.Length; i++) if (fpsVals[i] == d.targetFrameRate) fpsSel = i;
        var fpsCtrl = UIFactory.Row(t, "帧率上限（关 VSync 生效）", 54, out var fpc);
        UIFactory.Stepper(fpc, fpsLabels, fpsSel, i => { d.targetFrameRate = fpsVals[i]; GameSettings.Apply(); });
        UIFactory.Stretch(fpc);

        // —— 画质 ——
        var qNames = QualitySettings.names;
        var qLabels = new List<string> { "自动（工程默认）" };
        for (int i = 0; i < qNames.Length; i++) qLabels.Add(qLabels.Count + ". " + qNames[i]);
        int qSel = d.qualityLevel < 0 ? 0 : d.qualityLevel + 1;
        qSel = Mathf.Clamp(qSel, 0, qLabels.Count - 1);
        var qCtrl = UIFactory.Row(t, "画质等级", 54, out var qc);
        UIFactory.Stepper(qc, qLabels.ToArray(), qSel, i => { d.qualityLevel = i == 0 ? -1 : i - 1; GameSettings.Apply(); });
        UIFactory.Stretch(qc);

        // —— 音量 ——
        VolumeRow(t, "主音量", d.masterVolume, v => { d.masterVolume = v; AudioListener.volume = v; });
        VolumeRow(t, "音乐音量", d.musicVolume, v => { d.musicVolume = v; });
        VolumeRow(t, "音效音量", d.sfxVolume, v => { d.sfxVolume = v; });

        // —— 辅助 ——
        var dn = UIFactory.Row(t, "显示伤害数字", 54, out var dnc);
        UIFactory.Toggle(dnc, "开启", d.showDamageNumbers, v => d.showDamageNumbers = v); UIFactory.Stretch(dnc);
        var ss = UIFactory.Row(t, "屏幕震动", 54, out var ssc);
        UIFactory.Toggle(ssc, "开启", d.screenShake, v => d.screenShake = v); UIFactory.Stretch(ssc);
    }

    void VolumeRow(Transform parent, string title, float value, System.Action<float> onChanged)
    {
        var ctrl = UIFactory.Row(parent, title, 54, out var c);
        var slider = UIFactory.Slider(c, 0f, 1f, false, value, v => onChanged(v));
        UIFactory.Stretch((RectTransform)slider.transform);
        var pct = UIFactory.Label(ctrl, Mathf.RoundToInt(value * 100) + "%", 22, UIFactory.TextDim, TextAnchor.MiddleRight);
        var le = pct.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 64; le.flexibleWidth = 0;
        slider.onValueChanged.AddListener(v => pct.text = Mathf.RoundToInt(v * 100) + "%");
    }

    void Rebuild()
    {
        GameSettings.ResetToDefault();
        for (int i = _content.transform.childCount - 1; i >= 0; i--)
            Destroy(_content.transform.GetChild(i).gameObject);
        BuildContent(_content.transform);
    }

    public void Toggle() { if (_open) Close(); else Open(); }

    public void Open()
    {
        _open = true;
        gameObject.SetActive(true);
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f; // 打开设置时暂停模拟，UI 输入使用 unscaled 时间不受影响
    }

    public void Close()
    {
        _open = false;
        gameObject.SetActive(false);
        Time.timeScale = _prevTimeScale > 0f ? _prevTimeScale : 1f;
        GameSettings.Save();
    }

    void QuitGame()
    {
        GameSettings.Save();
        SaveSystem.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
