using System;
using System.Collections.Generic;
using UnityEngine;

// ============ 远征系统核心（移植自 expedition.js） ============
public partial class Expedition
{
    public MapDef map;
    public float timeLeft;
    public PlayerState player;
    public float[] skillCooldowns = new float[4];
    public float[] skillFlashes = new float[4];
    public float attackAnim, weaponPulse, hitStop, killFlash, screenShake, elapsed, nextEventAt;
    public int weaponIndex;
    public WeaponDef weapon;
    public Dictionary<string,int> consumableFlashes = new Dictionary<string,int>();
    public Dictionary<string,int> skillBoosts = new Dictionary<string,int>();
    public Dictionary<string,int> consumables = new Dictionary<string,int>();
    public List<Monster> monsters = new List<Monster>();
    public List<Chest> chests = new List<Chest>();
    public List<Tower> towers = new List<Tower>();
    public List<Raider> raiders = new List<Raider>();
    public List<TerrainPatch> terrainPatches = new List<TerrainPatch>();
    public List<TerrainField> terrainFields = new List<TerrainField>();
    public List<TerrainRoad> terrainRoads = new List<TerrainRoad>();
    public List<TerrainDecor> terrainDecor = new List<TerrainDecor>();
    public List<Obstacle> obstacles = new List<Obstacle>();
    public List<Trap> traps = new List<Trap>();
    public List<GroundLoot> groundLoot = new List<GroundLoot>();
    public List<Projectile> projectiles = new List<Projectile>();
    public List<Particle> particles = new List<Particle>();
    public List<DamageNumber> damageNumbers = new List<DamageNumber>();
    public List<ExtractPoint> extractPoints = new List<ExtractPoint>();
    public List<GroundLoot> bag = new List<GroundLoot>();
    public bool extracting, paused, gameOver, bossSpawned;
    public float extractProgress; public string extractType, result;
    public int killCount, chestOpened; public float damageTaken;
    public Balance balance;
    public Mission objective; public Monster boss;
    public List<MapEvent> mapEvents = new List<MapEvent>();
    public MapEvent activeEvent;
    public EventModifiers eventModifiers = new EventModifiers();
    public BeastWave beastWave = new BeastWave();
    public ECamera camera = new ECamera();
    public float visionCellSize = 96, visionRadius = 360;
    public HashSet<long> exploredCells = new HashSet<long>();
    public float fogUpdateInterval = 0.12f, fogUpdateTimer; public bool fogDirty = true;
    public Vector2 sunVector;
    public Dictionary<string,bool> keys = new Dictionary<string,bool>();
    public Vector2 mouse = Vector2.zero; public bool mouseDown;
    public Action<string> OnToastCallback;

    public class Balance { public float enemyHp,enemyDamage,enemySpeed,reward,eliteChance; public float bossHp,bossDamage; }
    public class EventModifiers { public float enemySpeed=1,enemyDamage=1,loot=1,vision=1; }
    public class BeastWave { public int wave; public float nextIn=48; public bool active; public int remaining; public float duration; }

    public Expedition(string mapId, WeaponDef startWeapon)
    {
        map = GameData.Maps[0];
        foreach(var m in GameData.Maps) if(m.id==mapId) map=m;
        timeLeft = 720f;
        player = new PlayerState{ x=640, y=360, hp=100, maxHp=100, energy=100, maxEnergy=100, speed=220, radius=16, collisionRadius=11 };
        skillBoosts = CardSystem.GetSelectedBoosts();
        consumables = new Dictionary<string,int>();
        foreach(var kv in GameState.loadout) consumables[kv.Key]=kv.Value;
        foreach(string id in new[]{"herb_kit","thorn_storm","signal_flare"}) if(!consumables.ContainsKey(id)) consumables[id]=0;
        weaponIndex = 0; weapon = startWeapon;
        for(int i=0;i<GameData.Weapons.Length;i++) if(GameData.Weapons[i].id==GameState.selectedWeapon){ weaponIndex=i; weapon=GameData.Weapons[i]; }
        if (weaponIndex<0){ weaponIndex=0; weapon=GameData.Weapons[0]; }
        balance = GetBalanceProfile();
        if (map.tier==2) sunVector=new Vector2(0.82f,0.28f); else if(map.tier==3) sunVector=new Vector2(0.58f,0.48f);
        else if(map.tier==4) sunVector=new Vector2(-0.55f,0.34f); else sunVector=new Vector2(0.72f,0.38f);
        GenerateTerrain();
        SpawnEntities();
        SetupMission();
        UpdateVision();
    }

