using System;
using System.Collections.Generic;
using UnityEngine;

// ================= v0.8.0 建筑升级 + 科技树（Unity版） =================
public static class TechSystem
{
    [Serializable]
    public class BuildingDef {
        public string id, name, icon, description;
        public int maxLevel;
        public int baseGold, baseMaterials;
        public float costMultiplier;
        public List<string> effects = new List<string>();
    }
    [Serializable]
    public class TechNode { public string id, name, desc; public int cost; public List<string> requires = new List<string>(); }
    [Serializable]
    public class TechTreeDef { public string id, name, icon, color; public List<TechNode> nodes = new List<TechNode>(); }

    public static Dictionary<string, BuildingDef> Buildings = new Dictionary<string, BuildingDef>();
    public static Dictionary<string, TechTreeDef> TechTrees = new Dictionary<string, TechTreeDef>();
    static bool _init = false;

    public static void Init() {
        if (_init) return; _init = true;
        // 4个建筑
        Buildings["workshop"] = new BuildingDef{ id="workshop", name="工坊", icon="🔨", description="加工作物", maxLevel=5, baseGold=200, baseMaterials=10, costMultiplier=1.8f,
            effects=new List<string>{"基础加工","加工速度+20%","高级加工","加工产量+30%","终极加工"} };
        Buildings["greenhouse"] = new BuildingDef{ id="greenhouse", name="温室", icon="🏠", description="反季节作物", maxLevel=3, baseGold=500, baseMaterials=20, costMultiplier=2.0f,
            effects=new List<string>{"4格温室农田","生长+30%","变异率+20%"} };
        Buildings["barn"] = new BuildingDef{ id="barn", name="畜舍", icon="🐔", description="养鸡养蜂", maxLevel=4, baseGold=300, baseMaterials=15, costMultiplier=1.8f,
            effects=new List<string>{"养鸡产蛋","鸡+2","养蜂产蜜","高级食物"} };
        Buildings["lab"] = new BuildingDef{ id="lab", name="研究所", icon="🔬", description="解锁科技树", maxLevel=3, baseGold=800, baseMaterials=30, costMultiplier=2.5f,
            effects=new List<string>{"农业科技线","战斗科技线","生存科技线+50%点"} };
        // 3条科技树
        TechTrees["agriculture"] = new TechTreeDef{ id="agriculture", name="农业", icon="🌾", color="#66cc44",
            nodes = new List<TechNode>{
                new TechNode{id="agri_1",name="精耕细作",desc="产量+10%",cost=1},
                new TechNode{id="agri_2",name="优质肥料",desc="品质+15%",cost=2,requires=new List<string>{"agri_1"}},
                new TechNode{id="agri_3",name="杂交育种",desc="变异率+20%",cost=3,requires=new List<string>{"agri_2"}},
                new TechNode{id="agri_4",name="温室栽培",desc="反季节解锁",cost=2,requires=new List<string>{"agri_1"}},
                new TechNode{id="agri_5",name="基因优化",desc="传说掉率+10%",cost=5,requires=new List<string>{"agri_3","agri_4"}}
            }};
        TechTrees["combat"] = new TechTreeDef{ id="combat", name="战斗", icon="⚔️", color="#cc4444",
            nodes = new List<TechNode>{
                new TechNode{id="combat_1",name="武器打磨",desc="伤害+10%",cost=1},
                new TechNode{id="combat_2",name="连击精通",desc="连击+2%/层",cost=2,requires=new List<string>{"combat_1"}},
                new TechNode{id="combat_3",name="闪避训练",desc="闪避窗口+20ms",cost=2,requires=new List<string>{"combat_1"}},
                new TechNode{id="combat_4",name="怒气掌控",desc="怒气+20%",cost=3,requires=new List<string>{"combat_2","combat_3"}},
                new TechNode{id="combat_5",name="战神血脉",desc="低血伤害+30%",cost=5,requires=new List<string>{"combat_4"}}
            }};
        TechTrees["survival"] = new TechTreeDef{ id="survival", name="生存", icon="🔥", color="#cc8844",
            nodes = new List<TechNode>{
                new TechNode{id="surv_1",name="火把改良",desc="火把消耗-20%",cost=1},
                new TechNode{id="surv_2",name="急救术",desc="回血+30%",cost=2,requires=new List<string>{"surv_1"}},
                new TechNode{id="surv_3",name="视野拓展",desc="视野+15%",cost=2,requires=new List<string>{"surv_1"}},
                new TechNode{id="surv_4",name="野外求生",desc="补给+25%",cost=3,requires=new List<string>{"surv_2","surv_3"}},
                new TechNode{id="surv_5",name="不屈意志",desc="低血无敌1s",cost=5,requires=new List<string>{"surv_4"}}
            }};
    }

    public static int GetBuildingLevel(string id) {
        if (GameState.buildingLevels == null) GameState.buildingLevels = new Dictionary<string, int>();
        return GameState.buildingLevels.ContainsKey(id) ? GameState.buildingLevels[id] : 0;
    }

    public static bool UpgradeBuilding(string id) {
        Init();
        if (!Buildings.ContainsKey(id)) return false;
        var b = Buildings[id]; int level = GetBuildingLevel(id);
        if (level >= b.maxLevel) return false;
        float mul = Mathf.Pow(b.costMultiplier, level);
        int gold = Mathf.FloorToInt(b.baseGold * mul), mat = Mathf.FloorToInt(b.baseMaterials * mul);
        if (GameState.gold < gold || GameState.materials < mat) { Debug.Log("资源不足"); return false; }
        GameState.gold -= gold; GameState.materials -= mat;
        GameState.buildingLevels[id] = level + 1;
        Debug.Log($"🎉 {b.name} Lv.{level+1}");
        return true;
    }

    public static bool IsTechUnlocked(string techId) {
        if (GameState.unlockedTech == null) GameState.unlockedTech = new List<string>();
        return GameState.unlockedTech.Contains(techId);
    }

    public static bool CanUnlockTech(string techId) {
        Init();
        if (IsTechUnlocked(techId)) return false;
        if (GameState.techPoints <= 0) return false;
        foreach (var tree in TechTrees.Values) {
            var node = tree.nodes.Find(n => n.id == techId);
            if (node != null) {
                if (GameState.techPoints < node.cost) return false;
                return node.requires.TrueForAll(r => IsTechUnlocked(r));
            }
        }
        return false;
    }

    public static bool UnlockTech(string techId) {
        if (!CanUnlockTech(techId)) return false;
        Init();
        foreach (var tree in TechTrees.Values) {
            var node = tree.nodes.Find(n => n.id == techId);
            if (node != null) {
                GameState.techPoints -= node.cost;
                GameState.unlockedTech.Add(techId);
                Debug.Log($"🔬 解锁：{node.name}");
                return true;
            }
        }
        return false;
    }

    public static void AddTechPoints(int amount) { GameState.techPoints += amount; }

    public static int OnExpeditionComplete(int tier, string difficulty) {
        int basePts = tier * 2;
        float mul = difficulty == "hard" ? 1.5f : difficulty == "nightmare" ? 2.0f : 1.0f;
        int pts = Mathf.FloorToInt(basePts * mul);
        AddTechPoints(pts); return pts;
    }
}
