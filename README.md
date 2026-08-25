# 农场卡牌：荒野远征 — Unity 版

《农场卡牌：荒野远征》的 **Unity 6 桌面版**。在浏览器网页版的基础上，使用 Unity（URP）重新实现，提供原生 Windows 程序体验：真实的 2.5D 俯视角相机、四主题地图、战争迷雾、丁达尔光柱与完整的农场 + 卡牌 + 远征 + 撤离玩法循环。

> 📺 网页版（同款游戏，浏览器直接游玩）：[farm-cards-expedition](https://github.com/congming666/eas) · [在线体验](https://congming666.github.io/eas/)

## 运行

- Windows 直接运行构建产物：`Build/FarmCards.exe`（本仓库不包含构建产物，请按下方方式自行构建）
- 流程：主菜单 →【开始游戏】→ 家园 →【进入远征准备大厅】→ 选地图 →【确认配置并出发】

## 环境与构建

- 引擎：**Unity 6000.0.82f1**（URP 通用渲染管线）
- 用 Unity Hub 打开本目录（`Assets` / `Packages` / `ProjectSettings` 所在层级），等待导入完成
- 一键构建 Windows 64 位：菜单 Build → Windows64；或命令行：

```
Unity.exe -batchmode -quit -projectPath "<本目录>" -executeMethod BuildPlayer.Build
```

构建脚本位于 `Assets/Scripts/Editor/BuildPlayer.cs`，产物输出到 `Build/`。

## 已实现系统（对照网页版）

| 系统 | 文件 | 说明 |
|---|---|---|
| 数据配置 | `GameConfig.cs` | 武器 / 地图 / 技能 / 消耗品 / 怪物 / 作物全量数据 |
| 存档 | `SaveSystem.cs` | JsonUtility → `persistentDataPath/save.json` |
| 农场 | `CardFarmReward.cs` | 6×6 地块、种植/生长/状态（旱/虫/草）、收获、扩建、催化剂 |
| 卡牌工坊 | `CardFarmReward.cs` | 品质掉落、技能强化卡、携带卡、消耗卡 |
| 每日奖励 | `CardFarmReward.cs` | 7 天连签、开荒保障 |
| 远征（核心） | `Expedition.cs` / `Combat` / `Terrain` / `Render` | 地图程序化生成、地形块烘焙、摄像机、战争迷雾 |
| 战斗 | `ExpeditionCombat.cs` | 3 武器、4 技能、3 消耗品、5 类怪+精英、兽潮、防御塔、Boss |
| 特效 | `ExpeditionEffects.cs` | 粒子 / AOE / 刀光 / 藤蔓 / 烟雾 / 冲刺拖尾 |
| 渲染 | `ExpeditionRender*.cs` + `Canvas2D.cs` | 整帧软件光栅器 + 地形块缓存 + 迷雾 |
| UI | `UIHost.cs` | OnGUI：主菜单 / 农场 / 准备 / 远征 HUD / 结算 / 工坊 / 提示 |
| 主流程 | `GameFlow.cs` | 状态机、60Hz 固定步长、输入、渲染呈现 |
| 音频 | `AudioManager.cs` | 程序化背景乐 + 命中打击音效 |
| 测试 | `SmokeTest.cs` | 批处理自动战斗模拟验证 |
| 构建 | `Editor/BuildPlayer.cs` | 一键 Build Windows64 |

## 性能优化（相对网页版）

- 原生 Windows 程序，去掉浏览器开销，固定帧更稳定
- 固定 60Hz 模拟 + 渲染分离
- 地形预烘焙成 512px 块缓存（只计算一次）
- 空间哈希载入 + 视野剔除
- 逐帧列表改用索引压缩（避免 LINQ / 数组过滤的 GC 分配）
- 小地图纹理、障碍物前后层列表复用（减少逐帧分配）

## 美术资源

- 角色 / 怪物 / 障碍为精灵，带朝向、血条、伤害跳字、交互提示
- Boss 与地图使用斜俯视伪 3D 素材（`Assets/StreamingAssets/sprites/`，PNG 版）
- 战争迷雾、光柱为自定义 Shader（`Assets/Resources/Shaders/`）

## 已知限制 / 待办

1. UI 使用 OnGUI 实现（中文用系统字体渲染），后续可迁移到 UGUI + 图标字体以获得更精致界面
2. Boss 当前使用程序化轮廓绘制，可接入 `StreamingAssets/sprites` 的原始 PNG 素材获得更精细表现
3. 触屏 / 移动端适配未做（当前仅 Windows 桌面）

## 项目作者

个人独立项目：玩法设计、程序实现、界面、数值、测试与构建均由个人完成。