    Balance GetBalanceProfile(){ int t=map.tier; var b=new Balance(); b.enemyHp=1+(t-1)*0.32f; b.enemyDamage=1+(t-1)*0.22f; b.enemySpeed=1+(t-1)*0.055f; b.reward=1+(t-1)*0.48f; b.eliteChance=t<3?0:0.08f+t*0.025f; b.bossHp=520+t*260; b.bossDamage=14+t*5; return b; }

    void SetupMission(){
        int targetKills=3+map.tier*2;
        Mission[] ms = new Mission[]{
            new Mission{ type="hunt", title="清剿威胁", target=targetKills, progress=0, description="击败 "+targetKills+" 只野怪" },
            new Mission{ type="scavenge", title="物资回收", target=Math.Min(4,1+map.tier), progress=0, description="开启 "+Math.Min(4,1+map.tier)+" 个宝箱" },
            new Mission{ type="tower", title="据点争夺", target=Math.Min(3,1+(int)Math.Floor(map.tier/2f)), progress=0, description="占领地图防御塔" },
        };
        objective = ms[(map.tier + G.RandInt(0,ms.Length-1)) % ms.Length];
        mapEvents = new List<MapEvent>{
            new MapEvent{ id="spirit_rain", name="灵雨赐福", duration=18, color="#72e6bf", text="持续恢复生命与能量" },
            new MapEvent{ id="blood_moon", name="血月侵袭", duration=22, color="#ff6b5b", text="怪物强化，掉落翻倍" },
            new MapEvent{ id="mist", name="峡谷迷雾", duration=20, color="#a8c6d7", text="视野收缩，怪物移速降低" },
            new MapEvent{ id="meteor", name="晶石坠落", duration=16, color="#d6a6ff", text="地图出现危险落点与额外材料" },
        };
        nextEventAt = 45f; elapsed = 0; bossSpawned=false;
    }

    // ---------------- 输入 ----------------
    public void SetKey(string key, bool down){ keys[key]=down; }
    public void OnSkill(int i){ UseSkill(i); }
    public void OnConsumable(string id){ UseConsumable(id); }
    public void CycleWeapon(int dir){ weaponIndex=(weaponIndex+dir+GameData.Weapons.Length)%GameData.Weapons.Length; weapon=GameData.Weapons[weaponIndex]; GameState.selectedWeapon=weapon.id; weaponPulse=0.35f; UIHost.ShowToast("切换武器："+weapon.name,"success"); }
    public void SetMouse(Vector2 pos){ mouse=pos; }
    public void SetMouseDown(bool down){ if(down && !mouseDown){ TryInteract(); } mouseDown=down; }

    void Toast(string msg, string type=""){ if(OnToastCallback!=null) OnToastCallback(msg); else UIHost.ShowToast(msg,type); }

    // ---------------- 技能 ----------------
    public void UseSkill(int idx){
        if (idx<0||idx>=4||skillCooldowns[idx]>0) return;
        var skill = SkillMath.GetStats(GameData.Skills[idx], skillBoosts.ContainsKey(GameData.Skills[idx].id)?skillBoosts[GameData.Skills[idx].id]:0);
        if (player.energy < skill.energyCost){ Toast("能量不足","warning"); return; }
        player.energy -= skill.energyCost;
        skillCooldowns[idx]=skill.cooldown; skillFlashes[idx]=0.28f;
        float px=player.x, py=player.y;
        float wx=mouse.x+camera.x, wy=mouse.y+camera.y;
        float angle=(float)Math.Atan2(wy-py, wx-px);
        if (skill.def.id=="straw_smash"){
            foreach(var m in monsters) if(m.hp>0 && G.Dist(m.x,m.y,px,py)<skill.range){ DamageEnemy(m,skill.damage,"#f2c45b",true); m.stunned=Math.Max(m.stunned,0.5f); }
            foreach(var r in raiders) if(r.hp>0 && G.Dist(r.x,r.y,px,py)<skill.range){ DamageRaider(r,skill.damage,"#f2c45b"); r.stunned=Math.Max(r.stunned,0.5f); }
            SpawnAoeEffect(px,py,skill.range,"#f2c45b","ring"); SpawnRadialBurst(px,py,"#fff0a6",12);
        } else if (skill.def.id=="vine_bind"){
            foreach(var m in monsters) if(m.hp>0 && G.Dist(m.x,m.y,px,py)<skill.range){ m.stunned=Math.Max(m.stunned,skill.stunDuration); SpawnHitParticles(m.x,m.y,"#55aa55"); }
            foreach(var r in raiders) if(r.hp>0 && G.Dist(r.x,r.y,px,py)<skill.range){ r.stunned=Math.Max(r.stunned,skill.stunDuration); }
            SpawnVineEffect(px,py,skill.range);
        } else if (skill.def.id=="earth_dash"){
            player.x+=(float)Math.Cos(angle)*skill.dashDistance; player.y+=(float)Math.Sin(angle)*skill.dashDistance;
            player.invuln=skill.def.invulnDuration; player.visualVz=125; SpawnDashParticles(px,py,angle); SpawnDashTrail(px,py,angle,"#a6e7ff");
        } else if (skill.def.id=="smoke_screen"){
            player.stealth=skill.stealthDuration; SpawnSmokeEffect(px,py); Toast("进入隐身状态","success");
        }
    }

