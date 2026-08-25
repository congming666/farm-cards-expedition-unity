using System;
using System.Collections.Generic;

// ============ 远征特效（移植自 expedition.js spawn* 系列） ============
public partial class Expedition
{
    public void SpawnAoeEffect(float x,float y,float size,string color,string type){ particles.Add(new Particle{ x=x,y=y,vx=0,vy=0,life=0.5f,maxLife=0.5f,color=color,size=size,type="aoe" }); }
    public void SpawnSlashEffect(float x,float y,float angle,string color,float size){ particles.Add(new Particle{ x=x,y=y,vx=0,vy=0,life=0.2f,maxLife=0.2f,color=color,size=size,angle=angle,type="slash" }); }
    public void SpawnMuzzleEffect(float x,float y,float angle,string color){ for(int i=0;i<6;i++){ float spread=angle+G.Rand(-0.32f,0.32f); particles.Add(new Particle{ x=x+(float)Math.Cos(angle)*22,y=y+(float)Math.Sin(angle)*22,vx=(float)Math.Cos(spread)*G.Rand(65,150),vy=(float)Math.Sin(spread)*G.Rand(65,150),life=0.22f,maxLife=0.22f,color=color,size=G.Rand(2,5),type="spark" }); } }
    public void SpawnRadialBurst(float x,float y,string color,int count){ for(int i=0;i<count;i++){ float angle=(float)(Math.PI*2*i/count)+G.Rand(-0.1f,0.1f); particles.Add(new Particle{ x=x,y=y,vx=(float)Math.Cos(angle)*G.Rand(100,230),vy=(float)Math.Sin(angle)*G.Rand(100,230),life=0.48f,maxLife=0.48f,color=color,size=G.Rand(3,7),type="chaff" }); } }
    public void SpawnHitParticles(float x,float y,string color){ for(int i=0;i<11;i++){ particles.Add(new Particle{ x=x,y=y,vx=G.Rand(-175,175),vy=G.Rand(-175,175),life=0.46f,maxLife=0.46f,color=color,size=G.Rand(2,6) }); } }
    public void SpawnVineEffect(float x,float y,float radius){ for(int i=0;i<9;i++){ float angle=(float)(Math.PI*2*i/9); particles.Add(new Particle{ x=x,y=y,vx=0,vy=0,life=0.75f,maxLife=0.75f,color=(i%2==1)?"#89db67":"#3f9d56",size=radius*G.Rand(0.62f,1),angle=angle,type="vine" }); } }
    public void SpawnDashTrail(float x,float y,float angle,string color){ for(int i=0;i<7;i++){ particles.Add(new Particle{ x=x-(float)Math.Cos(angle)*i*22,y=y-(float)Math.Sin(angle)*i*22,vx=0,vy=0,life=0.38f-i*0.025f,maxLife=0.38f,color=color,size=18-i,angle=angle,type="earthTrail" }); } }
    public void SpawnSmokeEffect(float x,float y){ for(int i=0;i<18;i++){ float angle=G.Rand(0f,(float)(Math.PI*2)), speed=G.Rand(22,85); particles.Add(new Particle{x=x+G.Rand(-20,20),y=y+G.Rand(-20,20),vx=(float)Math.Cos(angle)*speed,vy=(float)Math.Sin(angle)*speed,life=G.Rand(0.7f,1.15f),maxLife=1.15f,color=(i%3==0)?"#b5c3ba":"#808c88",size=G.Rand(12,28),type="smoke" }); } }
    public void SpawnDashParticles(float x,float y,float angle){ for(int i=0;i<8;i++){ particles.Add(new Particle{x=x-(float)Math.Cos(angle)*i*15,y=y-(float)Math.Sin(angle)*i*15,vx=G.Rand(-30,30),vy=G.Rand(-30,30),life=0.3f,maxLife=0.3f,color="#88ccff",size=G.Rand(3,6) }); } }
    public void SpawnWeaponSwitchEffect(){ string[] colors={"#f2c45b","#75dc68","#7be5c4"}; for(int ring=0;ring<3;ring++){ particles.Add(new Particle{x=player.x,y=player.y,vx=0,vy=0,life=0.38f+ring*0.08f,maxLife=0.38f+ring*0.08f,color=colors[ring],size=34+ring*10,type="weaponRing" }); } }
}
