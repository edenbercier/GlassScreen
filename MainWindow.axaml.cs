using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using GlassScreenMac.Services;

namespace GlassScreenMac;

public partial class MainWindow : Window
{
    private sealed class BrowserTab
    {
        public required Border TabControl { get; init; }
        public required TextBlock TitleText { get; init; }
        public required NativeWebView WebView { get; init; }

        public string Title { get; set; } = "New Tab";
        public string? Address { get; set; }
        public bool HasNavigated { get; set; }
    }

    private readonly WindowEffectsService _windowEffects;
    private readonly GlassScreenFullScreenService _fullScreenService;
    private readonly MacGlobalShortcutService _globalShortcuts;

    private readonly List<BrowserTab> _tabs = new();

    private BrowserTab? _activeTab;

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

            CreateNewTab();
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

        NewTabButton.Click += (_, _) =>
        {
            CreateNewTab();
        };

        GoButton.Click += (_, _) =>
        {
            Navigate(AddressBar.Text);
        };

        ReloadButton.Click += (_, _) =>
        {
            Reload();
        };

        BackButton.Click += (_, _) =>
        {
            GoBack();
        };

        ForwardButton.Click += (_, _) =>
        {
            GoForward();
        };

        AddressBar.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                Navigate(AddressBar.Text);
                args.Handled = true;
            }
        };

        WelcomeSearchBar.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                Navigate(WelcomeSearchBar.Text);
                args.Handled = true;
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

    private void CreateNewTab()
    {
        var webView = new NativeWebView
        {
            IsVisible = false
        };

        BrowserContent.Children.Add(webView);

        var titleText = new TextBlock
        {
            Text = "New Tab",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(
                Color.Parse("#202124")),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var closeButton = new Button
        {
            Width = 22,
            Height = 22,
            Padding = new Avalonia.Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(0),
            HorizontalContentAlignment =
                HorizontalAlignment.Center,
            VerticalContentAlignment =
                VerticalAlignment.Center,
            Content = new TextBlock
            {
                Text = "×",
                FontSize = 14,
                Foreground = new SolidColorBrush(
                    Color.Parse("#555B5E"))
            }
        };

        var tabGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(
                "Auto,*,Auto"),
            ColumnSpacing = 7
        };

        var logo = new Image
        {
            Source = new Avalonia.Media.Imaging.Bitmap(
                Avalonia.Platform.AssetLoader.Open(
                    new Uri(
                        "avares://GlassScreenMac/Assets/glassscreen-logo.png"))),
            Width = 18,
            Height = 18,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(logo, 0);
        Grid.SetColumn(titleText, 1);
        Grid.SetColumn(closeButton, 2);

        tabGrid.Children.Add(logo);
        tabGrid.Children.Add(titleText);
        tabGrid.Children.Add(closeButton);

        var tabBorder = new Border
        {
            Width = 220,
            Height = 30,
            CornerRadius =
                new Avalonia.CornerRadius(7, 7, 0, 0),
            BorderThickness =
                new Avalonia.Thickness(1, 1, 1, 0),
            Padding = new Avalonia.Thickness(8, 0),
            Child = tabGrid
        };

        var tab = new BrowserTab
        {
            TabControl = tabBorder,
            TitleText = titleText,
            WebView = webView
        };

        tabBorder.PointerPressed += (_, _) =>
        {
            SelectTab(tab);
        };

        closeButton.Click += (_, args) =>
        {
            args.Handled = true;
            CloseTab(tab);
        };

        _tabs.Add(tab);

        TabPanel.Children.Add(tabBorder);

        SelectTab(tab);

        WelcomeSearchBar.Text = string.Empty;
        WelcomeSearchBar.Focus();
    }

    private void SelectTab(BrowserTab tab)
    {
        _activeTab = tab;

        foreach (var current in _tabs)
        {
            bool active = current == tab;

            current.WebView.IsVisible =
                active && current.HasNavigated;

            current.TabControl.Background =
                new SolidColorBrush(
                    Color.Parse(
                        active
                            ? "#FFFFFF"
                            : "#E5E8EA"));

            current.TabControl.BorderBrush =
                new SolidColorBrush(
                    Color.Parse("#D4D8DB"));
        }

        if (tab.HasNavigated)
        {
            NewTabPage.IsVisible = false;

            AddressBar.Text =
                tab.Address ?? string.Empty;
        }
        else
        {
            NewTabPage.IsVisible = true;

            AddressBar.Text = string.Empty;
            WelcomeSearchBar.Text = string.Empty;
        }
    }

    private void CloseTab(BrowserTab tab)
    {
        int index = _tabs.IndexOf(tab);

        if (index < 0)
        {
            return;
        }

        bool wasActive =
            tab == _activeTab;

        TabPanel.Children.Remove(tab.TabControl);
        BrowserContent.Children.Remove(tab.WebView);

        _tabs.RemoveAt(index);

        if (_tabs.Count == 0)
        {
            _activeTab = null;

            CreateNewTab();

            return;
        }

        if (wasActive)
        {
            int nextIndex =
                Math.Min(index, _tabs.Count - 1);

            SelectTab(_tabs[nextIndex]);
        }
    }

    private void Navigate(string? input)
    {
        if (_activeTab == null)
        {
            return;
        }

        input = input?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        Uri uri;

        if (Uri.TryCreate(
                input,
                UriKind.Absolute,
                out var absoluteUri) &&
            (absoluteUri.Scheme == Uri.UriSchemeHttp ||
             absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            uri = absoluteUri;
        }
        else if (!input.Contains(' ') &&
                 input.Contains('.'))
        {
            if (!Uri.TryCreate(
                    "https://" + input,
                    UriKind.Absolute,
                    out uri!))
            {
                return;
            }
        }
        else
        {
            var search =
                Uri.EscapeDataString(input);

            uri = new Uri(
                $"https://www.google.com/search?q={search}");
        }

        _activeTab.Address =
            uri.ToString();

        _activeTab.HasNavigated = true;

        _activeTab.Title =
            GetDisplayTitle(input, uri);

        _activeTab.TitleText.Text =
            _activeTab.Title;

        AddressBar.Text =
            uri.ToString();

        NewTabPage.IsVisible = false;

        foreach (var tab in _tabs)
        {
            tab.WebView.IsVisible =
                tab == _activeTab;
        }

        _activeTab.WebView.Navigate(uri);
    }

    private static string GetDisplayTitle(
        string input,
        Uri uri)
    {
        if (!input.Contains(' ') &&
            input.Contains('.'))
        {
            return uri.Host.Replace(
                "www.",
                string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        if (input.Length <= 24)
        {
            return input;
        }

        return input[..24] + "…";
    }

    private void Reload()
    {
        if (_activeTab?.HasNavigated == true)
        {
            _activeTab.WebView.Refresh();
        }
    }

    private void GoBack()
    {
        if (_activeTab?.HasNavigated == true &&
            _activeTab.WebView.CanGoBack)
        {
            _activeTab.WebView.GoBack();
        }
    }

    private void GoForward()
    {
        if (_activeTab?.HasNavigated == true &&
            _activeTab.WebView.CanGoForward)
        {
            _activeTab.WebView.GoForward();
        }
    }

    private void ToggleClickThrough()
    {
        bool enabled =
            ClickThroughCheckBox.IsChecked != true;

        ClickThroughCheckBox.IsChecked =
            enabled;

        _windowEffects.SetClickThrough(
            enabled);
    }

    private void ToggleAlwaysOnTop()
    {
        bool enabled =
            AlwaysOnTopCheckBox.IsChecked != true;

        AlwaysOnTopCheckBox.IsChecked =
            enabled;

        _windowEffects.SetAlwaysOnTop(
            enabled);
    }

    private void ToggleFullScreen()
    {
        _fullScreenService.Toggle();

        _windowEffects.Reapply();
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

        ClickThroughCheckBox.IsChecked =
            false;

        AlwaysOnTopCheckBox.IsChecked =
            false;

        _windowEffects.Restore();
    }
}