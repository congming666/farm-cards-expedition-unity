# 《农场卡牌：荒野远征》Steam 化改造说明（第 1 轮）

目标：把 Unity 移植版从“能跑的翻译式移植”推进到“可在 Steam 发行的桌面游戏”基线。
原则：**玩法逻辑层（Expedition / ExpeditionCombat / GameConfig）一行未改**，所有改造都在渲染、平台、存档、UI、构建层。

---

## 一、本轮交付清单

### 1. 渲染层去软件化（GPU 收尾）
| 文件 | 改动 |
| --- | --- |
| `Assets/Scripts/Render/WorldFx.cs`（新增） | GPU 粒子桥：对象池 `SpriteRenderer` 消费逻辑层 `Expedition.particles`（aoe/slash/spark/chaff/vine/earthTrail/smoke/weaponRing），软圆点/圆环贴图运行时生成、按生命衰减缩放与透明度。补上了原先只在已停用软件链路里才看得到的战斗特效 |
| `Assets/Scripts/Game/WorldRenderer.cs`（改） | Build 时创建 WorldFx、Tick 时同步、Clear 时释放；地面烘焙/角色怪物/迷雾本就是 GPU，维持不变 |
| `Assets/Scripts/Game/RenderBackend.cs`（改） | 两张 720p 软件像素缓冲改为**懒加载**，启动不再常驻约 7.4MB 托管数组；死软渲染链路保持无调用方 |
| `Assets/Scripts/Game/GameFlow.cs`（改） | 小地图由每帧 `SetPixels+Apply` 降到 **10Hz** 重建（纹理仍每帧显示） |

> 已核实：世界渲染（SpriteRenderer + 正交斜俯视相机 + URP Volume）与迷雾（512 R8 RenderTexture + 两个 Shader）此前已是 GPU，无需重做。

### 2. UI：OnGUI → UGUI（框架 + 设置菜单样板）
| 文件 | 作用 |
| --- | --- |
| `Assets/Scripts/UI/UIRoot.cs` | 运行时生成 Canvas + CanvasScaler（1920×1080 参考分辨率，Match 0.5），解决 IMGUI 拉伸发糊；按“仅新输入系统”正确挂 `InputSystemUIInputModule` |
| `Assets/Scripts/UI/UIFactory.cs` | 面板/文本/按钮/滑条/开关/步进选择器统一工厂（统一字体、配色、布局） |
| `Assets/Scripts/UI/FontLoader.cs` | 优先加载 `Resources/Fonts/SourceHanSans`（可商用思源黑体），缺失时回退系统动态字体仅供开发 |
| `Assets/Scripts/UI/SettingsPanel.cs` | 完整设置菜单：分辨率 / 显示模式 / VSync / 帧率上限 / 画质 / 主·音乐·音效音量 / 伤害数字 / 屏幕震动 / 恢复默认 / 应用保存 / 退出游戏。**F10 全局呼出**，打开时暂停模拟 |

### 3. 存档系统 v2
`Assets/Scripts/Game/SaveSystem.cs` 重写，**对外 `Load()/Save()` 与全部查询函数签名不变**：
- schema `version` 升至 2 + `MigrateData` 迁移钩子；自动把旧单文件 `farm-cards-save.json` 一次性导入 0 号槽；
- **3 个存档位**（`SelectSlot/LoadSlot/SaveSlot/HasSlot/DeleteSlot/EnumerateSlots`）；
- **原子写**：先写 `.tmp` 再覆盖、旧文件留 `.bak`；主文件损坏自动回退备份（`AtomicFile.cs`）；
- 为阶段 2 Steam 云存档预留 `savedAtUnix` 与云端同步接口。

