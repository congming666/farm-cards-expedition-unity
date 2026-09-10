using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================= v0.8.0 NPC + 科技树 UI（Unity版） =================
public class NpcTechUI : MonoBehaviour
{
    static NpcTechUI _i;
    GameObject _npcPanel, _techPanel;
    Text _npcDialogue, _techInfo;

    public static void Sync(string screen) {
        bool show = screen == "farm";
        if (show && _i == null) Create();
        if (_i != null) _i.gameObject.SetActive(show);
    }

    static void Create() {
        var root = UIRoot.Ensure();
        var rt = UIFactory.Rect("NpcTechUI", root.Root);
        UIFactory.Stretch(rt);
        _i = rt.gameObject.AddComponent<NpcTechUI>();
        _i.Build();
    }

    Button MakeBtn(Transform parent, float x, float y, float w, float h, string text, int size, Action onClick) {
        var img = UIFactory.PlacedPanel(parent, "Btn", x, y, w, h, new Color(0.13f, 0.20f, 0.16f, 0.98f));
        var btn = img.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());
        var lbl = UIFactory.PlacedLabel(img.transform, text, size, Color.white, 0, 0, w, h, TextAnchor.MiddleCenter);
        return btn;
    }

    void Build() {
        // 农场右下角两个入口按钮
        MakeBtn(transform, 1050, 640, 110, 50, "👥 NPC", 16, () => OpenNpc());
        MakeBtn(transform, 1170, 640, 110, 50, "🏗️ 科技", 14, () => OpenTech());
        // NPC面板
        var npcBg = UIFactory.PlacedPanel(transform, "NpcPanel", 200, 100, 880, 520, new Color(0.08f, 0.12f, 0.10f, 0.98f));
        _npcPanel = npcBg.gameObject; _npcPanel.SetActive(false);
        UIFactory.PlacedLabel(npcBg.transform, "👥 NPC 访客", 20, Color.yellow, 20, 10, 300, 36);
        MakeBtn(npcBg.transform, 800, 10, 60, 36, "✕", 14, () => _npcPanel.SetActive(false));
        _npcDialogue = UIFactory.PlacedLabel(npcBg.transform, "选择一位 NPC 开始对话", 14, Color.white, 20, 180, 840, 200);
        string[] ids = { "merchant", "farmer", "veteran", "traveler" };
        string[] names = { "🧳 流浪商人", "👴 老农夫", "⚔️ 远征老兵", "🌙 神秘旅人" };
        for (int i = 0; i < 4; i++) {
            string id = ids[i], nm = names[i];
            MakeBtn(npcBg.transform, 20 + i * 210, 80, 200, 50, nm, 13, () => TalkNpc(id));
        }
        // 科技面板
        var techBg = UIFactory.PlacedPanel(transform, "TechPanel", 200, 100, 880, 520, new Color(0.08f, 0.12f, 0.10f, 0.98f));
        _techPanel = techBg.gameObject; _techPanel.SetActive(false);
        UIFactory.PlacedLabel(techBg.transform, "🏗️ 建筑升级 + 科技树", 20, Color.yellow, 20, 10, 400, 36);
        MakeBtn(techBg.transform, 800, 10, 60, 36, "✕", 14, () => _techPanel.SetActive(false));
        _techInfo = UIFactory.PlacedLabel(techBg.transform, "点击建筑升级或查看科技树", 12, Color.white, 20, 140, 840, 300);
        string[] bids = { "workshop", "greenhouse", "barn", "lab" };
        string[] bnames = { "🔨 工坊", "🏠 温室", "🐔 畜舍", "🔬 研究所" };
        for (int i = 0; i < 4; i++) {
            string id = bids[i], nm = bnames[i];
            MakeBtn(techBg.transform, 20 + (i % 2) * 420, 70 + (i / 2) * 55, 400, 45, nm, 13, () => UpgradeBld(id));
        }
        string[] tids = { "agriculture", "combat", "survival" };
        string[] tnames = { "🌾 农业", "⚔️ 战斗", "🔥 生存" };
        for (int i = 0; i < 3; i++) {
            string id = tids[i], nm = tnames[i];
            MakeBtn(techBg.transform, 20 + i * 280, 190, 260, 40, nm, 13, () => ShowTech(id));
        }
    }

    void OpenNpc() { NpcSystem.Init(); _npcPanel.SetActive(true); _techPanel.SetActive(false); _npcDialogue.text = "选择一位 NPC 开始对话"; }
    void OpenTech() { TechSystem.Init(); _techPanel.SetActive(true); _npcPanel.SetActive(false); _techInfo.text = "🔬 科技点：" + GameState.techPoints + "\n点击建筑升级，或查看科技树"; }
    void TalkNpc(string id) {
        NpcSystem.AddAffection(id, 2);
        string dlg = NpcSystem.GetDialogue(id);
        var st = NpcSystem.GetState(id);
        _npcDialogue.text = dlg + "\n\n好感度：" + st.affection + "/100\n（对话+2好感，送礼可提升）";
    }
    void UpgradeBld(string id) {
        if (TechSystem.UpgradeBuilding(id)) _techInfo.text = "升级成功！科技点：" + GameState.techPoints;
        else _techInfo.text = "资源不足或已满级";
    }
    void ShowTech(string treeId) {
        var tree = TechSystem.TechTrees[treeId];
        string txt = "【" + tree.name + "科技树】 科技点：" + GameState.techPoints + "\n\n";
        foreach (var n in tree.nodes) {
            bool unlocked = TechSystem.IsTechUnlocked(n.id);
            bool can = TechSystem.CanUnlockTech(n.id);
            txt += (unlocked ? "✅ " : can ? "🔓 " : "🔒 ") + n.name + "：" + n.desc + "（" + n.cost + "点）\n";
        }
        txt += "\n点击 🔓 可解锁的科技";
        _techInfo.text = txt;
    }
}
