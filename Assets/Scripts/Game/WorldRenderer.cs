using System;
using System.Collections.Generic;
using UnityEngine;

// 阶段2：精灵化世界渲染器。把远征(像素 y-down)映射到 Unity 世界(x,z)用真实精灵 + 斜俯视相机呈现。
public class WorldRenderer
{
    public const float S = 0.025f;   // 1像素=0.025世界单位 (2400px 地图=60 units)
    public GameObject root;
    public SpriteRenderer playerSprite;
    public GameCameraFollow camFollow;
    public FogOfWar fog;
    List<EntityRef> monsters = new List<EntityRef>();
    List<EntityRef> obstacles = new List<EntityRef>();
    Dictionary<Monster,EntityRef> monsterRefs = new Dictionary<Monster,EntityRef>();
    class EntityRef { public SpriteRenderer sr; public Monster m; public Obstacle o; public float baseSort; }
    int mapSizePx = 2400; float mapUnits; Sprite circleSpr;
    public Sprite groundSpr;

    int LastTier=1;
    public void Build(Expedition exp){ LastTier=exp.map.tier;
        Clear();
        root = new GameObject("WorldRenderer"); mapUnits = mapSizePx*S;
        BuildGround(exp);
        BuildEntities(exp);
        if(Camera.main==null){ var c=new GameObject("Main Camera"); c.tag="MainCamera"; c.AddComponent<Camera>(); }
        camFollow = Camera.main.GetComponent<GameCameraFollow>() ?? Camera.main.gameObject.AddComponent<GameCameraFollow>();
        camFollow.target = playerSprite.transform;
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=0.008f; RenderSettings.fogColor=new Color(0.18f,0.25f,0.2f);
        BuildFog();
        BuildLightShafts();
        MapTheme.Apply(GameFlow.I!=null?GameFlow.I.sceneBoot?.Key:null, GameFlow.I!=null?GameFlow.I.sceneBoot?.Volume:null, Camera.main, exp.map.tier);
    }

    void BuildLightShafts(){
        Vector3[] spots = new Vector3[]{ new Vector3(mapUnits*0.3f,6,mapUnits*0.34f), new Vector3(mapUnits*0.62f,6,mapUnits*0.55f), new Vector3(mapUnits*0.4f,6,mapUnits*0.78f) };
        int tier=LastTier;
        Color shaftCol=MapTheme.Get(tier).shaft;
        foreach(var p in spots){
            var go=new GameObject("LightShaft"); go.transform.SetParent(root.transform);
            var ls=go.AddComponent<LightShaft>();
            go.transform.position=p;
            go.transform.localScale=new Vector3(ls.width, ls.height, 1f);
            if(ls.mat!=null){ ls.mat.SetColor("_Color", shaftCol); }
        }
    }

    void BuildFog(){
        var quad = new GameObject("FogOfWar"); quad.transform.SetParent(root.transform);
        var mf = quad.AddComponent<MeshFilter>(); mf.mesh = MeshFactory.Plane();
        fog = quad.AddComponent<FogOfWar>();
        quad.transform.position = new Vector3(mapUnits*0.5f, 6f, mapUnits*0.5f);
        quad.transform.localScale = new Vector3(mapUnits, 1f, mapUnits);
        fog.Setup(mapUnits);
    }

    public void Tick(Expedition exp){
        if(root==null) return;
        SyncMonsters(exp);
        // 玩家
        playerSprite.transform.position = new Vector3(exp.player.x*S, 0, exp.player.y*S);
        playerSprite.sortingOrder = (int)(exp.player.y*S*100);
        // 怪物
        foreach(var er in monsters){
            if(er.m==null) continue;
            er.sr.transform.position = new Vector3(er.m.x*S, 0, er.m.y*S);
            er.sr.sortingOrder = (int)(er.m.y*S*100);
            Sprite ms = MonsterSprite(er.m);
            bool hasSprite=ms!=null;
            if(hasSprite){ er.sr.sprite=ms; er.sr.color=Color.white; } else { er.sr.sprite=circleSpr; er.sr.color=MonsterColor(er.m.type); }
            er.sr.flipX = Math.Cos(er.m.facing) < 0;
            er.sr.enabled = exp.IsWorldVisible(er.m.x, er.m.y);
            float scale=hasSprite?Mathf.Max(0.55f,er.m.radius/24f):Mathf.Max(0.7f,er.m.radius*S*2f);
            er.sr.transform.localScale = new Vector3(scale,scale,1);
        }
        if(fog!=null) fog.Tick(exp.player);
        // 障碍（静态，只放一次位置）
        foreach(var er in obstacles){ if(er.o!=null && er.sr!=null){ if(er.sr.transform.position==Vector3.zero){ er.sr.transform.position=new Vector3(er.o.x*S,0,er.o.y*S); er.sr.sortingOrder=(int)(er.o.y*S*100);} er.sr.enabled = exp.IsWorldVisible(er.o.x,er.o.y); } }
    }

