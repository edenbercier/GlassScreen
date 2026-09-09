using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using GlassScreen.Models;
using GlassScreen.Native;
using GlassScreen.Picker;
using GlassScreen.Services;
using System.Linq;

namespace GlassScreen;

public partial class MainWindow : Window
{
    private readonly WindowStateService _stateService;
    private readonly WindowEffectsService _effectsService;
    private readonly WindowPicker _windowPicker;
    private readonly HotKeyService _hotKeyService;

    private readonly ObservableCollection<WindowControlState>
        _selectedWindows = new();

    private bool _updatingCardControls;

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

        SelectedWindowsList.ItemsSource =
            _selectedWindows;

        UpdateEmptyState();
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
                RestoreActiveWindow();
                break;
            case HotKeyService.RestoreAllId:
                RestoreAllWindows();
                break;

            case HotKeyService.CycleActiveWindowId:
                CycleActiveWindow();
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
        RemoveClosedWindows();
        
        StatusText.Text =
            "Click the window you want to add...";

        PickWindowButton.IsEnabled =
            false;

        WindowInfo? pickedWindow =
            await _windowPicker.PickWindowAsync();

        PickWindowButton.IsEnabled =
            true;

        if (pickedWindow == null)
        {
            StatusText.Text =
                "No window selected.";

            return;
        }

        bool alreadySelected =
            _selectedWindows.Any(
                state =>
                    state.Window.Handle ==
                    pickedWindow.Handle
            );

        if (alreadySelected)
        {
            StatusText.Text =
                "That window is already selected.";

            return;
        }

        var state =
            new WindowControlState(
                pickedWindow
            );

        _selectedWindows.Add(
            state
        );

        SelectedWindowsList.SelectedItem =
            state;

        UpdateEmptyState();

