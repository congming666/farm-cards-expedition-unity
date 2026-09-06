using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ================= 存档系统 v2（多槽位 / 版本迁移 / 原子写 / 损坏备份） =================
// 对外保持原有 API：Load() / Save() / MapById ... 不变，业务层无需改动。
// 内部：默认作用于 ActiveSlot；文件用 AtomicFile 原子写并留 .bak；读取损坏自动回退备份；
//       支持 schema 版本迁移；首次运行把旧的单文件 farm-cards-save.json 导入到 0 号槽。
[Serializable] public class KeyVal { public string key; public int val; public KeyVal(){} public KeyVal(string k, int v){ key=k; val=v; } }
[Serializable] public class CardSave { public string id, rarity, skillId, icon, name, desc; public int power; public bool singleUse; }
[Serializable] public class PlotSave { public string cropId; public double plantedAt; public string status; }
[Serializable] public class GreenhousePlotSave { public string plantId; public double plantedAt; public string status; }
[Serializable] public class SaveData
{
    public int version = SAVE_VERSION;
    public double savedAtUnix;                       // v2：写入时间，便于排查/展示存档时间
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

    // v1.9 物资仓库
    public int warehouseCapacity = 50;
    public List<KeyVal> warehouseItems = new List<KeyVal>();

    // v1.9 育种温室
    public int greenhouseUnlockedPlots = 4;
    public string selectedGreenhousePlant = "golden_wheat";
    public List<string> unlockedGreenhousePlants = new List<string>();
    public float weaponBonus = 0f;
    public bool expBoostActive = false;
    public List<GreenhousePlotSave> greenhousePlots = new List<GreenhousePlotSave>();

    public const int SAVE_VERSION = 2;
}

public static class SaveSystem
{
    public const int SLOT_COUNT = 3;                 // 0/1/2 三个存档位
    public static int ActiveSlot = 0;

    static string Dir { get { return Path.Combine(Application.persistentDataPath, "saves"); } }
    public static string SlotPath(int slot) { return Path.Combine(Dir, "save_slot_" + slot + ".json"); }
    // 旧的单文件存档（v1 时代），用于一次性导入
    static string LegacyPath { get { return Path.Combine(Application.persistentDataPath, "farm-cards-save.json"); } }

    // ============ 对外兼容 API：作用于当前槽 ============
    public static bool Load() { return LoadSlot(ActiveSlot); }
    public static void Save() { SaveSlot(ActiveSlot); }

    public static void SelectSlot(int slot) { ActiveSlot = G.Clamp(slot, 0, SLOT_COUNT - 1); }

    public static bool HasSlot(int slot)
    {
        try { return File.Exists(SlotPath(slot)); } catch { return false; }
    }

    public static void DeleteSlot(int slot)
    {
        try
        {
            string p = SlotPath(slot);
            if (File.Exists(p)) File.Delete(p);
            string bak = p + ".bak"; if (File.Exists(bak)) File.Delete(bak);
        }
        catch (Exception e) { Debug.LogWarning("[Save] 删除存档失败：" + e.Message); }
    }

    public static List<int> EnumerateSlots()
    {
        var list = new List<int>();
        for (int i = 0; i < SLOT_COUNT; i++) if (HasSlot(i)) list.Add(i);
        return list;
    }

    public static bool LoadSlot(int slot)
    {
        ActiveSlot = G.Clamp(slot, 0, SLOT_COUNT - 1);
        try
        {
            // 所有槽都不存在时，尝试导入旧单文件存档到当前槽
            MigrateLegacyIfNeeded();

            string path = SlotPath(ActiveSlot);
            string raw;
            if (!AtomicFile.ReadAllTextRobust(path, out raw)) return false;
            SaveData d = JsonUtility.FromJson<SaveData>(raw);
            if (d == null) return false;
            MigrateData(d);
            ApplyToState(d);
            return true;
        }
        catch (Exception e) { Debug.LogWarning("读取存档失败：" + e.Message); return false; }
    }

    public static void SaveSlot(int slot)
    {
        try
        {
            SaveData d = CaptureState();
            d.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            AtomicFile.WriteAllTextAtomic(SlotPath(G.Clamp(slot, 0, SLOT_COUNT - 1)), JsonUtility.ToJson(d, true));
        }
        catch (Exception e) { Debug.LogWarning("保存存档失败：" + e.Message); }
    }

    // 旧版本数据补齐默认值 / 结构升级（v1 -> v2 目前字段兼容，仅做健壮性兜底）
    static void MigrateData(SaveData d)
    {
        if (d.version <= 0) d.version = 1;
        // 未来：if (d.version < 2) { ...补字段... }
        if (d.unlockedCrops == null) d.unlockedCrops = new List<string>();
        if (d.loadout == null) d.loadout = new List<KeyVal>();
        if (d.farmItems == null) d.farmItems = new List<KeyVal>();
        if (d.skillLevels == null) d.skillLevels = new List<KeyVal>();
        if (d.cardInventory == null) d.cardInventory = new List<CardSave>();
        if (d.selectedBoostCards == null) d.selectedBoostCards = new List<string>();
        if (d.farmPlots == null) d.farmPlots = new List<PlotSave>();
        if (d.warehouseItems == null) d.warehouseItems = new List<KeyVal>();
        if (d.unlockedGreenhousePlants == null) d.unlockedGreenhousePlants = new List<string>();
        if (d.greenhousePlots == null) d.greenhousePlots = new List<GreenhousePlotSave>();
        d.version = SaveData.SAVE_VERSION;
    }

