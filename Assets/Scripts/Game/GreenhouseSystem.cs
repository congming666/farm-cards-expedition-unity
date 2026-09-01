using System;
using System.Collections.Generic;
using UnityEngine;

// ================= v1.9 育种温室系统 =================
public static class GreenhouseSystem
{
    public static bool greenhouseOpen = false;
    public static bool warehouseOpen = false;

    public static void Init()
    {
        GameState.EnsureGreenhousePlots();
        // 初始种一些
        int planted = 0;
        for (int i = 0; i < GameState.greenhouseUnlockedPlots && planted < 2; i++)
        {
            if (GameState.greenhousePlots[i].plant == null)
            {
                GameState.greenhousePlots[i].plant = SaveSystem.GreenhousePlantById("golden_wheat");
                GameState.greenhousePlots[i].plantedAt = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds - G.Rand(10, 60);
                planted++;
            }
        }
        SaveSystem.Save();
    }

    public static void Plant(int idx)
    {
        if (idx >= GameState.greenhouseUnlockedPlots) return;
        var plant = SaveSystem.GreenhousePlantById(GameState.selectedGreenhousePlant);
        if (plant == null) { GameFlow.I.AddToast("请选择要种植的稀有植物", "warning"); return; }
        if (!GameState.unlockedGreenhousePlants.Contains(plant.id)) { GameFlow.I.AddToast(plant.name + "尚未解锁", "warning"); return; }
        if (GameState.gold < plant.seedPrice) { GameFlow.I.AddToast("金币不足，需要" + plant.seedPrice + "金币购买种子", "warning"); return; }
        GameState.gold -= plant.seedPrice;
        GameState.greenhousePlots[idx].plant = plant;
        GameState.greenhousePlots[idx].plantedAt = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        GameState.greenhousePlots[idx].ready = false;
        GameFlow.I.AddToast("在温室种下了" + plant.name, "success");
        SaveSystem.Save();
    }

    public static void Harvest(int idx)
    {
        var plot = GameState.greenhousePlots[idx];
        if (!plot.ready) { GameFlow.I.AddToast("还没成熟呢", "warning"); return; }
        var plant = plot.plant;
        var rewards = new List<string>();

        foreach (var drop in plant.drops)
        {
            if (UnityEngine.Random.value < drop.chance)
            {
                int amount = G.RandInt(drop.minAmount, drop.maxAmount);
                if (drop.id == "gold")
                {
                    GameState.gold += amount;
                    rewards.Add("💰+" + amount);
                }
                else
                {
                    AddWarehouseItem(drop.id, amount);
                    var def = GreenhouseData.Drops.ContainsKey(drop.id) ? GreenhouseData.Drops[drop.id] : null;
                    if (def != null) rewards.Add(def.icon + "×" + amount);
                }
            }
        }

        // 传说植物小概率解锁新植物
        if (plant.rarity == "legendary" && UnityEngine.Random.value < 0.15f)
        {
            var locked = new List<GreenhousePlantDef>();
            foreach (var p in GreenhouseData.Plants) if (!GameState.unlockedGreenhousePlants.Contains(p.id)) locked.Add(p);
            if (locked.Count > 0)
            {
                var newPlant = locked[G.RandInt(0, locked.Count - 1)];
                GameState.unlockedGreenhousePlants.Add(newPlant.id);
                rewards.Add("🎉解锁" + newPlant.name);
                GameFlow.I.AddToast("解锁新稀有植物：" + newPlant.name + "！", "gold");
            }
        }

        string rewardText = rewards.Count > 0 ? "获得：" + string.Join(" ", rewards) : "什么都没掉...";
        GameFlow.I.AddToast(plant.name + "收获！" + rewardText, "gold");

        plot.plant = null;
        plot.ready = false;
        plot.status = null;
        SaveSystem.Save();
    }

    public static void UnlockPlot(int idx)
    {
        if (idx != GameState.greenhouseUnlockedPlots) { GameFlow.I.AddToast("请依次解锁温室格子", "warning"); return; }
        int level = idx - 3;
        int goldCost = 200 * level;
        int matCost = 5 * level;
        if (GameState.gold < goldCost) { GameFlow.I.AddToast("金币不足，需要" + goldCost + "金币", "warning"); return; }
        if (GameState.materials < matCost) { GameFlow.I.AddToast("材料不足，需要" + matCost + "材料", "warning"); return; }
        GameState.gold -= goldCost;
        GameState.materials -= matCost;
        GameState.greenhouseUnlockedPlots++;
        GameFlow.I.AddToast("温室格子解锁成功！当前" + GameState.greenhouseUnlockedPlots + "/16", "success");
        SaveSystem.Save();
    }

    public static int GetUnlockCost(int idx)
    {
        int level = idx - 3;
        return 200 * level;
    }

    public static int GetUnlockMatCost(int idx)
    {
        int level = idx - 3;
        return 5 * level;
    }

    // ===== 物资仓库 =====
    public static int GetWarehouseCount(string itemId)
    {
        return GameState.warehouseItems.ContainsKey(itemId) ? GameState.warehouseItems[itemId] : 0;
    }

    public static int GetWarehouseUsed()
    {
        int total = 0;
        foreach (var kv in GameState.warehouseItems) total += kv.Value;
        return total;
    }

    public static int AddWarehouseItem(string itemId, int count)
    {
        int free = GameState.warehouseCapacity - GetWarehouseUsed();
        int actual = Math.Min(count, free);
        if (actual <= 0) { GameFlow.I.AddToast("仓库已满！请出售或扩建仓库", "warning"); return 0; }
        if (!GameState.warehouseItems.ContainsKey(itemId)) GameState.warehouseItems[itemId] = 0;
        GameState.warehouseItems[itemId] += actual;
        if (actual < count) GameFlow.I.AddToast("仓库空间不足，只存入" + actual + "个", "warning");
        return actual;
    }

