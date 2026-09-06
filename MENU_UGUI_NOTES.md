# 主菜单 OnGUI → UGUI 迁移说明（第 2 轮增量）

目标：把主菜单（`screen=="menu"`）从 IMGUI（`UIHost.DrawMenu`）迁到 UGUI，分辨率自适应、不再发糊；**逻辑层一行未改**，其余屏幕暂留 OnGUI，后续逐屏迁移。

## 一、改动文件

| 文件 | 类型 | 说明 |
| --- | --- | --- |
| `Assets/Scripts/UI/MainMenuUI.cs` | 新增 | 运行时程序化生成的 UGUI 主菜单（沿用 UIRoot/UIFactory/FontLoader，不手编场景/prefab） |
| `Assets/Scripts/Game/UIHost.cs` | 修改 3 处 | ① DrawUI 开头 `MainMenuUI.Sync(gf.screen)`；② menu 屏不再用 OnGUI 铺背景；③ `case "menu"` 不再调 DrawMenu（方法保留备查）。farm/prep/expedition/result 等其余屏原样不动 |

## 二、元素与行为一一对应（保真迁移）

| 旧 OnGUI（DrawMenu） | 新 UGUI（MainMenuUI） | 行为 |
| --- | --- | --- |
| menuBackdrop 竖向渐变背景 | 全屏 Image + 运行时生成的同色竖向渐变 Sprite（顶 #092020→底 #0F141F） | 还原原配色 |
| 标题「🌾 农庄牌」44 号绿 | 64 号 Bold #7fff7f + 上下绿色分隔线 | 纯展示 |
| 副标题「荒 野 远 征」 | 30 号 #aaccaa | 纯展示 |
| 「农场经营 × 卡牌构筑 × 搜打撤撤离」 | 22 号 #aaccaa | 纯展示 |
| 「🌱 开始游戏」按钮 | 480×78 深绿主按钮 | `GameFlow.I.StartGame()`（不变） |
| 「📖 游戏说明」按钮 | 480×66 次按钮 | `AddToast("WASD移动…","success")`（不变） |
| 特色行「物资仓库·育种温室·T1-T4远征·双撤离」 | 20 号 #8ac8d8 | 纯展示 |
| 底部「v1.9 · 农场卡牌 · 荒野远征」 | 底部居中 16 号 #aeb8ae | 纯展示 |
| （无） | 右上角新增「⚙ 设置 (F10)」 | 调 `SettingsPanel.ToggleInstance()`，桌面习惯补一个显式入口（F10 仍可用） |

- 布局：挂在 UIRoot 的 1920×1080 CanvasScaler 下，Match=0.5，任意分辨率等比自适应，解决旧 1280×720 矩阵拉伸发糊。
- 显隐：`Sync(screen)` 幂等——进入 menu 显示、切到任意其它屏自动隐藏；主菜单的 toast（如点"游戏说明"）仍由原 OnGUI toast 通道正常显示。
- 字体：走 FontLoader（打包思源黑体优先，开发期回退系统动态字体），不随包分发微软雅黑。
- 额外收益：UGUI 按钮自带 EventSystem 导航，手柄方向键可在主菜单移动选择。

## 三、验证结论（真实跑过，非"应该能跑"）

1. 脚本编译：`compile_menu2.log` —— **0 error，本次新增代码 0 warning**（修复了 1 处真实报错：legacy LayoutElement 无 preferredSize，改用 preferredWidth/preferredHeight）。
2. IL2CPP 发布出包：`BuildPlayer.ReleaseCLI` —— **Succeeded / 错误 0 / 558 文件 / 738.2s**；`Build/standalone/GameAssembly.dll`（40.46MB）时间戳已更新为本轮，证明新主菜单确实进包；FarmCards.exe 可直接双击运行查看。
3. 逻辑层零改动（硬约束，文件时间戳为证）：`Expedition.cs`=2026-08-22、`ExpeditionCombat.cs`=2026-09-01、`GameConfig.cs`=2026-09-01，均非本轮改动。
4. 构建报告里的 2 个 warning（`Canvas2D.cs:66` 未用局部函数、`UIHost.cs:233` 结算屏未用变量 rowind）是**工程基线既有**，不在主菜单范围，未擅自改动；需要的话可下一轮顺手清掉。

## 四、仍为 OnGUI、待后续逐屏迁移的界面

农场主页 DrawFarm、远征准备 DrawPrep、远征 HUD DrawHUD、结算 DrawResult、卡牌工坊 DrawWorkshop、仓库 DrawWarehouse、温室 DrawGreenhouse，以及 toast/掉落横幅/受击红屏、GameFlow 的世界伤害跳字。迁移方式与本轮完全一致：新建对应 UGUI 屏 → DrawUI 对应 case 切换 → 编译+出包验证。

## 五、你现在怎么验收

双击 `Build/standalone/FarmCards.exe`，首屏即新 UGUI 主菜单；点「开始游戏」进农场（农场仍是旧 OnGUI，符合本轮范围），农场内「返回菜单」会回到新主菜单；主菜单右上角或 F10 开设置。
