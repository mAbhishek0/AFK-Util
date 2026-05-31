using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace AfkBot;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const uint WM_SETICON = 0x0080;
    private static readonly IntPtr ICON_SMALL = new IntPtr(0);
    private static readonly IntPtr ICON_BIG = new IntPtr(1);
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x0010;

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const ushort SCAN_W = 0x11;
    private const ushort SCAN_A = 0x1E;
    private const ushort SCAN_S = 0x1F;
    private const ushort SCAN_D = 0x20;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public INPUTUNION union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL, wParamH;
    }

    private CancellationTokenSource? _cts;
    private readonly Random _rng = new();
    private int _loopCount;

    private static readonly (char Label, ushort ScanCode)[] Keys =
    [
        ('W', SCAN_W), ('A', SCAN_A), ('S', SCAN_S), ('D', SCAN_D),
    ];

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "AFK Bot";

        var iconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "app.ico");
        bool hasIcon = System.IO.File.Exists(iconPath);

        if (AppWindow is not null)
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(480, 700));
            if (hasIcon) try { AppWindow.SetIcon(iconPath); } catch { }
        }

        if (hasIcon)
        {
            try
            {
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (hWnd != IntPtr.Zero)
                {
                    IntPtr big = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 32, 32, LR_LOADFROMFILE);
                    IntPtr small = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
                    if (big != IntPtr.Zero) SendMessage(hWnd, WM_SETICON, ICON_BIG, big);
                    if (small != IntPtr.Zero) SendMessage(hWnd, WM_SETICON, ICON_SMALL, small);
                }
            }
            catch { }
        }

        if (this.Content is FrameworkElement root)
        {
            UpdateTitleBarTheme(root.ActualTheme);
            root.Loaded += (_, _) => UpdateTitleBarTheme(root.ActualTheme);
            root.ActualThemeChanged += (_, _) => UpdateTitleBarTheme(root.ActualTheme);
        }
    }

    private bool IsSystemDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int v) return v == 0;
        }
        catch { }
        return false;
    }

    private void UpdateTitleBarTheme(ElementTheme theme)
    {
        bool isDark = IsSystemDarkMode();

        if (AppWindow is not null && Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
        {
            try
            {
                var tb = AppWindow.TitleBar;
                if (isDark)
                {
                    var fg = ColorHelper.FromArgb(255, 201, 209, 217);
                    var bg = ColorHelper.FromArgb(255, 13, 17, 23);
                    var hover = ColorHelper.FromArgb(255, 48, 54, 61);
                    var press = ColorHelper.FromArgb(255, 33, 38, 45);
                    var inactive = ColorHelper.FromArgb(255, 139, 148, 158);

                    tb.BackgroundColor = bg;
                    tb.ForegroundColor = fg;
                    tb.ButtonBackgroundColor = Colors.Transparent;
                    tb.ButtonForegroundColor = fg;
                    tb.ButtonHoverBackgroundColor = hover;
                    tb.ButtonHoverForegroundColor = fg;
                    tb.ButtonPressedBackgroundColor = press;
                    tb.ButtonPressedForegroundColor = fg;
                    tb.InactiveBackgroundColor = bg;
                    tb.InactiveForegroundColor = inactive;
                    tb.ButtonInactiveBackgroundColor = Colors.Transparent;
                    tb.ButtonInactiveForegroundColor = inactive;
                }
                else
                {
                    var fg = ColorHelper.FromArgb(255, 36, 41, 47);
                    var bg = ColorHelper.FromArgb(255, 246, 248, 250);
                    var hover = ColorHelper.FromArgb(255, 208, 215, 222);
                    var press = ColorHelper.FromArgb(255, 234, 240, 246);
                    var inactive = ColorHelper.FromArgb(255, 87, 96, 106);

                    tb.BackgroundColor = bg;
                    tb.ForegroundColor = fg;
                    tb.ButtonBackgroundColor = Colors.Transparent;
                    tb.ButtonForegroundColor = fg;
                    tb.ButtonHoverBackgroundColor = hover;
                    tb.ButtonHoverForegroundColor = fg;
                    tb.ButtonPressedBackgroundColor = press;
                    tb.ButtonPressedForegroundColor = fg;
                    tb.InactiveBackgroundColor = bg;
                    tb.InactiveForegroundColor = inactive;
                    tb.ButtonInactiveBackgroundColor = Colors.Transparent;
                    tb.ButtonInactiveForegroundColor = inactive;
                }
            }
            catch { }
        }

        try
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            if (hWnd != IntPtr.Zero)
            {
                int dm = isDark ? 1 : 0;
                DwmSetWindowAttribute(hWnd, 20, ref dm, sizeof(int));
                DwmSetWindowAttribute(hWnd, 19, ref dm, sizeof(int));
            }
        }
        catch { }
    }

    private void SlideUpFadeIn(UIElement el, float startY = 4f, float startOpacity = 0.5f, int durationMs = 250)
    {
        var vis = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(el);
        var comp = vis.Compositor;
        Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetIsTranslationEnabled(el, true);

        var fadeAnim = comp.CreateScalarKeyFrameAnimation();
        fadeAnim.InsertKeyFrame(0f, startOpacity);
        fadeAnim.InsertKeyFrame(1f, 1f);
        fadeAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

        var slideAnim = comp.CreateScalarKeyFrameAnimation();
        slideAnim.InsertKeyFrame(0f, startY);
        var ease = comp.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));
        slideAnim.InsertKeyFrame(1f, 0f, ease);
        slideAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

        vis.StartAnimation("Opacity", fadeAnim);
        vis.StartAnimation("Translation.Y", slideAnim);
    }

    private void UpdateStatus(string text)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (StatusText.Text == text) return;
            StatusText.Text = text;
            SlideUpFadeIn(StatusText, startY: 6f, startOpacity: 0f, durationMs: 300);
        });
    }

    private void UpdateStatusDot(Windows.UI.Color color)
    {
        DispatcherQueue.TryEnqueue(() => StatusStrip.Background = new SolidColorBrush(color));
    }

    private void UpdateActiveKey(string label, string time, bool visible)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            bool lblChanged = ActiveKeyLabel.Text != label;
            bool tmChanged = ActiveKeyTime.Text != time;

            ActiveKeyLabel.Text = label;
            ActiveKeyTime.Text = time;

            if (visible && ActiveKeyBorder.Opacity < 1.0)
            {
                ActiveKeyBorder.Opacity = 1.0;
                SlideUpFadeIn(ActiveKeyBorder, startY: 8f, startOpacity: 0f, durationMs: 300);
            }
            else if (!visible)
            {
                ActiveKeyBorder.Opacity = 0.0;
            }

            if (!visible) return;
            if (lblChanged && !string.IsNullOrEmpty(label))
                SlideUpFadeIn(ActiveKeyLabel, startY: 3f, startOpacity: 0.6f, durationMs: 220);
            if (tmChanged && !string.IsNullOrEmpty(time))
                SlideUpFadeIn(ActiveKeyTime, startY: 3f, startOpacity: 0.6f, durationMs: 220);
        });
    }

    private void UpdateLoopCount(int count)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            string txt = count > 0 ? $"Cycle #{count}" : "Cycle --";
            bool changed = LoopCountText.Text != txt;
            LoopCountText.Text = txt;

            if (count > 0 && LoopCountText.Opacity < 1.0)
            {
                LoopCountText.Opacity = 1.0;
                SlideUpFadeIn(LoopCountText, startY: 6f, startOpacity: 0f, durationMs: 300);
            }
            else if (count > 0 && changed)
                SlideUpFadeIn(LoopCountText, startY: 3f, startOpacity: 0.6f, durationMs: 220);
            else if (count == 0)
                LoopCountText.Opacity = 0.0;
        });
    }

    private void AppendLog(string message)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var ts = DateTime.Now.ToString("HH:mm:ss");
            string cur = LogText.Text;
            string[] lines = cur.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length >= 150)
            {
                int skip = lines.Length - 100;
                LogText.Text = string.Join('\n', lines, skip, 100) + $"\n[{ts}] {message}";
            }
            else
            {
                LogText.Text = cur + (string.IsNullOrEmpty(cur) ? "" : "\n") + $"[{ts}] {message}";
            }
        });
    }

    private void LogText_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
    }

    private void PressKeyDown(ushort scanCode)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            union = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0, wScan = scanCode,
                    dwFlags = KEYEVENTF_SCANCODE,
                    time = 0, dwExtraInfo = IntPtr.Zero,
                }
            }
        };
        if (SendInput(1, [input], Marshal.SizeOf<INPUT>()) == 0)
            AppendLog($"[Warning] KeyDown fail: error {Marshal.GetLastWin32Error()}");
    }

    private void PressKeyUp(ushort scanCode)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            union = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0, wScan = scanCode,
                    dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP,
                    time = 0, dwExtraInfo = IntPtr.Zero,
                }
            }
        };
        if (SendInput(1, [input], Marshal.SizeOf<INPUT>()) == 0)
            AppendLog($"[Warning] KeyUp fail: error {Marshal.GetLastWin32Error()}");
    }

    private async Task HoldKeyAsync(ushort scanCode, TimeSpan duration, CancellationToken ct)
    {
        PressKeyDown(scanCode);
        try { await Task.Delay(duration, ct); }
        finally { PressKeyUp(scanCode); }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        _cts = new CancellationTokenSource();
        _loopCount = 0;
        var token = _cts.Token;

        try
        {
            UpdateStatusDot(ColorHelper.FromArgb(255, 227, 179, 65));
            AppendLog("Countdown started — switch to your game!");
            UpdateStatus("Switch to game!");

            double countdown = 5.0;
            while (countdown > 0 && !token.IsCancellationRequested)
            {
                UpdateActiveKey("⏳", $"{(int)Math.Ceiling(countdown)}s", visible: true);
                double d = Math.Min(1.0, countdown);
                await Task.Delay(TimeSpan.FromSeconds(d), token);
                countdown -= d;
            }

            UpdateStatusDot(ColorHelper.FromArgb(255, 63, 185, 80));

            while (!token.IsCancellationRequested)
            {
                _loopCount++;
                var (label, scanCode) = Keys[_rng.Next(Keys.Length)];
                double dur = _rng.NextDouble() * 2.0 + 1.0;
                double gap = _rng.NextDouble() * 5.0 + 10.0;

                AppendLog($"Pressing {label} for {dur:F1}s");
                UpdateLoopCount(_loopCount);
                UpdateStatus($"Pressing {label}");

                PressKeyDown(scanCode);
                try
                {
                    double rem = dur;
                    while (rem > 0 && !token.IsCancellationRequested)
                    {
                        UpdateActiveKey($"[ {label} ]", $"{(int)Math.Ceiling(rem)}s", visible: true);
                        double d = Math.Min(1.0, rem);
                        await Task.Delay(TimeSpan.FromSeconds(d), token);
                        rem -= d;
                    }
                }
                finally { PressKeyUp(scanCode); }

                AppendLog($"Waiting {gap:F0}s...");
                UpdateStatus("Waiting...");

                double wait = gap;
                while (wait > 0 && !token.IsCancellationRequested)
                {
                    UpdateActiveKey("Idle", $"{(int)Math.Ceiling(wait)}s", visible: true);
                    double d = Math.Min(1.0, wait);
                    await Task.Delay(TimeSpan.FromSeconds(d), token);
                    wait -= d;
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            UpdateStatus("Stopped");
            UpdateStatusDot(ColorHelper.FromArgb(255, 72, 79, 88));
            UpdateActiveKey("", "", visible: false);
            AppendLog($"Bot stopped after {_loopCount} cycle(s).");
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();
}
