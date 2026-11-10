# Hisense Windows Display Switcher

Automatically switches a Windows PC between its monitor and a Hisense TV according to the TV's power state and selected HDMI input.

## Status

Early prototype. It is currently being validated on a Hisense 85E7N running VIDAA/Android 14.

## First-time setup

1. Enable Android/ADB debugging on the TV.
2. Run `HisenseDisplaySwitcher.exe` on the Windows PC.
3. Enter the TV IP address and approve the authorization prompt shown by the TV.
4. Select the HDMI input connected to the PC, then click **Capture current HDMI**.
5. Run both safe display tests. Each test restores the previous output after 12 seconds.
6. Save the configuration and start monitoring.

When the TV is awake and showing the calibrated HDMI input, Windows uses the TV only. Otherwise, Windows uses the PC monitor only.

## Build

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/win-x64
```

## Privacy

The application operates locally. It stores only its configuration under `%LOCALAPPDATA%\HisenseDisplaySwitcher` and does not send telemetry.

## License

MIT

