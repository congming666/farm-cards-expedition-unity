using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================= 全局覆盖层（UGUI，替代 OnGUI 的 toast / 掉落横幅 / 受击红屏 / 世界伤害跳字 / 交互提示） =================
// 常驻不随屏幕隐藏；由 UIHost.DrawUI 每帧调用 Refresh(gf)。
public class GlobalOverlay : MonoBehaviour
{
    static GlobalOverlay _i;
    Image _flash;
    readonly Text[] _toasts = new Text[5];
    readonly Image[] _dropBg = new Image[8];
    readonly Text[] _dropText = new Text[8];
    readonly Text[] _dmg = new Text[80];
    Text _interact;

    public static void Ensure()
    {
        if (_i != null) return;
        var root = UIRoot.Ensure();
        var rt = UIFactory.Rect("GlobalOverlay", root.Root); UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<GlobalOverlay>(); _i.Build();
    }

    void Build()
    {
        // 受击红屏（全屏，默认透明）
        var flashRt = UIFactory.Rect("Flash", transform); UIFactory.Stretch(flashRt);
        _flash = flashRt.gameObject.AddComponent<Image>();
        _flash.color = new Color(1f, 0.34f, 0.24f, 0f); _flash.raycastTarget = false;

        for (int i = 0; i < _toasts.Length; i++)
        {
            _toasts[i] = UIFactory.PlacedLabel(transform, "", 15, Color.white, 0, 84 + i * 32, 1280, 30, TextAnchor.MiddleCenter);
            _toasts[i].raycastTarget = false;
        }
        for (int i = 0; i < _dropBg.Length; i++)
        {
            _dropBg[i] = UIFactory.PlacedPanel(transform, "DropBg", 960, 90 + i * 58, 220, 52, Color.white);
            _dropBg[i].raycastTarget = false; _dropBg[i].gameObject.SetActive(false);
            _dropText[i] = UIFactory.PlacedLabel(transform, "", 13, Color.white, 960, 90 + i * 58, 220, 52, TextAnchor.MiddleCenter);
            _dropText[i].raycastTarget = false; _dropText[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < _dmg.Length; i++)
        {
            _dmg[i] = UIFactory.PlacedLabel(transform, "", 14, Color.white, 0, 0, 240, 30, TextAnchor.MiddleCenter);
            _dmg[i].fontStyle = FontStyle.Bold; _dmg[i].raycastTarget = false; _dmg[i].gameObject.SetActive(false);
        }
        _interact = UIFactory.PlacedLabel(transform, "", 13, G.ParseColor("#ffd700"), 0, 0, 280, 36, TextAnchor.MiddleCenter);
        _interact.fontStyle = FontStyle.Bold; _interact.raycastTarget = false; _interact.gameObject.SetActive(false);
    }

    public static void Refresh(GameFlow gf)
    {
        Ensure();
        if (_i == null || gf == null) return;
        _i.DoRefresh(gf);
    }

    void DoRefresh(GameFlow gf)
    {
        // toast
        int ti = 0;
        foreach (var t in gf.toasts)
        {
            if (ti >= _toasts.Length) break;
            float alpha = Mathf.Max(0, Mathf.Min(1, t.life / 0.4f));
            Color c = t.type == "warning" ? G.ParseColor("#ff8866") : t.type == "gold" ? G.ParseColor("#ffd700") : t.type == "success" ? G.ParseColor("#99ff99") : Color.white;
            c.a = alpha;
            _toasts[ti].text = t.msg; _toasts[ti].color = c; _toasts[ti].gameObject.SetActive(true);
            ti++;
        }
        for (; ti < _toasts.Length; ti++) _toasts[ti].gameObject.SetActive(false);

        // 掉落横幅
        int di = 0;
        foreach (var b in gf.dropBanners)
        {
            if (di >= _dropBg.Length) break;
            float alpha = Mathf.Max(0, Mathf.Min(1, b.life / 0.4f));
            Color c = b.card.rarity == "legendary" ? G.ParseColor("#ee9637") : b.card.rarity == "rare" ? G.ParseColor("#55aaf1") : Color.white;
            c.a = alpha;
            _dropBg[di].color = c; _dropBg[di].gameObject.SetActive(true);
            _dropText[di].text = "收获掉落 · " + b.card.rarity + "\n" + b.card.icon + " " + b.card.name;
            _dropText[di].color = c; _dropText[di].gameObject.SetActive(true);
            di++;
        }
        for (; di < _dropBg.Length; di++) { _dropBg[di].gameObject.SetActive(false); _dropText[di].gameObject.SetActive(false); }

        // 受击红屏
        var fc = _flash.color; fc.a = gf.signalFlash * 0.72f; _flash.color = fc;

        // 世界伤害跳字 + 交互提示（仅远征）
        bool inExp = gf.screen == "expedition" && gf.current != null && Camera.main != null;
        if (!inExp)
        {
            for (int i = 0; i < _dmg.Length; i++) _dmg[i].gameObject.SetActive(false);
            _interact.gameObject.SetActive(false);
            return;
        }
        float scale = UIRoot.Ensure().Canvas.scaleFactor;
        float S = WorldRenderer.S;
        int dmgIdx = 0;
        foreach (var d in gf.current.damageNumbers)
        {
            if (dmgIdx >= _dmg.Length) break;
            var v = Camera.main.WorldToScreenPoint(new Vector3(d.x * S, 0.6f, d.y * S));
            if (v.z <= 0) { _dmg[dmgIdx].gameObject.SetActive(false); dmgIdx++; continue; }
            var t = _dmg[dmgIdx];
            t.fontSize = Mathf.RoundToInt((d.heavy ? 20 : 14) * 1.5f);
            t.color = G.ParseColor(d.color);
            t.text = "-" + d.value;
            var rt = (RectTransform)t.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(v.x / scale, v.y / scale);
            rt.sizeDelta = new Vector2(240 * 1.5f, 30 * 1.5f);
            t.gameObject.SetActive(true);
            dmgIdx++;
        }
        for (; dmgIdx < _dmg.Length; dmgIdx++) _dmg[dmgIdx].gameObject.SetActive(false);

        // 交互提示
        var ip = gf.InteractPrompt();
        if (ip.HasValue)
        {
            var v = Camera.main.WorldToScreenPoint(new Vector3(ip.Value.x * S, 1.2f, ip.Value.y * S));
            if (v.z > 0)
            {
                _interact.text = ip.Value.text;
                var rt = (RectTransform)_interact.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(v.x / scale, v.y / scale);
                rt.sizeDelta = new Vector2(280 * 1.5f, 36 * 1.5f);
                _interact.gameObject.SetActive(true);
            }
            else _interact.gameObject.SetActive(false);
        }
        else _interact.gameObject.SetActive(false);
    }
}
