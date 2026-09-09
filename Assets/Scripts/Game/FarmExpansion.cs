using System;
using System.Collections.Generic;
using UnityEngine;

// ================= v2.0 农场大更新：作物特性 / 照料天气 / 加工产业链 / 收集变异 / 装饰访客 =================

// ========== 1. 作物特性系统 ==========
public static class FarmTraitSystem
{
    // 计算某格作物的实际生长速度倍率（受相邻作物、天气、湿度、肥料影响）
    public static float GrowSpeedMultiplier(int idx)
    {
        var plot = GameState.farmPlots[idx];
        if (plot.crop == null) return 0f;
        float mul = 1f;

        // 湿度：低于30减速50%
        if (plot.moisture < 30f) mul *= 0.5f;
        else if (plot.moisture < 60f) mul *= 0.8f;

        // 肥料
        if (plot.fertilized) mul *= 1.2f;

        // 天气
        if (GameState.weather == "rain") mul *= 1.15f;
        else if (GameState.weather == "fog") mul *= 0.85f;
        else if (GameState.weather == "storm") mul *= 0.7f;

        // 季节
        if (GameState.season == "winter" && plot.crop.trait != "hardy") mul *= 0.75f;
        if (GameState.season == "summer" && plot.crop.id == "watermelon") mul *= 1.2f;

        // 向日葵光环：相邻有成熟向日葵则+15%
        if (HasAdjacentMatureSunflower(idx)) mul *= 1.15f;

        // 小麦连作：相邻小麦数量>=2则+20%
        if (plot.crop.trait == "monoculture" && AdjacentCropCount(idx, "wheat") >= 2) mul *= 1.2f;

        // 卷心菜耐寒：冬季不受减速
        if (plot.crop.trait == "hardy" && GameState.season == "winter") mul /= 0.75f; // 抵消上面的冬季减速

        return mul;
    }

    // 收获产量倍率（受品质、连作、特性影响）
    public static int HarvestYield(int idx)
    {
        var plot = GameState.farmPlots[idx];
        if (plot.crop == null) return 1;
        int baseYield = 1;

        // 品质加成
        if (plot.quality == "fine") baseYield += 1;
        else if (plot.quality == "rare") baseYield += 2;
        else if (plot.quality == "legendary") baseYield += 4;

        // 小麦连作加成
        if (plot.crop.trait == "monoculture" && AdjacentCropCount(idx, "wheat") >= 2) baseYield = (int)Math.Ceiling(baseYield * 1.2f);

        // 西瓜大型作物产量高
        if (plot.crop.trait == "giant") baseYield += 1;

        return Math.Max(1, baseYield);
    }

    // 收获后是否保留作物（豌豆可反复收获3次）
    public static bool ShouldRemainAfterHarvest(int idx)
    {
        var plot = GameState.farmPlots[idx];
        if (plot.crop == null) return false;
        if (plot.crop.trait == "reharvest" && plot.harvestCount < 3) return true;
        return false;
    }

    // 玉米成熟后吸引野兽的概率
    public static bool ShouldSpawnBeast(int idx)
    {
        var plot = GameState.farmPlots[idx];
        if (plot.crop == null || plot.crop.trait != "beast" || !plot.ready) return false;
        return G.Rng.NextDouble() < 0.15; // 每tick 15%概率
    }

    static bool HasAdjacentMatureSunflower(int idx)
    {
        int row = idx / 6, col = idx % 6;
        int[] dr = {-1,1,0,0}, dc = {0,0,-1,1};
        for (int i = 0; i < 4; i++)
        {
            int nr = row + dr[i], nc = col + dc[i];
            if (nr < 0 || nr >= 6 || nc < 0 || nc >= 6) continue;
            int nidx = nr * 6 + nc;
            if (nidx >= GameState.farmPlots.Length) continue;
            var p = GameState.farmPlots[nidx];
            if (p.crop != null && p.crop.id == "sunflower" && p.ready) return true;
        }
        return false;
    }

    static int AdjacentCropCount(int idx, string cropId)
    {
        int count = 0;
        int row = idx / 6, col = idx % 6;
        int[] dr = {-1,1,0,0}, dc = {0,0,-1,1};
        for (int i = 0; i < 4; i++)
        {
            int nr = row + dr[i], nc = col + dc[i];
            if (nr < 0 || nr >= 6 || nc < 0 || nc >= 6) continue;
            int nidx = nr * 6 + nc;
            if (nidx >= GameState.farmPlots.Length) continue;
            var p = GameState.farmPlots[nidx];
            if (p.crop != null && p.crop.id == cropId) count++;
        }
        return count;
    }

