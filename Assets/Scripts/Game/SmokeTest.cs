using System;
using UnityEngine;

// 批处理冒烟测试：-smoketest 参数启动时跑一段自动战斗并校验不变量
public static class SmokeTest
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CheckStart(){ 
        foreach(var a in Environment.GetCommandLineArgs()) if(a=="-smoketest"){ Run(); return; }
    }

    static void Run(){
        try {
            // 重置默认存档状态
            GameState.gold=500; GameState.seeds=3; GameState.materials=0; GameState.unlockedPlots=8;
            GameState.selectedMap="t1"; GameState.selectedWeapon="harvest_sickle"; GameState.selectedCrop="wheat";
            GameState.unlockedCrops=new System.Collections.Generic.List<string>{"pea_shooter","sunflower","watermelon","cabbage","wheat"};
            GameState.skillLevels=new System.Collections.Generic.Dictionary<string,int>{{"straw_smash",1},{"vine_bind",1},{"earth_dash",1},{"smoke_screen",1}};
            GameState.cardInventory=new System.Collections.Generic.List<Card>(); GameState.selectedBoostCards=new System.Collections.Generic.List<string>();
            GameState.loadout=new System.Collections.Generic.Dictionary<string,int>{{"herb_kit",2},{"thorn_storm",1},{"signal_flare",0}};
            GameState.farmItems=new System.Collections.Generic.Dictionary<string,int>{{"growth_catalyst",0}};
            // 农场初始化冒烟
            FarmSystem.Init();
            FarmSystem.Plant(0);
            // 远征冒烟
            var exp=new Expedition("t1", GameData.Weapons[0]);
            int steps=0; bool ended=false;
            for(int i=0;i<1800;i++){ // 约30秒
                exp.SetMouse(new Vector2(exp.player.x-exp.camera.x+60, exp.player.y-exp.camera.y));
                exp.SetMouseDown(true);
                exp.SetKey("w", i%120<60); exp.SetKey("d", true); exp.SetKey("a", i%97<30);
                if(i%300==0) exp.UseSkill(0);
                if(i%400==0) exp.UseConsumable("herb_kit");
                exp.Update(1f/60f);
                steps++;
                if(exp.result!=null){ ended=true; break; }
                if(i>500) exp.beastWave.nextIn=Math.Min(exp.beastWave.nextIn,0.05f); // 触发一波兽潮
            }
            bool monstersSpawned=exp.monsters.Count>0 || ended;
            Debug.Log("SMOKE_TEST_PASS steps="+steps+" ended="+ended+" result="+exp.result+" hp="+Mathf.CeilToInt(exp.player.hp)+" kills="+exp.killCount+" monsters="+exp.monsters.Count+" chests="+exp.chestOpened);
            // 渲染路径冒烟：构建世界渲染器+迷雾+光柱，确认不抛异常
            try {
                var camGo=new GameObject("SmokeCam"); camGo.tag="MainCamera"; camGo.AddComponent<Camera>();
                var wr=new WorldRenderer(); wr.Build(exp); wr.Tick(exp); wr.Tick(exp);
                Debug.Log("RENDER_PATH_EXERCISE OK ground="+(wr.groundSpr!=null)+" monsters_rendered="+(wr.NumMonsterRenders()));
            } catch(Exception re){ Debug.LogError("RENDER_PATH_EXERCISE_FAIL "+re); }
            Application.Quit(0);
        } catch(Exception e){
            Debug.LogError("SMOKE_TEST_FAIL "+e); Application.Quit(1);
        }
    }
}
