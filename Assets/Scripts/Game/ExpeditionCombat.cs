using System;
using System.Collections.Generic;
using UnityEngine;

// ============ 远征战斗：怪物AI/塔/弹道/任务/波次/结算 ============
public partial class Expedition
{
    void UpdateMonsterAI(Monster m, float dt){
        if(m.hp<=0) return;
        m.attackCd=Math.Max(0,m.attackCd-dt); m.stunned=Math.Max(0,m.stunned-dt); m.hitFlash=Math.Max(0,m.hitFlash-dt);
        m.stateTimer=Math.Max(0,m.stateTimer-dt); m.animTime+=dt*(1.8f+m.speed/120f);
        if(m.stunned>0) return;
        // 特有行为
        m.abilityCd=Math.Max(0,m.abilityCd-dt);
        float d=G.Dist(m.x,m.y,player.x,player.y);
        if(player.stealth>0 || d>430 || m.stunned>0){ /* wander path below */ }
        bool canSee=beastWave.active || (player.stealth<=0 && d<400);
        if(canSee){
            float angle=(float)Math.Atan2(player.y-m.y,player.x-m.x); m.facing=angle;
            if(d>m.attackRange){ m.state="move"; MoveEntityWithCollisions(m,(float)Math.Cos(angle)*m.speed*dt,(float)Math.Sin(angle)*m.speed*dt); }
            else if(m.attackCd<=0){ m.state="attack"; m.stateTimer=0.28f; m.attackCd=m.attackCooldown;
                if(m.ranged){ projectiles.Add(new Projectile{ x=m.x,y=m.y,vx=(float)Math.Cos(angle)*300,vy=(float)Math.Sin(angle)*300,damage=m.damage,life=2,fromMonster=true,radius=6,color=m.type=="spider"?"#9bea55":"#ff6644",monsterType=m.type }); }
                else DamagePlayer(m.damage);
            } else m.state="idle";
        } else {
            if(!m.wanderX.HasValue || G.Dist(m.x,m.y,m.wanderX.Value,m.wanderY.Value)<30){ m.wanderX=m.x+G.Rand(-200,200); m.wanderY=m.y+G.Rand(-200,200); }
            float angle=(float)Math.Atan2(m.wanderY.Value-m.y,m.wanderX.Value-m.x); m.facing=angle;
            MoveEntityWithCollisions(m,(float)Math.Cos(angle)*m.speed*0.3f*dt,(float)Math.Sin(angle)*m.speed*0.3f*dt);
        }
        // 首领与特殊能力（沿 web updateWorldSystems）
        if(m.type=="boss" && m.abilityCd<=0){ m.phase=m.hp/m.maxHp<0.5f?2:1; m.abilityCd=m.phase==2?2.7f:4.2f; SpawnAoeEffect(player.x,player.y,88,"#d59aff","ring"); if(d<155) DamagePlayer(m.damage*0.72f); }
        if(m.type=="boar" && m.abilityCd<=0 && d>120 && d<275){ m.abilityCd=5.5f; m.x+=(float)Math.Cos(Math.Atan2(player.y-m.y,player.x-m.x))*64; m.y+=(float)Math.Sin(Math.Atan2(player.y-m.y,player.x-m.x))*64; SpawnAoeEffect(m.x,m.y,42,"#e9a15e","ring"); }
    }

    void UpdateRaiders(float dt){
        for(int i=raiders.Count-1;i>=0;i--){ var r=raiders[i]; if(r.hp<=0){ killCount++; Toast("击败掠夺者！战利品已掉落","gold"); for(int k=0;k<r.loot;k++) SpawnGroundLoot("gold","金币",G.RandInt(20,50),"💰",r.x,r.y); raiders.RemoveAt(i); continue; }
            r.attackCd=Math.Max(0,r.attackCd-dt); r.stunned=Math.Max(0,r.stunned-dt); if(r.stunned>0) continue;
            float d=G.Dist(r.x,r.y,player.x,player.y);
            if(d<300 && player.stealth<=0){ float angle=(float)Math.Atan2(player.y-r.y,player.x-r.x);
                if(d>150){ r.x+=(float)Math.Cos(angle)*r.speed*dt; r.y+=(float)Math.Sin(angle)*r.speed*dt; }
                else if(r.attackCd<=0){ r.attackCd=1.5f; projectiles.Add(new Projectile{ x=r.x,y=r.y,vx=(float)Math.Cos(angle)*250,vy=(float)Math.Sin(angle)*250,damage=r.damage,life=2,fromMonster=true,radius=6 }); }
            } else { if(G.Dist(r.x,r.y,r.patrolX,r.patrolY)<30){ r.patrolX=G.Rand(200,2200); r.patrolY=G.Rand(200,2200); }
                float angle=(float)Math.Atan2(r.patrolY-r.y,r.patrolX-r.x); r.x+=(float)Math.Cos(angle)*r.speed*0.5f*dt; r.y+=(float)Math.Sin(angle)*r.speed*0.5f*dt; }
        }
    }

