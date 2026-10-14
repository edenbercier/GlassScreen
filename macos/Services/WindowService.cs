using System;
using System.Collections.Generic;
using System.Linq;
using GlassScreenMac.Models;
using GlassScreenMac.Native;

namespace GlassScreenMac.Services;

public class WindowService
{
    public List<WindowInfo> GetOpenWindows()
    {
        var windows =
            new List<WindowInfo>();

        uint options =
            MacNativeMethods.WindowListOptionOnScreenOnly |
            MacNativeMethods.WindowListExcludeDesktopElements;

        IntPtr windowList =
            MacNativeMethods.CGWindowListCopyWindowInfo(
                options,
                MacNativeMethods.NullWindowId);

        if (windowList == IntPtr.Zero)
        {
            return windows;
        }

        try
        {
            nint count =
                MacNativeMethods.CFArrayGetCount(
                    windowList);

            for (nint i = 0; i < count; i++)
            {
                IntPtr dictionary =
                    MacNativeMethods.CFArrayGetValueAtIndex(
                        windowList,
                        i);

                if (dictionary == IntPtr.Zero)
                {
                    continue;
                }

                int? windowId =
                    MacNativeMethods.GetDictionaryInt(
                        dictionary,
                        "kCGWindowNumber");

                int? processId =
                    MacNativeMethods.GetDictionaryInt(
                        dictionary,
                        "kCGWindowOwnerPID");

                int? layer =
                    MacNativeMethods.GetDictionaryInt(
                        dictionary,
                        "kCGWindowLayer");

                string? ownerName =
                    MacNativeMethods.GetDictionaryString(
                        dictionary,
                        "kCGWindowOwnerName");

                string? title =
                    MacNativeMethods.GetDictionaryString(
                        dictionary,
                        "kCGWindowName");

                if (windowId == null ||
                    processId == null ||
                    layer != 0 ||
                    string.IsNullOrWhiteSpace(ownerName))
                {
                    continue;
                }

                if (processId.Value == Environment.ProcessId)
                {
                    continue;
                }

                windows.Add(
                    new WindowInfo(
                        (uint)windowId.Value,
                        processId.Value,
                        ownerName,
                        title ?? string.Empty));
            }
        }
        finally
        {
            MacNativeMethods.CFRelease(
                windowList);
        }

        return windows
            .OrderBy(window => window.OwnerName)
            .ThenBy(window => window.Title)
            .ToList();
    }
}