    public static string TraitDesc(string trait)
    {
        switch (trait)
        {
            case "monoculture": return "连作：相邻≥2小麦时产量+20%";
            case "aura": return "光环：成熟后周围作物生长+15%";
            case "giant": return "大型：生长慢但产量+1";
            case "reharvest": return "反复：可收获3次后枯萎";
            case "hardy": return "耐寒：冬季不受减速";
            case "mutate": return "变异：收获时有概率获得稀有变异";
            case "beast": return "招兽：成熟后可能吸引野兽偷食";
            case "carve": return "雕刻：可雕刻成南瓜灯装饰";
            case "legendary": return "传说：极高稀有度与奖励";
            default: return "";
        }
    }
}

// ========== 2. 照料与天气系统 ==========
public static class FarmCareSystem
{
    static readonly string[] Weathers = {"sunny","sunny","sunny","rain","rain","fog","storm"};
    static readonly string[] Seasons = {"spring","summer","autumn","winter"};

    public static void Tick(float dt)
    {
        // 天气计时
        GameState.weatherTimer -= dt;
        if (GameState.weatherTimer <= 0)
        {
            GameState.weather = Weathers[G.RandInt(0, Weathers.Length - 1)];
            GameState.weatherTimer = G.Rand(60f, 180f);
            UIHost.ShowToast("天气变化：" + WeatherName(GameState.weather), "info");
        }

        // 季节推进（每300秒=5分钟推进一天，每7天换季）
        GameState.seasonDay++;
        if (GameState.seasonDay > 7)
        {
            GameState.seasonDay = 1;
            int si = Array.IndexOf(Seasons, GameState.season);
            GameState.season = Seasons[(si + 1) % 4];
            UIHost.ShowToast("季节更替：进入" + SeasonName(GameState.season), "info");
        }

        // 湿度衰减（每格每秒降0.15，雨天自动恢复）
        for (int i = 0; i < GameState.farmPlots.Length; i++)
        {
            var p = GameState.farmPlots[i];
            if (p.crop == null) continue;
            if (GameState.weather == "rain") p.moisture = Math.Min(100f, p.moisture + dt * 5f);
            else p.moisture = Math.Max(0f, p.moisture - dt * 0.15f);

            // 低湿度有概率触发干旱
            if (p.moisture < 20f && p.status == null && G.Rng.NextDouble() < 0.005f)
            {
                p.status = "drought";
                UIHost.ShowToast("一块农田干旱了，点击照料", "warning");
            }
        }

        // 浇水冷却
        GameState.waterCooldown = Math.Max(0f, GameState.waterCooldown - dt);

        // 玉米招兽检查
        for (int i = 0; i < GameState.farmPlots.Length; i++)
        {
            if (FarmTraitSystem.ShouldSpawnBeast(i))
            {
                var p = GameState.farmPlots[i];
                p.status = "beast";
                UIHost.ShowToast("野兽正在偷食玉米！点击驱赶", "warning");
            }
        }
    }

    public static void WaterPlot(int idx)
    {
        if (GameState.waterCooldown > 0) { UIHost.ShowToast("浇水冷却中，稍等片刻", "warning"); return; }
        var p = GameState.farmPlots[idx];
        if (p.crop == null) { UIHost.ShowToast("这块地没有作物", "warning"); return; }
        p.moisture = 100f;
        if (p.status == "drought") p.status = null;
        GameState.waterCooldown = 3f;
        UIHost.ShowToast("浇水完成，作物恢复活力", "success");
        SaveSystem.Save();
    }

    public static void FertilizePlot(int idx, bool premium)
    {
        var p = GameState.farmPlots[idx];
        if (p.crop == null) { UIHost.ShowToast("这块地没有作物", "warning"); return; }
        if (p.fertilized) { UIHost.ShowToast("已经施过肥了", "warning"); return; }

        if (premium)
        {
            if (GameState.gold < 20) { UIHost.ShowToast("高级肥需要20金币", "warning"); return; }
            GameState.gold -= 20;
            // 高级肥：+50%速度但20%概率烧苗
            if (G.Rng.NextDouble() < 0.2f)
            {
                p.status = "burn";
                UIHost.ShowToast("施肥过量，作物烧苗了！点击照料", "warning");
            }
            else
            {
                p.fertilized = true;
                p.plantedAt -= p.crop.growTime * 0.3 * 1000; // 直接减少30%时间
                UIHost.ShowToast("高级肥生效，生长大幅加速", "success");
            }
        }
        else
        {
            if (GameState.gold < 8) { UIHost.ShowToast("普通肥需要8金币", "warning"); return; }
            GameState.gold -= 8;
            p.fertilized = true;
            p.plantedAt -= p.crop.growTime * 0.15 * 1000; // 减少15%时间
            UIHost.ShowToast("施肥完成，生长加速", "success");
        }
        SaveSystem.Save();
    }