    void UpdateTower(Tower t, float dt){
        if(t.state=="broken") return;
        t.attackCd=Math.Max(0,t.attackCd-dt); if(t.attackCd>0) return;
        if(t.state=="player"){
            Monster nearest=null; float minD=t.range;
            foreach(var m in monsters){ if(m.hp<=0) continue; float d=G.Dist(t.x,t.y,m.x,m.y); if(d<minD){ minD=d; nearest=m; } }
            if(nearest!=null){ DamageEnemy(nearest,t.damage*(beastWave.active?2.15f:1),"#8affb5",false); t.attackCd=beastWave.active?0.42f:0.72f;
                projectiles.Add(new Projectile{ x=t.x,y=t.y,vx=(nearest.x-t.x)/minD*400,vy=(nearest.y-t.y)/minD*400,damage=0,life=0.3f,fromTower=true,radius=4,target=nearest }); }
        } else if(t.state=="enemy"){ if(G.Dist(t.x,t.y,player.x,player.y)<t.range){ DamagePlayer(t.damage); t.attackCd=1.0f; } }
    }

    void UpdateProjectiles(float dt){
        for(int i=projectiles.Count-1;i>=0;i--){ var p=projectiles[i]; p.x+=p.vx*dt; p.y+=p.vy*dt; p.life-=dt;
            if(p.life<=0){ projectiles.RemoveAt(i); continue; }
            if(p.fromTower){ if(p.target!=null){ var mt=p.target as Monster; if(mt==null||mt.hp<=0){ projectiles.RemoveAt(i); continue; } } continue; }
            if(p.fromPlayer){ bool remove=false;
                foreach(var m in monsters){ if(m.hp<=0||p.hit.Contains(m)) continue; if(G.Dist(p.x,p.y,m.x,m.y)<m.radius+p.radius){ DamageEnemy(m,p.damage,p.color,p.weaponId=="vine_staff"); m.visualVz=Math.Max(m.visualVz,p.weaponId=="vine_staff"?82:52); p.hit.Add(m); if(p.weaponId=="vine_staff") m.stunned=Math.Max(m.stunned,0.18f); int pierce=int.TryParse(p.pierce,out var pi)?pi:1; p.pierce=(pierce-1).ToString(); if(pierce<=1){ remove=true; break; } } }
                foreach(var r in raiders){ if(r.hp<=0||p.hit.Contains(r)) continue; if(G.Dist(p.x,p.y,r.x,r.y)<r.radius+p.radius){ DamageRaider(r,p.damage,p.color); p.hit.Add(r); int pierce=int.TryParse(p.pierce,out var pi)?pi:1; p.pierce=(pierce-1).ToString(); if(pierce<=1){ remove=true; break; } } }
                if(remove){ projectiles.RemoveAt(i); continue; }
            }
            if(p.fromMonster && G.Dist(p.x,p.y,player.x,player.y)<player.collisionRadius+p.radius){ DamagePlayer(p.damage); projectiles.RemoveAt(i); continue; }
        }
    }

