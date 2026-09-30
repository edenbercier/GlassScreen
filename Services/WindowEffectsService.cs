using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace GlassScreenMac.Services;

public sealed class WindowEffectsService
{
    private const string ObjectiveCLibrary =
        "/usr/lib/libobjc.A.dylib";

    private readonly Window _window;

    private IntPtr _nativeWindow;
    private bool _initialized;

    // GlassScreen's desired window state.
    private double _opacity = 1.0;
    private bool _clickThrough;
    private bool _alwaysOnTop;

    public WindowEffectsService(Window window)
    {
        _window = window ??
            throw new ArgumentNullException(nameof(window));
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

        var platformHandle =
            _window.TryGetPlatformHandle();

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

        ApplyToApplicationWindows();
    }

    public void SetClickThrough(bool enabled)
    {
        _clickThrough = enabled;

        if (!EnsureInitialized())
        {
            return;
        }

        ApplyToApplicationWindows();
    }

    public void SetAlwaysOnTop(bool enabled)
    {
        _alwaysOnTop = enabled;

        if (!EnsureInitialized())
        {
            return;
        }

        ApplyToApplicationWindows();
    }

    public void Reapply()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        ApplyToApplicationWindows();
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

        ApplyToApplicationWindows();
    }

    private void ApplyToApplicationWindows()
    {
        // Always apply to the main GlassScreen window.
        ApplyEffects(_nativeWindow);

        IntPtr applicationClass =
            objc_getClass("NSApplication");

        if (applicationClass == IntPtr.Zero)
        {
            return;
        }

        IntPtr application =
            SendIntPtr(
                applicationClass,
                "sharedApplication");

        if (application == IntPtr.Zero)
        {
            return;
        }

        IntPtr windows =
            SendIntPtr(
                application,
                "windows");

        if (windows == IntPtr.Zero)
        {
            return;
        }

        nint count =
            SendIntegerResult(
                windows,
                "count");

        for (nint index = 0; index < count; index++)
        {
            IntPtr nativeWindow =
                SendIntPtrWithInteger(
                    windows,
                    "objectAtIndex:",
                    index);

            if (nativeWindow == IntPtr.Zero ||
                nativeWindow == _nativeWindow)
            {
                continue;
            }

            ApplyEffects(nativeWindow);
        }
    }

    private void ApplyEffects(IntPtr nativeWindow)
    {
        if (nativeWindow == IntPtr.Zero)
        {
            return;
        }

        SendDouble(
            nativeWindow,
            "setAlphaValue:",
            _opacity);

        SendBool(
            nativeWindow,
            "setIgnoresMouseEvents:",
            _clickThrough);

        nint level =
            _alwaysOnTop ? 3 : 0;

        SendInteger(
            nativeWindow,
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

    private static IntPtr GetSelector(
        string selectorName)
    {
        return sel_registerName(selectorName);
    }

    private static IntPtr SendIntPtr(
        IntPtr target,
        string selector)
    {
        return objc_msgSend_IntPtr(
            target,
            GetSelector(selector));
    }

    private static IntPtr SendIntPtrWithInteger(
        IntPtr target,
        string selector,
        nint value)
    {
        return objc_msgSend_IntPtr_integer(
            target,
            GetSelector(selector),
            value);
    }

    private static nint SendIntegerResult(
        IntPtr target,
        string selector)
    {
        return objc_msgSend_integer_result(
            target,
            GetSelector(selector));
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

    [DllImport(ObjectiveCLibrary)]
    private static extern IntPtr objc_getClass(
        string name);

    [DllImport(
        ObjectiveCLibrary,
        EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr(
        IntPtr receiver,
        IntPtr selector);

    [DllImport(
        ObjectiveCLibrary,
        EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr_integer(
        IntPtr receiver,
        IntPtr selector,
        nint value);

    [DllImport(
        ObjectiveCLibrary,
        EntryPoint = "objc_msgSend")]
    private static extern nint objc_msgSend_integer_result(
        IntPtr receiver,
        IntPtr selector);

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