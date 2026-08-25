using System;
using UnityEngine;

// ============ 实体角色绘制（移植自 renderObstacle / renderMonster / renderHero / renderWeather / renderFogOfWar / renderMinimap） ============
public partial class Expedition
{
    void RenderObstacle(Canvas2D c, Obstacle o, ECamera cam){
        float sx=o.x-cam.x, sy=o.y-cam.y; if(sx<-100||sx>1380||sy<-130||sy>820) return;
        float s=o.scale; float heightByType=o.type=="tree"?76f:o.type=="deadTree"?62f:o.type=="rock"?34f:o.type=="hay"?30f:o.type=="fence"?30f:o.type=="ruin"?60f:o.type=="monolith"?72f:58f;
        // 阴影
        float len=0.7f+Math.Min(heightByType,90)/100f; float dx=sunVector.x*heightByType*len*0.45f, dy=sunVector.y*heightByType*0.55f*0.45f;
        Color sh=new Color(0.008f,0.027f,0.024f,0.3f); c.SaveTransform(); c.Translate(sx+dx,sy+dy); c.Rotate((float)Math.Atan2(dy,dx)); c.globalAlpha=0.3f; c.FillEllipse(0,0,14*s,Math.Max(6,heightByType*0.34f*s),sh); c.globalAlpha=1; c.RestoreTransform();
        c.SaveTransform(); c.Translate(sx,sy); c.Rotate(o.rotation); c.Scale(s,s);
        if(o.type=="tree"){ c.FillRect(-7,-31,14,37,G.ParseColor("#4b2d18")); c.FillCircle(-12,-39,20,G.ParseColor("#2c5a32")); c.FillCircle(11,-43,23,G.ParseColor("#2c5a32")); c.FillCircle(0,-59,24,G.ParseColor("#2c5a32")); Color g=new Color(0.63f,0.83f,0.46f,0.32f); c.FillCircle(-7,-61,12,g); }
        else if(o.type=="deadTree"){ c.StrokeLine(0,3,0,-49,G.ParseColor("#4b3428"),9f); c.StrokeLine(0,-35,-19,-51,G.ParseColor("#4b3428"),7f); c.StrokeLine(1,-29,20,-44,G.ParseColor("#4b3428"),7f); c.StrokeLine(-2,0,-2,-46,G.ParseColor("#8a6750"),2f); }
        else if(o.type=="rock"){ float[] poly={-27,3,-21,-18,-7,-32,18,-25,28,-5,17,10,-12,12}; c.FillPoly(poly,G.ParseColor("#555960"),null,null); }
        else if(o.type=="hay"){ c.RoundRect(-27,-25,54,32,9,G.ParseColor("#e1b84c")); c.StrokeLine(-22,-19,22,-22,G.ParseColor("#f0d06c"),2f); c.StrokeLine(-22,-12,22,-15,G.ParseColor("#f0d06c"),2f); c.StrokeLine(-22,-5,22,-8,G.ParseColor("#f0d06c"),2f); c.StrokeLine(0,-24,0,7,G.ParseColor("#6e431e"),3f); }
        else if(o.type=="fence"){ c.FillRect(-34,-30,8,37,G.ParseColor("#61401f")); c.FillRect(26,-30,8,37,G.ParseColor("#61401f")); c.FillRect(-38,-22,76,8,G.ParseColor("#9b6a34")); c.FillRect(-38,-4,76,8,G.ParseColor("#9b6a34")); }
        else if(o.type=="ruin"){ c.FillRect(-29,-42,49,47,G.ParseColor("#5e4538")); float[] top={-29,-42,-19,-52,30,-52,20,-42}; c.FillPoly(top,G.ParseColor("#806251"),null,null); }
        else if(o.type=="monolith"){ float[] mp={-19,4,-15,-57,10,-67,22,-8}; c.FillPoly(mp,G.ParseColor("#29273d"),null,null); c.StrokeLine(-2,-45,5,-34,G.ParseColor("#b89cff"),2f); c.StrokeLine(5,-34,-4,-20,G.ParseColor("#b89cff"),2f); }
        else { bool toxic=o.type=="toxicCrystal"; Color base2=toxic?G.ParseColor("#4b8d3d"):G.ParseColor("#6653a0"); Color light=toxic?G.ParseColor("#b4ef67"):G.ParseColor("#c1a5ff");
            float[][] pts={ new[]{-16f,2f,-10f,-37f,0f,-5f}, new[]{0f,5f,7f,-52f,13f,-2f}, new[]{11f,5f,22f,-31f,25f,5f} }; foreach(var p in pts){ float[] t2={p[0],p[1],p[2],p[3],p[4],p[5]}; c.FillPoly(t2,base2,null,null); } c.StrokeLine(7,-48,7,-6,light,1.4f); }
        c.RestoreTransform();
    }

