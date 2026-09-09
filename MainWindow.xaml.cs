using System.Windows;
using GlassScreen.Models;
using GlassScreen.Picker;
using GlassScreen.Services;
using System.Windows.Interop;
using GlassScreen.Native;
using System.Windows.Controls;

namespace GlassScreen;

public partial class MainWindow : Window
{
    private readonly WindowStateService _stateService;
    private readonly WindowEffectsService _effectsService;
    private readonly WindowPicker _windowPicker;
    private readonly HotKeyService _hotKeyService;
    
    private readonly List<WindowInfo> _selectedWindows =
        new();
    private bool _updatingControls;
    private bool _clickThroughEnabled;
    private bool _alwaysOnTopEnabled;

    public MainWindow()
    {
        InitializeComponent();

        _stateService =
            new WindowStateService();
        _effectsService =
            new WindowEffectsService(
                _stateService
            );
        _windowPicker =
            new WindowPicker();
        _hotKeyService =
            new HotKeyService();
    }
    protected override void OnSourceInitialized(
        EventArgs e)
    {
        base.OnSourceInitialized(e);

        IntPtr handle =
            new WindowInteropHelper(this).Handle;

        HwndSource? source =
            HwndSource.FromHwnd(handle);

        source?.AddHook(
            WindowMessageHook
        );

        _hotKeyService.Register(handle);
    }
    private IntPtr WindowMessageHook(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (msg != NativeMethods.WM_HOTKEY)
        {
            return IntPtr.Zero;
        }

        int hotKeyId =
            wParam.ToInt32();

        switch (hotKeyId)
        {
            case HotKeyService.ToggleClickThroughId:
                ToggleClickThrough();
                break;
            case HotKeyService.ToggleAlwaysOnTopId:
                ToggleAlwaysOnTop();
                break;
            case HotKeyService.OpacityUpId:
                ChangeOpacity(5);
                break;

            case HotKeyService.OpacityDownId:
                ChangeOpacity(-5);
                break;

            case HotKeyService.RestoreId:
                RestoreSelectedWindow();
                break;

            case HotKeyService.ShowGlassScreenId:
                ShowGlassScreen();
                break;
        }

        handled = true;

        return IntPtr.Zero;
    }

    private async void PickWindowButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        StatusText.Text =
            "Click the window you want to add...";

        PickWindowButton.IsEnabled = false;

        WindowInfo? pickedWindow =
            await _windowPicker.PickWindowAsync();

        PickWindowButton.IsEnabled = true;

        if (pickedWindow == null)
        {
            StatusText.Text =
                "No window selected.";

            return;
        }

        bool alreadySelected =
            _selectedWindows.Any(
                window =>
                    window.Handle == pickedWindow.Handle
            );

        if (alreadySelected)
        {
            StatusText.Text =
                "That window is already selected.";

            return;
        }

        _selectedWindows.Add(
            pickedWindow
        );

        RefreshSelectedWindows();