    public static void ChaseBeast(int idx)
    {
        var p = GameState.farmPlots[idx];
        if (p.status != "beast") return;
        p.status = null;
        // 驱赶野兽有概率掉落材料
        if (G.Rng.NextDouble() < 0.4f)
        {
            GreenhouseSystem.AddWarehouseItem("materials", 1);
            UIHost.ShowToast("驱赶野兽成功，捡到材料 ×1", "success");
        }
        else UIHost.ShowToast("驱赶野兽成功", "success");
        SaveSystem.Save();
    }

    public static string WeatherName(string w)
    {
        switch (w) { case "sunny": return "晴朗"; case "rain": return "小雨"; case "storm": return "雷暴"; case "fog": return "浓雾"; default: return w; }
    }
    public static string WeatherIcon(string w)
    {
        switch (w) { case "sunny": return "☀️"; case "rain": return "🌧️"; case "storm": return "⛈️"; case "fog": return "🌫️"; default: return "🌤️"; }
    }
    public static string SeasonName(string s)
    {
        switch (s) { case "spring": return "春季"; case "summer": return "夏季"; case "autumn": return "秋季"; case "winter": return "冬季"; default: return s; }
    }
    public static string SeasonIcon(string s)
    {
        switch (s) { case "spring": return "🌸"; case "summer": return "☀️"; case "autumn": return "🍂"; case "winter": return "❄️"; default: return "🌍"; }
    }
}

// ========== 3. 加工与产业链系统 ==========
public static class FarmProcessingSystem
{
    public class Recipe { public string id, name, icon, inputCrop, outputId, outputName, outputIcon; public int inputQty, outputQty; public float time; public int workshopLevel; public string desc; }

    public static readonly Recipe[] Recipes = new Recipe[]
    {
        new Recipe{ id="flour", name="面粉", icon="🌾", inputCrop="wheat", inputQty=3, outputId="flour", outputName="面粉", outputIcon="🥛", outputQty=1, time=20f, workshopLevel=1, desc="小麦→面粉，可做面包" },
        new Recipe{ id="bread", name="面包", icon="🍞", inputCrop="flour", inputQty=2, outputId="bread", outputName="面包", outputIcon="🍞", outputQty=1, time=30f, workshopLevel=2, desc="面粉→面包，远征回血+80" },
        new Recipe{ id="oil", name="植物油", icon="🌻", inputCrop="sunflower", inputQty=3, outputId="oil", outputName="植物油", outputIcon="🫒", outputQty=1, time=25f, workshopLevel=1, desc="向日葵→植物油" },
        new Recipe{ id="torch", name="火把", icon="🔥", inputCrop="oil", inputQty=2, outputId="torch", outputName="火把", outputIcon="🔥", outputQty=1, time=25f, workshopLevel=2, desc="植物油→火把，远征迷雾视野+" },
        new Recipe{ id="juice", name="西瓜汁", icon="🧃", inputCrop="watermelon", inputQty=2, outputId="juice", outputName="西瓜汁", outputIcon="🧃", outputQty=1, time=15f, workshopLevel=1, desc="西瓜→果汁，远征能量上限+" },
        new Recipe{ id="feed", name="饲料", icon="🌽", inputCrop="corn", inputQty=3, outputId="feed", outputName="饲料", outputIcon="🥣", outputQty=1, time=20f, workshopLevel=1, desc="玉米→饲料，养鸡下蛋" },
        new Recipe{ id="egg", name="鸡蛋", icon="🥚", inputCrop="feed", inputQty=2, outputId="egg", outputName="鸡蛋", outputIcon="🥚", outputQty=2, time=40f, workshopLevel=2, desc="饲料→鸡蛋，远征buff食物" },
        new Recipe{ id="pumpkin_lantern", name="南瓜灯", icon="🎃", inputCrop="pumpkin", inputQty=1, outputId="pumpkin_lantern", outputName="南瓜灯", outputIcon="🎃", outputQty=1, time=15f, workshopLevel=1, desc="南瓜→南瓜灯装饰，美观+5" },
        new Recipe{ id="insecticide", name="驱虫剂", icon="🧪", inputCrop="cabbage", inputQty=2, outputId="insecticide", outputName="驱虫剂", outputIcon="🧪", outputQty=1, time=18f, workshopLevel=1, desc="卷心菜→驱虫剂，治病虫害" },
    };