    void RenderMonster(Canvas2D c, Monster m, float sx, float sy, ECamera cam){ float t=Time.time; float scale=(m.elite?1.18f:1f)*(m.radius/18f);
        float stride=(float)Math.Sin(m.animTime); float hpPct=G.Clamp(m.hp/m.maxHp,0,1); float lift=m.visualZ;
        // 阴影
        Color sh=new Color(0,0,0,0.38f); c.SaveTransform(); c.Translate(sx+sunVector.x*14,sy+sunVector.y*9); c.globalAlpha=0.38f; c.FillEllipse(0,0,22*scale,10*scale,sh); c.globalAlpha=1; c.RestoreTransform();
        c.SaveTransform(); c.Translate(sx,sy-lift);
        if(m.state=="death"){ c.globalAlpha=G.Clamp(m.deathTimer/0.42f,0,1); float rot=(1-c.globalAlpha)*0.85f; c.Rotate(rot); c.Scale(1,0.65f+c.globalAlpha*0.35f); }
        if(m.type=="boss"){ // 巨型首领：大幅放大 + 专属配色
            c.Scale(scale*2.6f, scale*2.6f);
            // 用当前地图 tier 配置的轮廓绘制（大狼形态）+ 发光
            Color glow=map.tier==2?G.ParseColor("#54dcff"):G.ParseColor("#fff4cf"); Color fur=map.tier==2?G.ParseColor("#3a5a77"):G.ParseColor("#7a6a4a");
            c.FillEllipse(-2,0,30,22,fur); c.FillEllipse(23,0,16,15,fur); Color eye=glow; c.FillCircle(27,-6,3,eye); c.FillCircle(27,6,3,eye);
            c.globalAlpha=0.5f+(float)Math.Sin(m.animTime*4)*0.06f; c.StrokeCircle(0,0,40,glow,3f); c.globalAlpha=1;
        } else if(m.type=="boar"){ c.Rotate(m.facing); c.Scale(scale,scale); if(Math.Cos(m.facing)<0) c.Scale(-1,1);
            Color hide=G.ParseColor(m.hitFlash>0?"#fff0e7":"#9b5a3e"); c.FillEllipse(-2,0,30,20,hide); c.FillEllipse(23,0,15,14,G.ParseColor("#3b2a2a")); c.FillEllipse(35,0,8,7,G.ParseColor("#1d1518")); c.FillEllipse(27,-6,3,2,G.ParseColor("#f4b64b"));
        } else if(m.type=="bat"){ c.Rotate(m.facing); c.Scale(scale,scale); if(Math.Cos(m.facing)<0) c.Scale(-1,1); float flap=(float)Math.Sin(m.animTime*5)*0.25f; Color wing=G.ParseColor(m.hitFlash>0?"#fff4f1":"#31243f");
            c.SaveTransform(); c.Rotate(-(0.55f+flap)); c.FillEllipse(-20,0,18,10,wing); c.RestoreTransform(); c.SaveTransform(); c.Rotate(0.55f+flap); c.FillEllipse(-20,0,18,10,wing); c.RestoreTransform(); c.FillEllipse(4,0,16,13,G.ParseColor(m.hitFlash>0?"#ffd7d1":"#6f4a83"));
        } else if(m.type=="spider"){ c.Rotate(m.facing); c.Scale(scale,scale); if(Math.Cos(m.facing)<0) c.Scale(-1,1); c.StrokeLine(-4,5,-22,16,G.ParseColor("#3f342e"),4f); c.StrokeLine(-4,-5,-22,-16,G.ParseColor("#3f342e"),4f); c.FillEllipse(0,0,22,17,G.ParseColor("#58633a")); c.FillCircle(15,5,2.5f,G.ParseColor("#ef5b6d")); c.FillCircle(15,-5,2.5f,G.ParseColor("#ef5b6d"));
        } else if(m.type=="locust"){ c.Rotate(m.facing); c.Scale(scale,scale); if(Math.Cos(m.facing)<0) c.Scale(-1,1); c.globalAlpha=0.55f; c.FillEllipse(-13,8,27,8,G.ParseColor("#d8f2ba")); c.FillEllipse(-13,-8,27,8,G.ParseColor("#d8f2ba")); c.globalAlpha=1; c.FillEllipse(0,0,25,10,G.ParseColor("#5d7e35")); c.FillCircle(23,0,9,G.ParseColor("#31452c"));
        } else { c.Rotate(m.facing); c.Scale(scale,scale); if(Math.Cos(m.facing)<0) c.Scale(-1,1); Color fur=G.ParseColor(m.hitFlash>0?"#eef7ff":"#8794a3"); c.FillEllipse(-7,14,15,6,fur); c.FillEllipse(-7,-14,15,6,fur); c.FillEllipse(0,0,30,18,fur); c.FillEllipse(18,-8,14,10,G.ParseColor("#394554")); c.FillCircle(30,5,2.2f,G.ParseColor("#7ee5ff")); c.FillCircle(30,-5,2.2f,G.ParseColor("#7ee5ff")); }
        c.RestoreTransform();
        // 血条
        float barW=m.elite?54:44, barY=sy-m.radius*scale-24; c.FillRect(sx-barW/2-2,barY-2,barW+4,9,new Color(0.03f,0.04f,0.05f,0.82f)); Color hpc=hpPct>0.35f?G.ParseColor("#57c96b"):G.ParseColor("#db4b43"); c.FillRect(sx-barW/2,barY,barW*hpPct,5,hpc); if(m.elite) worldTexts.Add(new WorldTextDraw{ x=sx,y=barY-5,text="ELITE",size=9,color="#edc7ff",align=1 });
    }

