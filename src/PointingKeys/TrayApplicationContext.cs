using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PointingKeys;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private const string IdleTooltip = "PointingKeys — CapsLock을 누른 채 IJKL 이동 / QSW 가속 / 3·Space·E·D 좌클릭 / 4·R·F·Alt 우클릭 / N·H 스크롤";
    private const string ActiveTooltip = "PointingKeys — 조작 중";

    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _idleIcon;
    private readonly Icon _activeIcon;
    private readonly CapsLockMouseHook _hook;
    private readonly SpeedState _speedState = new();
    private readonly MouseMover _mouseMover;
    private readonly Scroller _scroller;
    private ToolStripMenuItem _momentaryMenuItem = null!;
    private ToolStripMenuItem _toggleMenuItem = null!;
    private readonly Settings _settings;

    public TrayApplicationContext()
    {
        Logger.Log("=== PointingKeys starting ===");
        _settings = Settings.Load();
        _idleIcon = CreateArrowIcon(Color.FromArgb(150, 150, 150));
        _activeIcon = CreateArrowIcon(Color.FromArgb(0, 122, 255));

        _mouseMover = new MouseMover(_speedState);
        _scroller = new Scroller(_speedState);
        _hook = new CapsLockMouseHook();
        _hook.CapsLockModeChanged += OnCapsLockModeChanged;
        _hook.KeyChanged += OnKeyChanged;
        _hook.SetMode(_settings.Mode);

        _momentaryMenuItem = new ToolStripMenuItem("누르고 있기 (기본)", null, (_, _) => SetActivationMode(CapsLockActivationMode.Momentary))
        {
            Checked = _settings.Mode == CapsLockActivationMode.Momentary,
        };
        _toggleMenuItem = new ToolStripMenuItem("토글", null, (_, _) => SetActivationMode(CapsLockActivationMode.Toggle))
        {
            Checked = _settings.Mode == CapsLockActivationMode.Toggle,
        };

        var autostart = new ToolStripMenuItem("Windows 시작 시 실행") { Checked = Autostart.IsEnabled, CheckOnClick = true };
        autostart.CheckedChanged += (_, _) => Autostart.Set(autostart.Checked);

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem($"PointingKeys v{Application.ProductVersion.Split('+')[0]}") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripLabel("CapsLock 작동 방식"));
        menu.Items.Add(_momentaryMenuItem);
        menu.Items.Add(_toggleMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(autostart);
        menu.Items.Add("로그 보기", null, (_, _) => OpenLog());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("종료", null, (_, _) => ExitApplication()));

        _notifyIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = IdleTooltip,
            ContextMenuStrip = menu,
            Visible = true,
        };
        _ = TrayPromotion.PromoteAsync();
    }

    private static void OpenLog()
    {
        try
        {
            if (!File.Exists(Logger.FilePath)) Logger.Log("log opened");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Logger.FilePath) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception ex) { Logger.Log($"open log failed: {ex}"); }
    }

    private void SetActivationMode(CapsLockActivationMode mode)
    {
        _hook.SetMode(mode);
        _momentaryMenuItem.Checked = mode == CapsLockActivationMode.Momentary;
        _toggleMenuItem.Checked = mode == CapsLockActivationMode.Toggle;

        _settings.Mode = mode;
        _settings.Save();
    }

    private void OnKeyChanged(int vkCode, bool isDown)
    {
        switch (vkCode)
        {
            case VirtualKeys.I: _mouseMover.SetDirection(Direction.Up, isDown); break;
            case VirtualKeys.K: _mouseMover.SetDirection(Direction.Down, isDown); break;
            case VirtualKeys.J: _mouseMover.SetDirection(Direction.Left, isDown); break;
            case VirtualKeys.L: _mouseMover.SetDirection(Direction.Right, isDown); break;

            case VirtualKeys.Q:
            case VirtualKeys.S:
            case VirtualKeys.W:
                _speedState.SetBoostKey(vkCode, isDown);
                break;

            case VirtualKeys.Three:
            case VirtualKeys.Space:
            case VirtualKeys.E:
            case VirtualKeys.D:
                MouseButtons.SetLeftDown(isDown);
                break;

            case VirtualKeys.Four:
            case VirtualKeys.R:
            case VirtualKeys.F:
            case VirtualKeys.LeftAlt:
            case VirtualKeys.RightAlt:
                MouseButtons.SetRightDown(isDown);
                break;

            case VirtualKeys.N: _scroller.SetDirection(ScrollDirection.Down, isDown); break;
            case VirtualKeys.H: _scroller.SetDirection(ScrollDirection.Up, isDown); break;
        }
    }

    private void OnCapsLockModeChanged(bool active)
    {
        // Fired from inside the low-level keyboard hook callback. Shell_NotifyIcon can block
        // on a busy/restarting shell; if the hook proc doesn't return within ~300ms, Windows
        // silently uninstalls it. Post the UI update instead of applying it inline so the hook
        // callback always returns immediately.
        SynchronizationContext.Current?.Post(_ =>
        {
            _notifyIcon.Icon = active ? _activeIcon : _idleIcon;
            _notifyIcon.Text = active ? ActiveTooltip : IdleTooltip;
        }, null);
    }

    // Same cursor-arrow silhouette as icon.ico (see icon.svg), just flat-tinted per state
    // instead of the gradient used for the app icon itself.
    private static readonly PointF[] ArrowShapeNorm =
    {
        new(0.20f, 0.12f),
        new(0.20f, 0.72f),
        new(0.34f, 0.58f),
        new(0.44f, 0.82f),
        new(0.54f, 0.78f),
        new(0.43f, 0.55f),
        new(0.66f, 0.55f),
    };

    private static Icon CreateArrowIcon(Color color)
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var points = new PointF[ArrowShapeNorm.Length];
            for (int i = 0; i < ArrowShapeNorm.Length; i++)
                points[i] = new PointF(ArrowShapeNorm[i].X * size, ArrowShapeNorm[i].Y * size);

            using var brush = new SolidBrush(color);
            g.FillPolygon(brush, points);
        }

        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            using var handleIcon = Icon.FromHandle(hIcon);
            return (Icon)handleIcon.Clone();
        }
        finally
        {
            NativeIconHandle.DestroyIcon(hIcon);
        }
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _mouseMover.StopAll();
        _mouseMover.Dispose();
        _scroller.StopAll();
        _scroller.Dispose();
        _hook.Dispose();
        _notifyIcon.Dispose();
        _idleIcon.Dispose();
        _activeIcon.Dispose();
        Application.Exit();
    }
}

internal static class NativeIconHandle
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr handle);
}
