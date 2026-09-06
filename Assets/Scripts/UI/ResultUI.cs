using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================= 远征结算页（UGUI，替代 UIHost.DrawResult） =================
public class ResultUI : MonoBehaviour
{
    static ResultUI _i;
    Text _title, _subtitle;
    Text[] _stats = new Text[6];
    RectTransform _lootHost;
    string _lootSig = "";

    public static void Sync(string screen)
    {
        bool show = screen == "result";
        if (show) { if (_i == null) Create(); if (_i != null && !_i.gameObject.activeSelf) _i.gameObject.SetActive(true); }
        else if (_i != null && _i.gameObject.activeSelf) _i.gameObject.SetActive(false);
    }
    static void Create()
    {
        var root = UIRoot.Ensure();
        var rt = UIFactory.Rect("ResultUI", root.Root); UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<ResultUI>(); _i.Build();
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
        UIFactory.PlacedPanel(transform, "Bg", 0, 0, 1280, 720, new Color(0f, 0f, 0f, 0.85f)).raycastTarget = true;
        _title = Lab(0, 60, 1280, 70, "", 42, Color.white, TextAnchor.MiddleCenter);
        _title.fontStyle = FontStyle.Bold;
        _subtitle = Lab(0, 130, 1280, 28, "", 18, G.ParseColor("#aaccaa"), TextAnchor.MiddleCenter);
        UIFactory.PlacedPanel(transform, "Panel", 340, 180, 600, 280, new Color(0f, 0f, 0f, 0.5f));
        _stats[0] = Lab(360, 190, 300, 24, "", 15, Color.white);
        _stats[1] = Lab(660, 190, 300, 24, "", 15, Color.white);
        _stats[2] = Lab(360, 220, 300, 24, "", 15, Color.white);
        _stats[3] = Lab(660, 220, 300, 24, "", 15, Color.white);
        _stats[4] = Lab(360, 250, 300, 24, "", 15, Color.white);
        _stats[5] = Lab(660, 250, 300, 24, "", 15, Color.white);
        Lab(360, 284, 560, 22, "—— 战利品清单 ——", 14, Color.white, TextAnchor.MiddleCenter);
        _lootHost = UIFactory.Rect("Loot", transform);
        Btn(500, 500, 280, 54, "返回农场", 20, () => { if (GameFlow.I != null) GameFlow.I.ReturnToFarm(); },
            new Color(0.14f, 0.30f, 0.20f, 1f));
    }

    void Update()
    {
        if (!gameObject.activeSelf || GameFlow.I == null) return;
        var rd = GameFlow.I.PeekResult(); if (rd == null) return;
        _title.text = rd.success ? "远征成功！" : "远征失败...";
        _title.color = rd.success ? G.ParseColor("#7fff7f") : G.ParseColor("#ff6644");
        Set(_subtitle, rd.subtitle);
        Set(_stats[0], "用时 " + rd.timeUsed + "秒");
        Set(_stats[1], "击杀数 " + rd.kills);
        Set(_stats[2], "开启宝箱 " + rd.chests);
        Set(_stats[3], "获得金币 +" + rd.goldEarned);
        Set(_stats[4], "受到伤害 " + rd.damageTaken);
        Set(_stats[5], "地图 " + rd.mapName);
        RebuildLoot(rd);
    }

    void RebuildLoot(GameFlow.ResultData rd)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var i in rd.kept) sb.Append('k').Append(i.name).Append(i.amount);
        foreach (var i in rd.lost) sb.Append('l').Append(i.name).Append(i.amount);
        string sig = sb.ToString();
        if (sig == _lootSig) return; _lootSig = sig;
        for (int i = _lootHost.childCount - 1; i >= 0; i--) Destroy(_lootHost.GetChild(i).gameObject);
        float ly = 312;
        foreach (var i in rd.kept) { Lab(380, ly, 500, 20, i.icon + " " + i.name + " ×" + i.amount + "   ✓ 保留", 13, G.ParseColor("#7fff7f")); ly += 22; }
        foreach (var i in rd.lost) { Lab(380, ly, 500, 20, i.icon + " " + i.name + " ×" + i.amount + "   ✗ 丢失", 13, G.ParseColor("#ff6644")); ly += 22; }
        if (rd.kept.Count == 0 && rd.lost.Count == 0) Lab(380, 312, 500, 20, "本次远征没有获得物资", 12, G.ParseColor("#aeb8ae"));
    }
    static void Set(Text t, string s) { if (t != null && t.text != s) t.text = s; }
}
