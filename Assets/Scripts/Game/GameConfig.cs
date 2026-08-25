using System;
using System.Collections.Generic;
using UnityEngine;

// ================= 数据定义（移植自 config.js / ui.js getSkillStats） =================

[Serializable] public class WeaponDef { public string id, name, shortName, icon, mode, color, description; public int damage, range; public float cooldown, projectileSpeed; public int pierce; }
[Serializable] public class MapDef { public string id, name, danger, bgColor, accentColor, terrainGround, terrainSoil, terrainPath, terrainWater, terrainGlow; public int tier, entryFee, monsterCount, chestCount, raiderCount, gridSize; public float rareSeedChance, legendarySeedChance; public string[] decor; }
[Serializable] public class SkillDef { public string id, name, icon, key, type, color, desc; public float cooldown, energyCost, damage, range, stunDuration, dashDistance, invulnDuration, stealthDuration; public bool aoe; }
[Serializable] public class ConsumableDef { public string id, name, icon, key, desc; public float value, heal, damage, range; public bool aoe; }
[Serializable] public class MonsterDef { public string name, icon, type; public float hp, damage, speed, radius, collisionRadius, attackRange, attackCooldown, xp, gold; public bool aerial, ranged; }
[Serializable] public class CropDef { public string id, name, icon, rarity, rewardType, rewardLabel, upgradeSkill; public float growTime, sellPrice, seedPrice, cardChance; public bool rare; }
[Serializable] public class TerrainDef { public string[] decor; }

public static class GameData
{
    public static readonly WeaponDef[] Weapons = new WeaponDef[]
    {
        new WeaponDef{ id="harvest_sickle", name="丰收镰刃", shortName="镰刃", icon="镰", mode="melee", damage=18, range=82, cooldown=0.46f, color="#f2c45b", description="宽幅近战，可短暂打断敌人" },
        new WeaponDef{ id="pea_repeater", name="豌豆连弩", shortName="连弩", icon="弩", mode="ranged", damage=11, range=470, cooldown=0.28f, projectileSpeed=620, color="#75dc68", description="快速远射，适合持续压制" },
        new WeaponDef{ id="vine_staff", name="藤芯法杖", shortName="法杖", icon="杖", mode="pierce", damage=24, range=390, cooldown=0.72f, projectileSpeed=440, pierce=2, color="#7be5c4", description="灵藤波可贯穿多个目标" },
    };

    public static readonly MapDef[] Maps = new MapDef[]
    {
        new MapDef{ id="t1", name="荒废野田", tier=1, entryFee=50, danger="低危", monsterCount=6, chestCount=5, raiderCount=0, rareSeedChance=0.02f, legendarySeedChance=0, bgColor="#4d6848", accentColor="#8eae70", terrainGround="#384a35", terrainSoil="#5b4934", terrainPath="#807459", terrainWater="#385a5c", terrainGlow="#6f875b", decor=new string[]{"🌾","🌿","🪨","🌳","🪵"} },
        new MapDef{ id="t2", name="废弃小农庄", tier=2, entryFee=250, danger="中危", monsterCount=10, chestCount=7, raiderCount=1, rareSeedChance=0.10f, legendarySeedChance=0.01f, bgColor="#625d49", accentColor="#c1a76b", terrainGround="#4b4737", terrainSoil="#6a5036", terrainPath="#8c8065", terrainWater="#425a57", terrainGlow="#8e7950", decor=new string[]{"🌻","🪨","🛖","🪵","🌳"} },
        new MapDef{ id="t3", name="灾变农田", tier=3, entryFee=800, danger="高危", monsterCount=15, chestCount=9, raiderCount=2, rareSeedChance=0.25f, legendarySeedChance=0.06f, bgColor="#514541", accentColor="#bd7567", terrainGround="#463b35", terrainSoil="#5a342e", terrainPath="#78634e", terrainWater="#3d4a45", terrainGlow="#9a695b", decor=new string[]{"🍄","🦴","🪨","🌵","☣️"} },
        new MapDef{ id="t4", name="古老谷场", tier=4, entryFee=2000, danger="绝境", monsterCount=20, chestCount=12, raiderCount=3, rareSeedChance=0.45f, legendarySeedChance=0.15f, bgColor="#3b414a", accentColor="#938ba9", terrainGround="#2f3134", terrainSoil="#3d3940", terrainPath="#625f58", terrainWater="#263842", terrainGlow="#77708c", decor=new string[]{"🗿","🔮","🪨","🌲","✨"} },
    };

