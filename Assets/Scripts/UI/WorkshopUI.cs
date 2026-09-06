using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================= 卡牌工坊（UGUI overlay，替代 UIHost.DrawWorkshop） =================
public class WorkshopUI : MonoBehaviour
{
    static WorkshopUI _i;
    readonly List<Text> _skillName = new List<Text>();
    readonly List<Text> _skillInfo = new List<Text>();
    RectTransform _cardHost;
    string _cardSig = "";

    public static void Sync(bool open)
    {
        if (open) { if (_i == null) Create(); if (_i != null && !_i.gameObject.activeSelf) _i.gameObject.SetActive(true); }
        else if (_i != null && _i.gameObject.activeSelf) _i.gameObject.SetActive(false);
    }
    static void Create()
    {
        var root = UIRoot.Ensure();
        var rt = UIFactory.Rect("WorkshopUI", root.Root); UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<WorkshopUI>(); _i.Build();
    }

    Text Lab(float x, float y, float w, float h, string t, int ls, Color c, TextAnchor a = TextAnchor.MiddleLeft)
        => UIFactory.PlacedLabel(transform, t, ls, c, x, y, w, h, a);

    void Build()
    {
        UIFactory.PlacedPanel(transform, "Bg", 0, 0, 1280, 720, new Color(0f, 0f, 0f, 0.7f));
        UIFactory.PlacedPanel(transform, "Box", 300, 40, 680, 640, new Color(0.08f, 0.10f, 0.12f, 0.95f));
        Lab(320, 56, 640, 32, "卡牌工坊", 24, Color.white);
        Lab(320, 92, 640, 24, "收获作物有概率掉落强化卡，使用后永久提升基础技能。", 12, G.ParseColor("#aeb8ae"));
        float sy = 130;
        foreach (var sk in GameData.Skills)
        {
            _skillName.Add(Lab(340, sy, 300, 22, "", 16, Color.white));
            _skillInfo.Add(Lab(340, sy + 20, 300, 16, "", 12, G.ParseColor("#aeb8ae")));
            sy += 40;
        }
        Lab(320, sy + 6, 640, 22, "—— 作物卡牌库存 ——", 14, Color.white, TextAnchor.MiddleCenter);
        _cardHost = UIFactory.Rect("Cards", transform);
        var close = UIFactory.Button(transform, "关闭", () => { UIHost.workshopOpen = false; }, Mathf.RoundToInt(14 * 1.5f));
        UIFactory.Place((RectTransform)close.transform, 560, 620, 160, 40);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;
        for (int i = 0; i < _skillName.Count && i < GameData.Skills.Length; i++)
        {
            var sk = GameData.Skills[i];
            int lv = GameState.skillLevels.ContainsKey(sk.id) ? GameState.skillLevels[sk.id] : 1;
            var stats = SkillMath.GetStats(sk);
            string power = stats.damage > 0 ? ("伤害 " + stats.damage) : stats.stunDuration > 0 ? ("控制 " + stats.stunDuration + "s")
                : stats.dashDistance > 0 ? ("位移 " + stats.dashDistance) : ("隐身 " + stats.stealthDuration + "s");
            Set(_skillName[i], sk.icon + " " + sk.name + "  Lv." + lv);
            Set(_skillInfo[i], power + " · 能量 " + stats.energyCost + " · CD " + stats.cooldown + "s");
        }
        RebuildCards();
    }

    void RebuildCards()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in GameState.cardInventory) sb.Append(c.id).Append('|');
        string sig = sb.ToString();
        if (sig == _cardSig) return; _cardSig = sig;
        for (int i = _cardHost.childCount - 1; i >= 0; i--) Destroy(_cardHost.GetChild(i).gameObject);
        if (GameState.cardInventory.Count == 0)
        {
            UIFactory.PlacedLabel(_cardHost, "卡牌库存为空。收获作物有概率掉落强化卡。", 13, Color.white, 320, 160, 640, 24, TextAnchor.MiddleCenter);
            return;
        }
        int count = 0;
        foreach (var card in GameState.cardInventory)
        {
            float bx = 340 + (count % 4) * 150, by = 160 + (count / 4) * 86;
            string cid = card.id;
            var b = UIFactory.Button(_cardHost, "", () => CardSystem.Apply(cid), Mathf.RoundToInt(11 * 1.5f),
                new Color(0.14f, 0.12f, 0.20f, 0.98f));
            UIFactory.Place((RectTransform)b.transform, bx, by, 138, 74);
            var sk = SaveSystem.SkillById(card.skillId);
            var t = b.GetComponentInChildren<Text>();
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.color = card.rarity == "legendary" ? G.ParseColor("#ee9637") : card.rarity == "rare" ? G.ParseColor("#55aaf1") : Color.white;
            t.text = card.icon + " " + (sk != null ? sk.name : card.skillId) + " +" + card.power + "\n" + card.rarity + "\n点击使用";
            count++;
        }
    }
    static void Set(Text t, string s) { if (t != null && t.text != s) t.text = s; }
}