    void UpdateWorldSystems(float dt){
        elapsed+=dt;
        int waveRemaining=0; foreach(var m in monsters) if(m.beastWave&&m.hp>0) waveRemaining++;
        beastWave.remaining=waveRemaining;
        if(beastWave.active){ if(waveRemaining==0){ beastWave.active=false; beastWave.nextIn=Math.Max(58,92-map.tier*4); GameState.gold+=(int)(20*beastWave.wave*map.tier); Toast("第 "+beastWave.wave+" 波兽潮已击退，获得守塔奖励","success"); } }
        else { beastWave.nextIn-=dt; if(beastWave.nextIn<=0) SpawnBeastWave(); }
        if(elapsed>=nextEventAt && activeEvent==null){ StartMapEvent(); nextEventAt+=90+G.Rand(0,35); }
        if(activeEvent!=null){ activeEvent.timeLeft-=dt;
            if(activeEvent.id=="spirit_rain"){ player.hp=Math.Min(player.maxHp,player.hp+dt*1.25f); player.energy=Math.Min(player.maxEnergy,player.energy+dt*2); }
            if(activeEvent.timeLeft<=0){ activeEvent=null; eventModifiers=new EventModifiers(); } }
        UpdateMission();
    }

    public void PlayerAttack(){ if(player.attackCd>0) return; player.attackCd=weapon.cooldown;
        float wx=mouse.x+camera.x, wy=mouse.y+camera.y; float angle=(float)Math.Atan2(wy-player.y,wx-player.x); player.angle=angle;
        weaponPulse=0.18f; attackAnim=0.24f;
        if(weapon.mode=="melee"){ foreach(var m in monsters){ if(m.hp<=0) continue; float d=G.Dist(m.x,m.y,player.x,player.y); if(d<weapon.range){ float ma=(float)Math.Atan2(m.y-player.y,m.x-player.x); float ad=(float)Math.Abs(((ma-angle+Math.PI*3)%(Math.PI*2))-Math.PI); if(ad<Math.PI/2){ DamageEnemy(m,weapon.damage,weapon.color,true); m.stunned=Math.Max(m.stunned,0.2f); m.visualVz=Math.Max(m.visualVz,105); } } }
            foreach(var r in raiders){ if(r.hp<=0) continue; float d=G.Dist(r.x,r.y,player.x,player.y); if(d<weapon.range){ float ma=(float)Math.Atan2(r.y-player.y,r.x-player.x); float ad=(float)Math.Abs(((ma-angle+Math.PI*3)%(Math.PI*2))-Math.PI); if(ad<Math.PI/2){ DamageRaider(r,weapon.damage,weapon.color); } } }
            SpawnSlashEffect(player.x,player.y,angle,weapon.color,64);
        } else { projectiles.Add(new Projectile{ x=player.x+(float)Math.Cos(angle)*24,y=player.y+(float)Math.Sin(angle)*24,vx=(float)Math.Cos(angle)*weapon.projectileSpeed,vy=(float)Math.Sin(angle)*weapon.projectileSpeed,damage=weapon.damage,life=weapon.range/weapon.projectileSpeed,radius=7,fromPlayer=true,weaponId=weapon.id,pierce=(weapon.pierce>0?weapon.pierce:1).ToString(),color=weapon.color }); SpawnMuzzleEffect(player.x,player.y,angle,weapon.color); } }

    public void TryInteract(){ float wx=mouse.x+camera.x, wy=mouse.y+camera.y;
        foreach(var c in chests){ if(!c.opened && G.Dist(player.x,player.y,c.x,c.y)<50 && G.Dist(wx,wy,c.x,c.y)<40){ OpenChest(c); return; } }
        foreach(var t in towers){ if(t.state!="player" && G.Dist(player.x,player.y,t.x,t.y)<50 && G.Dist(wx,wy,t.x,t.y)<40){ t.state="player"; t.hp=t.maxHp; Toast("防御塔已占领：进入射程可获得护盾减伤！","success"); SpawnAoeEffect(t.x,t.y,50,"#7fff7f","ring"); return; } }
        foreach(var ep in extractPoints){ if(G.Dist(player.x,player.y,ep.x,ep.y)<ep.radius){ StartExtract("fixed"); return; } }
        PlayerAttack();
    }

