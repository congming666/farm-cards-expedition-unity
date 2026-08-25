using System;
using System.Collections.Generic;
using UnityEngine;

// ============ 远征地形：生成 + 地形块烘焙（移植自 expedition.js generateTerrain / renderTerrain / renderTerrainDirect） ============
public partial class Expedition
{
    // 障碍类型表
    static readonly string[][] ObstacleTypes = new string[][]{
        new[]{"tree","bush","rock","hay","fence"},
        new[]{"tree","bush","rock","hay","fence","ruin"},
        new[]{"deadTree","rock","ruin","toxicCrystal","fence"},
        new[]{"deadTree","rock","monolith","voidCrystal","ruin"},
    };
    static readonly Dictionary<string,float> ObstFoot = new Dictionary<string,float>{{"tree",24},{"bush",24},{"rock",19},{"hay",20},{"fence",26},{"ruin",25},{"deadTree",20},{"toxicCrystal",16},{"voidCrystal",16},{"monolith",19}};

    void GenerateTerrain(){
        int size=2400; var theme=map;
        // 出生区固定道路
        terrainRoads.Add(new TerrainRoad{ x1=-100,y1=470,cx=size*0.46f,cy=650,x2=size+100,y2=560,width=62 });
        terrainPatches.Add(new TerrainPatch{ x=1030,y=360,rx=185,ry=120,rotation=-0.18f,type="water",color=theme.terrainWater,alpha=0.46f,phase=0.8f });
        terrainFields.Add(new TerrainField{ x=390,y=250,w=360,h=235,rotation=0.03f,ruined=map.tier>=3 });
        // 额外道路
        for(int i=0;i<Math.Max(1,map.tier-1);i++){
            bool horiz=i%2==0;
            if(horiz) terrainRoads.Add(new TerrainRoad{ x1=-100,y1=G.Rand(260,size-260),cx=size*0.5f,cy=G.Rand(240,size-240),x2=size+100,y2=G.Rand(260,size-260),width=G.Rand(46,72) });
            else terrainRoads.Add(new TerrainRoad{ x1=G.Rand(260,size-260),y1=-100,cx=G.Rand(240,size-240),cy=size*0.5f,x2=G.Rand(260,size-260),y2=size+100,width=G.Rand(46,72) });
        }
        // 地形斑块
        for(int i=0;i<12+map.tier*3;i++){ float wc=0.14f+map.tier*0.015f; string type=RandPct(wc*100)?"water":(RandPct(50)?"soil":"grass");
            terrainPatches.Add(new TerrainPatch{ x=G.Rand(100,size-100),y=G.Rand(100,size-100),rx=G.Rand(90,260),ry=G.Rand(65,190),rotation=G.Rand(0f,(float)Math.PI),type=type,color=type=="water"?theme.terrainWater:(type=="soil"?theme.terrainSoil:theme.terrainGlow),alpha=type=="grass"?0.07f:(type=="water"?0.42f:0.34f),phase=G.Rand(0f,(float)(Math.PI*2)) }); }
        // 田块
        for(int i=0;i<3+map.tier;i++){ terrainFields.Add(new TerrainField{ x=G.Rand(120,size-520),y=G.Rand(120,size-420),w=G.Rand(230,470),h=G.Rand(150,330),rotation=G.Rand(-0.16f,0.16f),ruined=RandPct(map.tier*16) }); }
        // 地表细节
        string[] gd = map.tier<=2 ? new[]{"grass","pebble","straw"} : new[]{"crack","pebble","blight"};
        for(int i=0;i<30+map.tier*8;i++){ terrainDecor.Add(new TerrainDecor{ x=G.Rand(50,size-50),y=G.Rand(50,size-50),kind=gd[G.RandInt(0,gd.Length-1)],size=G.RandInt(5,13),alpha=G.Rand(0.18f,0.42f),rotation=G.Rand(0f,(float)(Math.PI*2)) }); }
        // 障碍
        var obstacleTypes=ObstacleTypes[map.tier-1];
        for(int i=0;i<22+map.tier*7;i++){ float x=G.Rand(120,size-120),y=G.Rand(120,size-120); int attempts=0;
            while(G.Dist(x,y,player.x,player.y)<260 && attempts++<12){ x=G.Rand(120,size-120); y=G.Rand(120,size-120); }
            string type=obstacleTypes[G.RandInt(0,obstacleTypes.Length-1)];
            float scale=G.Rand(0.78f,1.22f);
            if(type=="tree")scale*=1.1f; else if(type=="bush")scale*=0.88f; else if(type=="deadTree")scale*=1.05f; else if(type=="rock")scale*=0.9f; else if(type=="hay")scale*=0.9f; else if(type=="fence")scale*=1.15f; else if(type=="ruin")scale*=1.25f; else if(type=="monolith")scale*=1.25f;
            float fp=ObstFoot.ContainsKey(type)?ObstFoot[type]:18;
            float rX=0,rY=0,oY=0;
            if(type=="tree"){rX=29;rY=17;oY=4;} else if(type=="bush"){rX=31;rY=17;oY=4;} else if(type=="rock"){rX=23;rY=15;oY=3;} else if(type=="deadTree"){rX=24;rY=15;oY=3;}
            obstacles.Add(new Obstacle{ type=type,x=x,y=y,scale=scale,radius=fp*scale,collisionRx=rX*scale,collisionRy=rY*scale,collisionOffsetY=oY*scale,rotation=G.Rand(-0.16f,0.16f) });
        }
        // 陷阱
        var traps=new[]{ new Trap{type="thorn",name="荆棘丛",icon="🌵",color="#85c85d",radius=34,damage=8,cooldown=1.4f,slow=1.1f},
            new Trap{type="bear",name="捕兽夹",icon="⚙️",color="#e5b65a",radius=26,damage=18,cooldown=3.5f,slow=2.2f},
            new Trap{type="poison",name="毒孢子",icon="☣️",color="#90d354",radius=58,damage=7,cooldown=1.1f,slow=0.7f},
            new Trap{type="lightning",name="落雷符文",icon="⚡",color="#b999ff",radius=52,damage=28,cooldown=4.5f,slow=0.3f} };
        int avail=Math.Min(traps.Length,Math.Max(2,map.tier+1));
        for(int i=0;i<5+map.tier*3;i++){ var b=traps[G.RandInt(0,avail-1)];
            this.traps.Add(new Trap{ type=b.type,name=b.name,icon=b.icon,color=b.color,radius=b.radius,damage=b.damage,cooldown=b.cooldown,slow=b.slow,x=G.Rand(380,size-180),y=G.Rand(180,size-180),triggerCd=G.Rand(0,b.cooldown),phase=G.Rand(0f,(float)(Math.PI*2)) });
        }
    }