    public void UseConsumable(string id){
        int have=consumables.ContainsKey(id)?consumables[id]:0;
        if (have<=0){ Toast("没有该消耗品","warning"); return; }
        ConsumableDef item=null; foreach(var c in GameData.Consumables) if(c.id==id) item=c;
        consumableFlashes[id]=1;
        if (id=="herb_kit"){ player.hp=Math.Min(player.maxHp,player.hp+item.heal); consumables[id]=have-1; Toast("使用"+item.name+"，回复"+item.heal+"生命","success"); SpawnAoeEffect(player.x,player.y,60,"#ff66aa","ring"); }
        else if (id=="thorn_storm"){ foreach(var m in monsters) if(m.hp>0 && G.Dist(m.x,m.y,player.x,player.y)<item.range) DamageEnemy(m,item.damage,"#ff9a55",true);
            foreach(var r in raiders) if(r.hp>0 && G.Dist(r.x,r.y,player.x,player.y)<item.range) DamageRaider(r,item.damage,"#ff9a55");
            consumables[id]=have-1; SpawnAoeEffect(player.x,player.y,item.range,"#aa5500","ring"); Toast("释放"+item.name+"！","success"); }
        else if (id=="signal_flare"){ consumables[id]=have-1; StartExtract("signal"); UIHost.SignalFlash(); Toast("释放撤离信号弹！全地图敌人正在逼近！","warning"); }
    }