    void OpenChest(Chest chest){ chest.opened=true; chestOpened++; List<GroundLoot> loot=new List<GroundLoot>();
        int gold=G.RandInt(20,80)*map.tier; loot.Add(new GroundLoot{ type="gold", name="金币", amount=gold, icon="💰" });
        if(RandPct(60)){ var crop=GameData.Crops[G.RandInt(0,3)]; loot.Add(new GroundLoot{ type="seed", name=crop.name+"种子", amount=G.RandInt(1,2), icon=crop.icon, cropId=crop.id }); }
        if(RandPct(map.rareSeedChance*100)) loot.Add(new GroundLoot{ type="seed", name="稀有种子", amount=1, icon="✨", rare=true });
        if(RandPct(map.legendarySeedChance*100)) loot.Add(new GroundLoot{ type="seed", name="月光稻种子", amount=1, icon="🌟", legendary=true, cropId="moon_rice" });
        if(RandPct(40)) loot.Add(new GroundLoot{ type="material", name="建材", amount=G.RandInt(1,3), icon="📦" });
        if(RandPct(30)) loot.Add(new GroundLoot{ type="consumable", name="草药包扎包", amount=1, icon="💊", cropId="herb_kit" });
        if(chest.hasSignal) loot.Add(new GroundLoot{ type="consumable", name="撤离信号弹", amount=1, icon="🔥", cropId="signal_flare" });
        if(RandPct(24)) loot.Add(new GroundLoot{ type="farm_item", name="生长催化剂", amount=1, icon="⏳", cropId="growth_catalyst" });
        foreach(var li in loot) SpawnGroundLoot(li);
        Toast("宝箱打开，掉落"+loot.Count+"件物品，进入攻击范围后自动拾取","gold"); SpawnAoeEffect(chest.x,chest.y,50,"#ffd700","ring");
    }
    bool RandPct(float p){ return G.RandInt(0,1000) < p*10; }

    public void SpawnGroundLoot(GroundLoot item){ groundLoot.Add(new GroundLoot{ type=item.type,name=item.name,amount=item.amount,icon=item.icon,cropId=item.cropId,rare=item.rare,legendary=item.legendary,duration=item.duration, x=item.x+G.Rand(-28,28), y=item.y+G.Rand(-28,28), bob=G.Rand(0f,(float)(Math.PI*2)) }); }
    public void SpawnGroundLoot(string type,string name,float amount,string icon,float x,float y){ SpawnGroundLoot(new GroundLoot{ type=type,name=name,amount=amount,icon=icon,x=x,y=y }); }
    public void SpawnGroundLoot(string type,string name,float amount,string icon,float x,float y,float dur){ SpawnGroundLoot(new GroundLoot{ type=type,name=name,amount=amount,icon=icon,x=x,y=y,duration=dur }); }

    void PickupLoot(){ if(groundLoot.Count==0) return; float range=82;
        int best=-1; float bestD=float.MaxValue;
        for(int i=0;i<groundLoot.Count;i++){ var item=groundLoot[i]; float d=G.Dist(player.x,player.y,item.x,item.y); if(d<=range && IsWorldVisible(item.x,item.y) && d<bestD){ bestD=d; best=i; } }
        if(best<0) return; var it=groundLoot[best]; groundLoot.RemoveAt(best);
        if(it.type=="invincible"){ player.invuln=Math.Max(player.invuln,it.duration>0?it.duration:5); player.slow=0; SpawnAoeEffect(it.x,it.y,70,"#7de7ff","ring"); SpawnRadialBurst(it.x,it.y,"#e6fbff",22); Toast("无敌核心生效：5 秒内免疫一切伤害和控制！","success"); }
        else { bag.Add(it); SpawnAoeEffect(it.x,it.y,34,"#f6c75b","ring"); Toast("拾取 "+it.icon+" "+it.name+" ×"+it.amount,"gold"); }
    }

    public void StartExtract(string type){ if(extracting) return; extracting=true; extractType=type; extractProgress=0; Toast(type=="signal"?"信号弹撤离启动！坚持20秒！":"开始撤离读条，坚持15秒！","warning"); }
    void CompleteExtract(){ gameOver=true; result="success"; EndExpedition(); }
    public void PlayerDeath(){ gameOver=true; result="failed"; EndExpedition(); }