    void BuildGround(Expedition exp){
        groundSpr=SpriteStore.Map(exp.map.id);
        if(groundSpr==null){
            int res=1024; var c2d=new Canvas2D(res,res); PaintWholeMap(c2d,exp,res);
            var tex=new Texture2D(res,res,TextureFormat.RGBA32,false); Flip(c2d,tex); tex.Apply(false);
            groundSpr=Sprite.Create(tex,new Rect(0,0,res,res),new Vector2(0.5f,0.5f),res/mapUnits);
        }
        var go=new GameObject("Ground"); go.transform.SetParent(root.transform);
        var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=groundSpr; sr.sortingOrder=-1000;
        go.transform.position=new Vector3(mapUnits*0.5f,0,mapUnits*0.5f);
        go.transform.rotation=Quaternion.Euler(90f,0,0);
        Vector2 size=groundSpr.bounds.size;
        if(size.x>0&&size.y>0) go.transform.localScale=new Vector3(mapUnits/size.x,mapUnits/size.y,1);
    }

    void PaintWholeMap(Canvas2D c, Expedition exp, int res){
        float scale=res/(float)mapSizePx;
        c.FillRect(0,0,res,res,G.ParseColor(exp.map.bgColor));
        foreach(var p in exp.terrainPatches){ Color col=G.ParseColor(p.color); col.a=p.alpha; c.FillBlob(p.x*scale,p.y*scale,p.rx*scale,p.ry*scale,p.rotation,p.phase,col,0.08f); }
        foreach(var r in exp.terrainRoads){ Color path=G.ParseColor(exp.map.terrainPath); path.a=0.62f; c.StrokeQuadCurve(r.x1*scale,r.y1*scale,r.cx*scale,r.cy*scale,r.x2*scale,r.y2*scale,path,Math.Max(1,r.width*scale)); }
        foreach(var f in exp.terrainFields){ Color fc=f.ruined?new Color(0.196f,0.137f,0.118f,0.14f):new Color(0.396f,0.275f,0.153f,0.13f); c.FillBlob((f.x+f.w/2)*scale,(f.y+f.h/2)*scale,f.w*0.46f*scale,f.h*0.46f*scale,f.rotation,f.x,fc,0.08f); }
    }
    void Flip(Canvas2D c, Texture2D tex){ Color32[] fl=new Color32[c.w*c.h]; for(int y=0;y<c.h;y++) Array.Copy(c.px,y*c.w,fl,(c.h-1-y)*c.w,c.w); tex.SetPixels32(fl); }

    void BuildEntities(Expedition exp){
        circleSpr = MakeColorSprite(new Color(1,1,1,1));
        // 玩家
        var pgo=new GameObject("Player"); pgo.transform.SetParent(root.transform);
        playerSprite=pgo.AddComponent<SpriteRenderer>(); playerSprite.sprite=MakeColorSprite(G.ParseColor("#c84e2f")); playerSprite.sortingOrder=10;
        playerSprite.transform.localScale=new Vector3(0.9f,0.9f,1f);
        playerSprite.transform.position=new Vector3(exp.player.x*S,0,exp.player.y*S);
        // 怪物
        foreach(var m in exp.monsters) AddMonster(m);
        // 障碍
        foreach(var o in exp.obstacles){ var spr=ObstacleSprite(o.type); var go=new GameObject("O_"+o.type); go.transform.SetParent(root.transform); var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=spr; obstacles.Add(new EntityRef{sr=sr,o=o}); }
    }
    public int NumMonsterRenders(){ int n=0; foreach(var er in monsters) if(er.sr!=null&&er.sr.enabled) n++; return n; }
    Sprite MonsterSprite(Monster m){ return m.type=="boss"?SpriteStore.Boss(LastTier):SpriteStore.Monster(m.type,m.hitFlash>0?"hit":(m.state=="death"?"death":(m.state=="attack"?"attack":"idle"))); }
    Sprite ObstacleSprite(string type){ return SpriteStore.Obstacle(type) ?? (type=="fence"?SpriteStore.Obstacle("tree"):type=="hay"?SpriteStore.Obstacle("bush"):circleSpr); }
    void AddMonster(Monster m){
        if(m==null||monsterRefs.ContainsKey(m)) return;
        var spr=MonsterSprite(m); var go=new GameObject("M_"+m.type); go.transform.SetParent(root.transform);
        var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=spr??circleSpr;
        var er=new EntityRef{sr=sr,m=m}; monsters.Add(er); monsterRefs[m]=er;
    }
    void SyncMonsters(Expedition exp){ foreach(var m in exp.monsters) AddMonster(m); }
    static Color MonsterColor(string t){ switch(t){ case "boar": return new Color(0.62f,0.46f,0.32f); case "bat": return new Color(0.55f,0.4f,0.68f); case "spider": return new Color(0.5f,0.6f,0.4f); case "locust": return new Color(0.6f,0.7f,0.3f); case "wolf": return new Color(0.7f,0.72f,0.78f); default: return Color.white; } }
    public void Clear(){ if(root!=null) UnityEngine.Object.Destroy(root); monsters.Clear(); monsterRefs.Clear(); obstacles.Clear(); }
    static Sprite MakeColorSprite(Color col){
        const int n=32; var t=new Texture2D(n,n,TextureFormat.RGBA32,false); var px=new Color32[n*n];
        for(int y=0;y<n;y++) for(int x=0;x<n;x++){ float dx=x-(n-1)*0.5f,dy=y-(n-1)*0.5f; px[y*n+x]=dx*dx+dy*dy<=(n*0.47f)*(n*0.47f)?(Color32)col:new Color32(0,0,0,0); }
        t.SetPixels32(px); t.Apply(); return Sprite.Create(t,new Rect(0,0,n,n),new Vector2(0.5f,0.5f),32f);
    }
}