        StatusText.Text =
            $"{_selectedWindows.Count} window(s) selected.";
    }

    private void WindowCard_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (element.DataContext is not WindowControlState state)
        {
            return;
        }

        state.IsUiReady = true;
    }

    private void WindowOpacitySlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingCardControls)
        {
            return;
        }

        if (sender is not Slider slider)
        {
            return;
        }

        if (slider.DataContext is not WindowControlState state)
        {
            return;
        }

        if (!state.IsUiReady)
        {
            return;
        }

        SelectedWindowsList.SelectedItem =
            state;

        int opacity =
            Math.Clamp(
                (int)Math.Round(slider.Value),
                10,
                100
            );

        state.Opacity =
            opacity;

        _effectsService.SetOpacity(
            state.Window.Handle,
            opacity
        );

        StatusText.Text =
            $"{state.Window.Title}: {opacity}% opacity";
    }

    private void WindowClickThroughCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (_updatingCardControls)
        {
            return;
        }

        if (sender is not CheckBox checkBox)
        {
            return;
        }

        if (checkBox.DataContext is not WindowControlState state)
        {
            return;
        }

        if (!state.IsUiReady)
        {
            return;
        }

        SelectedWindowsList.SelectedItem =
            state;

        bool enabled =
            checkBox.IsChecked == true;

        state.ClickThrough =
            enabled;

        _effectsService.SetClickThrough(
            state.Window.Handle,
            enabled
        );

        StatusText.Text =
            enabled
                ? $"{state.Window.Title}: click-through enabled"
                : $"{state.Window.Title}: click-through disabled";
    }

    private void WindowAlwaysOnTopCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (_updatingCardControls)
        {
            return;
        }

        if (sender is not CheckBox checkBox)
        {
            return;
        }

        if (checkBox.DataContext is not WindowControlState state)
        {
            return;
        }

        if (!state.IsUiReady)
        {
            return;
        }

        SelectedWindowsList.SelectedItem =
            state;

        bool enabled =
            checkBox.IsChecked == true;

        state.AlwaysOnTop =
            enabled;

        _effectsService.SetAlwaysOnTop(
            state.Window.Handle,
            enabled
        );

        StatusText.Text =
            enabled
                ? $"{state.Window.Title}: always-on-top enabled"
                : $"{state.Window.Title}: always-on-top disabled";
    }

    private void RemoveWindowButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.Tag is not WindowControlState state)
        {
            return;
        }

        _stateService.Restore(
            state.Window.Handle
        );

        _selectedWindows.Remove(
            state
        );

        if (_selectedWindows.Count > 0)
        {
            SelectedWindowsList.SelectedItem =
                _selectedWindows[0];
        }

        UpdateEmptyState();

        StatusText.Text =
            $"Removed: {state.Window.Title}";
    }

    private void RestoreButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        RestoreActiveWindow();
    }

    private void RestoreAllButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        RestoreAllWindows();
    }

    private WindowControlState? GetActiveWindow()
    {
        RemoveClosedWindows();
        
        if (SelectedWindowsList.SelectedItem
            is WindowControlState selected)
        {
            return selected;
        }

        if (_selectedWindows.Count == 0)
        {
            return null;
        }

        WindowControlState first =
            _selectedWindows[0];

        SelectedWindowsList.SelectedItem =
            first;

        return first;
    }

    private void ToggleClickThrough()
    {
        WindowControlState? state =
            GetActiveWindow();

        if (state == null)
        {
            return;
        }

        bool enabled =
            !state.ClickThrough;

        _updatingCardControls = true;

        state.ClickThrough =
            enabled;

        _updatingCardControls = false;

        _effectsService.SetClickThrough(
            state.Window.Handle,
            enabled
        );

        StatusText.Text =
            enabled
                ? $"{state.Window.Title}: click-through enabled"
                : $"{state.Window.Title}: click-through disabled";
    }

    private void ToggleAlwaysOnTop()
    {
        WindowControlState? state =
            GetActiveWindow();

        if (state == null)
        {
            return;
        }

        bool enabled =
            !state.AlwaysOnTop;

        _updatingCardControls = true;

        state.AlwaysOnTop =
            enabled;

        _updatingCardControls = false;

        _effectsService.SetAlwaysOnTop(
            state.Window.Handle,
            enabled
        );

        StatusText.Text =
            enabled
                ? $"{state.Window.Title}: always-on-top enabled"
                : $"{state.Window.Title}: always-on-top disabled";
    }

    private void ChangeOpacity(
        int amount)
    {
        WindowControlState? state =
            GetActiveWindow();

        if (state == null)
        {
            return;
        }

        int newOpacity =
            Math.Clamp(
                state.Opacity + amount,
                10,
                100
            );

        _updatingCardControls = true;

        state.Opacity =
            newOpacity;

        _updatingCardControls = false;

        _effectsService.SetOpacity(
            state.Window.Handle,
            newOpacity
        );

        StatusText.Text =
            $"{state.Window.Title}: {newOpacity}% opacity";
    }

    private void RestoreActiveWindow()
    {
        WindowControlState? state =
            GetActiveWindow();

        if (state == null)
        {
            StatusText.Text =
                "No active window.";

            return;
        }

        bool restored =
            _stateService.Restore(
                state.Window.Handle
            );

        _updatingCardControls = true;

        state.Opacity = 100;
        state.ClickThrough = false;
        state.AlwaysOnTop = false;

        _updatingCardControls = false;

        StatusText.Text =
            restored
                ? $"{state.Window.Title} restored."
                : $"{state.Window.Title} has not been modified.";
    }
    private void RestoreAllWindows()
    {
        if (_selectedWindows.Count == 0)
        {
            StatusText.Text =
                "No windows selected.";

            return;
        }

        _stateService.RestoreAll();

        _updatingCardControls = true;

        foreach (WindowControlState state in _selectedWindows)
        {
            state.Opacity = 100;
            state.ClickThrough = false;
            state.AlwaysOnTop = false;
        }

        _updatingCardControls = false;

        StatusText.Text =
            "All windows restored.";
    }
    private void CycleActiveWindow()
    {
        if (_selectedWindows.Count == 0)
        {
            StatusText.Text =
                "No windows selected.";

            return;
        }

        int currentIndex =
            SelectedWindowsList.SelectedIndex;

        int nextIndex;

        if (currentIndex < 0)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex =
                (currentIndex + 1) %
                _selectedWindows.Count;
        }

        SelectedWindowsList.SelectedIndex =
            nextIndex;

        WindowControlState active =
            _selectedWindows[nextIndex];

        SelectedWindowsList.ScrollIntoView(
            active
        );

        StatusText.Text =
            $"Active: {active.Window.Title}";
    }
    private void RemoveClosedWindows()
    {
        List<WindowControlState> closedWindows =
            _selectedWindows
                .Where(state =>
                    !NativeMethods.IsWindow(
                        state.Window.Handle
                    ))
                .ToList();

        foreach (WindowControlState state in closedWindows)
        {
            _selectedWindows.Remove(state);
        }

        UpdateEmptyState();

        if (_selectedWindows.Count > 0 &&
            SelectedWindowsList.SelectedItem == null)
        {
            SelectedWindowsList.SelectedItem =
                _selectedWindows[0];
        }
    }
    private void MinimizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState =
            System.Windows.WindowState.Minimized;
    }

    private void MaximizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState =
            WindowState == System.Windows.WindowState.Maximized
                ? System.Windows.WindowState.Normal
                : System.Windows.WindowState.Maximized;
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
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

    private void UpdateEmptyState()
    {
        EmptyStateCard.Visibility =
            _selectedWindows.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    protected override void OnClosed(
        EventArgs e)
    {
        _hotKeyService.Unregister();

        _stateService.RestoreAll();

        base.OnClosed(e);
    }
}