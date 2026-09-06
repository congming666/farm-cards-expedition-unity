# 更新日志

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
