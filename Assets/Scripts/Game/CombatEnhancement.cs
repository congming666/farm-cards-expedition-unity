using System;
using System.Collections.Generic;
using UnityEngine;

// ================= v0.6.0 战斗系统大升级（Unity版） =================
public static class CombatEnhancement
{
    // ===== 全局状态 =====
    public static int combo = 0;
    public static float comboTimer = 0;
    public static float rage = 0;
    public static float rageMax = 100;
    public static float dodgeCd = 0;
    public static float perfectDodgeWindow = 0;
    public static float slowMotion = 0;
    public static bool nextAttackCrit = false;
    public static float hitStop = 0;
    public static float torchFuel = 100;
    public static int bossPhase = 1;
    public static bool executing = false;
    public static Monster execTarget = null;
    public static float execTimer = 0;
    public static bool branchActive = false;
    public static float elapsed = 0;
    static Expedition _exp;

    [Serializable]
    public class Destructible { public string type, name, icon, color; public float hp, radius, explodeRadius, explodeDamage; public float x, y, currentHp; public bool destroyed; }
    public static List<Destructible> destructibles = new List<Destructible>();

    public static string[] AffixNames = { "狂暴", "迅捷", "吸血", "分裂" };
    public static string[] AITypes = { "chaser", "charger", "ranged", "bomber", "healer" };

    // ===== 初始化 =====
    public static void Init(Expedition exp)
    {
        _exp = exp;
        combo = 0; comboTimer = 0; rage = 0; dodgeCd = 0;
        perfectDodgeWindow = 0; slowMotion = 0; nextAttackCrit = false;
        hitStop = 0; torchFuel = 100; bossPhase = 1;
        executing = false; execTarget = null; execTimer = 0;
        branchActive = false; elapsed = 0;
        destructibles.Clear();
        SpawnDestructibles();
    }

    // ===== 系统1：连击 =====
    public static void OnEnemyHit() { combo++; comboTimer = 3f; AddRage(2); }
    public static void OnPlayerHit() { if (combo > 5) Debug.Log($"连击中断！{combo}"); combo = 0; comboTimer = 0; AddRage(8); }
    public static float GetComboMul() { return 1f + Mathf.Min(combo * 0.05f, 1f); }

    // ===== 系统2：完美闪避 =====
    public static bool TryDodge()
    {
        if (dodgeCd > 0 || _exp == null) return false;
        var p = _exp.player;
        float dx = 0, dy = 0;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dy -= 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dy += 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dx -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dx += 1;
        if (dx == 0 && dy == 0) { dx = Mathf.Cos(p.angle); dy = Mathf.Sin(p.angle); }
        float len = Mathf.Sqrt(dx * dx + dy * dy); if (len < 0.01f) len = 1;
        p.x = Mathf.Clamp(p.x + dx / len * 120, 30, 2260);
        p.y = Mathf.Clamp(p.y + dy / len * 120, 30, 2260);
        p.invuln = Mathf.Max(p.invuln, 0.35f);
        perfectDodgeWindow = 0.2f;
        dodgeCd = 1.2f;
        return true;
    }
    public static bool CheckPerfectDodge()
    {
        if (perfectDodgeWindow > 0)
        {
            perfectDodgeWindow = 0;
            slowMotion = 0.3f;
            nextAttackCrit = true;
            AddRage(15);
            Debug.Log("完美闪避！下次必暴击");
            return true;
        }
        return false;
    }

    // ===== 系统3：血条分段 =====
    public static int GetSegments(Monster m) { if (m.type == "boss") return 3; if (m.elite) return 2; return 1; }
    public static void OnSegmentBreak(Monster m)
    {
        m.stunned = Mathf.Max(m.stunned, 0.6f);
        m.hitFlash = 0.3f;
        Debug.Log("击破护甲！敌人硬直");
    }

