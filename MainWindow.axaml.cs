using System;
using Avalonia.Controls;
using Avalonia.Input;
using GlassScreenMac.Services;

namespace GlassScreenMac;

public partial class MainWindow : Window
{
    private readonly WindowEffectsService _windowEffects;
    private readonly GlassScreenFullScreenService _fullScreenService;
    private readonly MacGlobalShortcutService _globalShortcuts;

    public MainWindow()
    {
        InitializeComponent();

        _windowEffects = new WindowEffectsService(this);
        _fullScreenService = new GlassScreenFullScreenService(this);
        _globalShortcuts = new MacGlobalShortcutService();

        Opened += (_, _) =>
        {
            _windowEffects.Initialize();

            _globalShortcuts.ClickThroughPressed += ToggleClickThrough;
            _globalShortcuts.AlwaysOnTopPressed += ToggleAlwaysOnTop;
            _globalShortcuts.FullScreenPressed += ToggleFullScreen;

            if (!_globalShortcuts.HasInputMonitoringPermission())
            {
                _globalShortcuts.RequestInputMonitoringPermission();
            }

            _globalShortcuts.Start();
        };

        Closed += (_, _) =>
        {
            _globalShortcuts.Dispose();
        };

        PropertyChanged += (_, args) =>
        {
            if (args.Property == WindowStateProperty)
            {
                _windowEffects.Reapply();
            }
        };

        GoButton.Click += (_, _) => Navigate();
        ReloadButton.Click += (_, _) => Reload();
        BackButton.Click += (_, _) => GoBack();
        ForwardButton.Click += (_, _) => GoForward();

        AddressBar.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                Navigate();
            }
        };

        OpacitySlider.PropertyChanged += (_, args) =>
        {
            if (args.Property == Slider.ValueProperty)
            {
                UpdateOpacity();
            }
        };

        ClickThroughCheckBox.Click += (_, _) =>
        {
            bool enabled =
                ClickThroughCheckBox.IsChecked == true;

            _windowEffects.SetClickThrough(enabled);
        };

        AlwaysOnTopCheckBox.Click += (_, _) =>
        {
            bool enabled =
                AlwaysOnTopCheckBox.IsChecked == true;

            _windowEffects.SetAlwaysOnTop(enabled);
        };

        RestoreButton.Click += (_, _) =>
        {
            RestoreWindow();
        };
    }

    private void ToggleClickThrough()
    {
        bool enabled =
            ClickThroughCheckBox.IsChecked != true;

        ClickThroughCheckBox.IsChecked = enabled;

        _windowEffects.SetClickThrough(enabled);
    }

    private void ToggleAlwaysOnTop()
    {
        bool enabled =
            AlwaysOnTopCheckBox.IsChecked != true;

        AlwaysOnTopCheckBox.IsChecked = enabled;

        _windowEffects.SetAlwaysOnTop(enabled);
    }

    private void ToggleFullScreen()
    {
        _fullScreenService.Toggle();

        _windowEffects.Reapply();
    }

    private void Navigate()
    {
        var address = AddressBar.Text?.Trim();

        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        if (!address.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase) &&
            !address.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
        {
            address = "https://" + address;
        }

        if (!Uri.TryCreate(
                address,
                UriKind.Absolute,
                out var uri))
        {
            return;
        }

        AddressBar.Text = uri.ToString();

        NewTabPage.IsVisible = false;
        WebView.IsVisible = true;

        WebView.Navigate(uri);
    }

    private void Reload()
    {
        if (WebView.IsVisible)
        {
            WebView.Refresh();
        }
    }

    private void GoBack()
    {
        if (WebView.IsVisible &&
            WebView.CanGoBack)
        {
            WebView.GoBack();
        }
    }

    private void GoForward()
    {
        if (WebView.IsVisible &&
            WebView.CanGoForward)
        {
            WebView.GoForward();
        }
    }

    private void UpdateOpacity()
    {
        double opacity =
            OpacitySlider.Value / 100.0;

        OpacityValue.Text =
            $"{(int)OpacitySlider.Value}%";

        _windowEffects.SetOpacity(opacity);
    }

    private void RestoreWindow()
    {
        OpacitySlider.Value = 100;

        ClickThroughCheckBox.IsChecked = false;
        AlwaysOnTopCheckBox.IsChecked = false;

        _windowEffects.Restore();
    }
}