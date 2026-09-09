using GlassScreen.Models;
using GlassScreen.Native;

namespace GlassScreen.Services;

public class WindowStateService
{
    private readonly Dictionary<IntPtr, WindowState> _states =
        new();

    public void CaptureIfNeeded(IntPtr handle)
    {
        if (_states.ContainsKey(handle))
        {
            return;
        }

        long originalStyle =
            NativeMethods
                .GetWindowLongPtr(
                    handle,
                    NativeMethods.GWL_EXSTYLE
                )
                .ToInt64();

        bool wasTopMost =
            (originalStyle & NativeMethods.WS_EX_TOPMOST) != 0;

        _states[handle] =
            new WindowState(
                handle,
                originalStyle,
                wasTopMost
            );
    }

    public bool Restore(IntPtr handle)
    {
        if (!_states.TryGetValue(
                handle,
                out WindowState? state))
        {
            return false;
        }

        // FIRST: put opacity back to fully opaque.
        NativeMethods.SetLayeredWindowAttributes(
            handle,
            0,
            255,
            NativeMethods.LWA_ALPHA
        );

        // Restore the exact extended style the window
        // had before GlassScreen touched it.
        NativeMethods.SetWindowLongPtr(
            handle,
            NativeMethods.GWL_EXSTYLE,
            new IntPtr(state.OriginalExtendedStyle)
        );

        // Restore original top-most state.
        IntPtr insertAfter =
            state.WasTopMost
                ? NativeMethods.HWND_TOPMOST
                : NativeMethods.HWND_NOTOPMOST;

        NativeMethods.SetWindowPos(
            handle,
            insertAfter,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE |
            NativeMethods.SWP_NOSIZE |
            NativeMethods.SWP_NOACTIVATE
        );

        _states.Remove(handle);

        return true;
    }

    public void RestoreAll()
    {
        IntPtr[] handles =
            _states.Keys.ToArray();

        foreach (IntPtr handle in handles)
        {
            Restore(handle);
        }
    }
}