    public static Recipe GetRecipe(string id) { foreach (var r in Recipes) if (r.id == id) return r; return null; }

    public static bool StartProcessing(string recipeId, int qty = 1)
    {
        var recipe = GetRecipe(recipeId);
        if (recipe == null) return false;
        if (recipe.workshopLevel > GameState.workshopLevel) { UIHost.ShowToast("需要工坊等级 " + recipe.workshopLevel, "warning"); return false; }

        // 检查原料
        int have = GreenhouseSystem.GetWarehouseCount(recipe.inputCrop);
        if (have < recipe.inputQty * qty) { UIHost.ShowToast("原料不足：需要 " + (recipe.inputQty * qty) + " " + recipe.inputCrop, "warning"); return false; }

        GreenhouseSystem.RemoveWarehouseItem(recipe.inputCrop, recipe.inputQty * qty);
        GameState.processingQueue.Add(new ProcessingJob { recipeId = recipeId, remaining = recipe.time, total = recipe.time, qty = qty });
        UIHost.ShowToast("开始加工：" + recipe.name + " ×" + qty, "success");
        SaveSystem.Save();
        return true;
    }

    public static void Tick(float dt)
    {
        for (int i = GameState.processingQueue.Count - 1; i >= 0; i--)
        {
            var job = GameState.processingQueue[i];
            job.remaining -= dt;
            if (job.remaining <= 0)
            {
                var recipe = GetRecipe(job.recipeId);
                if (recipe != null)
                {
                    GreenhouseSystem.AddWarehouseItem(recipe.outputId, recipe.outputQty * job.qty);
                    // 南瓜灯自动加美观
                    if (recipe.outputId == "pumpkin_lantern") GameState.farmBeauty += 5;
                    UIHost.ShowToast("加工完成：" + recipe.outputName + " ×" + (recipe.outputQty * job.qty), "gold");
                }
                GameState.processingQueue.RemoveAt(i);
                SaveSystem.Save();
            }
        }
    }

    public static bool UpgradeWorkshop()
    {
        int cost = GameState.workshopLevel * 100;
        if (GameState.gold < cost) { UIHost.ShowToast("升级工坊需要 " + cost + " 金币", "warning"); return false; }
        if (GameState.workshopLevel >= 3) { UIHost.ShowToast("工坊已满级", "warning"); return false; }
        GameState.gold -= cost;
        GameState.workshopLevel++;
        UIHost.ShowToast("工坊升级到 Lv." + GameState.workshopLevel, "gold");
        SaveSystem.Save();
        return true;
    }
}

// ========== 4. 收集与变异系统 ==========
public static class FarmCollectionSystem
{
    static readonly string[] Qualities = {"common","fine","rare","legendary"};
    static readonly int[] QualityWeight = {60,25,12,3};

    // 收获时roll品质
    public static string RollQuality(CropDef crop)
    {
        // 基础权重
        int total = 0; foreach (var w in QualityWeight) total += w;
        int roll = G.RandInt(0, total - 1);
        int acc = 0;
        for (int i = 0; i < Qualities.Length; i++)
        {
            acc += QualityWeight[i];
            if (roll < acc) return Qualities[i];
        }
        return "common";
    }

    // 记录图鉴
    public static void RecordCollection(string cropId, string quality)
    {
        if (!GameState.cropCollection.ContainsKey(cropId))
            GameState.cropCollection[cropId] = quality;
        else
        {
            int oldQ = Array.IndexOf(Qualities, GameState.cropCollection[cropId]);
            int newQ = Array.IndexOf(Qualities, quality);
            if (newQ > oldQ) GameState.cropCollection[cropId] = quality;
        }
    }

    // 图鉴收集奖励（全局buff）
    public static float CollectionBonus()
    {
        int fineCount = 0, rareCount = 0, legCount = 0;
        foreach (var kv in GameState.cropCollection)
        {
            if (kv.Value == "fine") fineCount++;
            else if (kv.Value == "rare") rareCount++;
            else if (kv.Value == "legendary") legCount++;
        }
        // 每5个优质+5%生长，每3个稀有+8%，每个传说+10%
        return 1f + fineCount * 0.01f + rareCount * 0.025f + legCount * 0.1f;
    }

    // 变异检查（胡萝卜等）
    public static bool TryMutate(int idx)
    {
        var p = GameState.farmPlots[idx];
        if (p.crop == null || p.crop.trait != "mutate") return false;
        // 雾天变异率+，肥料+
        float chance = 0.08f;
        if (GameState.weather == "fog") chance += 0.1f;
        if (p.fertilized) chance += 0.05f;
        if (G.Rng.NextDouble() < chance)
        {
            p.quality = "rare";
            UIHost.ShowToast("✨ 胡萝卜变异了！获得稀有品质", "gold");
            return true;
        }
        return false;
    }

