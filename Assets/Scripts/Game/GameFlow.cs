using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

// ============ 主控制器：游戏循环 / 输入 / 屏幕状态机 / 渲染呈现（移植自 game.js） ============
public class GameFlow : MonoBehaviour
{
    public static GameFlow I;
    public RenderBackend backend;
    public WorldRenderer world;
    public GameSceneBoot sceneBoot;
    public string screen = "menu";
    public Expedition current;
    public float accumulator;
    public float lastTime;
    public List<Toast> toasts = new List<Toast>();
    public List<DropBanner> dropBanners = new List<DropBanner>();
    public List<ResultData> resultQueue = new List<ResultData>();
    public float signalFlash;
    public GameObject cameraGo;
    bool prevW,prevS,prevA,prevD;
    InputAction moveAction;
    static readonly Dictionary<int,bool> nativePrev=new Dictionary<int,bool>();
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
    static bool NativeDown(int vKey){ return (GetAsyncKeyState(vKey)&0x8000)!=0; }
    static bool NativePressed(int vKey){ bool now=NativeDown(vKey), old=nativePrev.ContainsKey(vKey)&&nativePrev[vKey]; nativePrev[vKey]=now; return now&&!old; }

    public class Toast { public string msg,type; public float life=2.5f; public Toast(string m,string t){msg=m;type=t;} }
    public class DropBanner { public Card card; public float life=3.1f; public DropBanner(Card c){card=c;} }
    public class ResultData { public bool success; public string mapName,timeUsed,subtitle,title; public int kills,chests,damageTaken,goldEarned; public List<GroundLoot> kept,lost; }

    void Awake(){
        I=this; backend=new RenderBackend();
        moveAction=new InputAction("Move",InputActionType.Value);
        var wasd=moveAction.AddCompositeBinding("2DVector");
        wasd.With("Up","<Keyboard>/w").With("Up","<Keyboard>/upArrow");
        wasd.With("Down","<Keyboard>/s").With("Down","<Keyboard>/downArrow");
        wasd.With("Left","<Keyboard>/a").With("Left","<Keyboard>/leftArrow");
        wasd.With("Right","<Keyboard>/d").With("Right","<Keyboard>/rightArrow");
        moveAction.Enable();
    }
    void OnDestroy(){ if(moveAction!=null) moveAction.Dispose(); }

    void Start(){
        Cursor.visible=true;
        sceneBoot = new GameSceneBoot(); sceneBoot.Build(9f);
        SaveSystem.Load();
        AudioManager.I.SetScene("menu");
    }

    // ------------- 屏幕流转 -------------
    public void StartGame(){ AudioManager.I.SetScene("farm"); screen="farm"; SaveSystem.Load(); FarmSystem.Init(); if(!RewardSystem.DailyClaimedToday()) AddToast("家园补给站有今日奖励可以领取","gold"); }
    public void BackToMenu(){ AudioManager.I.SetScene("menu"); SaveSystem.Save(); screen="menu"; }
    public void OpenPrep(){ AudioManager.I.SetScene("prep"); SaveSystem.Save(); screen="prep"; }
    public void ClosePrep(){ AudioManager.I.SetScene("farm"); screen="farm"; }
    public void StartExpedition(){
        var map=SaveSystem.MapById(GameState.selectedMap); if(map==null) map=GameData.Maps[0];
        if(GameState.gold<map.entryFee){ AddToast("金币不足，无法支付入场费","warning"); return; }
        GameState.gold-=map.entryFee; SaveSystem.Save();
        var startWeapon=SaveSystem.WeaponById(GameState.selectedWeapon)??GameData.Weapons[0];
        // 消耗一次性卡
        var consumed=GameState.selectedBoostCards.FindAll(id=>{ var c=GameState.cardInventory.Find(x=>x.id==id); return c!=null&&c.singleUse; });
        if(consumed.Count>0){ foreach(var id in consumed) GameState.cardInventory.RemoveAll(c=>c.id==id); foreach(var id in consumed) GameState.selectedBoostCards.Remove(id); AddToast("已消耗 "+consumed.Count+" 张一次性技能卡，本次远征生效","gold"); SaveSystem.Save(); }
        AudioManager.I.SetScene("expedition"); screen="expedition";
        current=new Expedition(map.id,startWeapon); UpdateFogBind(); lastTime=Time.time; accumulator=0;
        if(world==null) world=new WorldRenderer();
        world.Build(current);
    }
    void UpdateFogBind(){ if(current!=null){ current.BindTerrainPainter(backend); backend.ClearChunks(); } }
    public bool PreppingExpedition(){ return screen=="expedition"; }
    public void ReturnToFarm(){ AudioManager.I.SetScene("farm"); SaveSystem.Save(); current=null; backend.ClearChunks(); if(world!=null){ world.Clear(); world=null; } screen="farm"; }

