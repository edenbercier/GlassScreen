using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GlassScreen.Models;

public class WindowControlState : INotifyPropertyChanged
{
    private int _opacity = 100;
    private bool _clickThrough;
    private bool _alwaysOnTop;

    public WindowInfo Window { get; }

    public bool IsUiReady { get; set; }

    public int Opacity
    {
        get => _opacity;

        set
        {
            if (_opacity == value)
            {
                return;
            }

            _opacity = value;
            OnPropertyChanged();
        }
    }

    public bool ClickThrough
    {
        get => _clickThrough;

        set
        {
            if (_clickThrough == value)
            {
                return;
            }

            _clickThrough = value;
            OnPropertyChanged();
        }
    }

    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;

        set
        {
            if (_alwaysOnTop == value)
            {
                return;
            }

            _alwaysOnTop = value;
            OnPropertyChanged();
        }
    }

    public WindowControlState(
        WindowInfo window)
    {
        Window = window;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName)
        );
    }
}