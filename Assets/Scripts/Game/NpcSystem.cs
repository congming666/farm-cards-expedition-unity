using System;
using System.Collections.Generic;
using UnityEngine;

// ================= v0.8.0 NPC + 剧情线系统（Unity版） =================
public static class NpcSystem
{
    [Serializable]
    public class NpcDef {
        public string id, name, icon, description;
        public int baseAffection;
        public Dictionary<string, int> gifts = new Dictionary<string, int>();
        public List<AffReward> rewards = new List<AffReward>();
    }
    [Serializable]
    public class AffReward { public int aff; public string desc, type; }
    [Serializable]
    public class NpcState {
        public int affection;
        public List<string> unlockedLore = new List<string>();
        public List<string> completedQuests = new List<string>();
        public string activeQuest;
        public string lastDialogue;
    }
    [Serializable]
    public class DiaryPage { public string id, title, content, source; }

    public static Dictionary<string, NpcDef> Npcs = new Dictionary<string, NpcDef>();
    public static Dictionary<string, DiaryPage> DiaryPages = new Dictionary<string, DiaryPage>();
    static Dictionary<string, NpcState> _states = new Dictionary<string, NpcState>();

    public static void Init() {
        if (Npcs.Count > 0) return;
        // 4个NPC
        Npcs["merchant"] = new NpcDef { id="merchant", name="流浪商人·阿洛", icon="🧳", description="走南闯北的商人，每周刷新稀有商品", baseAffection=10,
            gifts = new Dictionary<string,int>{{"wheat",2},{"sunflower",5},{"watermelon",8}},
            rewards = new List<AffReward>{ new AffReward{aff=20,desc="解锁稀有商品栏",type="shop_tier2"}, new AffReward{aff=50,desc="每日免费种子",type="daily_seed"}, new AffReward{aff=80,desc="所有商品8折",type="discount"} } };
        Npcs["farmer"] = new NpcDef { id="farmer", name="老农夫·王伯", icon="👴", description="种了一辈子地的老人", baseAffection=20,
            gifts = new Dictionary<string,int>{{"wheat",3},{"cabbage",4},{"carrot",5}},
            rewards = new List<AffReward>{ new AffReward{aff=20,desc="高级施肥+20%产量",type="farm_boost1"}, new AffReward{aff=50,desc="变异率+10%",type="farm_boost2"}, new AffReward{aff=80,desc="传说作物培育",type="farm_boost3"} } };
        Npcs["veteran"] = new NpcDef { id="veteran", name="远征老兵·铁山", icon="⚔️", description="从深渊活着回来的战士", baseAffection=5,
            gifts = new Dictionary<string,int>{{"beast_core",10},{"boss_trophy",25}},
            rewards = new List<AffReward>{ new AffReward{aff=20,desc="解锁铁剑蓝图",type="weapon_1"}, new AffReward{aff=50,desc="解锁长矛蓝图",type="weapon_2"}, new AffReward{aff=80,desc="解锁暗影刃蓝图",type="weapon_3"} } };
        Npcs["traveler"] = new NpcDef { id="traveler", name="神秘旅人·影", icon="🌙", description="身份不明，知道太多秘密", baseAffection=0,
            gifts = new Dictionary<string,int>{{"diary_page",30}},
            rewards = new List<AffReward>{ new AffReward{aff=30,desc="解锁每日随机事件",type="daily_event"}, new AffReward{aff=60,desc="解锁预言",type="predict"}, new AffReward{aff=90,desc="解锁真结局线索",type="true_ending"} } };
        // 日记残页
        DiaryPages["page_1"] = new DiaryPage{ id="page_1", title="研究员日记·第1天", content="项目批准了。融合计划正式启动。", source="T1宝箱" };
        DiaryPages["page_2"] = new DiaryPage{ id="page_2", title="研究员日记·第47天", content="第3号实验体成功了！它同时具有植物和动物的特征。", source="T2 Boss" };
        DiaryPages["page_3"] = new DiaryPage{ id="page_3", title="研究员日记·第120天", content="实验体开始失控。主管说加速最终阶段。", source="T3精英" };
        DiaryPages["page_4"] = new DiaryPage{ id="page_4", title="研究员日记·最后一页", content="门打开了。不是我们打开的，是它们。快跑。", source="T4 Boss" };
        DiaryPages["page_5"] = new DiaryPage{ id="page_5", title="幸存者笔记", content="灾变后第3年。农田变成荒野，人类躲在少数据点。", source="神秘旅人好感50" };
    }

    public static NpcState GetState(string id) {
        if (!_states.ContainsKey(id)) {
            _states[id] = new NpcState { affection = Npcs.ContainsKey(id) ? Npcs[id].baseAffection : 0 };
        }
        return _states[id];
    }

    // 智能对话选择
    public static string GetDialogue(string npcId) {
        Init();
        var npc = Npcs[npcId]; var st = GetState(npcId);
        int aff = st.affection;
        // 1. 任务对话
        if (!string.IsNullOrEmpty(st.activeQuest) && !st.completedQuests.Contains(st.activeQuest))
            return "（任务进行中）继续加油！";
        // 2. 问候（按好感度）
        if (aff >= 80) return "我的挚友！今天所有商品给你打八折！";
        if (aff >= 60) return "哈哈，就知道你会来！给你留了最好的货。";
        if (aff >= 30) return "老朋友来了！我这刚到一批好货。";
        if (aff >= 10) return npc.id == "veteran" ? "你最近的战斗我听说了，有点意思。" : "哟，又见面了！";
        return npc.id == "traveler" ? "……你能看见我？有意思。" : "……又是你。";
    }

    public static void AddAffection(string npcId, int amount) {
        var st = GetState(npcId); int before = st.affection;
        st.affection = Mathf.Min(100, st.affection + amount);
        if (Npcs.ContainsKey(npcId)) {
            foreach (var r in Npcs[npcId].rewards) {
                if (before < r.aff && st.affection >= r.aff)
                    Debug.Log($"🎉 {Npcs[npcId].name} 好感度{r.aff}！解锁：{r.desc}");
            }
        }
    }

    public static bool GiveGift(string npcId, string itemId) {
        if (!Npcs.ContainsKey(npcId)) return false;
        int val = Npcs[npcId].gifts.ContainsKey(itemId) ? Npcs[npcId].gifts[itemId] : 1;
        AddAffection(npcId, val); return true;
    }

    public static void CompleteQuest(string npcId, string questId) {
        var st = GetState(npcId);
        if (st.completedQuests.Contains(questId)) return;
        st.completedQuests.Add(questId);
        if (st.activeQuest == questId) st.activeQuest = null;
        AddAffection(npcId, 15);
        GameState.gold += 100;
    }

    public static bool FindDiaryPage(string pageId) {
        if (GameState.diaryPages == null) GameState.diaryPages = new List<string>();
        if (GameState.diaryPages.Contains(pageId)) return false;
        GameState.diaryPages.Add(pageId);
        Debug.Log($"📜 发现日记残页：{DiaryPages[pageId].title}");
        return true;
    }

    public static List<string> GetCollectedPages() { return GameState.diaryPages ?? new List<string>(); }
}
