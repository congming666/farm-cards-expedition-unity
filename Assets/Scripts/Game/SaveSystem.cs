using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ================= 存档系统（移植自 save.js，改用 JsonUtility + 文件） =================
[Serializable] public class KeyVal { public string key; public int val; public KeyVal(){} public KeyVal(string k, int v){ key=k; val=v; } }
[Serializable] public class CardSave { public string id, rarity, skillId, icon, name, desc; public int power; public bool singleUse; }
[Serializable] public class PlotSave { public string cropId; public double plantedAt; public string status; }
[Serializable] public class SaveData
{
    public int version = 1;
    public int gold, seeds, materials, unlockedPlots;
    public string selectedMap, selectedWeapon, selectedCrop;
    public List<string> unlockedCrops = new List<string>();
    public List<KeyVal> loadout = new List<KeyVal>();
    public List<KeyVal> farmItems = new List<KeyVal>();
    public List<KeyVal> skillLevels = new List<KeyVal>();
    public List<CardSave> cardInventory = new List<CardSave>();
    public List<string> selectedBoostCards = new List<string>();
    public string lastDailyClaim = "", lastReliefClaim = "";
    public int dailyStreak;
    public List<PlotSave> farmPlots = new List<PlotSave>();
}

public static class SaveSystem
{
    static string Path_ { get { return System.IO.Path.Combine(Application.persistentDataPath, "farm-cards-save.json"); } }

    public static bool Load()
    {
        try {
            if (!File.Exists(Path_)) return false;
            string raw = File.ReadAllText(Path_);
            SaveData d = JsonUtility.FromJson<SaveData>(raw);
            if (d == null || d.version != 1) return false;
            GameState.gold = d.gold; GameState.seeds = d.seeds; GameState.materials = d.materials;
            GameState.unlockedPlots = G.Clamp(d.unlockedPlots, 8, 36);
            if (MapById(d.selectedMap) != null) GameState.selectedMap = d.selectedMap;
            if (WeaponById(d.selectedWeapon) != null) GameState.selectedWeapon = d.selectedWeapon;
            if (CropById(d.selectedCrop) != null) GameState.selectedCrop = d.selectedCrop;
            GameState.unlockedCrops = new List<string>();
            GameState.unlockedCrops.Add("wheat");
            foreach (var id in d.unlockedCrops) if (CropById(id) != null && !GameState.unlockedCrops.Contains(id)) GameState.unlockedCrops.Add(id);
            ApplyKv(d.loadout, GameState.loadout);
            ApplyKv(d.farmItems, GameState.farmItems);
            ApplyKv(d.skillLevels, GameState.skillLevels);
            GameState.cardInventory = new List<Card>();
            foreach (var c in d.cardInventory) GameState.cardInventory.Add(new Card{ id=c.id, rarity=c.rarity, skillId=c.skillId, icon=c.icon, name=c.name, desc=c.desc, power=c.power, singleUse=c.singleUse });
            GameState.selectedBoostCards = new List<string>();
            foreach (var id in d.selectedBoostCards) if (GameState.cardInventory.Exists(c=>c.id==id)) GameState.selectedBoostCards.Add(id);
            GameState.lastDailyClaim = d.lastDailyClaim; GameState.dailyStreak = Math.Max(0, d.dailyStreak); GameState.lastReliefClaim = d.lastReliefClaim;
            GameState.EnsurePlots();
            if (d.farmPlots != null && d.farmPlots.Count == 36) {
                for (int i=0;i<36;i++){ var p=d.farmPlots[i]; GameState.farmPlots[i]=new Plot{ crop=CropById(p.cropId), plantedAt=p.plantedAt, status=p.status, ready=false }; }
            }
            return true;
        } catch (Exception e) { Debug.LogWarning("读取存档失败："+e.Message); return false; }
    }

    public static void Save()
    {
        try {
            SaveData d = new SaveData();
            d.gold=GameState.gold; d.seeds=GameState.seeds; d.materials=GameState.materials; d.unlockedPlots=GameState.unlockedPlots;
            d.selectedMap=GameState.selectedMap; d.selectedWeapon=GameState.selectedWeapon; d.selectedCrop=GameState.selectedCrop;
            d.unlockedCrops=new List<string>(GameState.unlockedCrops);
            d.loadout=ToKv(GameState.loadout); d.farmItems=ToKv(GameState.farmItems); d.skillLevels=ToKv(GameState.skillLevels);
            d.cardInventory=new List<CardSave>();
            foreach (var c in GameState.cardInventory) d.cardInventory.Add(new CardSave{ id=c.id, rarity=c.rarity, skillId=c.skillId, icon=c.icon, name=c.name, desc=c.desc, power=c.power, singleUse=c.singleUse });
            d.selectedBoostCards=new List<string>(GameState.selectedBoostCards);
            d.lastDailyClaim=GameState.lastDailyClaim; d.dailyStreak=GameState.dailyStreak; d.lastReliefClaim=GameState.lastReliefClaim;
            d.farmPlots=new List<PlotSave>();
            for (int i=0;i<GameState.farmPlots.Length;i++){ var p=GameState.farmPlots[i]; d.farmPlots.Add(new PlotSave{ cropId=p.crop?.id, plantedAt=p.plantedAt, status=p.status }); }
            File.WriteAllText(Path_, JsonUtility.ToJson(d, true));
        } catch (Exception e) { Debug.LogWarning("保存存档失败："+e.Message); }
    }

    static List<KeyVal> ToKv(Dictionary<string,int> d){ var r=new List<KeyVal>(); foreach(var kv in d) r.Add(new KeyVal(kv.Key, kv.Value)); return r; }
    static void ApplyKv(List<KeyVal> list, Dictionary<string,int> dict){ foreach(var kv in list) dict[kv.key]=kv.val; }

    public static MapDef MapById(string id){ foreach(var m in GameData.Maps) if(m.id==id) return m; return null; }
    public static WeaponDef WeaponById(string id){ foreach(var w in GameData.Weapons) if(w.id==id) return w; return null; }
    public static CropDef CropById(string id){ foreach(var c in GameData.Crops) if(c.id==id) return c; return null; }
    public static SkillDef SkillById(string id){ foreach(var s in GameData.Skills) if(s.id==id) return s; return null; }
}
