# Steam 接入与上线路径（Steamworks.NET + SteamPipe）

本目录是平台层交付模板。游戏侧已用 `Assets/Scripts/Platform/Steam/SteamPlatform.cs`
把 Steam 能力隔离在 `#if STEAMWORKS` 之后：**没有 SDK 也能正常编译、正常出包**；
放入 DLL 并启用编译符号后即自动生效，玩法逻辑一行都不用改。

## 1. 引入 Steamworks.NET（三选一）

- 推荐：从 https://github.com/rlabrecque/Steamworks.NET/releases 下载 `Steamworks.NET.dll`
  （Standalone 用 Windows x64 版本），放到 `Assets/Plugins/Steamworks.NET.dll`；
- 或用 UPM：在 Package Manager 里 “Add package from git URL” 填 Steamworks.NET 的 git 地址；
- 回到 Unity 等待编译通过。

## 2. 启用 STEAMWORKS 编译符号

- 临时（推荐，不污染工程）：菜单 `Build/Windows64 发布构建(Steam/STEAMWORKS)`，
  构建脚本通过 `extraScriptingDefines` 仅对本次构建注入 `STEAMWORKS`；
- 长期：Project Settings → Player → Other Settings → Scripting Define Symbols 加 `STEAMWORKS`。

## 3. 开发联调 AppID

- `steam_appid.txt` 内容为 `480`（Valve 公共测试 AppID = Spacewar），仅用于本地联调；
- 编辑器运行时把它放在**工程根目录**；出包后放在 **FarmCards.exe 同目录**；
- 联调时本机需登录并运行 Steam 客户端；
- 拿到自己的 AppID 后，把本文件、`app_build_480.vdf`、`depot_481.vdf` 里的 480/481 全部替换。

## 4. 已封装的能力（SteamPlatform.cs）

| 能力 | API | 在哪里接 |
| --- | --- | --- |
| 初始化/回调/释放 | `Init / Tick / Shutdown` | AppBootstrap、AppDriver 已自动调用 |
| 成就 | `UnlockAchievement("api_name")` | 在击杀 Boss / 首次通关等节点调用 |
| 统计 | `SetStat(name,int/float)` | 累计击杀、远征次数等 |
| Rich Presence | `SetPlayState("正在 T2 远征")` | 进入远征时调用，好友列表可见 |
| 云存档 | `SyncSavesToCloud / UploadFileToCloud` | 退出时 AppDriver 已自动同步存档槽 |

成就 / 统计的名称需要先在 Steamworks 后台 “Stats and Achievements” 配置，名字保持一致。

## 5. 用 SteamPipe 上传 Depot

1. 安装 Steamworks SDK 的 `tools/contentbuilder`（含 steamcmd.exe）；
2. Unity 菜单 `Build/Windows64 发布构建(Steam/STEAMWORKS)` 出包到 `Build/standalone`；
3. 第一次把 `app_build_480.vdf` 里 `"preview"` 设为 `1`，执行：
   ```
   steamcmd.exe +login <Steamworks后台账号> +run_app_build_http "<...>\Steam\app_build_480.vdf" +quit
   ```
   检查输出的文件清单是否正确（不含 pdb / 调试文件）；
4. 确认无误把 `preview` 改回 `0` 正式上传，再在后台把构建设到对应分支。

## 6. 商店素材尺寸（2026 现行，旧 460×215 已停用）

- 小胶囊 462×174、页头胶囊 920×430、主胶囊 1232×706、竖胶囊 748×896
- 库胶囊 600×900、库 Hero 3840×1240(PNG)、库 Logo 1280 宽/720 高(透明 PNG)
- 截图建议 1920×1080(16:9)

## 7. 费用与合规

- Steam Direct 每款产品一次性 $100；收入累计达到 $1000 后在分成中返还；
- 随包字体必须可商用：本项目已改为优先加载 `Assets/Resources/Fonts/SourceHanSans`，
  请放入**思源黑体**（SIL OFL 可商用）；微软雅黑/苹方禁止随商业包分发；
- `LowPolyForestPack` 等第三方美术资源请逐一核对 License 并保留购买/授权凭证。