    // 没有任何槽位、但存在旧单文件存档时，导入为 0 号槽（只做一次）
    static void MigrateLegacyIfNeeded()
    {
        try
        {
            if (EnumerateSlots().Count > 0) return;
            if (!File.Exists(LegacyPath)) return;
            string raw;
            if (!AtomicFile.ReadAllTextRobust(LegacyPath, out raw)) return;
            var d = JsonUtility.FromJson<SaveData>(raw);
            if (d == null) return;
            MigrateData(d);
            AtomicFile.WriteAllTextAtomic(SlotPath(0), JsonUtility.ToJson(d, true));
            Debug.Log("[Save] 已将旧单文件存档导入到 0 号槽");
        }
        catch (Exception e) { Debug.LogWarning("[Save] 旧档迁移失败：" + e.Message); }
    }

    // ============ SaveData -> GameState（原 Load 主体，逻辑不变） ============
    static void ApplyToState(SaveData d)
    {
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

        // 物资仓库
        GameState.warehouseCapacity = d.warehouseCapacity > 0 ? d.warehouseCapacity : 50;
        GameState.warehouseItems = new Dictionary<string,int>();
        if (d.warehouseItems != null) ApplyKv(d.warehouseItems, GameState.warehouseItems);

        // 育种温室
        GameState.greenhouseUnlockedPlots = G.Clamp(d.greenhouseUnlockedPlots, 4, 16);
        GameState.selectedGreenhousePlant = d.selectedGreenhousePlant ?? "golden_wheat";
        GameState.unlockedGreenhousePlants = new List<string>();
        if (d.unlockedGreenhousePlants != null) foreach (var id in d.unlockedGreenhousePlants) if (!GameState.unlockedGreenhousePlants.Contains(id)) GameState.unlockedGreenhousePlants.Add(id);
        if (GameState.unlockedGreenhousePlants.Count == 0) { GameState.unlockedGreenhousePlants.Add("golden_wheat"); GameState.unlockedGreenhousePlants.Add("void_mushroom"); }
        GameState.weaponBonus = d.weaponBonus;
        GameState.expBoostActive = d.expBoostActive;
        GameState.EnsureGreenhousePlots();
        if (d.greenhousePlots != null && d.greenhousePlots.Count == 16) {
            for (int i=0;i<16;i++){ var p=d.greenhousePlots[i]; var plant=GreenhousePlantById(p.plantId); GameState.greenhousePlots[i]=new GreenhousePlot{ plant=plant, plantedAt=p.plantedAt, status=p.status, ready=false }; }
        }
    }

    // ============ GameState -> SaveData（原 Save 主体，逻辑不变） ============
    static SaveData CaptureState()
    {
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

        d.warehouseCapacity = GameState.warehouseCapacity;
        d.warehouseItems = ToKv(GameState.warehouseItems);

        d.greenhouseUnlockedPlots = GameState.greenhouseUnlockedPlots;
        d.selectedGreenhousePlant = GameState.selectedGreenhousePlant;
        d.unlockedGreenhousePlants = new List<string>(GameState.unlockedGreenhousePlants);
        d.weaponBonus = GameState.weaponBonus;
        d.expBoostActive = GameState.expBoostActive;
        d.greenhousePlots = new List<GreenhousePlotSave>();
        GameState.EnsureGreenhousePlots();
        for (int i=0;i<GameState.greenhousePlots.Length;i++){ var p=GameState.greenhousePlots[i]; d.greenhousePlots.Add(new GreenhousePlotSave{ plantId=p.plant?.id, plantedAt=p.plantedAt, status=p.status }); }
        return d;
    }

    static List<KeyVal> ToKv(Dictionary<string,int> d){ var r=new List<KeyVal>(); foreach(var kv in d) r.Add(new KeyVal(kv.Key, kv.Value)); return r; }
    static void ApplyKv(List<KeyVal> list, Dictionary<string,int> dict){ if(list==null)return; foreach(var kv in list) dict[kv.key]=kv.val; }

    public static MapDef MapById(string id){ foreach(var m in GameData.Maps) if(m.id==id) return m; return null; }
    public static WeaponDef WeaponById(string id){ foreach(var w in GameData.Weapons) if(w.id==id) return w; return null; }
    public static CropDef CropById(string id){ foreach(var c in GameData.Crops) if(c.id==id) return c; return null; }
    public static GreenhousePlantDef GreenhousePlantById(string id){ foreach(var p in GreenhouseData.Plants) if(p.id==id) return p; return null; }
    public static SkillDef SkillById(string id){ foreach(var s in GameData.Skills) if(s.id==id) return s; return null; }
}
