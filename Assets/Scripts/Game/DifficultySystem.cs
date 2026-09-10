using System;
using System.Collections.Generic;
using UnityEngine;

// ================= v0.7.0 难度系统（Unity版） =================
public static class DifficultySystem
{
    [Serializable]
    public class DifficultyDef {
        public string id, name, icon;
        public float hpMul, dmgMul, supplyMul, torchMul, rewardMul;
        public int maxAffixes;
        public float comboCap, dodgeWindow, dodgeCritDmg, ultBossDmg, ultEliteDmg, executeDmg, envPlayerMul;
        public bool dodgeCrit, executeKill, ultDisabled;
    }

    public static Dictionary<string, DifficultyDef> Difficulties = new Dictionary<string, DifficultyDef> {
        {"casual", new DifficultyDef{ id="casual", name="休闲", icon="🌱", hpMul=0.7f, dmgMul=0.7f, maxAffixes=1, supplyMul=1.3f, torchMul=0.5f, rewardMul=0.8f, comboCap=1.0f, dodgeWindow=200, dodgeCrit=true, ultBossDmg=0.25f, ultEliteDmg=0.99f, executeKill=true, envPlayerMul=0.5f }},
        {"normal", new DifficultyDef{ id="normal", name="普通", icon="⚔️", hpMul=1.0f, dmgMul=1.0f, maxAffixes=2, supplyMul=1.0f, torchMul=1.0f, rewardMul=1.0f, comboCap=0.6f, dodgeWindow=200, dodgeCrit=true, ultBossDmg=0.25f, ultEliteDmg=0.99f, executeKill=true, envPlayerMul=0.5f }},
        {"hard", new DifficultyDef{ id="hard", name="困难", icon="🔥", hpMul=1.4f, dmgMul=1.3f, maxAffixes=2, supplyMul=0.7f, torchMul=1.5f, rewardMul=1.5f, comboCap=0.4f, dodgeWindow=150, dodgeCrit=false, dodgeCritDmg=1.5f, ultBossDmg=0.15f, ultEliteDmg=0.5f, executeKill=false, executeDmg=0.8f, envPlayerMul=1.0f }},
        {"nightmare", new DifficultyDef{ id="nightmare", name="噩梦", icon="💀", hpMul=2.0f, dmgMul=1.8f, maxAffixes=3, supplyMul=0.4f, torchMul=2.0f, rewardMul=2.5f, comboCap=0.3f, dodgeWindow=100, dodgeCrit=false, dodgeCritDmg=1.5f, ultBossDmg=0.15f, ultEliteDmg=0.5f, executeKill=false, executeDmg=0.8f, envPlayerMul=1.5f }}
    };

    [Serializable]
    public class TierMechanic {
        public string name, subtitle;
        public float visionMul, beastWaveInterval, nightRaidChance, nightRaidDuration;
        public bool nightRaid, poisonDOT, rockfall, dark, miniBoss, chargerUnlocked, rangedUnlocked, healerUnlocked, bomberUnlocked, chargerDominant, allAITypes;
        public float poisonDPS, poisonDuration, rockfallInterval, rockfallDamage, rockfallRadius, torchMulExtra, eliteChanceBonus, obstacleDensity, poisonBarrelDensity;
        public int miniBossCount;
    }

    public static Dictionary<int, TierMechanic> TierMechanics = new Dictionary<int, TierMechanic> {
        {1, new TierMechanic{ name="荒废野田", subtitle="兽潮频发 · 夜间突袭", visionMul=1.0f, beastWaveInterval=90, nightRaid=true, nightRaidChance=0.15f, nightRaidDuration=30, chargerUnlocked=true, eliteChanceBonus=0 }},
        {2, new TierMechanic{ name="废弃小农庄", subtitle="常驻雾天 · 剧毒蔓延", visionMul=0.7f, beastWaveInterval=120, poisonDOT=true, poisonDPS=3, poisonDuration=4, rangedUnlocked=true, healerUnlocked=true, poisonBarrelDensity=2.0f, eliteChanceBonus=0.05f }},
        {3, new TierMechanic{ name="灾变农田", subtitle="峡谷险地 · 落石无情", visionMul=0.9f, beastWaveInterval=100, rockfall=true, rockfallInterval=30, rockfallDamage=30, rockfallRadius=80, chargerDominant=true, bomberUnlocked=true, obstacleDensity=1.5f, eliteChanceBonus=0.1f }},
        {4, new TierMechanic{ name="古老谷场", subtitle="永恒黑暗 · 深渊巡逻", visionMul=0.5f, beastWaveInterval=80, dark=true, torchMulExtra=2.0f, miniBoss=true, miniBossCount=1, allAITypes=true, eliteChanceBonus=0.15f }}
    };

    public class HeatDef { public string id, name, desc; public float rewardBonus; }
    public static Dictionary<string, HeatDef> HeatModifiers = new Dictionary<string, HeatDef> {
        {"ironwall", new HeatDef{ id="ironwall", name="铁壁", desc="所有怪物+50%血量", rewardBonus=0.2f }},
        {"frenzy", new HeatDef{ id="frenzy", name="狂乱", desc="所有怪物+30%攻速", rewardBonus=0.25f }},
        {"darkness", new HeatDef{ id="darkness", name="黑暗", desc="视野永久-30%", rewardBonus=0.15f }},
        {"barren", new HeatDef{ id="barren", name="贫瘠", desc="补给掉落-50%", rewardBonus=0.3f }},
        {"headless", new HeatDef{ id="headless", name="无头", desc="禁用怒气超杀", rewardBonus=0.5f }}
    };