    // ---------------- 主更新（移植自 update，含固定步长与怪物AI、弹道、粒子、陷阱、塔、事件） ----------------
    public void Update(float dt){
        if (paused || gameOver) return;
        // 视口尺寸见 RenderBackend
        UpdateWorldSystems(dt);
        fogUpdateTimer -= dt;
        if (fogUpdateTimer<=0){ fogUpdateTimer+=fogUpdateInterval; fogDirty=true; }
        PickupLoot();
        timeLeft -= dt;
        if (timeLeft<=0){ timeLeft=0; PlayerDeath(); return; }

        float dx=0,dy=0;
        // Unity 相机位于玩家的 -Z 方向；世界 +Y 映射到屏幕向上。
        if (KeyDown("w")) dy+=1; if (KeyDown("s")) dy-=1; if (KeyDown("a")) dx-=1; if (KeyDown("d")) dx+=1;
        float len=(float)Math.Sqrt(dx*dx+dy*dy); if(len>0){ dx/=len; dy/=len; }
        float speed=220;
        if (KeyDown("shift") && player.energy>1){ speed=380; player.energy-=30*dt; }
        float tf=1f;
        foreach(var p in terrainPatches){ float nx=(player.x-p.x)/p.rx, ny=(player.y-p.y)/p.ry; if(nx*nx+ny*ny<=1){ if(p.type=="water") tf=Math.Min(tf,0.58f); else if(p.type=="soil") tf=Math.Min(tf,0.82f); } }
        if (player.slow>0) tf*=0.56f; speed*=tf;
        float px=player.x, py=player.y;
        player.x+=dx*speed*dt; player.y+=dy*speed*dt;
        player.x=G.Clamp(player.x,20,2400-20); player.y=G.Clamp(player.y,20,2400-20);
        ResolvePlayerObstacle(px, py);
        UpdateVision();

        player.energy=Math.Min(player.maxEnergy,player.energy+15*dt);
        player.attackCd=Math.Max(0,player.attackCd-dt); player.invuln=Math.Max(0,player.invuln-dt);
        player.stealth=Math.Max(0,player.stealth-dt); player.slow=Math.Max(0,player.slow-dt);
        weaponPulse=Math.Max(0,weaponPulse-dt); attackAnim=Math.Max(0,attackAnim-dt);
        screenShake=Math.Max(0,screenShake-dt*4.5f);
        player.visualZ=Math.Max(0,player.visualZ+player.visualVz*dt); player.visualVz-=360*dt;
        if(player.visualZ<=0){ player.visualZ=0; player.visualVz=0; }
        foreach(var m in monsters){ m.deathTimer=Math.Max(0,m.deathTimer-dt); m.visualZ=Math.Max(0,m.visualZ+m.visualVz*dt); m.visualVz-=330*dt; if(m.visualZ<=0){m.visualZ=0;m.visualVz=0;} }
        for(int i=0;i<4;i++){ skillCooldowns[i]=Math.Max(0,skillCooldowns[i]-dt); skillFlashes[i]=Math.Max(0,skillFlashes[i]-dt); }
        foreach(var id in new List<string>(consumableFlashes.Keys)){ consumableFlashes[id]=(int)Math.Max(0,consumableFlashes[id]-dt); }

        // 陷阱
        foreach(var trap in traps){
            trap.triggerCd=Math.Max(0,trap.triggerCd-dt); trap.phase+=dt;
            if(trap.triggerCd<=0 && G.Dist(player.x,player.y,trap.x,trap.y)<trap.radius){
                trap.triggerCd=trap.cooldown;
                if(player.invuln<=0) player.slow=Math.Max(player.slow,trap.slow);
                DamagePlayer(trap.damage+Math.Max(0,map.tier-1)*2); SpawnAoeEffect(trap.x,trap.y,trap.radius,trap.color,"ring"); Toast("触发陷阱："+trap.name+"！","warning");
            }
        }
        if (gameOver) return;

        if (mouseDown) PlayerAttack();

        // 摄像机跟随
        float lookX=G.Clamp(mouse.x-640,-260,260)*0.16f; float lookY=G.Clamp(mouse.y-360,-180,180)*0.11f;
        float shX=screenShake>0?G.Rand(-1,1)*screenShake*8:0; float shY=screenShake>0?G.Rand(-1,1)*screenShake*5:0;
        float ctx=G.Clamp(player.x-640+lookX+shX,0,2400-1280); float cty=G.Clamp(player.y-360+lookY+shY,0,2400-720);
        camera.x=G.Lerp(camera.x,ctx,0.085f); camera.y=G.Lerp(camera.y,cty,0.085f);

        // 怪物AI
        foreach(var m in monsters){ UpdateMonsterAI(m, dt); }
        ResolveUnitCollisions();

        // 移除死亡怪物
        for(int i=monsters.Count-1;i>=0;i--){ var m=monsters[i]; if(m.hp<=0){ if(!m.deathProcessed){ m.deathProcessed=true; m.deathTimer=0.42f; m.state="death"; killCount++; SpawnKillFeedback(m); SpawnHitParticles(m.x,m.y,"#ff4444");
            if(m.type=="boss"){ SpawnGroundLoot("material","首领核心",2+map.tier,"◆",m.x+18,m.y); SpawnGroundLoot("gold","首领赏金",150*map.tier,"💰",m.x-18,m.y); Toast("首领「"+m.name+"」已击败，撤离奖励提升","gold"); boss=null; }
            if(G.RandInt(0,100)<30) SpawnGroundLoot("gold","金币",m.gold>0?m.gold:5,"💰",m.x,m.y);
            if(m.type!="boss" && G.RandInt(0,1000)<55) SpawnGroundLoot("invincible","无敌核心",1,"🛡️",m.x,m.y,5);
        } if(m.deathTimer<=0) monsters.RemoveAt(i); } }

        UpdateRaiders(dt);

        // 防御塔
        foreach(var t in towers){ UpdateTower(t, dt); }

        // 弹道
        UpdateProjectiles(dt);

        // 粒子、伤害数字
        for(int i=particles.Count-1;i>=0;i--){ var p=particles[i]; p.life-=dt; if(!(p.type=="aoe"||p.type=="slash"||p.type=="weaponRing"||p.type=="vine"||p.type=="earthTrail")){ p.x+=p.vx*dt; p.y+=p.vy*dt; } if(p.life<=0) particles.RemoveAt(i); }
        for(int i=damageNumbers.Count-1;i>=0;i--){ var d=damageNumbers[i]; d.life-=dt; d.x+=d.vx*dt; d.y+=d.vy*dt; d.vy+=72*dt; if(d.life<=0) damageNumbers.RemoveAt(i); }
        killFlash=Math.Max(0,killFlash-dt);

        // 撤离读条
        if (extracting){ float et=extractType=="signal"?20:15; extractProgress+=dt; if(extractProgress>=et) CompleteExtract(); }
    }

