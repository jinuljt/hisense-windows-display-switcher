# Hisense Windows Display Switcher

[English](#english)

让同一台 Windows 游戏电脑在桌面显示器和海信电视之间自动切换，不再手动按 `Win + P`。

## 应用场景

例如，一台客厅游戏电脑同时连接：

- DP → 桌面显示器
- HDMI → 海信电视

当电视打开并切换到连接游戏电脑的 HDMI 输入时，程序自动让 Windows **仅输出到电视**，适合坐到沙发上用手柄玩 Steam 游戏。

当电视关机、待机，或者切换到机顶盒、游戏主机等其他 HDMI 输入时，程序自动让 Windows **仅输出到桌面显示器**。电脑不会继续把画面送到无人观看的电视，也不需要每次手动修改显示设置。

```text
电视已唤醒 + 当前是游戏电脑 HDMI  →  仅电视
电视关机、待机或切到其他 HDMI    →  仅桌面显示器
```

## 当前状态

早期原型，正在海信 85E7N（VIDAA / Android 14）上进行实机验证。其他型号是否兼容取决于电视提供的 ADB 命令和输入源状态。

## 首次配置

1. 在电视上启用 Android / ADB 调试。
2. 在 Windows 游戏电脑上运行 `HisenseDisplaySwitcher.exe`。
3. 输入电视 IP，并在电视上允许 ADB 授权。
4. 把电视切换到连接这台电脑的 HDMI 输入，点击 **Capture current HDMI** 自动记录输入源编号。
5. 分别运行电视和显示器安全测试；每次测试会在 12 秒后自动恢复，避免黑屏后无法操作。
6. 保存配置并启动监控。程序可以在当前 Windows 用户登录时自动运行。

程序首次运行时会自动下载 Google Android Platform Tools。无需另外安装 .NET、PowerShell 或 Python。

## 隐私

程序只在本地运行，不收集遥测。配置保存在 `%LOCALAPPDATA%\HisenseDisplaySwitcher`。

## 从源码构建

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/win-x64
```

## English

Automatically route a Windows gaming PC between a desk monitor and a Hisense TV—without pressing `Win + P` every time.

### Typical setup

- DisplayPort → desk monitor
- HDMI → Hisense TV

When the TV is awake and showing the HDMI input connected to the gaming PC, Windows switches to **TV only** for couch gaming. If the TV is turned off, enters standby, or changes to another HDMI input, Windows automatically returns to **monitor only**.

```text
TV awake + gaming-PC HDMI selected  →  TV only
TV off, asleep, or another input     →  monitor only
```

### First-time setup

1. Enable Android / ADB debugging on the TV.
2. Run `HisenseDisplaySwitcher.exe` on the Windows PC.
3. Enter the TV IP address and approve the authorization prompt shown by the TV.
4. Select the HDMI input connected to the PC, then click **Capture current HDMI**.
5. Run both safe display tests. Each test restores the previous output after 12 seconds.
6. Save the configuration and start monitoring.

The current prototype is being validated on a Hisense 85E7N running VIDAA / Android 14. Compatibility with other models depends on the ADB commands exposed by the TV.

### Privacy

The application operates locally, sends no telemetry, and stores its configuration under `%LOCALAPPDATA%\HisenseDisplaySwitcher`.

## License

MIT

