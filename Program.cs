using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Win32;

namespace HisenseDisplaySwitcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed record AppConfig(string TvHost = "", int AdbPort = 5555, int TargetInput = 1284, int PollSeconds = 2);

internal sealed class MainForm : Form
{
    private static readonly string DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HisenseDisplaySwitcher");
    private static readonly string ConfigPath = Path.Combine(DataDir, "config.json");
    private static readonly string AdbPath = Path.Combine(DataDir, "platform-tools", "adb.exe");
    private readonly TextBox host = new() { PlaceholderText = "TV IP address, for example 192.168.1.20", Dock = DockStyle.Fill };
    private readonly NumericUpDown target = new() { Minimum = 1, Maximum = 9999, Value = 1284, Dock = DockStyle.Fill };
    private readonly TextBox log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Button monitorButton = new() { Text = "Start monitoring", AutoSize = true };
    private readonly System.Windows.Forms.Timer timer = new();
    private AppConfig config = new();
    private string currentTarget = "";

    public MainForm()
    {
        Text = "Hisense Windows Display Switcher";
        Width = 720;
        Height = 520;
        StartPosition = FormStartPosition.CenterScreen;

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        AddButton(buttons, "Connect / authorize", async (_, _) => await ConnectAsync());
        AddButton(buttons, "Capture current HDMI", async (_, _) => await CaptureAsync());
        AddButton(buttons, "Test TV output", async (_, _) => await SafeDisplayTestAsync("/external", "/internal"));
        AddButton(buttons, "Test monitor output", async (_, _) => await SafeDisplayTestAsync("/internal", "/external"));
        AddButton(buttons, "Save + start with Windows", SaveAndInstall);
        monitorButton.Click += ToggleMonitor;
        buttons.Controls.Add(monitorButton);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "TV address", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(host, 1, 0);
        layout.Controls.Add(new Label { Text = "Windows HDMI source ID", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(target, 1, 1);
        layout.Controls.Add(buttons, 0, 2);
        layout.SetColumnSpan(buttons, 2);
        layout.Controls.Add(log, 0, 3);
        layout.SetColumnSpan(log, 2);
        Controls.Add(layout);

        timer.Tick += async (_, _) => await PollAsync();
        LoadConfig();
        Shown += async (_, _) => { if (!string.IsNullOrWhiteSpace(config.TvHost)) await StartMonitoringAsync(); };
    }

    private static void AddButton(Control parent, string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += click;
        parent.Controls.Add(button);
    }

    private void LoadConfig()
    {
        Directory.CreateDirectory(DataDir);
        if (File.Exists(ConfigPath)) config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath)) ?? new();
        host.Text = config.TvHost;
        target.Value = Math.Clamp(config.TargetInput, (int)target.Minimum, (int)target.Maximum);
    }

    private async Task EnsureAdbAsync()
    {
        if (File.Exists(AdbPath)) return;
        Append("Downloading Google Android Platform Tools...");
        var zip = Path.Combine(Path.GetTempPath(), "platform-tools-latest-windows.zip");
        using var http = new HttpClient();
        await File.WriteAllBytesAsync(zip, await http.GetByteArrayAsync("https://dl.google.com/android/repository/platform-tools-latest-windows.zip"));
        ZipFile.ExtractToDirectory(zip, DataDir, true);
        File.Delete(zip);
        Append("ADB installed.");
    }

    private async Task<string> AdbAsync(params string[] arguments)
    {
        await EnsureAdbAsync();
        var start = new ProcessStartInfo(AdbPath) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start ADB.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (await stdout) + (await stderr);
    }

    private async Task ConnectAsync()
    {
        try
        {
            var address = Address();
            Append((await AdbAsync("connect", address)).Trim());
            Append("If the TV asks for authorization, approve it and click again.");
        }
        catch (Exception ex) { Append("ERROR: " + ex.Message); }
    }

    private async Task<TvState> ReadStateAsync()
    {
        var address = Address();
        await AdbAsync("connect", address);
        var command = "echo PORT=$(getprop persist.sys.last_source); echo SOURCE=$(hspdebugtools instant_call -c GetCurSourceId -a READ -p '{}' 2>/dev/null | od -An -tu1 | awk 'NF{print $1; exit}'); dumpsys power | grep mWakefulness=";
        return TvState.Parse(await AdbAsync("-s", address, "shell", command));
    }

    private async Task CaptureAsync()
    {
        try
        {
            var state = await ReadStateAsync();
            Append($"Captured: awake={state.Awake}, source={state.Source}, input={state.Port}");
            if (state.Port > 0) target.Value = state.Port;
        }
        catch (Exception ex) { Append("ERROR: " + ex.Message); }
    }

    private async Task SafeDisplayTestAsync(string testArgument, string restoreArgument)
    {
        RunDisplaySwitch(testArgument);
        Append($"Testing {testArgument}; automatically restoring in 12 seconds.");
        await Task.Delay(TimeSpan.FromSeconds(12));
        RunDisplaySwitch(restoreArgument);
        Append("Display restored. If the test was correct, continue setup.");
    }

    private void SaveAndInstall(object? sender, EventArgs e)
    {
        try
        {
            SaveConfig();
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)!;
            key.SetValue("HisenseDisplaySwitcher", $"\"{Application.ExecutablePath}\"");
            Append("Configuration saved. The app will start when this user signs in.");
        }
        catch (Exception ex) { Append("ERROR: " + ex.Message); }
    }

    private void SaveConfig()
    {
        config = new(host.Text.Trim(), 5555, (int)target.Value, 2);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        timer.Interval = config.PollSeconds * 1000;
    }

    private async void ToggleMonitor(object? sender, EventArgs e)
    {
        if (timer.Enabled)
        {
            timer.Stop();
            monitorButton.Text = "Start monitoring";
            Append("Monitoring stopped.");
        }
        else await StartMonitoringAsync();
    }

    private async Task StartMonitoringAsync()
    {
        try
        {
            SaveConfig();
            await EnsureAdbAsync();
            timer.Interval = config.PollSeconds * 1000;
            timer.Start();
            monitorButton.Text = "Stop monitoring";
            Append("Monitoring started.");
            await PollAsync();
        }
        catch (Exception ex) { Append("ERROR: " + ex.Message); }
    }

    private async Task PollAsync()
    {
        timer.Stop();
        try
        {
            var state = await ReadStateAsync();
            var desired = state.IsTarget(config.TargetInput) ? "tv" : "monitor";
            Append($"awake={state.Awake} source={state.Source} input={state.Port} target={desired}");
            if (desired != currentTarget)
            {
                RunDisplaySwitch(desired == "tv" ? "/external" : "/internal");
                currentTarget = desired;
            }
        }
        catch (Exception ex) { Append("ERROR: " + ex.Message); }
        finally { if (monitorButton.Text == "Stop monitoring") timer.Start(); }
    }

    private static void RunDisplaySwitch(string argument) => Process.Start(new ProcessStartInfo("DisplaySwitch.exe", argument) { UseShellExecute = true });

    private string Address()
    {
        var value = host.Text.Trim();
        if (!System.Net.IPAddress.TryParse(value, out _)) throw new InvalidOperationException("Enter a valid TV IP address.");
        return $"{value}:5555";
    }

    private void Append(string message)
    {
        log.AppendText($"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
    }
}