    public static bool RemoveWarehouseItem(string itemId, int count)
    {
        if (!GameState.warehouseItems.ContainsKey(itemId) || GameState.warehouseItems[itemId] < count) return false;
        GameState.warehouseItems[itemId] -= count;
        if (GameState.warehouseItems[itemId] <= 0) GameState.warehouseItems.Remove(itemId);
        return true;
    }

    public static void SellWarehouseItem(string itemId, int count)
    {
        var def = GreenhouseData.Drops.ContainsKey(itemId) ? GreenhouseData.Drops[itemId] : null;
        if (def == null || def.sellPrice <= 0) { GameFlow.I.AddToast("该物品无法出售", "warning"); return; }
        int current = GetWarehouseCount(itemId);
        int actual = Math.Min(count, current);
        if (actual <= 0) return;
        int gold = def.sellPrice * actual;
        RemoveWarehouseItem(itemId, actual);
        GameState.gold += gold;
        GameFlow.I.AddToast("出售" + def.name + " ×" + actual + "，获得" + gold + "金币", "gold");
        SaveSystem.Save();
    }

    public static void UpgradeWarehouse()
    {
        int level = (GameState.warehouseCapacity - 50) / 25 + 1;
        int cost = 100 * level;
        if (GameState.gold < cost) { GameFlow.I.AddToast("扩建需要" + cost + "金币", "warning"); return; }
        GameState.gold -= cost;
        GameState.warehouseCapacity += 25;
        GameFlow.I.AddToast("仓库扩建成功！容量提升至" + GameState.warehouseCapacity, "success");
        SaveSystem.Save();
    }

    public static int GetWarehouseUpgradeCost()
    {
        int level = (GameState.warehouseCapacity - 50) / 25 + 1;
        return 100 * level;
    }

    // ===== 使用温室道具 =====
    public static void UseDropItem(string itemId)
    {
        if (GetWarehouseCount(itemId) <= 0) { GameFlow.I.AddToast("没有该道具", "warning"); return; }
        var def = GreenhouseData.Drops.ContainsKey(itemId) ? GreenhouseData.Drops[itemId] : null;
        if (def == null) return;

        switch (itemId)
        {
            case "gold_card":
                RemoveWarehouseItem(itemId, 1);
                GameState.gold += def.value;
                GameFlow.I.AddToast("使用金币卡，获得" + def.value + "金币", "gold");
                break;
            case "big_gold_card":
                RemoveWarehouseItem(itemId, 1);
                GameState.gold += def.value;
                GameFlow.I.AddToast("使用大金币卡，获得" + def.value + "金币！", "gold");
                break;
            case "transform_card":
                UseTransformCard();
                return;
            case "rare_seed_pack":
                UseRareSeedPack();
                return;
            case "exp_boost_card":
                RemoveWarehouseItem(itemId, 1);
                GameState.expBoostActive = true;
                GameFlow.I.AddToast("经验加成已激活，下次远征击杀经验+50%", "success");
                break;
            case "weapon_upgrade_stone":
                RemoveWarehouseItem(itemId, 1);
                GameState.weaponBonus += 0.1f;
                GameFlow.I.AddToast("武器已强化！当前伤害+" + Mathf.FloorToInt(GameState.weaponBonus * 100) + "%", "success");
                break;
            default:
                GameFlow.I.AddToast("该道具暂不可用", "warning");
                return;
        }
        SaveSystem.Save();
    }

    static void UseTransformCard()
    {
        var readyPlots = new List<int>();
        for (int i = 0; i < GameState.unlockedPlots; i++)
        {
            if (GameState.farmPlots[i].ready && GameState.farmPlots[i].crop != null) readyPlots.Add(i);
        }
        if (readyPlots.Count == 0) { GameFlow.I.AddToast("需要一块成熟的普通作物才能转化", "warning"); return; }
        RemoveWarehouseItem("transform_card", 1);
        int idx = readyPlots[G.RandInt(0, readyPlots.Count - 1)];
        var rarePlants = new List<GreenhousePlantDef>();
        foreach (var p in GreenhouseData.Plants) if (GameState.unlockedGreenhousePlants.Contains(p.id)) rarePlants.Add(p);
        var newPlant = rarePlants[G.RandInt(0, rarePlants.Count - 1)];
        GameState.farmPlots[idx].crop = null;
        GameState.farmPlots[idx].ready = false;
        GameState.greenhousePlots[0].plant = newPlant;
        GameState.greenhousePlots[0].plantedAt = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        GameState.greenhousePlots[0].ready = false;
        GameFlow.I.AddToast("作物转化为" + newPlant.icon + newPlant.name + "！", "gold");
        SaveSystem.Save();
    }

    static void UseRareSeedPack()
    {
        var locked = new List<GreenhousePlantDef>();
        foreach (var p in GreenhouseData.Plants) if (!GameState.unlockedGreenhousePlants.Contains(p.id)) locked.Add(p);
        if (locked.Count == 0) { GameFlow.I.AddToast("所有稀有植物都已解锁", "warning"); return; }
        RemoveWarehouseItem("rare_seed_pack", 1);
        var newPlant = locked[G.RandInt(0, locked.Count - 1)];
        GameState.unlockedGreenhousePlants.Add(newPlant.id);
        GameFlow.I.AddToast("解锁新稀有植物：" + newPlant.icon + newPlant.name + "！", "gold");
        SaveSystem.Save();
    }
}
