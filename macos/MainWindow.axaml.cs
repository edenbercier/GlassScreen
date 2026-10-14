
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
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
    private readonly YouTubeSearchService? _youTubeSearchService;
    private readonly YouTubePlayerHost _youTubePlayerHost = new();

    private readonly List<BrowserTab> _tabs = new();
    private readonly HashSet<BrowserTab> _playerTabs = new();

    private BrowserTab? _activeTab;

    private bool _playerExpanded;
    private bool _windowFilled;

    private PixelPoint _previousPosition;
    private double _previousWidth;
    private double _previousHeight;

    public MainWindow()
    {
        InitializeComponent();

        _windowEffects = new WindowEffectsService(this);
        _fullScreenService = new GlassScreenFullScreenService(this);
        _globalShortcuts = new MacGlobalShortcutService();

        _youTubePlayerHost.Start();

        var youtubeApiKey =
            Environment.GetEnvironmentVariable("YOUTUBE_API_KEY");

        if (!string.IsNullOrWhiteSpace(youtubeApiKey))
        {
            _youTubeSearchService =
                new YouTubeSearchService(youtubeApiKey);
        }

        Opened += async (_, _) =>
        {
            _windowEffects.Initialize();

            _globalShortcuts.ClickThroughPressed += ToggleClickThrough;
            _globalShortcuts.AlwaysOnTopPressed += ToggleAlwaysOnTop;
            _globalShortcuts.FullScreenPressed += ToggleFullScreen;

            if (_globalShortcuts.HasInputMonitoringPermission())
            {
                _globalShortcuts.Start();
            }
            else
            {
                _globalShortcuts.RequestInputMonitoringPermission();

                var permissionDialog = new Window
                {
                    Title = "GlassScreen Permission",
                    Width = 440,
                    Height = 230,
                    CanResize = false,
                    WindowStartupLocation =
                        WindowStartupLocation.CenterOwner
                };

                var message = new TextBlock
                {
                    Text =
                        "Global Shortcuts Need Permission\n\n" +
                        "GlassScreen needs Input Monitoring so its keyboard " +
                        "shortcuts work while you're using other apps.\n\n" +
                        "Enable GlassScreen in Input Monitoring, then reopen GlassScreen.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(24)
                };

                var openSettingsButton = new Button
                {
                    Content = "Open System Settings",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(8)
                };

                var notNowButton = new Button
                {
                    Content = "Not Now",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(8)
                };

                openSettingsButton.Click += (_, _) =>
                {
                    MacPermissionService.OpenInputMonitoringSettings();
                    permissionDialog.Close();
                };

                notNowButton.Click += (_, _) =>
                {
                    permissionDialog.Close();
                };

                var buttons = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        openSettingsButton,
                        notNowButton
                    }
                };

                permissionDialog.Content = new StackPanel
                {
                    Children =
                    {
                        message,
                        buttons
                    }
                };

                await permissionDialog.ShowDialog(this);
            }

            CreateNewTab();
        };

        Closed += (_, _) =>
        {
            _globalShortcuts.Dispose();
            _youTubePlayerHost.Dispose();
        };

        PropertyChanged += (_, args) =>
        {
            if (args.Property == WindowStateProperty)
            {
                _windowEffects.Reapply();
            }
        };

        NewTabButton.Click += (_, _) => CreateNewTab();

        GoButton.Click += (_, _) =>
            Navigate(AddressBar.Text);

        YouTubeButton.Click += async (_, _) =>
            await SearchYouTubeAsync();

        CloseYouTubeResultsButton.Click += (_, _) =>
            YouTubeResultsPanel.IsVisible = false;

        ExpandPlayerButton.Click += (_, _) =>
            TogglePlayerExpansion();

        FillScreenButton.Click += (_, _) =>
            ToggleFillScreen();

        ReloadButton.Click += (_, _) => Reload();
        BackButton.Click += (_, _) => GoBack();
        ForwardButton.Click += (_, _) => GoForward();

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

        RestoreButton.Click += (_, _) => RestoreWindow();
    }

    private void CreateNewTab()
    {
        var webView = new NativeWebView
        {
            IsVisible = false
        };

        BrowserContent.Children.Add(webView);
        MacWebViewService.EnableElementFullscreen(webView);

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
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
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
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
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
            CornerRadius = new CornerRadius(7, 7, 0, 0),
            BorderThickness = new Thickness(1, 1, 1, 0),
            Padding = new Thickness(8, 0),
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
        if (_playerExpanded)
        {
            SetPlayerExpanded(false);
        }

        _activeTab = tab;

        ExpandPlayerButton.IsEnabled =
            _playerTabs.Contains(tab);

        foreach (var current in _tabs)
        {
            bool active = current == tab;

            current.WebView.IsVisible =
                active && current.HasNavigated;

            current.TabControl.Background =
                new SolidColorBrush(
                    Color.Parse(
                        active ? "#FFFFFF" : "#E5E8EA"));

            current.TabControl.BorderBrush =
                new SolidColorBrush(
                    Color.Parse("#D4D8DB"));
        }

        if (tab.HasNavigated)
        {
            NewTabPage.IsVisible = false;
            AddressBar.Text = tab.Address ?? string.Empty;
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

        bool wasActive = tab == _activeTab;

        if (_playerExpanded && wasActive)
        {
            SetPlayerExpanded(false);
        }

        _playerTabs.Remove(tab);

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
            int nextIndex = Math.Min(index, _tabs.Count - 1);
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
        else if (!input.Contains(' ') && input.Contains('.'))
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
            string search = Uri.EscapeDataString(input);
            uri = new Uri(
                $"https://www.google.com/search?q={search}");
        }

        _playerTabs.Remove(_activeTab);
        ExpandPlayerButton.IsEnabled = false;

        if (_playerExpanded)
        {
            SetPlayerExpanded(false);
        }

        _activeTab.Address = uri.ToString();
        _activeTab.HasNavigated = true;
        _activeTab.Title = GetDisplayTitle(input, uri);
        _activeTab.TitleText.Text = _activeTab.Title;

        AddressBar.Text = uri.ToString();
        NewTabPage.IsVisible = false;

        foreach (var tab in _tabs)
        {
            tab.WebView.IsVisible = tab == _activeTab;
        }

        _activeTab.WebView.Navigate(uri);
    }

    private static string GetDisplayTitle(string input, Uri uri)
    {
        if (!input.Contains(' ') && input.Contains('.'))
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

        ClickThroughCheckBox.IsChecked = enabled;

        _windowEffects.SetClickThrough(enabled);
        _windowEffects.Reapply();
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

    private void UpdateOpacity()
    {
        double opacity = OpacitySlider.Value / 100.0;

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

    private async Task SearchYouTubeAsync()
    {
        var query = AddressBar.Text?.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Enter a YouTube search term.");
            return;
        }

        if (_youTubeSearchService == null)
        {
            Console.WriteLine(
                "YouTube API key missing. Launch GlassScreen " +
                "with YOUTUBE_API_KEY configured.");
            return;
        }

        YouTubeButton.IsEnabled = false;

        try
        {
            Console.WriteLine($"Searching YouTube for: {query}");

            var videos =
                await _youTubeSearchService.SearchAsync(query);

            DisplayYouTubeResults(videos);

            Console.WriteLine($"Found {videos.Count} videos.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"YouTube search failed: {ex.Message}");
        }
        finally
        {
            YouTubeButton.IsEnabled = true;
        }
    }

    private void DisplayYouTubeResults(
        IReadOnlyList<YouTubeVideo> videos)
    {
        YouTubeResultsList.Children.Clear();

        YouTubeResultsTitle.Text =
            $"YouTube Search Results ({videos.Count})";

        foreach (var video in videos)
        {
            var title = new TextBlock
            {
                Text = video.Title,
                FontSize = 15,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap
            };

            var channel = new TextBlock
            {
                Text = video.ChannelTitle,
                FontSize = 12,
                Foreground = Brushes.Gray
            };

            var details = new StackPanel
            {
                Spacing = 5,
                Children =
                {
                    title,
                    channel
                }
            };

            var resultButton = new Button
            {
                Content = details,
                HorizontalContentAlignment =
                    HorizontalAlignment.Stretch,
                HorizontalAlignment =
                    HorizontalAlignment.Stretch,
                Padding = new Thickness(12),
                Background = Brushes.White
            };

            resultButton.Click += (_, _) =>
            {
                PlayYouTubeVideo(video);
            };

            YouTubeResultsList.Children.Add(resultButton);
        }

        YouTubeResultsPanel.IsVisible = true;
    }

    private void PlayYouTubeVideo(YouTubeVideo video)
    {
        YouTubeResultsPanel.IsVisible = false;

        Uri playerUri =
            _youTubePlayerHost.GetPlayerUri(video.VideoId);

        Navigate(playerUri.ToString());

        if (_activeTab != null)
        {
            _playerTabs.Add(_activeTab);
            ExpandPlayerButton.IsEnabled = true;

            _activeTab.Title = video.Title;
            _activeTab.TitleText.Text = video.Title;
        }
    }

    // Expand the embedded player within GlassScreen.
    // This does not enter macOS fullscreen.
    private void TogglePlayerExpansion()
    {
        if (_activeTab == null ||
            !_playerTabs.Contains(_activeTab))
        {
            return;
        }

        SetPlayerExpanded(!_playerExpanded);
    }

    private void SetPlayerExpanded(bool expanded)
    {
        _playerExpanded = expanded;

        TabStripBorder.IsVisible = !expanded;
        NavigationToolbarBorder.IsVisible = !expanded;

        ExpandPlayerButton.Content =
            expanded ? "Show Browser" : "Expand Player";

        if (expanded)
        {
            YouTubeResultsPanel.IsVisible = false;
        }
    }

    // Fill the current screen's available desktop area
    // without entering a macOS fullscreen Space.
    private void ToggleFillScreen()
    {
        if (_windowFilled)
        {
            Position = _previousPosition;
            Width = _previousWidth;
            Height = _previousHeight;

            _windowFilled = false;
            FillScreenButton.Content = "Fill Screen";
        }
        else
        {
            var screen = Screens.ScreenFromWindow(this);

            if (screen == null)
            {
                return;
            }

            if (WindowState != WindowState.Normal)
            {
                WindowState = WindowState.Normal;
            }

            _previousPosition = Position;
            _previousWidth = Width;
            _previousHeight = Height;

            var workingArea = screen.WorkingArea;
            double scale = screen.Scaling;

            Position = workingArea.Position;
            Width = workingArea.Width / scale;
            Height = workingArea.Height / scale;

            _windowFilled = true;
            FillScreenButton.Content = "Restore Size";
        }

        _windowEffects.Reapply();
    }
}
