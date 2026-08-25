using System;
using System.Collections.Generic;
using UnityEngine;

// ============ UI 外观层（OnGUI）：主菜单/农场/准备/远征HUD/结算/工坊/提示/掉落 ============
public static class UIHost
{
    public static bool workshopOpen;
    static GUIStyle title, subtitle, btn, small, label, panelTitle;
    static Texture2D white, farmBackdrop, menuBackdrop, buttonTex, buttonHoverTex, buttonActiveTex, boxTex;
    static bool init;
    static Font cjkFont;

    static void Init(){
        if(init) return; init=true;
        cjkFont = null;
        try { cjkFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 20); } catch {}
        if(cjkFont==null) { try { cjkFont = Font.CreateDynamicFontFromOSFont("SimHei", 16); } catch {} }
        if(cjkFont==null) cjkFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GUI.skin.font = cjkFont;
        white = new Texture2D(1,1); white.SetPixel(0,0,Color.white); white.Apply();
        farmBackdrop=MakeBackdrop(new Color(0.08f,0.20f,0.14f),new Color(0.29f,0.27f,0.12f),true);
        menuBackdrop=MakeBackdrop(new Color(0.035f,0.10f,0.10f),new Color(0.06f,0.08f,0.12f),false);
        buttonTex=SolidTex(new Color(0.12f,0.25f,0.18f,0.98f));
        buttonHoverTex=SolidTex(new Color(0.18f,0.40f,0.25f,1));
        buttonActiveTex=SolidTex(new Color(0.70f,0.39f,0.09f,1));
        boxTex=SolidTex(new Color(0.035f,0.09f,0.065f,0.92f));
        GUI.skin.button.normal.background=buttonTex; GUI.skin.button.hover.background=buttonHoverTex; GUI.skin.button.active.background=buttonActiveTex;
        GUI.skin.button.normal.textColor=Color.white; GUI.skin.button.hover.textColor=Color.white; GUI.skin.button.active.textColor=Color.white;
        GUI.skin.box.normal.background=boxTex; GUI.skin.box.normal.textColor=Color.white;
        title=new GUIStyle(GUI.skin.label){ fontSize=44, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter }; title.normal.textColor=G.ParseColor("#7fff7f");
        subtitle=new GUIStyle(GUI.skin.label){ fontSize=18, alignment=TextAnchor.MiddleCenter }; subtitle.normal.textColor=G.ParseColor("#aaccaa");
        btn=new GUIStyle(GUI.skin.button){ fontSize=18, fixedHeight=46 }; btn.normal.textColor=Color.white;
        small=new GUIStyle(GUI.skin.label){ fontSize=12 }; small.normal.textColor=G.ParseColor("#aeb8ae");
        label=new GUIStyle(GUI.skin.label){ fontSize=15 }; label.normal.textColor=Color.white;
        panelTitle=new GUIStyle(GUI.skin.label){ fontSize=16, fontStyle=FontStyle.Bold }; panelTitle.normal.textColor=G.ParseColor("#f0d58f");
    }
    static GUIStyle Col(GUIStyle baseStyle, Color c){ var s=new GUIStyle(baseStyle); s.normal.textColor=c; return s; }
    static GUIStyle Col2(Color c, int size, TextAnchor align){ var s=new GUIStyle(GUI.skin.label){fontSize=size,alignment=align}; s.normal.textColor=c; return s; }
    static GUIStyle Btn(Color c, int size){ var s=new GUIStyle(GUI.skin.button){fontSize=size}; s.normal.textColor=c; return s; }

    static Texture2D SolidTex(Color c){ var t=new Texture2D(2,2); t.SetPixels(new[]{c,c,c,c}); t.Apply(); return t; }
    static Texture2D MakeBackdrop(Color top,Color bottom,bool fields){
        const int w=320,h=180; var t=new Texture2D(w,h,TextureFormat.RGBA32,false); var px=new Color[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++){
            float v=y/(float)(h-1); Color c=Color.Lerp(bottom,top,v);
            float glow=Mathf.Max(0,1-Mathf.Abs(x-w*0.64f)/(w*0.72f))*(0.035f+0.025f*Mathf.Sin(x*0.11f+y*0.07f));
            if(fields && y<72){ float row=(y/12)%2==0?0.018f:-0.01f; c+=new Color(row*0.7f,row,row*0.4f,0); }
            c+=new Color(glow*0.45f,glow,glow*0.55f,0); c.a=1; px[y*w+x]=c;
        }
        t.SetPixels(px); t.Apply(); t.filterMode=FilterMode.Bilinear; t.wrapMode=TextureWrapMode.Clamp; return t;
    }
    static void Fill(Rect r,Color c){ GUI.color=c; GUI.DrawTexture(r,white); GUI.color=Color.white; }
    static void Outline(Rect r,Color c,float n=2){ Fill(new Rect(r.x,r.y,r.width,n),c); Fill(new Rect(r.x,r.yMax-n,r.width,n),c); Fill(new Rect(r.x,r.y,n,r.height),c); Fill(new Rect(r.xMax-n,r.y,n,r.height),c); }
    static string CropGlyph(string id){ return id=="pea_shooter"?"豌":id=="sunflower"?"葵":id=="watermelon"?"瓜":id=="cabbage"?"菜":id=="wheat"?"麦":id=="carrot"?"萝":id=="corn"?"玉":id=="pumpkin"?"南":"月"; }
    static string SkillGlyph(string id){ return id=="straw_smash"?"稻":id=="vine_bind"?"藤":id=="earth_dash"?"遁":"烟"; }
    static string ConsumableGlyph(string id){ return id=="herb_kit"?"药":id=="thorn_storm"?"棘":"信"; }
    static string ItemText(GroundLoot item){ return item==null?"":item.name; }

    static void DrawBar(float x,float y,float w,float h,float pct,Color fill,Color bg){ GUI.color=bg; GUI.DrawTexture(new Rect(x,y,w,h),white); GUI.color=fill; GUI.DrawTexture(new Rect(x,y,w*pct,h),white); GUI.color=Color.white; }

    public static void DrawUI(GameFlow gf){
        Init(); GUI.matrix=Matrix4x4.identity; GUI.color=Color.white;
        var r = gf.backend.viewRect.width>0 ? gf.backend.viewRect : new Rect(0,0,Screen.width,Screen.height);
        GUI.matrix = Matrix4x4.TRS(new Vector3(r.x,r.y,0),Quaternion.identity,new Vector3(r.width/1280f,r.height/720f,1));
        if(gf.screen!="expedition"){
            GUI.DrawTexture(new Rect(0,0,1280,720),gf.screen=="farm"?farmBackdrop:menuBackdrop,ScaleMode.StretchToFill);
            Fill(new Rect(0,0,1280,720),gf.screen=="farm"?new Color(0.02f,0.08f,0.045f,0.16f):new Color(0.01f,0.025f,0.035f,0.2f));
        }
        switch(gf.screen){
            case "menu": DrawMenu(gf); break;
            case "farm": DrawFarm(gf); break;
            case "prep": DrawPrep(gf); break;
            case "expedition": DrawHUD(gf); break;
            case "result": DrawResult(gf); break;
        }
        if(workshopOpen) DrawWorkshop(gf);
        DrawToasts(gf); DrawDropBanners(gf);
        if(gf.signalFlash>0){ GUI.color=new Color(1f,0.34f,0.24f,gf.signalFlash*0.72f); GUI.DrawTexture(new Rect(0,0,1280,720),white); GUI.color=Color.white; }
        GUI.matrix=Matrix4x4.identity;
    }

    static void DrawMenu(GameFlow gf){
        GUI.Label(new Rect(0,120,1280,90),"农庄牌",title);
        GUI.Label(new Rect(0,210,1280,40),"荒 野 远 征",subtitle);
        if(GUI.Button(new Rect(500,320,280,50),"开始游戏",btn)){ gf.StartGame(); }
        if(GUI.Button(new Rect(500,380,280,50),"游戏说明",btn)){ gf.AddToast("WASD移动，左键攻击/交互，1-4技能，Q/R/E消耗品","success"); }
        GUI.Label(new Rect(0,690,1280,20),"v1.8 · 农场卡牌 · 荒野远征",small);
    }

    static void DrawFarm(GameFlow gf){
        Fill(new Rect(0,0,1280,64),new Color(0.025f,0.105f,0.075f,0.94f));
        GUI.Label(new Rect(34,14,400,34),"我的家园农场",Col2(Color.white,25,TextAnchor.MiddleLeft));
        DrawResourceBadge(610,13,120,"金币",GameState.gold,G.ParseColor("#f5c84c"));
        DrawResourceBadge(740,13,110,"种子",GameState.seeds,G.ParseColor("#78e48c"));
        DrawResourceBadge(860,13,110,"材料",GameState.materials,G.ParseColor("#ee9540"));
        if(GUI.Button(new Rect(1132,13,120,38),"返回菜单",btn)){ gf.BackToMenu(); }

        Fill(new Rect(26,80,704,612),new Color(0.045f,0.11f,0.072f,0.90f)); Outline(new Rect(26,80,704,612),new Color(0.31f,0.58f,0.37f,0.55f),1);
        int cs=84; float gx=54, gy=142;
        GUI.Label(new Rect(gx,98,620,30),"农田 "+GameState.unlockedPlots+"/36    点击空地播种 · 点击成熟作物收获",Col2(G.ParseColor("#efd78d"),16,TextAnchor.MiddleLeft));
        for(int i=0;i<36;i++){ int row=i/6, col=i%6; var plot=GameState.farmPlots[i];
            float x=gx+col*cs, y=gy+row*cs;
            Rect cell=new Rect(x,y,cs-7,cs-7);
            if(i>=GameState.unlockedPlots){ bool next=i==GameState.unlockedPlots; var cost=FarmSystem.GetUnlockCost(i);
                Fill(cell,next?new Color(0.25f,0.21f,0.12f,0.95f):new Color(0.11f,0.13f,0.105f,0.90f)); Outline(cell,next?G.ParseColor("#b79245"):new Color(0.35f,0.38f,0.32f,0.6f),1);
                if(GUI.Button(cell,next?("锁定\n"+cost.gold+" 金"+(cost.materials>0?("\n"+cost.materials+" 材料"):"")):"锁定",new GUIStyle(GUI.skin.button){fontSize=11,alignment=TextAnchor.MiddleCenter})){ if(next) FarmSystem.UnlockPlot(i); } }
            else if(plot.crop!=null){
                float progress=CropProgress(plot);
                Fill(cell,new Color(0.19f,0.115f,0.045f,0.98f)); Outline(cell,progress>=1?G.ParseColor("#e7c946"):new Color(0.38f,0.65f,0.29f,0.8f),2);
                bool hasStatus=!string.IsNullOrEmpty(plot.status);
                string state=hasStatus?("\n"+StatusIcon(plot.status)):progress>=1?"\n可收获":"";
                if(GUI.Button(cell,CropGlyph(plot.crop.id)+state,new GUIStyle(GUI.skin.button){fontSize=progress>=1?19:24,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter})){ if(hasStatus) FarmSystem.Tend(i); else if(progress>=1) FarmSystem.Harvest(i); }
                DrawBar(x+5,y+cs-16,cs-17,6,progress,G.ParseColor("#70ef72"),new Color(0.12f,0.12f,0.09f,1));
            } else {
                Fill(cell,new Color(0.20f,0.105f,0.035f,0.96f)); Outline(cell,new Color(0.54f,0.32f,0.12f,0.9f),1);
                if(GUI.Button(cell,"播种",new GUIStyle(GUI.skin.button){fontSize=13,alignment=TextAnchor.MiddleCenter})){ FarmSystem.Plant(i); }
            }
        }
        float sx=760, sy=502;
        DrawPanel(sx,sy,492,174,"选择作物");
        float cx=sx+12;
        foreach(var crop in GameData.Crops){ if(!GameState.unlockedCrops.Contains(crop.id)) continue; bool sel=GameState.selectedCrop==crop.id;
            GUIStyle cropStyle=new GUIStyle(GUI.skin.button){fontSize=12,alignment=TextAnchor.MiddleCenter}; cropStyle.normal.textColor=sel?G.ParseColor("#ffe27a"):Color.white;
            if(GUI.Button(new Rect(cx,sy+42,88,60),CropGlyph(crop.id)+"\n"+crop.name,cropStyle)){ GameState.selectedCrop=crop.id; SaveSystem.Save(); }
            cx+=94;
        }
        float px=760;
        DrawPanel(px,80,492,126,"家园设施");
        DrawFacility(px+12,120,148,72,"工","卡牌工坊",()=>gf.OpenWorkshop());
        DrawFacility(px+172,120,148,72,"仓","物资仓库",()=>gf.AddToast("仓库会保存远征带回的物资","success"));
        DrawFacility(px+332,120,148,72,"档","远征档案",()=>gf.AddToast("T1–T4 情报已登记","gold"));
        float sy2=218; DrawPanel(px,sy2,492,132,"家园补给站");
        GUI.Label(new Rect(px+12,sy2+34,160,24),"生长催化剂 ×"+GameState.farmItems["growth_catalyst"],small);
        if(GUI.Button(new Rect(px+174,sy2+30,122,32),"使用催化剂")){ gf.UseCatalyst(); }
        int day=RewardSystem.GetNextDailyDay(); bool claimed=RewardSystem.DailyClaimedToday();
        GUI.Label(new Rect(px+12,sy2+72,150,24),claimed?("已领 第"+RewardSystem.TodayDay()+"天"):("每日 第"+day+"天"),small);
        if(GUI.Button(new Rect(px+150,sy2+70,100,30),claimed?"今日已领":"领取奖励",new GUIStyle(GUI.skin.button){fontSize=13})){ gf.ClaimDaily(); }
        GUI.Label(new Rect(px+300,sy2+72,150,24),RewardSystem.IsReliefEligible()?"可领保障":"保障暂不可领",small);
        if(GUI.Button(new Rect(px+382,sy2+70,98,30),"领取保障",new GUIStyle(GUI.skin.button){fontSize=13})){ gf.ClaimRelief(); }
        DrawPanel(px,364,492,124,"荒野远征站");
        GUI.Label(new Rect(px+14,400,456,32),"选择地图、技能与携带物进入荒野，获取战利品后撤离。",small);
        if(GUI.Button(new Rect(px+14,438,464,38),"进入远征准备大厅  →",new GUIStyle(GUI.skin.button){fontSize=17,fontStyle=FontStyle.Bold})){ gf.OpenPrep(); }
    }

    static float CropProgress(Plot p){ if(p.crop==null) return 0; double now=DateTime.Now.Ticks/(double)TimeSpan.TicksPerMillisecond; double elapsed=(now-p.plantedAt)/1000.0; float f=p.status=="drought"?0.55f:p.status=="pest"?0.72f:p.status=="weeds"?0.82f:1f; double pr=(elapsed*f)/p.crop.growTime; return (float)Math.Min(1,Math.Max(0,pr)); }
    static string StatusIcon(string s){ return s=="drought"?"缺水":s=="pest"?"虫害":"杂草"; }
    static void DrawResourceBadge(float x,float y,float w,string name,int value,Color c){ Fill(new Rect(x,y,w,38),new Color(0.04f,0.12f,0.085f,0.96f)); Outline(new Rect(x,y,w,38),new Color(c.r,c.g,c.b,0.52f),1); GUI.Label(new Rect(x+8,y+6,w-16,26),name+"  "+value,Col2(c,15,TextAnchor.MiddleCenter)); }
    static void DrawFacility(float x,float y,float w,float h,string icon,string name,Action onClick){ DrawPanel(x,y,w,h,""); GUI.Label(new Rect(x,y+4,w,30),icon,Col2(Color.white,26,TextAnchor.MiddleCenter)); GUI.Label(new Rect(x,y+38,w,22),name,Col2(Color.white,13,TextAnchor.MiddleCenter)); if(GUI.Button(new Rect(x,y,w,h),"",new GUIStyle())) onClick(); }
    static void DrawPanel(float x,float y,float w,float h,string t){ GUI.color=new Color(0,0,0,0.5f); GUI.DrawTexture(new Rect(x,y,w,h),white); GUI.color=Color.white; if(!string.IsNullOrEmpty(t)){ GUI.Label(new Rect(x+12,y+8,w-24,26),t,panelTitle); } }

    static void DrawPrep(GameFlow gf){
        Fill(new Rect(0,0,1280,64),new Color(0.025f,0.105f,0.075f,0.94f));
        GUI.Label(new Rect(34,14,470,36),"荒野远征准备大厅",Col2(Color.white,26,TextAnchor.MiddleLeft));
        DrawResourceBadge(980,13,120,"金币",GameState.gold,G.ParseColor("#f5c84c"));
        if(GUI.Button(new Rect(1132,13,120,38),"返回家园")){ gf.ClosePrep(); }
        float x=28; DrawPanel(x,80,374,286,"选择远征区域");
        float yy=104; var selMap=SaveSystem.MapById(GameState.selectedMap)??GameData.Maps[0];
        foreach(var mp in GameData.Maps){ bool locked=GameState.gold<mp.entryFee; bool s=mp.id==GameState.selectedMap;
            GUIStyle st=new GUIStyle(GUI.skin.button){fontSize=14,alignment=TextAnchor.MiddleLeft}; st.normal.textColor=locked?new Color(0.5f,0.5f,0.5f):(s?G.ParseColor("#7fff7f"):Color.white);
            if(GUI.Button(new Rect(x+10,yy,354,54),"T"+mp.tier+"  "+mp.name+"  ["+mp.danger+"]\n入场 "+mp.entryFee+" 金 · 怪物 "+mp.monsterCount+" · 宝箱 "+mp.chestCount,st)){ if(!locked){ GameState.selectedMap=mp.id; SaveSystem.Save(); } } yy+=60; }
        DrawPanel(28,378,374,314,"本次常驻技能");
        var boosts=CardSystem.GetSelectedBoosts(); float sy=410;
        foreach(var sk in GameData.Skills){ var stats=SkillMath.GetStats(sk,boosts.ContainsKey(sk.id)?boosts[sk.id]:0); string power=stats.damage>0?("伤害 "+stats.damage):stats.stunDuration>0?("控制 "+stats.stunDuration+"s"):stats.dashDistance>0?("位移 "+stats.dashDistance):("隐身 "+stats.stealthDuration+"s");
            GUI.Label(new Rect(42,sy,340,40),SkillGlyph(sk.id)+"  "+sk.name+" · Lv."+stats.level+(stats.extraLevels>0?(" (+"+stats.extraLevels+")"):""),Col2(Color.white,15,TextAnchor.MiddleLeft)); GUI.Label(new Rect(42,sy+20,340,18),power+" · 能量 "+stats.energyCost+" · CD "+stats.cooldown+"s",small); sy+=46; }
        float px=420; DrawPanel(px,80,410,138,"携带消耗品");
        float cxx=px+8;
        foreach(var item in GameData.Consumables){ int cnt=GameState.loadout.ContainsKey(item.id)?GameState.loadout[item.id]:0;
            string txt=ConsumableGlyph(item.id)+"  "+item.name; GUIStyle st=new GUIStyle(GUI.skin.button){fontSize=13,alignment=TextAnchor.MiddleCenter}; st.normal.textColor=cnt>0?G.ParseColor("#7fff7f"):Color.white;
            if(GUI.Button(new Rect(cxx,104,124,64),txt+"\n×"+cnt+(cnt>0?"\n(点击卸下)":""),st)){ if(cnt>0) GameState.loadout[item.id]=cnt-1; else GameState.loadout[item.id]=Math.Min(5,cnt+1); SaveSystem.Save(); }
            cxx+=132;
        }
        DrawPanel(px,230,410,220,"携带强化卡  "+GameState.selectedBoostCards.Count+"/3");
        GUI.Label(new Rect(px+12,250,376,20),GameState.cardInventory.Count==0?"卡牌工坊暂无强化卡。收获作物后再来配置。":("拥有 "+GameState.cardInventory.Count+" 张强化卡，点击装备/卸下"),small);
        float bx=px+8; int bcnt=0;
        foreach(var card in GameState.cardInventory){ if(bcnt>=3) break; bool s=GameState.selectedBoostCards.Contains(card.id); var sk=SaveSystem.SkillById(card.skillId);
            GUIStyle st=new GUIStyle(GUI.skin.button){fontSize=12,alignment=TextAnchor.MiddleCenter,wordWrap=true}; st.normal.textColor=s?G.ParseColor("#83f2b2"):Color.white;
            if(GUI.Button(new Rect(bx,276,124,72),SkillGlyph(sk.id)+" "+sk.name+" +"+card.power+"\n"+(s?"[已携带]":"点击携带")+"\n"+card.rarity+"卡",st)){ CardSystem.ToggleBoost(card.id); } bx+=132; bcnt++;
        }
        DrawPanel(px,462,410,118,"区域提示");
        GUI.Label(new Rect(px+12,480,376,80),"T"+selMap.tier+" · "+selMap.name+"\n区域有陷阱、水域/泥地减速与持续伤害区域。\n环境威胁："+(3+selMap.tier*2)+"–"+(5+selMap.tier*3)+" 个陷阱",small);
        if(GUI.Button(new Rect(px,596,410,56),"确认配置并出发",new GUIStyle(GUI.skin.button){fontSize=20,fontStyle=FontStyle.Bold})){ gf.StartExpedition(); }
    }

    static void DrawHUD(GameFlow gf){ var e=gf.current; if(e==null) return;
        DrawPanel(12,10,220,66,"");
        DrawBar(20,40,180,14,G.Clamp(e.player.hp/e.player.maxHp,0,1),G.ParseColor("#ff6666"),new Color(0.13f,0.13f,0.13f,1)); GUI.Label(new Rect(20,40,180,14),"生命 "+Mathf.CeilToInt(e.player.hp)+"/"+Mathf.CeilToInt(e.player.maxHp),Col2(Color.white,12,TextAnchor.MiddleCenter));
        DrawBar(20,58,180,12,G.Clamp(e.player.energy/e.player.maxEnergy,0,1),G.ParseColor("#66aaff"),new Color(0.13f,0.13f,0.13f,1)); GUI.Label(new Rect(20,58,180,12),"能量 "+Mathf.CeilToInt(e.player.energy)+"/"+Mathf.CeilToInt(e.player.maxEnergy),Col2(Color.white,11,TextAnchor.MiddleCenter));
        int mins=(int)Math.Floor(e.timeLeft/60), secs=(int)Math.Floor(e.timeLeft%60); GUI.Label(new Rect(540,10,200,40),mins+":"+secs.ToString("00"),Col2(Color.white,30,TextAnchor.MiddleCenter)); GUI.Label(new Rect(540,52,200,20),"T"+e.map.tier+" · "+e.map.name+" · "+e.map.danger,small);
        GUI.Label(new Rect(320,10,220,30),e.weapon.icon+" "+e.weapon.name+"  [Tab]",Col2(Color.white,17,TextAnchor.MiddleLeft));
        for(int i=0;i<4;i++){ var sk=SkillMath.GetStats(GameData.Skills[i],e.skillBoosts.ContainsKey(GameData.Skills[i].id)?e.skillBoosts[GameData.Skills[i].id]:0); float cd=e.skillCooldowns[i]; float x=320+i*60, y=640;
            GUIStyle st=new GUIStyle(GUI.skin.box){fontSize=16,alignment=TextAnchor.MiddleCenter}; st.normal.textColor=cd<=0?Color.white:new Color(0.4f,0.4f,0.4f,1);
            GUI.Box(new Rect(x,y,52,52),sk.def.icon+"\n"+sk.def.key+(cd>0?("\n"+cd.ToString("F0")):""),st); }
        int qi=0; foreach(var item in GameData.Consumables){ int cnt=e.consumables.ContainsKey(item.id)?e.consumables[item.id]:0; float x=560+qi*60, y=640; GUIStyle st=new GUIStyle(GUI.skin.box){fontSize=14,alignment=TextAnchor.MiddleCenter}; st.normal.textColor=cnt>0?G.ParseColor("#ffd700"):new Color(0.4f,0.4f,0.4f,1); GUI.Box(new Rect(x,y,52,52),item.icon+"\n"+item.key+" ×"+cnt,st); qi++; }
        float gg=0; int ss=0; foreach(var i in e.bag){ if(i.type=="gold")gg+=i.amount; if(i.type=="seed")ss+=(int)i.amount; }
        DrawPanel(1120,10,140,72,""); GUI.Label(new Rect(1128,18,124,22),"金币 "+gg,label); GUI.Label(new Rect(1128,42,124,22),"种子 "+ss,label);
        DrawPanel(12,90,230,96,""); if(e.objective!=null){ GUI.Label(new Rect(20,96,214,20),e.objective.title+(e.objective.complete?" · 已完成":""),Col2(Color.white,14,TextAnchor.MiddleLeft)); GUI.Label(new Rect(20,118,214,18),e.objective.progress+"/"+e.objective.target,small); }
        GUI.Label(new Rect(20,150,214,20),e.beastWave.active?("⚠ 第 "+e.beastWave.wave+" 波兽潮 剩余 "+e.beastWave.remaining):("兽潮预警 "+Mathf.CeilToInt(e.beastWave.nextIn)+"s"),Col2(e.beastWave.active?G.ParseColor("#ff6644"):G.ParseColor("#f2d078"),12,TextAnchor.MiddleLeft));
        GUI.Label(new Rect(20,174,214,16),e.activeEvent!=null?("事件："+e.activeEvent.name+" · "+Mathf.CeilToInt(e.activeEvent.timeLeft)+"s"):"区域平静",small);
        if(e.paused){
            GUI.color=new Color(0,0,0,0.72f); GUI.DrawTexture(new Rect(0,0,1280,720),white); GUI.color=Color.white;
            GUI.Label(new Rect(0,270,1280,70),"游戏已暂停",TitleCol(Color.white));
            GUI.Label(new Rect(0,342,1280,36),"按 Esc 继续游戏",subtitle);
        }
    }

    static void DrawResult(GameFlow gf){ var rd=gf.PeekResult(); if(rd==null) return;
        GUI.color=new Color(0,0,0,0.85f); GUI.DrawTexture(new Rect(0,0,1280,720),white); GUI.color=Color.white;
        GUI.Label(new Rect(0,60,1280,70),rd.success?"远征成功！":"远征失败...",TitleCol(rd.success?G.ParseColor("#7fff7f"):G.ParseColor("#ff6644")));
        GUI.Label(new Rect(0,130,1280,28),rd.subtitle,subtitle);
        DrawPanel(340,180,600,280,""); int rowind=0;
        GUI.Label(new Rect(360,190,300,24),"用时 "+rd.timeUsed+"秒",label); GUI.Label(new Rect(660,190,300,24),"击杀数 "+rd.kills,label);
        GUI.Label(new Rect(360,220,300,24),"开启宝箱 "+rd.chests,label); GUI.Label(new Rect(660,220,300,24),"获得金币 +"+rd.goldEarned,label);
        GUI.Label(new Rect(360,250,300,24),"受到伤害 "+rd.damageTaken,label); GUI.Label(new Rect(660,250,300,24),"地图 "+rd.mapName,label);
        GUI.Label(new Rect(360,284,560,22),"—— 战利品清单 ——",Col2(Color.white,14,TextAnchor.MiddleCenter));
        float ly=312; foreach(var i in rd.kept){ GUI.Label(new Rect(380,ly,500,20),i.icon+" "+i.name+" ×"+i.amount+"   ✓ 保留",Col2(G.ParseColor("#7fff7f"),13,TextAnchor.MiddleLeft)); ly+=22; } foreach(var i in rd.lost){ GUI.Label(new Rect(380,ly,500,20),i.icon+" "+i.name+" ×"+i.amount+"   ✗ 丢失",Col2(G.ParseColor("#ff6644"),13,TextAnchor.MiddleLeft)); ly+=22; }
        if(rd.kept.Count==0&&rd.lost.Count==0) GUI.Label(new Rect(380,312,500,20),"本次远征没有获得物资",small);
        if(GUI.Button(new Rect(500,500,280,54),"返回农场",new GUIStyle(GUI.skin.button){fontSize=20,fontStyle=FontStyle.Bold})){ gf.ReturnToFarm(); }
    }

    static void DrawWorkshop(GameFlow gf){ GUI.color=new Color(0,0,0,0.7f); GUI.DrawTexture(new Rect(0,0,1280,720),white); GUI.color=Color.white;
        GUI.Box(new Rect(300,40,680,640),""); GUI.Label(new Rect(320,56,640,32),"卡牌工坊",Col2(Color.white,24,TextAnchor.MiddleLeft));
        GUI.Label(new Rect(320,92,640,24),"收获作物有概率掉落强化卡，使用后永久提升基础技能。",small);
        float sy=130; foreach(var sk in GameData.Skills){ int lv=GameState.skillLevels.ContainsKey(sk.id)?GameState.skillLevels[sk.id]:1; var stats=SkillMath.GetStats(sk); string power=stats.damage>0?("伤害 "+stats.damage):stats.stunDuration>0?("控制 "+stats.stunDuration+"s"):stats.dashDistance>0?("位移 "+stats.dashDistance):("隐身 "+stats.stealthDuration+"s");
            GUI.Label(new Rect(340,sy,300,22),sk.icon+" "+sk.name+"  Lv."+lv,Col2(Color.white,16,TextAnchor.MiddleLeft)); GUI.Label(new Rect(340,sy+20,300,16),power+" · 能量 "+stats.energyCost+" · CD "+stats.cooldown+"s",small); sy+=40; }
        GUI.Label(new Rect(320,sy+6,640,22),"—— 作物卡牌库存 ——",Col2(Color.white,14,TextAnchor.MiddleCenter));
        sy+=30; int count=0; if(GameState.cardInventory.Count==0){ GUI.Label(new Rect(320,sy,640,24),"卡牌库存为空。收获作物有概率掉落强化卡。",Col2(Color.white,13,TextAnchor.MiddleCenter)); }
        foreach(var card in GameState.cardInventory){ float bx=340+(count%4)*150, by=sy+(count/4)*86; var sk=SaveSystem.SkillById(card.skillId);
            GUIStyle st=new GUIStyle(GUI.skin.button){fontSize=11,alignment=TextAnchor.MiddleCenter,wordWrap=true}; st.normal.textColor=card.rarity=="legendary"?G.ParseColor("#ee9637"):card.rarity=="rare"?G.ParseColor("#55aaf1"):Color.white;
            if(GUI.Button(new Rect(bx,by,138,74),card.icon+" "+sk.name+" +"+card.power+"\n"+card.rarity+"\n点击使用",st)){ CardSystem.Apply(card.id); } count++; }
        if(GUI.Button(new Rect(560,620,160,40),"关闭")){ workshopOpen=false; }
    }

    static void DrawToasts(GameFlow gf){ float y=84; foreach(var t in gf.toasts){ float alpha=Math.Min(1,t.life/0.4f); Color c=t.type=="warning"?G.ParseColor("#ff8866"):t.type=="gold"?G.ParseColor("#ffd700"):t.type=="success"?G.ParseColor("#99ff99"):Color.white; c.a=Math.Max(0,alpha);
            GUI.Label(new Rect(0,y,1280,30),t.msg,Col2(c,15,TextAnchor.MiddleCenter)); y+=32; } }
    static void DrawDropBanners(GameFlow gf){ float y=90; foreach(var b in gf.dropBanners){ float alpha=Math.Min(1,b.life/0.4f); Color c=b.card.rarity=="legendary"?G.ParseColor("#ee9637"):b.card.rarity=="rare"?G.ParseColor("#55aaf1"):Color.white; c.a=Math.Max(0,alpha); GUI.color=c; GUI.DrawTexture(new Rect(960,y,220,52),white); GUI.color=Color.white;
            GUI.Label(new Rect(960,y,220,52),"收获掉落 · "+b.card.rarity+"\n"+b.card.icon+" "+b.card.name,Col2(c,13,TextAnchor.MiddleCenter)); y+=58; } }

    static GUIStyle TitleCol(Color c){ var s=new GUIStyle(GUI.skin.label){fontSize=42,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter}; s.normal.textColor=c; return s; }

    // -------- 外观层 API -------
    public static void ShowToast(string msg,string type=""){ if(GameFlow.I!=null) GameFlow.I.AddToast(msg,type); }
    public static void ShowDrop(Card c){ if(GameFlow.I!=null) GameFlow.I.AddDrop(c); }
    public static void SignalFlash(){ if(GameFlow.I!=null) GameFlow.I.SignalFlashFx(); }
    public static void ShowResult(bool success,string mapName,string timeUsed,int kills,int chests,int damageTaken,int goldEarned,List<GroundLoot> kept,List<GroundLoot> lost){
        var rd=new GameFlow.ResultData{ success=success, mapName=mapName, timeUsed=timeUsed, subtitle=success?("你成功从"+mapName+"撤离，战利品已存入仓库"):("你在"+mapName+"倒下了，大部分物资丢失"), kills=kills, chests=chests, damageTaken=damageTaken, goldEarned=goldEarned, kept=kept, lost=lost };
        if(GameFlow.I!=null) GameFlow.I.ShowResult(rd);
    }
}