    // 地形块 painter：把世界坐标 [wx, wx+cw]x[wy, wy+ch] 的纯地面画进 c2d
    public void PaintChunk(Canvas2D c2d, int wx, int wy){
        int cw=c2d.w, ch=c2d.h;
        c2d.FillRect(0,0,cw,ch,G.ParseColor(map.bgColor));
        // 草地草叶（确定性）
        c2d.globalAlpha=0.11f;
        Color glow=G.ParseColor(map.terrainGlow);
        int seed=(int)Math.Floor(wx/38f)*17 + (int)Math.Floor(wy/38f)*31;
        for(int i=0;i<160;i++){ float x=((i*83+seed*7)%(cw+80))-40; float y=((i*137+seed*11)%(ch+80))-40; c2d.StrokeLine(x,y+4,x+3,y-3,glow,1f); }
        c2d.globalAlpha=1;
        // 斑块
        foreach(var p in terrainPatches){ float sx=p.x-wx, sy=p.y-wy; if(sx<-p.rx||sx>cw+p.rx||sy<-p.ry||sy>ch+p.ry) continue;
            Color col=G.ParseColor(p.color); col.a=p.alpha;
            c2d.FillBlob(sx,sy,p.rx,p.ry,p.rotation,p.phase,col,0.08f);
            if(p.type=="water"){ Color wlc=G.ParseColor("#c1eef0"); c2d.globalAlpha=0.24f; c2d.SaveTransform(); c2d.Translate(sx,sy); c2d.Rotate(p.rotation); c2d.StrokeCircle(0,0,Math.Min(p.rx,p.ry)*0.9f,wlc,3f); c2d.RestoreTransform(); c2d.globalAlpha=1; } }
        // 道路（阴影+路面+高光）
        foreach(var r in terrainRoads){ Color shadow=new Color(0.078f,0.07f,0.05f,0.32f); c2d.StrokeQuadCurve(r.x1-wx,r.y1-wy,r.cx-wx,r.cy-wy,r.x2-wx,r.y2-wy,shadow,r.width+12);
            Color path=G.ParseColor(map.terrainPath); path.a=0.62f; c2d.StrokeQuadCurve(r.x1-wx,r.y1-wy,r.cx-wx,r.cy-wy,r.x2-wx,r.y2-wy,path,r.width); }
        // 道路高光
        Color hl=new Color(0.94f,0.86f,0.67f,0.16f); foreach(var r in terrainRoads){ c2d.StrokeQuadCurve(r.x1-wx,r.y1-wy,r.cx-wx,r.cy-wy,r.x2-wx,r.y2-wy,hl,Math.Max(4,r.width*0.16f)); }
        // 田块
        foreach(var f in terrainFields){ float cx=f.x+f.w/2f-wx, cy=f.y+f.h/2f-wy; if(cx<-f.w||cx>cw+f.w||cy<-f.h||cy>ch+f.h) continue;
            Color fc=f.ruined?new Color(0.196f,0.137f,0.118f,0.14f):new Color(0.396f,0.275f,0.153f,0.13f);
            c2d.FillBlob(cx,cy,f.w*0.46f,f.h*0.46f,f.rotation,f.x,fc,0.08f); }
        // 地表细节
        foreach(var d in terrainDecor){ float sx=d.x-wx; if(sx<-40||sx>cw+40) continue; float sy=d.y-wy; if(sy<-40||sy>ch+40) continue;
            c2d.SaveTransform(); c2d.Translate(sx,sy); c2d.Rotate(d.rotation); c2d.globalAlpha=d.alpha;
            if(d.kind=="grass"){ Color g=G.ParseColor("#92b56c"); for(int i=-1;i<=1;i++){ c2d.StrokeLine(i*3,3,i*5,-d.size,g,1.4f); } }
            else if(d.kind=="pebble"){ c2d.FillEllipse(0,0,d.size,d.size*0.45f,G.ParseColor("#9a9687")); }
            else if(d.kind=="straw"){ Color st=G.ParseColor("#c5a75d"); for(int i=-2;i<=2;i++){ c2d.StrokeLine(-d.size,i*2,d.size,i*2-4,st,1.2f); } }
            else if(d.kind=="crack"){ Color ck=G.ParseColor("#1d1718"); c2d.StrokeLine(-d.size,-3,0,0,ck,1.6f); c2d.StrokeLine(0,0,d.size,4,ck,1.6f); c2d.StrokeLine(0,0,4,-d.size,ck,1.6f); }
            else { c2d.FillCircle(0,0,d.size,G.ParseColor("#667b3b")); }
            c2d.globalAlpha=1; c2d.RestoreTransform();
        }
    }

