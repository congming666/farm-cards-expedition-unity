# UGUI 全量迁移说明（OnGUI → UGUI，剩余界面一次性迁完）

本轮把项目中**所有剩余 OnGUI(IMGUI) 界面**全部迁到 UGUI，至此主战斗流程与全部边缘界面 100% UGUI 化，OnGUI 仅保留背景纹理铺设与已废弃方法（保留备查，不再被调用）。

## 一、本轮迁移清单

| 原 OnGUI 方法 | 所在文件 | 新 UGUI 类 | 驱动方式 |
|---|---|---|---|
| `DrawResult`（远征结算） | UIHost.cs | `ResultUI.cs` | `screen=="result"` |
| `DrawWorkshop`（卡牌工坊） | UIHost.cs | `WorkshopUI.cs` | `UIHost.workshopOpen` |
| `DrawWarehouse`（物资仓库） | UIHost.cs | `WarehouseUI.cs` | `GreenhouseSystem.warehouseOpen` |
| `DrawGreenhouse`（育种温室） | UIHost.cs | `GreenhouseUI.cs` | `GreenhouseSystem.greenhouseOpen` |
| `DrawToasts`（全局提示） | UIHost.cs | `GlobalOverlay.cs` | 常驻，每帧刷新 |
| `DrawDropBanners`（掉落横幅） | UIHost.cs | `GlobalOverlay.cs` | 常驻，每帧刷新 |
| `signalFlash`（受击红屏） | UIHost.cs | `GlobalOverlay.cs` | 常驻，每帧刷新 |
| `DrawWorldOverlays`（伤害跳字+交互提示） | GameFlow.cs | `GlobalOverlay.cs` | 远征中激活 |
| `DrawMinimap`（小地图显示） | GameFlow.cs | `ExpeditionHUD.cs`（RawImage） | 远征中显示 |

加上前几轮已迁的主菜单 / 农场主页 / 远征准备大厅 / 远征 HUD / 设置菜单，**全部 14 个界面已 UGUI 化**。

## 二、新增文件

- `Assets/Scripts/UI/ResultUI.cs` — 结算页：全屏遮罩 + 大标题（成功绿/失败红）+ 6 项统计 + 战利品动态列表（kept 绿✓ / lost 红✗）+ 返回农场按钮
- `Assets/Scripts/UI/WorkshopUI.cs` — 卡牌工坊：4 技能当前等级/数值展示 + 卡牌库存 4 列网格（点击 `CardSystem.Apply`）+ 关闭
- `Assets/Scripts/UI/WarehouseUI.cs` — 物资仓库：容量条 + 一键出售作物 + 扩建 + 物品 4 列网格（出售/全部出售）+ 关闭
- `Assets/Scripts/UI/GreenhouseUI.cs` — 育种温室：稀有植物选择器 + 4×4 温室格（锁定/已种进度/空地，点击播种/收获/解锁）+ 右侧温室道具栏 + 提示 + 关闭
- `Assets/Scripts/UI/GlobalOverlay.cs` — 全局覆盖层（常驻）：toast 队列（5 条对象池）+ 掉落横幅（8 条对象池）+ 受击红屏 Image + 世界伤害跳字（80 条对象池，`Camera.WorldToScreenPoint` / `Canvas.scaleFactor` 换算）+ 交互提示

## 三、修改文件

- `Assets/Scripts/UI/ExpeditionHUD.cs` — 新增右下角小地图 `RawImage`（240×240），每帧把 `GameFlow.I.mmTex` 赋给它；纹理仍由 GameFlow 以 10Hz 生成上传
- `Assets/Scripts/Game/GameFlow.cs` —
  - `mmTex` 改为 `public`（供 ExpeditionHUD 读取）
  - `DrawMinimap()` 保留 10Hz 纹理生成，删除 `GUI.DrawTexture` 显示行
  - `OnGUI()` 删除 `DrawWorldOverlays()` 调用（迁 GlobalOverlay）
  - `InteractPrompt()` 改为 `public`（供 GlobalOverlay 调用，逻辑零改动）
- `Assets/Scripts/Game/UIHost.cs` — `DrawUI` 中 `case result` 与三个 overlay 全部 break，删除 `DrawToasts/DrawDropBanners/signalFlash` 的 OnGUI 调用，改为 `GlobalOverlay.Refresh(gf)`；新增 5 个 UGUI Sync 调用

## 四、设计要点

1. **控件建一次 + Update 每帧只改值**：所有 UGUI 屏遵循"首次显示时 Build 一次控件，之后每帧 Update 只更新文本/填充/颜色，值不变不重赋值"，避免每帧重建 GC。
2. **动态列表用签名缓存重建**：战利品、卡牌库存、仓库物品、温室道具等数量变化的列表，用内容签名（id+数量拼接）判断是否需要重建，不变时零开销。
3. **坐标映射**：沿用 `UIFactory.Place` 把旧 IMGUI 的 1280×720 左上坐标 ×1.5 映射到 1920×1080 参考分辨率，旧布局 1:1 搬运。
4. **世界坐标→UGUI 坐标**：伤害跳字和交互提示用 `Camera.main.WorldToScreenPoint` 取屏幕像素，再除以 `Canvas.scaleFactor` 转 UGUI 参考坐标；UGUI 左下原点与 WorldToScreenPoint 一致，y 无需翻转。
5. **小地图渲染与显示分离**：纹理生成（`Canvas2D` 10Hz `UploadTo`）仍在 GameFlow（属于渲染数据层），UGUI 只负责用 `RawImage` 显示，职责清晰。
6. **raycastTarget=false**：HUD 面板、toast、伤害跳字等纯显示元素全部 `raycastTarget=false`，避免挡住 3D 世界鼠标攻击。
7. **逻辑层零改动**：`Expedition.cs` / `ExpeditionCombat.cs` / `GameConfig.cs` 三个文件时间戳分别为 8/22、9/1、9/1，本轮未触碰任何一行。

## 五、验证结果

- **编译**：Unity batchmode 编译，0 CS 错误
- **出包**：IL2CPP Release 出包 `Build Successful`，Tundra 54 items updated，`GameAssembly.dll` 42.5MB 已更新
- **逻辑层零改动**：三文件时间戳核对通过
- **废弃方法保留**：`DrawResult/DrawWorkshop/DrawWarehouse/DrawGreenhouse/DrawToasts/DrawDropBanners/DrawWorldOverlays` 仍在源码中（private，未被调用，C# 不报警告），可随时对照或后续清理

## 六、启动方式

- **发布版**：双击 `Build/standalone/FarmCards.exe`
- **编辑器**：Unity 打开 `Assets/Scenes/SampleScene.unity`，点 Play

## 七、后续可选清理（非必须）

- 删除 UIHost.cs 中已废弃的 `DrawMenu/DrawFarm/DrawPrep/DrawHUD/DrawResult/DrawWorkshop/DrawWarehouse/DrawGreenhouse/DrawToasts/DrawDropBanners` 方法（约 300 行 dead code）
- 删除 GameFlow.cs 中 `DrawWorldOverlays` 方法
- UIHost.Init 中微软雅黑动态字体可改为思源黑体 TTF（FontLoader 已优先加载 Resources/Fonts/SourceHanSans）