    public static string QualityName(string q)
    {
        switch (q) { case "fine": return "优质"; case "rare": return "稀有"; case "legendary": return "传说"; default: return "普通"; }
    }
    public static Color QualityColor(string q)
    {
        switch (q) { case "fine": return G.ParseColor("#7fff7f"); case "rare": return G.ParseColor("#7be5c4"); case "legendary": return G.ParseColor("#ffd700"); default: return Color.white; }
    }
    public static int CollectionCount() { return GameState.cropCollection.Count; }
}

// ========== 5. 装饰与访客系统 ==========
public static class FarmDecorationSystem
{
    public class DecorationDef { public string id, name, icon; public int beauty, cost; public string desc; }

    public static readonly DecorationDef[] ShopItems = new DecorationDef[]
    {
        new DecorationDef{ id="scarecrow", name="稻草人", icon="🎃", beauty=3, cost=50, desc="驱赶野兽，美观+3" },
        new DecorationDef{ id="fence", name="木栅栏", icon="🚧", beauty=1, cost=20, desc="基础装饰，美观+1" },
        new DecorationDef{ id="well", name="水井", icon="⛲", beauty=5, cost=120, desc="浇水冷却减半，美观+5" },
        new DecorationDef{ id="statue", name="丰收雕像", icon="🗿", beauty=8, cost=300, desc="全局生长+5%，美观+8" },
        new DecorationDef{ id="flowerbed", name="花坛", icon="🌷", beauty=2, cost=30, desc="美观+2" },
        new DecorationDef{ id="windmill", name="风车", icon="🌬️", beauty=6, cost=200, desc="美观+6" },
    };

    static readonly string[] VisitorNames = {"旅行商人","农夫老王","园艺大师","远征猎人","神秘旅人"};

    public static void Tick(float dt)
    {
        // 访客计时
        GameState.visitorTimer -= dt;
        if (GameState.visitorTimer <= 0 && GameState.visitorState == "none")
        {
            // 美观度越高，访客越频繁
            float chance = 0.3f + GameState.farmBeauty * 0.01f;
            if (G.Rng.NextDouble() < chance)
            {
                GameState.visitorState = "visiting";
                GameState.visitorName = VisitorNames[G.RandInt(0, VisitorNames.Length - 1)];
                GameState.visitorTimer = G.Rand(30f, 60f);
                UIHost.ShowToast(GameState.visitorName + "来访了！点击访客互动", "info");
            }
            else GameState.visitorTimer = G.Rand(60f, 120f);
        }
        else if (GameState.visitorState == "visiting" && GameState.visitorTimer <= 0)
        {
            GameState.visitorState = "none";
            GameState.visitorTimer = G.Rand(60f, 120f);
        }
    }

    public static bool BuyDecoration(string id)
    {
        foreach (var item in ShopItems)
        {
            if (item.id != id) continue;
            if (GameState.gold < item.cost) { UIHost.ShowToast("金币不足", "warning"); return false; }
            GameState.gold -= item.cost;
            GameState.decorations.Add(new DecorationItem { id = item.id, name = item.name, icon = item.icon, beauty = item.beauty, x = G.RandInt(0,5), y = G.RandInt(0,5) });
            GameState.farmBeauty += item.beauty;
            UIHost.ShowToast("购买了" + item.name + "，美观+" + item.beauty, "success");
            SaveSystem.Save();
            return true;
        }
        return false;
    }

    public static void InteractVisitor()
    {
        if (GameState.visitorState != "visiting") return;
        // 根据美观度给奖励
        int reward = 20 + GameState.farmBeauty * 2;
        GameState.gold += reward;
        // 有概率给种子
        if (G.Rng.NextDouble() < 0.3f)
        {
            GreenhouseSystem.AddWarehouseItem("seeds", 2);
            UIHost.ShowToast(GameState.visitorName + "：" + reward + "金币 + 种子×2，再见！", "gold");
        }
        else UIHost.ShowToast(GameState.visitorName + "留下了 " + reward + " 金币", "gold");
        GameState.visitorState = "none";
        GameState.visitorTimer = G.Rand(60f, 120f);
        SaveSystem.Save();
    }

    // 美观度带来的全局金币加成
    public static float BeautyGoldBonus() { return 1f + GameState.farmBeauty * 0.005f; }
}
