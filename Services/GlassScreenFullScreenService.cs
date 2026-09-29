using System;
using Avalonia;
using Avalonia.Controls;

namespace GlassScreenMac.Services;

public sealed class GlassScreenFullScreenService
{
    private readonly Window _window;

    private PixelPoint _restorePosition;
    private double _restoreWidth;
    private double _restoreHeight;
    private bool _isFullScreen;

    public bool IsFullScreen => _isFullScreen;

    public GlassScreenFullScreenService(Window window)
    {
        _window = window ??
                  throw new ArgumentNullException(nameof(window));
    }

    public void Toggle()
    {
        if (_isFullScreen)
        {
            Exit();
        }
        else
        {
            Enter();
        }
    }

    public void Enter()
    {
        if (_isFullScreen)
        {
            return;
        }

        var screen = _window.Screens.ScreenFromWindow(_window);

        if (screen == null)
        {
            return;
        }

        // Remember exactly where GlassScreen was.
        _restorePosition = _window.Position;
        _restoreWidth = _window.Width;
        _restoreHeight = _window.Height;

        /*
         * IMPORTANT:
         *
         * We deliberately do NOT use:
         *
         * WindowState = WindowState.FullScreen
         *
         * because native macOS fullscreen moves GlassScreen
         * into a separate fullscreen Space.
         *
         * Instead, GlassScreen remains a normal window on
         * the current desktop and is expanded to the display.
         */

        _window.WindowState = WindowState.Normal;

        var bounds = screen.Bounds;

        _window.Position =
            new PixelPoint(
                bounds.X,
                bounds.Y);

        _window.Width =
            bounds.Width / screen.Scaling;

        _window.Height =
            bounds.Height / screen.Scaling;

        _isFullScreen = true;
    }

    public void Exit()
    {
        if (!_isFullScreen)
        {
            return;
        }

        _window.WindowState = WindowState.Normal;

        _window.Position = _restorePosition;
        _window.Width = _restoreWidth;
        _window.Height = _restoreHeight;

        _isFullScreen = false;
    }
}