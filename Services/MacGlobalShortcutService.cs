using System;
using System.Runtime.InteropServices;

namespace GlassScreenMac.Services;

public sealed class MacGlobalShortcutService : IDisposable
{
    private const string CoreGraphicsFramework =
        "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    private const string CoreFoundationFramework =
        "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    private const string LibSystem =
        "/usr/lib/libSystem.B.dylib";

    // CGEventTapLocation
    private const uint SessionEventTap = 1;

    // CGEventTapPlacement
    private const uint HeadInsertEventTap = 0;

    // CGEventTapOptions
    // Listen only: GlassScreen observes but does not suppress keyboard events.
    private const uint ListenOnly = 1;

    // CGEventType
    private const uint KeyDownEventType = 10;
    private const uint TapDisabledByTimeout = 0xFFFFFFFE;
    private const uint TapDisabledByUserInput = 0xFFFFFFFF;

    // CGEventField
    private const uint KeyboardEventKeycode = 9;

    // macOS virtual key codes
    private const long KeyCodeF = 3;
    private const long KeyCodeX = 7;
    private const long KeyCodeT = 17;

    // CGEventFlags
    private const ulong CommandFlagMask = 1UL << 20;
    private const ulong OptionFlagMask = 1UL << 19;

    private IntPtr _eventTap;
    private IntPtr _runLoopSource;

    private readonly CGEventTapCallback _callback;

    public event Action? ClickThroughPressed;
    public event Action? AlwaysOnTopPressed;
    public event Action? FullScreenPressed;

    public bool IsRunning =>
        _eventTap != IntPtr.Zero;

    public MacGlobalShortcutService()
    {
        // Keep the native callback alive for the lifetime of the event tap.
        _callback = HandleEvent;
    }

    public bool HasInputMonitoringPermission()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        return CGPreflightListenEventAccess();
    }

    public bool RequestInputMonitoringPermission()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        return CGRequestListenEventAccess();
    }

    public bool Start()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        if (IsRunning)
        {
            return true;
        }

        ulong eventMask =
            1UL << (int)KeyDownEventType;

        _eventTap =
            CGEventTapCreate(
                SessionEventTap,
                HeadInsertEventTap,
                ListenOnly,
                eventMask,
                _callback,
                IntPtr.Zero);

        if (_eventTap == IntPtr.Zero)
        {
            return false;
        }

        _runLoopSource =
            CFMachPortCreateRunLoopSource(
                IntPtr.Zero,
                _eventTap,
                0);

        if (_runLoopSource == IntPtr.Zero)
        {
            CFRelease(_eventTap);
            _eventTap = IntPtr.Zero;

            return false;
        }

        IntPtr mainRunLoop =
            CFRunLoopGetMain();

        IntPtr commonModes =
            GetCommonModes();

        CFRunLoopAddSource(
            mainRunLoop,
            _runLoopSource,
            commonModes);

        CGEventTapEnable(
            _eventTap,
            true);

        return true;
    }

    public void Stop()
    {
        if (_runLoopSource != IntPtr.Zero)
        {
            IntPtr mainRunLoop =
                CFRunLoopGetMain();

            IntPtr commonModes =
                GetCommonModes();

            CFRunLoopRemoveSource(
                mainRunLoop,
                _runLoopSource,
                commonModes);

            CFRelease(_runLoopSource);

            _runLoopSource = IntPtr.Zero;
        }

        if (_eventTap != IntPtr.Zero)
        {
            CGEventTapEnable(
                _eventTap,
                false);

            CFRelease(_eventTap);

            _eventTap = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private IntPtr HandleEvent(
        IntPtr proxy,
        uint type,
        IntPtr cgEvent,
        IntPtr userInfo)
    {
        // macOS can temporarily disable an event tap.
        // Re-enable it rather than silently losing GlassScreen shortcuts.
        if (type == TapDisabledByTimeout ||
            type == TapDisabledByUserInput)
        {
            if (_eventTap != IntPtr.Zero)
            {
                CGEventTapEnable(
                    _eventTap,
                    true);
            }

            return cgEvent;
        }

        if (type != KeyDownEventType)
        {
            return cgEvent;
        }

        ulong flags =
            CGEventGetFlags(cgEvent);

        bool commandPressed =
            (flags & CommandFlagMask) != 0;

        bool optionPressed =
            (flags & OptionFlagMask) != 0;

        if (!commandPressed ||
            !optionPressed)
        {
            return cgEvent;
        }

        long keyCode =
            CGEventGetIntegerValueField(
                cgEvent,
                KeyboardEventKeycode);

        switch (keyCode)
        {
            case KeyCodeX:
                ClickThroughPressed?.Invoke();
                break;

            case KeyCodeT:
                AlwaysOnTopPressed?.Invoke();
                break;

            case KeyCodeF:
                FullScreenPressed?.Invoke();
                break;
        }

        // Listen-only event tap:
        // always return the original keyboard event.
        return cgEvent;
    }

    private static IntPtr GetCommonModes()
    {
        IntPtr symbol =
            dlsym(
                new IntPtr(-2),
                "kCFRunLoopCommonModes");

        if (symbol == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Could not locate kCFRunLoopCommonModes.");
        }

        return Marshal.ReadIntPtr(symbol);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr CGEventTapCallback(
        IntPtr proxy,
        uint type,
        IntPtr cgEvent,
        IntPtr userInfo);

    [DllImport(CoreGraphicsFramework)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CGPreflightListenEventAccess();

    [DllImport(CoreGraphicsFramework)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CGRequestListenEventAccess();

    [DllImport(CoreGraphicsFramework)]
    private static extern IntPtr CGEventTapCreate(
        uint tap,
        uint place,
        uint options,
        ulong eventsOfInterest,
        CGEventTapCallback callback,
        IntPtr userInfo);

    [DllImport(CoreGraphicsFramework)]
    private static extern void CGEventTapEnable(
        IntPtr tap,
        [MarshalAs(UnmanagedType.I1)] bool enable);

    [DllImport(CoreGraphicsFramework)]
    private static extern ulong CGEventGetFlags(
        IntPtr cgEvent);

    [DllImport(CoreGraphicsFramework)]
    private static extern long CGEventGetIntegerValueField(
        IntPtr cgEvent,
        uint field);

    [DllImport(CoreFoundationFramework)]
    private static extern IntPtr CFMachPortCreateRunLoopSource(
        IntPtr allocator,
        IntPtr port,
        nint order);

    [DllImport(CoreFoundationFramework)]
    private static extern IntPtr CFRunLoopGetMain();

    [DllImport(CoreFoundationFramework)]
    private static extern void CFRunLoopAddSource(
        IntPtr runLoop,
        IntPtr source,
        IntPtr mode);

    [DllImport(CoreFoundationFramework)]
    private static extern void CFRunLoopRemoveSource(
        IntPtr runLoop,
        IntPtr source,
        IntPtr mode);

    [DllImport(CoreFoundationFramework)]
    private static extern void CFRelease(
        IntPtr cf);

    [DllImport(LibSystem)]
    private static extern IntPtr dlsym(
        IntPtr handle,
        string symbol);
}