        StatusText.Text =
            $"{_selectedWindows.Count} window(s) selected.";
    }
    private void OpacitySlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingControls)
        {
            return;
        }

        int opacity =
            (int)OpacitySlider.Value;

        if (OpacityLabel != null)
        {
            OpacityLabel.Text =
                $"Opacity: {opacity}%";
        }

        if (_selectedWindows.Count == 0)
        {
            return;
        }

        foreach (WindowInfo window in _selectedWindows)
        {
            _effectsService.SetOpacity(
                window.Handle,
                opacity
            );
        }
    }

    private void ClickThroughCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (_updatingControls ||
            _selectedWindows.Count == 0)
        {
            return;
        }

        _clickThroughEnabled =
            ClickThroughCheckBox.IsChecked == true;

        foreach (WindowInfo window in _selectedWindows)
        {
            _effectsService.SetClickThrough(
                window.Handle,
                _clickThroughEnabled
            );
        }
    }
    private void AlwaysOnTopCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (_updatingControls ||
            _selectedWindows.Count == 0)
        {
            return;
        }

        _alwaysOnTopEnabled =
            AlwaysOnTopCheckBox.IsChecked == true;

        foreach (WindowInfo window in _selectedWindows)
        {
            _effectsService.SetAlwaysOnTop(
                window.Handle,
                _alwaysOnTopEnabled
            );
        }
    }
    private void RestoreButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedWindows.Count == 0)
        {
            StatusText.Text =
                "No windows selected.";

            return;
        }

        foreach (WindowInfo window in _selectedWindows)
        {
            _stateService.Restore(
                window.Handle
            );
        }

        ResetControlsForNewSelection();

        StatusText.Text =
            "Selected windows restored.";
    }
    private void RestoreAllButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _stateService.RestoreAll();

        ResetControlsForNewSelection();

        StatusText.Text =
            "All modified windows restored.";
    }
    private void ToggleClickThrough()
    {
        if (_selectedWindows.Count == 0)
        {
            return;
        }

        _clickThroughEnabled =
            !_clickThroughEnabled;

        foreach (WindowInfo window in _selectedWindows)
        {
            _effectsService.SetClickThrough(
                window.Handle,
                _clickThroughEnabled
            );
        }

        _updatingControls = true;

        ClickThroughCheckBox.IsChecked =
            _clickThroughEnabled;

        _updatingControls = false;

        StatusText.Text =
            _clickThroughEnabled
                ? "Click-through enabled."
                : "Click-through disabled.";
    }
    private void RemoveWindowButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.Tag is not WindowInfo window)
        {
            return;
        }

        _stateService.Restore(
            window.Handle
        );

        _selectedWindows.Remove(
            window
        );

        RefreshSelectedWindows();
        
        if (_selectedWindows.Count == 0)
        {
            ResetControlsForNewSelection();
        }
        StatusText.Text =
            $"Removed: {window.Title}";
    }
    private void ToggleAlwaysOnTop()
    {
        if (_selectedWindows.Count == 0)
        {
            return;
        }

        _alwaysOnTopEnabled =
            !_alwaysOnTopEnabled;

        foreach (WindowInfo window in _selectedWindows)
        {
            _effectsService.SetAlwaysOnTop(
                window.Handle,
                _alwaysOnTopEnabled
            );
        }

        _updatingControls = true;

        AlwaysOnTopCheckBox.IsChecked =
            _alwaysOnTopEnabled;

        _updatingControls = false;

        StatusText.Text =
            _alwaysOnTopEnabled
                ? "Always-on-top enabled."
                : "Always-on-top disabled.";
    }
    private void ChangeOpacity(int amount)
    {
        if (_selectedWindows.Count == 0)
        {
            return;
        }

        int newOpacity =
            Math.Clamp(
                (int)OpacitySlider.Value + amount,
                10,
                100
            );

        _updatingControls = true;

        OpacitySlider.Value =
            newOpacity;

        OpacityLabel.Text =
            $"Opacity: {newOpacity}%";

        _updatingControls = false;

        foreach (WindowInfo window in _selectedWindows)
        {
            _effectsService.SetOpacity(
                window.Handle,
                newOpacity
            );
        }

        StatusText.Text =
            $"Opacity: {newOpacity}%";
    }
    private void RestoreSelectedWindow()
    {
        if (_selectedWindows.Count == 0)
        {
            return;
        }

        foreach (WindowInfo window in _selectedWindows)
        {
            _stateService.Restore(
                window.Handle
            );
        }

        _clickThroughEnabled = false;
        _alwaysOnTopEnabled = false;

        ResetControlsForNewSelection();

        StatusText.Text =
            "Selected windows restored.";
    }
    private void ShowGlassScreen()
    {
        Show();

        WindowState =
            System.Windows.WindowState.Normal;
        Activate();

        Topmost = true;
        Topmost = false;

        Focus();
    }
    private void RefreshSelectedWindows()
    {
        SelectedWindowsList.ItemsSource = null;

        SelectedWindowsList.ItemsSource =
            _selectedWindows;

        NoWindowsText.Visibility =
            _selectedWindows.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
    }
    private void ResetControlsForNewSelection()
    {
        _updatingControls = true;

        OpacitySlider.Value = 100;
        OpacityLabel.Text = "Opacity: 100%";

        ClickThroughCheckBox.IsChecked = false;
        AlwaysOnTopCheckBox.IsChecked = false;

        _clickThroughEnabled = false;
        _alwaysOnTopEnabled = false;

        _updatingControls = false;
    }
    protected override void OnClosed(
        EventArgs e)
    {
        _hotKeyService.Unregister();

        _stateService.RestoreAll();

        base.OnClosed(e);
    }
}