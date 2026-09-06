# 农场主页 + 远征 HUD：OnGUI → UGUI 迁移说明（第 3 轮增量）

继主菜单后，把**农场主页 DrawFarm** 与**远征 HUD DrawHUD** 两屏从 IMGUI 迁到 UGUI。**逻辑层一行未改**；准备大厅/结算/工坊/仓库/温室仍留 OnGUI。

## 一、改动文件

| 文件 | 类型 | 说明 |
| --- | --- | --- |
| `Assets/Scripts/UI/FarmUI.cs` | 新增 | 农场主页 UGUI（建一次、每帧刷新动态值） |
| `Assets/Scripts/UI/ExpeditionHUD.cs` | 新增 | 远征 HUD UGUI（透明叠加在 3D 世界上，每帧刷新） |
| `Assets/Scripts/UI/UIFactory.cs` | 增加 | 新增 `Place / PlacedPanel / PlacedLabel`：旧 1280×720 坐标 ×1.5 映射到 1920×1080，1:1 还原旧布局 |
| `Assets/Scripts/Game/UIHost.cs` | 小改 | DrawUI 加两屏 Sync；`case farm / expedition` 交给 UGUI；`CropGlyph/CropProgress/StatusIcon` 三个纯函数改 public 供 FarmUI 复用（实现不动）；DrawFarm/DrawHUD 方法保留备查 |

## 二、农场主页 FarmUI（与旧 DrawFarm 行为一致）

- 顶部：标题、金币/种子/材料徽章（实时）、返回菜单 → `BackToMenu()`。
- 6×6 共 36 块农田：锁定/可扩建（显示金币+材料价、`FarmSystem.UnlockPlot`）、已种（作物图标 + 缺水/虫害/杂草状态、生长进度条、成熟变金框，点击 `Tend/Harvest`）、空地（播种 `Plant`），逻辑与旧代码逐分支一致。
- 家园设施：卡牌工坊 `OpenWorkshop`、物资仓库、育种温室（`GreenhouseSystem.warehouseOpen/greenhouseOpen + Init`）。
- 家园补给站：生长催化剂数量、使用催化剂 `UseCatalyst`；每日奖励天数/领取状态与按钮文案、保障资格与领取，全部实时刷新。
- 荒野远征站：进入远征准备大厅 `OpenPrep`。
- 选择作物：按 `unlockedCrops` 生成，选中作物高亮金色，点击写 `selectedCrop` 并存档。
- 农场全屏渐变背景仍由原 OnGUI 通道铺（farmBackdrop），UGUI 面板叠在上面，观感与旧版一致、省重做。

## 三、远征 HUD ExpeditionHUD（与旧 DrawHUD 一致）

- 左上生命/能量双条（填充比例 + 数值）、顶部倒计时与「T级·地图名·危险度」、当前武器名 [Tab]。
- 底部 4 技能格（图标+按键+冷却秒数，冷却中置灰）、消耗品格（图标+按键+数量，有货金色/无货灰色）。
- 右上背包金币/种子实时统计；左中任务目标进度、兽潮预警/波次（激活变红）、区域事件。
- `Esc` 暂停遮罩「游戏已暂停 / 按 Esc 继续」。
- **关键修复**：HUD 是纯展示叠加层，所有面板/条的 `raycastTarget` 已关闭，不会挡住 3D 世界的鼠标攻击与交互（这是叠加型 HUD 最容易踩的坑）。

## 四、动态屏的实现方式（区别于主菜单静态屏）

UGUI 对象是持久的，不像 OnGUI 每帧重建。因此两屏都采用「**控件只创建一次 → MonoBehaviour.Update 每帧只改文本/填充比例/颜色，且值没变不重赋值**」，避免每帧 GC 与文本重建开销；显隐仍由 `UIHost.DrawUI` 每帧 `Sync(screen)` 幂等控制，切屏自动隐藏。

## 五、验证结论（真实跑过）

1. 脚本编译 `compile_farmhud.log`：**0 error，本轮新增代码 0 warning**。
2. IL2CPP 发布出包 `BuildPlayer.ReleaseCLI`：**Succeeded / 错误 0 / 558 文件 / 431.9s**；`Build/standalone/GameAssembly.dll`（40.5MB）时间戳更新为本轮，证明两屏确实进包，FarmCards.exe 可直接运行。
3. 逻辑层零改动（文件时间戳为证）：`Expedition.cs`=2026-08-22、`ExpeditionCombat.cs`/`GameConfig.cs`=2026-09-01，均非本轮改动。
4. 构建报告 2 个 warning（`Canvas2D.cs:66`、`UIHost.cs:233` 结算屏旧变量 rowind）仍是工程基线既有，不在本次范围。

## 六、UI 迁移进度与剩余

- 已迁 UGUI：**主菜单、农场主页、远征 HUD、设置菜单**。
- 仍为 OnGUI：远征准备 DrawPrep、结算 DrawResult、卡牌工坊 DrawWorkshop、物资仓库 DrawWarehouse、育种温室 DrawGreenhouse、toast/掉落横幅/受击红屏、世界伤害跳字与小地图。
- 迁移套路已完全固化（UIFactory.Place 搬坐标 + 建一次/每帧刷新 + case 切换 + 编译出包验证），后续每屏可同法快速推进。

## 七、验收方式

运行 `Build/standalone/FarmCards.exe`：主菜单→开始游戏进入**新农场主页**（点农田播种/收获、切作物、进各设施）→进入远征后看到**新 HUD**（血能条、技能冷却、暂停遮罩），Esc 暂停、攻击交互不受 HUD 遮挡。