    public static readonly SkillDef[] Skills = new SkillDef[]
    {
        new SkillDef{ id="straw_smash", name="稻草猛击", icon="🌾", key="1", type="active", color="#e0b94d", cooldown=3, energyCost=20, damage=35, range=100, aoe=true, desc="对周围敌人造成35点伤害" },
        new SkillDef{ id="vine_bind", name="藤蔓缠绕", icon="🌿", key="2", type="active", color="#69ce78", cooldown=8, energyCost=30, damage=0, range=150, stunDuration=2.5f, desc="定身范围内敌人2.5秒" },
        new SkillDef{ id="earth_dash", name="泥土遁走", icon="💨", key="3", type="active", color="#64aee8", cooldown=6, energyCost=25, dashDistance=200, invulnDuration=0.8f, desc="短距离位移，短暂无敌" },
        new SkillDef{ id="smoke_screen", name="烟雾迷障", icon="💨", key="4", type="active", color="#a184d8", cooldown=15, energyCost=40, stealthDuration=3, desc="隐身3秒，敌人失去目标" },
    };

    public static readonly ConsumableDef[] Consumables = new ConsumableDef[]
    {
        new ConsumableDef{ id="herb_kit", name="草药包扎包", icon="💊", key="Q", value=40, heal=50, desc="回复50点生命值" },
        new ConsumableDef{ id="thorn_storm", name="荆棘狂潮", icon="🌵", key="R", value=120, damage=80, range=180, aoe=true, desc="大范围80点AOE伤害" },
        new ConsumableDef{ id="signal_flare", name="撤离信号弹", icon="🔥", key="E", value=280, desc="就地召唤撤离点（需宝箱获取）" },
    };

    public static readonly Dictionary<string, MonsterDef> Monsters = new Dictionary<string, MonsterDef>
    {
        { "boar",   new MonsterDef{ name="野猪", icon="🐗", type="boar",   hp=60, damage=15, speed=140, radius=18, collisionRadius=15, attackRange=40,  attackCooldown=1.2f,  xp=10, gold=8 } },
        { "bat",    new MonsterDef{ name="腐翼蝙蝠", icon="🦇", type="bat", hp=32, damage=10, speed=205, radius=15, collisionRadius=9,  attackRange=42,  attackCooldown=0.9f,  xp=9, gold=6, aerial=true } },
        { "spider", new MonsterDef{ name="毒雾蛛", icon="🕷️", type="spider", hp=44, damage=13, speed=108, radius=17, collisionRadius=13, attackRange=260, attackCooldown=1.65f, xp=11, gold=8, ranged=true } },
        { "locust", new MonsterDef{ name="巨型蝗虫", icon="🦗", type="locust", hp=35, damage=8, speed=160, radius=14, collisionRadius=12, attackRange=200, attackCooldown=1.8f, xp=8, gold=5, ranged=true } },
        { "wolf",   new MonsterDef{ name="野狼", icon="🐺", type="wolf", hp=50, damage=12, speed=180, radius=16, collisionRadius=14, attackRange=35, attackCooldown=1.0f, xp=12, gold=10 } },
    };

    public static readonly CropDef[] Crops = new CropDef[]
    {
        new CropDef{ id="pea_shooter", name="豌豆射手", icon="🫛", growTime=24, sellPrice=12, seedPrice=8, rarity="rare", cardChance=1, upgradeSkill="straw_smash", rewardType="attack_card", rewardLabel="必得攻击卡" },
        new CropDef{ id="sunflower", name="向日葵", icon="🌻", growTime=18, sellPrice=45, seedPrice=6, rarity="common", cardChance=1, upgradeSkill="all", rewardType="skill_card", rewardLabel="永久技能强化卡" },
        new CropDef{ id="watermelon", name="西瓜", icon="🍉", growTime=36, sellPrice=20, seedPrice=15, rarity="rare", cardChance=1, upgradeSkill="all", rewardType="consumable_skill_card", rewardLabel="一次性技能卡" },
        new CropDef{ id="cabbage", name="卷心菜", icon="🥬", growTime=28, sellPrice=18, seedPrice=10, rarity="common", cardChance=0, upgradeSkill="earth_dash", rewardType="healing", rewardLabel="草药包扎包" },
        new CropDef{ id="wheat", name="小麦", icon="🌾", growTime=15, sellPrice=15, seedPrice=5, rarity="common", cardChance=0, upgradeSkill=null, rewardType="gold", rewardLabel="金币" },
        new CropDef{ id="carrot", name="胡萝卜", icon="🥕", growTime=20, sellPrice=25, seedPrice=10, rarity="common", cardChance=0.14f, upgradeSkill="earth_dash" },
        new CropDef{ id="corn", name="玉米", icon="🌽", growTime=30, sellPrice=45, seedPrice=15, rarity="rare", cardChance=0.20f, upgradeSkill="vine_bind" },
        new CropDef{ id="pumpkin", name="南瓜", icon="🎃", growTime=45, sellPrice=80, seedPrice=25, rarity="rare", cardChance=0.30f, upgradeSkill="smoke_screen" },
        new CropDef{ id="moon_rice", name="月光稻", icon="✨", growTime=60, sellPrice=200, seedPrice=0, rarity="legendary", cardChance=0.58f, upgradeSkill="all", rare=true },
    };
}