    // ===== 系统4：敌人AI多样化 =====
    public static void AssignAIType(Monster m)
    {
        if (m.type == "boss") { m.aiType = "boss"; return; }
        if (m.type == "boar") { m.aiType = "charger"; return; }
        float roll = UnityEngine.Random.value;
        int tier = _exp != null ? _exp.map.tier : 1;
        if (roll < 0.15f + tier * 0.05f) m.aiType = "ranged";
        else if (roll < 0.25f + tier * 0.05f) m.aiType = "bomber";
        else if (roll < 0.32f + tier * 0.03f && tier >= 2) m.aiType = "healer";
        else m.aiType = "chaser";
    }
    public static void UpdateMonsterAI(Monster m, float dt)
    {
        if (_exp == null) return;
        var p = _exp.player;
        float d = Vector2.Distance(new Vector2(m.x, m.y), new Vector2(p.x, p.y));
        m.aiTimer = (m.aiTimer > 0 ? m.aiTimer : 0) - dt;
        switch (m.aiType)
        {
            case "charger":
                if (m.aiState == "chase")
                {
                    if (d > 100 && d < 300 && m.aiTimer <= 0) { m.aiState = "windup"; m.aiTimer = 0.8f; m.chargeAngle = Mathf.Atan2(p.y - m.y, p.x - m.x); }
                    else { float a = Mathf.Atan2(p.y - m.y, p.x - m.x); m.x += Mathf.Cos(a) * m.speed * 0.6f * dt; m.y += Mathf.Sin(a) * m.speed * 0.6f * dt; }
                }
                else if (m.aiState == "windup") { if (m.aiTimer <= 0) { m.aiState = "charging"; m.aiTimer = 0.4f; } }
                else if (m.aiState == "charging")
                {
                    m.x += Mathf.Cos(m.chargeAngle) * 400 * dt; m.y += Mathf.Sin(m.chargeAngle) * 400 * dt;
                    if (d < 35) _exp.DamagePlayer(m.damage * 1.2f);
                    if (m.aiTimer <= 0) { m.aiState = "chase"; m.aiTimer = 3f; }
                }
                break;
            case "ranged":
                if (d < 180) { float a = Mathf.Atan2(m.y - p.y, m.x - p.x); m.x += Mathf.Cos(a) * m.speed * dt; m.y += Mathf.Sin(a) * m.speed * dt; }
                else if (d > 350) { float a = Mathf.Atan2(p.y - m.y, p.x - m.x); m.x += Mathf.Cos(a) * m.speed * 0.7f * dt; m.y += Mathf.Sin(a) * m.speed * 0.7f * dt; }
                if (m.aiTimer <= 0 && d < 400)
                {
                    m.aiTimer = 2f;
                    float a = Mathf.Atan2(p.y - m.y, p.x - m.x);
                    _exp.projectiles.Add(new Projectile { x = m.x, y = m.y, vx = Mathf.Cos(a) * 250, vy = Mathf.Sin(a) * 250, damage = m.damage * 0.7f, life = 2, radius = 6, fromMonster = true, color = "#aa66ff", pierce = "1" });
                }
                break;
            case "bomber":
                if (d > 50) { float a = Mathf.Atan2(p.y - m.y, p.x - m.x); m.x += Mathf.Cos(a) * m.speed * 1.3f * dt; m.y += Mathf.Sin(a) * m.speed * 1.3f * dt; }
                else if (!m.exploding) { m.exploding = true; m.aiTimer = 1.2f; }
                if (m.exploding && m.aiTimer <= 0)
                {
                    if (d < 80) _exp.DamagePlayer(m.damage * 1.5f);
                    m.hp = 0;
                }
                break;
            case "healer":
                Monster target = null; float minD = 300;
                foreach (var other in _exp.monsters) { if (other == m || other.hp <= 0) continue; if (other.hp < other.maxHp * 0.8f) { float od = Vector2.Distance(new Vector2(other.x, other.y), new Vector2(m.x, m.y)); if (od < minD) { minD = od; target = other; } } }
                if (target != null)
                {
                    float a = Mathf.Atan2(target.y - m.y, target.x - m.x);
                    if (minD > 150) { m.x += Mathf.Cos(a) * m.speed * 0.8f * dt; m.y += Mathf.Sin(a) * m.speed * 0.8f * dt; }
                    if (m.aiTimer <= 0) { m.aiTimer = 3f; target.hp = Mathf.Min(target.maxHp, target.hp + target.maxHp * 0.15f); }
                }
                break;
        }
    }

