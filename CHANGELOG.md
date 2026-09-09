# 更新日志

## v0.5.0 — 2026-09-09 农场大更新

### 新增：作物机制差异化
- 8 种作物各有独特特性：小麦连作加成、向日葵光环、西瓜大型高产、豌豆反复收获、卷心菜耐寒、胡萝卜变异、玉米招兽、南瓜可雕刻
- 种植时随机 roll 品质（普通/优质/稀有/传说），品质影响产量

### 新增：照料与天气系统
- 每格湿度条，干旱时生长减速 50%，点击浇水恢复（3 秒冷却）
- 施肥：普通肥（8 金，+15% 速度）/ 高级肥（20 金，+30% 但 20% 概率烧苗）
- 天气循环：晴/雨/雾/雷暴，雨天自动浇水、雾天变异率+、雷暴减速
- 季节系统：春夏秋冬，冬季非耐寒作物减速，夏季西瓜加速
- 玉米成熟后概率吸引野兽偷食，需点击驱赶（有概率掉落材料）

### 新增：加工与产业链
- 9 种加工配方：小麦→面粉→面包、向日葵→油→火把、西瓜→果汁、玉米→饲料→鸡蛋、南瓜→南瓜灯、卷心菜→驱虫剂
- 加工队列实时推进，完成后自动入仓
- 工坊可升级（最高 Lv.3），解锁高级配方
- 加工产品可作远征消耗品或农场装饰

### 新增：收集与变异
- 作物图鉴：记录每种作物的最高品质，收集给全局生长 buff
- 变异系统：胡萝卜在雾天/施肥时概率变异为稀有品质
- 图鉴收集进度显示全局生长加成百分比

### 新增：装饰与访客
- 装饰商店：稻草人/栅栏/水井/雕像/花坛/风车，购买后增加美观度
- 美观度提供全局金币加成，水井减少浇水冷却，雕像提供生长 buff
- 访客系统：NPC 随机来访，美观度越高来访越频繁，互动获得金币/种子奖励

### 数据与存档
- Plot 扩展：moisture/quality/harvestCount/fertilized/pestType
- GameState 扩展：weather/season/workshopLevel/processingQueue/cropCollection/decorations/farmBeauty/visitorState
- SaveSystem v2 兼容：新字段自动默认值，旧档无损升级

### 验证
- batchmode 编译 0 错
- IL2CPP 出包 Build Successful

---

## v0.4.0 — 2026-09-06

### 修复
- 确认 Unity 版兽潮逻辑无网页版那个 `waveMonsters` 未定义变量 bug（使用 `waveRemaining` 正确）

### 新增
- **Boss 四技能循环**（`CastBossAbility`）：
  1. 地裂震荡 — 玩家脚下 AOE + 冲击环 + 近距伤害
  2. 狂暴冲锋 — 向玩家突进 140-170 + 路径火花 + 终点冲击
  3. 召唤兽群 — 召唤 2-3 只 wolf/spider/bat，带召唤阵
  4. 暗影弹幕 — 6-8 发扇形投射物
  - 阶段二（hp<50%）：伤害 ×1.3、数量 +1、冷却 4.0s → 2.6s
  - Monster 新增 `abilityIndex` 字段
- **近战三连击**：`attackCombo` 循环 0-2，横扫 → 反手 → 终结，终结技重击破甲 + 高击退 + 大斩击范围
- 新增 `SpawnImpact` / `SpawnShockRing` 通用特效方法

### 调整
- **处决 / 死亡动画大幅调低调**：
  - Boss：killFlash 0.22→0.10、hitStop 0.13→0.07、screenShake 1→0.45、radialBurst 30→18
  - 普通怪：killFlash 0.11→0.06、hitStop 0.07→0.05、screenShake 0.65→0.35、radialBurst 18→14
  - 终结技：visualVz 170/125→120/95、slash 范围 86/66→70/58、lungePower [10,15,23]→[10,13,17]

### 优化
- **所有攻击特效加双层层次**：
  - `SpawnAoeEffect` 加白色内环（0.6 倍大小）
  - `SpawnSlashEffect` 加白色内层高光弧（0.7 倍大小）
  - `SpawnImpact`：光爆 + 外环 shock
  - `SpawnShockRing`：主环 + 白色内环（0.55 倍大小短寿命）
- `SpawnKillFeedback` 新增 impact + shockRing 层次

### 验证
- batchmode 编译 0 错
- IL2CPP 出包 Build Successful

---

## v0.3.0 — 2026-08-XX

### 平台地基
- Unity 6 移植完成
- AtomicFile / GameSettings / CrashLogger / AppBootstrap
- 存档 v2（多槽位 + 原子写 + 迁移）
- 渲染 GPU 化（Texture2D/Sprite/RenderTexture/ParticleSystem）
- Steamworks.NET 最小集成（#if 隔离）
- BuildPlayer（IL2CPP + 关 Development + 图标）

### UI 全量 UGUI 迁移
- 14 个 OnGUI 界面全部迁移到 UGUI Canvas + Canvas Scaler
- 中文思源黑体 TextMeshPro 字体资产
- 主菜单 / 农场 / 准备大厅 / 远征 HUD / 设置 / 结算 / 卡牌工坊 / 物资仓库 / 育种温室 / 全局 toast + 掉落 + 红屏 / 世界伤害跳字 / 小地图
