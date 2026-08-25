using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldTextDraw { public float x,y; public string text; public float size; public string color; public int align; public bool heavy; }

// ============ 远征渲染（移植自 expedition.js render / renderMonster / renderObstacle / renderHero / renderWeather / renderFogOfWar） ============
public partial class Expedition
{
    public List<WorldTextDraw> worldTexts = new List<WorldTextDraw>();
    public List<Obstacle> renderBehind = new List<Obstacle>();
    public List<Obstacle> renderFront = new List<Obstacle>();
    float mapSize=2400;

    public void Render(Canvas2D frame, ECamera cam, RenderBackend backend){
        worldTexts.Clear();
        float t=Time.time;
        // 事件迷雾
        if(activeEvent!=null && activeEvent.id=="mist"){ frame.SaveTransform(); frame.globalAlpha=1; frame.composite=0;
            float wx=640,wy=360; frame.FillRadialGrad(wx,wy,150,650,new[]{new Color(0.75f,0.84f,0.86f,0.02f),new Color(0.59f,0.71f,0.73f,0.18f),new Color(0.05f,0.09f,0.11f,0.72f)},new[]{0f,0.55f,1f}); }
        // 地形块
        float chunk=RenderBackend.CHUNK;
        int minCX=Math.Max(0,(int)Math.Floor(cam.x/chunk));
        int maxCX=Math.Min((int)Math.Ceiling(mapSize/chunk)-1,(int)Math.Floor((cam.x+1280)/chunk));
        int minCY=Math.Max(0,(int)Math.Floor(cam.y/chunk));
        int maxCY=Math.Min((int)Math.Ceiling(mapSize/chunk)-1,(int)Math.Floor((cam.y+720)/chunk));
        for(int cy=minCY;cy<=maxCY;cy++) for(int cx=minCX;cx<=maxCX;cx++){ backend.BlitChunk(backend.GetChunk(cx,cy),cx*chunk-cam.x,cy*chunk-cam.y); }
        // 地图边界
        frame.StrokeRect(-cam.x,-cam.y,mapSize,mapSize,G.ParseColor(map.accentColor),4);
        // 障碍（玩家身后）
        renderBehind.Clear(); renderFront.Clear();
        foreach(var o in obstacles){ if(o.y<=player.y) renderBehind.Add(o); else renderFront.Add(o); } renderBehind.Sort((a,b)=>a.y.CompareTo(b.y)); renderFront.Sort((a,b)=>a.y.CompareTo(b.y));
        foreach(var o in renderBehind) RenderObstacle(frame,o,cam);
        // 陷阱
        foreach(var tr in traps){ if(!IsWorldVisible(tr.x,tr.y)) continue; float sx=tr.x-cam.x, sy=tr.y-cam.y; if(sx<-80||sx>1360||sy<-80||sy>800) continue;
            float pulse=0.65f+(float)Math.Sin(tr.phase*3)*0.18f; Color col=G.ParseColor(tr.color); col.a=0.55f; frame.FillCircle(sx,sy,tr.radius*pulse,col); col.a=1; frame.StrokeCircle(sx,sy,tr.radius*pulse,col,2f);
            worldTexts.Add(new WorldTextDraw{ x=sx,y=sy+7,text=tr.icon,size=tr.type=="bear"?20:24,color=tr.color }); }
        // 地面战利品
        foreach(var loot in groundLoot){ if(!IsWorldVisible(loot.x,loot.y)) continue; float sx=loot.x-cam.x, sy=loot.y-cam.y+(float)Math.Sin(t*3+loot.bob)*4; if(sx<-50||sx>1330||sy<-50||sy>770) continue;
            bool near=G.Dist(player.x,player.y,loot.x,loot.y)<=82; Color glow=loot.type=="invincible"?new Color(0.35f,0.88f,1f):new Color(0.965f,0.78f,0.357f); glow.a=near?0.52f:0.28f; frame.FillCircle(sx,sy,28,glow);
            worldTexts.Add(new WorldTextDraw{ x=sx,y=sy+7,text=loot.icon,size=25,color="#fff" });
            if(near) worldTexts.Add(new WorldTextDraw{ x=sx,y=sy-22,text="自动拾取 "+loot.name,size=10,color="#f6d77e" }); }
        // 撤离点
        foreach(var ep in extractPoints){ if(!IsWorldVisible(ep.x,ep.y)) continue; float sx=ep.x-cam.x, sy=ep.y-cam.y; Color green=new Color(0.5f,1f,0.5f,0.15f); frame.FillCircle(sx,sy,ep.radius,green); frame.StrokeCircle(sx,sy,ep.radius,new Color(0.5f,1f,0.5f,1f),2f); worldTexts.Add(new WorldTextDraw{ x=sx,y=sy-ep.radius-8,text="🚁 撤离点",size=20,color="#7fff7f",align=1 }); }
        // 宝箱
        foreach(var c in chests){ if(!IsWorldVisible(c.x,c.y)) continue; float sx=c.x-cam.x, sy=c.y-cam.y; if(sx<-50||sx>1330||sy<-50||sy>770) continue; worldTexts.Add(new WorldTextDraw{ x=sx,y=sy+8,text=c.opened?"📭":"📦",size=28,color="#fff" }); if(!c.opened) frame.StrokeCircle(sx,sy,20,new Color(1f,0.84f,0f,1f),2f); }
        // 防御塔
        foreach(var tw in towers){ if(!IsWorldVisible(tw.x,tw.y)) continue; float sx=tw.x-cam.x, sy=tw.y-cam.y; if(sx<-50||sx>1330||sy<-50||sy>770) continue;
            string icon="🗼", color="#666"; if(tw.state=="player"){icon="🏰";color="#7fff7f";} else if(tw.state=="enemy"){color="#ff4444";} else if(tw.state=="broken"){icon="💨";color="#444";}
            worldTexts.Add(new WorldTextDraw{ x=sx,y=sy+8,text=icon,size=28,color=color }); if(tw.state!="broken"){ Color cc=G.ParseColor(color); frame.StrokeCircle(sx,sy,tw.radius,cc,2f); cc.a=0.2f; frame.StrokeCircle(sx,sy,tw.range,cc,1.5f); } }
        // 怪物
        foreach(var m in monsters){ if(!IsWorldVisible(m.x,m.y)) continue; float sx=m.x-cam.x, sy=m.y-cam.y; if(sx<-50||sx>1330||sy<-50||sy>770) continue; RenderMonster(frame,m,sx,sy,cam); if(m.stunned>0) worldTexts.Add(new WorldTextDraw{ x=sx,y=sy-m.radius-18,text="💫",size=14,color="#ffff00" }); }
        // 掠夺者
        foreach(var r in raiders){ if(!IsWorldVisible(r.x,r.y)) continue; float sx=r.x-cam.x, sy=r.y-cam.y; frame.FillRect(sx-20,sy-r.radius-12,40,5,new Color(0.2f,0.2f,0.2f,1f)); float pct=G.Clamp(r.hp/r.maxHp,0,1); frame.FillRect(sx-20,sy-r.radius-12,40*pct,5,new Color(1f,0.4f,0.27f,1f)); worldTexts.Add(new WorldTextDraw{ x=sx,y=sy+8,text="🥷",size=24,color="#fff" }); }
        // 弹道
        foreach(var p in projectiles){ if(p.fromMonster && !IsWorldVisible(p.x,p.y)) continue; float sx=p.x-cam.x, sy=p.y-cam.y; Color col=G.ParseColor(p.color); col.a=1;
            if(p.fromPlayer && p.weaponId=="pea_repeater"){ frame.FillCircle(sx,sy,p.radius,col); }
            else if(p.fromPlayer && p.weaponId=="vine_staff"){ frame.FillEllipse(sx,sy,27,7,col,(float)Math.Atan2(p.vy,p.vx)); }
            else frame.FillCircle(sx,sy,p.radius,col); }
        // 玩家
        float psx=player.x-cam.x, psy=player.y-cam.y; frame.globalAlpha=player.stealth>0?0.4f:1f; if(player.invuln>0 && (int)(player.invuln*10)%2==0) frame.globalAlpha*=0.5f; RenderHero(frame,psx,psy,cam,t); frame.globalAlpha=1;
        // 障碍（玩家身前）
        foreach(var o in renderFront) RenderObstacle(frame,o,cam);
        // 粒子
        foreach(var p in particles){ float sx=p.x-cam.x, sy=p.y-cam.y; float alpha=G.Clamp(p.life/p.maxLife,0,1); Color col=G.ParseColor(p.color); col.a=1;
            if(p.type=="aoe"){ frame.globalAlpha=alpha*0.6f; frame.StrokeCircle(sx,sy,p.size*(1-alpha*0.3f),col,3f); frame.globalAlpha=1; }
            else if(p.type=="slash"){ frame.SaveTransform(); frame.Translate(sx,sy); frame.Rotate(p.angle); frame.globalAlpha=alpha; frame.StrokeCircle(0,0,p.size,col,4f); frame.globalAlpha=1; frame.RestoreTransform(); }
            else if(p.type=="weaponRing"){ frame.globalAlpha=alpha*0.65f; frame.StrokeCircle(sx,sy,p.size*(1.25f-alpha*0.25f),col,2f); frame.globalAlpha=1; }
            else if(p.type=="vine"){ frame.SaveTransform(); frame.Translate(sx,sy); frame.Rotate(p.angle); frame.globalAlpha=alpha; float[] pts=new float[12]; for(int i=0;i<=5;i++){ pts[i*2]=(float)Math.Sin(i*2.4f)*11; pts[i*2+1]=p.size*i/5f; } frame.StrokePolyLine(pts,col,4f); frame.globalAlpha=1; frame.RestoreTransform(); }
            else if(p.type=="earthTrail"){ frame.SaveTransform(); frame.Translate(sx,sy); frame.Rotate(p.angle); frame.globalAlpha=alpha*0.7f; frame.FillEllipse(0,0,p.size*1.8f,p.size*0.55f,G.ParseColor("#765c3d")); frame.globalAlpha=1; frame.RestoreTransform(); }
            else if(p.type=="smoke"){ col.a=alpha*0.32f; frame.FillCircle(sx,sy,p.size*(1.35f-alpha*0.35f),col); }
            else { col.a=alpha; frame.FillCircle(sx,sy,p.size*alpha,col); } frame.globalAlpha=1; }
        // 伤害数字（WorldText）
        foreach(var d in damageNumbers){ float sx=d.x-cam.x, sy=d.y-cam.y; float alpha=G.Clamp(d.life/d.maxLife,0,1); worldTexts.Add(new WorldTextDraw{ x=sx,y=sy,text="-"+d.value,size=d.heavy?20:15,color=d.color,heavy=d.heavy,align=1 }); }
        // 击杀闪白
        if(killFlash>0){ frame.globalAlpha=G.Clamp(killFlash*4.2f,0,0.42f); frame.FillRect(0,0,1280,720,new Color(1,1,1,1)); frame.globalAlpha=1; }
        // 撤离读条（世界文字）
        if(extracting){ float et=extractType=="signal"?20:15; float pct=G.Clamp(extractProgress/et,0,1); WorldTextDraw bar=new WorldTextDraw{ x=640,y=100,text=extractType=="signal"?"🔥 信号弹撤离中...":"🚁 撤离读条中...",size=16,color="#fff",align=1 }; worldTexts.Add(bar);
            worldTexts.Add(new WorldTextDraw{ x=640,y=142,text=(et-extractProgress).ToString("F1")+"秒",size=14,color="#fff",align=1 });
            frame.FillRect(490,100,300,50,new Color(0,0,0,0.7f)); frame.StrokeRect(490,100,300,50,new Color(1f,0.84f,0,1f),2f); frame.FillRect(492,120,296*pct,28,new Color(1f,0.84f,0,1f)); }
        RenderInteractPrompt(frame,cam);
    }

