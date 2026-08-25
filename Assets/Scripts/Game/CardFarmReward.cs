using System;
using System.Collections.Generic;
using UnityEngine;

// ============ 卡牌系统（移植自 card.js） ============
public static class CardSystem
{
    static Dictionary<string,int> rarityPower = new Dictionary<string,int>{{"common",1},{"rare",2},{"legendary",3}};
    static string[] rn = new[]{"common","rare","legendary"};

    public static Card CreateCard(CropDef crop){
        float roll=(float)G.Rng.NextDouble(); string rarity="common";
        if(crop.rarity=="legendary") rarity = roll<0.42f?"legendary":(roll<0.86f?"rare":"common");
        else if(crop.rarity=="rare") rarity = roll<0.06f?"legendary":(roll<0.38f?"rare":"common");
        else rarity = roll<0.12f?"rare":"common";
        string skillId = crop.upgradeSkill=="all" ? GameData.Skills[G.RandInt(0,GameData.Skills.Length-1)].id : crop.upgradeSkill;
        var skill = SaveSystem.SkillById(skillId);
        int power = rarityPower[rarity];
        return new Card{ id="c"+DateTime.Now.Ticks+"-"+G.Rng.Next(10000), rarity=rarity, power=power, skillId=skillId, icon=crop.icon,
            name=crop.name+"·"+skill.name+"强化", desc="使用后令「"+skill.name+"」永久提升 "+power+" 级，最高 8 级。" };
    }

    public static Card TryDrop(CropDef crop){ if((float)G.Rng.NextDouble()>=crop.cardChance) return null; var card=CardSystem.CreateCard(crop); GameState.cardInventory.Add(card); SaveSystem.Save(); UIHost.ShowDrop(card); return card; }

    public static void Apply(string cardId){
        int index=GameState.cardInventory.FindIndex(c=>c.id==cardId); if(index<0) return;
        var card=GameState.cardInventory[index]; int before=GameState.skillLevels.ContainsKey(card.skillId)?GameState.skillLevels[card.skillId]:1;
        if(before>=8){ UIHost.ShowToast("该技能已达到最高等级","warning"); return; }
        GameState.skillLevels[card.skillId]=Math.Min(8,before+card.power);
        GameState.selectedBoostCards.RemoveAll(id=>id==card.id);
        GameState.cardInventory.RemoveAt(index); SaveSystem.Save();
        var skill=SaveSystem.SkillById(card.skillId); UIHost.ShowToast(skill.name+"提升至 Lv."+GameState.skillLevels[card.skillId],"gold");
    }

    public static Dictionary<string,int> GetSelectedBoosts(){ var boosts=new Dictionary<string,int>(); foreach(var id in GameState.selectedBoostCards){ var card=GameState.cardInventory.Find(c=>c.id==id); if(card!=null) boosts[card.skillId]=(boosts.ContainsKey(card.skillId)?boosts[card.skillId]:0)+card.power; } return boosts; }

    public static bool ToggleBoost(string cardId){
        if(GameState.selectedBoostCards.Contains(cardId)){ GameState.selectedBoostCards.Remove(cardId); return false; }
        if(GameState.selectedBoostCards.Count>=3){ UIHost.ShowToast("每次远征最多携带3张强化卡","warning"); return false; }
        GameState.selectedBoostCards.Add(cardId); return true;
    }
}

// ============ 农场系统（移植自 farm.js） ============
public static class FarmSystem
{
    public static void Init(){
        GameState.EnsurePlots();
        if(GameState.unlockedPlots<8) GameState.unlockedPlots=8;
        bool any=false; for(int i=0;i<GameState.farmPlots.Length;i++) if(GameState.farmPlots[i].crop!=null) any=true;
        if(any) return;
        for(int i=0;i<3;i++){ int idx=G.RandInt(0,GameState.unlockedPlots-1); if(GameState.farmPlots[idx].crop==null){ GameState.farmPlots[idx].crop=GameData.Crops[0]; GameState.farmPlots[idx].plantedAt=(DateTime.Now.Ticks-TimeSpan.FromSeconds(G.Rand(5,20)).Ticks)/TimeSpan.TicksPerMillisecond; } }
        SaveSystem.Save();
    }

