# 农场卡牌：荒野远征（Unity 6 移植版）

融合 QQ 农场经营 + 卡牌收集 + 搜打撤远征的 2D 游戏，从网页 Canvas2D 移植到 Unity 6，面向 Steam 发布方向优化。

## 已完成的 Steam 方向优化

### 渲染层 GPU 化
- 地形块烘焙结果一次性生成 Texture2D/Sprite 交 GPU
- 角色 / 怪物 / 弹道用 SpriteRenderer
- 迷雾用 RenderTexture + Shader
- 特效用 ParticleSystem（软件绘制已替换）

### UI 全量 UGUI 迁移
- 全部 14 个 OnGUI 界面迁移到 UGUI Canvas + Canvas Scaler（解决 1280×720 拉伸发糊）
- 中文使用思源黑体 TextMeshPro 字体资产（规避微软雅黑商业分发版权）
- 覆盖：主菜单 / 农场主页 / 远征 HUD / 准备大厅 / 设置菜单 / 结算 / 卡牌工坊 / 物资仓库 / 育种温室 / 全局 toast + 掉落 + 红屏 / 世界伤害跳字 / 小地图

### 存档 v2
- schema 版本号 + 迁移
- 多存档位
- 临时文件原子写 + 损坏备份（为 Steam 云存档铺路）

### 桌面标配
- 设置菜单（分辨率 / 全屏 / 音量 / 键位 / 手柄）
- `runInBackground=1`、正常退出、全局异常捕获 + 崩溃日志
- BuildPlayer：关 Development、IL2CPP、挂图标、产品名 / 公司名

### 平台层
- Steamworks.NET 最小集成（初始化 → 成就 → 云存档 → Rich Presence → Overlay），`#if` 隔离
- SteamPipe depot 出包流程就绪

## 游戏性更新（v0.4.0）

- 修复网页版第一波兽潮卡死 bug（Unity 版无此 bug，已确认）
- Boss 四技能循环（地裂震荡 / 狂暴冲锋 / 召唤兽群 / 暗影弹幕），阶段二强化
- 近战三连击系统（横扫 / 反手 / 终结）
- 处决 / 死亡动画大幅调低调
- 所有攻击特效加双层层次（白色内环 + 主环）

## 技术栈

- Unity 6000.0.82f1
- UGUI + TextMeshPro
- IL2CPP 编译
- Steamworks.NET（条件编译隔离）

## 构建

```bash
# 编译验证（不打包）
Unity.exe -batchmode -quit -nographics -projectPath . -logFile compile.log

# IL2CPP 出包
Unity.exe -batchmode -quit -nographics -projectPath . -executeMethod BuildPlayer.ReleaseCLI -logFile build.log
```

产物：`Build/standalone/FarmCards.exe`

## 目录

```
Assets/Scripts/
  Game/              游戏逻辑（Expedition 核心 + Combat + Effects + Types）
  Platform/          平台层（Steam / 存档 / 设置 / 崩溃日志）
  UI/                UGUI 界面
  Editor/BuildPlayer.cs  构建脚本
```

## 更新日志

见 [CHANGELOG.md](./CHANGELOG.md)