    // 运行时状态
    public static string CurrentDifficulty = "normal";
    public static List<string> CurrentHeat = new List<string>();
    public static int CurrentTier = 1;
    public static float HpMul=1, DmgMul=1, SpeedMulExtra=1, SupplyMul=1, TorchMul=1, RewardMul=1;
    public static float ComboCap=0.6f, DodgeWindow=200, UltBossDmg=0.25f, UltEliteDmg=0.99f, ExecuteDmg=1, EnvPlayerMul=0.5f, VisionMul=1;
    public static int MaxAffixes=2;
    public static bool DodgeCrit=true, ExecuteKill=true, UltDisabled=false;
    public static float NightRaidTimer=0, NightRaidCooldown=60, RockfallTimer=30, PoisonTickTimer=0;
    public static bool NightRaidActive=false;

    public static void ApplyDifficulty(string diffId, List<string> heatIds, int tier) {
        var d = Difficulties.ContainsKey(diffId) ? Difficulties[diffId] : Difficulties["normal"];
        var m = TierMechanics.ContainsKey(tier) ? TierMechanics[tier] : TierMechanics[1];
        CurrentDifficulty = diffId; CurrentHeat = heatIds ?? new List<string>(); CurrentTier = tier;
        HpMul = d.hpMul; DmgMul = d.dmgMul; MaxAffixes = d.maxAffixes;
        SupplyMul = d.supplyMul; TorchMul = d.torchMul * (m.torchMulExtra > 0 ? m.torchMulExtra : 1);
        RewardMul = d.rewardMul; ComboCap = d.comboCap; DodgeWindow = d.dodgeWindow;
        DodgeCrit = d.dodgeCrit; UltBossDmg = d.ultBossDmg; UltEliteDmg = d.ultEliteDmg;
        ExecuteKill = d.executeKill; ExecuteDmg = d.executeDmg > 0 ? d.executeDmg : 1;
        EnvPlayerMul = d.envPlayerMul; VisionMul = m.visionMul;
        UltDisabled = false; SpeedMulExtra = 1;
        float hpExtra = 1, supplyExtra = 1, visionExtra = 1;
        foreach (var id in CurrentHeat) {
            switch (id) {
                case "ironwall": hpExtra *= 1.5f; break;
                case "frenzy": SpeedMulExtra *= 1.3f; break;
                case "darkness": visionExtra *= 0.7f; break;
                case "barren": supplyExtra *= 0.5f; break;
                case "headless": UltDisabled = true; break;
            }
        }
        HpMul *= hpExtra; SupplyMul *= supplyExtra; VisionMul *= visionExtra;
        NightRaidActive = false; NightRaidTimer = 0; NightRaidCooldown = 60;
        RockfallTimer = m.rockfallInterval > 0 ? m.rockfallInterval : 30; PoisonTickTimer = 0;
    }

    public static float GetHeatRewardMultiplier() {
        float mul = 1.0f;
        foreach (var id in CurrentHeat) if (HeatModifiers.ContainsKey(id)) mul += HeatModifiers[id].rewardBonus;
        return mul;
    }

    public static float GetEffectiveVision() {
        var m = TierMechanics.ContainsKey(CurrentTier) ? TierMechanics[CurrentTier] : TierMechanics[1];
        float v = VisionMul;
        if (m.nightRaid && NightRaidActive) v *= 0.6f;
        return v;
    }

    public static void Tick(float dt, Expedition exp) {
        var m = TierMechanics.ContainsKey(CurrentTier) ? TierMechanics[CurrentTier] : TierMechanics[1];
        if (m.nightRaid) {
            if (NightRaidActive) { NightRaidTimer -= dt; if (NightRaidTimer <= 0) { NightRaidActive = false; NightRaidCooldown = 60; } }
            else { NightRaidCooldown -= dt; if (NightRaidCooldown <= 0 && UnityEngine.Random.value < m.nightRaidChance * dt * 10) { NightRaidActive = true; NightRaidTimer = m.nightRaidDuration; Debug.Log("夜间突袭！"); } }
        }
        if (m.rockfall) {
            RockfallTimer -= dt;
            if (RockfallTimer <= 0 && exp != null && exp.player != null) {
                RockfallTimer = m.rockfallInterval;
                float px = exp.player.x + (UnityEngine.Random.value - 0.5f) * 300;
                float py = exp.player.y + (UnityEngine.Random.value - 0.5f) * 300;
                if (Vector2.Distance(new Vector2(exp.player.x, exp.player.y), new Vector2(px, py)) < m.rockfallRadius) exp.DamagePlayer(m.rockfallDamage);
                foreach (var mon in exp.monsters) if (mon.hp > 0 && Vector2.Distance(new Vector2(mon.x, mon.y), new Vector2(px, py)) < m.rockfallRadius) exp.DamageEnemy(mon, m.rockfallDamage, "#888888", true);
            }
        }
    }

    public static void ApplyPoison(Monster m, Expedition exp) {
        var tm = TierMechanics.ContainsKey(CurrentTier) ? TierMechanics[CurrentTier] : TierMechanics[1];
        if (!tm.poisonDOT || exp == null || exp.player == null) return;
        exp.player.poisoned = true; exp.player.poisonTimer = tm.poisonDuration; exp.player.poisonDPS = tm.poisonDPS;
    }

    public static void TickPoison(float dt, Expedition exp) {
        if (exp == null || exp.player == null || !exp.player.poisoned) return;
        exp.player.poisonTimer -= dt; PoisonTickTimer -= dt;
        if (PoisonTickTimer <= 0) { PoisonTickTimer = 1; exp.DamagePlayer(exp.player.poisonDPS > 0 ? exp.player.poisonDPS : 3); }
        if (exp.player.poisonTimer <= 0) exp.player.poisoned = false;
    }
}
