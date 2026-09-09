using System.Windows.Interop;
using GlassScreen.Native;

namespace GlassScreen.Services;

public class HotKeyService
{
    public const int ToggleClickThroughId = 1;
    public const int OpacityUpId = 2;
    public const int OpacityDownId = 3;
    public const int RestoreId = 4;
    public const int ShowGlassScreenId = 5;
    public const int ToggleAlwaysOnTopId = 6;

    private IntPtr _windowHandle;

    public void Register(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;

        NativeMethods.RegisterHotKey(
            windowHandle,
            ToggleClickThroughId,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
            NativeMethods.VK_T
        );

        NativeMethods.RegisterHotKey(
            windowHandle,
            OpacityUpId,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
            NativeMethods.VK_UP
        );

        NativeMethods.RegisterHotKey(
            windowHandle,
            OpacityDownId,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
            NativeMethods.VK_DOWN
        );

        NativeMethods.RegisterHotKey(
            windowHandle,
            RestoreId,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
            NativeMethods.VK_R
        );

        NativeMethods.RegisterHotKey(
            windowHandle,
            ShowGlassScreenId,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
            NativeMethods.VK_G
        );
        NativeMethods.RegisterHotKey(
            windowHandle,
            ToggleAlwaysOnTopId,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
            NativeMethods.VK_A
        );
    }

    public void Unregister()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(
            _windowHandle,
            ToggleClickThroughId
        );

        NativeMethods.UnregisterHotKey(
            _windowHandle,
            OpacityUpId
        );

        NativeMethods.UnregisterHotKey(
            _windowHandle,
            OpacityDownId
        );

        NativeMethods.UnregisterHotKey(
            _windowHandle,
            RestoreId
        );

        NativeMethods.UnregisterHotKey(
            _windowHandle,
            ShowGlassScreenId
        );
        NativeMethods.UnregisterHotKey(
            _windowHandle,
            ToggleAlwaysOnTopId
        );
    }
}