    public static void Plant(int idx){
        if(idx>=GameState.unlockedPlots) return;
        if(GameState.seeds<=0){ UIHost.ShowToast("种子不足！去远征获取更多种子","warning"); return; }
        var crop=SaveSystem.CropById(GameState.selectedCrop)??GameData.Crops[0];
        var plot=GameState.farmPlots[idx]; plot.crop=crop; plot.plantedAt=CurrentMs(); plot.ready=false; plot.status=null;
        float roll=(float)G.Rng.NextDouble(); plot.status=roll<0.08f?"drought":(roll<0.14f?"pest":(roll<0.21f?"weeds":null));
        GameState.seeds--; UIHost.ShowToast("种下了"+crop.name,"success"); SaveSystem.Save();
    }

    public static void Harvest(int idx){
        var plot=GameState.farmPlots[idx]; if(plot.crop==null||!plot.ready){ UIHost.ShowToast("还没成熟呢","warning"); return; }
        var crop=plot.crop; int reward=(int)crop.sellPrice; string rewardText=reward+"金币"; GameState.gold+=reward;
        if((float)G.Rng.NextDouble()<0.3f) GameState.seeds++;
        if(crop.rare) GameState.materials+=G.RandInt(1,3);
        if(crop.rewardType=="gold"){ int bonus=G.RandInt(25,45); GameState.gold+=bonus; rewardText=(reward+bonus)+"金币"; }
        else if(crop.rewardType=="healing"){ GameState.loadout["herb_kit"]=(GameState.loadout.ContainsKey("herb_kit")?GameState.loadout["herb_kit"]:0)+1; rewardText="草药包扎包 x1"; }
        else if(crop.rewardType=="attack_card"){ var card=CardSystem.CreateCard(crop); card.name="豌豆连射 · "+card.name; card.desc="远征攻击强化：提高基础攻击与稻草猛击等级。"; GameState.cardInventory.Add(card); UIHost.ShowDrop(card); rewardText="豌豆攻击强化卡 x1"; }
        else if(crop.rewardType=="skill_card"){ var card=CardSystem.CreateCard(crop); GameState.cardInventory.Add(card); UIHost.ShowDrop(card); rewardText="强化技能卡："+card.name+" x1"; }
        else if(crop.rewardType=="consumable_skill_card"){ var card=CardSystem.CreateCard(crop); card.singleUse=true; card.name=card.name+"（一次性）"; card.desc="本次远征可使用一次：临时提升「"+SaveSystem.SkillById(card.skillId).name+"」"+card.power+"级，撤离后消耗。"; GameState.cardInventory.Add(card); UIHost.ShowDrop(card); rewardText="一次性技能卡："+card.name+" x1"; }
        else CardSystem.TryDrop(crop);
        UIHost.ShowToast("收获"+crop.name+"，获得"+rewardText,"gold");
        plot.crop=null; plot.ready=false; plot.status=null; SaveSystem.Save();
    }

    public static void Tend(int idx){
        var plot=GameState.farmPlots[idx]; if(plot==null||plot.status==null) return;
        if(GameState.gold<4){ UIHost.ShowToast("需要4金币购买基础农具","warning"); return; }
        GameState.gold-=4; plot.status=null; SaveSystem.Save(); UIHost.ShowToast("照顾完成，作物恢复正常生长","success");
    }

    public static void UseGrowthCatalyst(){
        int count=GameState.farmItems.ContainsKey("growth_catalyst")?GameState.farmItems["growth_catalyst"]:0;
        if(count<=0){ UIHost.ShowToast("没有生长催化剂，可从远征宝箱中获取","warning"); return; }
        double now=CurrentMs(); int best=-1; float bestRem=0;
        for(int i=0;i<GameState.farmPlots.Length;i++){ var p=GameState.farmPlots[i]; if(i>=GameState.unlockedPlots||p.crop==null||p.ready) continue; float rem=p.crop.growTime-(float)((now-p.plantedAt)/1000.0); if(rem>0 && rem>bestRem){ bestRem=rem; best=i; } }
        if(best<0){ UIHost.ShowToast("当前没有正在生长的作物","warning"); return; }
        var plot=GameState.farmPlots[best]; float reduce=plot.crop.growTime*0.1f; plot.plantedAt-=reduce*1000; GameState.farmItems["growth_catalyst"]=count-1; SaveSystem.Save();
        UIHost.ShowToast("使用生长催化剂："+plot.crop.name+"生长时间缩短"+reduce.ToString("F1")+"秒（总时长10%）","success");
    }