    // ===== 系统5：精英词缀 =====
    public static void MakeElite(Monster m)
    {
        m.elite = true;
        m.maxHp = Mathf.RoundToInt(m.maxHp * 2.5f); m.hp = m.maxHp;
        m.damage = Mathf.RoundToInt(m.damage * 1.3f);
        m.affixes = new List<string>();
        int count = UnityEngine.Random.value < 0.4f ? 2 : 1;
        var pool = new List<string>(AffixNames);
        for (int i = 0; i < count && pool.Count > 0; i++) { int idx = UnityEngine.Random.Range(0, pool.Count); m.affixes.Add(pool[idx]); pool.RemoveAt(idx); }
        if (m.affixes.Contains("迅捷")) m.speed *= 1.5f;
    }
    public static void OnEliteDeath(Monster m)
    {
        if (!m.elite || m.affixes == null || !m.affixes.Contains("分裂")) return;
        for (int i = 0; i < 2; i++)
        {
            var mini = new Monster
            {
                type = m.type, x = m.x + UnityEngine.Random.Range(-20, 20), y = m.y + UnityEngine.Random.Range(-20, 20),
                hp = m.maxHp * 0.25f, maxHp = m.maxHp * 0.25f,
                damage = Mathf.RoundToInt(m.damage * 0.5f), speed = m.speed * 1.2f,
                radius = m.radius * 0.7f, elite = false, aiType = "chaser",
                facing = 0, state = "idle", stateTimer = 0, stunned = 0, hitFlash = 0
            };
            _exp.monsters.Add(mini);
        }
        Debug.Log("精英分裂！");
    }
    public static void OnEliteHitPlayer(Monster m)
    {
        if (m.elite && m.affixes != null && m.affixes.Contains("吸血")) m.hp = Mathf.Min(m.maxHp, m.hp + m.damage * 0.3f);
    }

    // ===== 系统6：环境互动 =====
    static void SpawnDestructibles()
    {
        destructibles.Clear();
        string[] types = { "barrel", "rock", "poison" };
        string[] icons = { "🛢️", "🪨", "☣️" };
        string[] colors = { "#cc6633", "#888888", "#66cc44" };
        float[] hp = { 15, 25, 10 };
        float[] rad = { 18, 22, 16 };
        float[] expRad = { 90, 60, 100 };
        float[] expDmg = { 40, 25, 15 };
        int count = 4 + UnityEngine.Random.Range(0, 4);
        for (int i = 0; i < count; i++)
        {
            int t = UnityEngine.Random.Range(0, 3);
            destructibles.Add(new Destructible
            {
                type = types[t], name = types[t], icon = icons[t], color = colors[t],
                hp = hp[t], radius = rad[t], explodeRadius = expRad[t], explodeDamage = expDmg[t],
                x = UnityEngine.Random.Range(150, 2100), y = UnityEngine.Random.Range(150, 2100),
                currentHp = hp[t], destroyed = false
            });
        }
    }
    public static void DamageDestructible(float x, float y, float dmg)
    {
        foreach (var d in destructibles)
        {
            if (d.destroyed) continue;
            if (Vector2.Distance(new Vector2(d.x, d.y), new Vector2(x, y)) < d.radius + 20)
            {
                d.currentHp -= dmg;
                if (d.currentHp <= 0) ExplodeDestructible(d);
            }
        }
    }
    static void ExplodeDestructible(Destructible d)
    {
        d.destroyed = true;
        hitStop = Mathf.Max(hitStop, 0.05f);
        foreach (var m in _exp.monsters)
        {
            if (m.hp <= 0) continue;
            if (Vector2.Distance(new Vector2(m.x, m.y), new Vector2(d.x, d.y)) < d.explodeRadius)
                _exp.DamageEnemy(m, d.explodeDamage, d.color, true);
        }
        if (Vector2.Distance(new Vector2(_exp.player.x, _exp.player.y), new Vector2(d.x, d.y)) < d.explodeRadius)
            _exp.DamagePlayer(d.explodeDamage * 0.5f);
        Debug.Log($"{d.name}爆炸！");
    }

    // ===== 系统7：怒气超杀 =====
    public static void AddRage(float amount)
    {
        if (_exp == null) return;
        float hpPct = _exp.player.hp / _exp.player.maxHp;
        float mul = hpPct < 0.3f ? 2f : (hpPct < 0.6f ? 1.5f : 1f);
        rage = Mathf.Min(rageMax, rage + amount * mul);
    }
    public static bool TryUltimate()
    {
        if (rage < rageMax || _exp == null) return false;
        rage = 0;
        var p = _exp.player;
        p.invuln = Mathf.Max(p.invuln, 2f);
        slowMotion = 0.5f; hitStop = 0.15f;
        foreach (var m in _exp.monsters)
        {
            if (m.hp <= 0) continue;
            float d = Vector2.Distance(new Vector2(m.x, m.y), new Vector2(p.x, p.y));
            if (d < 400) _exp.DamageEnemy(m, m.type == "boss" ? m.maxHp * 0.25f : 999, "#ffdd44", true);
        }
        Debug.Log("⚡ 超杀释放！");
        return true;
    }

