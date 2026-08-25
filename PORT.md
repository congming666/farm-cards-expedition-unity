# 农场卡牌：荒野远征 — Unity 6 移植说明

## 状态
- Unity 版本：6000.0.82f1（URP）
- 编译：通过（0 错误）
- Windows 构建：通过 → `Build/FarmCards.exe`（约 111MB）
- 冒烟测试：通过（农场初始化/播种、远征地图生成、怪物AI、兽潮、战斗、结算全流程）
- 启动验证：通过（正常进入主菜单）

## 运行
- 直接运行 `Build/FarmCards.exe`
- 源码与工程：`My project (2)`

## 已移植系统（对照网页版）
| 系统 | 文件 | 说明 |
|---|---|---|
| 数据配置 | GameConfig.cs | 武器/地图/技能/消耗品/怪物/作物 全量数据 |
| 存档 | SaveSystem.cs | JsonUtility → persistentDataPath/save.json |
| 农场 | CardFarmReward.cs | 6x6地块、种植/生长/状态(旱/虫/草)、收获、扩建、催化剂 |
| 卡牌工坊 | CardFarmReward.cs | 品质掉落、技能强化卡、携带卡、消耗卡 |
| 每日奖励 | CardFarmReward.cs | 7天连签、开荒保障 |
| 远征(核心) | Expedition.cs / Combat / Terrain / Render | 地图程序化生成、地形块烘焙、摄像机、战争迷雾 |
| 战斗 | ExpeditionCombat.cs | 3武器、4技能、3消耗品、5类怪+精英、兽潮、防御塔、Boss |
| 特效 | ExpeditionEffects.cs | 粒子/AOE/刀光/藤蔓/烟雾/冲刺拖尾 |
| 渲染 | ExpeditionRender*.cs + Canvas2D.cs | 整帧软件光栅器 + 地形块缓存 + 迷雾 |
| UI | UIHost.cs | OnGUI：主菜单/农场/准备/远征HUD/结算/工坊/提示 |
| 主流程 | GameFlow.cs | 状态机、60Hz固定步长、输入、渲染呈现 |
| 音频 | AudioManager.cs | 程序化背景乐 + 命中打击音效 |
| 测试 | SmokeTest.cs | 批处理自动战斗模拟验证 |
| 构建 | Editor/BuildPlayer.cs | 一键 Build Windows64 |

## 性能优化（相对网页版）
- 原生 Windows 程序（去浏览器开销、稳定固定帧）
- 固定 60Hz 模拟 + 渲染分开
- 地形预烘焙成 512px 块缓存（只计算一次）
- 空间哈希载入 + 视野剔除
- 逐帧列表改用索引压缩（避免 LINQ/数组过滤的 GC 分配）
- 小地图纹理、障碍物前后层列表复用（减少逐帧分配）

## 已知限制 / 待办
1. **视觉未人工核验**：我无法截图查看，画面观感(坐标/迷雾/配色)请运行后反馈，我可即时修。
2. **UI 用 OnGUI 实现**（中文用微软雅黑动态字体，部分 emoji 可能显示为方块），后续可迁移到 UGUI + 图标字体以获得更精致界面。
3. Boss 使用程序化轮廓绘制；后续可接入已转换的 PNG 精灵(assets/StreamingAssets/sprites)获得原素材伪3D Boss。
4. 触屏/移动端适配未做（当前仅 Windows 桌面）。

## 重新构建
在 Unity 打开 `My project (2)`，菜单 Build → Windows64；或命令行：
```
Unity.exe -batchmode -quit -projectPath "My project (2)" -executeMethod BuildPlayer.Build
```
