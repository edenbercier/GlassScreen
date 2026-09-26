using Avalonia.Controls;
using GlassScreenMac.Services;

namespace GlassScreenMac;

public partial class MainWindow : Window
{
    private readonly WindowService _windowService = new();

    public MainWindow()
    {
        InitializeComponent();

        RefreshButton.Click += (_, _) => LoadWindows();

        LoadWindows();
    }

    private void LoadWindows()
    {
        var windows = _windowService.GetOpenWindows();

        WindowListBox.ItemsSource = windows;

        StatusText.Text =
            windows.Count == 1
                ? "1 window found"
                : $"{windows.Count} windows found";
    }
}