    // 实体生成（移植自 spawnEntities）
    void SpawnEntities(){
        int size=2400;
        string[] basic={"boar","bat","spider"};
        for(int i=0;i<map.monsterCount;i++){ var extra=new List<string>(); if(map.tier>=2) extra.Add("locust"); if(map.tier>=3) extra.Add("wolf");
            var pool=new List<string>(basic); pool.AddRange(extra); string type=pool[G.RandInt(0,pool.Count-1)]; var data=GameData.Monsters[type];
            var pos=FindSafeSpawn(300,size-300,data.radius); bool elite=RandPct(balance.eliteChance*100);
            float hpScale=balance.enemyHp*(elite?1.75f:1);
            monsters.Add(new Monster{ type=type,name=data.name,icon=data.icon,x=pos.Item1,y=pos.Item2,hp=(int)Math.Round(data.hp*hpScale),maxHp=(int)Math.Round(data.hp*hpScale),damage=(int)Math.Round(data.damage*balance.enemyDamage*(elite?1.25f:1)),speed=data.speed*balance.enemySpeed,radius=data.radius,collisionRadius=data.collisionRadius,attackRange=data.attackRange,attackCooldown=data.attackCooldown,xp=data.xp,gold=data.gold,aerial=data.aerial,ranged=data.ranged,facing=G.Rand(0f,(float)(Math.PI*2)),animTime=G.Rand(0,10),hitFlash=0,elite=elite,abilityCd=G.Rand(1,4),packOffset=G.Rand(-1,1),state="idle",stateTimer=0 });
        }
        for(int i=0;i<map.chestCount;i++){ var pos=FindSafeSpawn(200,size-200,24); chests.Add(new Chest{x=pos.Item1,y=pos.Item2,opened=false,radius=24,hasSignal=RandPct(15) }); }
        int towerCount=3+map.tier;
        for(int i=0;i<towerCount;i++){ var pos=FindSafeSpawn(300,size-300,30); string st=i==0?"neutral":(RandPct(55)?"neutral":(RandPct(72)?"enemy":"broken"));
            towers.Add(new Tower{x=pos.Item1,y=pos.Item2,state=st,radius=30,range=230,damage=10+map.tier*2,attackCd=0,hp=180+map.tier*55,maxHp=180+map.tier*55,captureProgress=0 }); }
        for(int i=0;i<map.raiderCount;i++){ var pos=FindSafeSpawn(400,size-400,16); raiders.Add(new Raider{x=pos.Item1,y=pos.Item2,hp=80,maxHp=80,damage=10,speed=130,radius=16,attackCd=0,stunned=0,state="patrol",patrolX=G.Rand(200,size-200),patrolY=G.Rand(200,size-200),loot=G.RandInt(1,3) }); }
        extractPoints.Add(new ExtractPoint{x=G.Rand(100,300),y=G.Rand(100,size-100),radius=50 });
        extractPoints.Add(new ExtractPoint{x=G.Rand(size-300,size-100),y=G.Rand(100,size-100),radius=50 });
    }
}