    // 地形块 painter 委托给 RenderBackend
    public void BindTerrainPainter(RenderBackend backend){ backend.TerrainPainter=(c2d,wx,wy)=>PaintChunk(c2d,wx,wy); }

    void RenderInteractPrompt(Canvas2D frame, ECamera cam){
        string prompt=null; float py=0, px=0;
        foreach(var c in chests){ if(!c.opened && IsWorldVisible(c.x,c.y) && G.Dist(player.x,player.y,c.x,c.y)<60){ prompt="左键打开宝箱"; px=c.x; py=c.y-40; break; } }
        if(prompt==null){ foreach(var tw in towers){ if(tw.state!="player" && IsWorldVisible(tw.x,tw.y) && G.Dist(player.x,player.y,tw.x,tw.y)<60){ prompt=tw.state=="broken"?"点击修复并占领防御塔":"点击占领防御塔"; px=tw.x; py=tw.y-40; break; } } }
        if(prompt==null){ foreach(var ep in extractPoints){ if(IsWorldVisible(ep.x,ep.y) && G.Dist(player.x,player.y,ep.x,ep.y)<ep.radius){ prompt="点击开始撤离"; px=ep.x; py=ep.y-ep.radius-20; break; } } }
        if(prompt!=null){ float sx=px-cam.x, sy=py-cam.y; worldTexts.Add(new WorldTextDraw{ x=sx,y=sy,text=prompt,size=13,color="#ffd700",align=1 }); }
    }
}