    void RenderHero(Canvas2D c, float sx, float sy, ECamera cam, float t){
        float angle=player.angle; bool moving=KeyDown("w")||KeyDown("a")||KeyDown("s")||KeyDown("d"); float bob=moving?(float)Math.Sin(elapsed*12)*2f:(float)Math.Sin(elapsed*3)*0.8f; float lift=player.visualZ;
        // 阴影
        c.SaveTransform(); c.Translate(sx,sy+45-lift); c.globalAlpha=0.42f; c.FillEllipse(0,0,24,10,new Color(0,0,0,1)); c.globalAlpha=1; c.RestoreTransform();
        c.SaveTransform(); c.Translate(sx,sy+bob-lift);
        if(player.invuln>0){ c.StrokeCircle(0,20,34,new Color(0.49f,0.9f,1f,0.6f),3f); }
        c.Scale(0.6f,0.6f); if(Math.Cos(angle)<0) c.Scale(-1,1);
        // 背包
        c.RoundRect(-29,-10,30,52,8,G.ParseColor("#553625")); c.RoundRect(-32,-5,27,32,5,G.ParseColor("#9a6337")); c.StrokeLine(-28,4,-9,4,G.ParseColor("#d39a4c"),2f);
        // 靴子/裤子/腰带
        c.RoundRect(-25,48,22,25,7,G.ParseColor("#4a3026")); c.RoundRect(3,48,22,25,7,G.ParseColor("#4a3026")); c.RoundRect(-27,25,54,38,11,G.ParseColor("#777553")); c.FillRect(-30,20,60,8,G.ParseColor("#71502d"));
        // 衫
        c.RoundRect(-34,-26,68,55,17,G.ParseColor("#b7945e"));
        // 草编领
        float[] collar={-35,-27,-22,-39,0,-31,22,-39,36,-25,26,-14,-25,-14}; c.FillPoly(collar,G.ParseColor("#d8b85d"),null,null);
        // 手臂/手
        c.StrokeLine(-28,-8,-39,23,G.ParseColor("#d98f6f"),13f); c.StrokeLine(27,-7,39,19,G.ParseColor("#d98f6f"),13f); c.FillCircle(-40,20,7,G.ParseColor("#e6a07e")); c.FillCircle(40,15,7,G.ParseColor("#e6a07e"));
        // 头部/胡子/帽
        c.FillCircle(0,-47,30,G.ParseColor("#c84e2f")); c.FillEllipse(0,-51,23,21,G.ParseColor("#efb092")); c.FillEllipse(0,-57,6,8,G.ParseColor("#fff8de")); c.FillEllipse(7,-57,6,8,G.ParseColor("#fff8de")); c.FillCircle(6,-57,2.2f,G.ParseColor("#2a211d")); c.FillCircle(-6,-57,2.2f,G.ParseColor("#2a211d")); c.StrokeLine(0,-34,12,-36,G.ParseColor("#7c251c"),4f); c.FillCircle(0,-70,8,G.ParseColor("#505654")); c.RoundRect(-24,-92,48,23,9,G.ParseColor("#505654"));
        RenderHeroWeapon(c,angle,0);
        c.RestoreTransform();
    }
    void RenderHeroWeapon(Canvas2D c, float angle, float swing){ float localAngle=(float)Math.Atan2(Math.Sin(angle),Math.Abs(Math.Cos(angle)))+swing*0.8f; c.SaveTransform(); c.Translate(28,6); c.Rotate(localAngle*0.45f-0.25f);
        if(weapon.id=="harvest_sickle"){ c.StrokeLine(-5,17,30,-31,G.ParseColor("#684328"),6f); c.StrokeCircle(25,-22,20,G.ParseColor("#cfd6d3"),7f); }
        else if(weapon.id=="pea_repeater"){ c.RoundRect(-3,-12,41,18,6,G.ParseColor("#496b32")); c.FillCircle(35,-3,9,G.ParseColor("#82bd48")); }
        else { c.StrokeLine(0,17,34,-30,G.ParseColor("#67462d"),6f); c.StrokeCircle(31,-33,13,G.ParseColor("#4f9b64"),3f); c.FillCircle(31,-33,6,G.ParseColor("#86f0c9")); }
        c.RestoreTransform();
    }

