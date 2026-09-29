using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace GlassScreenMac.Services;

public sealed class WindowEffectsService
{
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";

    private readonly Window _window;

    private IntPtr _nativeWindow;
    private bool _initialized;

    // GlassScreen's desired window state.
    private double _opacity = 1.0;
    private bool _clickThrough;
    private bool _alwaysOnTop;

    public WindowEffectsService(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public bool Initialize()
    {
        if (_initialized)
        {
            return true;
        }

        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        var platformHandle = _window.TryGetPlatformHandle();

        if (platformHandle == null ||
            platformHandle.Handle == IntPtr.Zero ||
            platformHandle.HandleDescriptor != "NSWindow")
        {
            return false;
        }

        _nativeWindow = platformHandle.Handle;
        _initialized = true;

        Reapply();

        return true;
    }

    public void SetOpacity(double opacity)
    {
        _opacity = Math.Clamp(opacity, 0.1, 1.0);

        if (!EnsureInitialized())
        {
            return;
        }

        ApplyOpacity();
    }

    public void SetClickThrough(bool enabled)
    {
        _clickThrough = enabled;

        if (!EnsureInitialized())
        {
            return;
        }

        ApplyClickThrough();
    }

    public void SetAlwaysOnTop(bool enabled)
    {
        _alwaysOnTop = enabled;

        if (!EnsureInitialized())
        {
            return;
        }

        ApplyAlwaysOnTop();
    }

    public void Reapply()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        ApplyOpacity();
        ApplyClickThrough();
        ApplyAlwaysOnTop();
    }

    public void Restore()
    {
        _opacity = 1.0;
        _clickThrough = false;
        _alwaysOnTop = false;

        if (!EnsureInitialized())
        {
            return;
        }

        Reapply();
    }

    private void ApplyOpacity()
    {
        SendDouble(
            _nativeWindow,
            "setAlphaValue:",
            _opacity);
    }

    private void ApplyClickThrough()
    {
        SendBool(
            _nativeWindow,
            "setIgnoresMouseEvents:",
            _clickThrough);
    }

    private void ApplyAlwaysOnTop()
    {
        nint level = _alwaysOnTop ? 3 : 0;

        SendInteger(
            _nativeWindow,
            "setLevel:",
            level);
    }

    private bool EnsureInitialized()
    {
        if (_initialized)
        {
            return true;
        }

        return Initialize();
    }

    private static IntPtr GetSelector(string selectorName)
    {
        return sel_registerName(selectorName);
    }

    private static void SendDouble(
        IntPtr target,
        string selector,
        double value)
    {
        objc_msgSend_double(
            target,
            GetSelector(selector),
            value);
    }

    private static void SendBool(
        IntPtr target,
        string selector,
        bool value)
    {
        objc_msgSend_bool(
            target,
            GetSelector(selector),
            value);
    }

    private static void SendInteger(
        IntPtr target,
        string selector,
        nint value)
    {
        objc_msgSend_integer(
            target,
            GetSelector(selector),
            value);
    }

    [DllImport(ObjectiveCLibrary)]
    private static extern IntPtr sel_registerName(
        string name);

    [DllImport(
        ObjectiveCLibrary,
        EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_double(
        IntPtr receiver,
        IntPtr selector,
        double value);

    [DllImport(
        ObjectiveCLibrary,
        EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_bool(
        IntPtr receiver,
        IntPtr selector,
        [MarshalAs(UnmanagedType.I1)] bool value);

    [DllImport(
        ObjectiveCLibrary,
        EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_integer(
        IntPtr receiver,
        IntPtr selector,
        nint value);
}