    public void ShowResult(ResultData d){ resultQueue.Clear(); resultQueue.Add(d); screen="result"; AudioManager.I.SetScene("result"); }
    public ResultData PeekResult(){ return resultQueue.Count>0?resultQueue[0]:null; }

    public void AddToast(string msg,string type=""){ toasts.Add(new Toast(msg,type)); if(toasts.Count>5) toasts.RemoveAt(0); }
    public void AddDrop(Card c){ dropBanners.Add(new DropBanner(c)); }
    public void SignalFlashFx(){ signalFlash=0.85f; }

    // ------------- 主循环 -------------
    void Update(){
        if(screen=="expedition" && current!=null && !current.gameOver){
            PollInput();
            float frameTime=Math.Min(Time.deltaTime,0.25f);
            if(current.paused){
                accumulator=0;
            } else if(current.hitStop>0){
                current.hitStop=Math.Max(0,current.hitStop-frameTime);
            } else {
                float fixedStep=1f/60f; accumulator=Math.Min(accumulator+frameTime,fixedStep*8); int steps=0;
                while(accumulator>=fixedStep && steps<8){ current.Update(fixedStep); accumulator-=fixedStep; steps++; }
            }
            if(world!=null) world.Tick(current);
        } else if(screen=="expedition" && current!=null && current.gameOver && screen!="result"){
            // result handled via ShowResult
        }
        // 提示/掉落 计时
        for(int i=toasts.Count-1;i>=0;i--){ toasts[i].life-=Time.deltaTime; if(toasts[i].life<=0) toasts.RemoveAt(i); }
        for(int i=dropBanners.Count-1;i>=0;i--){ dropBanners[i].life-=Time.deltaTime; if(dropBanners[i].life<=0) dropBanners.RemoveAt(i); }
        if(signalFlash>0) signalFlash=Math.Max(0,signalFlash-Time.deltaTime);
    }

    void PollInput(){
        if(current==null) return;
        var keyboard=Keyboard.current;
        var mouse=Mouse.current;

        if((keyboard!=null && keyboard.escapeKey.wasPressedThisFrame)||NativePressed(0x1B)){
            current.paused=!current.paused;
            accumulator=0;
        }

        // 暂停时立即释放持续输入，避免恢复后角色继续移动或攻击。
        if(current.paused){
            current.SetKey("w",false); current.SetKey("s",false);
            current.SetKey("a",false); current.SetKey("d",false); current.SetKey("shift",false);
            current.SetMouseDown(false);
            return;
        }

        Vector2 move=moveAction!=null?moveAction.ReadValue<Vector2>():Vector2.zero;
        bool w=move.y>0.1f || NativeDown(0x57) || NativeDown(0x26);
        bool s=move.y<-0.1f || NativeDown(0x53) || NativeDown(0x28);
        bool a=move.x<-0.1f || NativeDown(0x41) || NativeDown(0x25);
        bool d=move.x>0.1f || NativeDown(0x44) || NativeDown(0x27);
        current.SetKey("w",w); current.SetKey("s",s); current.SetKey("a",a); current.SetKey("d",d);
        if(w!=prevW||s!=prevS||a!=prevA||d!=prevD){
            Debug.Log("INPUT_STATE W="+w+" S="+s+" A="+a+" D="+d+" device="+(keyboard!=null)+" player="+current.player.x.ToString("F1")+","+current.player.y.ToString("F1"));
            prevW=w; prevS=s; prevA=a; prevD=d;
        }
        current.SetKey("shift",keyboard!=null && (keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed));
        if(keyboard!=null){
            if((keyboard.digit1Key.wasPressedThisFrame)||NativePressed(0x31)) current.OnSkill(0);
            if((keyboard.digit2Key.wasPressedThisFrame)||NativePressed(0x32)) current.OnSkill(1);
            if((keyboard.digit3Key.wasPressedThisFrame)||NativePressed(0x33)) current.OnSkill(2);
            if((keyboard.digit4Key.wasPressedThisFrame)||NativePressed(0x34)) current.OnSkill(3);
            if((keyboard.tabKey.wasPressedThisFrame)||NativePressed(0x09)) current.CycleWeapon(1);
            if((keyboard.qKey.wasPressedThisFrame)||NativePressed(0x51)) current.OnConsumable("herb_kit");
            if((keyboard.rKey.wasPressedThisFrame)||NativePressed(0x52)) current.OnConsumable("thorn_storm");
            if((keyboard.eKey.wasPressedThisFrame)||NativePressed(0x45)) current.OnConsumable("signal_flare");
        }
        if(mouse!=null){
            current.SetMouse(ScreenToCanvas(mouse.position.ReadValue()));
            if(mouse.leftButton.wasPressedThisFrame) current.SetMouseDown(true);
            if(mouse.leftButton.wasReleasedThisFrame) current.SetMouseDown(false);
        } else {
            current.SetMouseDown(false);
        }
    }