    void RenderWeather(Canvas2D c, ECamera cam){ float t=Time.time; int tier=map.tier;
        if(tier==1){ for(int i=0;i<26;i++){ float x=(i*83+t*(9+i%4))%1280, y=(i*137+(float)Math.Sin(t+i)*35+720)%720; c.globalAlpha=0.18f+((float)Math.Sin(t*2+i)+1)*0.12f; c.FillCircle(x,y,1.7f,G.ParseColor("#d8ff8a")); c.globalAlpha=1; } }
        else if(tier==2){ for(int i=0;i<32;i++){ float x=(i*71+t*90)%(1400)-60, y=(i*109+t*18)%720; c.globalAlpha=0.12f+(i%3)*0.04f; c.StrokeLine(x,y,x+28,y+5,G.ParseColor("#d5b67a"),2f); c.globalAlpha=1; } }
        else if(tier==3){ for(int i=0;i<12;i++){ float x=(i*131+(float)Math.Sin(t*0.3f+i)*90+1280)%1280, y=(i*79+t*13)%760-40; float radius=55+(i%4)*24; Color fogG=new Color(0.455f,0.678f,0.29f,0.1f); c.FillRadialGrad(x,y,0,radius,new[]{fogG,new Color(0.27f,0.41f,0.2f,0)},new[]{0f,1f}); } }
        else { for(int i=0;i<38;i++){ float x=(i*97+t*44)%1280, y=(i*61+t*145)%720-30; c.globalAlpha=0.12f+(i%5)*0.025f; c.StrokeLine(x,y,x-9,y+24,G.ParseColor("#b9a2ff"),1.4f); c.globalAlpha=1; } }
    }

    public void RenderFogOfWar(Canvas2D fogCanvas, ECamera cam){ float radius=visionRadius*eventModifiers.vision; float px=player.x-cam.x, py=player.y-cam.y;
        fogCanvas.Clear(); fogCanvas.composite=0; fogCanvas.globalAlpha=1; Color dark=new Color(0.039f,0.063f,0.094f,0.78f); fogCanvas.FillRect(0,0,1280,720,dark);
        fogCanvas.composite=1; fogCanvas.FillRadialGrad(px,py,radius*0.82f,radius*1.08f,new[]{new Color(0,0,0,1),new Color(0,0,0,1),new Color(0,0,0,0)},new[]{0,0.78f,1}); fogCanvas.composite=0; }

    public void RenderMinimap(Canvas2D mm, ECamera cam){ float size=2400, scale=160/size; mm.Clear(); mm.FillRect(0,0,160,160,new Color(0,0,0,0.8f));
        foreach(var p in terrainPatches){ if(p.type=="water") mm.FillEllipse(p.x*scale,p.y*scale,Math.Max(1,p.rx*scale),Math.Max(1,p.ry*scale),G.ParseColor(p.color),p.rotation); }
        foreach(var r in terrainRoads){ mm.StrokeLine(r.x1*scale,r.y1*scale,r.x2*scale,r.y2*scale,G.ParseColor(map.terrainPath),2f); }
        foreach(var ep in extractPoints){ if(IsWorldVisible(ep.x,ep.y)) mm.FillCircle(ep.x*scale,ep.y*scale,4,G.ParseColor("#7fff7f")); }
        foreach(var c in chests){ if(!c.opened && IsWorldVisible(c.x,c.y)) mm.FillRect(c.x*scale-2,c.y*scale-2,4,4,G.ParseColor("#ffd700")); }
        foreach(var m in monsters){ if(IsWorldVisible(m.x,m.y)) mm.FillRect(m.x*scale-1,m.y*scale-1,3,3,G.ParseColor("#ff4444")); }
        foreach(var r in raiders){ if(IsWorldVisible(r.x,r.y)) mm.FillRect(r.x*scale-2,r.y*scale-2,4,4,G.ParseColor("#ff8800")); }
        foreach(var tw in towers){ if(IsWorldVisible(tw.x,tw.y)){ Color col=tw.state=="player"?G.ParseColor("#7fff7f"):tw.state=="enemy"?G.ParseColor("#ff4444"):G.ParseColor("#666666"); mm.FillRect(tw.x*scale-2,tw.y*scale-2,4,4,col); } }
        mm.FillCircle(player.x*scale,player.y*scale,4,G.ParseColor("#4488ff"));
        mm.StrokeRect(cam.x*scale,cam.y*scale,1280*scale,720*scale,new Color(1,1,1,0.3f),1f);
    }
}