### 4. 桌面标配
| 文件 | 能力 |
| --- | --- |
| `Platform/GameSettings.cs` | 设置数据持久化 `settings.json` + `Apply()`（分辨率/全屏/VSync/帧率/画质/主音量） |
| `Platform/CrashLogger.cs` | 挂钩 `logMessageReceivedThreaded` + `AppDomain.UnhandledException` + 未观察 Task 异常，落盘到 `persistentDataPath/logs`，含硬件/版本头 |
| `Platform/AppBootstrap.cs` | `BeforeSceneLoad` 最早引导：崩溃日志→设置→Steam；常驻 `AppDriver` 每帧驱动、失焦/退出自动存盘与云同步 |
| 设置菜单 | `runInBackground`、正常退出（退出前自动存档） |

### 5. Steam 平台层 + 出包
| 文件 | 作用 |
| --- | --- |
| `Platform/Steam/SteamPlatform.cs` | `#if STEAMWORKS` 隔离 Steamworks.NET：Init/Tick/Shutdown、成就、统计、Rich Presence、云存档；**无 DLL 时全部 NoOp，工程照常编译出包** |
| `Editor/BuildPlayer.cs`（重写） | 菜单/CLI 构建：显式关 Development、**IL2CPP + x86_64 + Master 优化 + Minimal 裁剪**、产品/公司/标识、构建报告；另提供 Steam 构建（注入 STEAMWORKS）与仅编译检查 |
| `Steam/app_build_480.vdf`、`depot_481.vdf`、`steam_appid.txt`、`README_SteamPipe.md` | SteamPipe 上传模板（含 AutoCloud 云存档路径、商店素材尺寸、费用与合规说明） |
| `ProjectSettings.asset` | 公司 `FarmCards Studio`、产品 `FarmCards Expedition`、标识 `com.farmcards.expedition`、默认 1920×1080、`runInBackground=1`、Standalone IL2CPP、版本 0.2.0 |

---

## 二、怎么用

- **设置菜单**：游戏内任意时刻按 `F10`（再按一次或点关闭）；改分辨率/音量即时生效。
- **出包**：Unity 菜单 `Build / Windows64 发布构建(IL2CPP)`，产物在 `Build/standalone/FarmCards.exe`；
  命令行：
  ```
  Unity.exe -batchmode -quit -projectPath <工程> -executeMethod BuildPlayer.ReleaseCLI -logFile build.log
  ```
- **接 Steam**：按 `Steam/README_SteamPipe.md` 放入 `Steamworks.NET.dll`，用 `Build/...(Steam/STEAMWORKS)` 出包，再用 steamcmd 上传 depot。

## 三、仍需增量迭代（不阻塞当前编译/出包）
1. 世界实体补 SpriteRenderer：宝箱 / 防御塔 / 撤离点 / 地面掉落 / 弹道 / 陷阱（目前只在小地图与交互提示出现）；
2. UIHost 其余 OnGUI 屏（主菜单/农场/整备/结算）以设置菜单为样板逐屏迁 UGUI；伤害跳字同步迁移；
3. 键位重绑定的实际输入映射（数据结构与默认表已就位）；音乐/音效分通道接 AudioManager；
4. **发布前合规**：放入可商用思源黑体、确认 LowPolyForestPack 商用授权、替换 Steam AppID/配置成就、制作游戏图标与商店素材。

## 四、验证（Unity 6000.0.82f1，真实 batchmode）
- 脚本编译验证：连续编译，最终 **0 error / 本轮新增代码 0 warning**；
- **IL2CPP 发布出包成功**：`Build/Windows64 发布构建(IL2CPP)` 等价 CLI 全流程跑通，
  报告 `Succeeded / 错误 0 / 警告 0 / 558 文件 / 耗时约 417.6s`；
  产物 `Build/standalone/FarmCards.exe`，其中 `GameAssembly.dll`(约 40MB) 即 IL2CPP 原生产物（Mono 后端不会有此文件，可据此确认后端生效）；
- 出包目录里的 `*_BackUpThisFolder_ButDontShipItWithYourGame`、`*_BurstDebugInformation_DoNotShip` 是 IL2CPP/Burst 本地调试符号，**不随 Steam depot 下发**（depot 已排除），玩家实际下载体积显著小于出包目录。
