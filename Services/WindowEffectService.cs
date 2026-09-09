using GlassScreen.Native;

namespace GlassScreen.Services;

public class WindowEffectsService
{
    private readonly WindowStateService _stateService;

    public WindowEffectsService(
        WindowStateService stateService)
    {
        _stateService = stateService;
    }

    public void SetOpacity(
        IntPtr handle,
        int opacityPercent)
    {
        _stateService.CaptureIfNeeded(handle);

        opacityPercent =
            Math.Clamp(
                opacityPercent,
                10,
                100
            );

        long style =
            NativeMethods
                .GetWindowLongPtr(
                    handle,
                    NativeMethods.GWL_EXSTYLE
                )
                .ToInt64();

        style |=
            NativeMethods.WS_EX_LAYERED;

        NativeMethods.SetWindowLongPtr(
            handle,
            NativeMethods.GWL_EXSTYLE,
            new IntPtr(style)
        );

        byte alpha =
            (byte)Math.Round(
                255 *
                (opacityPercent / 100.0)
            );

        NativeMethods.SetLayeredWindowAttributes(
            handle,
            0,
            alpha,
            NativeMethods.LWA_ALPHA
        );
    }

    public void SetClickThrough(
        IntPtr handle,
        bool enabled)
    {
        _stateService.CaptureIfNeeded(handle);

        long style =
            NativeMethods
                .GetWindowLongPtr(
                    handle,
                    NativeMethods.GWL_EXSTYLE
                )
                .ToInt64();

        if (enabled)
        {
            style |=
                NativeMethods.WS_EX_LAYERED;

            style |=
                NativeMethods.WS_EX_TRANSPARENT;
        }
        else
        {
            style &=
                ~NativeMethods.WS_EX_TRANSPARENT;
        }

        NativeMethods.SetWindowLongPtr(
            handle,
            NativeMethods.GWL_EXSTYLE,
            new IntPtr(style)
        );
    }

    public void SetAlwaysOnTop(
        IntPtr handle,
        bool enabled)
    {
        _stateService.CaptureIfNeeded(handle);

        IntPtr insertAfter =
            enabled
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
    }
}