    void EndExpedition(){
        float totalGold=0; foreach(var i in bag) if(i.type=="gold") totalGold+=i.amount;
        List<GroundLoot> kept=new List<GroundLoot>(), lost=new List<GroundLoot>();
        if(result=="success"){
            kept.AddRange(bag); GameState.gold+=(int)totalGold;
            foreach(var i in bag) if(i.type=="seed"){ GameState.seeds+=(int)i.amount; if(!string.IsNullOrEmpty(i.cropId) && !GameState.unlockedCrops.Contains(i.cropId)){ GameState.unlockedCrops.Add(i.cropId); Toast("解锁新作物："+(CropName(i.cropId)),"gold"); } }
            foreach(var i in bag) if(i.type=="material") GameState.materials+=(int)i.amount;
            foreach(var i in bag) if(i.type=="consumable") GameState.loadout[i.cropId]=(GameState.loadout.ContainsKey(i.cropId)?GameState.loadout[i.cropId]:0)+(int)i.amount;
            foreach(var i in bag) if(i.type=="farm_item") GameState.farmItems[i.cropId]=(GameState.farmItems.ContainsKey(i.cropId)?GameState.farmItems[i.cropId]:0)+(int)i.amount;
        } else {
            foreach(var i in bag){ if(RandPct(20)){ kept.Add(i); if(i.type=="gold") GameState.gold+=(int)Math.Floor(i.amount*0.2f); if(i.type=="seed") GameState.seeds+=(int)Math.Floor(i.amount*0.5f); } else lost.Add(i); }
        }
        SaveSystem.Save();
        UIHost.ShowResult(result=="success", map.name, (720-timeLeft).ToString("F1"), killCount, chestOpened, (int)damageTaken, result=="success"?(int)totalGold:(int)Math.Floor(totalGold*0.2f), kept, lost);

    }
    string CropName(string id){ var c=SaveSystem.CropById(id); return c!=null?c.name:id; }

    public void DamageEnemy(Monster m,float amount,string color,bool heavy){
        if(m==null||m.hp<=0) return; m.hp-=amount; m.hitFlash=heavy?0.22f:0.14f; m.state=m.hp<=0?"death":"hit"; m.stateTimer=m.hp<=0?0.4f:0.18f;
        damageNumbers.Add(new DamageNumber{ x=m.x+G.Rand(-8,8), y=m.y-m.radius-8, value=(int)Math.Round(amount), color=color, life=0.72f, maxLife=0.72f, vx=G.Rand(-10,10), vy=heavy?-64:-48, heavy=heavy });
        SpawnHitParticles(m.x,m.y,color); hitStop=Math.Max(hitStop,heavy?0.065f:0.032f);
    }
    public void DamageRaider(Raider r,float amount,string color){ if(r==null||r.hp<=0) return; r.hp-=amount; damageNumbers.Add(new DamageNumber{ x=r.x+G.Rand(-8,8), y=r.y-r.radius-8, value=(int)Math.Round(amount), color=color, life=0.72f, maxLife=0.72f, vx=G.Rand(-10,10), vy=-48 }); SpawnHitParticles(r.x,r.y,color); }

    public void DamagePlayer(float amount){ if(player.invuln>0) return;
        bool tower=false; foreach(var t in towers) if(t.state=="player" && G.Dist(t.x,t.y,player.x,player.y)<=t.range){ tower=true; break; }
        if(beastWave.active) amount*=tower?0.38f:1.45f; else if(tower) amount*=0.76f;
        player.hp-=amount; damageTaken+=amount; screenShake=Math.Min(1,screenShake+0.48f); SpawnHitParticles(player.x,player.y,"#ff4444");
        if(extracting){ extracting=false; extractProgress=0; Toast("撤离被打断！","warning"); }
        if(player.hp<=0){ player.hp=0; PlayerDeath(); }
    }

    void SpawnKillFeedback(Monster m){ bool boss=m.type=="boss"; killFlash=Math.Max(killFlash,boss?0.22f:0.11f); hitStop=Math.Max(hitStop,boss?0.13f:0.07f); screenShake=Math.Max(screenShake,boss?1:0.65f); SpawnRadialBurst(m.x,m.y,boss?"#ffe8a0":"#ff7868",boss?30:18); }

    void SpawnBoss(){ if(bossSpawned) return; bossSpawned=true; var pos=FindSafeSpawn(650,2050,46);
        boss=new Monster{ type="boss", name=new[]{"苔岩裂颚兽","幽潮骨翼龙","霜脉巨灵","紫月灾兽"}[map.tier-1], x=pos.Item1,y=pos.Item2,radius=46,hp=balance.bossHp,maxHp=balance.bossHp,damage=balance.bossDamage,speed=76+map.tier*5,attackRange=70,attackCd=1.5f,abilityCd=4,phase=1,elite=true,gold=100*map.tier };
        monsters.Add(boss); Toast("区域首领「"+boss.name+"」已现身","warning");
    }

