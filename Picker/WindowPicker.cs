using System.Runtime.InteropServices;
using System.Text;
using GlassScreen.Models;
using GlassScreen.Native;

namespace GlassScreen.Picker;

public class WindowPicker
{
    private IntPtr _hookHandle = IntPtr.Zero;

    private NativeMethods.LowLevelMouseProc? _mouseProc;

    private TaskCompletionSource<WindowInfo?>? _completionSource;

    public Task<WindowInfo?> PickWindowAsync()
    {
        _completionSource =
            new TaskCompletionSource<WindowInfo?>();

        _mouseProc = MouseHookCallback;

        _hookHandle =
            NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_MOUSE_LL,
                _mouseProc,
                IntPtr.Zero,
                0
            );

        if (_hookHandle == IntPtr.Zero)
        {
            _completionSource.SetResult(null);
        }

        return _completionSource.Task;
    }

    private IntPtr MouseHookCallback(
        int nCode,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (nCode >= 0 &&
            wParam.ToInt32() == NativeMethods.WM_LBUTTONDOWN)
        {
            var hookData =
                Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(
                    lParam
                );

            WindowInfo? window =
                GetWindowFromPoint(hookData.pt);

            StopHook();

            _completionSource?.TrySetResult(window);
        }

        return NativeMethods.CallNextHookEx(
            _hookHandle,
            nCode,
            wParam,
            lParam
        );
    }

    private WindowInfo? GetWindowFromPoint(
        NativeMethods.POINT point)
    {
        IntPtr handle =
            NativeMethods.WindowFromPoint(point);

        if (handle == IntPtr.Zero)
        {
            return null;
        }

        handle =
            NativeMethods.GetAncestor(
                handle,
                NativeMethods.GA_ROOT
            );

        if (handle == IntPtr.Zero)
        {
            return null;
        }

        int titleLength =
            NativeMethods.GetWindowTextLength(handle);

        if (titleLength <= 0)
        {
            return null;
        }

        var builder =
            new StringBuilder(titleLength + 1);

        NativeMethods.GetWindowText(
            handle,
            builder,
            builder.Capacity
        );

        string title =
            builder.ToString();

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return new WindowInfo(
            handle,
            title
        );
    }

    private void StopHook()
    {
        if (_hookHandle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnhookWindowsHookEx(
            _hookHandle
        );

        _hookHandle = IntPtr.Zero;
    }
}