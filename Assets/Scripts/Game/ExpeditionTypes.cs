using System;
using System.Collections.Generic;
using UnityEngine;

// ================= 远征实体类型（移植自 expedition.js 数据结构 + performance.js SpatialHash） =================
public class PlayerState { public float x,y,hp,maxHp,energy,maxEnergy,speed,radius,collisionRadius, angle, attackCd, invuln, stealth, slow, vx,vy, visualZ, visualVz; }

public class Monster { public string type,name,icon,state; public float hp,maxHp,damage,speed,radius,collisionRadius,attackRange,attackCooldown,xp,gold; public bool aerial,ranged;
    public float x,y,attackCd,stunned,facing,animTime,hitFlash,abilityCd,packOffset,stateTimer,deathTimer,visualZ,visualVz; public float? wanderX,wanderY; public int phase; public object target; public bool elite, beastWave, deathProcessed; }

public class Chest { public float x,y,radius; public bool opened,hasSignal; }
public class Tower { public float x,y,radius,range,damage,attackCd,hp,maxHp,captureProgress; public string state; }
public class Raider { public float x,y,hp,maxHp,damage,speed,radius,attackCd,stunned; public object target; public string state; public float patrolX,patrolY; public int loot; }
public class ExtractPoint { public float x,y,radius; }
public class Obstacle { public string type; public float x,y,scale,radius,collisionRx,collisionRy,collisionOffsetY,rotation; }
public class Trap { public string type,name,icon,color; public float radius,damage,cooldown,slow,x,y,triggerCd,phase; }
public class GroundLoot { public string type,name,icon,cropId; public float amount, x,y,bob, duration; public bool rare,legendary; }
public class Projectile { public float x,y,vx,vy,damage,life,radius; public bool fromPlayer,fromTower,fromMonster; public string weaponId,color,monsterType,pierce; public object target; public List<object> hit=new List<object>(); }
public class Particle { public float x,y,vx,vy,life,maxLife,size,angle; public string color,type; }
public class DamageNumber { public float x,y,vx,vy,life,maxLife; public float value; public string color; public bool heavy; }
public class TerrainPatch { public float x,y,rx,ry,rotation,alpha,phase; public string type,color; }
public class TerrainRoad { public float x1,y1,cx,cy,x2,y2,width; }
public class TerrainField { public float x,y,w,h,rotation; public bool ruined; }
public class TerrainDecor { public float x,y,size,alpha,rotation; public string kind; }
public class ECamera { public float x,y; }
public class Mission { public string type,title,description; public int target,progress; public bool complete; }
public class MapEvent { public string id,name,color,text; public float duration,timeLeft; }

// 空间哈希（移植自 performance.js SpatialHash）
public class SpatialHash
{
    float cellSize; Dictionary<long,List<Monster>> cells = new Dictionary<long,List<Monster>>();
    public void Rebuild(List<Monster> items, float cs){ }

    // rebuilt each frame with current lists; query via monsters list directly per-cell
    private float cs; private List<Monster> all;
    public SpatialHash(float cellSize){ this.cellSize=cellSize; }
    public void Build(List<Monster> items){ all=items; cells.Clear(); for(int i=0;i<items.Count;i++){ var it=items[i]; if(it==null||it.hp<=0)continue; long k=Key(it.x,it.y); if(!cells.ContainsKey(k)) cells[k]=new List<Monster>(); cells[k].Add(it);} }
    long Key(float x,float y){ long cx=(long)Math.Floor(x/cellSize), cy=(long)Math.Floor(y/cellSize); return cx*100000+cy; }
    public void QueryCircle(float x,float y,float radius,List<Monster> result){
        result.Clear();
        long minX=(long)Math.Floor((x-radius)/cellSize), maxX=(long)Math.Floor((x+radius)/cellSize);
        long minY=(long)Math.Floor((y-radius)/cellSize), maxY=(long)Math.Floor((y+radius)/cellSize);
        for(long cy=minY;cy<=maxY;cy++) for(long cx=minX;cx<=maxX;cx++){ long k=cx*100000+cy; List<Monster> b; if(cells.TryGetValue(k,out b)) { for(int i=0;i<b.Count;i++) result.Add(b[i]); } }
    }
}