    // ===== 系统8：Boss多阶段 =====
    static Dictionary<int, bool> _phaseTriggered = new Dictionary<int, bool>();
    public static void UpdateBossPhase(Monster boss)
    {
        if (boss.type != "boss") return;
        float hpPct = boss.hp / boss.maxHp;
        if (hpPct <= 0.6f && !_phaseTriggered.ContainsKey(2)) { _phaseTriggered[2] = true; bossPhase = 2; boss.damage = Mathf.RoundToInt(boss.damage * 1.2f); boss.speed *= 1.1f; Debug.Log("Boss阶段2：狂暴化！"); }
        if (hpPct <= 0.3f && !_phaseTriggered.ContainsKey(3)) { _phaseTriggered[3] = true; bossPhase = 3; boss.damage = Mathf.RoundToInt(boss.damage * 1.3f); boss.abilityCd = 1.5f; Debug.Log("Boss阶段3：终极狂暴！"); }
    }
    public static void ResetBossPhase() { _phaseTriggered.Clear(); bossPhase = 1; }

    // ===== 系统9：Roguelike岔路 =====
    public static void TriggerBranch() { if (branchActive) return; branchActive = true; if (_exp != null) _exp.paused = true; }
    public static void ChooseBranch(int idx)
    {
        if (!branchActive || _exp == null) return;
        branchActive = false; _exp.paused = false;
        var p = _exp.player;
        switch (idx)
        {
            case 0: GameState.gold += 50; Debug.Log("宝箱：获得金币"); break;
            case 1: var m = _exp.monsters.Find(x => x.hp > 0); if (m != null) MakeElite(m); Debug.Log("精英战！"); break;
            case 2: GameState.gold = Mathf.Max(0, GameState.gold - 30); p.hp = Mathf.Min(p.maxHp, p.hp + 40); AddRage(30); Debug.Log("商店：回血+怒气"); break;
        }
    }

    // ===== 系统10：生存压力 =====
    static void UpdateTorch(float dt)
    {
        torchFuel = Mathf.Max(0, torchFuel - dt * 0.5f);
    }

    // ===== 系统11：处决 =====
    public static bool CanExecute(Monster m) { return m != null && (m.elite || m.type == "boss") && m.hp > 0 && m.hp / m.maxHp < 0.15f && !executing; }
    public static bool TryExecute(Monster m)
    {
        if (!CanExecute(m)) return false;
        executing = true; execTarget = m; execTimer = 1.2f;
        if (_exp != null) _exp.player.invuln = Mathf.Max(_exp.player.invuln, 1.5f);
        slowMotion = 0.3f; hitStop = 0.1f;
        return true;
    }
    static void UpdateExecute(float dt)
    {
        if (!executing) return;
        execTimer -= dt;
        if (execTimer <= 0 && execTarget != null)
        {
            execTarget.hp = 0;
            AddRage(40); GameState.gold += 30;
            executing = false; execTarget = null;
            Debug.Log("处决成功！");
        }
    }

    // ===== 主tick =====
    public static void Update(float dt)
    {
        elapsed += dt;
        if (hitStop > 0) { hitStop -= dt; return; }
        float timeScale = slowMotion > 0 ? 0.3f : 1f;
        if (slowMotion > 0) slowMotion -= dt;
        float sdt = dt * timeScale;
        if (comboTimer > 0) { comboTimer -= sdt; if (comboTimer <= 0) combo = 0; }
        if (dodgeCd > 0) dodgeCd -= sdt;
        if (perfectDodgeWindow > 0) perfectDodgeWindow -= sdt;
        UpdateTorch(sdt);
        UpdateExecute(sdt);
        if (_exp != null)
        {
            foreach (var m in _exp.monsters) if (m.hp > 0 && m.elite && m.affixes != null && m.affixes.Contains("狂暴") && m.hp < m.maxHp * 0.3f) m.aiTimer = Mathf.Max(0, m.aiTimer - sdt);
            var boss = _exp.monsters.Find(m => m.type == "boss" && m.hp > 0);
            if (boss != null) UpdateBossPhase(boss);
        }
    }
}