    public static Cost GetUnlockCost(int idx){ int step=Math.Max(0,idx-8); return new Cost{ gold=90+step*35, materials=step<4?0:(int)Math.Floor((step-4)/5f)+1 }; }
    public static bool UnlockPlot(int idx){
        if(idx!=GameState.unlockedPlots){ UIHost.ShowToast("请按顺序扩建相邻农田","warning"); return false; }
        var cost=GetUnlockCost(idx);
        if(GameState.gold<cost.gold||GameState.materials<cost.materials){ UIHost.ShowToast("扩建需要 "+cost.gold+"金币"+(cost.materials>0?" 和 "+cost.materials+"材料":"")+"","warning"); return false; }
        GameState.gold-=cost.gold; GameState.materials-=cost.materials; GameState.unlockedPlots++; SaveSystem.Save(); UIHost.ShowToast("新农田已解锁："+GameState.unlockedPlots+"/36","gold"); return true;
    }
    static double CurrentMs(){ return DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond; }
}
public class Cost { public int gold, materials; }

// ============ 每日奖励与开荒保障（移植自 ui.js RewardSystem） ============
public static class RewardSystem
{
    static int[] rg={80,100,120,150,180,220,300}; static int[] rs={2,2,3,3,4,4,5}; static int[] rm={0,0,0,0,0,1,2};
    public static string DateKey(){ var d=DateTime.Now; return d.Year+"-"+d.Month.ToString("00")+"-"+d.Day.ToString("00"); }
    static int DayGap(string from){ if(string.IsNullOrEmpty(from)) return int.MaxValue; DateTime fromD; if(!DateTime.TryParse(from+"T12:00:00",out fromD)) return int.MaxValue; return (int)Math.Round((DateTime.Now.Date-fromD.Date).TotalDays); }
    public static int GetNextDailyDay(){ if(GameState.lastDailyClaim==DateKey()) return ((GameState.dailyStreak-1)%7)+1; return DayGap(GameState.lastDailyClaim)==1?(GameState.dailyStreak%7)+1:1; }
    public static bool IsReliefEligible(){ return GameState.lastReliefClaim!=DateKey() && (GameState.gold<60||GameState.seeds<=0); }
    public static bool ClaimDaily(){
        string today=DateKey(); if(GameState.lastDailyClaim==today){ UIHost.ShowToast("今天的家园补给已经领取","warning"); return false; }
        bool cons=DayGap(GameState.lastDailyClaim)==1; GameState.dailyStreak=cons?GameState.dailyStreak+1:1; int day=((GameState.dailyStreak-1)%7)+1;
        GameState.gold+=rg[day-1]; GameState.seeds+=rs[day-1]; GameState.materials+=rm[day-1]; GameState.lastDailyClaim=today; SaveSystem.Save();
        UIHost.ShowToast("第"+day+"天补给："+rg[day-1]+"金币、"+rs[day-1]+"种子"+(rm[day-1]>0?"、"+rm[day-1]+"材料":"")+"","gold"); return true;
    }
    public static bool ClaimRelief(){
        if(GameState.lastReliefClaim==DateKey()){ UIHost.ShowToast("今天已经领取过开荒保障","warning"); return false; }
        if(!IsReliefEligible()){ UIHost.ShowToast("金币低于60或种子耗尽时才能申请保障","warning"); return false; }
        int g=Math.Max(0,120-GameState.gold), s=Math.Max(0,3-GameState.seeds); GameState.gold+=g; GameState.seeds+=s; GameState.lastReliefClaim=DateKey(); SaveSystem.Save();
        UIHost.ShowToast("保障已送达：补充"+g+"金币、"+s+"种子","success"); return true;
    }
    public static bool DailyClaimedToday(){ return GameState.lastDailyClaim==DateKey(); }
    public static int TodayDay(){ return ((GameState.dailyStreak-1)%7)+1; }
}