// ================= 游戏运行时状态（移植自 config.js GameState + save.js） =================
public static class GameState
{
    public static string screen = "menu"; // menu / farm / prep / expedition / result
    public static int gold = 500;
    public static int seeds = 3;
    public static int materials = 0;
    public static Plot[] farmPlots = new Plot[0]; // 36
    public static int unlockedPlots = 8;
    public static string selectedCrop = "wheat";
    public static List<string> unlockedCrops = new List<string>{ "pea_shooter","sunflower","watermelon","cabbage","wheat" };
    public static Dictionary<string,int> skillLevels = new Dictionary<string,int>{{"straw_smash",1},{"vine_bind",1},{"earth_dash",1},{"smoke_screen",1}};
    public static List<Card> cardInventory = new List<Card>();
    public static List<string> selectedBoostCards = new List<string>();
    public static string selectedMap = "t1";
    public static string selectedWeapon = "harvest_sickle";
    public static Dictionary<string,int> loadout = new Dictionary<string,int>{{"herb_kit",2},{"thorn_storm",1},{"signal_flare",0}};
    public static Dictionary<string,int> farmItems = new Dictionary<string,int>{{"growth_catalyst",0}};
    public static string lastDailyClaim = "";
    public static int dailyStreak = 0;
    public static string lastReliefClaim = "";
    public static Expedition expedition = null;

    public static void EnsurePlots()
    {
        if (farmPlots.Length == 36) return;
        farmPlots = new Plot[36];
        for (int i = 0; i < 36; i++) farmPlots[i] = new Plot();
    }
}

[Serializable] public class Plot { public CropDef crop; public double plantedAt; public bool ready; public string status; }
[Serializable] public class Card { public string id, rarity, skillId, icon, name, desc; public int power; public bool singleUse; }

// ================= 工具函数 =================
public static class G
{
    public static System.Random Rng = new System.Random();
    public static float Rand(float min, float max) { return (float)(Rng.NextDouble() * (max - min) + min); }
    public static int RandInt(int min, int max) { return Rng.Next(min, max + 1); }
    public static float Clamp(float v, float min, float max) { return Mathf.Max(min, Mathf.Min(max, v)); }
    public static int Clamp(int v, int min, int max) { return Math.Max(min, Math.Min(max, v)); }
    public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
    public static float Dist(float ax, float ay, float bx, float by) { return (float)Math.Sqrt((ax-bx)*(ax-bx) + (ay-by)*(ay-by)); }
    public static float Dist(float ax, float ay, object b) { return 0; } // placeholder (unused)
    public static Color ParseColor(string hex)
    {
        Color c; if (ColorUtility.TryParseHtmlString(hex, out c)) return c; return Color.white;
    }
}

// ================= 技能数值（移植自 ui.js getSkillStats） =================
public class SkillStats
{
    public SkillDef def;
    public int level, baseLevel, extraLevels;
    public int damage, range, dashDistance;
    public float stunDuration, stealthDuration, cooldown, energyCost;
}

public static class SkillMath
{
    public static SkillStats GetStats(SkillDef skill, int extraLevels = 0)
    {
        int baseLevel = G.Clamp(GameState.skillLevels.ContainsKey(skill.id) ? GameState.skillLevels[skill.id] : 1, 1, 8);
        int level = G.Clamp(baseLevel + extraLevels, 1, 12);
        int bonus = level - 1;
        var s = new SkillStats();
        s.def = skill; s.level = level; s.baseLevel = baseLevel; s.extraLevels = extraLevels;
        s.damage = skill.damage > 0 ? (int)Math.Round(skill.damage * (1 + bonus * 0.18)) : 0;
        s.range = skill.range > 0 ? (int)Math.Round(skill.range * (1 + bonus * 0.045)) : (int)skill.range;
        s.dashDistance = skill.dashDistance > 0 ? (int)Math.Round(skill.dashDistance * (1 + bonus * 0.08)) : (int)skill.dashDistance;
        s.stunDuration = skill.stunDuration > 0 ? (float)Math.Round(skill.stunDuration + bonus * 0.18, 1) : skill.stunDuration;
        s.stealthDuration = skill.stealthDuration > 0 ? (float)Math.Round(skill.stealthDuration + bonus * 0.25, 1) : skill.stealthDuration;
        s.cooldown = (float)Math.Round(Math.Max(1, skill.cooldown * (1 - bonus * 0.055)), 1);
        s.energyCost = Math.Max(8, skill.energyCost - bonus * 2);
        return s;
    }
}