    bool KeyDown(string k){ bool v; return keys.TryGetValue(k,out v) && v; }

    void ResolvePlayerObstacle(float px, float py){
        if (CollidesWithObstacle(player.x,player.y,player.collisionRadius)){
            float mx=player.x, my=player.y;
            player.x=mx; player.y=py;
            if(CollidesWithObstacle(player.x,player.y,player.collisionRadius)) player.x=px;
            player.y=my;
            if(CollidesWithObstacle(player.x,player.y,player.collisionRadius)) player.y=py;
            if(CollidesWithObstacle(player.x,player.y,player.collisionRadius)){ player.x=px; player.y=py; }
        }
    }

    bool CollidesWithObstacle(float x,float y,float radius){
        foreach(var o in obstacles){ float dx=x-o.x, dy=y-(o.y+o.collisionOffsetY);
            float min;
            if(o.collisionRx>0&&o.collisionRy>0){ float c=Mathf.Cos(-o.rotation), s=Mathf.Sin(-o.rotation); float lx=dx*c-dy*s, ly=dx*s+dy*c; float rx=o.collisionRx+radius, ry=o.collisionRy+radius; if(lx*lx/(rx*rx)+ly*ly/(ry*ry)<1) return true; }
            else { min=radius+o.radius; if(dx*dx+dy*dy<min*min) return true; }
        }
        return false;
    }

    void MoveEntityWithCollisions(Monster e, float dx, float dy){
        if(dx==0&&dy==0) return; float ox=e.x, oy=e.y;
        e.x+=dx; if(CollidesWithObstacle(e.x,e.y,e.collisionRadius>0?e.collisionRadius:e.radius*0.72f)) e.x=ox;
        e.y+=dy; if(CollidesWithObstacle(e.x,e.y,e.collisionRadius>0?e.collisionRadius:e.radius*0.72f)) e.y=oy;
        float r=e.collisionRadius>0?e.collisionRadius:e.radius; e.x=G.Clamp(e.x,r,2400-r); e.y=G.Clamp(e.y,r,2400-r);
    }

    void ResolveUnitCollisions(){
        for(int i=0;i<monsters.Count;i++){ var a=monsters[i]; if(a.hp<=0)continue; float ar=a.collisionRadius>0?a.collisionRadius:a.radius*0.72f;
            float pdx=a.x-player.x, pdy=a.y-player.y; float pm=ar+player.collisionRadius; float pd=Math.Max((float)Math.Sqrt(pdx*pdx+pdy*pdy),0.001f);
            if(pd<pm){ float push=(pm-pd)*0.7f; MoveEntityWithCollisions(a,pdx/pd*push,pdy/pd*push); }
            for(int j=i+1;j<monsters.Count;j++){ var b=monsters[j]; if(b.hp<=0)continue; float br=b.collisionRadius>0?b.collisionRadius:b.radius*0.72f;
                float dx=b.x-a.x, dy=b.y-a.y, minD=ar+br; float d=Math.Max((float)Math.Sqrt(dx*dx+dy*dy),0.001f);
                if(d<minD){ float push=(minD-d)*0.32f, nx=dx/d, ny=dy/d; MoveEntityWithCollisions(a,-nx*push,-ny*push); MoveEntityWithCollisions(b,nx*push,ny*push); }
            }
        }
    }

    void UpdateVision(){
        float cell=visionCellSize; float radius=visionRadius*(eventModifiers.vision);
        long minX=Math.Max(0,(long)Math.Floor((player.x-radius)/cell)), maxX=Math.Min((long)Math.Ceiling(2400f/cell),(long)Math.Ceiling((player.x+radius)/cell));
        long minY=Math.Max(0,(long)Math.Floor((player.y-radius)/cell)), maxY=Math.Min((long)Math.Ceiling(2400f/cell),(long)Math.Ceiling((player.y+radius)/cell));
        bool changed=false;
        for(long cy=minY;cy<=maxY;cy++) for(long cx=minX;cx<=maxX;cx++){
            float cxw=cx*cell+cell/2, cyw=cy*cell+cell/2; long key=cx*100000+cy;
            if(G.Dist(cxw,cyw,player.x,player.y)<=radius+cell*0.68f && !exploredCells.Contains(key)){ exploredCells.Add(key); changed=true; }
        }
        if(changed) fogDirty=true;
    }

    public bool IsWorldVisible(float x,float y){ return G.Dist(x,y,player.x,player.y)<=visionRadius*eventModifiers.vision; }
}