    void SpawnBeastWave(){ beastWave.wave++; beastWave.active=true; beastWave.duration=32+map.tier*3;
        int count=Math.Min(42,10+map.tier*4+beastWave.wave*4);
        string[] types={"boar","bat","spider","locust","wolf"};
        for(int i=0;i<count;i++){ string type=types[G.RandInt(0,types.Length-1)]; var data=GameData.Monsters[type];
            float angle=(float)(Math.PI*2*i/count)+G.Rand(-0.22f,0.22f); float dist=G.Rand(430,620);
            float x=G.Clamp(player.x+(float)Math.Cos(angle)*dist,80,2320), y=G.Clamp(player.y+(float)Math.Sin(angle)*dist,80,2320);
            float hpScale=balance.enemyHp*(0.72f+Math.Min(0.18f,beastWave.wave*0.035f));
            monsters.Add(new Monster{ type=type,name=data.name,icon=data.icon,hp=(int)Math.Round(data.hp*hpScale),maxHp=(int)Math.Round(data.hp*hpScale),damage=Math.Max(3,(int)Math.Round(data.damage*balance.enemyDamage*(0.68f+Math.Min(0.16f,beastWave.wave*0.03f)))),speed=data.speed*balance.enemySpeed*0.88f,radius=data.radius,collisionRadius=data.collisionRadius,attackRange=data.attackRange,attackCooldown=data.attackCooldown,xp=data.xp,gold=data.gold,aerial=data.aerial,ranged=data.ranged,x=x,y=y,facing=angle+(float)Math.PI,animTime=G.Rand(0,10),elite=false,beastWave=true,state="idle" });
        }
        beastWave.remaining=count; screenShake=1; Toast("第 "+beastWave.wave+" 波兽潮来袭！立即进入已占领防御塔射程","warning");
    }

    void StartMapEvent(){ var e=mapEvents[G.RandInt(0,mapEvents.Count-1)]; activeEvent=new MapEvent{ id=e.id,name=e.name,color=e.color,text=e.text,duration=e.duration,timeLeft=e.duration };
        eventModifiers=new EventModifiers();
        if(activeEvent.id=="blood_moon"){ eventModifiers=new EventModifiers{enemySpeed=1.18f,enemyDamage=1.22f,loot=2,vision=1}; }
        if(activeEvent.id=="mist"){ eventModifiers=new EventModifiers{enemySpeed=0.78f,enemyDamage=1,loot=1,vision=0.62f}; }
        if(activeEvent.id=="meteor"){ for(int i=0;i<4;i++) SpawnGroundLoot("material","天外晶屑",1,"◆",G.Rand(350,2050),G.Rand(350,2050)); }
        Toast("地图事件："+activeEvent.name+" - "+activeEvent.text,"warning");
    }

    void UpdateMission(){ if(objective==null||objective.complete) return;
        if(objective.type=="hunt") objective.progress=killCount;
        if(objective.type=="scavenge") objective.progress=chestOpened;
        if(objective.type=="tower"){ int p=0; foreach(var t in towers) if(t.state=="player") p++; objective.progress=p; }
        if(objective.progress>=objective.target){ objective.complete=true; GameState.gold+=35*map.tier;
            consumables["herb_kit"]=(consumables.ContainsKey("herb_kit")?consumables["herb_kit"]:0)+1; Toast("任务完成："+objective.title+"，获得金币与补给","success"); SpawnBoss(); }
    }

    Tuple<float,float> FindSafeSpawn(float minEdge,float maxEdge,float radius){ float x=G.Rand(minEdge,maxEdge),y=G.Rand(minEdge,maxEdge);
        for(int a=0;a<24;a++){ if(!CollidesWithObstacle(x,y,radius+12) && G.Dist(x,y,player.x,player.y)>140) return Tuple.Create(x,y); x=G.Rand(minEdge,maxEdge); y=G.Rand(minEdge,maxEdge); }
        return Tuple.Create(x,y);
    }
}