    public Vector2 ScreenToCanvas(Vector3 screenPos){
        if(backend.viewRect.width<=0) backend.ComputeViewRect(Screen.width,Screen.height);
        var r=backend.viewRect;
        float cx=(screenPos.x-r.x)/r.width*1280f; float cy=720f-(screenPos.y-r.y)/r.height*720f;
        return new Vector2(cx,cy);
    }

    void OnGUI(){
        backend.ComputeViewRect(Screen.width,Screen.height);
        GUI.matrix=Matrix4x4.identity;
        // 世界由 2.5D 相机渲染；OnGUI 只画 HUD/菜单层与交互提示
        if(screen=="expedition" && current!=null){
            backend.hasExpedition=true;
            DrawMinimap();
        } else {
            backend.hasExpedition=false;
        }
        UIHost.DrawUI(this);
        // 世界浮动文字（远征中）
        if(screen=="expedition" && current!=null){ DrawWorldOverlays(); }
        GUI.matrix=Matrix4x4.identity;
    }

    void DrawWorldOverlays(){
        if(current==null || Camera.main==null) return;
        float S=WorldRenderer.S;
        // 伤害跳字
        foreach(var d in current.damageNumbers){
            var v=Camera.main.WorldToScreenPoint(new Vector3(d.x*S,0.6f,d.y*S)); if(v.z<=0) continue;
            var st=new GUIStyle(GUI.skin.label){fontSize=d.heavy?20:14,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            st.normal.textColor=G.ParseColor(d.color); GUI.Label(new Rect(v.x-120,v.y-16,240,30),"-"+d.value,st);
        }
        // 交互提示
        var ip=InteractPrompt(); if(ip.HasValue){
            var v=Camera.main.WorldToScreenPoint(new Vector3(ip.Value.x*S,1.2f,ip.Value.y*S)); if(v.z<=0) return;
            var st=new GUIStyle(GUI.skin.label){fontSize=13,alignment=TextAnchor.MiddleCenter};
            st.normal.textColor=G.ParseColor("#ffd700"); GUI.Label(new Rect(v.x-140,v.y-22,280,36),ip.Value.text,st);
        }
    }
    // 计算最近可交互物提示（宝箱/塔/撤离点）
    (float x,float y,string text)? InteractPrompt(){
        if(current==null) return null;
        var p=current.player;
        foreach(var c in current.chests){ if(!c.opened && current.IsWorldVisible(c.x,c.y) && G.Dist(p.x,p.y,c.x,c.y)<60) return (c.x,c.y,"左键打开宝箱"); }
        foreach(var t in current.towers){ if(t.state!="player" && current.IsWorldVisible(t.x,t.y) && G.Dist(p.x,p.y,t.x,t.y)<60) return (t.x,t.y,t.state=="broken"?"点击修复并占领防御塔":"点击占领防御塔"); }
        foreach(var ep in current.extractPoints){ if(current.IsWorldVisible(ep.x,ep.y) && G.Dist(p.x,p.y,ep.x,ep.y)<ep.radius) return (ep.x,ep.y,"点击开始撤离"); }
        return null;
    }

    void DrawWorldTexts(){
        var r=backend.viewRect; float sc=r.width/1280f; GUI.color=Color.white;
        var prev=GUI.skin.label; var style=new GUIStyle(prev); style.alignment=TextAnchor.MiddleCenter; style.fontStyle=FontStyle.Bold; style.normal.textColor=Color.white;
        foreach(var wt in current.worldTexts){
            float sx=r.x+wt.x*sc, sy=r.y+wt.y*sc;
            style.fontSize=Math.Max(8,(int)(wt.size*sc)); style.normal.textColor=GUI.color=G.ParseColor(wt.color);
            var r2=new Rect(sx-300,sy-14,600,28); if(wt.align==1) GUI.Label(r2,wt.text,style); else GUI.Label(r2,wt.text,style);
        }
        GUI.color=Color.white;
    }

    Canvas2D mmCanvas; Texture2D mmTex;
    void DrawMinimap(){
        if(current==null) return;
        if(mmCanvas==null){ mmCanvas=new Canvas2D(160,160); mmTex=new Texture2D(160,160,TextureFormat.RGBA32,false); }
        current.RenderMinimap(mmCanvas,current.camera); mmCanvas.UploadTo(mmTex);
        var r=backend.viewRect; float sc=r.width/1280f; float size=160*sc; GUI.DrawTexture(new Rect(r.x+r.width-size-12*sc,r.y+r.height-size-12*sc,size,size),mmTex);
    }

    // 供 UI 调用的资源按钮
    public void UseCatalyst(){ FarmSystem.UseGrowthCatalyst(); }
    public void ClaimDaily(){ RewardSystem.ClaimDaily(); }
    public void ClaimRelief(){ RewardSystem.ClaimRelief(); }
    public void OpenWorkshop(){ UIHost.workshopOpen=true; SaveSystem.Save(); }
    public void CloseWorkshop(){ UIHost.workshopOpen=false; }
}
